using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace ChinaTower.Verification.Models.Abstractions
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
    }
}
