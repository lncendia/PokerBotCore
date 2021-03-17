using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PokerBotCore.Bot;
using PokerBotCore.Enums;
using PokerBotCore.Keyboards;
using PokerBotCore.Model;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

namespace PokerBotCore.Rooms.RoomTypes
{
    public class Room
    {
        protected static readonly TelegramBotClient TgBot = BotSettings.Get();

        protected List<string> cards = GameOperations.CreateCards();

        // protected List<string> cards = new List<string>()
        // {"Король ♠", "Король ♥", "Король ♦", "Восемь ♣", "Король ♣", "Восемь ♥", "Шесть ♦", "Пять ♣", "Восемь ♦", "Два ♣", "Три ♥", "Пять ♣", "Туз ♥" };
        public readonly List<string> openedCards = new();
        protected static readonly Random Rnd = new();
        public List<User> players;
        public readonly List<User> foldUsers = new();
        public readonly List<User> allInUsers = new();
        public readonly List<int> leavedPlayers = new();
        public int bet;
        public readonly int countPlayers;
        public readonly int id;
        public int lastRaise;
        public readonly int key;
        public bool block;
        public bool started;
        public bool next;

        public Room(User user, int count, bool isProtected)
        {
            for (int i = 0; i <= BotSettings.rooms.Count; i++)
            {
                if (i == BotSettings.rooms.Count)
                {
                    id = i;
                    break;
                }

                if (i == BotSettings.rooms[i].id) continue;
                id = i;
                break;
            }

            if (isProtected)
            {
                key = Rnd.Next(1000, 9999);
                try
                {
                    TgBot.SendTextMessageAsync(user.Id, $"Пароль комнаты: {key}");
                }
                catch
                {
                    RemovePlayer(user);
                }
            }

            countPlayers = count;
            Console.WriteLine($"Создана комната с id {id}");
            players = new List<User>() {user};
        }

        public Room(List<User> users, int count, int id)
        {
            foreach (User user in users)
            {
                user.room = this;
                user.state = State.wait;
            }

            players = users;
            countPlayers = count;
            this.id = id;
            //SendMessage($"Вы находитесь в комнате {id} [{players.Count}/{count_players}].", players, null);
        }

        private readonly object _lock = new();

        public void AddPlayer(User user, string firstName = "")
        {
            lock (_lock)
            {
                if (started) //если игра уже началась
                {
                    TgBot.SendTextMessageAsync(user.Id, $"Игра в этой комнате уже началась.");
                    return;
                }

                user.room = this;
                user.state = State.wait;
                try
                {
                    TgBot.SendTextMessageAsync(user.Id,
                        $"Вы подключились к комнате {id} [{players.Count}/{countPlayers}].");
                }
                catch
                {
                    RemovePlayer(user);
                    return;
                }

                players.Add(user);
                user.firstName = firstName;
                SendMessage($"{firstName} подключился(лась). [{players.Count}/{countPlayers}].", players,
                    GameKeyboards.Exit).Wait();
                if (players.Count != countPlayers) return;
                started = true;
                SendCards().Wait();
            }
        }

        public void RemovePlayer(User user)
        {
            lock (_lock)
            {
                players.Remove(user);
                foldUsers.Remove(user);
                allInUsers.Remove(user);
                user.combination = null;
                user.lastRaise = 0;
                user.cards = null;
                leavedPlayers.Add(user.bet);
                SendMessage($"{user.firstName} отключился(лась). [{players.Count}/{countPlayers}].", players, null)
                    .Wait();
                if (user.state == State.waitBet || players.Count - foldUsers.Count - allInUsers.Count == 1)
                    next = true;
                user.state = State.main;
                user.room = null;
                if (players.Count == 0)
                    BotSettings.rooms.Remove(this);
                try
                {
                    TgBot.SendTextMessageAsync(user.Id, $"Вы покинули комнату {id}.",
                        replyMarkup: MainKeyboards.MainKeyboard);
                }
                catch
                {
                    // ignored
                }
            }
        }

        public virtual async Task SendMessage(string message, IEnumerable<User> users, IReplyMarkup replyMarkup,
            ParseMode parseMode = ParseMode.Default, bool needLeave = true)
        {
            foreach (var player in users.ToList())
            {
                try
                {
                    await TgBot.SendTextMessageAsync(player.Id, message, replyMarkup: replyMarkup,
                        parseMode: parseMode);
                }
                catch
                {
                    if (needLeave) RemovePlayer(player);
                }
            }
        }

        protected virtual async Task SendCards()
        {
            try
            {
                block = true; //Для того, чтоб человек не мог выйти.
                foreach (User user1 in players)
                {
                    user1.state = State.play;
                    user1.lastRaise = 0;
                    user1.combination = null;
                    user1.bet = 0;
                    user1.cards = new List<string>();
                    int card = Rnd.Next(0, cards.Count);
                    user1.cards.Add(cards[card]);
                    cards.Remove(cards[card]);
                    card = Rnd.Next(0, cards.Count);
                    user1.cards.Add(cards[card]);
                    cards.Remove(cards[card]);

                    CombinationChecker.CheckCombination(user1.cards, user1);
                    await SendMessage("Игра началась! \nВаши карты: ", new List<User>() {user1},
                        GameKeyboards.CombinationKeyboard(user1));

                }

                players[0].lastRaise = 10;
                players[0].Money -= 10;
                bet += 10;
                players[0].bet += 10;
                await SendMessage("Блайнд - 10 коинов.", new List<User>() {players[0]}, null);
                players[1].lastRaise = 25;
                players[1].Money -= 25;
                bet += 25;
                players[1].bet += 25;
                await SendMessage("Блайнд - 25 коинов.", new List<User>() {players[1]}, null);
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

                block = false;
                lastRaise = 25;
                GameNext();
            }
            catch (Exception ex)
            {
                await ExceptionHandler.ExceptionInGame(ex, this);
            }
        }


        protected virtual async void GameNext()
        {
            try
            {
                for (int i = 0; i < players.Count; i++)
                {
                    if (players.Count - foldUsers.Count - allInUsers.Count == 1) continue;
                    User player = players[i];
                    if (player.lastRaise == lastRaise && lastRaise != 0) continue;
                    if (!players.Contains(player)) continue;
                    if (foldUsers.Contains(player)) continue;
                    if (allInUsers.Contains(player)) continue;
                    if (!await DoBet(player)) i--;
                }

                if (openedCards.Count == 0 && lastRaise == 25)
                {
                    if (players.Count - foldUsers.Count - allInUsers.Count != 1)
                    {
                        if (countPlayers > 2) await DoBet(players[^1]);
                        else await DoBet(players[1]);
                    }
                }

                if (!GameOperations.CheckRaise(players, lastRaise) &&
                    players.Count - foldUsers.Count - allInUsers.Count != 1)
                {
                    GameNext();
                    return;
                }

                lastRaise = 0;
                if (openedCards.Count == 0)
                    OpenCards(3);
                else OpenCards();
            }
            catch (Exception ex)
            {
                await ExceptionHandler.ExceptionInGame(ex, this);
            }
        }

        protected async Task<bool> DoBet(User player)
        {
            try
            {
                next = false;
                if (lastRaise == 0 || lastRaise - player.lastRaise == 0)
                    await SendMessage("Ваш ход. У вас есть 60 секунд.", new List<User>() {player},
                        GameKeyboards.DoKeyboard);
                else
                {
                    await SendMessage("Ваш ход. У вас есть 60 секунд.", new List<User>() {player},
                        GameKeyboards.DoKeyboardCall(lastRaise - player.lastRaise));
                }

                player.state = State.waitBet;
                int j = 0;
                do
                {
                    j++;
                    await Task.Delay(1000);
                    if (j != 60) continue;
                    if (!players.Contains(player)) break;
                    RemovePlayer(player);
                    break;
                } while (next == false);

                if (!players.Contains(player))
                {
                    return false;
                }

                player.state = State.play;
                return true;
            }
            catch (Exception ex)
            {
                await ExceptionHandler.ExceptionInGame(ex, this);
                return true;
            }
        }

        protected async void OpenCards(int count = 1)
        {
            try
            {
                if (openedCards.Count == 5)
                {
                    SetWinner();
                    return;
                }

                if (count == 3 && players.Count > 2)
                {
                    for (int j = 0; j < 2; j++)
                    {
                        var player1 = players[^1];
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
                    if (foldUsers.Contains(user1))
                        await SendMessage("Вы сбросили карты.", new List<User>() {user1}, null);
                    if (allInUsers.Contains(user1))
                        await SendMessage("Вы пошли ва-банк.", new List<User>() {user1}, null);
                    await SendTable(user1);
                }

                if (players.Count - foldUsers.Count - allInUsers.Count == 1)
                {
                    OpenCards();
                    return;
                }

                foreach (User user in players.ToList()) user.lastRaise = 0;
                GameNext();
            }
            catch (Exception ex)
            {
                await ExceptionHandler.ExceptionInGame(ex, this);
            }

        }

        protected virtual async Task SendTable(User user1)
        {
            try
            {
                var x = openedCards.ToList();
                x.Add(user1.cards[0]);
                x.Add(user1.cards[1]);
                CombinationChecker.CheckCombination(x, user1);
                var path = ImageGenerator.GetImage(x, user1);
                await using var ms = new MemoryStream();
                path.Save(ms, ImageFormat.Jpeg);
                ms.Position = 0;
                try
                {
                    await TgBot.SendPhotoAsync(user1.Id, new InputOnlineFile(ms),
                        replyMarkup: GameKeyboards.CombinationKeyboard(user1));
                }
                catch
                {
                    RemovePlayer(user1);
                }
            }
            catch (Exception ex)
            {
                BotSettings.reviews.Enqueue(
                    $"{user1.Id}:Эксепшн: {ex.Message}\nОбъект, вызвавший исключение: {ex.Source}\nМетод, вызвавший исключение: {ex.TargetSite}");
                RemovePlayer(user1);
            }
        }

        private async void SetWinner()
        {
            try
            {
                block = true;
                var winners = players.Count == 1 ? players : GameOperations.MaxCombination(players);
                var playersNotLeave = players.ToList();
                string message = string.Empty, winnersFirstName = string.Empty;
                foreach (User user in playersNotLeave)
                {
                    message +=
                        $"Игрок {user.firstName}(<a href =\"https://telegram.me/PokerGame777_bot?start=info_{user.Id}\">+</a>): {user.cards[0]}, {user.cards[1]}";
                    if (foldUsers.Contains(user)) message += " - Fold\n";
                    else message += "\n";
                }

                await SendMessage(message, players, null, ParseMode.Html, false);
                GameOperations.Payment(winners, playersNotLeave, this);
                if (winners.Count > 1)
                {
                    winnersFirstName =
                        winners.Aggregate("", (current, user) => current + (user.firstName + ", "));
                }
                message = winners.Count > 1
                    ? $"Игра окончена: {winnersFirstName.Remove(winnersFirstName.Length - 2)} разделили выигрыш между собой с комбинацией {winners[0].combination.ToString(winners[0])}"
                    : $"Игра окончена: Победил {winners[0].firstName} с комбинацией {winners[0].combination.ToString(winners[0])}";
                await SendMessage(message,
                    players, null);
                await CreateNewRoom(playersNotLeave);
            }
            catch (Exception ex)
            {
                await ExceptionHandler.ExceptionInGame(ex, this, true);
            }
        }

        protected virtual async Task CreateNewRoom(List<User> playersNotLeave)
        {
            try
            {
                started = false;
                foreach (User user in playersNotLeave)
                {
                    user.state = State.wait;
                    if (user.Money >= 100 || user.Id == 0) continue;
                    try
                    {
                        await TgBot.SendTextMessageAsync(user.Id,
                            "Недостаточно средств. Счет должен быть больше 100 коинов.",
                            replyMarkup: MainKeyboards.MainKeyboard);
                        RemovePlayer(user);
                    }
                    catch
                    {
                        RemovePlayer(user);
                    }
                }

                var players1 = players.ToList();
                bet = 0;
                cards = GameOperations.CreateCards();
                openedCards.Clear();
                var x = players1[0];
                players1.Remove(x);
                players1.Add(x);
                allInUsers.Clear();
                foldUsers.Clear();
                players = players1;
                block = false;
                await SendMessage($"Вы находитесь в комнате {id} [{players.Count}/{countPlayers}].", players, null);
                if (players.Count != countPlayers) return;
                await SendMessage($"Новая игра начнеться через 10 секунд.", players, null);
                await Task.Delay(10000);
                if (players.Count != countPlayers)
                {
                    await SendMessage($"Недостаточно игроков для начала игры. Ожидание...", players, null);
                    return;
                }

                await SendCards();
            }
            catch
            {
                block = true;
                foreach (User user in players.ToList())
                {
                    RemovePlayer(user);
                }
            }
        }

        ~Room()
        {
            Console.WriteLine($"Комната {id} уничтожена!");
        }
    }
}
