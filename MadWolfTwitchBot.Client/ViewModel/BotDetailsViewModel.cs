using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MadWolfTwitchBot.Client.Model;
using MadWolfTwitchBot.Client.View.Modals;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
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
        public ICommand ConfirmCommand { get; private set; }

        public BotDetailsViewModel() : this(new BasicBot()) { }
        public BotDetailsViewModel(BasicBot bot)
        {
            Username = bot.Username;
            DisplayName = bot.DisplayName;

            OAuthToken = bot.OAuthToken;
            RefreshToken = bot.RefreshToken;
            TokenTimestamp = bot.TokenTimestamp;

            IsEditMode = !string.IsNullOrEmpty(bot.Username);

            TokenCommand = new RelayCommand(GenerateOAuthToken, CanGenerateToken);
            ConfirmCommand = new RelayCommand<Window>(ConfirmDetails);
        }

        public bool CanGenerateToken()
        {
            return string.IsNullOrWhiteSpace(m_refresh) && !string.IsNullOrWhiteSpace(Username);
        }
        public void GenerateOAuthToken()
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
