using System.ComponentModel.DataAnnotations;

namespace ChinaTower.Verification.Models
{
    public class City
    {
        [MaxLength(16)]
        public string Id { get; set; }

        public string BorderJson { get; set; }
    }
}
