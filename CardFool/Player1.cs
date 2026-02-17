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
        private RoundAttacker roundAttacker; // кто атакует в текущем раунде
        private Suits trumpSuit; // козырная масть
        // запоминаем карты для стратегии двойки/тройки/четверки
        private Queue<KeyValuePair<int, SCard>> batchMemoCards = new Queue<KeyValuePair<int, SCard>>();
        private int countBittedCards = 0; // количество карт в бито
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
        private List<int> GetTableRanks(List<SCardPair> table)
        {
            List<int> ranks = new List<int>(); // оптимальнее использовать HashSet

            table.ForEach(pair =>
            {
                if (!ranks.Contains(pair.Down.Rank)) ranks.Add(pair.Down.Rank);
                if (pair.Beaten && !ranks.Contains(pair.Up.Rank)) ranks.Add(pair.Up.Rank);
            });

            return ranks;
        }

        // проверка "может ли оппонент отбиться?"
        private bool OpponentCanBeat(SCard myCard)
        {
            return opponentHand.Exists(card => SCard.CanBeat(myCard, card, trumpSuit));
        }

        // играем стратегию двоек/троек/четверок
        private SCard? PlayBatchStrategy(List<SCard>? hand = null)
        {
            if (hand == null) hand = this.hand;

            List<SCard> filteredHand = countBittedCards <= 18 ? // в первой половине игры придерживаем козыри
                           hand.Where(card => card.Suit != trumpSuit).ToList() :
                           hand;
            // ищем пары/тройки/четверки одного ранга
            List<SCard> cardPairGroup = filteredHand
                           .GroupBy(card => card.Rank) // группируем по рангу
                           .OrderBy(cardGroup => cardGroup.Key) // сортируем по рангу по возрастанию
                           .FirstOrDefault(cardGroup => cardGroup.Count() > 1 && cardGroup.Count() <= opponentCurrentCountCards) // первая группа, где количество карт больше одной
                           ?.ToList() ?? new List<SCard>();

            if (cardPairGroup.Count > 1)
            {
                SCard first = cardPairGroup[0];

                cardPairGroup.RemoveAt(0); // ходим по одной
                hand.Remove(first);
                batchMemoCards.Clear();

                // запоминаем карты для подкидывания
                for (int i = 0; i < cardPairGroup.Count; i++)
                    batchMemoCards.Append(new KeyValuePair<int, SCard>(i, cardPairGroup[i]));

                return first;
            }

            return null;
        }

        // возвращает имя игрока
        public string GetName() => "Mikhail";

        // возвращает количество карт на руке
        public int GetCount() => hand.Count;

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

            int notTrumpCardIdx = -1; // индекс в таблице и сама карта для быстрого удаления
            int trumpCardIdx = -1;
            int selectedCardIdx = -1; // итоговая карта, которой будем ходить

            if (opponentHand.Count <= 6) // стратегия 1. если мы точно знаем карты соперника
            {
                for (int i = 0; i < hand.Count; i++)
                {
                    SCard card = hand[i];

                    // если соперник не может отбиться от некозырной карты
                    if (
                        card.Suit != trumpSuit &&
                        !OpponentCanBeat(card) &&
                        (notTrumpCardIdx == -1 || card.Rank < hand[notTrumpCardIdx].Rank)
                    )
                    {
                        notTrumpCardIdx = i;
                    }
                    // если соперник не может отбиться от козырной карты
                    else if (
                        (trumpCardIdx == -1 ||
                        card.Rank < hand[trumpCardIdx].Rank) &&
                        !OpponentCanBeat(card)
                    )
                    {
                        trumpCardIdx = i;
                    }
                }

                selectedCardIdx = notTrumpCardIdx != -1 ? notTrumpCardIdx : trumpCardIdx; // лучше походить некозырной

                if (selectedCardIdx != -1)
                {
                    SCard selectedCard = hand[selectedCardIdx];
                    hand.RemoveAt(selectedCardIdx);
                    return new List<SCard> { selectedCard };
                }
            }

            // стратегия 2. играем стратегию пар/двоек/троек
            SCard? batchFirst = PlayBatchStrategy();
            if (batchFirst.HasValue)
            {
                hand.Remove(batchFirst.Value);
                return new List<SCard> { batchFirst.Value };
            }

            notTrumpCardIdx = -1;
            trumpCardIdx = -1;

            // выбираем минимальную некозырную карту и минимальную козырь для хода
            for (int i = 0; i < hand.Count; i++)
            {
                SCard card = hand[i];

                if (
                    card.Suit != trumpSuit &&
                    (notTrumpCardIdx == -1 || card.Rank < hand[notTrumpCardIdx].Rank)
                )
                    notTrumpCardIdx = i;
                else if (trumpCardIdx == -1 || card.Rank < hand[trumpCardIdx].Rank)
                    trumpCardIdx = i;
            }

            selectedCardIdx = notTrumpCardIdx != -1 ? notTrumpCardIdx : trumpCardIdx; // лучше походить некозырной

            if (selectedCardIdx != -1)
            {
                SCard selectedCard = hand[selectedCardIdx];
                hand.RemoveAt(selectedCardIdx);
                return new List<SCard> { selectedCard };
            }

            return new List<SCard>();
        }

        // защита от карт
        // на вход подается набор карт на столе, часть из них могут быть уже покрыты
        public bool Defend(List<SCardPair> table)
        {
            roundAttacker = RoundAttacker.OPPONENT;
            if (hand.Count == 0) return false;

            for (int i = 0; i < table.Count; i++)
            {
                if (table[i].Beaten) continue; // если карта уже побита

                // выбираем минимальную карту, чтобы побиться
                int selectedIdx = -1;

                // ! предположение оказалось провальным
                // bool selectedCreatesBatch = false;

                for (int j = 0; j < hand.Count; j++)
                {
                    if (!SCard.CanBeat(table[i].Down, hand[j], trumpSuit)) continue; // если не можем побить

                    bool currentIsTrump = hand[j].Suit == trumpSuit;
                    bool cardIsTrump = selectedIdx != -1 && hand[selectedIdx].Suit == trumpSuit;

                    if (currentIsTrump && countBittedCards <= 12) continue; // в первой трети игры придерживаем козыри

                    // предпочитаем некозырные карты козырям
                    if (
                        selectedIdx == -1 ||
                        // если карта не козырная, но "лучшая" - козырная
                        (!currentIsTrump && cardIsTrump) ||
                        // если выбираем среди некозырных, но эта карта ниже рангом
                        (!currentIsTrump && !cardIsTrump && hand[j].Rank < hand[selectedIdx].Rank) ||
                        // если выбираем среди козырных, но эта карта ниже рангом
                        (currentIsTrump && cardIsTrump && hand[j].Rank < hand[selectedIdx].Rank))
                    {
                        // ! предположение оказалось провальным
                        // bool cardCreatesBatch = hand.FindAll(card => card.Rank == hand[j].Rank).Count > 1;

                        // if (
                        //     // если карту еще не выбрали
                        //     selectedIdx == -1 ||
                        //     // если эта карта не образует пару/двойку/тройку, а предыдущая уже образует
                        //     (!cardCreatesBatch && selectedCreatesBatch)
                        // )
                        // {
                        //     selectedIdx = j;
                        //     selectedCreatesBatch = cardCreatesBatch;
                        // }
                        selectedIdx = j;
                    }
                }

                if (selectedIdx == -1) return false;

                SCardPair pair = table[i];
                pair.SetUp(hand[selectedIdx], trumpSuit); // бьемся
                table[i] = pair;

                hand.RemoveAt(selectedIdx);
            }

            return true;
        }

        // добавление карт
        // на вход подается набор карт на столе, а также отбился ли оппонент
        public bool AddCards(List<SCardPair> table, bool opponentDefensed)
        {
            if (hand.Count == 0) return false; // нет карт в руке
            if (table.Count >= Math.Min(opponentCurrentCountCards, 6)) // не можем подкидывать больше, чем есть у оппонента
                return false;


            if (batchMemoCards.Count > 0) // если активна стратегия пар/двоек/троек
            {
                var cardPair = batchMemoCards.Dequeue(); // снова ходим по одной, берем первую карту в очереди

                hand.RemoveAt(cardPair.Key);
                table.Add(new SCardPair(cardPair.Value));

                return true;
            }

            // // выбираем карты для подкидывания
            // List<SCard> batchHand = hand.Where(card =>
            // {
            //     return table.Any(tableCard =>
            //     {
            //         if (tableCard.Beaten)
            //             return card.Rank == tableCard.Down.Rank || card.Rank == tableCard.Up.Rank;
            //         return card.Rank == tableCard.Down.Rank;
            //     });
            // }).ToList();
            // SCard? batchFirst = PlayBatchStrategy(batchHand); // пытаемся подкинуть пары/двоики/троики
            // if (batchFirst.HasValue)
            // {
            //     hand.Remove(batchFirst.Value);
            //     table.Add(new SCardPair(batchFirst.Value));
            //     return true;
            // }

            List<int> ranksOnTable = GetTableRanks(table);

            // ищем минимальную подходящую карту
            int selectedCardIdx = -1;
            bool selectedCanBeat = true; // могут ли побить выбранную карту

            for (int i = 0; i < hand.Count; i++)
            {
                if (hand[i].Suit == trumpSuit && countBittedCards <= 18) continue; // в первой половине игры придерживаем козыри

                if (ranksOnTable.Contains(hand[i].Rank)) // если можем подкинуть
                {
                    if (
                        opponentHand.Count <= 6 && // если знаем точные карты оппонента и можем отбиться
                        OpponentCanBeat(hand[i]) &&
                        // если карты нет, карту могут отбить или есть карта меньшего ранга, которую нельзя отбить 
                        (selectedCardIdx == -1 || selectedCanBeat || hand[i].Rank < hand[selectedCardIdx].Rank)
                    )
                    {
                        selectedCardIdx = i;
                        selectedCanBeat = false;
                    }
                    // если выбранную можно отбить или есть карта меньшего ранга
                    else if (selectedCanBeat && (selectedCardIdx == -1 || hand[i].Rank < hand[selectedCardIdx].Rank))
                    {
                        selectedCardIdx = i;
                        selectedCanBeat = true;
                    }
                }
            }

            if (selectedCardIdx == -1) return false;

            table.Add(new SCardPair(hand[selectedCardIdx]));
            hand.RemoveAt(selectedCardIdx);

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
            int opponentNeed = Math.Max(0, 6 - opponentCurrentCountCards);

            int opponentDraw = 0; // сколько доберет оппонент

            if (deckRemaining > 0)
            {
                if (roundAttacker == RoundAttacker.ME)
                {
                    // если атаковали первыми, то добираем первым
                    // добираем сколько необходимо или сколько осталось
                    deckRemaining -= Math.Min(myNeed, deckRemaining);

                    opponentDraw = Math.Min(opponentNeed, deckRemaining);
                    deckRemaining -= opponentDraw; // аналогично добирает соперник
                }
                else
                {
                    opponentDraw = Math.Min(opponentNeed, deckRemaining);
                    deckRemaining -= opponentDraw; // первый добирает соперник
                    deckRemaining -= Math.Min(myNeed, deckRemaining); // потом добираю я
                }
            }

            opponentCurrentCountCards += opponentDraw;

            // сбрасываем память для стратегии двоек/троек/четверок
            batchMemoCards.Clear();
        }

        // установка козыря, на вход подаётся козырь, вызывается перед первой раздачей карт
        public void SetTrump(SCard newTrump)
        {
            trumpSuit = newTrump.Suit;
            InitOpponentHand();
        }
    }
}
