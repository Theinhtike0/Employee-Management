using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HR_Products.Models.Entitites
{
    public class Leavescheme
    {
        [Key] // Composite primary key
        public int SCHEME_ID { get; set; }

        public string SCHEME_NAME { get; set; }

        public string DESCRIPTION { get; set; }

        public DateTime CREATED_AT { get; set; }
        public DateTime UPDATED_AT { get; set; }

        // Navigation property for child records
        public ICollection<Leaveschemetype> LEAV_SCHEME_TYPE { get; set; }
    }
}