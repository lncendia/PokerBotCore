using System;
using System.Linq;
using PokerBotCore.Entities;
using Telegram.Bot.Types.ReplyMarkups;

namespace PokerBotCore.Bot
{
    public static class Admin
    {
        public static async void Tgbot_Admin(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            if(e.Message.From.Id!=346978522) return;
            var user = Operations.GetUser(e.Message.From.Id);
            if (user.state != User.State.admin&&user.state!=User.State.mailing&&user.state!=User.State.addCoin&&user.state!=User.State.answer) return;
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
                    while (MainBot.reviews.Count != 0)
                    {
                        var x = MainBot.reviews.Dequeue().Split(new[] {':'}, 2);
                        var id = new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Ответить", x[0]));
                        await MainBot.Tgbot.SendTextMessageAsync(e.Message.Chat.Id, x[1], replyMarkup: id);
                    }

                    break;
                default:
                    DB db;
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
                            db = new DB();
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
                    }

                    break;
            }
        } 
    }
}