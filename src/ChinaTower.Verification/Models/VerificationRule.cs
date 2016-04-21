using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ChinaTower.Verification.Models.Infrastructures;
using CodeComb.Data.Verification.EntityFramework;

namespace ChinaTower.Verification.Models
{
    public class VerificationRule
    {
        public Guid Id { get; set; }

        [MaxLength(64)]
        public string Alias { get; set; }

        public FormType Type { get; set; }

        [ForeignKey("Rule")]
        public Guid RuleId { get; set; }

        public virtual DataVerificationRule Rule { get; set; }
    }
}
