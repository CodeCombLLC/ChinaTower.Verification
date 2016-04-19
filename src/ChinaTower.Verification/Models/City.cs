using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CodeComb.Algorithm.Geography;
using Newtonsoft.Json;

namespace ChinaTower.Verification.Models
{
    public class City
    {
        [MaxLength(16)]
        public string Id { get; set; }
         
        public string EdgeJson { get; set; }

        [NotMapped]
        public Polygon Edge
        {
            get
            {
                return JsonConvert.DeserializeObject<Polygon>(EdgeJson);
            }
        }
    }
}
