using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using PokerBotCore.Bot;
using PokerBotCore.Enums;
using PokerBotCore.Keyboards;
using PokerBotCore.Model;

namespace PokerBotCore.Rooms
{
    static class GameOperations
    {
        public static void EmergencySaveMoney(List<User> players, Room room)
        {
            room.bet = 0;
            foreach (User user in players.ToList())
            {
                user.state = State.main;
                user.Money += user.bet;
            }
        }

        public static List<string> CreateCards()
        {
            List<string> nominal = new List<string>
            {
                "Два", "Три", "Четыре", "Пять", "Шесть", "Семь", "Восемь", "Девять", "Десять", "Валет", "Дама",
                "Король", "Туз"
            };
            List<string> deck = new List<string>() {"♣", "♠", "♥", "♦"};
            return (from str in nominal from strDeck in deck select $"{str} {strDeck}").ToList();

        }
        public static void ExceptionInGame(Exception ex, Room room, bool isFinal = false)
        {
            BotSettings.reviews.Enqueue(
                $"0:Эксепшн: {ex.Message}\nОбъект, вызвавший исключение: {ex.Source}\nМетод, вызвавший исключение: {ex.TargetSite}");
            if (room.block && !isFinal) return;
            room.block = true;
            EmergencySaveMoney(room.players, room);
            room.SendMessage($"Произошла ошибка. Приносим свои извинения, средства были возвращены!", room.players,
                MainKeyboards.MainKeyboard, false);
            foreach (User user in room.players.ToList())
            {
                room.UserLeave(user);
            }
        }
        #region CheckCombination

        public static sbyte GetNominal(string card)
        {
            return card.Remove(card.Length - 2) switch
            {
                "Два" => 2,
                "Три" => 3,
                "Четыре" => 4,
                "Пять" => 5,
                "Шесть" => 6,
                "Семь" => 7,
                "Восемь" => 8,
                "Девять" => 9,
                "Десять" => 10,
                "Валет" => 11,
                "Дама" => 12,
                "Король" => 13,
                "Туз" => 14,
                _ => 0
            };
        }

        private static void Sort(List<char> cd, List<sbyte> keys)
        {
            for (int i = 0; i < keys.Count - 1; i++)
            {
                for (int j = i + 1; j < keys.Count; j++)
                {
                    if (keys[i] > keys[j])
                    {
                        sbyte vol = keys[i];
                        keys[i] = keys[j];
                        keys[j] = vol;
                        char s = cd[i];
                        cd[i] = cd[j];
                        cd[j] = s;
                    }
                }
            }
        }

        private static bool CheckStraightFlush(List<char> su, List<sbyte> nom, User user, ref string name)
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
                    name = "Роялфлеш";
                    return true;
                default:
                    user.combination = new Combination((sbyte) first, Combination.Comb.straightFlush);
                    name = "Стритфлеш";
                    return true;
            }
        }

        static bool CheckKare(List<sbyte> nominal, User user, ref string name, ref List<sbyte> cards)
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
                name = "Каре";
                return true;
            }

            return false;
        }

        private static bool CheckFullHouse(List<sbyte> nominal, User user, ref string name)
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
            name = "Фулл хаус";
            return true;
        }

        static bool CheckFlush(List<char> suit, List<sbyte> nominal, User user, ref string name)
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
            name = "Флеш";
            return true;
        }

        static bool CheckStraight(List<sbyte> nom, User user, ref string name)
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
            name = "Стрит";
            return true;
        }

        static bool CheckSet(List<sbyte> nominal, User user, ref string name, ref List<sbyte> cards)
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
            name = "Сет";
            return true;
        }

        static bool CheckPair(List<sbyte> nominal, User user, ref string name, ref List<sbyte> cards)
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
                    name = "Две пары";
                    return true;
                }
            }

            if (nominals.Count != 1) return false;
            cards = new List<sbyte>() {nominals.Max()};
            user.combination = new Combination(nominals[0], Combination.Comb.pair);
            name = "Пара";
            return true;
        }

        #endregion

        public static string GetNameFile(string card)
        {
            sbyte nominal1 = GetNominal(card);
            string nominal;
            switch (nominal1)
            {
                case 11:
                    nominal = "jack";
                    break;
                case 12:
                    nominal = "queen";
                    break;
                case 13:
                    nominal = "king";
                    break;
                case 14:
                    nominal = "ace";
                    break;
                default:
                    nominal = nominal1.ToString();
                    break;
            }

            string suitStr = card.Substring(card.Length - 1), suit = "";
            switch (suitStr)
            {
                case "♣":
                    suit = "clubs";
                    break;
                case "♠":
                    suit = "spades";
                    break;
                case "♥":
                    suit = "hearts";
                    break;
                case "♦":
                    suit = "diamonds";
                    break;
            }

            return $"{nominal}_of_{suit}.png";
        }

        public static Image GetImage(List<string> cards, User user)
        {
            Image table;
            table = Image.FromFile(File.Exists($"tables\\{user.Id}.jpg")
                ? $"tables\\{user.Id}.jpg"
                : "tables\\table.jpg");
            Graphics g = Graphics.FromImage(table);
            List<Image> images = new List<Image>();
            foreach (string str in cards)
            {
                images.Add(Image.FromFile($"cards\\{GetNameFile(str)}"));
            }
            
            int x = 50;
            int y = (table.Width - 25) / 5;
            for (int i = 0; i < images.Count - 2; i++)
            {
                g.DrawImage(images[i], x + y * i, 75);
            }
            
            g.DrawImage(images[^2], 520, 545);
            g.DrawImage(images[^1], 820, 545);
            return table;
        }

        public static string CheckCombination(List<string> cardm, User user)
        {
            var suit = new List<char>();
            var nominal = new List<sbyte>();
            foreach (var t in cardm)
            {
                suit.Add(t[^1]);
                nominal.Add(GetNominal(t));
            }

            Sort(suit, nominal);

            #region check

            string name = "";
            var x = new List<sbyte>();
            if (CheckStraightFlush(suit, nominal, user, ref name)) return name;
            if (CheckKare(nominal, user, ref name, ref x)) return name;
            if (CheckFullHouse(nominal, user, ref name)) return name;
            if (CheckFlush(suit, nominal, user, ref name)) return name;
            if (CheckStraight(nominal, user, ref name)) return name;
            if (CheckSet(nominal, user, ref name, ref x)) return name;
            if (CheckPair(nominal, user, ref name, ref x)) return name;
            if (GetNominal(user.cards[0]) > GetNominal(user.cards[1]))
            {
                user.combination = new Combination(0);
                return $"Высшая карта {user.cards[0].Remove(user.cards[0].Length - 2)}";
            }
            else
            {
                user.combination = new Combination(0);
                return $"Высшая карта {user.cards[1].Remove(user.cards[1].Length - 2)}";
            }

            #endregion
        }

        public static bool CheckRaise(List<User> users, int lastRise)
        {
            List<User> folded = users[0].room.foldUsers;
            List<User> allIn = users[0].room.allInUsers;
            List<User> playingUsers = users.Where(t => !folded.Contains(t)).Where(t => !allIn.Contains(t)).ToList();
            return playingUsers.All(t => t.lastRaise == lastRise);
        }

        public static List<User> MaxCombination(List<User> us)
        {
            var users = us.ToList();
            foreach (var user in users.ToList()
                .Where(user => user.combination == null || user.room.foldUsers.Contains(user)))
            {
                users.Remove(user);
            }

            Combination.Comb combination = users[0].combination.combination;
            foreach (var user in users.Where(user => user.combination.combination > combination))
            {
                combination = user.combination.combination;
            }

            List<User> winners = users.Where(user => user.combination.combination == combination).ToList();
            sbyte max = winners[0].combination.nominal;
            foreach (var user in winners.Where(user => user.combination.nominal > max))
            {
                max = user.combination.nominal;
            }

            foreach (var user in winners.ToList().Where(user => user.combination.nominal != max))
            {
                winners.Remove(user);
            }

            if (winners.Count == 1) return winners;
            if (winners[0].combination.combination == Combination.Comb.twoPair ||
                winners[0].combination.combination == Combination.Comb.fullHouse)
            {
                max = winners[0].combination.nominal2;
                foreach (var user in winners.Where(user => user.combination.nominal2 > max))
                {
                    max = user.combination.nominal2;
                }

                foreach (var user in winners.ToList().Where(user => user.combination.nominal2 != max))
                {
                    winners.Remove(user);
                }
            }

            if (winners.Count == 1) return winners;
            List<sbyte> cards = new List<sbyte>();
            if (winners[0].combination.combination != Combination.Comb.pair &&
                winners[0].combination.combination != Combination.Comb.twoPair &&
                winners[0].combination.combination != Combination.Comb.set &&
                winners[0].combination.combination != Combination.Comb.kare &&
                winners[0].combination.combination != Combination.Comb.nullCombination) return winners;
            {
                string name = "";
                var cardsNotHand = new List<sbyte>();
                foreach (User user in winners)
                {
                    var openedCards = user.room.openedCards.ToList();
                    openedCards.AddRange(user.cards);
                    var suit = new List<char>();
                    var nominal = new List<sbyte>();
                    foreach (var t in openedCards)
                    {
                        suit.Add(t[^1]);
                        nominal.Add(GetNominal(t));
                    }

                    Sort(suit, nominal);
                    switch (combination)
                    {
                        case Combination.Comb.set:
                            CheckSet(nominal, user, ref name, ref cardsNotHand);
                            break;
                        case Combination.Comb.kare:
                            CheckKare(nominal, user, ref name, ref cardsNotHand);
                            break;
                        case Combination.Comb.nullCombination:
                            break;
                        default:
                            CheckPair(nominal, user, ref name, ref cardsNotHand);
                            break;
                    }

                    cards.AddRange(from x in openedCards
                        where !cardsNotHand.Contains(GetNominal(x))
                        select GetNominal(x));
                }

                cards = cards.Distinct().ToList();
                cards.Sort();
                cards.Reverse();
                List<sbyte> kickers = new List<sbyte>();
                switch (combination)
                {
                    case Combination.Comb.nullCombination:
                        kickers.AddRange(cards.GetRange(0,5));
                        break;
                    case Combination.Comb.pair:
                        kickers.AddRange(cards.GetRange(0,3));
                        break;
                    case Combination.Comb.set:
                        kickers.AddRange(cards.GetRange(0,2));
                        break;
                    default:
                        kickers.Add(cards[0]);
                        break;
                }

                users.Clear();
                foreach (sbyte kick in kickers)
                {
                    users.AddRange(from user in winners
                        let cardsUser = new List<sbyte> {GetNominal(user.cards[0]), GetNominal(user.cards[1])}
                        where cardsUser.Contains(kick)
                        select user);
                    if (users.Count > 0) break;
                }

                if (users.Count != 0) return users;
            }
            return winners;
        }
        public static void Payment(List<User> winners, List<User> playersNotLeave, Room room)
        {
            try
            {
                using Db db = new Db();
                if (room.allInUsers.Count != 0)
                {
                    var notFold = playersNotLeave.ToList();
                    foreach (User user in room.foldUsers) notFold.Remove(user);
                    var playSidePod = notFold.ToList();
                    int j = 0;
                    while (room.bet > 0)
                    {
                        int mainBank = 0, min = playSidePod[0].bet;
                        min = playSidePod.Select(user => user.bet).Prepend(min).Min();
                        foreach (User user in playersNotLeave)
                        {
                            if (user.bet < min)
                            {
                                mainBank += user.bet;
                                user.bet = 0;
                            }
                            else
                            {
                                mainBank += min;
                                user.bet -= min;
                            }

                            if (user.bet == 0) playSidePod.Remove(user);
                        }
                        
                        for (int i = 0; i < room.leavedPlayers.Count; i++)
                        {
                            if (room.leavedPlayers[i] < min)
                            {
                                mainBank += room.leavedPlayers[0];
                                room.leavedPlayers[0] = 0;
                            }
                            else
                            {
                                mainBank += min;
                                room.leavedPlayers[i] -= min;
                            }
                        }

                        room.bet -= mainBank;
                        int win = mainBank / winners.Count;
                        if (j > 0) room.SendMessage($"Вы забираете побочный банк №{j}.", winners, null);
                        foreach (var user in winners.Where(_ => playSidePod.Count != playersNotLeave.Count))
                        {
                            user.AddMoney(win);
                        }

                        j++;
                        winners = MaxCombination(playSidePod);
                    }
                }
                else
                {
                    int win = room.bet / winners.Count;
                    foreach (User user in winners)
                    {
                        user.AddMoney(win);
                    }
                }

                db.UpdateRange(playersNotLeave);
                db.SaveChanges();
            }
            catch
            {
                // ignored
            }
        }
    }
}
