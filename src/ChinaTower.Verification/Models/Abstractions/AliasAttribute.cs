using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChinaTower.Verification.Models.Abstractions
{
    public class AliasAttribute : Attribute
    {
        public string Name { get; set; }
        public int Index { get; set; }
        public AliasAttribute(string name, int index)
        {
            Name = name;
            Index = index;
        }
    }
}
