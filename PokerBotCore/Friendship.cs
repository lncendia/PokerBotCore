using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokerBot
{
    class Friendship
    {
        public int ID { get; set; }
        public virtual User User1 { get; set; }
        public virtual User User2 { get; set; }
        public bool Accepted { get; set; }
    }
}
