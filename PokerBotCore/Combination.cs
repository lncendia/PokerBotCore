namespace PokerBotCore
{
    class Combination
    {
        public sbyte nominal;
        public sbyte nominal2;
        public enum Comb { nullcomb, pair, twopair, set, straight, flush, fullhouse, kare, straightflush, royalflush }
        public Comb combination;
        public Combination(sbyte nominal, Comb comb = Comb.nullcomb, sbyte nominal2 = 0)
        {
            combination = comb;
            this.nominal = nominal;
            this.nominal2 = nominal2;
        }
        public string ToString(User user)
        {
            switch (combination)
            {
                case Comb.nullcomb:
                    if (Operation.GetNominal(user.cards[0]) > Operation.GetNominal(user.cards[1]))
                        return $"Высшая карта {user.cards[0].Remove(user.cards[0].Length - 2)}";
                    else
                        return $"Высшая карта {user.cards[1].Remove(user.cards[1].Length - 2)}";
                case Comb.pair:
                    return "Пара";
                case Comb.twopair:
                    return "Две пары";
                case Comb.set:
                    return "Сет";
                case Comb.straight:
                    return "Стрит";
                case Comb.flush:
                    return "Флеш";
                case Comb.fullhouse:
                    return "Фулл хаус";
                case Comb.kare:
                    return "Каре";
                case Comb.straightflush:
                    return "Стритфлеш";
                case Comb.royalflush:
                    return "Роялфлеш";
            }
            return null;
        }
    }
}
