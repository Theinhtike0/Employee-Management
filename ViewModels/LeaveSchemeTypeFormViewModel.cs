using System;
using System.Collections.Generic;
using HR_Products.Models.Entitites;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace HR_Products.ViewModels
{
    public class LeaveSchemeTypeFormViewModel
    {
        public Leavescheme LeaveScheme { get; set; }
        public Leaveschemetype LeaveSchemeType { get; set; }
        public IEnumerable<SelectListItem> LeaveTypes { get; set; } // You likely won't need this dropdown anymore

        [Required(ErrorMessage = "From Year is required")]
        public int FROM_YEAR { get; set; }

        [Required(ErrorMessage = "To Year is required")]
        public int TO_YEAR { get; set; }

        [Required(ErrorMessage = "Day per year is required")]
        public int DAYS_PER_YEAR { get; set; }

        public DateTime UPDATED_AT { get; set; }

        public int TYPE_ID { get; set; } // You might need to handle this differently if it's auto-filled
        public int SCHEME_ID { get; set; }
    }
}