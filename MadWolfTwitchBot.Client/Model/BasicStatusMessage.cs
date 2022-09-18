using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MadWolfTwitchBot.Client.Model
{
    public class BasicStatusMessage : INotifyPropertyChanged
    {
        private string m_message, m_hex;
        public string Message 
        { 
            get { return m_message; }
            set
            {
                m_message = value;
                NotifyPropertyChanged();
            } 
        }
        public string ColourHex 
        {
            get { return m_hex; }
            set
            {
                m_hex = value;
                NotifyPropertyChanged();
            }
        }

        public void NotifyPropertyChanged([CallerMemberName] string propertyName = null) 
        { 
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
