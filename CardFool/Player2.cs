using System;
using System.Collections.Generic;
using System.Linq;

namespace CardFool
{
    public class MPlayer2
    {
        private enum RoundAttacker { ME, OPPONENT }

        private List<SCard> hand = new List<SCard>();
        private Suits trumpSuit;
        private int countBittedCards = 0;

        private List<int> GetTableRanks(List<SCardPair> table)
        {
            List<int> ranks = new List<int>();
            foreach (var pair in table)
            {
                if (!ranks.Contains(pair.Down.Rank)) ranks.Add(pair.Down.Rank);
                if (pair.Beaten && !ranks.Contains(pair.Up.Rank)) ranks.Add(pair.Up.Rank);
            }
            return ranks;
        }

        public string GetName() => "Bot";

        public int GetCount() => hand.Count;

        public void AddToHand(SCard card)
        {
            hand.Add(card);
        }

        public void SetTrump(SCard newTrump)
        {
            trumpSuit = newTrump.Suit;
        }

        // самая простая и стабильная стратегия:
        // минимальная не козырная, если нет - минимальный козырь
        public List<SCard> LayCards()
        {
            if (hand.Count == 0)
                return new List<SCard>();

            int notTrumpCardIdx = -1;
            int trumpCardIdx = -1;

            for (int i = 0; i < hand.Count; i++)
            {
                SCard card = hand[i];

                if (card.Suit != trumpSuit)
                {
                    if (notTrumpCardIdx == -1 || card.Rank < hand[notTrumpCardIdx].Rank)
                        notTrumpCardIdx = i;
                }
                else if (trumpCardIdx == -1 || card.Rank < hand[trumpCardIdx].Rank)
                {
                    trumpCardIdx = i;
                }
            }

            int selectedCardIdx = notTrumpCardIdx != -1 ? notTrumpCardIdx : trumpCardIdx;
            SCard selectedCard = hand[selectedCardIdx];
            hand.RemoveAt(selectedCardIdx);

            return new List<SCard> { selectedCard };
        }

        // бьём минимальной возможной картой
        public bool Defend(List<SCardPair> table)
        {
            Dictionary<int, int> selected = new Dictionary<int, int>();

            for (int i = 0; i < table.Count; i++)
            {
                if (table[i].Beaten)
                    continue;

                int selectedIndex = -1;
                int selectedRank = int.MaxValue;

                for (int j = 0; j < hand.Count; j++)
                {
                    if (selected.Values.Contains(j))
                        continue;

                    if (SCard.CanBeat(table[i].Down, hand[j], trumpSuit))
                    {
                        if (hand[j].Rank < selectedRank)
                        {
                            selectedRank = hand[j].Rank;
                            selectedIndex = j;
                        }
                    }
                }

                if (selectedIndex == -1) return false;
                selected[i] = selectedIndex;
            }

            foreach (var selectedPair in selected)
            {
                int pairIndex = selectedPair.Key;
                int handIndex = selectedPair.Value;

                var pair = table[pairIndex];
                pair.SetUp(hand[handIndex], trumpSuit);
                table[pairIndex] = pair;
            }

            foreach (int idx in selected.Values.OrderByDescending(x => x))
                hand.RemoveAt(idx);

            return true;
        }

        private int GetCountCards(List<SCardPair> table)
        {
            int count = 0;
            foreach (var pair in table)
            {
                count++;
                if (pair.Beaten) count++;
            }
            return count;
        }

        public bool AddCards(List<SCardPair> table, bool opponentDefensed)
        {
            if (!opponentDefensed || table.Count >= 6 || hand.Count == 0)
                return false;
            if ((36 - countBittedCards - hand.Count - GetCountCards(table)) <= 0)
                return false;

            List<int> ranksOnTable = GetTableRanks(table);

            int selectedIndex = -1;
            int selectedRank = int.MaxValue;

            for (int i = 0; i < hand.Count; i++)
            {
                if (ranksOnTable.Contains(hand[i].Rank))
                {
                    if (hand[i].Rank < selectedRank)
                    {
                        selectedRank = hand[i].Rank;
                        selectedIndex = i;
                    }
                }
            }

            if (selectedIndex == -1) return false;

            table.Add(new SCardPair(hand[selectedIndex]));
            hand.RemoveAt(selectedIndex);
            return true;
        }

        public void OnEndRound(List<SCardPair> table, bool isDefenseSuccessful)
        {
            if (isDefenseSuccessful) countBittedCards += GetCountCards(table);
        }
    }
}