using System;
using System.Linq;
using System.Threading.Tasks;
using PokerBotCore.Entities;
using PokerBotCore.Rooms;

namespace PokerBotCore.Bot
{
    public static class Operations
    {
        public static Room GetRoom(int id)
        {
            return MainBot.rooms.ToList().FirstOrDefault(room => id == room.id);
        }
        public static User GetUser(long id)
        {
            return MainBot.users.ToList().FirstOrDefault(user => id == user.Id);
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
                    foreach (User user in MainBot.users)
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

        public static async void SaveDb(User user)
        {
            
        }
    }
}