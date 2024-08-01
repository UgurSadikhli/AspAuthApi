using Microsoft.AspNetCore.Identity;

namespace Task.Models
{
    public class ApplicationUser : IdentityUser
    {
       public string AvatarUrl { get; set; } = "https://ugurstorage.blob.core.windows.net/profile-images/art21.jpg";
    }
}