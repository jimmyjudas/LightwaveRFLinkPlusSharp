using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace LightwaveRFLinkPlusSharp
{
    public class LightwaveAPI
    {
        private Uri _baseAddress = new Uri("https://publicapi.lightwaverf.com/v1/");

        private Authentication _auth;

        public LightwaveAPI(string bearer, string initialRefreshToken = null)
        {
            _auth = new Authentication(bearer, initialRefreshToken);
        }

        #region Generalised API calls

        private async Task<JObject> GetAsync(string uriSegment)
        {
            return await GetOrPostAsync(uriSegment, CallMode.GET, null);
        }

        private async Task<JObject> PostAsync(string uriSegment, string body)
        {
            return await GetOrPostAsync(uriSegment, CallMode.POST, body);
        }

        private async Task<JObject> GetOrPostAsync(string uriSegment, CallMode callMode, string body, bool forceRefreshAccessToken = false)
        {
            string accessToken = await _auth.GetAccessTokenAsync(forceRefreshAccessToken);

            using (var httpClient = new HttpClient { BaseAddress = _baseAddress })
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", $"bearer {accessToken}");

                HttpResponseMessage response;
                if (callMode == CallMode.POST)
                {
                    if (body == null)
                    {
                        throw new Exception();
                    }

                    var httpRequestContent = new StringContent(body);
                    httpRequestContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    response = await httpClient.PostAsync(_baseAddress + uriSegment, httpRequestContent);
                    if (response.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        throw new Exception();
                    }
                }
                else
                {
                    response = await httpClient.GetAsync(_baseAddress + uriSegment);
                }

                string responseData = await response.Content.ReadAsStringAsync();
                JObject json = JsonConvert.DeserializeObject<JObject>(responseData);

                response.Dispose();

                if (json.ContainsKey("message") && json["message"].ToString() == "Unauthorized")
                {
                    // The access token we were just given doesn't work. Try the call again, this time forcing a refresh of the access token
                    await GetOrPostAsync(uriSegment, callMode, body, true);
                }

                return json;
            }
        }

        private enum CallMode
        {
            GET,
            POST
        }

        #endregion

        #region Official API Endpoints

        /// <summary>
        /// Gets the IDs of the structures in your LinkPlus ecosystem. For more details on structures
        /// see https://linkpluspublicapi.docs.apiary.io/#introduction/structure. 
        /// </summary>
        public async Task<string[]> GetStructuresAsync()
        {
            JObject json = await GetAsync("structures");

            return json["structures"].ToObject<string[]>();
        }

        /// <summary>
        /// Gets the details of the specified structure from your LinkPlus ecosystem. For more details on structures
        /// see https://linkpluspublicapi.docs.apiary.io/#introduction/structure.
        /// </summary>
        /// <returns>A JSON object describing the structure</returns>
        public async Task<JObject> GetStructureAsync(string structureId)
        {
            if (structureId == null)
            {
                return null;
            }

            return await GetAsync($"structure/{structureId}");
        }

        /// <summary>
        /// Gets the value of a specified feature for a device, e.g. whether the device is on or off
        /// </summary>
        /// <param name="featureId">The ID of the feature on the device. This is not the same as the feature
        /// _type_, e.g. "switch". Instead, get the ID from the Device using either one of the helper properties 
        /// (e.g. SwitchFeatureId) or the generic GetFeatureId</param>
        public async Task<int> GetFeatureValueAsync(string featureId)
        {
            if (featureId == null)
            {
                return -1;
            }

            JObject json = await GetAsync($"feature/{featureId}");
            return json["value"].ToObject<int>();
        }

        /// <summary>
        /// Sets the value of a specified feature for a device, e.g. whether the device is on or off
        /// </summary>
        /// <param name="featureId">The ID of the feature on the device. This is not the same as the feature
        /// _type_, e.g. "switch". Instead, get the ID from the Device using either one of the helper properties 
        /// (e.g. SwitchFeatureId) or the generic GetFeatureId</param>
        /// <param name="newValue">The numerical value to which you want to set the feature</param>
        public async Task SetFeatureValueAsync(string featureId, int newValue)
        {
            string body = JsonConvert.SerializeObject(new
            {
                value = newValue
            });

            await PostAsync($"feature/{featureId}", body);
            return;
        }

        public async Task<Dictionary<string, int>> GetFeatureValuesAsync(IEnumerable<string> featureIds)
        {
            var featureIdsArray = featureIds.Select(x => new { featureId = x });
            string body = JsonConvert.SerializeObject(new { features = featureIdsArray });

            JObject json = await PostAsync("features/read", body);

            return json.ToObject<Dictionary<string, int>>();
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets the details of the first "structure" in your LinkPlus ecosystem. This is a helper for if your ecosystem 
        /// only has a single structure; if you have more than one, use GetStructuresAsync instead. For more details on structures, 
        /// see https://linkpluspublicapi.docs.apiary.io/#introduction/structure.
        /// </summary>
        /// <returns>A JSON object describing the structure</returns>
        public async Task<JObject> GetFirstStructureAsync()
        {
            string[] structures = await GetStructuresAsync();
            string structureId = structures[0];
            return await GetStructureAsync(structureId);
        }

        /// <summary>
        /// Gets the details of the devices in the first structure in your LinkPlus ecosystem. This is a helper for if your ecosystem 
        /// only has a single structure; if you have more than one, use GetStructuresAsync and GetDevicesAsync instead. For more details on 
        /// structures, see https://linkpluspublicapi.docs.apiary.io/#introduction/structure.
        /// </summary>
        public async Task<Device[]> GetDevicesInFirstStructureAsync()
        {
            return GetDevices(await GetFirstStructureAsync());
        }

        /// <summary>
        /// Gets the details for the devices in a specified structure. For more details on structures,
        /// see https://linkpluspublicapi.docs.apiary.io/#introduction/structure.
        /// </summary>
        public async Task<Device[]> GetDevicesAsync(string structureId)
        {
            JObject json = await GetStructureAsync(structureId);

            return GetDevices(json);
        }

        private Device[] GetDevices(JObject structureJson)
        {
            return structureJson["devices"].Select(x => new Device(x)).ToArray();
        }

        public async Task PopulateFeatureValuesAsync(Device device)
        {
            Dictionary<string, int> featureValues = await GetFeatureValuesAsync(device.Features.Select(x => x.Id));

            foreach (var featureValue in featureValues)
            {
                var matchingFeature = device.Features.FirstOrDefault(x => x.Id == featureValue.Key);
                if (matchingFeature != null)
                {
                    matchingFeature.Value = featureValue.Value;
                }
            }
        }

        #endregion

        #region Typed helper methods for getting and setting the state of various features

        /// <summary>
        /// Returns true if the device is switched on, or false if not
        /// </summary>
        public async Task<bool> GetSwitchStateAsync(Device device)
        {
            int featureValue = await GetFeatureValueAsync(device.SwitchFeatureId);
            return featureValue == 1 ? true : false;
        }

        /// <summary>
        /// Turn the device on or off
        /// </summary>
        public async Task SetSwitchStateAsync(Device device, bool on)
        {
            await SetFeatureValueAsync(device.SwitchFeatureId, on ? 1 : 0);
        }

        #endregion
    }
}
