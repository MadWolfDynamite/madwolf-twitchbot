using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MadWolfTwitchBot.Client.Constants;
using MadWolfTwitchBot.Client.Model;
using MadWolfTwitchBot.Client.View.Modals;
using MadWolfTwitchBot.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace MadWolfTwitchBot.Client.ViewModel
{
    public class BotDetailsViewModel : ObservableObject
    {
        private string m_username;
        public string Username
        {
            get => m_username;
            set => SetProperty(ref m_username, value);
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

        public ICommand TokenCommand { get; private set; }
        public ICommand VerifyCommand { get; private set; }

        public ICommand ConfirmCommand { get; private set; }

        public BotDetailsViewModel() : this(new BasicBot()) { }
        public BotDetailsViewModel(BasicBot bot)
        {
            WolfAPIService.SetApiEndpoint(ApiSettings.Endpoint);

            Username = bot.Username;
            DisplayName = bot.DisplayName;

            OAuthToken = bot.OAuthToken;
            RefreshToken = bot.RefreshToken;
            TokenTimestamp = bot.TokenTimestamp;

            IsEditMode = !string.IsNullOrEmpty(bot.Username);

            TokenCommand = new RelayCommand(GenerateOAuthToken, CanGenerateToken);
            VerifyCommand = new AsyncRelayCommand(VerifyOAuthToken, CanVerifyToken);

            ConfirmCommand = new RelayCommand<Window>(ConfirmDetails);
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
        } 

        private void ConfirmDetails(Window window)
        {
            window.DialogResult = true;
            window.Close();
        }
    }

    class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var isInEditMode = (bool)value;
            return isInEditMode ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
