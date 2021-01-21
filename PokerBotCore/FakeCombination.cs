using System;
using System.Collections.Generic;
using System.Linq;

namespace PokerBotCore
{
    class FakeCombination
    {
        List<string> nominal = new List<string>() { "Два", "Три", "Четыре", "Пять", "Шесть", "Семь", "Восемь", "Девять", "Десять", "Валет", "Дама", "Король", "Туз" };
        List<string> deck = new List<string>() { "♣", "♠", "♥", "♦" };
        List<string> botcards = new List<string>();
        List<string> cards = new List<string>();
        static Random rnd = new Random();
        public FakeCombination(Combination.Comb combination, int count)
        {
            DoCards(combination, count);
        }
        void DoCards(Combination.Comb combination, int count)
        {
            List<string> nominalCopy = nominal.ToList();
            List<string> deckCopy = deck.ToList();

            switch (combination)
            {
                case Combination.Comb.flush:
                    string deck1, card, card2, deck2;
                    deck1 = deckCopy[rnd.Next(0, deckCopy.Count)];
                    deckCopy.Remove(deck1);

                    card2 = nominalCopy[rnd.Next(0, nominalCopy.Count)];
                    nominalCopy.Remove(card2);
                    botcards.Add(card2 + $" {deck1}");
                    deck2 = deckCopy[rnd.Next(0, deckCopy.Count)];
                    botcards.Add(card2 + $" {deck2}");
                    nominalCopy.Remove("Пять");
                    nominalCopy.Remove("Десять");
                    for (int i = 0; i < (count - 1) * 2 + 1; i++)
                    {
                        card2 = nominalCopy[rnd.Next(0, nominalCopy.Count)];
                        nominalCopy.Remove(card2);
                        cards.Add($"{card2} {deckCopy[i % 4]}");
                    }
                    for (int i = 0; i < 4; i++)
                    {
                        card = nominalCopy[rnd.Next(0, nominalCopy.Count)];
                        nominalCopy.Remove(card);
                        cards.Add($"{card} {deck1}");
                    }
                    break;
                case Combination.Comb.kare:
                    card = nominalCopy[rnd.Next(0, nominalCopy.Count)];
                    nominalCopy.Remove(card);
                    deck1 = deckCopy[rnd.Next(0, deckCopy.Count)];
                    deckCopy.Remove(deck1);
                    botcards.Add(card + $" {deck1}");
                    deck1 = deckCopy[rnd.Next(0, deckCopy.Count)];
                    deckCopy.Remove(deck1);
                    botcards.Add(card + $" {deck1}");

                    deck1 = deckCopy[rnd.Next(0, deckCopy.Count)];
                    deckCopy.Remove(deck1);
                    deck2 = deckCopy[rnd.Next(0, deckCopy.Count)];

                    nominalCopy.Remove("Пять");
                    nominalCopy.Remove("Десять");
                    for (int i = 0; i < (count - 1) * 2 + 3; i++)
                    {
                        card2 = nominalCopy[rnd.Next(0, nominalCopy.Count)];
                        nominalCopy.Remove(card2);
                        cards.Add($"{card2} {deck[i % 4]}");
                    }
                    cards.Add(card + $" {deck2}");
                    cards.Add(card + $" {deck1}");
                    break;
                case Combination.Comb.straight:

                    break;
                case Combination.Comb.twopair:
                    card = nominalCopy[rnd.Next(0, nominalCopy.Count)];
                    nominalCopy.Remove(card);
                    card2 = nominalCopy[rnd.Next(0, nominalCopy.Count)];
                    nominalCopy.Remove(card2);
                    deck1 = deckCopy[rnd.Next(0, deckCopy.Count)];
                    deckCopy.Remove(deck1);
                    botcards.Add(card + $" {deck1}");
                    deck1 = deckCopy[rnd.Next(0, deckCopy.Count)];
                    botcards.Add(card2 + $" {deck1}");

                    deckCopy = deck.ToList();
                    deck1 = deckCopy[rnd.Next(0, deckCopy.Count)];
                    deckCopy.Remove(deck1);
                    deck2 = deckCopy[rnd.Next(0, deckCopy.Count)];

                    nominalCopy.Remove("Пять");
                    nominalCopy.Remove("Десять");
                    for (int i = 0; i < (count - 1) * 2 + 3; i++)
                    {
                        string card1 = nominalCopy[rnd.Next(0, nominalCopy.Count)];
                        nominalCopy.Remove(card1);
                        cards.Add($"{card1} {deck[i % 4]}");
                    }
                    cards.Add(card + $" {deck2}");
                    cards.Add(card2 + $" {deck1}");
                    break;
                case Combination.Comb.set:
                    card = nominalCopy[rnd.Next(0, nominalCopy.Count)];
                    nominalCopy.Remove(card);
                    deck1 = deckCopy[rnd.Next(0, deckCopy.Count)];
                    deckCopy.Remove(deck1);
                    botcards.Add(card + $" {deck1}");
                    deck1 = deckCopy[rnd.Next(0, deckCopy.Count)];
                    deckCopy.Remove(deck1);
                    botcards.Add(card + $" {deck1}");
                    deck1 = deckCopy[rnd.Next(0, deckCopy.Count)];

                    nominalCopy.Remove("Пять");
                    nominalCopy.Remove("Десять");
                    for (int i = 0; i < (count - 1) * 2 + 4; i++)
                    {
                        card2 = nominalCopy[rnd.Next(0, nominalCopy.Count)];
                        nominalCopy.Remove(card2);
                        cards.Add($"{card2} {deck[i % 4]}");
                    }
                    cards.Add(card + $" {deck1}");
                    break;
            }
        }
        public void GetCards(List<string> table, List<string> botcards)
        {
            table.Clear();
            table.AddRange(cards);
            botcards.Clear();
            botcards.AddRange(this.botcards);
        }
    }
}
