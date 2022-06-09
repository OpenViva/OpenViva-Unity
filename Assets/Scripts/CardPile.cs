using System.Collections.Generic;
using UnityEngine;

namespace viva
{

    public class CardPile : MonoBehaviour
    {

        [SerializeField]
        private PokerGame parentGame;
        [SerializeField]
        private AudioClip cardPlaceSound;

        public List<PokerCard> playedCards = new List<PokerCard>();
        private List<PokerCard> nearbyCards = new List<PokerCard>();
        private List<Tuple<TransformBlend, int>> repositioning = new List<Tuple<TransformBlend, int>>();

        public int? GetLastValuePlayed()
        {
            if (playedCards.Count == 0)
            {
                return null;
            }
            return playedCards[playedCards.Count - 1].topValue;
        }

        public void ReleaseAllCards()
        {
            foreach (var card in playedCards)
            {
                card.rigidBody.isKinematic = false;
            }
        }

        public void PlayCard(PokerCard card)
        {
            if (card == null)
            {
                Debug.LogError("[Card Pile] played card is null!");
                return;
            }
            if (card.lastOwner == null)
            {
                return;
            }
            if (!nearbyCards.Contains(card))
            {
                nearbyCards.Add(card);
            }
        }

        private void FixedUpdate()
        {

            for (int i = nearbyCards.Count; i-- > 0;)
            {

                if (!parentGame.gameIsActive)
                {
                    return;
                }
                PokerCard card = nearbyCards[i];
                if (card == null)
                {
                    nearbyCards.RemoveAt(i);
                    continue;
                }
                //wait till it is dropped
                if (card.mainOwner != null || card.isFanGroupChild)
                {
                    continue;
                }
                if (parentGame.rules.AllowedToPlayCard(card, parentGame))
                {

                    //add to graveyard
                    var transformBlend = new TransformBlend();
                    transformBlend.SetTarget(true, card.transform, false, false, 0.0f, 1.0f, 0.2f);
                    card.rigidBody.isKinematic = true;
                    repositioning.Add(new Tuple<TransformBlend, int>(transformBlend, playedCards.Count));

                    //hide last played card front
                    if (playedCards.Count > 0)
                    {
                        playedCards[playedCards.Count - 1].SetShowFrontValueModel(false);
                    }

                    playedCards.Add(card);
                    card.SetAttribute(Item.Attributes.DISABLE_PICKUP);
                    nearbyCards.RemoveAt(i);
                    SoundManager.main.RequestHandle(transform.position).PlayOneShot(cardPlaceSound);

                    //play card effect
                    parentGame.OnCardPlayed(card);
                }
            }
            for (int i = repositioning.Count; i-- > 0;)
            {
                Tuple<TransformBlend, int> entry = repositioning[i];
                var blend = entry._1;
                if (blend.target == null)
                {
                    repositioning.RemoveAt(i);
                    continue;
                }
                float height = PokerCard.maxDeckHeight * (entry._2 / 52.0f);

                blend.Blend(transform.position + Vector3.up * height, Quaternion.LookRotation(Tools.FlatForward(blend.target.forward), Vector3.up));
                if (blend.blend.finished)
                {
                    repositioning.RemoveAt(i);
                    continue;
                }
            }
        }

        private void OnTriggerExit(Collider collider)
        {
            PokerCard card = collider.GetComponent<PokerCard>();
            if (card == null)
            {
                return;
            }
            nearbyCards.Remove(card);
        }

        private void OnTriggerEnter(Collider collider)
        {
            PokerCard card = collider.GetComponent<PokerCard>();
            if (card == null)
            {
                return;
            }
            PlayCard(card);
        }
    }

}