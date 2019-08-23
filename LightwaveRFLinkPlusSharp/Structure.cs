using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightwaveRFLinkPlusSharp
{
    public class Structure
    {
        public Device[] Devices { get; set; }

        internal Structure(JToken structureJson)
        {
            JToken devices = structureJson["devices"];
            if (devices != null)
            {
                Devices = devices.Select(x => new Device(x)).ToArray();
            }
        }
    }
}
