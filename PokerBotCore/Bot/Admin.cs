using System;
using System.Linq;
using PokerBotCore.Entities;
using PokerBotCore.Keyboards;
using Telegram.Bot.Types.ReplyMarkups;

namespace PokerBotCore.Bot
{
    public static class Admin
    {
        public static async void Tgbot_Admin(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            if (e.Message.From.Id != 346978522) return;
            var user = Operations.GetUser(e.Message.From.Id);
            if (user == null) return;
            switch (e.Message.Text)
            {
                case "Рассылка":
                    await MainBot.Tgbot.SendTextMessageAsync(e.Message.Chat.Id, "Введите сообщение.");
                    user.state = User.State.mailing;
                    break;
                case "Добавить средства":
                    await MainBot.Tgbot.SendTextMessageAsync(e.Message.Chat.Id,
                        "Введите сообщение в формате: <id>:<Money>");
                    user.state = User.State.addCoin;
                    break;
                case "Просмотр отзывов":
                    while (MainBot.Reviews.Count != 0)
                    {
                        var x = MainBot.Reviews.Dequeue().Split(new[] {':'}, 2);
                        var id = new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Ответить", x[0]));
                        await MainBot.Tgbot.SendTextMessageAsync(e.Message.Chat.Id, x[1], replyMarkup: id);
                    }

                    break;
                case "Фейк комнаты":
                    string str = "Фейк комнаты: ";
                    foreach (var room in MainBot.FakeRooms.ToList())
                    {
                        string start = room.started
                            ? "Игра идет."
                            : "Ожидание.";
                        str += $"Комната {room.id}. [{room.players.Count}/{room.countPlayers}]. " + start+"\n";
                    }
                    await MainBot.Tgbot.SendTextMessageAsync(e.Message.Chat.Id, str,replyMarkup:MainKeyboards.CreateOrRemoveFaceRoom);
                    break;
                default:
                    switch (user.state)
                    {
                        case User.State.mailing:
                            foreach (User user1 in MainBot.users)
                            {
                                await MainBot.Tgbot.SendTextMessageAsync(user1.Id, e.Message.Text);
                            }

                            user.state = User.State.admin;
                            break;
                        case User.State.addCoin:
                        {
                            await using Db db = new Db();
                            try
                            {
                                var x = e.Message.Text.Split(':');
                                User user1 = MainBot.users.FirstOrDefault(y => y.Id == int.Parse(x[0]));
                                if (user1 != null)
                                {
                                    user1.AddMoney(int.Parse(x[1]));
                                    db.Update(user1);
                                }

                                await db.SaveChangesAsync();
                            }
                            catch (Exception ex)
                            {
                                await MainBot.Tgbot.SendTextMessageAsync(e.Message.Chat.Id, $"Ошибка: {ex.Message}");
                            }

                            user.state = User.State.admin;
                            break;
                        }
                        case User.State.answer:
                            try
                            {
                                await MainBot.Tgbot.SendTextMessageAsync(user.idForAnswer,
                                    $"Администратор @{e.Message.From.Username} ответил вам: {e.Message.Text}");
                                await MainBot.Tgbot.SendTextMessageAsync(e.Message.Chat.Id, "Сообщение отправлено!");
                            }
                            catch (Exception ex)
                            {
                                await MainBot.Tgbot.SendTextMessageAsync(e.Message.Chat.Id,
                                    $"Ошибка: {ex.Message} Сообщение не отправлено!");
                            }

                            user.state = User.State.admin;
                            break;
                        case User.State.countFakeRoom:
                            if (int.TryParse(e.Message.Text, out int count))
                            {
                                MainBot.FakeRooms.Add(BuilderFaceRooms.CreateFakeRoom(count));
                                await MainBot.Tgbot.SendTextMessageAsync(e.Message.Chat.Id,
                                    $"Успешно");
                                user.state = User.State.admin;
                            }
                            else
                            {
                                await MainBot.Tgbot.SendTextMessageAsync(e.Message.Chat.Id,
                                    "Введи число.");
                            }
                            break;
                        case User.State.idFakeRoom:
                            if (int.TryParse(e.Message.Text, out count))
                            {
                                var fakeRoom = Operations.GetFaceRoom(count);
                                if(fakeRoom==null||fakeRoom.needDelete) return;
                                fakeRoom.needDelete = true;
                                await MainBot.Tgbot.SendTextMessageAsync(e.Message.Chat.Id,
                                    "Комната будет удалена по окончанию игры.");
                                user.state = User.State.admin;
                            }
                            else
                            {
                                await MainBot.Tgbot.SendTextMessageAsync(e.Message.Chat.Id,
                                    "Введи число.");
                            }
                            break;
                    }

                    break;
            }
        }

        public static async void Tgbot_AdminCallbackQuery(object sender, Telegram.Bot.Args.CallbackQueryEventArgs e)
        {
            if(e.CallbackQuery.From.Id!=346978522) return;
            var user = Operations.GetUser(e.CallbackQuery.From.Id);
            if(user==null) return;
            if (user.state != User.State.admin && user.state != User.State.answer) return;
            switch (e.CallbackQuery.Data)
            {
                case "createFakeRoom":
                    user.state = User.State.countFakeRoom;
                    await MainBot.Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id, "Введите количество игроков.");
                    break;
                case "removeFakeRoom":
                    user.state = User.State.idFakeRoom;
                    await MainBot.Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id, "Введите id комнаты.");
                    break;
                default:
                    user.state = User.State.answer;
                    await MainBot.Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "Введите сообщение.");
                    user.idForAnswer = int.Parse(e.CallbackQuery.Data);
                    break;
            }
        }
    }
}