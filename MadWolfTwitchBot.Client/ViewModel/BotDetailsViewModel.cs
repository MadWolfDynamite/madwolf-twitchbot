using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MadWolfTwitchBot.Client.Model;
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
        private readonly string m_refresh;

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

        public ICommand ConfirmCommand { get; private set; }

        public BotDetailsViewModel() : this(new BasicBot()) { }
        public BotDetailsViewModel(BasicBot bot)
        {
            Username = bot.Username;
            DisplayName = bot.DisplayName;

            OAuthToken = bot.OAuthToken;
            m_refresh = bot.RefreshToken;
            TokenTimestamp = bot.TokenTimestamp;

            IsEditMode = !string.IsNullOrEmpty(bot.Username);

            ConfirmCommand = new RelayCommand<Window>(ConfirmDetails);
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
            return isInEditMode ? Visibility.Hidden : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
