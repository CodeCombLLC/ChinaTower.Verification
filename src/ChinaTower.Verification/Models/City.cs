using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace ChinaTower.Verification.Models
{
    public class City
    {
        public long Id { get; set; }

        [MaxLength(16)]
        public string Title { get; set; }

        public string BorderJson { get; set; }
    }
}
