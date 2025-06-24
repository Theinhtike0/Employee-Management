namespace HR_Products.Models.Entitites
{
    public class Payroll
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

        public DateTime FrDate { get; set; }

        public DateTime ToDate { get; set; }

        public DateTime PayDate { get; set; }

        public virtual EmployeeProfile Employee { get; set; } // Navigation property

    }
}