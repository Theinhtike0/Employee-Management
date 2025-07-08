using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using HR_Products.Models.Entitites; 
using System;

namespace HR_Products.Models.Entitites
{
    public class ServiceBonusRequest
    {
        [Key]
        public int RequestId { get; set; }

        [Required]
        public int EmpeId { get; set; }

        [ForeignKey("EmpeId")]
        public EmployeeProfile Employee { get; set; }

        [Required]
        public string EmpeName { get; set; }

        [Required]
        public int ServiceYears { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal BasicSalary { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal BonusAmount { get; set; }

        [Required]
        public DateTime RequestDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Last Date")]
        [DataType(DataType.Date)]
        public DateTime? LastDate { get; set; } 

        [Required]
        public string Status { get; set; } = "Pending";

        [ForeignKey("ApprovedById")] 
        public EmployeeProfile? Approver { get; set; } 
        public int? ApprovedById { get; set; }

        public string? ApproverName { get; set; }

        public DateTime? ApprovalDate { get; set; }
    }
}