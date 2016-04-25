using System;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace ChinaTower.Verification.Models
{
    public class Log
    {
        public Guid Id { get; set; }

        [ForeignKey("Form")]
        public long FormId { get; set; }

        public virtual Form Form { get; set; }

        public string OriginJson { get; set; } = "[]";

        public string ModifiedJson { get; set; } = "[]";

        public DateTime Time { get; set; }

        [ForeignKey("User")]
        public string UserId { get; set; }

        public User User { set; get; }

        [NotMapped]
        public string[] Origin
        {
            get
            {
                return JsonConvert.DeserializeObject<string[]>(OriginJson);
            }
        }

        [NotMapped]
        public string[] Modified
        {
            get
            {
                return JsonConvert.DeserializeObject<string[]>(ModifiedJson);
            }
        }
    }
}
