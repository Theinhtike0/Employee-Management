using HR_Products.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HR_Products.ViewModels
{
    public class CreateLeaveSchemeViewModel
    {
        public HR_Products.Models.Entitites.Leavescheme LeaveScheme { get; set; }
        public List<SelectListItem> LeaveTypes { get; set; }
    }

}
