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
        private string _authResponseFileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), nameof(LightwaveRFLinkPlusSharp), "auth_response.txt");

        private Guid? _previousRequestIdentifier = null;
        private bool _previousRequestRequiredRefresh = false;
        private StringBuilder _tokenRequestLog = new StringBuilder();
        
        internal Authentication(string bearer, string seedRefreshToken = null)
        {
            _bearer = bearer;
            _seedRefreshToken = seedRefreshToken;
        }

        internal async Task<string> GetAccessTokenAsync(Guid uniqueRequestIdentifier)
        {
            bool forceRefresh = false;
            if (uniqueRequestIdentifier != _previousRequestIdentifier)
            {
                //This is the first attempt to get an access token for this request
                _previousRequestIdentifier = uniqueRequestIdentifier;
                _previousRequestRequiredRefresh = false;
                _tokenRequestLog.Clear();
            }
            else
            {
                //This the 2nd attempt to get an access token for this request, so the previously returned one obviously didn't
                //work. Try refreshing the token this time.
                forceRefresh = true;
            }

            _tokenRequestLog.AppendLine($"Access token requested for request {uniqueRequestIdentifier}");
            
            if (forceRefresh)
            {
                if (_previousRequestRequiredRefresh) // Alas, we already attempted to refresh the token last time so there's nothing we can do now
                {
                    throw new InvalidRefreshTokenException(_tokenRequestLog.ToString());
                }

                //Try refreshing the access token this time
                _tokenRequestLog.AppendLine("Force refresh triggered");
                await RefreshAccessTokenAsync();
                
            }
            else if (_currentAccessToken != null) // We have an access token in memory, so use that one
            {
                // Do nothing - just return the token at the end of the method
                _tokenRequestLog.AppendLine("Using access token in memory");
            }
            else if (File.Exists(_authResponseFileName)) // We don't have the access token in memory yet, so see if we have one stored from a previous request
            {
                _tokenRequestLog.AppendLine("Getting previous access token from file");
                string previousAuthResponse = File.ReadAllText(_authResponseFileName);
                JObject json = JsonConvert.DeserializeObject<JObject>(previousAuthResponse);
                _currentAccessToken = json["access_token"].ToString();
            }
            else // Otherwise request a new one from Lightwave
            {
                await RefreshAccessTokenAsync();
            }

            if (Debugger.IsAttached && _previousRequestRequiredRefresh)
            {
                Console.WriteLine();
                Console.WriteLine("************************");
                Console.WriteLine("Token Request Log:");
                Console.Write(_tokenRequestLog.ToString());
                Console.WriteLine("************************");
                Console.WriteLine();
            }

            return _currentAccessToken;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="useSeedRefreshToken"></param>
        private async Task RefreshAccessTokenAsync(bool useSeedRefreshToken = false)
        {
            _previousRequestRequiredRefresh = true;

            _tokenRequestLog.AppendLine("Refreshing access token");

            if (useSeedRefreshToken)
            {
                _tokenRequestLog.AppendLine("\tUsing Seed refresh token (forced)");
                _currentRefreshToken = _seedRefreshToken;
            }
            else if (_currentRefreshToken != null)
            {
                //Keep using the current refresh token
                _tokenRequestLog.AppendLine("\tUsing refresh token in memory");
            }
            else if (File.Exists(_authResponseFileName))
            {
                _tokenRequestLog.AppendLine("\tUsing previous refresh token from file");
                string previousAuthResponse = File.ReadAllText(_authResponseFileName);
                JObject json = JsonConvert.DeserializeObject<JObject>(previousAuthResponse);
                _currentRefreshToken = json["refresh_token"].ToString();
            }
            else
            {
                _tokenRequestLog.AppendLine("\tUsing Seed refresh token");
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
                        if (_currentRefreshToken == _seedRefreshToken)
                        {
                            //We've already tried to use the seed refresh token, so we're stuck
                            throw new InvalidRefreshTokenException(_tokenRequestLog.ToString());
                        }

                        _tokenRequestLog.AppendLine("Refresh failed. Try again using the seed refresh token.");

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
