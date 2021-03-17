using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PokerBotCore.Enums;
using PokerBotCore.Rooms;
using Telegram.Bot;

namespace PokerBotCore.Model
{
    public sealed class User
    {
        private static readonly TelegramBotClient Tgbot = new("1341769299:AAE4q84mx-NRrSJndKsCVNVLr-SzjYeN7wk");
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long Id { get; set; }
        public string firstName;
        public int Money { get; set; }
        public int bet;
        public User Referal { get; set; }
        public int lastRaise;
        public Combination combination;
        public uint countMessages = 0;
        public int output;
        public int idPrivateRoom;
        public int idForAnswer;
        public State state;
        public Room room;
        public List<string> cards;
        private static readonly object Block = new();
        public void AddMoney(int count)
        {
            if (Id == 0) return;
            lock (Block)
            {
                if (count == 0) return;
                Money += count;
                try
                {
                    Tgbot.SendTextMessageAsync(Id, $"Коинов зачислено: {count}.");
                }
                catch
                {
                    // ignored
                }
            }
        }
    }
}
