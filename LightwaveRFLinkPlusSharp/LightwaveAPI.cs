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
        Uri _baseAddress = new Uri("https://publicapi.lightwaverf.com/v1/");

        private string _bearer;
        private string _refreshToken;
        private string _accessToken;
        private string _authResponseFileName = "auth_response.txt";

        public LightwaveAPI(string bearer, string initialRefreshToken = null)
        {
            _bearer = bearer;
            _refreshToken = initialRefreshToken;
        }

        #region Generalised API calls

        private async Task<JObject> Get(string uriSegment)
        {
            return await GetOrPost(uriSegment, CallMode.GET, null);
        }

        private async Task<JObject> Post(string uriSegment, string body)
        {
            return await GetOrPost(uriSegment, CallMode.POST, body);
        }

        private async Task<JObject> GetOrPost(string uriSegment, CallMode callMode, string body, bool gotNewAccessToken = false)
        {
            if (_accessToken == null)
            {
                if (File.Exists(_authResponseFileName))
                {
                    string previousAuthResponse = File.ReadAllText(_authResponseFileName);
                    JObject json = JsonConvert.DeserializeObject<JObject>(previousAuthResponse);
                    _accessToken = json["access_token"].ToString();
                }
                else
                {
                    await RequestAccessToken();
                    gotNewAccessToken = true;
                }
            }

            using (var httpClient = new HttpClient { BaseAddress = _baseAddress })
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", $"bearer {_accessToken}");

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
                    //invalid access token
                    _accessToken = null;

                    if (!gotNewAccessToken)
                    {
                        await RequestAccessToken();
                        return await GetOrPost(uriSegment, callMode, body, true);
                    }
                    else
                    {
                        throw new Exception();
                    }
                }

                return json;
            }
        }

        private enum CallMode
        {
            GET,
            POST
        }

        private async Task RequestAccessToken(bool useInitialRefreshToken = false)
        {
            string refreshToken;

            //If we have a saved access token that may well be more up-to-date than the one passed in, so try that one first
            if (!useInitialRefreshToken && File.Exists(_authResponseFileName))
            {
                string previousAuthResponse = File.ReadAllText(_authResponseFileName);
                JObject json = JsonConvert.DeserializeObject<JObject>(previousAuthResponse);
                refreshToken = json["refresh_token"].ToString();
            }
            else
            {
                refreshToken = _refreshToken;
                _refreshToken = null;
            }

            string body = JsonConvert.SerializeObject(new
            {
                grant_type = "refresh_token",
                refresh_token = refreshToken
            });

            var httpRequestContent = new StringContent(body);
            httpRequestContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            using (var httpClient = new HttpClient { BaseAddress = new Uri("https://auth.lightwaverf.com/") })
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", $"basic {_bearer}");
                using (var response = await httpClient.PostAsync("token", httpRequestContent))
                {
                    string responseData = await response.Content.ReadAsStringAsync();
                    JObject json = JsonConvert.DeserializeObject<JObject>(responseData);

                    if (json.ContainsKey("error") && json["error"].ToString() == "invalid_token")
                    {
                        //try using the original refresh code
                        if (_refreshToken != null)
                        {
                            await RequestAccessToken(true);
                            return;
                        }

                        throw new Exception();
                    }
                    else
                    {
                        File.WriteAllText(_authResponseFileName, responseData);

                        _accessToken = json["access_token"].ToString();
                        _refreshToken = json["refresh_token"].ToString();
                    }
                }
            }
        }

        #endregion

        #region Official API Endpoints

        /// <summary>
        /// Gets the IDs of the structures in your LinkPlus ecosystem. For more details on structures
        /// see https://linkpluspublicapi.docs.apiary.io/#introduction/structure. 
        /// </summary>
        public async Task<string[]> GetStructures()
        {
            JObject json = await Get("structures");

            return json["structures"].ToObject<string[]>();
        }

        /// <summary>
        /// Gets the details of the specified structure from your LinkPlus ecosystem. For more details on structures
        /// see https://linkpluspublicapi.docs.apiary.io/#introduction/structure.
        /// </summary>
        /// <returns>A JSON object describing the structure</returns>
        public async Task<JObject> GetStructure(string structureId)
        {
            if (structureId == null)
            {
                return null;
            }

            return await Get($"structure/{structureId}");
        }

        /// <summary>
        /// Gets the value of a specified feature for a device, e.g. whether the device is on or off
        /// </summary>
        /// <param name="featureId">The ID of the feature on the device. This is not the same as the feature
        /// _type_, e.g. "switch". Instead, get the ID from the Device using either one of the helper properties 
        /// (e.g. SwitchFeatureId) or the generic GetFeatureId</param>
        public async Task<int> GetFeatureValue(string featureId)
        {
            if (featureId == null)
            {
                return -1;
            }

            JObject json = await Get($"feature/{featureId}");
            return json["value"].ToObject<int>();
        }

        /// <summary>
        /// Sets the value of a specified feature for a device, e.g. whether the device is on or off
        /// </summary>
        /// <param name="featureId">The ID of the feature on the device. This is not the same as the feature
        /// _type_, e.g. "switch". Instead, get the ID from the Device using either one of the helper properties 
        /// (e.g. SwitchFeatureId) or the generic GetFeatureId</param>
        /// <param name="newValue">The numerical value to which you want to set the feature</param>
        public async Task SetFeatureValue(string featureId, int newValue)
        {
            string body = JsonConvert.SerializeObject(new
            {
                value = newValue
            });

            await Post($"feature/{featureId}", body);
            return;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets the details of the first "structure" in your LinkPlus ecosystem. This is a helper for if your ecosystem 
        /// only has a single structure; if you have more than one, use GetStructures instead. For more details on structures, 
        /// see https://linkpluspublicapi.docs.apiary.io/#introduction/structure.
        /// </summary>
        /// <returns>A JSON object describing the structure</returns>
        public async Task<JObject> GetFirstStructure()
        {
            string[] structures = await GetStructures();
            string structureId = structures[0];
            return await GetStructure(structureId);
        }

        /// <summary>
        /// Gets the details of the devices in the first structure in your LinkPlus ecosystem. This is a helper for if your ecosystem 
        /// only has a single structure; if you have more than one, use GetStructures and GetDevices instead. For more details on 
        /// structures, see https://linkpluspublicapi.docs.apiary.io/#introduction/structure.
        /// </summary>
        public async Task<Device[]> GetDevicesInFirstStructure()
        {
            return GetDevices(await GetFirstStructure());
        }

        /// <summary>
        /// Gets the details for the devices in a specified structure. For more details on structures,
        /// see https://linkpluspublicapi.docs.apiary.io/#introduction/structure.
        /// </summary>
        public async Task<Device[]> GetDevices(string structureId)
        {
            JObject json = await GetStructure(structureId);

            return GetDevices(json);
        }

        private Device[] GetDevices(JObject structureJson)
        {
            return structureJson["devices"].Select(x => new Device(x)).ToArray();
        }

        #endregion
    }
}
