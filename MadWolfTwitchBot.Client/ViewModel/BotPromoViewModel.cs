using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MadWolfTwitchBot.Client.Model;
using MadWolfTwitchBot.Client.View.Modals;
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
    public class PromoDetails : ObservableObject 
    {
        private bool m_selected;

        public bool IsSelected
        {
            get { return m_selected; }
            set { SetProperty(ref m_selected, value); }
        }

        public BasicPromo Data { get; set; }
    }

    public class BotPromoViewModel : ObservableObject
    {
        private readonly long m_botId;

        public ObservableCollection<PromoDetails> BotPromoMessages { get; set; }

        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }

        public BotPromoViewModel() : this(0, new List<BasicPromo>()) { }

        public BotPromoViewModel(long botId, IEnumerable<BasicPromo> messages)
        {
            m_botId = botId;

            BotPromoMessages = new ObservableCollection<PromoDetails>();

            foreach (var message in messages)
            {
                var details = new PromoDetails
                {
                    IsSelected = false,
                    Data = message
                };

                BotPromoMessages.Add(details);
            }

            AddCommand = new AsyncRelayCommand(AddNewPromoMessage);
            EditCommand = new AsyncRelayCommand<BasicPromo>(EditSelectedPromoMessage);
            DeleteCommand = new AsyncRelayCommand(DeletePromoMessages, CanDeletePromoMessages);
        }

        private async Task RefreshPromoList(long id) 
        {
            BotPromoMessages.Clear();

            var promos = await PromoService.GetMessagesForBot(id);
            foreach (var message in promos)
            {
                var details = new PromoDetails
                {
                    IsSelected = false,
                    Data = new BasicPromo(message)
                };

                BotPromoMessages.Add(details);
            }
        }

        private async Task AddNewPromoMessage()
        {
            var windowModal = new PromoDetailsWindow
            {
                DataContext = new PromoDetailsViewModel()
            };

            if (windowModal.ShowDialog() == true)
            {
                var data = windowModal.DataContext as PromoDetailsViewModel;
                var result = await PromoService.CreateOrUpdateBotPromo(
                    0,
                    data.SelectedBot.Id,
                    data.ResponseMessage);

                if (result == null)
                {
                    return;
                }

                await RefreshPromoList(m_botId);
            }
        }

        private async Task EditSelectedPromoMessage(BasicPromo promo)
        {
            var windowModal = new PromoDetailsWindow
            {
                DataContext = new PromoDetailsViewModel(
                    promo.ResponseMessage,
                    promo.BotId)
            };

            if (windowModal.ShowDialog() == true)
            {
                var data = windowModal.DataContext as PromoDetailsViewModel;
                var result = await PromoService.CreateOrUpdateBotPromo(
                    promo.Id,
                    data.SelectedBot.Id,
                    data.ResponseMessage);

                if (result == null)
                {
                    return;
                }

                await RefreshPromoList(promo.BotId);
            }
        }

        private bool CanDeletePromoMessages()
        {
            return BotPromoMessages.Any(m => m.IsSelected);
        }
        private async Task DeletePromoMessages()
        {
            var messagesToDelete = BotPromoMessages.Where(m => m.IsSelected);
            var messageList = string.Join("\n", messagesToDelete.Select(c => c.Data.ResponseMessage));

            var dialog = MessageBox.Show($"Promotion Messages selected for deletion:\n\n{messageList}\n\nIs this correct?", $"Confirm Deletion", MessageBoxButton.YesNo);
            if (dialog == MessageBoxResult.Yes)
            {
                var successfulDelete = 0;
                var failedDelete = 0;

                foreach (var message in messagesToDelete)
                {
                    var result = await PromoService.DeleteBotPromo(message.Data.Id);

                    if (result)
                        successfulDelete++;
                    else
                        failedDelete++;
                }

                await RefreshPromoList(m_botId);
            }
        }
    }
}
