using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;
using ChinaTower.Verification.Models.Infrastructures;
using Newtonsoft.Json;

namespace ChinaTower.Verification.Models
{
    public class Form
    {
        public long Id { get; set; }

        public DateTime VerificationTime { get; set; }

        public FormType Type { get; set; }

        public VerificationStatus Status { get; set; }

        public string VerificationJson { get; set; }

        [NotMapped]
        public ICollection<VerificationLog> VerificationLogs
        {
            get { return JsonConvert.DeserializeObject<ICollection<VerificationLog>>(VerificationJson); }
        }
        
        public string FormJson { get; set; }
        
        [NotMapped]
        public string[] FormStringArray
        {
            get
            {
                return JsonConvert.DeserializeObject<string[]>(FormJson);
            }
        }

        [NotMapped]
        public Dictionary<string, string> FormDictionary
        {
            get
            {
                var ret = new Dictionary<string, string>();
                var stringArray = FormStringArray;
                var headers = Headers.Dictionary[Type];
                for (var i = 0; i < headers.Count(); i++)
                    ret.Add(headers[i], stringArray[i]);
                return ret;
            }
        }
    }
}
