using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SsisFtpSource
{
    public class CustomDataColumn
    {
        public string Name = "";
        public string Type = "";
        public string Length = "";

        public CustomDataColumn(string name, string type, string length)
        {
            Name = name;
            Type = type;
            Length = length;
        }
    }
}
