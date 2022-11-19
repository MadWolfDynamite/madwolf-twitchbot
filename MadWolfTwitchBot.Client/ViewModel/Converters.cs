using MadWolfTwitchBot.Client.Model;

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MadWolfTwitchBot.Client.ViewModel
{
    class TokenStatusToUriConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return new Uri("/icons/StatusSecurityWarningOutline_16x.png", UriKind.Relative);

            var status = (OAuthTokenStatus)value;
            return status switch
            {
                OAuthTokenStatus.Valid => new Uri("/icons/StatusOKOutline_16x.png", UriKind.Relative),
                OAuthTokenStatus.NotValid => new Uri("/icons/StatusInvalidOutline_16x.png", UriKind.Relative),
                OAuthTokenStatus.Error => new Uri("/icons/StatusCriticalErrorOutline_16x.png", UriKind.Relative),
                _ => new Uri("/icons/StatusSecurityWarningOutline_16x.png", UriKind.Relative)
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var icon = value as Uri;
            return icon.OriginalString switch
            {
                "/icons/StatusOKOutline_16x.png" => OAuthTokenStatus.Valid,
                "/icons/StatusInvalidOutline_16x.png" => OAuthTokenStatus.NotValid,
                "/icons/StatusCriticalErrorOutline_16x.png" => OAuthTokenStatus.Error,
                _ => OAuthTokenStatus.None
            };
        }
    }

    class ConnectionStatusToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var disconnected = value as bool?;
            return disconnected ?? true
                ? "Not Connected"
                : "Connected";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    class ApplicationStatusToVisiblityCoverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var statusMessage = value as string;
            return string.IsNullOrWhiteSpace(statusMessage)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
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
