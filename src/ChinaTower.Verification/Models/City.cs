using System.Collections.Generic;
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

        public string EdgeJson { get; set; } = "[]";

        public string DistrictJson { get; set; } = "[]";

        [NotMapped]
        public Polygon Edge
        {
            get
            {
                return JsonConvert.DeserializeObject<Polygon>(EdgeJson);
            }
        }

        [NotMapped]
        public List<string> Districts
        {
            get
            {
                return JsonConvert.DeserializeObject<List<string>>(DistrictJson);
            }
        }
    }
}
