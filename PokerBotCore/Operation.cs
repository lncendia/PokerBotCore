using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using RestSharp;
using Newtonsoft.Json.Linq;
using Qiwi.BillPayments.Client;
using Qiwi.BillPayments.Model.In;
using Qiwi.BillPayments.Model;
using System.Threading.Tasks;
using System.IO;

namespace PokerBot
{
    static class Operation
    {
        public static void AddUser(long id, User Referal)
        {
            Bot.users.Add(new User() { Id = id, Money = 0, Referal = Referal });
        }
        public static async void SaveDB()
        {
            while (true)
            {
                await Task.Delay(30000);
                GC.Collect();
                if (Bot.db.ChangeTracker.HasChanges())
                {
                    Bot.db.SaveChanges();
                    Console.WriteLine("База данных сохранена.");
                }
            }
        }
        public static DateTime time;
        public static async void Mute()
        {
            try
            {
                while (true)
                {
                    time = DateTime.Now.AddMinutes(3);
                    await Task.Delay(180000);
                    foreach (User user in Bot.users)
                    {
                        user.count_messages = 0;
                    }

                }
            }
            catch
            {
                Mute();
                return;
            }
        }
        private static readonly Random rnd = new Random();
        static readonly BillPaymentsClient client = BillPaymentsClientFactory.Create(secretKey: "eyJ2ZXJzaW9uIjoiUDJQIiwiZGF0YSI6eyJwYXlpbl9tZXJjaGFudF9zaXRlX3VpZCI6InBndDY4Ni0wMCIsInVzZXJfaWQiOiIzODA2NjYzMjA3OTAiLCJzZWNyZXQiOiIzZGE4MDliMTYzMjBkYjJhNGI0NmRhMTg0NjJmYTE5ODcyMjNhZGE1MDExZDI4NDI5ZTM5M2YxOTE1Zjg1MzhmIn19");
        public static string AddTransaction(int sum, User user, ref string bill_id)
        {
            try
            {
                var response = client.CreateBill(
                info: new CreateBillInfo
                {
                    BillId = Guid.NewGuid().ToString(),
                    Amount = new MoneyAmount
                    {
                        ValueDecimal = sum,
                        CurrencyEnum = CurrencyEnum.Rub
                    },
                    ExpirationDateTime = DateTime.Now.AddDays(5),
                    Customer = new Customer
                    {
                        Account = user.Id.ToString()
                    }
                });
                bill_id = response.BillId;
                return response.PayUrl.ToString();
            }
            catch { return null; }
        }
        public static bool CheckPay(User user, string bill_id)
        {
            try
            {
                var response = client.GetBillInfo(bill_id);
                if (response.Status.ValueEnum == BillStatusEnum.Paid)
                {
                    user.AddMoney((int)response.Amount.ValueDecimal);
                    if (user.Referal != null) user.Referal.AddMoney((int)(response.Amount.ValueDecimal * (decimal)0.07));
                    return true;
                }
                return false;
            }
            catch { return false; }
        }
        public static bool OutputMoney(string number, User user)
        {
            try
            {
                int Money = (int)(user.output * 0.85);
                var client = new RestClient("https://edge.qiwi.com/sinap/api/v2/terms/99/payments");
                var request = new RestRequest(Method.POST);
                request.AddHeader("Content-Type", "application/json");
                request.AddHeader("Accept", "application/json");
                request.AddHeader("Authorization", "Bearer 2834c05f3b6d08b019d5c6644e98bb4b");
                DateTime date = DateTime.Now;
                uint unixTime = (uint)(date.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
                request.AddParameter("application/json", $"{{ \n        \"id\":\"{unixTime * 1000}\", \n        \"sum\": {{ \n          \"amount\":{Money}, \n          \"currency\":\"643\" \n        }}, \n        \"paymentMethod\": {{ \n          \"type\":\"Account\", \n          \"accountId\":\"643\" \n        }},\n        \"comment\":\"Выплата с PokerBot {DateTime.Now:dd.MMM.yyyy}\", \n        \"fields\": {{ \n          \"account\":\"{number}\" \n        }} \n      }}", ParameterType.RequestBody);
                IRestResponse response = client.Execute(request);
                dynamic jobject = JObject.Parse(response.Content);
                if (jobject.transaction.state.code.ToString() == "Accepted")
                {
                    Bot.db.Transactions.Add(new Transaction() { User = user, Money = user.output, Number = number, Date = DateTime.Now.ToString("dd.MMM.yyyy") });
                    user.Money -= user.output;
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
        public static void EmergencySaveMoney(List<User> players, Room room)
        {
            room.bet = 0;
            foreach (User user in players.ToList())
            {
                user.state = User.State.main;
                user.Money += user.bet;
            }
            return;
        }
        public static User GetUser(long id)
        {
            return Bot.users.Find(id);
        }
        public static Room GetRoom(int id)
        {
            for (int i = 0; i < Bot.rooms.Count; i++)
            {
                Room room = Bot.rooms[i];
                if (id == room.id) return room;
            }
            return null;
        }
        public static List<string> CreateCards()
        {
            List<string> nominal = new List<string>() { "Два", "Три", "Четыре", "Пять", "Шесть", "Семь", "Восемь", "Девять", "Десять", "Валет", "Дама", "Король", "Туз" };
            List<string> deck = new List<string>() { "♣", "♠", "♥", "♦" };
            List<string> cards = new List<string>();
            foreach (string str in nominal)
            {
                foreach (string str_deck in deck)
                {
                    cards.Add($"{str} {str_deck}");
                }
            }
            return cards;

        }
        #region CheckCombination
        public static sbyte GetNominal(string card)
        {
            switch (card.Remove(card.Length - 2))
            {
                case "Два": return 2;
                case "Три": return 3;
                case "Четыре": return 4;
                case "Пять": return 5;
                case "Шесть": return 6;
                case "Семь": return 7;
                case "Восемь": return 8;
                case "Девять": return 9;
                case "Десять": return 10;
                case "Валет": return 11;
                case "Дама": return 12;
                case "Король": return 13;
                case "Туз": return 14;
            }
            return 0;
        }
        static void Sort(List<char> cd, List<sbyte> keys)
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
        static bool CheckStraightFlush(List<char> su, List<sbyte> nom, User user, ref string name)
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
                List<sbyte> straightFlush = new List<sbyte>() { nominal[i] };
                int count = 0;
                for (int j = i + 1; j < nominal.Count; j++)
                {
                    if (straightFlush[count] + 1 == nominal[j] && suit[i] == suit[j])
                    {
                        straightFlush.Add(nominal[j]);
                        count++;
                    }
                }
                if (count >= 4)
                {
                    first = straightFlush[0];
                }
            }
            if (first == 0) return false;
            if (first == 10)
            {
                user.combination = new Combination((sbyte)first, Combination.Comb.royalflush);
                name = "Роялфлеш";
                return true;
            }
            else
            {
                user.combination = new Combination((sbyte)first, Combination.Comb.straightflush);
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
                if (same == 4)
                {
                    cards = new List<sbyte> { nominal[i] };
                    user.combination = new Combination(nominal[i], Combination.Comb.kare);
                    name = "Каре";
                    return true;
                }
            }
            return false;
        }
        static bool CheckFullHouse(List<sbyte> nominal, User user, ref string name)
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
            sbyte max_set = sets.Max();
            List<sbyte> nominals = new List<sbyte>();
            for (int i = 0; i < nominal.Count - 1; i++)
            {
                if (nominal[i] == max_set) continue;
                int same = 1;
                for (int j = i + 1; j < nominal.Count; j++)
                {
                    if (nominal[i] == nominal[j]) same++;
                }
                if (same == 2) nominals.Add(nominal[i]);
            }
            if (nominals.Count == 0) return false;
            user.combination = new Combination(max_set, Combination.Comb.fullhouse, nominals.Max());
            name = "Фулл хаус";
            return true;
        }
        static bool CheckFlush(List<char> suit, List<sbyte> nominal, User user, ref string name)
        {
            int first = 0;
            for (int i = 0; i < nominal.Count - 4; i++)
            {
                List<sbyte> flush = new List<sbyte>() { nominal[i] };
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
            user.combination = new Combination((sbyte)first, Combination.Comb.flush);
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
                var straight = new List<sbyte>() { nominal[i] };
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
            user.combination = new Combination((sbyte)first, Combination.Comb.straight);
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
            if (nominals.Count > 0)
            {
                cards = new List<sbyte>() { nominals.Max() };
                user.combination = new Combination(nominals.Max(), Combination.Comb.set);
                name = "Сет";
                return true;
            }
            return false;
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
            if (nominals.Count > 0)
            {
                if (nominals.Count > 1)
                {
                    var x1 = nominals.Max();
                    nominals.Remove(nominals.Max());
                    var x2 = nominals.Max();
                    cards = new List<sbyte>() { x1, x2 };
                    user.combination = new Combination(x1, Combination.Comb.twopair, x2);
                    name = "Две пары";
                    return true;
                }
                else if (nominals.Count == 1)
                {
                    cards = new List<sbyte>() { nominals.Max() };
                    user.combination = new Combination(nominals[0], Combination.Comb.pair);
                    name = "Пара";
                    return true;
                }
            }
            return false;
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
            string suit_str = card.Substring(card.Length - 1), suit = "";
            switch (suit_str)
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
            if (File.Exists($"tables\\{user.Id}.jpg")) table = Image.FromFile($"tables\\{user.Id}.jpg");
            else
                table = Image.FromFile("tables\\table.jpg");
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
            g.DrawImage(images[images.Count - 2], 520, 545);
            g.DrawImage(images[images.Count - 1], 820, 545);
            return table;
        }
        public static string CheckCombination(List<string> cardm, User user)
        {
            var suit = new List<char>();
            var nominal = new List<sbyte>();
            for (int i = 0; i < cardm.Count; i++)
            {
                suit.Add(cardm[i][cardm[i].Length - 1]);
                nominal.Add(GetNominal(cardm[i]));
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
        public static bool CheckRaise(List<User> users, int lastrise)
        {
            bool allUsersCall = true;
            List<User> playingUsers = new List<User>();
            List<User> folded = users[0].room.foldUsers;
            List<User> allIn = users[0].room.allInUsers;
            for (int i = 0; i < users.Count; i++)
            {
                if (folded.Contains(users[i])) continue;
                if (allIn.Contains(users[i])) continue;
                playingUsers.Add(users[i]);
            }
            for (int i = 0; i < playingUsers.Count; i++)
            {
                if (playingUsers[i].lastraise != lastrise)
                {
                    allUsersCall = false;
                    break;
                }
            }
            return allUsersCall;
        }

        public static List<User> MaxCombination(List<User> us)
        {
            var users = us.ToList();
            foreach (User user in users.ToList())
            {
                if (user.combination == null || user.room.foldUsers.Contains(user)) users.Remove(user);
            }
            List<User> winners = new List<User>();

            Combination.Comb combination = users[0].combination.combination;
            foreach (User user in users)
            {
                if (user.combination.combination > combination) combination = user.combination.combination;
            }
            foreach (User user in users)
            {
                if (user.combination.combination == combination) winners.Add(user);
            }
            sbyte max = winners[0].combination.nominal;
            foreach (User user in winners)
            {
                if (user.combination.nominal > max) max = user.combination.nominal;
            }
            foreach (User user in winners.ToList())
            {
                if (user.combination.nominal != max) winners.Remove(user);
            }
            if (winners.Count == 1) return winners;
            if (winners[0].combination.combination == Combination.Comb.twopair || winners[0].combination.combination == Combination.Comb.fullhouse)
            {
                max = winners[0].combination.nominal2;
                foreach (User user in winners)
                {
                    if (user.combination.nominal2 > max) max = user.combination.nominal2;
                }
                foreach (User user in winners.ToList())
                {
                    if (user.combination.nominal2 != max) winners.Remove(user);
                }
            }
            if (winners.Count == 1) return winners;
            List<sbyte> cards = new List<sbyte>();
            if (winners[0].combination.combination == Combination.Comb.pair || winners[0].combination.combination == Combination.Comb.twopair || winners[0].combination.combination == Combination.Comb.set || winners[0].combination.combination == Combination.Comb.kare || winners[0].combination.combination == Combination.Comb.nullcomb)
            {
                string name = "";
                var cards_nothand = new List<sbyte>();
                foreach (User user in winners)
                {
                    var openedCards = user.room.openedCards.ToList();
                    openedCards.AddRange(user.cards);
                    var suit = new List<char>();
                    var nominal = new List<sbyte>();
                    for (int i = 0; i < openedCards.Count; i++)
                    {
                        suit.Add(openedCards[i][openedCards[i].Length - 1]);
                        nominal.Add(GetNominal(openedCards[i]));
                    }
                    Sort(suit, nominal);
                    switch (combination)
                    {
                        case Combination.Comb.set:
                            CheckSet(nominal, user, ref name, ref cards_nothand);
                            break;
                        case Combination.Comb.kare:
                            CheckKare(nominal, user, ref name, ref cards_nothand);
                            break;
                        case Combination.Comb.nullcomb:
                            break;
                        default:
                            CheckPair(nominal, user, ref name, ref cards_nothand);
                            break;
                    }
                    foreach (var x in cards_nothand)
                    {
                        Console.WriteLine(x);
                    }
                    foreach (var x in openedCards)
                    {
                        if (cards_nothand.Contains(GetNominal(x))) continue;
                        cards.Add(GetNominal(x));
                    }
                }
                cards = cards.Distinct().ToList();
                List<sbyte> kickers = new List<sbyte>();
                switch (combination)
                {
                    case Combination.Comb.nullcomb:
                        kickers.Add(cards.Max());
                        cards.Remove(kickers[0]);
                        kickers.Add(cards.Max());
                        cards.Remove(kickers[1]);
                        kickers.Add(cards.Max());
                        cards.Remove(kickers[2]);
                        kickers.Add(cards.Max());
                        cards.Remove(kickers[3]);
                        kickers.Add(cards.Max());
                        break;
                    case Combination.Comb.pair:
                        kickers.Add(cards.Max());
                        cards.Remove(kickers[0]);
                        kickers.Add(cards.Max());
                        cards.Remove(kickers[1]);
                        kickers.Add(cards.Max());
                        cards.Remove(kickers[2]);
                        break;
                    case Combination.Comb.set:
                        kickers.Add(cards.Max());
                        cards.Remove(kickers[0]);
                        kickers.Add(cards.Max());
                        cards.Remove(kickers[1]);
                        break;
                    default:
                        kickers.Add(cards.Max());
                        break;
                }
                users.Clear();
                foreach (sbyte kick in kickers)
                {
                    foreach (User user in winners)
                    {
                        List<sbyte> cardsUser = new List<sbyte>
                        {
                             GetNominal(user.cards[0]),
                             GetNominal(user.cards[1])
                        };
                        if (cardsUser.Contains(kick)) users.Add(user);
                    }
                }
                if (users.Count != 0) return users;
            }
            return winners;
        }
        public static FakeRoom CreateFakeRoom(int count)
        {
            User user = new User();
            user.Id = 0;
            user.Money = int.MaxValue;
            user.state = User.State.wait;
            Room room = new FakeRoom(user, CreateNickname(), count);
            user.room = room;
            return (FakeRoom)room;
        }
        static string a = "bcdfghjklmnpqrstvwxz";
        static string b = "aeiouy";
        static string emoji = "";
        static List<string> c = new List<string>() { a, b };
        static List<string> nameRus = new List<string> { "Александр", "Алексей", "Анатолий", "Андрей", "Антон", "Аркадий", "Арсений", "Артём", "Артур", "Борис", "Вадим", "Валентин", "Валерий", "Василий", "Виктор", "Виталий", "Владимир", "Владислав", "Вячеслав", "Глеб", "Даниил", "Денис", "Дмитрий", "Евгений", "Егор", "Иван", "Игорь", "Илья", "Кирилл", "Константин", "Максим", "Марк", "Матвей", "Михаил", "Никита", "Олег", "Павел", "Пётр", "Роман", "Руслан", "Сергей", "Степан", "Тимур", "Юрец", "Ярик" };
        static List<string> nameEng = new List<string> { "Aleksandr", "Aleksey", "Anatoliy", "Andrey", "Anton", "Arkadiy", "Arseniy", "Artyom", "Artur", "Boris", "Vadim", "Valentin", "Valeriy", "Vasiliy", "Viktor", "Vitaliy", "Vladimir", "Vladislav", "Vyacheslav", "Gleb", "Daniil", "Denis", "Dmitriy", "Evgeniy", "Egor", "Ivan", "Igor", "Il'ya", "Kirill", "Konstantin", "Maksim", "Mark", "Matvey", "Mihail", "Nikita", "Oleg", "Pavel", "Pyotr", "Roma", "Ruslan", "Sergey", "Stepan", "Timur", "Yura", "Yarik" };
        public static string CreateNickname()
        {
            string pass = "";
            switch (rnd.Next(0, 3))
            {
                case 0:
                    int r = rnd.Next(0, 2);
                    pass += char.ToUpper(c[r][rnd.Next(0, c[r].Length)]);
                    int count = rnd.Next(3, 6);
                    for (int i = r + 1; i < count + r + 1; i++)
                    {
                        pass += c[i % 2][rnd.Next(0, c[i % 2].Length)];
                    }
                    break;
                case 1:
                    pass = nameRus[rnd.Next(0, nameRus.Count)];
                    break;
                case 2:
                    pass = nameEng[rnd.Next(0, nameEng.Count)];
                    break;
            }
            pass += emoji;
            if (rnd.Next(0, 3) == 2) pass += emoji[rnd.Next(0, emoji.Length)];
            return pass;
        }
    }
}
