using Microsoft.AspNetCore.Identity;

namespace Task.Models
{
    public class ApplicationUser : IdentityUser
    {
       public string AvatarUrl { get; set; } = "https://ugurstorage.blob.core.windows.net/avatar/588e6dd2fc07831400c09b69249d6ade.jpg";
    }
}