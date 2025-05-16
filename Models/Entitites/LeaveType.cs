using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace HR_Products.Models.Entitites
{
    public class LeaveType
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LEAV_TYPE_ID { get; set; }

        [Required(ErrorMessage = "Leave Type Name is required.")]
        [StringLength(50)]
        public string LEAV_TYPE_NAME { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        [StringLength(255)]
        public string DESCRIPTION { get; set; }

        public string? IS_PAID { get; set; }

        [Required(ErrorMessage = "Default day is required.")]
        public int DEFAULT_DAY_PER_YEAR { get; set; }

        [Required(ErrorMessage = "Accrual method is required.")]
        [StringLength(255)]
        public string ACCRUAL_METHOD { get; set; }

        [Required(ErrorMessage = "Carry is required.")]
        [StringLength(20)]
        public string CARRY_OVER_LIMIT { get; set; }

        public string? IS_ACTIVE { get; set; }

        public string? REQUIRE_APPROVAL { get; set; }

        public string? ATTACH_REQUIRE { get; set; }

        [Required(ErrorMessage = "Gender Specific is required.")]
        [StringLength(255)]
        public string GENDER_SPECIFIC { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
