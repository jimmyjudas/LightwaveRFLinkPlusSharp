using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightwaveRFLinkPlusSharp
{
    public class Device
    {
        public string Id { get; }
        public string Name { get; }

        /// <summary>
        /// A list of the device's features. If you are wanting a specific feature's ID, use one of the helper
        /// properties (e.g. <see cref="SwitchFeatureId"/>) or the generic <see cref="GetFeatureId(string)"/> instead
        /// </summary>
        public List<Feature> Features { get; }

        internal Device(JToken deviceJson)
        {
            try
            {
                Id = deviceJson["deviceId"].ToString();
            }
            catch
            {
                throw new UnexpectedJsonException("Unable to parse device ID", deviceJson.ToString());
            }

            try
            {
                Name = deviceJson["name"].ToString();
            }
            catch
            {
                throw new UnexpectedJsonException("Unable to parse device name", deviceJson.ToString());
            }

            try
            {
                Features = new List<Feature>();

                JToken featureSets = deviceJson["featureSets"];
                if (featureSets != null)
                {
                    foreach (var featureSet in featureSets)
                    {
                        JToken features = featureSet["features"];
                        if (features != null)
                        {
                            foreach (var feature in features)
                            {
                                Features.Add(new Feature(feature["type"].ToString(), feature["featureId"].ToString()));
                            }
                        }
                    }
                }
            }
            catch
            {
                throw new UnexpectedJsonException("Unable to parse device's features", deviceJson.ToString());
            }
        }

        #region Properties for accessing the IDs of known feature types

        public string BulbSetupFeatureId => GetFeatureId("bulbSetup");
        public string ButtonPressFeatureId => GetFeatureId("buttonPress");
        public string CurrentTimeFeatureId => GetFeatureId("currentTime");
        public string DateFeatureId => GetFeatureId("date");
        public string DawnTimeFeatureId => GetFeatureId("dawnTime");
        public string DayFeatureId => GetFeatureId("day");
        public string DiagnosticsFeatureId => GetFeatureId("diagnostics");
        public string DimLevelFeatureId => GetFeatureId("dimLevel");
        public string DimSetupFeatureId => GetFeatureId("dimSetup");

        /// <summary>
        /// Feature returns time in seconds. Note, the LightwaveAPI class has a typed <see cref="LightwaveAPI.GetDuskTimeTimeZoneAdjustedAsync(Device)"/>
        /// helper method
        /// </summary>
        public string DuskTimeFeatureId => GetFeatureId("duskTime");

        public string EnergyFeatureId => GetFeatureId("energy");
        public string IdentifyFeatureId => GetFeatureId("identify");
        public string LocationLatitudeFeatureId => GetFeatureId("locationLatitude");
        public string LocationLongitudeFeatureId => GetFeatureId("locationLongitude");
        public string MonthFeatureId => GetFeatureId("month");
        public string MonthArrayFeatureId => GetFeatureId("monthArray");
        public string PeriodOfBroadcastFeatureId => GetFeatureId("periodOfBroadcast");
        public string PowerFeatureId => GetFeatureId("power");
        public string ProtectionFeatureId => GetFeatureId("protection");
        public string ResetFeatureId => GetFeatureId("reset");
        public string RGBColorFeatureId => GetFeatureId("rgbColor");

        /// <summary>
        /// Feature returns 1 for on, 0 for off. Note, the LightwaveAPI class has typed <see cref="LightwaveAPI.GetSwitchStateAsync(Device)"/> or 
        /// <see cref="LightwaveAPI.SetSwitchStateAsync(Device)"/> helper methods
        /// </summary>
        public string SwitchFeatureId => GetFeatureId("switch");

        public string TimeFeatureId => GetFeatureId("time");

        /// <summary>
        /// Feature returns number of seconds (positive or negative) offset from GMT. Note, the LightwaveAPI class has a
        /// <see cref="LightwaveAPI.GetFeatureTimeZoneAdjusted(string, string)"/> helper method which uses this feature to
        /// adjust all time-based features to the computer's timezone.
        /// </summary>
        public string TimeZoneFeatureId => GetFeatureId("timeZone");

        public string UpgradeFeatureId => GetFeatureId("upgrade");
        public string WeekdayFeatureId => GetFeatureId("weekday");
        public string WeekdayArrayFeatureId => GetFeatureId("weekdayArray");
        public string YearFeatureId => GetFeatureId("year");

        #endregion

        /// <summary>
        /// Gets the ID for a device's feature. Note there are also properties available for many common features - e.g. for
        /// the "switch" feature (whether the device is on or off), use SwitchFeatureId instead.
        /// </summary>
        /// <param name="type">The "type" of the device's desired feature, e.g. the "switch" feature controls whether the device
        /// is turned on or not</param>
        /// <returns>The ID of the device's feature, which can then be used with <see cref="LightwaveAPI.GetFeatureValueAsync(string)"/> or <see cref="LightwaveAPI.SetFeatureValueAsync(string, int)"/></returns>
        public string GetFeatureId(string type)
        {
            Feature match = Features.FirstOrDefault(x => x.Type == type);
            return match?.Id;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}