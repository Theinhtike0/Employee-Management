using Microsoft.AspNetCore.Identity;

namespace HR_Products.Models
{
    public class Users : IdentityUser
    {
        public string FullName { get; set; }
    }
}
