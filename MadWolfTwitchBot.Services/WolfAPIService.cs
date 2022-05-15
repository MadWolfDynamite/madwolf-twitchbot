using MadWolfTwitchBot.Models;
using MadWolfTwitchBot.Models.Twitch;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Tools.StreamSerializer;

namespace MadWolfTwitchBot.Services
{
    public static class WolfAPIService
    {
        private static readonly HttpClient _client = new();

        public static void SetApiEndpoint(string endpoint)
        {
            if (_client.BaseAddress == null)
                _client.BaseAddress = new Uri(endpoint);
        }

        public static async Task<bool> ValidateOAuthToken(string token)
        {
            string apiPath = $"token/{token}";

            var response = await _client.GetAsync(apiPath);
            var stream = await response.Content.ReadAsStreamAsync();

            if (response.IsSuccessStatusCode)
            {
                var validationData = StreamSerializer.DeserialiseJsonFromStream<bool>(stream);
                return validationData;
            }

            return false;
        }

        public static async Task<Token> RefreshTwitchTokenAsync(string client, string secret, string token)
        {
            string apiPath = "token/refresh";

            var data = new
            {
                ClientId = client,
                ClientSecret = secret,

                Token = token
            };

            var json = JsonConvert.SerializeObject(data);

            var request = new StringContent(json);
            request.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var response = await _client.PostAsync(apiPath, request);
            var stream = await response.Content.ReadAsStreamAsync();

            if (response.IsSuccessStatusCode)
            {
                var tokenData = StreamSerializer.DeserialiseJsonFromStream<Token>(stream);
                return tokenData;
            }

            var content = await StreamSerializer.StreamToStringAsync(stream);
            throw new Exception(content);
        }

        public async static Task<string> GenerateAuthenticationUrl(string client, string url)
        {
            string apiPath = "token/url";

            apiPath += $"?client={client}";
            apiPath += $"&url={url}";

            var response = await _client.GetAsync(apiPath);

            var stream = await response.Content.ReadAsStreamAsync();
            var content = await StreamSerializer.StreamToStringAsync(stream);

            if (response.IsSuccessStatusCode)
                return content;

            throw new Exception(content);
        }

        public static async Task<Token> GetTwitchTokenAsync(string client, string secret, string code, string url)
        {
            string apiPath = "token/oauth";

            var data = new
            {
                ClientId = client,
                ClientSecret = secret,

                Token = code,
                RedirectUrl = url
            };

            var json = JsonConvert.SerializeObject(data);

            var request = new StringContent(json);
            request.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var response = await _client.PostAsync(apiPath, request);
            var stream = await response.Content.ReadAsStreamAsync();

            if (response.IsSuccessStatusCode)
            {
                var tokenData = StreamSerializer.DeserialiseJsonFromStream<Token>(stream);
                return tokenData;
            }

            var content = await StreamSerializer.StreamToStringAsync(stream);
            throw new Exception(content);
        }

        public static async Task<Models.Channel> GetChannelDetails(string client, string token, string channel)
        {
            string apiPath = GenerateApiPath($"account/{channel}", client, token);

            var response = await _client.GetAsync(apiPath);
            var stream = await response.Content.ReadAsStreamAsync();

            if (!response.IsSuccessStatusCode)
            {
                var content = await StreamSerializer.StreamToStringAsync(stream);
                throw new Exception(content);
            }

            var users = StreamSerializer.DeserialiseJsonFromStream<IEnumerable<Account>>(stream);
            var selected = users.FirstOrDefault(u => u.Login.Equals(channel));

            if (selected == null)
                return null;

            var result = new Models.Channel()
            {
                Id = 0,

                Username = selected.Login,
                DisplayName = selected.Display_Name
            };

            return result;
        }

        public static async Task<ShoutOut> GetShoutOutDetails(string client, string token, string user)
        {
            var resource = new ShoutOut();

            string apiPath = GenerateApiPath($"account/{user}", client, token);

            var response = await _client.GetAsync(apiPath);
            var stream = await response.Content.ReadAsStreamAsync();

            if (!response.IsSuccessStatusCode)
            {
                var content = await StreamSerializer.StreamToStringAsync(stream);
                throw new Exception(content);
            }

            var users = StreamSerializer.DeserialiseJsonFromStream<IEnumerable<Account>>(stream);
            var userData = users.FirstOrDefault(u => u.Login.Equals(user));

            resource.Name = userData.Display_Name;
            resource.Link = new Uri($"https://www.twitch.tv/{userData.Login}");

            apiPath = GenerateApiPath($"channel/{userData.Login}", client, token);

            response = await _client.GetAsync(apiPath);
            stream = await response.Content.ReadAsStreamAsync();

            if (!response.IsSuccessStatusCode)
            {
                var content = await StreamSerializer.StreamToStringAsync(stream);
                throw new Exception(content);
            }

            var channels = StreamSerializer.DeserialiseJsonFromStream<IEnumerable<Models.Twitch.Channel>>(stream);
            var channelData = channels.FirstOrDefault(c => c.Id.Equals(userData.Id));

            resource.IsLive = channelData.Is_Live;
            resource.StreamDateTime = string.IsNullOrEmpty(channelData.Started_At) ? null : DateTime.Parse(channelData.Started_At);

            apiPath = GenerateApiPath($"game/{channelData.Game_Id}", client, token);

            response = await _client.GetAsync(apiPath);
            stream = await response.Content.ReadAsStreamAsync();

            if (response.IsSuccessStatusCode)
            {
                var games = StreamSerializer.DeserialiseJsonFromStream<IEnumerable<Game>>(stream);
                var gameData = games.FirstOrDefault(g => g.Id.Equals(channelData.Game_Id));

                resource.Game = gameData.Name;
            }
            else
                resource.Game = string.Empty;

            return resource;
        }

        private static string GenerateApiPath(string path, string client, string token)
        {
            var apiPath = path;
            apiPath += $"?client={client}";
            apiPath += $"&token={token}";

            return apiPath;
        }
    }
}
