using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Telegram.Bot;

namespace PokerBotCore
{
    class User
    {
        public static TelegramBotClient tgbot = new TelegramBotClient("1341769299:AAE4q84mx-NRrSJndKsCVNVLr-SzjYeN7wk");
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public long Id { get; set; }
        public string FirstName;
        //[ConcurrencyCheck]
        public int Money { get; set; }
        public int bet;
        public virtual User Referal { get; set; }
        public int lastraise;
        public Combination combination;
        public uint count_messages = 0;
        public int output;
        public int id_privateroom;
        public int id_for_answer;
        public enum State { main, waitcount, wait, play, waitbet, waitmoney, output, output_waitnumber, codprvt, feedback, admin, mailing, add_coin, answer, change_table, chat }
        public State state;
        public Room room;
        public List<string> cards;
        static object block = new object();
        public void AddMoney(int count)
        {
            if (Id == 0) return;
            lock (block)
            {
                if (count == 0) return;
                Money += count;
                try
                {
                    tgbot.SendTextMessageAsync(Id, $"Коинов зачислено: {count}.");
                }
                catch { }
            }
        }
    }
}
