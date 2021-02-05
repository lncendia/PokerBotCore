using System;
using System.Collections.Generic;
using PokerBotCore.Entities;
using PokerBotCore.Rooms;

namespace PokerBotCore.Bot
{
    public static class BuilderFaceRooms
    {
        private static Random Rnd = new();
        public static FakeRoom CreateFakeRoom(int count)
        {
            User user = new User {Id = 0, Money = int.MaxValue, state = User.State.wait, firstName = CreateNickname()};
            Room room = new FakeRoom(user, count);
            user.room = room;
            return (FakeRoom) Operations.CreateRoom(count, user, false, true);
        }

        private const string A = "bcdfghjklmnpqrstvwxz";
        private const string B = "aeiouy";
        private const string Emoji = "";
        static List<string> c = new() { A, B };
        static readonly List<string> NameRus = new() { "Александр", "Алексей", "Анатолий", "Андрей", "Антон", "Аркадий", "Арсений", "Артём", "Артур", "Борис", "Вадим", "Валентин", "Валерий", "Василий", "Виктор", "Виталий", "Владимир", "Владислав", "Вячеслав", "Глеб", "Даниил", "Денис", "Дмитрий", "Евгений", "Егор", "Иван", "Игорь", "Илья", "Кирилл", "Константин", "Максим", "Марк", "Матвей", "Михаил", "Никита", "Олег", "Павел", "Пётр", "Роман", "Руслан", "Сергей", "Степан", "Тимур", "Юрец", "Ярик" };
        static readonly List<string> NameEng = new() { "Aleksandr", "Aleksey", "Anatoliy", "Andrey", "Anton", "Arkadiy", "Arseniy", "Artyom", "Artur", "Boris", "Vadim", "Valentin", "Valeriy", "Vasiliy", "Viktor", "Vitaliy", "Vladimir", "Vladislav", "Vyacheslav", "Gleb", "Daniil", "Denis", "Dmitriy", "Evgeniy", "Egor", "Ivan", "Igor", "Il'ya", "Kirill", "Konstantin", "Maksim", "Mark", "Matvey", "Mihail", "Nikita", "Oleg", "Pavel", "Pyotr", "Roma", "Ruslan", "Sergey", "Stepan", "Timur", "Yura", "Yarik" };

        private static string CreateNickname()
        {
            string pass = "";
            switch (Rnd.Next(0, 3))
            {
                case 0:
                    int r = Rnd.Next(0, 2);
                    pass += char.ToUpper(c[r][Rnd.Next(0, c[r].Length)]);
                    int count = Rnd.Next(3, 6);
                    for (int i = r + 1; i < count + r + 1; i++)
                    {
                        pass += c[i % 2][Rnd.Next(0, c[i % 2].Length)];
                    }
                    break;
                case 1:
                    pass = NameRus[Rnd.Next(0, NameRus.Count)];
                    break;
                case 2:
                    pass = NameEng[Rnd.Next(0, NameEng.Count)];
                    break;
            }
            pass += Emoji;
            //if (Rnd.Next(0, 3) == 2) pass += Emoji[Rnd.Next(0, Emoji.Length)];
            return pass;
        }
    }
}