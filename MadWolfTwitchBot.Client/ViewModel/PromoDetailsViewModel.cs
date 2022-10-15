using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MadWolfTwitchBot.Client.Model;
using MadWolfTwitchBot.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace MadWolfTwitchBot.Client.ViewModel
{
    public class PromoDetailsViewModel : ObservableObject
    {
        private bool m_new;

        private string m_message;

        private BasicBot m_bot;

        public bool IsNew
        {
            get { return m_new; }
            set { SetProperty(ref m_new, value); }
        }

        public string ResponseMessage
        {
            get { return m_message; }
            set { SetProperty(ref m_message, value); }
        }

        public BasicBot SelectedBot
        {
            get { return m_bot; }
            set { SetProperty(ref m_bot, value); }
        }

        public ObservableCollection<BasicBot> AvailableBots { get; set; }

        public ICommand ConfirmCommand { get; }

        public PromoDetailsViewModel() : this(string.Empty, 0) { }

        public PromoDetailsViewModel(string message, long selectedBotId) 
        {
            IsNew = string.IsNullOrWhiteSpace(message);

            ResponseMessage = message;

            ConfirmCommand = new RelayCommand<Window>(ConfirmDetails, CanConfirmDetails);

            AvailableBots = new ObservableCollection<BasicBot>();
            GetAvailableBots(selectedBotId);
        }

        private async Task GetAvailableBots(long id)
        {
            AvailableBots.Clear();

            var data = await BotService.GetAllConfiguredBots();
            foreach (var bot in data)
                AvailableBots.Add(new BasicBot(bot));

            SelectedBot = AvailableBots.FirstOrDefault(b => b.Id == id);
        }

        private bool CanConfirmDetails(Window sender)
        {
            return !string.IsNullOrWhiteSpace(ResponseMessage) && SelectedBot != null;
        }
        private void ConfirmDetails(Window sender)
        {
            sender.DialogResult = true;
            sender.Close();
        }
    }
}
