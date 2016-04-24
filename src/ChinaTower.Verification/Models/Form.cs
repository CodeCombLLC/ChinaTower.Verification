using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ChinaTower.Verification.Models.Infrastructures;
using Newtonsoft.Json;

namespace ChinaTower.Verification.Models
{
    public class Form
    {
        #region Real fields
        public long Id { get; set; }

        [MaxLength(64)]
        public string UniqueKey { get; set; }

        public long? StationKey { get; set; }

        public DateTime VerificationTime { get; set; }

        public FormType Type { get; set; }

        public VerificationStatus Status { get; set; }

        public string VerificationJson { get; set; }

        public double? Lon { get; set; }

        public double? Lat { get; set; }

        public string FormJson { get; set; }

        [MaxLength(32)]
        public string City { get; set; }

        [MaxLength(32)]
        public string District { get; set; }
        #endregion
        #region Nested objects parsing
        [NotMapped]
        public ICollection<VerificationLog> VerificationLogs
        {
            get { return JsonConvert.DeserializeObject<ICollection<VerificationLog>>(VerificationJson); }
        }
        
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
                var headers = Hash.Headers[Type];
                for (var i = 0; i < headers.Count(); i++)
                    ret.Add(headers[i], stringArray[i]);
                return ret;
            }
        }
        #endregion
    }
}
