using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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

        /// <summary>
        /// In order to connect to the Lightwave API you must provide a bearer ID and an initial refresh token. You can get these
        /// from https://my.lightwaverf.com > Settings > API. (The bearer ID is the long string labelled "Basic" for some reason.)
        /// During use of the API, further refresh tokens will be provided which will be handled for you automatically. If you stop
        /// being able to access the API at any point, however, you will have to request a new refresh token from the Lightwave site
        /// and provide it in this constructor.
        /// </summary>
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

        private async Task<JObject> GetOrPostAsync(string uriSegment, CallMode callMode, string body, Guid? uniqueRequestIdentifier = null)
        {
            if (!uniqueRequestIdentifier.HasValue)
            {
                //This is a new request
                uniqueRequestIdentifier = Guid.NewGuid();
            }

            string accessToken = await _auth.GetAccessTokenAsync(uniqueRequestIdentifier.Value);

            using (var httpClient = new HttpClient { BaseAddress = _baseAddress })
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", $"bearer {accessToken}");

                HttpResponseMessage response = null;
                JObject json = null;
                LightwaveAPIRequestException exception = null;
                try
                {
                    if (callMode == CallMode.POST)
                    {
                        if (body == null)
                        {
                            throw new InvalidDataException("Trying to POST without any body will fail");
                        }

                        var httpRequestContent = new StringContent(body);
                        httpRequestContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                        response = await httpClient.PostAsync(_baseAddress + uriSegment, httpRequestContent);
                        if (response.StatusCode != HttpStatusCode.OK)
                        {
                            exception = new LightwaveAPIRequestException($"POST to {uriSegment} failed with status {response.StatusCode}. Body was: {body}.", response.StatusCode);
                        }
                    }
                    else
                    {
                        response = await httpClient.GetAsync(_baseAddress + uriSegment);
                        if (response.StatusCode != HttpStatusCode.OK)
                        {
                            exception = new LightwaveAPIRequestException($"GET from {uriSegment} failed with status {response.StatusCode}", response.StatusCode);
                        }
                    }

                    string responseData = await response.Content.ReadAsStringAsync();
                    json = JsonConvert.DeserializeObject<JObject>(responseData);
                }
                finally
                {
                    response?.Dispose();
                }

                if (exception != null)
                {
                    if (json != null && json["message"]?.ToString() == "Unauthorized")
                    {
                        // The access token we were just given doesn't work. Try the call again, this time forcing a refresh of the access token
                        return await GetOrPostAsync(uriSegment, callMode, body, uniqueRequestIdentifier);
                    }

                    throw exception;
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
        /// <exception cref="UnexpectedJsonException">Thrown when the Json received from the web API call can not be parsed as expected</exception>
        /// <exception cref="LightwaveAPIRequestException">Thrown when the web API call returns an unsuccessful status</exception>
        public async Task<string[]> GetStructuresAsync()
        {
            JObject json = await GetAsync("structures");

            string[] structures;
            try
            {
                structures = json["structures"].ToObject<string[]>();
            }
            catch
            {
                throw new UnexpectedJsonException("Unable to parse 'structures' array", json.ToString());
            }

            return structures;
        }

        /// <summary>
        /// Gets the details of the specified structure from your LinkPlus ecosystem. For more details on structures
        /// see https://linkpluspublicapi.docs.apiary.io/#introduction/structure.
        /// </summary>
        /// <returns>A JSON object describing the structure</returns>
        /// <exception cref="StructureNotFoundException">Thrown when the specified Structure cannot be found</exception>
        /// <exception cref="UnexpectedJsonException">Thrown when the Json received from the web API call can not be parsed as expected</exception>
        /// <exception cref="LightwaveAPIRequestException">Thrown when the web API call returns an unsuccessful status</exception>
        public async Task<Structure> GetStructureAsync(string structureId)
        {
            if (structureId == null)
            {
                return null;
            }

            JObject json = await GetAsync($"structure/{structureId}");

            if (json != null && json["message"]?.ToString() == "Discovery Failed")
            {
                throw new StructureNotFoundException(structureId);
            }

            return new Structure(json);
        }

        /// <summary>
        /// Gets the value of a specified feature for a device, e.g. whether the device is on or off
        /// </summary>
        /// <param name="featureId">The ID of the feature on the device. This is not the same as the feature
        /// _type_, e.g. "switch". Instead, get the ID from the Device using either one of the helper properties 
        /// (e.g. <see cref="Device.SwitchFeatureId"/>) or the generic <see cref="Device.GetFeatureId(string)"/></param>
        /// <exception cref="FeatureNotFoundException">Thrown when the specified Feature cannot be found</exception>
        /// <exception cref="UnexpectedJsonException">Thrown when the Json received from the web API call can not be parsed as expected</exception>
        /// <exception cref="LightwaveAPIRequestException">Thrown when the web API call returns an unsuccessful status</exception>
        public async Task<int> GetFeatureValueAsync(string featureId)
        {
            JObject json = await GetAsync($"feature/{featureId}");

            if (json != null && json.ContainsKey("message") && json["message"].ToString().Contains("Invalid feature id"))
            {
                throw new FeatureNotFoundException(featureId);
            }

            int featureValue;
            try
            {
                featureValue = json["value"].ToObject<int>();
            }
            catch
            {
                throw new UnexpectedJsonException("Unable to parse 'value' object", json.ToString());
            }

            return featureValue;
        }

        /// <summary>
        /// Sets the value of a specified feature for a device, e.g. whether the device is on or off
        /// </summary>
        /// <param name="featureId">The ID of the feature on the device. This is not the same as the feature
        /// _type_, e.g. "switch". Instead, get the ID from the Device using either one of the helper properties 
        /// (e.g. <see cref="Device.SwitchFeatureId"/>) or the generic <see cref="Device.GetFeatureId(string)"/></param>
        /// <param name="newValue">The numerical value to which you want to set the feature</param>
        /// <exception cref="FeatureNotFoundException">Thrown when the specified Feature cannot be found</exception>
        /// <exception cref="UnexpectedJsonException">Thrown when the Json received from the web API call can not be parsed as expected</exception>
        /// <exception cref="LightwaveAPIRequestException">Thrown when the web API call returns an unsuccessful status</exception>
        public async Task SetFeatureValueAsync(string featureId, int newValue)
        {
            string body = JsonConvert.SerializeObject(new
            {
                value = newValue
            });

            try
            {
                await PostAsync($"feature/{featureId}", body);
            }
            catch (LightwaveAPIRequestException ex)
            {
                if (ex.StatusCode == HttpStatusCode.BadRequest)
                {
                    throw new FeatureNotFoundException(featureId);
                }

                throw;
            }
        }

        /// <summary>
        /// Gets the values of a collection of specified features, e.g. whether the device is on or off
        /// </summary>
        /// <param name="featureIds">A collection of feature IDs</param>
        /// <returns>A dictionary of the feature IDs and values. Any unknown feature IDs will return with a value of 0. Any invalid feature IDs will result in a <see cref="LightwaveAPIRequestException"/> being thrown</returns>
        /// <exception cref="UnexpectedJsonException">Thrown when the Json received from the web API call can not be parsed as expected</exception>
        /// <exception cref="LightwaveAPIRequestException">Thrown when the web API call returns an unsuccessful status</exception>
        public async Task<Dictionary<string, int>> GetFeatureValuesAsync(IEnumerable<string> featureIds)
        {
            var featureIdsArray = featureIds.Select(x => new { featureId = x });
            string body = JsonConvert.SerializeObject(new { features = featureIdsArray });

            JObject json = await PostAsync("features/read", body);

            Dictionary<string, int> results;
            try
            {
                results = json.ToObject<Dictionary<string, int>>();
            }
            catch
            {
                throw new UnexpectedJsonException("Unable to parse feature values", json.ToString());
            }

            return results;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets the details of the first "structure" in your LinkPlus ecosystem. This is a helper for if your ecosystem 
        /// only has a single structure; if you have more than one, use <see cref="GetStructuresAsync"/> instead. For more details on structures, 
        /// see https://linkpluspublicapi.docs.apiary.io/#introduction/structure.
        /// </summary>
        /// <returns>A JSON object describing the structure</returns>
        /// <exception cref="NoStructuresFoundException">Thrown when no Structures can be found in your LinkPlus ecosystem</exception>
        /// <exception cref="StructureNotFoundException">Thrown when the specified Structure cannot be found</exception>
        /// <exception cref="UnexpectedJsonException">Thrown when the Json received from the web API call can not be parsed as expected</exception>
        /// <exception cref="LightwaveAPIRequestException">Thrown when the web API call returns an unsuccessful status</exception>
        public async Task<Structure> GetFirstStructureAsync()
        {
            string[] structures = await GetStructuresAsync();

            if (!structures.Any())
            {
                throw new NoStructuresFoundException();
            }

            string structureId = structures[0];
            return await GetStructureAsync(structureId);
        }

        /// <summary>
        /// Gets the details of the devices in the first structure in your LinkPlus ecosystem. This is a helper for if your ecosystem 
        /// only has a single structure; if you have more than one, use <see cref="GetStructuresAsync"/> and <see cref="GetDevicesAsync(string)"/> instead. For more details on 
        /// structures, see https://linkpluspublicapi.docs.apiary.io/#introduction/structure.
        /// </summary>
        /// <exception cref="NoStructuresFoundException">Thrown when no Structures can be found in your LinkPlus ecosystem</exception>
        /// <exception cref="StructureNotFoundException">Thrown when the specified Structure cannot be found</exception>
        /// <exception cref="UnexpectedJsonException">Thrown when the Json received from the web API call can not be parsed as expected</exception>
        /// <exception cref="LightwaveAPIRequestException">Thrown when the web API call returns an unsuccessful status</exception>
        public async Task<Device[]> GetDevicesInFirstStructureAsync()
        {
            Structure structure = await GetFirstStructureAsync();
            return structure.Devices;
        }

        /// <summary>
        /// Gets the details for the devices in a specified structure. For more details on structures,
        /// see https://linkpluspublicapi.docs.apiary.io/#introduction/structure.
        /// </summary>
        /// <exception cref="StructureNotFoundException">Thrown when the specified Structure cannot be found</exception>
        /// <exception cref="UnexpectedJsonException">Thrown when the Json received from the web API call can not be parsed as expected</exception>
        /// <exception cref="LightwaveAPIRequestException">Thrown when the web API call returns an unsuccessful status</exception>
        public async Task<Device[]> GetDevicesAsync(string structureId)
        {
            Structure structure = await GetStructureAsync(structureId);
            return structure.Devices;
        }

        /// <summary>
        /// Populates the specified device's Features with their current values
        /// </summary>
        /// <param name="device"></param>
        /// <exception cref="UnexpectedJsonException">Thrown when the Json received from the web API call can not be parsed as expected</exception>
        /// <exception cref="LightwaveAPIRequestException">Thrown when the web API call returns an unsuccessful status</exception>
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
        /// <exception cref="FeatureNotFoundException">Thrown when the specified Feature cannot be found</exception>
        /// <exception cref="UnexpectedJsonException">Thrown when the Json received from the web API call can not be parsed as expected</exception>
        /// <exception cref="LightwaveAPIRequestException">Thrown when the web API call returns an unsuccessful status</exception>
        public async Task<bool> GetSwitchStateAsync(Device device)
        {
            int featureValue = await GetFeatureValueAsync(device.SwitchFeatureId);
            return featureValue == 1 ? true : false;
        }

        /// <summary>
        /// Turn the device on or off
        /// </summary>
        /// <exception cref="FeatureNotFoundException">Thrown when the specified Feature cannot be found</exception>
        /// <exception cref="UnexpectedJsonException">Thrown when the Json received from the web API call can not be parsed as expected</exception>
        /// <exception cref="LightwaveAPIRequestException">Thrown when the web API call returns an unsuccessful status</exception>
        public async Task SetSwitchStateAsync(Device device, bool on)
        {
            await SetFeatureValueAsync(device.SwitchFeatureId, on ? 1 : 0);
        }

        /// <summary>
        /// Get the dusk time according to the device. Only works for the Link Plus Hub?
        /// </summary>
        /// <exception cref="FeatureNotFoundException">Thrown when the specified Feature cannot be found</exception>
        /// <exception cref="UnexpectedJsonException">Thrown when the Json received from the web API call can not be parsed as expected</exception>
        /// <exception cref="LightwaveAPIRequestException">Thrown when the web API call returns an unsuccessful status</exception>
        public async Task<TimeSpan> GetDuskTimeAsync(Device device)
        {
            int featureValue = await GetFeatureValueAsync(device.DuskTimeFeatureId);
            return TimeSpan.FromSeconds(featureValue);
        }

        #endregion
    }
}
