using System.ComponentModel.DataAnnotations;

namespace HR_Products.Models.Entitites
{
    public class Holiday
    {
        [Key]
        public int HolidayId { get; set; }

        [Required(ErrorMessage = "Holiday Name is required.")]
        [StringLength(100)]
        public string HolidayName { get; set; }

        [Required(ErrorMessage = "Holiday Date is required.")]
        public DateTime HolidayDate { get; set; }

        [StringLength(255)]
        public string Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
