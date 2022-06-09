using System.Collections;
using UnityEngine;




namespace viva
{

    [CreateAssetMenu(fileName = "Card Game One", menuName = "Logic/Card Game One", order = 1)]
    public class CardGameOne : CardGameRules
    {

        private PokerCard.Suit lastWildSuit = PokerCard.Suit.CLOVER;

        public enum PlayType
        {
            NUMBER,
            JACK_SKIP,
            QUEEN_DRAW_2,
            KING_DRAW_4,
            ACE_WILD,
            JOKER_RANDOMIZE
        }

        [SerializeField]
        private AudioClip[] cardPlaySounds = new AudioClip[System.Enum.GetValues(typeof(PlayType)).Length];


        public static PlayType GetPlayType(int value, ref int number)
        {

            number = PokerCard.GetCardType(value);

            switch (number)
            {
                case 0:
                    return PlayType.ACE_WILD;
                case 10:
                    return PlayType.JACK_SKIP;
                case 11:
                    return PlayType.QUEEN_DRAW_2;
                case 12:
                    return PlayType.KING_DRAW_4;
                case 13:
                    return PlayType.JOKER_RANDOMIZE;
            }
            return PlayType.NUMBER; //1-9
        }

        public override bool AllowedToPlayCard(PokerCard card, PokerGame game)
        {

            //check turn order first
            int index = game.FindPlayerByCharacter(card.lastOwner);
            if (index == -1)
            {
                index = game.FindPlayerByCharacter(card.mainOwner);
                //don't know who played card
                if (index == -1)
                {
                    Debug.Log("[Poker Game] Don't know who played card");
                    return false;
                }
            }
            //player must pick up draw cards before playing a card
            PokerGame.Player player = game.GetPlayerByIndex(index);
            if (player.requiredDrawCards > 0)
            {
                return false;
            }
            if (card.cardGroupSize > player.requiredPlayCards)
            {
                return false;
            }
            return IsCardPlayValid(card.topValue, game.cardPile);
        }
        public override bool IsCardPlayValid(int cardValue, CardPile cardPile)
        {
            int? lastValuePlayed = cardPile.GetLastValuePlayed();
            if (!lastValuePlayed.HasValue)
            {
                return true;
            }
            PokerCard.Suit lastSuit = PokerCard.GetCardSuit(lastValuePlayed.Value);
            int lastNumber = 0;
            var lastPlay = GetPlayType(lastValuePlayed.Value, ref lastNumber);
            if (lastPlay == PlayType.ACE_WILD || lastPlay == PlayType.JOKER_RANDOMIZE)
            {
                lastSuit = lastWildSuit;
            }

            PokerCard.Suit currSuit = PokerCard.GetCardSuit(cardValue);
            int currNumber = 0;
            var currPlay = GetPlayType(cardValue, ref currNumber);

            switch (currPlay)
            {
                case PlayType.NUMBER:
                    switch (lastPlay)
                    {
                        case PlayType.NUMBER:
                            return lastNumber == currNumber || lastSuit == currSuit;
                        case PlayType.JOKER_RANDOMIZE:
                        case PlayType.ACE_WILD:
                            return lastWildSuit == currSuit;
                        default:
                            return lastSuit == currSuit;
                    }
                case PlayType.JOKER_RANDOMIZE:
                case PlayType.ACE_WILD:
                    return true;    //always allowed to play
                default:
                    return lastSuit == currSuit;
            }
        }
        public override void PlayCardEffect(int value, PokerGame game)
        {

            PokerCard.Suit currSuit = PokerCard.GetCardSuit(value);
            int currNumber = 0;
            var currPlay = GetPlayType(value, ref currNumber);

            game.PlaySound(cardPlaySounds[(int)currPlay]);

            switch (currPlay)
            {
                case PlayType.JACK_SKIP:
                    game.CycleNextPlayerTurn(); //skip next turn
                    game.SetStatusMesh(PokerGame.Status.SKIP);
                    break;
                case PlayType.QUEEN_DRAW_2:
                    game.GetNextPlayer().requiredDrawCards += 2;
                    game.SetStatusMesh(PokerGame.Status.DRAW, 2);
                    break;
                case PlayType.KING_DRAW_4:
                    game.GetNextPlayer().requiredDrawCards += 4;
                    game.SetStatusMesh(PokerGame.Status.DRAW, 4);
                    break;
                case PlayType.JOKER_RANDOMIZE:
                    game.SetStatusMesh(PokerGame.Status.RANDOMIZE);
                    game.RandomizePlayerCardsInHand();
                    PickRandomSuit(game);
                    return; //dont end player turn
                case PlayType.NUMBER:
                    game.SetStatusMesh((PokerGame.Status)((int)PokerGame.Status.HEART + currSuit));
                    break;
                case PlayType.ACE_WILD:
                    game.SetStatusMesh(PokerGame.Status.WILD);
                    PickRandomSuit(game);
                    return;
            }
        }

        private void PickRandomSuit(PokerGame game)
        {
            lastWildSuit = game.GetCurrentPlayer().GetRandomSuitInHand(game);
            GameDirector.instance.StartCoroutine(EndPlayerTurnDelayed(game, 1.2f, (PokerGame.Status)(PokerGame.Status.HEART + (int)lastWildSuit)));
        }

        private IEnumerator EndPlayerTurnDelayed(PokerGame game, float time, PokerGame.Status status)
        {

            yield return new WaitForSeconds(time);
            if (game == null)
            {
                yield break;
            }
            game.SetStatusMesh(status);
        }
    }

}