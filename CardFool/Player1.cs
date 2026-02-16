using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CardFool
{
    public class MPlayer1
    {
        private enum RoundAttacker { ME, OPPONENT }

        private List<SCard> hand = new List<SCard>(); // карты на руке
        private List<SCard> opponentHand = new List<SCard>(); // предполагаемые карты на руке оппонента
        private RoundAttacker roundAttacker;
        private Suits trumpSuit; // козырь
        private Queue<SCard> batchMemoCards = new Queue<SCard>();
        private int countBittedCards = 0;
        private int deckRemaining = 36 - 12;  // сколько карт осталось в колоде (включая козырь)
        private int opponentCurrentCountCards = 6; // количество карт оппонента


        // инициализация потенциальных карт в руке оппонента. изначально оппонент может иметь любые карты
        private void InitOpponentHand()
        {
            for (int rank = 6; rank < 15; rank++)
            {
                opponentHand.AddRange(new List<SCard> {
                         new SCard(Suits.Clubs, rank),
                         new SCard(Suits.Diamonds, rank),
                         new SCard(Suits.Hearts, rank),
                         new SCard(Suits.Spades, rank)
                    });
            }
        }

        // получаем ранги карт на столе
        private HashSet<int> GetRanksOnTable(List<SCardPair> table)
        {
            HashSet<int> ranks = new HashSet<int>();

            table.ForEach(pair =>
            {
                ranks.Add(pair.Down.Rank);
                if (pair.Beaten) ranks.Add(pair.Up.Rank);
            });

            return ranks;
        }

        // проверка "может ли оппонент отбиться?"
        private bool OpponentCanBeat(SCard myCard)
        {
            return opponentHand.Exists(card => SCard.CanBeat(myCard, card, trumpSuit));
        }

        // возвращает имя игрока
        public string GetName()
        {
            return "Mikhail";
        }

        // возвращает количество карт на руке
        public int GetCount()
        {
            return hand.Count;
        }

        // добавление карты в руку, во время добора из колоды, или взятия карт
        public void AddToHand(SCard card)
        {
            hand.Add(card);
            opponentHand.Remove(card);
        }

        // начальная атака
        public List<SCard> LayCards()
        {
            roundAttacker = RoundAttacker.ME;

            if (hand.Count == 0) return new List<SCard>();

            SCard? bestNonTrump = null;
            SCard? bestTrump = null;
            SCard? chosen = null;

            if (opponentHand.Count <= 6) // если мы точно знаем карты соперника
            {
                foreach (var card in hand)
                {
                    if (card.Suit != trumpSuit && !OpponentCanBeat(card)) // соперник не может отбиться от некозырной карты
                    {
                        if (!bestNonTrump.HasValue || card.Rank < bestNonTrump.Value.Rank)
                            bestNonTrump = card;
                    }
                    else
                    {
                        if (
                             (!bestTrump.HasValue ||
                             card.Rank < bestTrump.Value.Rank) &&
                             !OpponentCanBeat(card) // соперник не может отбиться от козырной карты
                        )
                            bestTrump = card;
                    }
                }

                chosen = bestNonTrump ?? bestTrump;

                if (chosen.HasValue)
                {
                    hand.Remove(chosen.Value);
                    return new List<SCard> { chosen.Value };
                }
            }

            List<SCard> filteredHand = countBittedCards <= 18 ? // в первой половине игры придерживаем козыри
                           hand.Where(card => card.Suit != trumpSuit).ToList() :
                           hand;
            // ищем пары/тройки/четверки одного ранга
            List<SCard>? cardPairGroup = filteredHand
                           .GroupBy(card => card.Rank) // группируем по рангу
                           .OrderBy(cardGroup => cardGroup.Key) // сортируем по рангу по возрастанию
                           .FirstOrDefault(cardGroup => cardGroup.Count() > 1 && cardGroup.Count() <= opponentCurrentCountCards) // первая группа, где количество карт больше одной
                           ?.ToList() ?? new List<SCard>();

            if (cardPairGroup.Count > 1)
            {
                SCard first = cardPairGroup[0];

                cardPairGroup.RemoveAt(0); // ходим по одной
                hand.Remove(first);
                batchMemoCards = new Queue<SCard>(cardPairGroup.ToList()); // запоминаем карты для подкидывания

                return new List<SCard> { first };
            }

            bestNonTrump = null;
            bestTrump = null;

            // выбираем минимальную не козырную карту и минимальную козырь для хода
            foreach (var card in hand)
            {
                if (card.Suit != trumpSuit)
                {
                    if (!bestNonTrump.HasValue || card.Rank < bestNonTrump.Value.Rank)
                        bestNonTrump = card;
                }
                else
                {
                    if (!bestTrump.HasValue || card.Rank < bestTrump.Value.Rank)
                        bestTrump = card;
                }
            }

            chosen = bestNonTrump ?? bestTrump; // лучше сходит не козырной
            if (chosen.HasValue)
            {
                hand.Remove(chosen.Value);
                return new List<SCard> { chosen.Value };
            }

            return new List<SCard>();
        }

        // защита от карт
        // на вход подается набор карт на столе, часть из них могут быть уже покрыты
        public bool Defend(List<SCardPair> table)
        {
            roundAttacker = RoundAttacker.OPPONENT;

            if (hand.Count == 0) return false;

            Dictionary<int, int> chosenCards = new Dictionary<int, int>();

            for (int i = 0; i < table.Count; i++)
            {
                if (table[i].Beaten) continue;

                // выбираем лучшую карту, чтобы побиться
                int bestIndex = -1;
                int bestRank = int.MaxValue;
                bool bestIsTrump = false;


                for (int j = 0; j < hand.Count; j++)
                {
                    if (chosenCards.Values.Contains(j)) continue; // если карта уже побита
                    if (!SCard.CanBeat(table[i].Down, hand[j], trumpSuit)) continue; // если не можем побить данную карту

                    bool isTrump = hand[j].Suit == trumpSuit;

                    if (isTrump && countBittedCards <= 12) continue; // в первой трети игры придерживаем козыри

                    // предпочитаем некозырные карты козырям
                    if (bestIndex == -1 ||
                        (!isTrump && bestIsTrump) || // если карта не козырная, но "лучшая" - козырная
                        (!isTrump && !bestIsTrump && hand[j].Rank < bestRank) || // если выбираем среди некозырных, но эта карта ниже рангом
                        (isTrump && bestIsTrump && hand[j].Rank < bestRank)) // если выбираем среди козырных, но эта карта ниже рангом
                    {
                        bestIndex = j;
                        bestRank = hand[j].Rank;
                        bestIsTrump = isTrump;
                    }
                }

                if (bestIndex == -1) return false;

                chosenCards[i] = bestIndex;
            }

            foreach (var cardIndexPair in chosenCards)
            {
                int pairIndex = cardIndexPair.Key;
                int handIndex = cardIndexPair.Value;

                SCardPair pair = table[pairIndex];
                pair.SetUp(hand[handIndex], trumpSuit); // бьемся
                table[pairIndex] = pair;
            }

            // удаляем карты из руки от большего к меньшему
            foreach (int idx in chosenCards.Values.OrderByDescending(x => x))
                hand.RemoveAt(idx);

            return true;
        }

        // добавление карт
        // на вход подается набор карт на столе, а также отбился ли оппонент
        public bool AddCards(List<SCardPair> table, bool opponentDefensed)
        {
            if (table.Count >= Math.Min(opponentCurrentCountCards, 6)) // не можем подкидывать больше, чем есть у оппонента
                return false;

            if (batchMemoCards.Count > 0) // если активна стратегия пар/двоек/троек
            {
                SCard card = batchMemoCards.Dequeue(); // снова ходим по одной, берем первую карту в очереди

                hand.Remove(card);
                table.Add(new SCardPair(card));

                return true;
            }


            HashSet<int> ranksOnTable = GetRanksOnTable(table);

            // ищем минимальную подходящую карту
            int bestIndex = -1;
            int bestRank = int.MaxValue;
            bool bestCanBeat = true;

            for (int i = 0; i < hand.Count; i++)
            {
                if (hand[i].Suit == trumpSuit && countBittedCards <= 18) continue; // в первой половине игры придерживаем козыри

                if (ranksOnTable.Contains(hand[i].Rank)) // если можем подкинуть
                {
                    if (opponentHand.Count <= 6 && OpponentCanBeat(hand[i])) // если знаем точные карты оппонента и можем отбиться
                    {
                        if (bestCanBeat || hand[i].Rank < bestRank) // если лучшую карту могут отбить или есть карта меньшего ранга, которую нельзя отбить 
                        {
                            bestRank = hand[i].Rank;
                            bestIndex = i;
                            bestCanBeat = false;
                        }
                    }
                    else if (hand[i].Rank < bestRank)
                    {
                        bestRank = hand[i].Rank;
                        bestIndex = i;
                    }
                }
            }

            if (bestIndex == -1) return false;

            table.Add(new SCardPair(hand[bestIndex]));
            hand.RemoveAt(bestIndex);

            return true;
        }

        public void OnEndRound(List<SCardPair> table, bool isDefenseSuccessful)
        {
            int attackCards = table.Count; // сколько атакующих карт лежит снизу

            if (isDefenseSuccessful) countBittedCards += attackCards * 2; // если защита успешна, увеличиваем бито

            if (roundAttacker == RoundAttacker.ME)
            {
                // если соперник успешно защитился
                if (isDefenseSuccessful) opponentCurrentCountCards -= attackCards;
                else opponentCurrentCountCards += attackCards; // соперник взял
            }
            else opponentCurrentCountCards -= attackCards; // если соперник атаковал

            if (opponentCurrentCountCards < 0) opponentCurrentCountCards = 0;

            // удаляем карты, которые вышли из игры, из возможной руки соперника
            foreach (var pair in table)
            {
                opponentHand.Remove(pair.Down);
                if (pair.Beaten) opponentHand.Remove(pair.Up);
            }

            // сколько карт нужно добрать мне и сопернику
            int myNeed = Math.Max(0, 6 - hand.Count);
            int oppNeed = Math.Max(0, 6 - opponentCurrentCountCards);

            int oppDraw = 0;

            if (deckRemaining > 0)
            {
                if (roundAttacker == RoundAttacker.ME)
                {
                    deckRemaining -= Math.Min(myNeed, deckRemaining);
                    // если атаковали первыми, то добираем первым
                    // добираем сколько необходимо или сколько осталось

                    oppDraw = Math.Min(oppNeed, deckRemaining);
                    deckRemaining -= oppDraw; // аналогично добирает соперник
                }
                else
                {
                    oppDraw = Math.Min(oppNeed, deckRemaining);
                    deckRemaining -= oppDraw; // первый добирает соперник
                    deckRemaining -= Math.Min(myNeed, deckRemaining); // потом добираю я
                }
            }

            opponentCurrentCountCards += oppDraw;

            // сбрасываем память для стратегии двоек/троек/четверок
            batchMemoCards.Clear();
        }


        // Установка козыря, на вход подаётся козырь, вызывается перед первой раздачей карт
        public void SetTrump(SCard newTrump)
        {
            trumpSuit = newTrump.Suit;
            InitOpponentHand();
        }
    }
}
