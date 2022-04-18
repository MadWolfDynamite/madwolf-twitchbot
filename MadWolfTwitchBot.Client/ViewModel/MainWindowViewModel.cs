﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MadWolfTwitchBot.BotCommands;
using MadWolfTwitchBot.Client.Constants;
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
        private readonly BotConfiguration _config = new();

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
                    GetChannelHistory(m_selectedBot.Id);

                    TokenStatus = GetBotTokenStatus().Result; 
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

        public ICommand GetTokenCommand { get; private set; }
        public ICommand RefreshTokenCommand { get; private set; }

        public ICommand ConnectCommand { get; private set; }
        public ICommand DisconnectCommand { get; private set; }

        public ICommand AddCommand { get; private set; }
        public ICommand RemoveCommand { get; private set; }

        public MainWindowViewModel()
        {
            WolfAPIService.SetApiEndpoint(ApiSettings.Endpoint);
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

            EditBotCommand = new AsyncRelayCommand(EditBotDetails, CanEditBotDetails);

            GetTokenCommand = new AsyncRelayCommand(GetOAuthToken, CanGetOAuthToken);
            RefreshTokenCommand = new AsyncRelayCommand(RefreshOAuthToken, CanRefreshToken);

            ConnectCommand = new RelayCommand(Connect, CanConnect);
            DisconnectCommand = new RelayCommand(Disconnect, CanDisconnect);

            AddCommand = new RelayCommand(AddChatCommand, CanAddChatCommand);

            GetDbData();
        }

        private async Task GetDbData()
        {
            var botData = await BotService.GetAllConfiguredBots();
            foreach (var bot in botData)
                ConfiguredBots.Add(new BasicBot(bot));

            SetProperty(ref m_selectedBot, ConfiguredBots.FirstOrDefault(), nameof(SelectedBot));

            await GetChannelHistory(m_selectedBot.Id);
            m_selectedChannel = TwitchChannels.FirstOrDefault(c => c.Id == m_selectedBot.ChannelId);

            TokenStatus = await GetBotTokenStatus();
        }

        private async Task RefreshBotData(BasicBot selected = null)
        {
            ConfiguredBots.Clear();

            var botData = await BotService.GetAllConfiguredBots();
            foreach (var bot in botData)
                ConfiguredBots.Add(new BasicBot(bot));

            SelectedBot = ConfiguredBots.FirstOrDefault(b => b.Id == selected.Id);
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

            SelectedChannel = TwitchChannels.FirstOrDefault(c => c.Id == m_selectedBot.ChannelId);
        }

        private async Task<OAuthTokenStatus> GetBotTokenStatus()
        {
            if (String.IsNullOrEmpty(SelectedBot.OAuthToken))
                return OAuthTokenStatus.None;

            var tokenAge = DateTime.UtcNow - (SelectedBot.TokenTimestamp ?? DateTime.MinValue);
            if (tokenAge.TotalMinutes >= TokenSettings.RefreshMinutes) 
            { 
                var validationResult = await WolfAPIService.ValidateOAuthToken(SelectedBot.OAuthToken);
                return validationResult ? OAuthTokenStatus.Valid : OAuthTokenStatus.NotValid;
            }
                
            return OAuthTokenStatus.Valid;
        }

        private bool CanGetOAuthToken()
        {
            if (SelectedBot == null)
                return false;

            return TokenStatus == OAuthTokenStatus.None;
        }
        private async Task GetOAuthToken()
        {
            var tokenRequestWindow = new OAuthTokenRetievalWindow
            {
                DataContext = new OAuthTokenRetrievalViewModel(SelectedBot.Username)
            };

            if (tokenRequestWindow.ShowDialog() == true)
            {
                var data = tokenRequestWindow.DataContext as OAuthTokenRetrievalViewModel;

                var updatedDetails = await BotService.CreateOrUpdateBot(SelectedBot.Id, SelectedBot.Username, SelectedBot.DisplayName, data.OAuthToken, data.RefreshToken, data.TokenTimestamp);
                if (updatedDetails != null)
                    await RefreshBotData(new BasicBot(updatedDetails));
            }
        }
        
        private bool CanRefreshToken()
        {
            if (SelectedBot == null)
                return false;

            return TokenStatus == OAuthTokenStatus.NotValid;
        }
        private async Task RefreshOAuthToken()
        {
            var newToken = await WolfAPIService.RefreshTwitchTokenAsync(ApiSettings.ClientId, ApiSettings.ClientSecret, SelectedBot.RefreshToken);
            if (newToken != null)
            {
                var updatedDetails = await BotService.CreateOrUpdateBot(SelectedBot.Id, SelectedBot.Username, SelectedBot.DisplayName, newToken.Access_Token, newToken.Refresh_Token, DateTime.UtcNow);
                if (updatedDetails != null)
                    await RefreshBotData(new BasicBot(updatedDetails));
            }
        }

        private bool CanEditBotDetails()
        {
            return SelectedBot != null;
        }
        private async Task EditBotDetails()
        {
            var windowModal = new BotDetailsWindow
            {
                DataContext = new BotDetailsViewModel(SelectedBot)
            };

            if (windowModal.ShowDialog() == true)
            {
                var data = windowModal.DataContext as BotDetailsViewModel;
                var result = await BotService.CreateOrUpdateBot(SelectedBot.Id, data.Username, data.DisplayName, data.OAuthToken, data.RefreshToken, data.TokenTimestamp);

                if (result == null)
                {
                    System.Windows.MessageBox.Show("Oops...");
                    return;
                }

                await RefreshBotData(new BasicBot(result));
            }
        }

        private bool CanConnect()
        {
            if (SelectedBot == null || SelectedChannel == null)
                return false;

            return IsDisconnected && TokenStatus == OAuthTokenStatus.Valid;
        }
        private void Connect()
        {
            Messages.Clear();

            var msg = new Model.ChatMessage
            {
                Message = $"Connecting to {SelectedChannel.DisplayName}...",
                HexColour = "#FF000000"
            };
            Messages.Add(msg);

            ConnectionCredentials credentials = new(SelectedBot.Username, SelectedBot.OAuthToken);
            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };

            WebSocketClient customClient = new(clientOptions);
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
                Message = "Connected",
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

            var channel = _client.JoinedChannels.FirstOrDefault();
            var parsedMessage = msg.Message.Trim().ToLower();

            if (_commands.ContainsKey(parsedMessage))
            {
                var botMessage = _commands[parsedMessage].Replace("{name}", msg.DisplayName);
                _client.SendMessage(channel, botMessage);

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
                    _client.SendMessage(channel, "/me Uses Final Heaven");
                    Thread.Sleep(4200);
                    _client.SendMessage(channel, $"Critical direct hit! {msg.DisplayName} takes 731858 damage.");
                    break;
                case "!uptime":
                    break;
                case "!hello":
                case "!hi":
                case "!yo":
                    _client.SendMessage(channel, $"/me Nods at {msg.DisplayName}");
                    break;
                case "!o/":
                    _client.SendReply(channel, e.ChatMessage.Id, @"\o");
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
            if (value == null)
                return new Uri("/icons/StatusSecurityWarningOutline_16x.png", UriKind.Relative);

            var status = (OAuthTokenStatus)value;
            return status switch
            {
                OAuthTokenStatus.Valid => new Uri("/icons/StatusOKOutline_16x.png", UriKind.Relative),
                OAuthTokenStatus.NotValid => new Uri("/icons/StatusInvalidOutline_16x.png", UriKind.Relative),
                _ => new Uri("/icons/StatusSecurityWarningOutline_16x.png", UriKind.Relative)
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var icon = value as Uri;
            return icon.OriginalString switch 
            {
                "/icons/StatusOKOutline_16x.png" => OAuthTokenStatus.Valid,
                "/icons/StatusInvalidOutline_16x.png" => OAuthTokenStatus.NotValid,
                _ => OAuthTokenStatus.None
            };
        }
    }
}
