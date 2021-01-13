using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokerBot
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
