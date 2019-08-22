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

        public Feature(string type, string id)
        {
            Type = type;
            Id = id;
        }

        public override string ToString()
        {
            return Type;
        }
    }
}
