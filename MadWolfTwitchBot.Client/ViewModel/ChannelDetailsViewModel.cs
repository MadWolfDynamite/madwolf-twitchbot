using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MadWolfTwitchBot.Client.Constants;
using MadWolfTwitchBot.Client.Model;
using MadWolfTwitchBot.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace MadWolfTwitchBot.Client.ViewModel
{
    public class ChannelDetailsViewModel : ObservableObject
    {
        private readonly string m_token;

        private string m_username;
        private string m_displayName;

        public string AuthToken
        {
            get => m_token;
        }

        public string Username
        {
            get => m_username;
            set => SetProperty(ref m_username, value);
        }

        public string DisplayName
        {
            get => m_displayName;
            set => SetProperty(ref m_displayName, value);
        }

        public ICommand GenerateDisplayNameCommand { get; }

        public ICommand ConfirmCommand { get; }

        public ChannelDetailsViewModel() : this(new BasicChannel(), string.Empty) { }

        public ChannelDetailsViewModel(BasicChannel channel, string token)
        {
            m_token = token;

            Username = channel.Username;
            DisplayName = channel.DisplayName;

            GenerateDisplayNameCommand = new AsyncRelayCommand(GenerateDisplayName, CanGenerateDisplayName);

            ConfirmCommand = new RelayCommand<Window>(ConfirmDetails, CanConfirmDetails);
        }

        private bool CanGenerateDisplayName()
        {
            return !string.IsNullOrEmpty(Username);
        }
        private async Task GenerateDisplayName()
        {
            var data = await WolfAPIService.GetChannelDetails(ApiSettings.ClientId, AuthToken, Username);
            if (data == null)
                return;

            DisplayName = data.DisplayName;
        }
        
        private bool CanConfirmDetails(Window sender)
        {
            return !string.IsNullOrWhiteSpace(DisplayName);
        }
        private void ConfirmDetails(Window sender)
        {
            sender.DialogResult = true;
            sender.Close();
        }
    }

    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var output = (string)value;
            return string.IsNullOrEmpty(output) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
