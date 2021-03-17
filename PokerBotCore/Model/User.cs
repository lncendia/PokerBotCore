using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PokerBotCore.Bot;
using PokerBotCore.Enums;
using PokerBotCore.Rooms;
using PokerBotCore.Rooms.RoomTypes;
using Telegram.Bot;

namespace PokerBotCore.Model
{
    public sealed class User
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long Id { get; set; }
        public int Money { get; set; }
        public int bet;
        public User Referal { get; set; }
        
        [NotMapped] public string firstName;
        [NotMapped] public int lastRaise;
        [NotMapped] public Combination combination;
        [NotMapped] public uint countMessages = 0;
        [NotMapped] public int output;
        [NotMapped] public int idPrivateRoom;
        [NotMapped] public int idForAnswer;
        [NotMapped] public State state;
        [NotMapped] public Room room;
        [NotMapped] public List<string> cards;
        [NotMapped] private static readonly object Block = new();

        public void AddMoney(int count)
        {
            lock (Block)
            {
                if (Id == 0 || count == 0) return;
                Money += count;
                try
                {
                    BotSettings.Get().SendTextMessageAsync(Id, $"Коинов зачислено: {count}.");
                }
                catch
                {
                    // ignored
                }
            }
        }
    }
}
