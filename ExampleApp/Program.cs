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
            // In order to connect to the Lightwave API you must provide a bearer ID and an initial refresh token. You can get these
            // from https://my.lightwaverf.com > Settings > API. (The bearer ID is the long string labelled "Basic" for some reason.)
            // During use of the API, further refresh tokens will be provided which will be handled for you automatically. If you stop
            // being able to access the API at any point, however, you will have to request a new refresh token from the Lightwave site
            // and provide it in this constructor.
            LightwaveAPI api = new LightwaveAPI("INSERT BEARER TOKEN HERE", "INSERT INITIAL REFRESH TOKEN HERE");

            // Get a list of devices present in the LinkPlus' first "structure". This is a helper method for if your LinkPlus ecosystem 
            // only has a single structure; if you have more than one, use <see cref="GetStructuresAsync"/> and <see cref="GetDevicesAsync(string)"/>
            // instead. See https://linkpluspublicapi.docs.apiary.io/#introduction/structure for more details about Structures
            Device[] devices = await api.GetDevicesInFirstStructureAsync();

            // Output a list of all the devices along with their features and values
            Console.WriteLine($"{devices.Count()} devices discovered:");
            foreach (var discoveredDevice in devices)
            {
                Console.WriteLine($"\t{discoveredDevice.Name}");

                await api.PopulateFeatureValuesAsync(discoveredDevice);

                foreach (var deviceFeature in discoveredDevice.Features)
                {
                    Console.WriteLine($"\t\t{deviceFeature}");
                }
            }
            Console.WriteLine();

            // Get a device by name
            Device device = devices.First(x => x.Name == "INSERT DEVICE NAME HERE");

            // Get the current value of the device's "switch" feature, i.e. whether it is currently switched on or off. This is done
            // by getting the ID of the device's switch feature and then querying the value of that feature
            string featureId = device.GetFeatureId("switch");
            int featureValue = await api.GetFeatureValueAsync(featureId);

            // For a lot of the feature types, however, there are helper properties that get the feature ID for you. The above query
            // can therefore be simplified to just:
            featureValue = await api.GetFeatureValueAsync(device.SwitchFeatureId);

            // Note that for the older Connect devices, the LinkPlus may not know the current state if the last state change was not
            // triggered by the app or the API, as Connect devices do not support 2-way communication. Therefore, if the device is
            // currently switched on it may still return a value of 0

            // Toggle this switch state
            int toggledValue = 1 - featureValue;

            // Set this as the new value for the device's switch feature, i.e. turn the device on or off
            Console.WriteLine($"Changing switch state of {device.Name}...");
            await api.SetFeatureValueAsync(device.SwitchFeatureId, toggledValue);

            await Task.Delay(3000);

            // Note for some features, there are also typed helper methods for getting and setting states
            Console.WriteLine($"Changing one more time...");
            bool on = await api.GetSwitchStateAsync(device);
            await api.SetSwitchStateAsync(device, !on);

            Console.WriteLine();
            Console.WriteLine("Press enter to exit...");
            Console.ReadLine();
        }
    }
}
