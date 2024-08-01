using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Task.DTOs;
using Task.Models;
using Task.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using System.IO;
using Task.Data;

namespace Task.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly BlobService _blobService;
        private readonly IConfiguration _configuration;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly MyDbContext _dbContext;

        public AuthController(UserManager<ApplicationUser> userManager, BlobService blobService, IConfiguration configuration,BlobServiceClient blobServiceClient,MyDbContext dbContext)
        {
            _userManager = userManager;
            _blobService = blobService;
            _configuration = configuration;
            _blobServiceClient = blobServiceClient;
            _dbContext = dbContext;
        }


        [HttpPost("signup")]
        public async Task<IActionResult> Signup([FromForm] RegisterDto dto)
        {
            if (dto == null || !ModelState.IsValid)
            {
                return BadRequest("Invalid data.");
            }

            string imageUrl = null;
            if (dto.ProfileImage != null && dto.ProfileImage.Length > 0)
            {
                var blobContainerClient = _blobService.GetBlobContainerClient("profile-images");
                await blobContainerClient.CreateIfNotExistsAsync();

                var blobClient = blobContainerClient.GetBlobClient(dto.ProfileImage.FileName);
                using (var stream = dto.ProfileImage.OpenReadStream())
                {
                    await blobClient.UploadAsync(stream);
                }

                imageUrl = blobClient.Uri.ToString();
            }

            var user = new ApplicationUser { UserName = dto.Email, Email = dto.Email, AvatarUrl = imageUrl };
            var result = await _userManager.CreateAsync(user, dto.Password);

            if (result.Succeeded)
            {
                return Ok();
            }

            var errors = result.Errors.Select(e => e.Description).ToArray();
            return BadRequest(errors);
        }


        [HttpPost("signin")]
        public async Task<IActionResult> Signin([FromBody] LoginDto dto)
        {
            var user = await _userManager.FindByEmailAsync(dto.Email);

            if (user != null && await _userManager.CheckPasswordAsync(user, dto.Password))
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new Claim[]
                    {
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim(ClaimTypes.NameIdentifier, user.Id)
                    }),
                    Expires = DateTime.UtcNow.AddHours(1),
                    Issuer = _configuration["Jwt:Issuer"],
                    Audience = _configuration["Jwt:Issuer"],
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(token);

                return Ok(new { Token = tokenString });
            }

            return Unauthorized();
        }


        [HttpGet("avatar")]
        public async Task<IActionResult> GetUserAvatar()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    
            if (userId == null)
            {
            return Unauthorized("User not authenticated");
            }

            var user = await _userManager.FindByIdAsync(userId);
    
            if (user == null || string.IsNullOrEmpty(user.AvatarUrl))
            {
            return NotFound("User or avatar not found");
            }

            return Ok(new { AvatarUrl = user.AvatarUrl });
        }


        [HttpPut("avatar")]
        public async Task<IActionResult> UpdateAvatar(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found.");
            }
            var containerName = "profile-images";
            var blobContainerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await blobContainerClient.CreateIfNotExistsAsync();
            
            var fileName = file.FileName;
            var blobClient = blobContainerClient.GetBlobClient(fileName);
            using (var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, overwrite: true);
            }
            var avatarUrl = blobClient.Uri.ToString();

            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            if (!string.IsNullOrEmpty(user.AvatarUrl))
            {
                var oldBlobClient = blobContainerClient.GetBlobClient(new Uri(user.AvatarUrl).Segments.Last());
                if (await oldBlobClient.ExistsAsync())
                {
                    await oldBlobClient.DeleteAsync();
                }
            }
            user.AvatarUrl = avatarUrl;
            await _dbContext.SaveChangesAsync();

            return Ok(new { AvatarUrl = avatarUrl });
        }

    }

}
