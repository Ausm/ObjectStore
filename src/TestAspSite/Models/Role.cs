using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TestEmpty.Models
{
    public class Role
    {
        public Role(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }
}
