using System.Collections.Generic;
using System.Linq;
using PokerBotCore.Model;

namespace PokerBotCore.Rooms
{
    public static class CombinationChecker
    {
        private static bool CheckStraightFlush(List<char> su, List<sbyte> nom, User user)
        {
            var nominal = new List<sbyte>();
            var suit = new List<char>();
            if (nom.Contains(14))
            {
                var x = nom.IndexOf(14);
                nominal.Add(1);
                suit.Add(su[x]);
            }

            nominal.AddRange(nom);
            suit.AddRange(su);
            int first = 0;
            for (int i = 0; i < nominal.Count - 4; i++)
            {
                List<sbyte> straightFlush = new List<sbyte>() {nominal[i]};
                int count = 0;
                for (int j = i + 1; j < nominal.Count; j++)
                {
                    if (straightFlush[count] + 1 != nominal[j] || suit[i] != suit[j]) continue;
                    straightFlush.Add(nominal[j]);
                    count++;
                }

                if (count >= 4)
                {
                    first = straightFlush[0];
                }
            }

            switch (first)
            {
                case 0:
                    return false;
                case 10:
                    user.combination = new Combination((sbyte) first, Combination.Comb.royalFlush);
                    return true;
                default:
                    user.combination = new Combination((sbyte) first, Combination.Comb.straightFlush);
                    return true;
            }
        }

        public static bool CheckKare(List<sbyte> nominal, User user, ref List<sbyte> cards)
        {
            for (int i = 0; i < nominal.Count - 3; i++)
            {
                int same = 1;
                for (int j = i + 1; j < nominal.Count; j++)
                {
                    if (nominal[i] == nominal[j]) same++;
                }

                if (same != 4) continue;
                cards = new List<sbyte> {nominal[i]};
                user.combination = new Combination(nominal[i], Combination.Comb.kare);
                return true;
            }

            return false;
        }

        private static bool CheckFullHouse(List<sbyte> nominal, User user)
        {
            var sets = new List<sbyte>();
            for (int i = 0; i < nominal.Count - 2; i++)
            {
                int same = 1;
                for (int j = i + 1; j < nominal.Count; j++)
                {
                    if (nominal[i] == nominal[j]) same++;
                }

                if (same == 3) sets.Add(nominal[i]);
            }

            if (sets.Count == 0) return false;
            sbyte maxSet = sets.Max();
            List<sbyte> nominals = new List<sbyte>();
            for (int i = 0; i < nominal.Count - 1; i++)
            {
                if (nominal[i] == maxSet) continue;
                int same = 1;
                for (int j = i + 1; j < nominal.Count; j++)
                {
                    if (nominal[i] == nominal[j]) same++;
                }

                if (same == 2) nominals.Add(nominal[i]);
            }

            if (nominals.Count == 0) return false;
            user.combination = new Combination(maxSet, Combination.Comb.fullHouse, nominals.Max());
            return true;
        }

        static bool CheckFlush(List<char> suit, List<sbyte> nominal, User user)
        {
            int first = 0;
            for (int i = 0; i < nominal.Count - 4; i++)
            {
                List<sbyte> flush = new List<sbyte>() {nominal[i]};
                for (int j = i + 1; j < nominal.Count; j++)
                {
                    if (suit[i] == suit[j]) flush.Add(nominal[j]);
                }

                if (flush.Count >= 5)
                {
                    first = flush.Max();
                }
            }

            if (first == 0) return false;
            user.combination = new Combination((sbyte) first, Combination.Comb.flush);
            return true;
        }

        static bool CheckStraight(List<sbyte> nom, User user)
        {
            var nominal = new List<sbyte>();
            if (nom.Contains(14)) nominal.Add(1);
            nominal.AddRange(nom.Distinct());
            int first = 0;
            for (int i = 0; i < nominal.Count - 4; i++)
            {
                var straight = new List<sbyte>() {nominal[i]};
                for (int j = i + 1; j < nominal.Count; j++)
                {
                    if (nominal[i] + j - i == nominal[j]) straight.Add(nominal[j]);
                    else break;
                }

                if (straight.Count >= 5)
                {
                    first = nominal[i];
                }
            }

            if (first == 0) return false;
            user.combination = new Combination((sbyte) first, Combination.Comb.straight);
            return true;
        }

        public static bool CheckSet(List<sbyte> nominal, User user, ref List<sbyte> cards)
        {
            List<sbyte> nominals = new List<sbyte>();
            for (int i = 0; i < nominal.Count - 2; i++)
            {
                int same = 1;
                for (int j = i + 1; j < nominal.Count; j++)
                {
                    if (nominal[i] == nominal[j]) same++;
                }

                if (same == 3) nominals.Add(nominal[i]);
            }

            if (nominals.Count <= 0) return false;
            cards = new List<sbyte>() {nominals.Max()};
            user.combination = new Combination(nominals.Max(), Combination.Comb.set);
            return true;
        }

        public static bool CheckPair(List<sbyte> nominal, User user, ref List<sbyte> cards)
        {
            List<sbyte> nominals = new List<sbyte>();
            for (int i = 0; i < nominal.Count - 1; i++)
            {
                int same = 1;
                for (int j = i + 1; j < nominal.Count; j++)
                {
                    if (nominal[i] == nominal[j]) same++;
                }

                if (same == 2) nominals.Add(nominal[i]);
            }

            switch (nominals.Count)
            {
                case <= 0:
                    return false;
                case > 1:
                {
                    var x1 = nominals.Max();
                    nominals.Remove(nominals.Max());
                    var x2 = nominals.Max();
                    cards = new List<sbyte>() {x1, x2};
                    user.combination = new Combination(x1, Combination.Comb.twoPair, x2);
                    return true;
                }
            }

            if (nominals.Count != 1) return false;
            cards = new List<sbyte>() {nominals.Max()};
            user.combination = new Combination(nominals[0], Combination.Comb.pair);
            return true;
        }

        public static void CheckCombination(List<string> cardm, User user)
        {
            var suit = new List<char>();
            var nominal = new List<sbyte>();
            foreach (var t in cardm)
            {
                suit.Add(t[^1]);
                nominal.Add(CardsOperation.GetNominal(t));
            }

            CardsOperation.Sort(suit, nominal);


            var x = new List<sbyte>();
            if (CheckStraightFlush(suit, nominal, user)) return;
            if (CheckKare(nominal, user, ref x)) return;
            if (CheckFullHouse(nominal, user)) return;
            if (CheckFlush(suit, nominal, user)) return;
            if (CheckStraight(nominal, user)) return;
            if (CheckSet(nominal, user, ref x)) return;
            if (CheckPair(nominal, user, ref x)) return;
            if (CardsOperation.GetNominal(user.cards[0]) > CardsOperation.GetNominal(user.cards[1]))
                user.combination = new Combination(0);
        }
    }
}