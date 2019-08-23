using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightwaveRFLinkPlusSharp
{
    public class Feature
    {
        public string Type { get; set; }
        public string Id { get; set; }
        public int? Value { get; set; }

        public Feature(string type, string id, int? value = null)
        {
            Type = type;
            Id = id;
            Value = value;
        }

        public override string ToString()
        {
            string output = Type;
            
            if (Value.HasValue)
            {
                output += $": {Value}";
            }

            return output;
        }
    }
}
