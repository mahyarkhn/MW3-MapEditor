using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapEdit.Core
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class VariableAttribute : Attribute
    {
        string comment = string.Empty;
        public VariableAttribute() { }
        public VariableAttribute(string comment) { Comment = comment; }

        public string Comment { get => comment; set => comment = value; }
    }
}
