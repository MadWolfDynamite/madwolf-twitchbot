using CommunityToolkit.Mvvm.ComponentModel;
using MadWolfTwitchBot.Client.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public ObservableCollection<PromoDetails> BotPromoMessages { get; set; }

        public BotPromoViewModel() { }

        public BotPromoViewModel(IEnumerable<BasicPromo> messages)
        {
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
        }
    }
}
