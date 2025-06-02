namespace HR_Products.Models.Entitites
{
    using Microsoft.AspNetCore.Identity;

    namespace HR_Products.Models.Entities
    {
        public class Users : IdentityUser // Must inherit from IdentityUser
        {
            // Add any custom properties here
            public string? CustomProperty { get; set; }
        }
    }
}
