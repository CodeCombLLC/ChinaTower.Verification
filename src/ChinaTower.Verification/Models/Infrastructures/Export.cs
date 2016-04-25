using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChinaTower.Verification.Models.Infrastructures
{
    public class Export
    {
        public long TimeStamp { get; set; }

        public DateTime Expire { get; set; }

        public byte[] Blob { get; set; }

        public string UserId { get; set; }
    }
}
