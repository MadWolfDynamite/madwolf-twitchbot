using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MadWolfTwitchBot.Client.View.Modals
{
    /// <summary>
    /// Interaction logic for OAuthTokenRetievalWindow.xaml
    /// </summary>
    public partial class OAuthTokenRetievalWindow : Window
    {
        public OAuthTokenRetievalWindow()
        {
            InitializeComponent();

            InitializeAsync();
        }

        async void InitializeAsync()
        {
            await webView.EnsureCoreWebView2Async();
            webView.WebMessageReceived += UpdateTokenDetails;
        }

        void UpdateTokenDetails(object sender, CoreWebView2WebMessageReceivedEventArgs args)
        {
            var uri = args.Source;
            if (!uri.StartsWith("https://localhost:44301"))
                return;

            var data = args.TryGetWebMessageAsString();
            var tokenData = data.Split(';');

            if (tokenData.Length == 3)
            {
                var context = DataContext as ViewModel.OAuthTokenRetrievalViewModel;

                context.OAuthToken = tokenData[0];
                context.RefreshToken = tokenData[1];

                context.TokenTimestamp = DateTime.Parse(tokenData[2]);

                DialogResult = true;
                Close();
            }
        }
    }
}
