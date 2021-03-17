using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using PokerBotCore.Bot.Commands;
using PokerBotCore.Interfaces;
using PokerBotCore.Model;
using PokerBotCore.Rooms;
using Telegram.Bot;

namespace PokerBotCore.Bot
{
    public static class BotSettings
    {
        public static TelegramBotClient Get()
        {
            if (_client != null) return _client;
            _client = new TelegramBotClient("1341769299:AAE4q84mx-NRrSJndKsCVNVLr-SzjYeN7wk");
            commands = InitialiseCommands();
            callbackQueryCommands = new List<ICallbackQueryCommand>();
            using Db db = new Db();
            users = db.Users.ToList();
            friendships = db.Friendships.ToList();
            rooms = new List<Room>();
            fakeRooms = new List<FakeRoom>();
            reviews = new ConcurrentQueue<string>();
            return _client;

        }

        private static List<ITextCommand> InitialiseCommands()
        {
            return new()
            {
                new AdminMailingCommand(),
                new AdminRoomWithBotsCommand(),
                new ConnectToFriendCommand(),
                new CreateRoomCommand(),
                new ExitCommand(),
                new FeedbackCommand(),
                new ListRoomsCommand(),
                new PayoutCommand(),
                new ProfileCommand(),
                new RemoveFriendCommand(),
                new StartCommand(),
                new TopUpCommand(),
                new AddFriendCommand(),
                new GameChatCommand(),
                new AdminAddMoneyCommand(),
                new AdminCheckFeedbackCommand(),
                new AdminCommand(),
                new EnterBetCommand(),
                new EnterCountPlayersCommand(),
                new EnterFeedbackCommand(),
                new EnterPasswordCommand(),
                new EnterPayoutCommand(),
                new EnterPayoutNumberCommand(),
                new EnterPhotoTableCommand(),
                new EnterTopUpCommand(),
                new EnterMailingMessageCommand(),
                new EnterIdFakeRoomToDeleteCommand(),
                new EnterCountPlayersOfFakeRoomCommand(),
                new EnterAnswerMessageCommand(),
                new EnterCoinCountCommand(),
            };
        }
        public static List<Room> rooms;
        public static List<User> users;
        public static List<Friendship> friendships;
        public static List<FakeRoom> fakeRooms;
        public static ConcurrentQueue<string> reviews;
        private static TelegramBotClient _client;
        public static List<ITextCommand> commands;
        public static List<ICallbackQueryCommand> callbackQueryCommands;

    }
}