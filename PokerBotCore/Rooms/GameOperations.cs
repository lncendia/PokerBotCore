using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PokerBotCore.Model;
using PokerBotCore.Rooms.RoomTypes;

namespace PokerBotCore.Rooms
{
    static class GameOperations
    {
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
                        nominal.Add(CardsOperation.GetNominal(t));
                    }

                    CardsOperation.Sort(suit, nominal);
                    switch (combination)
                    {
                        case Combination.Comb.set:
                            CombinationChecker.CheckSet(nominal, user, ref cardsNotHand);
                            break;
                        case Combination.Comb.kare:
                            CombinationChecker.CheckKare(nominal, user, ref cardsNotHand);
                            break;
                        case Combination.Comb.nullCombination:
                            break;
                        default:
                            CombinationChecker.CheckPair(nominal, user, ref cardsNotHand);
                            break;
                    }

                    cards.AddRange(from x in openedCards
                        where !cardsNotHand.Contains(CardsOperation.GetNominal(x))
                        select CardsOperation.GetNominal(x));
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
                        let cardsUser = new List<sbyte> {CardsOperation.GetNominal(user.cards[0]), CardsOperation.GetNominal(user.cards[1])}
                        where cardsUser.Contains(kick)
                        select user);
                    if (users.Count > 0) break;
                }

                if (users.Count != 0) return users;
            }
            return winners;
        }
        public static async Task Payment(List<User> winners, List<User> playersNotLeave, Room room)
        {
            try
            {
                await using Db db = new Db();
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
                        if (j > 0) await room.SendMessage($"Вы забираете побочный банк №{j}.", winners, null);
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
                await db.SaveChangesAsync();
            }
            catch
            {
                // ignored
            }
        }
    }
}
