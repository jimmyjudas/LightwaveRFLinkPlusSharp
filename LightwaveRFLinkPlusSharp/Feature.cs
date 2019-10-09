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

        internal Feature(string type, string id, int? value = null)
        {
            Type = type;
            Id = id;
            Value = value;
        }

        /// <summary>
        /// Displays the Feature's Type and Value, if it has one
        /// </summary>
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
