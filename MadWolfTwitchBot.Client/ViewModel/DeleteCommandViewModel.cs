using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using MadWolfTwitchBot.Client.Model;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace MadWolfTwitchBot.Client.ViewModel
{
    public class CommandDetails : ObservableObject
    {
        private bool m_selected;

        public bool IsSelected 
        { 
            get { return m_selected; }
            set { SetProperty(ref m_selected, value); }
        }

        public BasicCommand Data { get; set; }
        public bool IsLocal { get; set; }
    }

    public class DeleteCommandViewModel : ObservableObject
    {
        public ObservableCollection<CommandDetails> ChatCommands { get; set; }

        public ICommand ConfirmCommand { get; }

        public DeleteCommandViewModel() : this(new List<BasicCommand>()) { }

        public DeleteCommandViewModel(IEnumerable<BasicCommand> commands) 
        {
            ChatCommands = new ObservableCollection<CommandDetails>();

            foreach (var command in commands.OrderBy(c => c.BotId))
            {
                var details = new CommandDetails
                {
                    IsSelected = false,

                    Data = command,
                    IsLocal = command.BotId != null
                };

                ChatCommands.Add(details);
            }

            ConfirmCommand = new RelayCommand<Window>(ConfirmDetails, CanConfirmDetails);
        }

        private bool CanConfirmDetails(Window sender)
        {
            return ChatCommands.Any(c => c.IsSelected);
        }
        private void ConfirmDetails(Window sender)
        {
            var commandsToDelete = ChatCommands.Where(c => c.IsSelected);

            var commandList = string.Join("\n", commandsToDelete.Select(c => c.Data.Name));
            var dialog = MessageBox.Show($"Commands selected for deletion:\n\n{commandList}\n\nIs this correct?", $"Confirm Deletion", MessageBoxButton.YesNo);

            if (dialog == MessageBoxResult.Yes)
            {
                sender.DialogResult = true;
                sender.Close();
            }
        }
    }
}
