using Microsoft.AspNetCore.Mvc.Rendering;

namespace HR_Products.ViewModels
{
    public class PayrollCreateViewModel
    {
        public int PayrollId { get; set; }
        public int EmpeId { get; set; }
        public string EmpeName { get; set; }
        public string Department { get; set; }
        public string Position { get; set; }

        public decimal OvertimeHours { get; set; }
        public decimal BasicSalary { get; set; }
        public decimal Allowance { get; set; }
        public decimal GrossPay { get; set; }
        public decimal Tax { get; set; }
        public decimal Deductions { get; set; }
        public decimal NetPay { get; set; }
        public DateTime FrDate { get; set; } = DateTime.Today;
        public DateTime ToDate { get; set; } = DateTime.Today;
        public DateTime PayDate { get; set; } = DateTime.Today;

        public List<SelectListItem> Employees { get; set; } = new List<SelectListItem>();
        public bool IsAdmin { get; set; }
    }
}
