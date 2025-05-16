using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HR_Products.Models.Entitites
{
    public class Leaveschemetypedetl
    {
        [Key] // Composite primary key
        public int DETL_ID { get; set; }

        public int TYPE_ID { get; set; } // Foreign key to LEAV_SCHEME_TYPE
        public int SCHEME_ID { get; set; } // Foreign key to LEAV_SCHEME_TYPE
        public string SCHEME_NAME { get; set; } // Foreign key to LEAV_SCHEME_TYPE

        public int FROM_YEAR { get; set; }

        public string LEAVE_TYPE_NAME { get; set; }
        public int TO_YEAR { get; set; }
        public int DAYS_PER_YEAR { get; set; }
        public DateTime CREATED_AT { get; set; }
        public DateTime UPDATED_AT { get; set; }

        // Navigation property
        public Leaveschemetype LEAV_SCHEME_TYPE { get; set; }
    }
}