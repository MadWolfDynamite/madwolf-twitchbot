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
using System.Windows.Shapes;

namespace MadWolfTwitchBot.Client.View.Modals
{
    /// <summary>
    /// Interaction logic for ChannelDetailsWindow.xaml
    /// </summary>
    public partial class ChannelDetailsWindow : Window
    {
        public ChannelDetailsWindow()
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;
        }
    }
}
