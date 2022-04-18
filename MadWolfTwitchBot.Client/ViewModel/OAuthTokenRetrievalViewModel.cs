using CommunityToolkit.Mvvm.ComponentModel;
using MadWolfTwitchBot.Client.Constants;
using MadWolfTwitchBot.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MadWolfTwitchBot.Client.ViewModel
{
    public class OAuthTokenRetrievalViewModel : ObservableObject
    {
        private string m_username;
        public string Username
        {
            get => m_username;
            set => SetProperty(ref m_username, value);
        }

        private string m_uri;
        public string Uri
        {
            get => m_uri;
            set => SetProperty(ref m_uri, value);
        }

        private string m_token;
        public string OAuthToken
        {
            get => m_token;
            set => SetProperty(ref m_token, value);
        }

        private string m_refresh;
        public string RefreshToken
        {
            get => m_refresh;
            set => SetProperty(ref m_refresh, value);
        }

        private DateTime? m_timestamp;
        public DateTime? TokenTimestamp
        {
            get => m_timestamp;
            set => SetProperty(ref m_timestamp, value);
        }

        public OAuthTokenRetrievalViewModel() : this(string.Empty) { }
        public OAuthTokenRetrievalViewModel(string user)
        {
            WolfAPIService.SetApiEndpoint(ApiSettings.Endpoint);

            Username = user;

            Uri = "";
            GenerateAuthUri();
        }

        private async Task GenerateAuthUri()
        {
            var baseUri = await WolfAPIService.GenerateAuthenticationUrl(ApiSettings.ClientId, "https://localhost:44301/");
            var uri = $"{baseUri}&force_verify=true&scope=channel:moderate chat:edit chat:read whispers:read whispers:edit";

            Uri = uri;
        }
    }
}
