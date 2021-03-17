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
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

namespace PokerBotCore.Rooms
{
    public class FakeRoom : Room
    {
        public bool needDelete = false;
        private readonly List<string> _botCards = new();

        private static readonly List<Combination.Comb> Combinations = new()
            {Combination.Comb.flush, Combination.Comb.kare, Combination.Comb.set, Combination.Comb.twoPair};

        public FakeRoom(User user, int count) : base(user, count, false)
        {
            var x = new FakeCombination(Combinations[Rnd.Next(0, Combinations.Count)], count);
            //var x = new FakeCombination(combinations[0], count);
            x.GetCards(cards, _botCards);
        }
        public override void SendMessage(string message, IEnumerable<User> users, IReplyMarkup replyMarkup,
            bool needLeave = true)
        {
            foreach (var player in users.ToList().Where(player => player.Id != 0))
            {
                try
                {
                    TgBot.SendTextMessageAsync(player.Id, message, replyMarkup: replyMarkup);
                }
                catch
                {
                    if (needLeave) UserLeave(player);
                }
            }
        }
        protected override async Task SendTable(User user1)
        {
            try
            {
                if (block) return;
                var x = openedCards.ToList();
                x.Add(user1.cards[0]);
                x.Add(user1.cards[1]);
                var combination = GameOperations.CheckCombination(x, user1);
                if (user1.Id == 0) return;
                var keyboard = GameKeyboards.CombinationKeyboard(user1, combination);
                var path = GameOperations.GetImage(x, user1);
                await using var ms = new MemoryStream();
                path.Save(ms, ImageFormat.Jpeg);
                ms.Position = 0;
                try
                {
                    await TgBot.SendPhotoAsync(user1.Id, new InputOnlineFile(ms), replyMarkup: keyboard);
                }
                catch
                {
                    UserLeave(user1);
                }
            }
            catch (Exception ex)
            {
                BotSettings.reviews.Enqueue(
                    $"{user1.Id}:Эксепшн: {ex.Message}\nОбъект, вызвавший исключение: {ex.Source}\nМетод, вызвавший исключение: {ex.TargetSite}");
                UserLeave(user1);
            }
        }

        protected override void SendCards()
        {
            try
            {
                block = true; //Для того, чтоб человек не мог выйти.
                players[0].cards = _botCards;
                players[0].state = State.play;
                players[0].lastRaise = 0;
                players[0].combination = null;
                players[0].bet = 0;
                GameOperations.CheckCombination(players[0].cards, players[0]);
                for (int i = 1; i < players.Count; i++)
                {
                    User user1 = players[i];
                    user1.state = State.play;
                    user1.lastRaise = 0;
                    user1.combination = null;
                    user1.bet = 0;
                    user1.cards = new List<string>();
                    int card = 0; //rnd.Next(0, cards.Count);
                    user1.cards.Add(cards[card]);
                    cards.Remove(cards[card]);
                    //card = rnd.Next(0, cards.Count);
                    user1.cards.Add(cards[card]);
                    cards.Remove(cards[card]);

                    string combination = GameOperations.CheckCombination(user1.cards, user1);
                    SendMessage("Игра началась! \nВаши карты: ", new List<User>() {user1},
                        GameKeyboards.CombinationKeyboard(user1, combination));
                }

                players[0].lastRaise = 10;
                players[0].Money -= 10;
                bet += 10;
                players[1].lastRaise = 25;
                players[1].Money -= 25;
                bet += 25;
                SendMessage("Блайнд - 25 коинов.", new List<User>() {players[1]}, null);
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
                GameOperations.ExceptionInGame(ex,this);
            }
        }

        protected override async void GameNext()
        {
            try
            {
                for (int i = 0; i < players.Count; i++)
                {
                    if (players.Count - foldUsers.Count - allInUsers.Count == 1) continue;
                    User player = players[i];
                    if (player.Id == 0)
                    {
                        await Task.Delay(Rnd.Next(300, 5000));
                        if (Rnd.Next(0, 3) == 2)
                        {
                            int round = Rnd.Next(25, 100) * (openedCards.Count + 1);
                            int raise = round - round % 10;
                            int raise1 = lastRaise - player.lastRaise + raise;
                            lastRaise += raise;
                            bet += raise1;
                            player.Money -= raise1;
                            player.lastRaise += raise1;
                            if (player.Money == 0) allInUsers.Add(player);
                            next = true;
                            SendMessage($"Игрок {player.firstName} повысил ставку на {raise} коинов.",
                                player.room.players, null);
                            continue;
                        }
                        player.Money -= lastRaise - player.lastRaise;
                        player.room.bet += lastRaise - player.lastRaise;
                        player.lastRaise += lastRaise - player.lastRaise;
                        next = true;
                        continue;
                    }
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
                GameOperations.ExceptionInGame(ex, this);
            }
        }

        protected override async Task CreateNewRoom(List<User> playersNotLeave)
        {
            try
            {
                for (int i = 1; i < playersNotLeave.Count; i++)
                {
                    User user = playersNotLeave[i];
                    if (user.Money >= 100) continue;
                    try
                    {
                        await TgBot.SendTextMessageAsync(user.Id,
                            "Недостаточно средств. Счет должен быть больше 100 коинов.",
                            replyMarkup: MainKeyboards.MainKeyboard);
                        UserLeave(user);
                    }
                    catch
                    {
                        UserLeave(user);
                    }
                }

                block = false;
                SendMessage($"Вы находитесь в комнате {id} [{players.Count}/{countPlayers}].", players, null);
                if (players.Count == countPlayers)
                {
                    SendMessage($"Новая игра начнеться через 10 секунд.", players, null);
                    await Task.Delay(5000);
                    UserLeave(players[0]);
                    if (players.Count > 0)
                    {
                        var players1 = players.ToList();
                        if (players.Count > 0)
                            BotSettings.rooms[BotSettings.rooms.IndexOf(this)] = new Room(players1, countPlayers, id);
                    }
                    await Task.Delay(5000);
                }
                else
                {
                    UserLeave(players[0]);
                    if (players.Count > 0)
                    {
                        var players1 = players.ToList();
                        if (players.Count > 0)
                            BotSettings.rooms[BotSettings.rooms.IndexOf(this)] = new Room(players1, countPlayers, id);
                    }
                }

                SendMessage($"Недостаточно игроков для начала игры. Ожидание...", players, null);
                if (!needDelete)
                    
                    BotSettings.fakeRooms[BotSettings.fakeRooms.IndexOf(this)] = BuilderFakeRooms.CreateFakeRoom(countPlayers);
                
            }
            catch
            {
                block = true;
                foreach (User user in players.ToList())
                {
                    UserLeave(user);
                }
            }
        }

    }
}
