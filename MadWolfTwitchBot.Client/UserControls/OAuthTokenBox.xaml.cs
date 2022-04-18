using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MadWolfTwitchBot.Client.UserControls
{
    /// <summary>
    /// Interaction logic for BindablePasswordBox.xaml
    /// </summary>
    public partial class OAuthTokenBox : UserControl
    {
        private bool m_isChanging;

        public string OAuthToken
        {
            get { return (string)GetValue(OAuthTokenProperty); }
            set { SetValue(OAuthTokenProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Property.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty OAuthTokenProperty =
            DependencyProperty.Register("OAuthToken", typeof(string), typeof(OAuthTokenBox), 
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, 
                    OAuthTokenChanged, null, false, UpdateSourceTrigger.PropertyChanged));

        private static void OAuthTokenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is OAuthTokenBox tokenBox)
                tokenBox.UpdateOAuthToken();
        }

        public OAuthTokenBox()
        {
            InitializeComponent();
        }

        private void SyncTokenDetails(object sender, RoutedEventArgs e)
        {
            m_isChanging = true;
            OAuthToken = passwordBox.Password;
            m_isChanging = false;
        }

        private void UpdateOAuthToken()
        {
            if (!m_isChanging)
                passwordBox.Password = OAuthToken;
        }
    }
}
