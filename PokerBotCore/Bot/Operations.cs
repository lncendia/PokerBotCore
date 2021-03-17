using System;
using System.Linq;
using System.Threading.Tasks;
using PokerBotCore.Enums;
using PokerBotCore.Model;
using PokerBotCore.Rooms;
using PokerBotCore.Rooms.RoomTypes;

namespace PokerBotCore.Bot
{
    public static class Operations
    {
        public static Room GetRoom(int id)
        {
            return BotSettings.rooms.ToList().FirstOrDefault(room => id == room.id);
        }
        public static FakeRoom GetFaceRoom(int id)
        {
            return BotSettings.fakeRooms.ToList().FirstOrDefault(room => id == room.id);
        }

        private static readonly object Obj = new();
        public static Room CreateRoom(int count, User user, bool isPrivate, bool isFake = false)
        {
            lock (Obj)
            {
                Room room;
                if (isFake)
                {
                    room = new FakeRoom(user, count);
                    BotSettings.rooms.Add(room);
                    return room;
                }
                if (isPrivate)
                {
                    room = new Room(user, count, true);
                    user.room = room;
                    user.state = State.wait;
                }
                else
                {
                    room = new Room(user, count, false);
                    user.room = room;
                    user.state = State.wait;
                }

                BotSettings.rooms.Add(room);
                return room;
            }
        }
        public static User GetUser(long id)
        {
            return BotSettings.users.ToList().FirstOrDefault(user => id == user.Id);
        }
        public static DateTime time;
        public static async void Mute()
        {
            try
            {
                while (true)
                {
                    time = DateTime.Now.AddMinutes(3);
                    await Task.Delay(180000);
                    foreach (User user in BotSettings.users)
                    {
                        user.countMessages = 0;
                    }

                }
            }
            catch
            {
                Mute();
            }
        }
    }
}