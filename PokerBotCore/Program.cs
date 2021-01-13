using System;

namespace PokerBot
{
    class Program
    {
        static void Main(string[] args)
        {
            Bot.Start();
            while (true)
            {
                Console.WriteLine("1 - Проверка комнат пользователей.");
                var x = Console.ReadLine();
                if (x == "1")
                {
                    Console.WriteLine(Bot.roomsfortest);
                }
                if (x == "2")
                {
                    Console.WriteLine("Введите колличество игроков");
                    int count = int.Parse(Console.ReadLine());
                    Operation.CreateFakeRoom(count);
                }
            }

        }
    }
}
