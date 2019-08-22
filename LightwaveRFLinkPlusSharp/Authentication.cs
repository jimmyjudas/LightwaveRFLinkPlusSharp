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
    internal class Authentication
    {
        private string _bearer;
        private string _seedRefreshToken;
        private string _currentRefreshToken;
        private string _currentAccessToken;
        private string _authResponseFileName = "auth_response.txt";

        private bool _justRefreshedAccessToken = false;
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="bearer"></param>
        /// <param name="seedRefreshToken"></param>
        public Authentication(string bearer, string seedRefreshToken = null)
        {
            _bearer = bearer;
            _seedRefreshToken = seedRefreshToken;
        }

        public async Task<string> GetAccessTokenAsync(bool forceRefreshAccessToken = false)
        {
            if (_justRefreshedAccessToken && forceRefreshAccessToken)
            {
                // The last time we asked for an access token it didn't work, but we had just refreshed it
                // so there's nothing we can do now
                throw new Exception();//?????????????????????????????????????
            }

            if (forceRefreshAccessToken) // The last time we asked for an access token it didn't work, so refresh it
            {
                await RefreshAccessTokenAsync();
                _justRefreshedAccessToken = true;
            }
            else if (_currentAccessToken != null) // We have an access token in memory, so use that one
            {
                // Do nothing - just return the token later
                _justRefreshedAccessToken = false;
            }
            else if (File.Exists(_authResponseFileName)) // We don't have the access token in memory yet, so see if we have one stored from a previous request
            {
                string previousAuthResponse = File.ReadAllText(_authResponseFileName);
                JObject json = JsonConvert.DeserializeObject<JObject>(previousAuthResponse);
                _currentAccessToken = json["access_token"].ToString();
                _justRefreshedAccessToken = false;
            }
            else // Otherwise request a new one from Lightwave
            {
                await RefreshAccessTokenAsync();
                _justRefreshedAccessToken = true;
            }

            return _currentAccessToken;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="useSeedRefreshToken"></param>
        private async Task RefreshAccessTokenAsync(bool useSeedRefreshToken = false)
        {
            if (useSeedRefreshToken)
            {
                _currentRefreshToken = _seedRefreshToken;
            }
            else if (_currentRefreshToken != null)
            {
                //Keep using the current refresh token
            }
            else if (File.Exists(_authResponseFileName))
            {
                string previousAuthResponse = File.ReadAllText(_authResponseFileName);
                JObject json = JsonConvert.DeserializeObject<JObject>(previousAuthResponse);
                _currentRefreshToken = json["refresh_token"].ToString();
            }
            else
            {
                _currentRefreshToken = _seedRefreshToken;
            }

            string body = JsonConvert.SerializeObject(new
            {
                grant_type = "refresh_token",
                refresh_token = _currentRefreshToken
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
                        if (useSeedRefreshToken)
                        {
                            //We've already tried to use the seed refresh token, so we're stuck
                            throw new Exception(); //????????????????????????????????????????????
                        }

                        //try using the seed refresh code
                        await RefreshAccessTokenAsync(true);
                        return;
                    }
                    else
                    {
                        File.WriteAllText(_authResponseFileName, responseData);

                        _currentAccessToken = json["access_token"].ToString();
                        _currentRefreshToken = json["refresh_token"].ToString();
                    }
                }
            }
        }
    }
}
