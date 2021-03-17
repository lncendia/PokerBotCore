using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using PokerBotCore.Bot.CallbackQueryCommands;
using PokerBotCore.Bot.Commands;
using PokerBotCore.Interfaces;
using PokerBotCore.Model;
using PokerBotCore.Rooms;
using PokerBotCore.Rooms.RoomTypes;
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
            callbackQueryCommands = InitialiseCallbackQueryCommands();
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
                new EnterCoinCountCommand()
            };
        }
        private static List<ICallbackQueryCommand> InitialiseCallbackQueryCommands()
        {
            return new()
            {
                new CallQuery(),
               new CheckQuery(),
               new FoldQuery(),
               new RaiseQuery(),
               new VABankQuery(),
               new ChangeTableQuery(),
               new CheckFriendsQuery(),
               new CheckPaymentQuery(),
               new ConnectToRoomQuery(),
               new CreateFakeRoomQuery(),
               new CreatePrivateRoomQuery(),
               new CreatePublicRoomQuery(),
               new ExitRoomQuery(),
               new RemoveFakeRoomQuery(),
               new SendRoomToChatQuery(),
               new SentRequestsQuery(),
               new SetStandardTableQuery(),
               new AddFriendQuery(),
               new BackAdminQuery(),
               new AnswerAdminQuery()


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