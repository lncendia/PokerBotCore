using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

namespace PokerBotCore
{
    class Room
    {
        protected static TelegramBotClient tgbot = new TelegramBotClient("1341769299:AAE4q84mx-NRrSJndKsCVNVLr-SzjYeN7wk");
        //protected List<string> cards = Operation.CreateCards();
        //"Восемь ♠", "Два ♥"
        protected List<string> cards = new List<string>()
            {"Король ♠", "Восемь ♥", "Король ♦", "Восемь ♣", "Девять ♠", "Два ♥", "Шесть ♦", "Пять ♣", "Четыре ♦"};//, "Два ♣", "Три ♥", "Пять ♣", "Восемь ♥" };//Operation.CreateCards();
        public List<string> openedCards = new List<string>();
        protected static readonly Random Rnd = new Random();
        public List<User> players;
        public List<User> foldUsers = new List<User>();
        public List<User> allInUsers = new List<User>();
        List<int> leavedPlayers = new List<int>();
        public int bet;
        public int countPlayers;
        public int id;
        public int key;
        public bool endgame;
        public bool next;
        public Room(User user, string firstName, int count, bool isPrivate)
        {
            for (int i = 0; i <= Bot.rooms.Count; i++)
            {
                if (i == Bot.rooms.Count)
                {
                    id = i;
                    break;
                }
                if (i == Bot.rooms[i].id) continue;
                id = i;
                break;
            }
            Bot.rooms.Add(this);
            if (isPrivate)
            {
                key = Rnd.Next(1000, 9999);
                try
                {
                    tgbot.SendTextMessageAsync(user.Id, $"Пароль комнаты: {key}");
                }
                catch { UserLeave(user); }
            }
            countPlayers = count;
            user.FirstName = firstName;
            Console.WriteLine($"Создана комната с id {id}");
            players = new List<User>() { user };
            Bot.roomsfortest++;
        }
        public Room(List<User> users, int count, int id)
        {
            foreach (User user in users)
            {
                user.room = this;
                user.state = User.State.wait;
            }
            players = users;
            countPlayers = count;
            this.id = id;
            //SendMessage($"Вы находитесь в комнате {id} [{players.Count}/{count_players}].", players, null);
        }
        protected static object lockAdd = new object();
        public void AddPlayer(User user, string firstName = "")
        {
            lock (lockAdd)
            {
                if (players.Count != 0 && players[0].state != User.State.wait) //если игра уже началась
                {
                    tgbot.SendTextMessageAsync(user.Id, $"Игра в этой комнате уже началась.");
                    return;
                }
                user.room = this;
                user.state = User.State.wait;
                try
                {
                    tgbot.SendTextMessageAsync(user.Id, $"Вы подключились к комнате {id} [{players.Count}/{countPlayers}].");
                }
                catch { UserLeave(user); return; }
                players.Add(user);
                user.FirstName = firstName;
                var key = new ReplyKeyboardMarkup(new KeyboardButton("Выход"));
                SendMessage($"{firstName} подключился(лась). [{players.Count}/{countPlayers}].", players, key);
                if (players.Count == countPlayers) SendCards();
            }
        }
        protected static object lockLeave = new object();
        public void UserLeave(User user)
        {
            lock (lockLeave)
            {
                players.Remove(user);
                foldUsers.Remove(user);
                allInUsers.Remove(user);
                user.combination = null;
                user.room = null;
                user.lastraise = 0;
                user.cards = null;
                leavedPlayers.Add(user.bet);
                SendMessage($"{user.FirstName} отключился(лась). [{players.Count}/{countPlayers}].", players, null, final: true);
                if (user.state == User.State.waitbet) next = true;
                if (!endgame)
                    if ((players.Count == 1 || players.Count - foldUsers.Count == 1) && (players[0].state == User.State.play || players[0].state == User.State.waitbet))
                    {
                        next = true;
                    }
                user.state = User.State.main;
                if (players.Count == 0)
                    Bot.rooms.Remove(this);
                try
                {
                    tgbot.SendTextMessageAsync(user.Id, $"Вы покинули комнату {id}.", replyMarkup: Bot.keyboard);
                }
                catch
                {
                    // ignored
                }
            }
        }

        public void SendMessage(string message, List<User> users, IReplyMarkup replyMarkup, bool neadLeave = true, bool final = false)
        {
            if (endgame && !final) return;
            foreach (User player in users.ToList())
            {
                if (player.Id == 0) continue;
                try
                {
                    tgbot.SendTextMessageAsync(player.Id, message, replyMarkup: replyMarkup);
                }
                catch { if (neadLeave) UserLeave(player); }
            }
        }

        protected virtual void SendCards()
        {
            try
            {
                endgame = true; //Для того, чтоб человек не мог выйти.
                foreach (User user1 in players.ToList())
                {
                    user1.state = User.State.play;
                    user1.lastraise = 0;
                    user1.combination = null;
                    user1.bet = 0;
                    user1.cards = new List<string>();
                    int card = 0;//Rnd.Next(0, cards.Count);
                    user1.cards.Add(cards[card]);
                    cards.Remove(cards[card]);
                    //card = Rnd.Next(0, cards.Count);
                    user1.cards.Add(cards[card]);
                    cards.Remove(cards[card]);

                    string combination = Operation.CheckCombination(user1.cards, user1);
                    var keyboard = new InlineKeyboardMarkup(new List<List<InlineKeyboardButton>>() { new List<InlineKeyboardButton>() { InlineKeyboardButton.WithCallbackData(user1.cards[0]), InlineKeyboardButton.WithCallbackData(user1.cards[1]) }, new List<InlineKeyboardButton>() { InlineKeyboardButton.WithCallbackData(combination) } });
                    SendMessage("Игра началась! \nВаши карты: ", new List<User>() { user1 }, keyboard, final: true);
                    //players[0].bet = 50;
                    //players[1].bet = 100;
                    //players[2].bet = 100;
                    //players[3].bet = 150;
                    ////players[4].bet = 200;
                    //allInUsers.Add(players[0]);
                    //allInUsers.Add(players[1]);
                    //allInUsers.Add(players[2]);
                    //allInUsers.Add(players[3]);
                    ////allInUsers.Add(players[4]);
                    //bet += 400;

                }
                players[0].lastraise = 10;
                players[0].Money -= 10;
                bet += 10;
                players[0].bet += 10;
                SendMessage("Блайнд - 10 коинов.", new List<User>() { players[0] }, null, true, final: true);
                players[1].lastraise = 25;
                players[1].Money -= 25;
                bet += 25;
                players[1].bet += 25;
                SendMessage("Блайнд - 25 коинов.", new List<User>() { players[1] }, null, final: true);
                if (countPlayers != 2)
                {
                    var player1 = players[0];
                    var player2 = players[1];
                    var players1 = players.ToList();
                    players1.Remove(player1);
                    players1.Remove(player2);
                    players1.Add(player1);
                    players1.Add(player2);
                    players = players1;
                }
                endgame = false;
                lastraise = 25;
                GameNext();
            }
            catch (Exception ex)
            {
                ExceptionInGame(ex);
            }
        }

        protected readonly InlineKeyboardMarkup keyboard = new InlineKeyboardMarkup(new List<InlineKeyboardButton>() { InlineKeyboardButton.WithCallbackData("Fold", "Fold"), InlineKeyboardButton.WithCallbackData("Check", "Check"), InlineKeyboardButton.WithCallbackData("Raise", "Raise") });
        public int lastraise;
        protected virtual async void GameNext()
        {
            try
            {
                if (openedCards.Count == 0)
                    OpenCards(3);
                else OpenCards();
                return;
                for (int i = 0; i < players.Count; i++)
                {
                    if (!endgame)
                        if (players.Count == 1 || players.Count - foldUsers.Count == 1)
                        {
                            endgame = true;
                            SetWinner();
                            return;
                        }
                    User player = players[i];
                    if (player.lastraise == lastraise && lastraise != 0) continue;
                    if (!players.Contains(player)) continue;
                    if (foldUsers.Contains(player)) continue;
                    if (allInUsers.Contains(player)) continue;
                    if (!await DoBet(player)) i--;
                }
                if (players.Count == 1 || players.Count - foldUsers.Count == 1)
                {
                    endgame = true;
                    SetWinner();
                    return;
                }
                else if (openedCards.Count == 0 && lastraise == 25)
                {
                    if (countPlayers > 2) await DoBet(players[players.Count - 1]);
                    else await DoBet(players[1]);
                }
                if (!Operation.CheckRaise(players, lastraise))
                {
                    GameNext();
                    return;
                }
                lastraise = 0;
                if (openedCards.Count == 0)
                    OpenCards(3);
                else OpenCards();
            }
            catch (Exception ex)
            {
                ExceptionInGame(ex);
            }
        }
        protected async Task<bool> DoBet(User player)
        {
            try
            {
                next = false;
                if (lastraise == 0 || lastraise - player.lastraise == 0)
                    SendMessage("Ваш ход. У вас есть 60 секунд.", new List<User>() { player }, keyboard, final: true);
                else
                {
                    InlineKeyboardMarkup keyboard2 = new InlineKeyboardMarkup(new List<InlineKeyboardButton>() { InlineKeyboardButton.WithCallbackData("Fold", "Fold"), InlineKeyboardButton.WithCallbackData($"Call {lastraise - player.lastraise}", "Call"), InlineKeyboardButton.WithCallbackData("Raise", "Raise") });
                    SendMessage("Ваш ход. У вас есть 60 секунд.", new List<User>() { player }, keyboard2);
                }
                player.state = User.State.waitbet;
                int j = 0;
                do
                {
                    j++;
                    await Task.Delay(1000);
                    if (j == 60)
                    {
                        if (!players.Contains(player)) break;
                        UserLeave(player);
                        break;
                    }
                }
                while (next == false);
                if (!players.Contains(player))
                {
                    return false;
                }
                player.state = User.State.play;
                return true;
            }
            catch (Exception ex) { ExceptionInGame(ex); return true; }
        }

        protected void ExceptionInGame(Exception ex, bool isfinal = false)
        {
            Bot.reviews.Enqueue($"0:Эксепшн: {ex.Message}\nОбъект, вызвавший исключение: {ex.Source}\nМетод, вызвавший исключение: {ex.TargetSite}");
            if (endgame && !isfinal) return;
            endgame = true;
            Operation.EmergencySaveMoney(players, this);
            SendMessage($"Произошла ошибка. Приносим свои извинения, средства были возвращены!", players, Bot.keyboard, false);
            foreach (User user in players.ToList())
            {
                UserLeave(user);
            }
        }

        protected async void OpenCards(int count = 1)
        {
            try
            {
                if (!endgame)
                    if ((players.Count == 1 || players.Count - foldUsers.Count == 1))
                    {
                        endgame = true;
                        SetWinner();
                        return;
                    }
                if (endgame) return; //Если после определения победителя открылся метод.
                if (openedCards.Count == 5)
                {
                    SetWinner();
                }
                else
                {
                    if (count == 3 && players.Count > 2)
                    {
                        for (int j = 0; j < 2; j++)
                        {
                            var player1 = players[players.Count - 1];
                            for (int i = players.Count - 1; i > 0; i--)
                            {
                                players[i] = players[i - 1];
                            }
                            players[0] = player1;
                        }
                    }
                    for (int i = 0; i < count; i++)
                    {
                        int card = Rnd.Next(0, cards.Count);
                        openedCards.Add(cards[card]);
                        cards.Remove(cards[card]);
                    }
                    foreach (User user1 in players.ToList())
                    {
                        if (foldUsers.Contains(user1)) SendMessage("Вы сбросили карты.", new List<User>() { user1 }, null);
                        if (allInUsers.Contains(user1)) SendMessage("Вы пошли ва-банк.", new List<User>() { user1 }, null);
                        await SendTable(user1);
                    }
                    if (players.Count - foldUsers.Count - allInUsers.Count == 1 && allInUsers.Count != 0)
                    {
                        OpenCards();
                        return;
                    }
                    foreach (User user in players.ToList()) user.lastraise = 0;
                    GameNext();
                }
            }
            catch (Exception ex)
            {
                ExceptionInGame(ex);
            }

        }

        protected virtual async Task SendTable(User user1)
        {
            try
            {
                if (endgame) return;
                var x = openedCards.ToList();
                x.Add(user1.cards[0]);
                x.Add(user1.cards[1]);
                var combination = Operation.CheckCombination(x, user1);
                var keyboard = new InlineKeyboardMarkup(new List<List<InlineKeyboardButton>>() { new List<InlineKeyboardButton>() { InlineKeyboardButton.WithCallbackData(user1.cards[0]), InlineKeyboardButton.WithCallbackData(user1.cards[1]) }, new List<InlineKeyboardButton>() { InlineKeyboardButton.WithCallbackData(combination) } });
                var path = Operation.GetImage(x, user1);
                using (var ms = new MemoryStream())
                {
                    path.Save(ms, ImageFormat.Jpeg);
                    path.Dispose();
                    ms.Position = 0;
                    try
                    {
                        await tgbot.SendPhotoAsync(user1.Id, new InputOnlineFile(ms), replyMarkup: keyboard);
                    }
                    catch { UserLeave(user1); }
                }
            }
            catch (Exception ex)
            {
                Bot.reviews.Enqueue($"{user1.Id}:Эксепшн: {ex.Message}\nОбъект, вызвавший исключение: {ex.Source}\nМетод, вызвавший исключение: {ex.TargetSite}");
                UserLeave(user1);
            }
        }

        protected async void SetWinner()
        {
            try
            {
                endgame = true;
                List<User> winners;
                if (players.Count == 1) winners = players;
                else winners = Operation.MaxCombination(players);
                var playersNotLeave = players.ToList();
                string allcards = "";
                foreach (User user in playersNotLeave)
                {
                    allcards += $"Игрок {user.FirstName}(<a href =\"https://telegram.me/PokerGame777_bot?start=info_{user.Id}\">+</a>): { user.cards[0]}, {user.cards[1]}";
                    if (foldUsers.Contains(user)) allcards += " - Fold\n";
                    else allcards += "\n";
                }
                foreach (var player in players.ToList().Where(player => player.Id != 0))
                {
                    try
                    {
                        await tgbot.SendTextMessageAsync(player.Id, allcards, Telegram.Bot.Types.Enums.ParseMode.Html);
                    }
                    catch
                    {
                        // ignored
                    }
                }
                if (winners.Count > 1)
                {
                    string winnersFirstName = "";
                    foreach (User user in winners)
                    {
                        winnersFirstName += user.FirstName + ", ";
                    }
                    Payment(winners, playersNotLeave);
                    SendMessage($"Игра окончена: {winnersFirstName.Remove(winnersFirstName.Length - 2)} разделили выигрыш между собой с комбинацией {winners[0].combination.ToString(winners[0])}", players, null, final: true);
                }
                else if (players.Count > 0)
                {
                    Payment(winners, playersNotLeave);
                    SendMessage($"Игра окончена: Победил {winners[0].FirstName} с комбинацией {winners[0].combination.ToString(winners[0])}", players, null, final: true);
                }
                await CreateNewRoom(playersNotLeave);
            }
            catch (Exception ex)
            {
                ExceptionInGame(ex, true);
            }
        }

        protected void Payment(List<User> winners, List<User> playersNotLeave)
        {
            try
            {
                using DB db = new DB();
                if (allInUsers.Count != 0)
                {
                    List<User> playersnotfold = playersNotLeave.ToList();
                    foreach (User user in foldUsers) playersnotfold.Remove(user);
                    var playSidePod = playersnotfold.ToList();
                    int j = 0;
                    while (bet > 0)
                    {
                        int mainbank = 0;
                        int min = playSidePod[0].bet;
                        min = playSidePod.Select(user => user.bet).Prepend(min).Min();
                        foreach (User user in playersNotLeave)
                        {
                            if (user.bet < min)
                            {
                                mainbank += user.bet;
                                user.bet = 0;
                            }
                            else
                            {
                                mainbank += min;
                                user.bet -= min;
                            }
                            if (user.bet == 0) playSidePod.Remove(user);
                        }
                        for (int i = 0; i < leavedPlayers.Count; i++)
                        {
                            if (leavedPlayers[i] < min)
                            {
                                mainbank += leavedPlayers[0];
                                leavedPlayers[0] = 0;
                            }
                            else
                            {
                                mainbank += min;
                                leavedPlayers[i] -= min;
                            }
                        }
                        bet -= mainbank;
                        int win = mainbank / winners.Count;
                        if (j > 0) SendMessage($"Вы забираете побочный банк №{j}.", winners, null, final: true);
                        foreach (User user in winners)
                        {
                            if (playSidePod.Count != playersNotLeave.Count)
                                user.AddMoney(win);
                        }
                        j++;
                        winners = Operation.MaxCombination(playSidePod);
                    }
                }
                else
                {
                    int win = bet / winners.Count;
                    foreach (User user in winners)
                    {
                        user.AddMoney(win);
                    }
                }
                db.UpdateRange(playersNotLeave);
                db.SaveChanges();
            }
            catch { }
        }
        protected virtual async Task CreateNewRoom(List<User> playersNotLeave)
        {
            try
            {
                foreach (User user in playersNotLeave)
                {
                    user.state = User.State.wait;
                    if (user.Money < 100 && user.Id != 0)
                    {
                        try
                        {
                            await tgbot.SendTextMessageAsync(user.Id, "Недостаточно средств. Счет должен быть больше 100 коинов.", replyMarkup: Bot.keyboard);
                            UserLeave(user);
                        }
                        catch { UserLeave(user); }
                    }
                }
                var players1 = players.ToList();
                bet = 0;
                cards = Operation.CreateCards();
                openedCards.Clear();
                var x = players1[0];
                players1.Remove(x);
                players1.Add(x);
                allInUsers.Clear();
                foldUsers.Clear();
                players = players1;
                endgame = false;
                SendMessage($"Вы находитесь в комнате {id} [{players.Count}/{countPlayers}].", players, null);
                if (players.Count != countPlayers) return;
                SendMessage($"Новая игра начнеться через 10 секунд.", players, null);
                await Task.Delay(10000);
                if (players.Count != countPlayers)
                {
                    SendMessage($"Недостаточно игроков для начала игры. Ожидание...", players, null);
                    return;
                }
                SendCards();
            }
            catch
            {
                endgame = true;
                foreach (User user in players.ToList())
                {
                    UserLeave(user);
                }
            }
        }

        ~Room()
        {
            Bot.roomsfortest--;
            Console.WriteLine($"Комната {id} уничтожена!");
        }
    }
}
