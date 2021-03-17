using System;
using PokerBotCore.Bot;
using PokerBotCore.Rooms;
using Telegram.Bot.Types;

namespace PokerBotCore
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            MainBot.Start();
            while (true)
            {
                var x = Console.ReadLine();
                switch (x)
                {
                    case "2":
                    {
                        Console.WriteLine("Введите колличество игроков");
                        int count = int.Parse(Console.ReadLine() ?? string.Empty);
                        break;
                    }
                }
            }

        }
    }
}
