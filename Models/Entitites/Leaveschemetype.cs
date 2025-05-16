using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HR_Products.Models.Entitites
{
    public class Leaveschemetype
    {
        [Key] // Composite primary key
        public int TYPE_ID { get; set; }

        public int SCHEME_ID { get; set; } // Foreign key to LEAV_SCHEME
        public string SCHEME_NAME { get; set; } // Foreign key to LEAV_SCHEME

        public int LEAVE_TYPE_ID { get; set; } // Foreign key to LEAVE_TYPE
        public string LEAVE_TYPE_NAME { get; set; } // Added LEAVE_TYPE_NAME

        public DateTime CREATED_AT { get; set; }
        public DateTime UPDATED_AT { get; set; }

        // Navigation properties
        public Leavescheme LEAV_SCHEME { get; set; }
        public LeaveType LEAVE_TYPE { get; set; }
        public ICollection<Leaveschemetypedetl> LEAV_SCHEME_TYPE_DETL { get; set; }
    }
}
