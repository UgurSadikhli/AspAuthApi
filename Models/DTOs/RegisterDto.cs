namespace Task.DTOs
{
    public class RegisterDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
         public IFormFile ProfileImage { get; set; } 
    }
}