using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MadWolfTwitchBot.BotCommands;
using MadWolfTwitchBot.Client.Model;
using MadWolfTwitchBot.Client.View.Modals;
using MadWolfTwitchBot.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace MadWolfTwitchBot.Client.ViewModel
{
    public class MainWindowViewModel : ObservableObject
    {
        private TwitchClient _client;
        private readonly BotConfiguration _config = new BotConfiguration();

        private readonly SynchronizationContext _uiContext;

        private string m_windowTitle;
        public string Title
        {
            get => m_windowTitle;
            set => SetProperty(ref m_windowTitle, value);
        }

        private bool m_disconnected = true;
        public bool IsDisconnected 
        {
            get => m_disconnected;
            set => SetProperty(ref m_disconnected, value);
        }

        private OAuthTokenStatus? m_tokenStatus;
        public OAuthTokenStatus? TokenStatus
        {
            get => m_tokenStatus;
            set => SetProperty(ref m_tokenStatus, value);
        }

        private readonly Dictionary<string, string> _commands;

        public ObservableCollection<BasicBot> ConfiguredBots { get; private set; } = new ObservableCollection<BasicBot>();

        private BasicBot m_selectedBot;
        public BasicBot SelectedBot
        {
            get => m_selectedBot;
            set
            {
                SetProperty(ref m_selectedBot, value);

                if (m_selectedBot != null)
                {
                    TwitchChannels.Clear();
                    GetChannelHistory(m_selectedBot.Id).GetAwaiter().GetResult();

                    TokenStatus = GetBotTokenStatus();
                }
                else { TokenStatus = null; }
            }
        }

        public ObservableCollection<BasicChannel> TwitchChannels { get; private set; } = new ObservableCollection<BasicChannel>();

        private BasicChannel m_selectedChannel;
        public BasicChannel SelectedChannel
        {
            get => m_selectedChannel;
            set
            {
                SetProperty(ref m_selectedChannel, value);
            }
        }

        public ObservableCollection<BotCommand> ChatCommands { get; set; } = new ObservableCollection<BotCommand>();

        public ObservableCollection<Model.ChatMessage> Messages { get; set; } = new ObservableCollection<Model.ChatMessage>();

        public ICommand EditBotCommand { get; private set; }

        public ICommand ConnectCommand { get; private set; }
        public ICommand DisconnectCommand { get; private set; }

        public ICommand AddCommand { get; private set; }
        public ICommand RemoveCommand { get; private set; }

        public MainWindowViewModel()
        {
            Title = "MadWolf Twitch Bot Client";

            _config.SetConfig("D:/TestConfig.json");
            _uiContext = SynchronizationContext.Current;

            _commands = BotCommandService.LoadCommands("D:/Commands.json");
            foreach (var command in _commands)
            {
                var data = new BotCommand
                {
                    Command = command.Key,
                    Message = command.Value
                };

                ChatCommands.Add(data);
            }

            EditBotCommand = new RelayCommand(EditBotDetails, CanEditBotDetails);

            ConnectCommand = new RelayCommand(Connect, CanConnect);
            DisconnectCommand = new RelayCommand(Disconnect, CanDisconnect);

            AddCommand = new RelayCommand(AddChatCommand, CanAddChatCommand);

            GetDbData().Wait();
        }

        private async Task GetDbData()
        {
            var botData = await BotService.GetAllConfiguredBots();
            foreach (var bot in botData)
                ConfiguredBots.Add(new BasicBot(bot));

            m_selectedBot = ConfiguredBots.FirstOrDefault();
            TokenStatus = GetBotTokenStatus();

            await GetChannelHistory(m_selectedBot.Id);
            m_selectedChannel = TwitchChannels.Where(c => c.Id == m_selectedBot.ChannelId).FirstOrDefault();
        }

        private async Task GetChannelHistory(long Id)
        {
            var historyData = await HistoryService.GetChannelHistoryForBot(Id);
            foreach (var history in historyData)
            {
                var channel = new BasicChannel()
                {
                    Id = history.ChannelId,

                    UserName = history.ChannelUsername,
                    DisplayName = history.ChannelName
                };

                TwitchChannels.Add(channel);
            }
        }

        private OAuthTokenStatus GetBotTokenStatus()
        {
            if (String.IsNullOrEmpty(SelectedBot.OAuthToken))
                return OAuthTokenStatus.None;

            var tokenAge = DateTime.Now - (SelectedBot.TokenTimestamp ?? DateTime.MinValue);
            if (tokenAge.TotalMinutes >= 30)
                return OAuthTokenStatus.NeedsValidating;

            return OAuthTokenStatus.Validated; // TODO: make an API call here
        }

        private bool CanEditBotDetails()
        {
            return SelectedBot != null;
        }
        private void EditBotDetails()
        {
            var windowModal = new BotDetailsWindow
            {
                DataContext = new BotDetailsViewModel(SelectedBot)
            };

            if (windowModal.ShowDialog() == true)
            {
                var test = windowModal.DataContext as BotDetailsViewModel;
            }
        }

        private bool CanConnect()
        {
            if (SelectedBot == null || SelectedChannel == null)
                return false;

            return IsDisconnected && TokenStatus == OAuthTokenStatus.Validated;
        }
        private void Connect()
        {
            var msg = new Model.ChatMessage
            {
                Message = $"Connecting to {SelectedChannel.DisplayName}...",
                HexColour = "#FF000000"
            };
            Messages.Add(msg);

            ConnectionCredentials credentials = new ConnectionCredentials(SelectedBot.Username, SelectedBot.OAuthToken);
            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };

            WebSocketClient customClient = new WebSocketClient(clientOptions);
            _client = new TwitchClient(customClient);
            _client.Initialize(credentials, SelectedChannel.UserName);

            _client.OnConnected += OnBotConnected;
            _client.OnDisconnected += OnBotDisconnected;
            _client.OnJoinedChannel += OnChannelJoined;
            _client.OnMessageReceived += OnReceivedMessage;

            if (!_client.Connect())
                System.Windows.MessageBox.Show("Connection Failed");
        }

        private bool CanDisconnect()
        {
            return !IsDisconnected;
        }
        private void Disconnect()
        {
            _client.Disconnect();
        }

        private void OnBotConnected(object sender, OnConnectedArgs e)
        {
            Title = $"[CONNECTED] {Title}";
            IsDisconnected = false;

            var msg = new Model.ChatMessage
            {
                Message = "Connected!",
                HexColour = "#FF000000"
            };
            _uiContext.Send(x => Messages.Add(msg), null);
        }
        private void OnBotDisconnected(object sender, TwitchLib.Communication.Events.OnDisconnectedEventArgs e)
        {
            Title = Title.Replace("[CONNECTED] ", "");
            IsDisconnected = true;

            var msg = new Model.ChatMessage
            {
                Message = $"Disconnected from {SelectedChannel.DisplayName}",
                HexColour = "#FF000000"
            };
            _uiContext.Send(x => Messages.Add(msg), null);
        }

        private void OnChannelJoined(object sender, OnJoinedChannelArgs e)
        {
            _client.SendMessage(e.Channel, "/me " + BotCommandService.GetConnectionMessage());
        }

        private void OnReceivedMessage(object sender, OnMessageReceivedArgs e) 
        {
            var msg = new Model.ChatMessage
            {
                DisplayName = e.ChatMessage.DisplayName,
                Message = e.ChatMessage.Message,
                HexColour = e.ChatMessage.ColorHex
            };

            if (e.ChatMessage.Username.Equals(SelectedBot.Username))
                return;

            var parsedMessage = msg.Message.Trim().ToLower();

            if (_commands.ContainsKey(parsedMessage))
            {
                var botMessage = _commands[parsedMessage].Replace("{name}", msg.DisplayName);
                _client.SendReply(_client.JoinedChannels.FirstOrDefault(), e.ChatMessage.Id, botMessage);

                return;
            }
            if (parsedMessage.Contains("!shoutout"))
            {
                //TODO: Shoutout command
                return;
            }

            switch (parsedMessage)
            {
                case "!heaven":
                    _client.SendMessage(_client.JoinedChannels.FirstOrDefault(), "/me Uses Final Heaven");
                    Thread.Sleep(4200);
                    _client.SendMessage(_client.JoinedChannels.FirstOrDefault(), $"Critical direct hit! {msg.DisplayName} takes 731858 damage.");
                    break;
                case "!uptime":
                    break;
                case "!hello":
                case "!hi":
                case "!yo":
                    _client.SendReply(_client.JoinedChannels.FirstOrDefault(), e.ChatMessage.Id, $"/me Nods at {msg.DisplayName}");
                    break;
                case "!o/":
                    _client.SendReply(_client.JoinedChannels.FirstOrDefault(), e.ChatMessage.Id, @"\o");
                    break;
                default:
                    _uiContext.Send(x => Messages.Add(msg), null);
                    break;
            }
        }

        private bool CanAddChatCommand()
        {
            return ChatCommands.Count == _commands.Count;
        }
        private void AddChatCommand()
        {
            ChatCommands.Add(new BotCommand());
        }
    }

    class TokenStatusToUriConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var status = (OAuthTokenStatus)value;
            return status switch
            {
                OAuthTokenStatus.Validated => new Uri("/icons/StatusOK_16x.png", UriKind.Relative),
                OAuthTokenStatus.NeedsValidating => new Uri("/icons/StatusOK_16x.png", UriKind.Relative),
                OAuthTokenStatus.NotValidated => new Uri("/icons/StatusOK_16x.png", UriKind.Relative),
                _ => new Uri("/icons/StatusOK_16x.png", UriKind.Relative)
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var icon = value as Uri;
            return icon.OriginalString switch 
            {
                "/icons/StatusOK_16x.png" => OAuthTokenStatus.Validated,
                _ => OAuthTokenStatus.None
            };
        }
    }
}
