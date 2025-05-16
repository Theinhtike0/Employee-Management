//using System.ComponentModel.DataAnnotations;
//namespace HR_Products.ViewModels
//{
//    public class RegisterViewModel
//    {
//        [Required(ErrorMessage ="Name is required.")]

//        public string Name { get; set; }

//        [Required (ErrorMessage ="Email is Required.")]
//        [EmailAddress]

//        public string Email { get; set; }

//        [Required(ErrorMessage ="Password is required.")]
//        [StringLength(40,MinimumLength =8,ErrorMessage ="The {0} must be at {2} and at max {1} character long.")]
//        [DataType(DataType.Password)]
//        [Display(Name = "New Password")]

//        public string Password { get; set; }

//        [Required(ErrorMessage ="Confirm Password is required.")]
//        [DataType(DataType.Password)]
//        [Display(Name = "Confirm Password")]

//        [Compare("ConfirmPassword", ErrorMessage = "Password does not match.")]
//        public string ConfirmPassword { get; set; }
//    }
//}

using System.ComponentModel.DataAnnotations;

namespace HR_Products.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Name is required.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(40, MinimumLength = 8, ErrorMessage = "Password must be 8-40 characters.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Confirm Password is required.")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; }
    }
}