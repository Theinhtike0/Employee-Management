using HR_Products.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HR_Products.ViewModels
{
    public class CreateLeaveSchemeViewModel
    {
        public List<SelectListItem> LeaveTypes { get; set; }
    }

}
