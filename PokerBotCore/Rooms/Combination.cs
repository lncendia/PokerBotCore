using PokerBotCore.Model;

namespace PokerBotCore.Rooms
{
    public class Combination
    {
        public readonly sbyte nominal;
        public readonly sbyte nominal2;

        public enum Comb
        {
            nullCombination,
            pair,
            twoPair,
            set,
            straight,
            flush,
            fullHouse,
            kare,
            straightFlush,
            royalFlush
        };

        public readonly Comb combination;

        public Combination(sbyte nominal, Comb comb = Comb.nullCombination, sbyte nominal2 = 0)
        {
            combination = comb;
            this.nominal = nominal;
            this.nominal2 = nominal2;
        }

        public string ToString(User user)
        {
            switch (combination)
            {
                case Comb.nullCombination:
                    return GameOperations.GetNominal(user.cards[0]) > GameOperations.GetNominal(user.cards[1])
                        ? $"Высшая карта {user.cards[0].Remove(user.cards[0].Length - 2)}"
                        : $"Высшая карта {user.cards[1].Remove(user.cards[1].Length - 2)}";
                case Comb.pair:
                    return "Пара";
                case Comb.twoPair:
                    return "Две пары";
                case Comb.set:
                    return "Сет";
                case Comb.straight:
                    return "Стрит";
                case Comb.flush:
                    return "Флеш";
                case Comb.fullHouse:
                    return "Фулл хаус";
                case Comb.kare:
                    return "Каре";
                case Comb.straightFlush:
                    return "Стритфлеш";
                case Comb.royalFlush:
                    return "Роялфлеш";
            }

            return null;
        }
    }
}
