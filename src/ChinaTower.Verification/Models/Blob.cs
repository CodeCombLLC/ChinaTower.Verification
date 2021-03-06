﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ChinaTower.Verification.Models.Infrastructures;

namespace ChinaTower.Verification.Models
{
    public class Blob
    {
        public Guid Id { get; set; }

        [MaxLength(64)]
        public string FileName { get; set; }

        public byte[] Content { get; set; }

        [MaxLength(64)]
        public string ContentType { get; set; }

        public long ContentLength { get; set; }

        public DateTime Time { get; set; }

        [ForeignKey("Form")]
        public long? FormId { get; set; }

        public virtual Form Form { get; set; }

        [ForeignKey("User")]
        public string UserId { get; set; }

        public virtual User User { get; set; }

        public ImageType? Type { get; set; }
    }
}
