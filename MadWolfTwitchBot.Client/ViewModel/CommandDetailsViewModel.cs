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
using System.Windows.Input;

namespace MadWolfTwitchBot.Client.ViewModel
{
    public class CommandDetailsViewModel : ObservableObject
    {
        private bool m_new;

        private string m_name;
        private string m_message;

        private bool m_local;
        private BasicBot m_bot;

        public bool IsNew
        {
            get { return m_new; }
            set { SetProperty(ref m_new, value); }
        }

        public string Name
        {
            get { return m_name; }
            set 
            { 
                var newValue = value;

                if (newValue.StartsWith("!"))
                {
                    newValue = newValue.Remove(0, 1);
                    MessageBox.Show("Exclamaition Mark at the start of command not required.\nExclamation mark has been removed");
                }

                SetProperty(ref m_name, newValue);
            }
        }

        public string ResponseMessage
        {
            get { return m_message; }
            set { SetProperty(ref m_message, value); }
        }

        public bool IsLocalCommand
        {
            get { return m_local; }
            set { SetProperty(ref m_local, value); }
        }

        public BasicBot SelectedBot
        {
            get { return m_bot; }
            set { SetProperty(ref m_bot, value); }
        }

        public ObservableCollection<BasicBot> AvailableBots { get; set; }

        public ICommand ConfirmCommand { get; }

        public CommandDetailsViewModel() : this(string.Empty, string.Empty, 0) { }
        public CommandDetailsViewModel(string name, string message, long selected) 
        { 
            IsNew = string.IsNullOrWhiteSpace(name);

            Name = name;
            ResponseMessage = message;

            ConfirmCommand = ConfirmCommand = new RelayCommand<Window>(ConfirmDetails, CanConfirmDetails);

            AvailableBots = new ObservableCollection<BasicBot>();
            GetAvailableBots(selected);
        }

        private async Task GetAvailableBots(long id)
        {
            AvailableBots.Clear();

            var data = await BotService.GetAllConfiguredBots();
            foreach (var bot in data)
                AvailableBots.Add(new BasicBot(bot));

            SelectedBot = AvailableBots.FirstOrDefault(b => b.Id == id);
            IsLocalCommand = SelectedBot != null;
        }

        private bool CanConfirmDetails(Window sender)
        {
            return !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(ResponseMessage);
        }
        private void ConfirmDetails(Window sender)
        {
            if (Name.StartsWith("!"))
            {
                Name = Name.Remove(0, 1);
                MessageBox.Show("Exclamaition Mark at the start of command not required.\n");
            }

            sender.DialogResult = true;
            sender.Close();
        }
    }
}
