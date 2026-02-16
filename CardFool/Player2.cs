using System.Collections.Generic;
using System.Linq;

namespace CardFool
{
    public class MPlayer2
    {
        private int countBittedCards = 0;
        private string Name = "TestPlayer";
        private List<SCard> hand = new List<SCard>();
        private SCard Trump;

        public string GetName() => Name;

        public int GetCount() => hand.Count;

        public void AddToHand(SCard card)
        {
            hand.Add(card);
        }

        public void SetTrump(SCard NewTrump)
        {
            Trump = NewTrump;
        }

        // Самая простая и стабильная стратегия:
        // Минимальная не козырная, если нет — минимальный козырь
        public List<SCard> LayCards()
        {
            if (hand.Count == 0)
                return new List<SCard>();

            SCard? bestNonTrump = null;
            SCard bestTrump = hand[0];

            foreach (var card in hand)
            {
                if (card.Suit != Trump.Suit)
                {
                    if (bestNonTrump == null || card.Rank < bestNonTrump.Value.Rank)
                        bestNonTrump = card;
                }
                else if (card.Rank < bestTrump.Rank)
                {
                    bestTrump = card;
                }
            }

            SCard chosen = bestNonTrump ?? bestTrump;
            hand.Remove(chosen);
            return new List<SCard> { chosen };
        }

        // Бьём минимальной возможной картой
        public bool Defend(List<SCardPair> table)
        {
            // Сначала ТОЛЬКО ищем карты, не трогая table и hand
            Dictionary<int, int> chosen = new Dictionary<int, int>();

            for (int i = 0; i < table.Count; i++)
            {
                if (table[i].Beaten)
                    continue;

                int bestIndex = -1;
                int bestRank = int.MaxValue;

                for (int j = 0; j < hand.Count; j++)
                {
                    if (chosen.Values.Contains(j))
                        continue;

                    if (SCard.CanBeat(table[i].Down, hand[j], Trump.Suit))
                    {
                        if (hand[j].Rank < bestRank)
                        {
                            bestRank = hand[j].Rank;
                            bestIndex = j;
                        }
                    }
                }

                // Если хотя бы одну карту не можем побить – сразу отмена,
                // НИЧЕГО не меняя
                if (bestIndex == -1)
                    return false;

                chosen[i] = bestIndex;
            }

            // Только если МЫ УВЕРЕНЫ, что можем отбиться полностью,
            // начинаем реально изменять состояние

            foreach (var kv in chosen)
            {
                int pairIndex = kv.Key;
                int handIndex = kv.Value;

                var pair = table[pairIndex];
                pair.SetUp(hand[handIndex], Trump.Suit);
                table[pairIndex] = pair;
            }

            // Удаляем использованные карты из руки
            foreach (int idx in chosen.Values.OrderByDescending(x => x))
                hand.RemoveAt(idx);

            return true;
        }

        private int getCountCards(List<SCardPair> table)
        {
            int countCards = 0;
            foreach (var pair in table)
            {
                ++countCards;
                if (pair.Beaten) ++countCards;
            }

            return countCards;
        }

        public bool AddCards(List<SCardPair> table, bool opponentDefensed)
        {
            if (!opponentDefensed || table.Count >= 6 || hand.Count == 0)
                return false;

            if ((36 - countBittedCards - hand.Count - getCountCards(table)) <= 0)
                return false;

            // Собираем допустимые ранги
            HashSet<int> ranksOnTable = new HashSet<int>();
            foreach (var pair in table)
            {
                ranksOnTable.Add(pair.Down.Rank);
                if (pair.Beaten)
                    ranksOnTable.Add(pair.Up.Rank);
            }

            // Ищем минимальную подходящую карту
            int bestIndex = -1;
            int bestRank = int.MaxValue;

            for (int i = 0; i < hand.Count; i++)
            {
                if (ranksOnTable.Contains(hand[i].Rank))
                {
                    if (hand[i].Rank < bestRank)
                    {
                        bestRank = hand[i].Rank;
                        bestIndex = i;
                    }
                }
            }

            if (bestIndex == -1)
                return false;

            table.Add(new SCardPair(hand[bestIndex]));
            hand.RemoveAt(bestIndex);
            return true;
        }

        public void OnEndRound(List<SCardPair> table, bool isDefenseSuccessful)
        {
            // Обновляем бито
            if (isDefenseSuccessful) countBittedCards += getCountCards(table);
        }
    }
}