namespace HR_Products.ViewModels
{
    public class PensionBonusViewModel
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public int ServiceYears { get; set; }
        public decimal LastMonthBasicSalary { get; set; }
        public decimal ServiceYearBonus { get; set; }
        public bool IsEligible => ServiceYears >= 1; // Minimum 1 year to be eligible
    }
}
