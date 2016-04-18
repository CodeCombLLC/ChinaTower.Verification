﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChinaTower.Verification.Models.Abstractions
{
    public class VerificationLog
    {
        public string Field { get; set; }

        public string Reason { get; set; }

        public DateTime Time { get; set; }
    }
}