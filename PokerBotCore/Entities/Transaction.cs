using System.ComponentModel.DataAnnotations;

namespace PokerBotCore.Entities
{
    class Transaction
    {
        public long Id { get; set; }
        public User User { get; set; }
        public int Money { get; set; }
        [MaxLength(15)]
        public string Number { get; set; }
        [MaxLength(15)]
        public string Date { get; set; }
    }
}
