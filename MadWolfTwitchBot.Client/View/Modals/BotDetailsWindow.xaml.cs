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
    /// Interaction logic for BotDetailsWindow.xaml
    /// </summary>
    public partial class BotDetailsWindow : Window
    {
        public BotDetailsWindow()
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;
        }
    }
}
