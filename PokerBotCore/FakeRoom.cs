﻿using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

namespace PokerBot
{
    class FakeRoom : Room
    {
        List<string> botcards = new List<string>();
        static readonly List<Combination.Comb> combinations = new List<Combination.Comb>() { Combination.Comb.flush, Combination.Comb.kare, Combination.Comb.set, Combination.Comb.twopair };
        public FakeRoom(User user, string FirstName, int count) : base(user, FirstName, count, false)
        {
            var x = new FakeCombination(combinations[rnd.Next(0, combinations.Count)], count);
            //var x = new FakeCombination(combinations[0], count);
            x.GetCards(cards, botcards);
        }
        protected override async Task SendTable(User user1)
        {
            try
            {
                if (endgame) return;
                var x = openedCards.ToList();
                x.Add(user1.cards[0]);
                x.Add(user1.cards[1]);
                var combination = Operation.CheckCombination(x, user1);
                if (user1.Id == 0) return;
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
        protected override void SendCards()
        {
            try
            {
                endgame = true; //Для того, чтоб человек не мог выйти.
                players[0].cards = botcards;
                players[0].state = User.State.play;
                players[0].lastraise = 0;
                players[0].combination = null;
                players[0].bet = 0;
                Operation.CheckCombination(players[0].cards, players[0]);
                for (int i = 1; i < players.Count; i++)
                {
                    User user1 = players[i];
                    user1.state = User.State.play;
                    user1.lastraise = 0;
                    user1.combination = null;
                    user1.bet = 0;
                    user1.cards = new List<string>();
                    int card = 0;//rnd.Next(0, cards.Count);
                    user1.cards.Add(cards[card]);
                    cards.Remove(cards[card]);
                    //card = rnd.Next(0, cards.Count);
                    user1.cards.Add(cards[card]);
                    cards.Remove(cards[card]);

                    string combination = Operation.CheckCombination(user1.cards, user1);
                    var keyboard = new InlineKeyboardMarkup(new List<List<InlineKeyboardButton>>() { new List<InlineKeyboardButton>() { InlineKeyboardButton.WithCallbackData(user1.cards[0]), InlineKeyboardButton.WithCallbackData(user1.cards[1]) }, new List<InlineKeyboardButton>() { InlineKeyboardButton.WithCallbackData(combination) } });
                    SendMessage("Игра началась! \nВаши карты: ", new List<User>() { user1 }, keyboard, final: true);
                }
                players[0].lastraise = 10;
                players[0].Money -= 10;
                bet += 10;
                players[1].lastraise = 25;
                players[1].Money -= 25;
                bet += 25;
                SendMessage("Блайнд - 25 коинов.", new List<User>() { players[1] }, null, final: true);
                if (count_players != 2)
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
        protected override async void GameNext()
        {
            try
            {
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
                    if (player.Id == 0)
                    {
                        await Task.Delay(rnd.Next(300, 5000));
                        if (rnd.Next(0, 3) == 2)
                        {
                            int round = rnd.Next(25, 100) * (openedCards.Count + 1);
                            int raise = round - round % 10;
                            int raise1 = lastraise - player.lastraise + raise;
                            lastraise += raise;
                            bet += raise1;
                            player.Money -= raise1;
                            player.lastraise += raise1;
                            if (player.Money == 0) allInUsers.Add(player);
                            next = true;
                            SendMessage($"Игрок {player.FirstName} повысил ставку на {raise} коинов.", player.room.players, null, true);
                            continue;
                        }
                        else
                        {
                            player.Money -= lastraise - player.lastraise;
                            player.room.bet += lastraise - player.lastraise;
                            player.lastraise += lastraise - player.lastraise;
                            next = true;
                            continue;
                        }
                    }

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
                    if (count_players > 2) await DoBet(players[players.Count - 1]);
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
        protected override async Task CreateNewRoom(List<User> playersNotLeave)
        {
            try
            {
                for (int i = 1; i < playersNotLeave.Count; i++)
                {
                    User user = playersNotLeave[i];
                    if (user.Money < 100)
                    {
                        try
                        {
                            await tgbot.SendTextMessageAsync(user.Id, "Недостаточно средств. Счет должен быть больше 100 коинов.", replyMarkup: Bot.keyboard);
                            UserLeave(user);
                        }
                        catch { UserLeave(user); }
                    }
                }
                SendMessage($"Вы находитесь в комнате {id} [{players.Count}/{count_players}].", players, null, final: true);
                if (players.Count == count_players)
                {
                    SendMessage($"Новая игра начнеться через 10 секунд.", players, null, final: true);
                    await Task.Delay(5000);
                    UserLeave(players[0]);
                    await Task.Delay(5000);
                }
                else UserLeave(players[0]);
                SendMessage($"Недостаточно игроков для начала игры. Ожидание...", players, null);
                var players1 = players.ToList();
                Bot.rooms[Bot.rooms.IndexOf(this)] = new Room(players1, count_players, id);
                //Bot.botrooms.IndexOf() Operation.CreateFakeRoom(count_players);
                return;
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

    }
}