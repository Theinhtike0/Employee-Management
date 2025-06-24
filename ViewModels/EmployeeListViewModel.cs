using HR_Products.Models.Entitites;

namespace HR_Products.ViewModels
{
    public class EmployeeListViewModel
    {
        public List<EmployeeProfile> Employees { get; set; }
        public bool IsAdminOrHrAdmin { get; set; }
        public bool IsFinanceAdmin { get; set; }
    }
}
