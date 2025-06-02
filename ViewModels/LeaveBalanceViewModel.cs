namespace HR_Products.ViewModels
{
    public class LeaveBalanceViewModel
    {
        public int EmpeId { get; set; }

        public string EmpeName { get; set; }

        public int Age { get; set; }

        public int ServiceYear { get; set; }

        public List<LeaveBalanceDetail> LeaveBalances { get; set; }
    }

    public class LeaveBalanceDetail
    {
        public string LeaveType { get; set; }
        public int Balance { get; set; }
    }

}
