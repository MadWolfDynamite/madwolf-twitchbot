using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MadWolfTwitchBot.Client.Constants;
using MadWolfTwitchBot.Client.Model;
using MadWolfTwitchBot.Client.View.Modals;
using MadWolfTwitchBot.Services;

using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace MadWolfTwitchBot.Client.ViewModel
{
    public class BotDetailsViewModel : ObservableObject
    {
        private readonly HttpClient m_client;

        private string m_username;
        public string Username
        {
            get => m_username;
            set
            {
                SetProperty(ref m_username, value);
                IsVerified = false;
            }
        }

        private string m_displayName;
        public string DisplayName
        {
            get => m_displayName;
            set => SetProperty(ref m_displayName, value);
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

        private bool m_verified;
        public bool IsVerified
        {
            get => m_verified;
            set => SetProperty(ref m_verified, value);
        }

        private bool m_edit;
        public bool IsEditMode
        {
            get => m_edit;
            set => SetProperty(ref m_edit, value);
        }
        public bool IsNew
        {
            get => !m_edit;
        }

        public ICommand VerifyUserCommand { get; }

        public ICommand TokenCommand { get; }
        public ICommand VerifyTokenCommand { get; }

        public ICommand ConfirmCommand { get; }

        public BotDetailsViewModel() : this(new BasicBot()) { }
        public BotDetailsViewModel(BasicBot bot)
        {
            WolfAPIService.SetApiEndpoint(ApiSettings.Endpoint);

            var socketHttpHandler = new SocketsHttpHandler()
            {
                PooledConnectionIdleTimeout = TimeSpan.FromSeconds(90),
                PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            };
            m_client = new HttpClient(socketHttpHandler);

            Username = bot.Username;
            DisplayName = bot.DisplayName;

            OAuthToken = bot.OAuthToken;
            RefreshToken = bot.RefreshToken;
            TokenTimestamp = bot.TokenTimestamp;

            IsEditMode = !string.IsNullOrEmpty(bot.Username);
            IsVerified = IsEditMode;

            VerifyUserCommand = new AsyncRelayCommand(ValidateUsername, CanVerifyUser);

            TokenCommand = new RelayCommand(GenerateOAuthToken, CanGenerateToken);
            VerifyTokenCommand = new AsyncRelayCommand(VerifyOAuthToken, CanVerifyToken);

            ConfirmCommand = new RelayCommand<Window>(ConfirmDetails, CanConfirmDetails);
        }

        private bool CanVerifyUser()
        {
            return !IsVerified && !string.IsNullOrWhiteSpace(Username);
        }
        private async Task ValidateUsername()
        {
            var url = $"https://www.twitch.tv/{Username.ToLower()}";
            var request = new HttpRequestMessage 
            { 
                RequestUri = new Uri(url),
                Method = HttpMethod.Head
            }; 

            var result = await m_client.SendAsync(request);
            IsVerified = result.IsSuccessStatusCode; //TODO: A better means of validating (this is always true regardless)
        }

        private bool CanGenerateToken()
        {
            return string.IsNullOrWhiteSpace(RefreshToken) && !string.IsNullOrWhiteSpace(Username);
        }
        private void GenerateOAuthToken()
        {
            var tokenRequestWindow = new OAuthTokenRetievalWindow
            {
                DataContext = new OAuthTokenRetrievalViewModel(Username)
            };

            if (tokenRequestWindow.ShowDialog() == true) 
            { 
                var data = tokenRequestWindow.DataContext as OAuthTokenRetrievalViewModel;

                OAuthToken = data.OAuthToken;
                RefreshToken = data.RefreshToken;
                TokenTimestamp = data.TokenTimestamp;
            }
        }

        private bool CanVerifyToken()
        {
            var tokenAge = DateTime.UtcNow - (TokenTimestamp ?? DateTime.MinValue);
            return tokenAge.TotalMinutes >= TokenSettings.RefreshMinutes && !string.IsNullOrWhiteSpace(RefreshToken);
        }
        private async Task VerifyOAuthToken()
        {
            var validationResult = await WolfAPIService.ValidateOAuthToken(OAuthToken);
            if (!validationResult)
            {
                var dialog = MessageBox.Show("OAuth Token has expired. Would you like to refresh?", "Token Expired", MessageBoxButton.YesNo);
                if (dialog != MessageBoxResult.Yes)
                    return;

                var newToken = await WolfAPIService.RefreshTwitchTokenAsync(ApiSettings.ClientId, ApiSettings.ClientSecret, RefreshToken);
                if (newToken != null)
                {
                    OAuthToken = newToken.Access_Token;
                    RefreshToken = newToken.Refresh_Token;
                    TokenTimestamp = DateTime.UtcNow;
                }
            }
            else { TokenTimestamp = DateTime.UtcNow; }
        } 

        private bool CanConfirmDetails(Window sender)
        {
            return IsVerified;
        }
        private void ConfirmDetails(Window sender)
        {
            sender.DialogResult = true;
            sender.Close();
        }
    }
}
