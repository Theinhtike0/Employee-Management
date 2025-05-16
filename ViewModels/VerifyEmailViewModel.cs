﻿using System.ComponentModel.DataAnnotations;

namespace HR_Products.ViewModels
{
    public class VerifyEmailViewModel
    {
        [Required (ErrorMessage = "Email is Required.")]
        [EmailAddress]

        public string Email { get; set; }
    }
}
