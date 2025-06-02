using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HR_Products.Models.Entitites
{
    public class EmployeeProfile
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] 
        public int EmpeId { get; set; }

        public string UserGuid { get; set; }

        [Required (ErrorMessage = "Employee Code is required.")]
        [StringLength(50)]
        public string EmpeCode { get; set; }

        [Required (ErrorMessage = "EmpeName is required.")]
        [StringLength(255)]
        public string EmpeName { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [Required (ErrorMessage = "Gender is required.")]
        [StringLength(1)]
        public string Gender { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [StringLength(255)]
        public string Email { get; set; }

        [Required(ErrorMessage = "PhoneNo is required.")]
        [StringLength(20)]
        public string PhoneNo { get; set; }

        [StringLength(255)]
        public string EmgcConctName { get; set; }

        [StringLength(20)]
        public string EmgcConctPhone { get; set; }

        [Required]
        [StringLength(255)]
        public string JobTitle { get; set; }

        public int? DeptId { get; set; }

        [Required]
        public DateTime JoinDate { get; set; }

        [Required]
        [StringLength(10)]
        public string Status { get; set; }

        public DateTime? TerminateDate { get; set; }

        [StringLength(100)]
        public string City { get; set; }

        [StringLength(100)]
        public string State { get; set; }

        [StringLength(20)]
        public string PostalCode { get; set; }

        [StringLength(100)]
        public string Country { get; set; }

        [StringLength(255)]
        public string ProfilePic { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}