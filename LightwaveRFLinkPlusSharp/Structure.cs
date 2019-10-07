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
        public Device[] Devices { get; }

        internal Structure(JToken structureJson)
        {
            try
            {
                Devices = structureJson["devices"].Select(x => new Device(x)).ToArray();
            }
            catch
            {
                throw new UnexpectedJsonException("Unable to parse structure's devices", structureJson.ToString());
            }
        }
    }
}
