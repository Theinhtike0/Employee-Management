using HR_Products.Models.Entitites;
using System.Collections.Generic;

namespace HR_Products.ViewModels
{
    public class LeaveSchemeDetailsViewModel
    {
        public Leavescheme LeaveScheme { get; set; } = new Leavescheme();
        public List<Leaveschemetype> LeaveSchemeTypes { get; set; } = new List<Leaveschemetype>();
        public List<Leaveschemetypedetl> LeaveSchemeTypeDetl { get; set; } = new List<Leaveschemetypedetl>();

        // Optional: To easily pass a specific TypeId for creating a detail
        public int? SelectedTypeIdForDetail { get; set; }
    }
}