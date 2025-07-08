using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using HR_Products.Models.Entitites;

namespace HR_Products.Models.Entitites
{
    public class PensionRequest
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RequestId { get; set; }

        [Required]
        [ForeignKey("EmployeeProfile")]
        public int EmpeId { get; set; }

        [StringLength(200)]
        public string? EmpeName { get; set; }

        public int? ApprovedById { get; set; }

        [StringLength(200)]
        public string? ApproverName { get; set; }

        public int? ServiceYears { get; set; }

        public decimal ServiceBonus { get; set; }

        public decimal PensionSalary { get; set; }

        [ForeignKey("ApprovedById")]
        public virtual EmployeeProfile? Approver { get; set; }

        [StringLength(200)]
        public string Department { get; set; } = string.Empty; 

        [StringLength(200)]
        public string Position { get; set; } = string.Empty; 

        public int? Age { get; set; } 

        public PensionReason? Reason { get; set; }

        public DateTime RequestDate { get; set; } = DateTime.UtcNow;

        [StringLength(20)]
        public string Status { get; set; } = "Pending"; 

        [Column(TypeName = "nvarchar(max)")]
        public string Remarks { get; set; } = string.Empty;

        public DateTime? ApprovalDate { get; set; }

        [StringLength(255)]
        public string? AttachFileName { get; set; } 

        [StringLength(100)]
        public string? AttachFileType { get; set; }

        [StringLength(500)]
        public string? AttachFilePath { get; set; }

        public long? AttachFileSize { get; set; }

        [Column(TypeName = "varbinary(max)")]
        public byte[]? AttachFileContent { get; set; } 

        public DateTime? AttachFileUploadDate { get; set; }

        public virtual EmployeeProfile? EmployeeProfile { get; set; }

        [NotMapped]
        public string? DisplayWebPath { get; set; }

        [NotMapped]
        public bool IsImageFile { get; set; }
    }

    public enum PensionReason
    {
        [Display(Name = "Age Pension (62 Years)")]
        Age_Pension_62_Years = 1,

        [Display(Name = "Service & Age Pension (Total 75)")]
        Service_And_Age_Pension_75 = 2,

        [Display(Name = "Medical Pension")]
        Medical_Pension = 3,

        [Display(Name = "Service Length Pension (30 Years)")]
        Service_Length_Pension_30_Years = 4

    }
}