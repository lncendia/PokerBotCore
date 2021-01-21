using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;

namespace PokerBotCore
{
    class Bot
    {
        public static readonly TelegramBotClient Tgbot = new TelegramBotClient("1341769299:AAE4q84mx-NRrSJndKsCVNVLr-SzjYeN7wk");
        public static List<Room> rooms = new List<Room>();
        public static List<User> chat = new List<User>();
        public static List<User> users;
        public static List<FakeRoom> botrooms = new List<FakeRoom>();
        public static Queue<string> reviews = new Queue<string>();
        public static int roomsfortest = 0;
        public static ReplyKeyboardMarkup keyboard = new ReplyKeyboardMarkup(new List<List<KeyboardButton>>() { new List<KeyboardButton>() { new KeyboardButton("🃏Список комнат"), new KeyboardButton("🥊Создать комнату") }, new List<KeyboardButton>() { new KeyboardButton("🎲Пополнить счет"), new KeyboardButton("💸Вывод") }, new List<KeyboardButton>() { new KeyboardButton("👤Профиль"), new KeyboardButton("⁉️Оставить отзыв") }, new List<KeyboardButton>() { new KeyboardButton("📬Игровой чат") } });
        static readonly ReplyKeyboardMarkup KeyboardAdmin = new ReplyKeyboardMarkup(new List<List<KeyboardButton>>() { new List<KeyboardButton>() { new KeyboardButton("Рассылка"), new KeyboardButton("Комнаты с ботами") }, new List<KeyboardButton>() { new KeyboardButton("Добавить средства"), new KeyboardButton("Просмотр отзывов") }, new List<KeyboardButton>() { new KeyboardButton("/admin") } });
        public static void Start()
        {
            using DB db = new DB();
            users = db.Users.ToList();
            db.Dispose();
            Tgbot.OnMessage += Tgbot_OnMessage;
            Tgbot.OnCallbackQuery += Tgbot_OnCallbackQuery;
            Operation.Mute();
            Tgbot.StartReceiving();
        }

        private static async void Tgbot_OnCallbackQuery(object sender, Telegram.Bot.Args.CallbackQueryEventArgs e)
        {
            try
            {
                await using DB db = new DB();
                var cb = e.CallbackQuery.Data;
                User user = users.FirstOrDefault(x => x.Id == e.CallbackQuery.From.Id);
                if (user == null) return;
                db.Update(user);
                if (cb.Contains("public") && user.state == User.State.waitcount)
                {
                    int count = Int32.Parse(cb.Substring(7));
                    Room room = new Room(user, e.CallbackQuery.From.FirstName, count, false);
                    user.room = room;
                    user.state = User.State.wait;
                    await Tgbot.DeleteMessageAsync(e.CallbackQuery.Message.Chat.Id, e.CallbackQuery.Message.MessageId);
                    var keyboard1 = new InlineKeyboardMarkup(new List<List<InlineKeyboardButton>>() { { new List<InlineKeyboardButton>() { InlineKeyboardButton.WithCallbackData("Поделиться в чате", "sentroom") } }, new List<InlineKeyboardButton>() { InlineKeyboardButton.WithCallbackData("Отмена", "exit") } });
                    await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id, $"Создана комната с ID {room.id}. Ожидаем подключения других игроков.", replyMarkup: keyboard1);
                }
                else if (cb.Contains("private") && user.state == User.State.waitcount)
                {
                    int count = Int32.Parse(cb.Substring(8));
                    Room room = new Room(user, e.CallbackQuery.From.FirstName, count, true);
                    user.room = room;
                    rooms.Add(room);
                    user.state = User.State.wait;
                     await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);
                    var keyboard1 = new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Отмена", "exit"));
                     await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id, $"Создана комната с ID {room.id}. Ожидаем подключения других игроков.", replyMarkup: keyboard1);
                }
                else if (cb.StartsWith("bill"))
                {
                    if (Operation.CheckPay(user, cb.Substring(5)))
                    {
                        string message = e.CallbackQuery.Message.Text;
                        message = message.Replace("Не оплачено", "Оплачено");
                        await Tgbot.EditMessageTextAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId, message);
                    }
                }
                else if (cb.StartsWith("Add"))
                {
                    long id;
                    try
                    {
                        id = Int64.Parse(cb.Substring(4));
                    }
                    catch { return; }
                    User info = users.FirstOrDefault(x => x.Id == id);
                    if (info == null)
                    {
                         await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, $"Пользователь не найден.");
                        return;
                    }
                    var f = db.Friendships.FirstOrDefault(friendship => (friendship.User1 == user.Id && friendship.User2 == info.Id) || (friendship.User1 == info.Id && friendship.User2 == user.Id));
                    if (f == null)
                    {
                         await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, $"Заявка была отклонена пользователем.");
                        return;
                    }
                    if (f.Accepted)
                    {
                         await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, $"Вы уже друзья.");
                        return;
                    };
                    f.Accepted = true;
                    await db.SaveChangesAsync();
                    await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, $"Запрос принят.");
                }
                switch (cb)
                {
                    case "exit":
                         await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);
                        if (user.state == User.State.wait || user.state == User.State.waitbet || user.state == User.State.play)
                        {
                            while (user.room.endgame) { }
                            user.room.UserLeave(user);
                        }
                         await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, $"Вы покинули комнату.");
                        break;
                    case "sentroom":
                        if (user.state != User.State.wait) return;
                        var keyboard1 = new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Отмена", "exit"));
                         await Tgbot.EditMessageTextAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId, e.CallbackQuery.Message.Text, replyMarkup: keyboard1);
                         SendMessageToChat($"Приглашаю вас в комнату {user.room.id}.", e.CallbackQuery.From.Username, user, new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData($"Комната {user.room.id} [{user.room.players.Count}/{user.room.countPlayers}]", user.room.id.ToString())));
                         await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, $"Приглашение отправлено.");
                        break;
                    case "Raise":
                        if (user.state == User.State.waitbet)
                        {
                             await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id, $"Введите колличество. На вашем счету {user.Money} коинов. Максимальная ставка: 1000 коинов.");
                        }
                        break;
                    case "Call":
                        if (user.state == User.State.waitbet)
                        {
                            var x = user.room.lastraise - user.lastraise;
                            if (user.Money >= x)
                            {
                                user.Money -= x;
                                user.room.bet += x;
                                user.lastraise += x;
                                user.bet += x;
                                if (user.Money == 0) user.room.allInUsers.Add(user);
                                user.room.next = true;
                                db.UpdateRange(user);
                                await db.SaveChangesAsync();
                                await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "Ход переходит к следующему игроку.");
                            }
                            else
                            {
                                var key = new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Ва-банк", "VA-Bank"));
                                await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id, $"Недостаточно средств!", replyMarkup: key);
                            }
                        }
                        await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);
                        break;
                    case "VA-Bank":
                        if (user.state == User.State.waitbet)
                        {
                            if (user.Money < user.room.lastraise - user.lastraise)
                            {
                                user.room.allInUsers.Add(user);
                                user.room.bet += user.Money;
                                user.bet += user.Money;
                                user.lastraise += user.Money;
                                user.Money = 0;
                                user.room.next = true;
                                 await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "Ход переходит к следующему игроку.");
                                db.UpdateRange(user);
                                await db.SaveChangesAsync();
                                user.room.SendMessage($"Игрок {user.FirstName} пошел ва-банк.", user.room.players, null);
                            }
                        }
                        await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);
                        break;
                    case "Check":
                        if (user.state == User.State.waitbet)
                        {
                            if (user.room.lastraise == 0 || user.room.lastraise - user.lastraise == 0)
                            {
                                user.room.next = true;
                                 await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "Ход переходит к следующему игроку.");
                            }
                            else
                            {
                                 await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "Отличная попытка схитрить... Но нет.", true);
                                break;
                            }
                        }
                        await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);
                        break;
                    case "Fold":
                        if (user.state == User.State.waitbet)
                        {
                            user.combination = null;
                            user.room.next = true;
                            user.room.foldUsers.Add(user);
                            user.lastraise = 0;
                            await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "Ход переходит к следующему игроку.");
                            user.room.SendMessage($"Игрок {user.FirstName} сбросил карты.", user.room.players, null);
                        }
                        await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);
                        break;
                    case "change_table":
                        if (user.state == User.State.main)
                        {
                            var image = Image.FromFile(File.Exists($"tables\\{user.Id}.jpg") ? $"tables\\{user.Id}.jpg" : $"tables\\table.jpg");
                            await using (var ms = new MemoryStream())
                            {
                                image.Save(ms, ImageFormat.Jpeg);
                                image.Dispose();
                                ms.Position = 0;
                                await Tgbot.SendPhotoAsync(e.CallbackQuery.From.Id, new InputOnlineFile(ms), caption: "Ваш нынешний фон.");
                                await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id, "Отправьте фотографию фона, который хотите установить.", replyMarkup: new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Установить стандартный фон", "standart_table")));
                            }
                            user.state = User.State.change_table;
                        }
                        break;
                    case "standard_table":
                        if (user.state == User.State.change_table)
                        {
                             await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);
                            File.Delete($"tables\\{user.Id}.jpg");
                            user.state = User.State.main;
                        }
                        break;
                    case "friends":
                        if (user.state == User.State.main)
                        {
                            var f = db.Friendships.Where(friendship => friendship.User1 == user.Id || friendship.User2 == user.Id);
                            if (!f.Any())
                            {
                                await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "У вас нет друзей :(");
                                return;
                            }

                            string friends = "Ваши друзья:\n";
                            foreach (Friendship friend in f)
                            {
                                if (friend.User1 == user.Id)
                                {
                                    var user2 = await Tgbot.GetChatMemberAsync((int) friend.User2, (int) friend.User2);
                                    var friendUser = users.FirstOrDefault(x => x.Id == friend.User2);
                                    string online = friendUser != null && friendUser.count_messages > 0 ? "В сети" : "Не в сети";
                                    if (friendUser?.room!=null && friendUser.state == User.State.wait)
                                    {
                                        friends += $"(<a href =\"https://telegram.me/PokerGame777_bot?start=remove_{friend.ID}\">-</a>)@{user2.User.Username} (<a href =\"https://telegram.me/PokerGame777_bot?start=connect_{friendUser.room.id}\">В игре</a>)\n";
                                    }
                                    else
                                        friends += $"(<a href =\"https://telegram.me/PokerGame777_bot?start=remove_{friend.ID}\">-</a>)@{user2.User.Username} ({online})\n";
                                }
                                else
                                {
                                    var user2 =  await Tgbot.GetChatMemberAsync((int)friend.User1, (int)friend.User1);
                                    var friendUser = users.FirstOrDefault(x => x.Id == friend.User1);
                                    string online = friendUser != null && friendUser.count_messages > 0 ? "В сети." : "Не в сети.";
                                    if (friendUser?.room != null && friendUser.state == User.State.wait)
                                    {
                                        friends += $"(<a href =\"https://telegram.me/PokerGame777_bot?start=remove_{friend.ID}\">-</a>)@{user2.User.Username} (<a href =\"https://telegram.me/PokerGame777_bot?start=connect_{friendUser.room.id}\">В игре</a>)\n";
                                    }
                                    else
                                        friends += $"(<a href =\"https://telegram.me/PokerGame777_bot?start=remove_{friend.ID}\">-</a>)@{user2.User.Username} ({online})\n";
                                }

                            }
                            await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id, friends, parseMode:ParseMode.Html);

                        }
                        break;
                    default:
                        if (user.state == User.State.admin)
                        {
                            user.state = User.State.answer;
                            await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "Введите сообщение.");
                            user.id_for_answer = int.Parse(cb);
                            return;
                        }
                        if (user.state != User.State.main && user.state != User.State.chat) return;
                        int idRoom;
                        try
                        {
                            idRoom = int.Parse(cb);
                            await Tgbot.DeleteMessageAsync(e.CallbackQuery.From.Id, e.CallbackQuery.Message.MessageId);
                        }
                        catch
                        {
                            return;
                        }
                        if (user.Money < 10) ////////////////////////////////////////////////////////////
                        {
                             await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "Недостаточно средств. Счет должен быть больше 100 коинов.");
                            return;
                        }
                        var room = Operation.GetRoom(Convert.ToInt32(cb));
                        if (room != null)
                        {
                            await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "Вы покинули чат.");
                            if (room.key != 0)
                            {
                                user.id_privateroom = idRoom;
                                user.state = User.State.codprvt;
                                await Tgbot.SendTextMessageAsync(e.CallbackQuery.From.Id, $"Введите пароль.");
                            }
                            else
                            {
                                room.AddPlayer(user, e.CallbackQuery.From.FirstName);
                            }
                        }
                        else
                        {
                            await Tgbot.AnswerCallbackQueryAsync(e.CallbackQuery.Id, "Комната не доступна для подключения. Возможно игра в ней уже началась.");
                        }
                        break;
                }
            }
            catch (Exception ex) { reviews.Enqueue($"Ошибка у пользователя {e.CallbackQuery.From.Id}: {ex.Message}\nОбъект, вызвавший исключение: {ex.Source}\nМетод, вызвавший исключение: {ex.TargetSite}"); }
        }
        private static async void Tgbot_OnMessage(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            try
            {
                var message = e.Message;
                if (message.Type != Telegram.Bot.Types.Enums.MessageType.Text && message.Type != Telegram.Bot.Types.Enums.MessageType.Photo) return;
                var user = users.FirstOrDefault(x=>x.Id==message.From.Id);
                if (user != null)
                {
                    user.count_messages++;
                    switch (user.count_messages)
                    {
                        case 10:
                            await Tgbot.SendTextMessageAsync(message.Chat.Id, "Пожалуйста, не флудите.");
                            break;
                        case 20:
                            await Tgbot.SendTextMessageAsync(message.Chat.Id, "Прекратите флуд.");
                            break;
                        case 30:
                            await Tgbot.SendTextMessageAsync(message.Chat.Id, $"Вам выдан мут до {Operation.time.ToString("HH:mm:ss")}");
                            return;
                        default:
                        {
                            if (user.count_messages > 30)
                            {
                                return;
                            }

                            break;
                        }
                    }
                }
                if (message.Type == MessageType.Text)
                {
                    if (message.Text.Contains("/start"))
                    {
                        await using DB db = new DB();
                        if (message.Text.Contains("info_") && user!=null) 
                        {
                             AddFriend(message, user);
                        }
                        if (message.Text.Contains("connect_") && user != null)
                        {
                            int idRoom = Int32.Parse(message.Text.Split('_')[1]);
                            Room room = rooms.Find((room1 => room1.id == idRoom));
                            if(room==null) return;
                            InlineKeyboardButton key;
                            if (room.key != 0) key = InlineKeyboardButton.WithCallbackData($"🔒Комната {room.id} [{room.players.Count}/{room.countPlayers}]", room.id.ToString());
                            else key = InlineKeyboardButton.WithCallbackData($"Комната {room.id} [{room.players.Count}/{room.countPlayers}]", room.id.ToString());
                            await Tgbot.SendTextMessageAsync(user.Id, $"Комната вашего друга:",replyMarkup: new InlineKeyboardMarkup(key));
                        }
                        if (message.Text.Contains("remove_") && user != null)
                        {
                            long id = Int64.Parse(message.Text.Split('_')[1]);
                            Friendship friendship = db.Friendships.FirstOrDefault(x => x.ID == id);
                            if(friendship==null) return;
                            if (friendship.User1 == user.Id || friendship.User2 == user.Id)
                            {
                                db.Friendships.Remove(friendship);
                                await db.SaveChangesAsync();
                            }
                            await Tgbot.SendTextMessageAsync(user.Id, $"Друг удален.");
                        }
                        if (user != null) return;
                        User refer;
                        try
                        {
                            long id = long.Parse(message.Text.Split(' ')[1]);
                            refer = users.FirstOrDefault(x => x.Id == id);
                            if (refer.Id == message.Chat.Id) refer = null;
                        }
                        catch { refer = null; }
                        if (refer != null)
                        {
                            try
                            {
                                 await Tgbot.SendTextMessageAsync(refer.Id, $"По вашей реферальной ссылке подключился игрок @{message.From.Username}.");
                            }
                            catch
                            {
                                // ignored
                            }
                        }
                        var userToAdd = new User() { Id = message.From.Id, Money = 0, Referal = refer };
                        users.Add(userToAdd);
                        if(refer!=null)db.Update(refer);
                        await db.Users.AddAsync(userToAdd);
                        await db.SaveChangesAsync();
                        await Tgbot.SendTextMessageAsync(message.Chat.Id, $"Добро пожаловать! Пополни свой счет и вперед играть!\nТвоя реферальная ссылка: https://t.me/PokerGame777_bot?start={message.From.Id} \nЗа каждого приглашенного игрока вы будете получать 7% от его пополнений.", replyMarkup: keyboard);
                        return;
                    }
                    if (user == null) return;
                    #region admin
                    if (user.state == User.State.admin)
                    {
                        switch (message.Text)
                        {
                            case "/admin":
                                 await Tgbot.SendTextMessageAsync(message.Chat.Id, "Вы вышли из меню админа.", replyMarkup: keyboard);
                                user.state = User.State.main;
                                break;
                            case "Рассылка":
                                 await Tgbot.SendTextMessageAsync(message.Chat.Id, "Введите сообщение.");
                                user.state = User.State.mailing;
                                break;
                            case "Добавить средства":
                                 await Tgbot.SendTextMessageAsync(message.Chat.Id, "Введите сообщение в формате: <id>:<Money>");
                                user.state = User.State.add_coin;
                                break;
                            case "Просмотр отзывов":
                                while (reviews.Count != 0)
                                {
                                    var x = reviews.Dequeue().Split(new char[] { ':' }, 2);
                                    var id = new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Ответить", x[0]));
                                     await Tgbot.SendTextMessageAsync(message.Chat.Id, x[1], replyMarkup: id);
                                }
                                break;
                        }
                        return;
                    }
                    #endregion
                    switch (message.Text)
                    {
                        case "🥊Создать комнату":
                            if (user.state == User.State.wait || user.state == User.State.waitbet || user.state == User.State.play) return;
                            if (user.Money < 10)//////////////////////////////
                            {
                                await Tgbot.SendTextMessageAsync(message.Chat.Id, "Недостаточно средств. Счет должен быть больше 100 коинов.");
                                return;
                            }
                            user.state = User.State.waitcount;
                             await Tgbot.SendTextMessageAsync(message.Chat.Id, "Введите количество мест. От 2 до 5.");
                            break;
                        case "🃏Список комнат":
                            if (user.state == User.State.wait || user.state == User.State.waitbet || user.state == User.State.play) return;
                            user.state = User.State.main;
                            var key = new List<List<InlineKeyboardButton>>();
                            foreach (Room room in rooms)
                            {
                                if (key.Count == 50) break;
                                if ((room.players.Count != 0 && room.players[0].state != User.State.wait)) continue;
                                if (room.key != 0) key.Add(new List<InlineKeyboardButton>() { InlineKeyboardButton.WithCallbackData($"🔒Комната {room.id} [{room.players.Count}/{room.countPlayers}]", room.id.ToString()) });
                                else key.Add(new List<InlineKeyboardButton>() { InlineKeyboardButton.WithCallbackData($"Комната {room.id} [{room.players.Count}/{room.countPlayers}]", room.id.ToString()) });
                            }
                            if (key.Count != 0)
                                 await Tgbot.SendTextMessageAsync(message.Chat.Id, "Показаны первые 50 комнат. Вы можете ввести ID нужной вам комнаты.", replyMarkup: new InlineKeyboardMarkup(key));
                            else
                                 await Tgbot.SendTextMessageAsync(message.Chat.Id, "Комнаты не найдены.");
                            break;
                        case "Выход":
                            if (user.state == User.State.wait || user.state == User.State.waitbet || user.state == User.State.play)
                            {
                                 await Tgbot.SendTextMessageAsync(message.Chat.Id, "Вы уверены?", replyMarkup: new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Да", "exit")));
                            }
                            break;
                        case "🎲Пополнить счет":
                            if (user.state == User.State.wait || user.state == User.State.waitbet || user.state == User.State.play) return;
                             await Tgbot.SendTextMessageAsync(user.Id, "Введите сумму, на которую хотите пополнить баланс.");
                            user.state = User.State.waitmoney;
                            break;
                        case "💸Вывод":
                            if (user.state == User.State.wait || user.state == User.State.waitbet || user.state == User.State.play) return;
                            user.state = User.State.output;
                             await Tgbot.SendTextMessageAsync(message.Chat.Id, "Введите сумму, которую хотите вывести.");
                            break;
                        case "⁉️Оставить отзыв":
                            if (user.state == User.State.wait || user.state == User.State.waitbet || user.state == User.State.play) return;
                             await Tgbot.SendTextMessageAsync(message.Chat.Id, "Напишите отзыв. Он будет отпрален автору бота.");
                            user.state = User.State.feedback;
                            break;
                        case "👤Профиль":
                            if (user.state == User.State.wait || user.state == User.State.waitbet || user.state == User.State.play) return;
                            user.state = User.State.main;
                            string str = $"Ваш ID: {user.Id}\nВаши средства: {user.Money}\nВаша реферальная ссылка: https://t.me/PokerGame777_bot?start={message.From.Id}";
                            if (user.Referal != null)
                            {
                                var user2 = await Tgbot.GetChatMemberAsync((int)user.Referal.Id, (int)user.Referal.Id);
                                str += $"\nВас пригласил: @{user2.User.Username}";
                            }
                            await Tgbot.SendTextMessageAsync(message.Chat.Id, str, replyMarkup: new InlineKeyboardMarkup(new List<InlineKeyboardButton>() { InlineKeyboardButton.WithCallbackData("Друзья", "friends"), InlineKeyboardButton.WithCallbackData("Сменить фон стола", "change_table") }));
                            break;
                        case "📬Игровой чат":
                            if (user.state == User.State.wait || user.state == User.State.waitbet || user.state == User.State.play) return;
                            user.state = User.State.chat;
                            chat.Add(user);
                             await Tgbot.SendTextMessageAsync(message.Chat.Id, "Вы вошли в игровой чат.", replyMarkup: new ReplyKeyboardMarkup(new List<KeyboardButton>() { new KeyboardButton("Покинуть чат") }));
                            break;
                        case "/admin":
                            if (user.Id != 346978522) return;
                            if (user.state == User.State.wait || user.state == User.State.waitbet || user.state == User.State.play) return;
                            user.state = User.State.admin;
                             await Tgbot.SendTextMessageAsync(message.Chat.Id, "Добро пожаловать в админ-панель.", replyMarkup: KeyboardAdmin);
                            break;
                        default:
                            int raise;
                            DB db;
                            switch (user.state)
                            {
                                case User.State.waitcount:
                                    int count;
                                    try
                                    {
                                        count = Int32.Parse(message.Text);
                                    }
                                    catch
                                    {
                                         await Tgbot.SendTextMessageAsync(message.Chat.Id, "Введите число!");
                                        return;
                                    }
                                    if (count < 2 || count > 5)
                                    {
                                         await Tgbot.SendTextMessageAsync(message.Chat.Id, "Написано же! От 2 до 5.");
                                        return;
                                    }
                                    var keyb = new List<InlineKeyboardButton> { InlineKeyboardButton.WithCallbackData("Публичная", $"public_{count}"), InlineKeyboardButton.WithCallbackData("Приватная", $"private_{count}") };
                                     await Tgbot.SendTextMessageAsync(message.Chat.Id, "Выберете тип комнаты:", replyMarkup: new InlineKeyboardMarkup(keyb));
                                    break;
                                case User.State.main:
                                    int id;
                                    try
                                    {
                                        id = int.Parse(message.Text);
                                    }
                                    catch
                                    {
                                         await Tgbot.SendTextMessageAsync(message.Chat.Id, "Введите число!");
                                        return;
                                    }
                                    Room room = Operation.GetRoom(id);
                                    if (room == null || (room.players.Count != 0 && room.players[0].state != User.State.wait))
                                    {
                                         await Tgbot.SendTextMessageAsync(message.Chat.Id, "Комната не существует или игра в ней уже началась.");
                                        return;
                                    }
                                    keyb = new List<InlineKeyboardButton>
                                    {
                                        InlineKeyboardButton.WithCallbackData($"Комната {room.id} [{room.players.Count}/{room.countPlayers}]", room.id.ToString())
                                    };
                                     await Tgbot.SendTextMessageAsync(message.Chat.Id, "Список комнат:", replyMarkup: new InlineKeyboardMarkup(keyb));
                                    break;
                                case User.State.waitbet:
                                {
                                    try
                                    {
                                        raise = int.Parse(message.Text);
                                    }
                                    catch
                                    {
                                        await Tgbot.SendTextMessageAsync(message.Chat.Id, "Введите число!");
                                        return;
                                    }
                                    if (raise > 1000)
                                    {
                                        await Tgbot.SendTextMessageAsync(message.Chat.Id, "Ставка не может быть больше 1000 коинов.");
                                        return;
                                    }
                                    if (raise > user.Money)
                                    {
                                        await Tgbot.SendTextMessageAsync(message.Chat.Id, "Недостаточно средств!");
                                        return;
                                    }
                                    if (raise < 2)/////////////////////////////////////////////////
                                    {
                                        await Tgbot.SendTextMessageAsync(message.Chat.Id, "Ставка должна быть больше 25 коинов.");
                                        return;
                                    }
                                    db = new DB();
                                        int raise1 = user.room.lastraise - user.lastraise + raise;
                                        Console.WriteLine(raise1);
                                        user.bet += raise1;
                                        user.room.lastraise += raise;
                                        user.room.bet += raise1;
                                        user.Money -= raise1;
                                        user.lastraise += raise1;
                                        if (user.Money == 0) user.room.allInUsers.Add(user);
                                        user.room.next = true;
                                        db.UpdateRange(user);
                                        await db.SaveChangesAsync();
                                        user.room.SendMessage($"Игрок {user.FirstName} повысил ставку на {raise} коинов.", user.room.players, null);
                                        break;
                                }
                                case User.State.waitmoney:
                                    try
                                    {
                                        raise = int.Parse(message.Text);
                                    }
                                    catch
                                    {
                                         await Tgbot.SendTextMessageAsync(message.Chat.Id, "Введите число!");
                                        return;
                                    }
                                    if (raise > 99999)
                                    {
                                         await Tgbot.SendTextMessageAsync(message.Chat.Id, "Слишком большая сумма!");
                                        return;
                                    }
                                    if (raise < 30)
                                    {
                                        await Tgbot.SendTextMessageAsync(message.Chat.Id, "Сумма должна быть больше 30 рублей!");
                                        return;
                                    }
                                    var billId = "";
                                    var pay_url = Operation.AddTransaction(raise, user, ref billId);
                                    if (pay_url == null)
                                    {
                                         await Tgbot.SendTextMessageAsync(message.Chat.Id, "Произошла ошибка. Попробуйте еще раз.");
                                        return;
                                    }
                                    await Tgbot.SendTextMessageAsync(message.Chat.Id, $"Пополнение счета на сумму {raise} р.\nДата: {DateTime.Now:dd.MMM.yyyy}\nСтатус: Не оплачено.\n\nОплатите счет по ссылке.\n{pay_url}", replyMarkup: new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Проверить оплату", $"bill_{billId}")));
                                    user.state = User.State.main;
                                    break;
                                case User.State.output:
                                    int money;
                                    try
                                    {
                                        money = int.Parse(message.Text);
                                    }
                                    catch
                                    {
                                         await Tgbot.SendTextMessageAsync(message.Chat.Id, "Введите число!");
                                        return;
                                    }
                                    if (money > user.Money)
                                    {
                                         await Tgbot.SendTextMessageAsync(message.Chat.Id, "Недостаточно средств.");
                                        return;
                                    }
                                    if (money < 50) //TO DO: Ограничение на вывод.
                                    {
                                         await Tgbot.SendTextMessageAsync(message.Chat.Id, "Вывод средств осуществляется от 50 рублей.");
                                        return;
                                    }
                                    user.output = money;
                                     await Tgbot.SendTextMessageAsync(message.Chat.Id, "Введите номер QIWI кошелька, на который будет осуществляться вывод.\nФормат: +<код страны><номер>");
                                    user.state = User.State.output_waitnumber;
                                    break;
                                case User.State.output_waitnumber:
                                    if (!message.Text.Contains("+"))
                                    {
                                         await Tgbot.SendTextMessageAsync(message.Chat.Id, "Номер должен начинаться с \"+\"");
                                        return;
                                    }
                                    bool success = Operation.OutputMoney(message.Text, user);
                                    user.output = 0;
                                    user.state = User.State.main;
                                    if (success)
                                    {
                                         await Tgbot.SendTextMessageAsync(message.Chat.Id, "Запрос отправлен.");
                                    }
                                    else
                                    {
                                         await Tgbot.SendTextMessageAsync(message.Chat.Id, "Произошла ошибка. Попробуйте позже.");
                                    }
                                    break;
                                case User.State.codprvt:
                                    int keyRoom;
                                    try
                                    {
                                        keyRoom = int.Parse(message.Text);
                                    }
                                    catch
                                    {
                                         await Tgbot.SendTextMessageAsync(message.Chat.Id, "Введите число!");
                                        return;
                                    }
                                    room = Operation.GetRoom(user.id_privateroom);
                                    if (room == null)
                                    {
                                         await Tgbot.SendTextMessageAsync(message.Chat.Id, "Комната не доступна для подключения. Возможно игра в ней уже началась.");
                                        user.id_privateroom = 0;
                                        return;
                                    }
                                    if (room.key != 0 && room.key == keyRoom)
                                    {
                                        room.AddPlayer(user, message.Chat.FirstName);
                                        user.id_privateroom = 0;
                                    }
                                    else
                                    {
                                         await Tgbot.SendTextMessageAsync(message.Chat.Id, "Пароль неверный.");
                                    }
                                    break;
                                case User.State.feedback:
                                    user.state = User.State.main;
                                    reviews.Enqueue($"{user.Id}:{message.Chat.FirstName} ({user.Id}): {message.Text}");
                                     await Tgbot.SendTextMessageAsync(message.Chat.Id, "Спасибо за отзыв. Мы рассмотрим его в ближайшее время.");
                                    break;
                                case User.State.chat:
                                    if (message.Text.Equals("Покинуть чат"))
                                    {
                                        user.state = User.State.main;
                                         await Tgbot.SendTextMessageAsync(message.Chat.Id, "Вы покинули чат.", replyMarkup: keyboard);
                                    }
                                    SendMessageToChat(message.Text, message.From.Username, user, null);
                                    break;
                                case User.State.answer:
                                    try
                                    {
                                         await Tgbot.SendTextMessageAsync(user.id_for_answer, $"Администратор {message.Chat.FirstName} ответил вам: {message.Text}");
                                         await Tgbot.SendTextMessageAsync(message.Chat.Id, "Сообщение отправлено!");
                                    }
                                    catch (Exception ex)
                                    {
                                         await Tgbot.SendTextMessageAsync(message.Chat.Id, $"Ошибка: {ex.Message} Сообщение не отправлено!");
                                    }
                                    user.state = User.State.admin;
                                    break;
                                case User.State.mailing:
                                    foreach (User user1 in users)
                                    {
                                         await Tgbot.SendTextMessageAsync(user1.Id, message.Text);
                                    }
                                    user.state = User.State.admin;
                                    break;
                                case User.State.add_coin:
                                    db = new DB();
                                    try
                                    {
                                        var x = message.Text.Split(':');
                                        User user1 = users.FirstOrDefault(y => y.Id == int.Parse(x[0]));
                                        if (user1 != null)
                                        {
                                            user1.AddMoney(int.Parse(x[1]));
                                            db.Update(user1);
                                        }

                                        await db.SaveChangesAsync();
                                    }
                                    catch (Exception ex)
                                    {
                                         await Tgbot.SendTextMessageAsync(message.Chat.Id, $"Ошибка: {ex.Message}");
                                    }
                                    user.state = User.State.admin;
                                    break;
                            }
                            break;
                    }
                }
                if (user.state == User.State.change_table)
                {
                    if (message.Type != MessageType.Photo)
                    {
                        await Tgbot.SendTextMessageAsync(message.Chat.Id, "Отправьте фотографию!");
                        return;
                    }
                    user.state = User.State.main;
                    await using (var ms = new MemoryStream())
                    {
                        await Tgbot.GetInfoAndDownloadFileAsync(message.Photo[message.Photo.Length - 1].FileId, ms);
                        Image image = Image.FromStream(ms);
                        var bmp = new Bitmap(image, 1590, 960);
                        bmp.Save($"tables\\{user.Id}.jpg");
                        bmp.Dispose();
                        image.Dispose();
                    }
                    await Tgbot.SendTextMessageAsync(message.Chat.Id, "Успешно.");
                }
            }
            catch (Exception ex) { reviews.Enqueue($"{e.Message.Chat.Id}:Ошибка у пользователя {e.Message.Chat.Id}: {ex.Message}\nОбъект, вызвавший исключение: {ex.Source}\nМетод, вызвавший исключение: {ex.TargetSite}"); }
        }

        private static  async void AddFriend(Telegram.Bot.Types.Message message, User user)
        {
            User info;
            await using DB db = new DB();
            try
            {
                string information = message.Text.Split(' ')[1];
                info = users.FirstOrDefault(x => x.Id == Int64.Parse(information.Substring(information.IndexOf('_') + 1)));
                if (info == null) return;
                if (info.Id == message.Chat.Id) return;
            }
            catch { return; }
            var f = db.Friendships.Where(friendship => (friendship.User1 == user.Id && friendship.User2 == info.Id) || (friendship.User1 == info.Id && friendship.User2 == user.Id));
            if (f.Any()) return;
            db.UpdateRange(user, info);
            Friendship frend = new Friendship() { User1 = user.Id, User2 = info.Id, Accepted = false };
            await db.Friendships.AddAsync(frend);
            await db.SaveChangesAsync();
            await Tgbot.SendTextMessageAsync(user.Id, $"Пользователю отправлена заявка в друзья.");
            InlineKeyboardMarkup addfrendkey = new InlineKeyboardMarkup(new List<InlineKeyboardButton>() { InlineKeyboardButton.WithCallbackData("Добавить", $"Add_{user.Id}"), InlineKeyboardButton.WithCallbackData("Отклонить", $"Remove_{user.Id}") });
            await Tgbot.SendTextMessageAsync(info.Id, $"Пользователь @{message.From.Username} отправил вам заявку в друзья.", replyMarkup: addfrendkey);
        }

        private static  async void SendMessageToChat(string message, string username, User user, IReplyMarkup markup)
        {
            foreach (User user1 in chat.ToList())
            {
                try
                {
                    if (user1 == user) continue;
                     await Tgbot.SendTextMessageAsync(user1.Id, $"@{username}: {message}", replyMarkup: markup);
                }
                catch {
                }
            }
        }
    }
}
