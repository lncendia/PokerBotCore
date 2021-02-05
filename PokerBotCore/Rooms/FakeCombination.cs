using System;
using System.Collections.Generic;
using System.Linq;

namespace PokerBotCore.Rooms
{
    internal class FakeCombination
    {
        readonly List<string> _nominal = new List<string>()
        {
            "Два", "Три", "Четыре", "Пять", "Шесть", "Семь", "Восемь", "Девять", "Десять", "Валет", "Дама", "Король",
            "Туз"
        };

        readonly List<string> _deck = new List<string>() {"♣", "♠", "♥", "♦"};
        readonly List<string> _botcards = new List<string>();
        readonly List<string> _cards = new List<string>();
        private static readonly Random Rnd = new Random();

        public FakeCombination(Combination.Comb combination, int count)
        {
            DoCards(combination, count);
        }

        void DoCards(Combination.Comb combination, int count)
        {
            var nominalCopy = _nominal.ToList();
            var deckCopy = _deck.ToList();

            switch (combination)
            {
                case Combination.Comb.flush:
                    string deck1, card, card2, deck2;
                    deck1 = deckCopy[Rnd.Next(0, deckCopy.Count)];
                    deckCopy.Remove(deck1);

                    card2 = nominalCopy[Rnd.Next(0, nominalCopy.Count)];
                    nominalCopy.Remove(card2);
                    _botcards.Add(card2 + $" {deck1}");
                    deck2 = deckCopy[Rnd.Next(0, deckCopy.Count)];
                    _botcards.Add(card2 + $" {deck2}");
                    nominalCopy.Remove("Пять");
                    nominalCopy.Remove("Десять");
                    for (int i = 0; i < (count - 1) * 2 + 1; i++)
                    {
                        card2 = nominalCopy[Rnd.Next(0, nominalCopy.Count)];
                        nominalCopy.Remove(card2);
                        _cards.Add($"{card2} {deckCopy[i % 4]}");
                    }

                    for (int i = 0; i < 4; i++)
                    {
                        card = nominalCopy[Rnd.Next(0, nominalCopy.Count)];
                        nominalCopy.Remove(card);
                        _cards.Add($"{card} {deck1}");
                    }

                    break;
                case Combination.Comb.kare:
                    card = nominalCopy[Rnd.Next(0, nominalCopy.Count)];
                    nominalCopy.Remove(card);
                    deck1 = deckCopy[Rnd.Next(0, deckCopy.Count)];
                    deckCopy.Remove(deck1);
                    _botcards.Add(card + $" {deck1}");
                    deck1 = deckCopy[Rnd.Next(0, deckCopy.Count)];
                    deckCopy.Remove(deck1);
                    _botcards.Add(card + $" {deck1}");

                    deck1 = deckCopy[Rnd.Next(0, deckCopy.Count)];
                    deckCopy.Remove(deck1);
                    deck2 = deckCopy[Rnd.Next(0, deckCopy.Count)];

                    nominalCopy.Remove("Пять");
                    nominalCopy.Remove("Десять");
                    for (int i = 0; i < (count - 1) * 2 + 3; i++)
                    {
                        card2 = nominalCopy[Rnd.Next(0, nominalCopy.Count)];
                        nominalCopy.Remove(card2);
                        _cards.Add($"{card2} {_deck[i % 4]}");
                    }

                    _cards.Add(card + $" {deck2}");
                    _cards.Add(card + $" {deck1}");
                    break;
                case Combination.Comb.straight:

                    break;
                case Combination.Comb.twoPair:
                    card = nominalCopy[Rnd.Next(0, nominalCopy.Count)];
                    nominalCopy.Remove(card);
                    card2 = nominalCopy[Rnd.Next(0, nominalCopy.Count)];
                    nominalCopy.Remove(card2);
                    deck1 = deckCopy[Rnd.Next(0, deckCopy.Count)];
                    deckCopy.Remove(deck1);
                    _botcards.Add(card + $" {deck1}");
                    deck1 = deckCopy[Rnd.Next(0, deckCopy.Count)];
                    _botcards.Add(card2 + $" {deck1}");

                    deckCopy = _deck.ToList();
                    deck1 = deckCopy[Rnd.Next(0, deckCopy.Count)];
                    deckCopy.Remove(deck1);
                    deck2 = deckCopy[Rnd.Next(0, deckCopy.Count)];

                    nominalCopy.Remove("Пять");
                    nominalCopy.Remove("Десять");
                    for (int i = 0; i < (count - 1) * 2 + 3; i++)
                    {
                        string card1 = nominalCopy[Rnd.Next(0, nominalCopy.Count)];
                        nominalCopy.Remove(card1);
                        _cards.Add($"{card1} {_deck[i % 4]}");
                    }

                    _cards.Add(card + $" {deck2}");
                    _cards.Add(card2 + $" {deck1}");
                    break;
                case Combination.Comb.set:
                    card = nominalCopy[Rnd.Next(0, nominalCopy.Count)];
                    nominalCopy.Remove(card);
                    deck1 = deckCopy[Rnd.Next(0, deckCopy.Count)];
                    deckCopy.Remove(deck1);
                    _botcards.Add(card + $" {deck1}");
                    deck1 = deckCopy[Rnd.Next(0, deckCopy.Count)];
                    deckCopy.Remove(deck1);
                    _botcards.Add(card + $" {deck1}");
                    deck1 = deckCopy[Rnd.Next(0, deckCopy.Count)];

                    nominalCopy.Remove("Пять");
                    nominalCopy.Remove("Десять");
                    for (int i = 0; i < (count - 1) * 2 + 4; i++)
                    {
                        card2 = nominalCopy[Rnd.Next(0, nominalCopy.Count)];
                        nominalCopy.Remove(card2);
                        _cards.Add($"{card2} {_deck[i % 4]}");
                    }

                    _cards.Add(card + $" {deck1}");
                    break;
            }
        }

        public void GetCards(List<string> table, List<string> botCards)
        {
            table.Clear();
            table.AddRange(_cards);
            botCards.Clear();
            botCards.AddRange(this._botcards);
        }
    }
}
