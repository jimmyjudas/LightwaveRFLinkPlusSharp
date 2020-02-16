
(For an easier-to-read version of this, see https://github.com/jimmyjudas/LightwaveRFLinkPlusSharp/blob/master/README.md)

# LightwaveRFLinkPlusSharp

#### A C# binding for the LightwaveRF LinkPlus API

In order to connect to the Lightwave API you must provide a bearer ID and an initial refresh token. You can get these from https://my.lightwaverf.com > Settings > API. (The bearer ID is the long string labelled "Basic" for some reason.) During use of the API, further refresh tokens will be provided which will be handled for you automatically. If you stop being able to access the API at any point, however, you will have to request a new refresh token from the Lightwave site and provide it in this constructor.

To start using the API, first create an instance of it:

`LightwaveAPI api = new LightwaveAPI("INSERT BEARER TOKEN HERE", "INSERT INITIAL REFRESH TOKEN HERE");`

You can then get a list of devices present in the LinkPlus' first "structure" using the method below. This is a helper method for if your LinkPlus ecosystem  only has a single structure; if you have more than one, use `GetStructuresAsync` and `GetDevicesAsync(string)` instead. See https://linkpluspublicapi.docs.apiary.io/#introduction/structure for more details about Structures.

`Device[] devices = await api.GetDevicesInFirstStructureAsync();`

This provides a list of devices and their features: the "switch" feature, for example, is used to control whether a device is currently switched on or not. At this stage, however, we only know what features each device supports, not the current value of each feature. We can get all the values for a device's features using `PopulateFeatureValuesAsync`:

```
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
```

We can also get the details for a particular device by name:

`Device device = devices.First(x => x.Name == "INSERT DEVICE NAME HERE");`

And then read the current value of a device's particular feature by getting the ID of the feature and then querying its value. For example, to get the value of a device's "switch" feature:

```
string featureId = device.GetFeatureId("switch");
int featureValue = await api.GetFeatureValueAsync(featureId);
```

For a lot of the feature types, however, there are helper properties that get the feature ID for you. The above query can therefore be simplified to just:

`int featureValue = await api.GetFeatureValueAsync(device.SwitchFeatureId);`

> Note that for the older Connect devices, the LinkPlus may not know the current state if the last state change was not triggered by the app or the API, as Connect devices do not support 2-way communication. Therefore, if the device is currently switched on it may still return a value of 0

In order to change the value of a device's feature, use `SetFeatureValue`. For example, to turn the device on:

`await api.SetFeatureValueAsync(device.SwitchFeatureId, toggledValue);`

Note for some features, there are also typed helper methods for getting and setting states, for example:

```
bool on = await api.GetSwitchStateAsync(device);
await api.SetSwitchStateAsync(device, !on);
```
