using LightwaveRFLinkPlusSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExampleApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            LightwaveAPI api = new LightwaveAPI("INSERT BEARER TOKEN HERE", "INSERT INITIAL REFRESH TOKEN HERE");

            // Get a list of devices present in the LinkPlus' first "structure". This is a helper method for if your LinkPlus ecosystem 
            // only has a single structure - see https://linkpluspublicapi.docs.apiary.io/#introduction/structure for more details
            Device[] devices = await api.GetDevicesInFirstStructure();

            Console.WriteLine($"{devices.Count()} devices discovered:");
            foreach (var discoveredDevice in devices)
            {
                Console.WriteLine($"\t{discoveredDevice.Name}");
            }
            Console.WriteLine();

            // Get a device by name
            Device device = devices.First(x => x.Name == "INSERT DEVICE NAME HERE");

            // Get the current value of the device's "switch" feature, i.e. whether it is currently switched on or off. This is done
            // by getting the ID of the device's switch feature and then querying the value of that feature
            string featureId = device.GetFeatureId("switch");
            int featureValue = await api.GetFeatureValue(featureId);

            // For a lot of the feature types, however, there are helper properties that get the feature ID for you. The above query
            // can therefore be simplified to just:
            featureValue = await api.GetFeatureValue(device.SwitchFeatureId);

            // Toggle this switch state
            int toggledValue = 1 - featureValue;

            // Set this as the new value for the device's switch feature, i.e. turn the device on or off
            await api.SetFeatureValue(device.SwitchFeatureId, toggledValue);

            await Task.Delay(3000);

            // Note for some features, there are also typed helper methods for getting and setting states
            bool on = await device.GetSwitchStateAsync(api);
            await device.SetSwitchStateAsync(false, api);

            Console.WriteLine("Press enter to exit...");
            Console.ReadLine();
        }
    }
}
