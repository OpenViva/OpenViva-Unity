using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace viva
{


    public class PokerCard : Item
    {

        public enum Suit
        {
            HEART,
            SPADE,
            DIAMOND,
            CLOVER,
            JOKER
        }

        [VivaFileAttribute]
        public int[] deckContent { get { return m_deckContent; } protected set { m_deckContent = value; } }
        [SerializeField]
        private int[] m_deckContent = new int[]{
        0,1,2,3,4,5,6,7,8,9,10,11,12,
        13,14,15,16,17,18,19,20,21,22,23,24,25,
        26,27,28,29,30,31,32,33,34,35,36,37,38,
        39,40,41,42,43,44,45,46,47,48,49,50,51,
        52,53	//include 2 jokers
	};
        [SerializeField]
        private MeshFilter meshFilter;
        [SerializeField]
        private MeshRenderer meshRenderer;
        [SerializeField]
        private Mesh cardDeckMesh;
        [SerializeField]
        private BoxCollider boxCollider;
        [VivaFileAttribute]
        public int cardPoolIndex { get { return m_cardPoolIndex; } protected set { m_cardPoolIndex = value; } }
        [SerializeField]
        private int m_cardPoolIndex = -1;
        [SerializeField]
        private GameObject pokerGameStation;
        [SerializeField]
        private AudioClip deckDrawSound;
        [SerializeField]
        private AudioClip springFlushSound;
        [SerializeField]
        private AudioClip shortShuffleSound;

        public int topValue { get { return deckContent[deckContent.Length - 1]; } }
        public int bottomValue { get { return deckContent[0]; } }
        public bool hasFullDeck { get { return deckContent.Length == 54; } }
        public int fanGroupSize { get { return fanGroupChildren.Count; } }
        private bool isFanningCards { get { return fanGroupSize > 0; } }
        private bool m_inPool = false;
        public bool inPool { get { return m_inPool; } }
        public int cardGroupSize { get { return fanGroupSize + deckContent.Length; } }
        private Material cachedMat = null;
        private bool drawnFromDeck = false;

        private static readonly Vector2 numberQuadTLUV = new Vector2(0.6338f, 0.5f);
        private static readonly Vector2 numberQuadSize = new Vector2(0.0732f, -0.0732f);
        private static readonly Vector2 suitQuadTLUV = new Vector2(0.7556f, 0.7197f);
        private static readonly Vector2 suitQuadSize = new Vector2(0.1221f, -0.1401f);
        private static readonly Vector2 royaltyQuadTopLeftUV = new Vector2(0.0f, 0.0f);
        private static readonly Vector2 royaltyQuadSize = new Vector2(0.318f, -0.4990f);
        private static readonly int redID = Shader.PropertyToID("_Red");
        private static readonly int highlightedID = Shader.PropertyToID("_Highlighted");

        private List<PokerCard> fanGroupChildren = new List<PokerCard>();
        private List<Tuple<int, TransformBlend>> newCardParentingBlends = new List<Tuple<int, TransformBlend>>();

        public bool isFanGroupParent { get { return fanGroupChildren.Count > 0; } }
        private PokerGame m_parentGame = null;
        public PokerGame parentGame { get { return m_parentGame; } }
        private PokerCard lastHighlightedCard = null;
        private bool ignoreNextKeyboardAddToOtherHand = false;
        private bool isPickingCard = false;
        private Character m_lastOwner = null;
        public Character lastOwner { get { return m_lastOwner; } }
        public bool isFanGroupChild
        {
            get
            {
                if (isAttached)
                {
                    return attachment.parentItem as PokerCard != null;
                }
                else
                {
                    return false;
                }
            }
        }

        protected static Dictionary<int, PokerCard[]> decks = new Dictionary<int, PokerCard[]>();
        public static float maxDeckHeight = 0.021f;


        public static List<PokerCard> FindAllCardsLastOwnedByCharacter(int cardPoolIndex, Character character)
        {
            if (cardPoolIndex <= -1)
            {
                Debug.LogError("[Card] bad card pool index");
                return null;
            }
            PokerCard[] deck = null;
            if (!decks.TryGetValue(cardPoolIndex, out deck))
            {
                Debug.LogError("[Card] Uninitialized card pool!");
                return null;
            }
            List<PokerCard> cards = new List<PokerCard>();
            foreach (PokerCard card in deck)
            {
                if (card.inPool)
                {
                    continue;
                }
                if (card.lastOwner == character)
                {
                    cards.Add(card);
                }
            }
            return cards;
        }

        public static PokerCard FindAvailableFullDeck(int cardPoolIndex)
        {

            PokerCard[] deck;
            if (decks.TryGetValue(cardPoolIndex, out deck))
            {
                PokerCard result = null;
                foreach (PokerCard card in deck)
                {
                    if (card.inPool)
                    {
                        if (result == null)
                        {
                            result = card;
                        }
                        else
                        {
                            result = null;
                            break;
                        }
                    }
                }
                return result;
            }
            return null;
        }

        public static PokerCard FindActivePooledCard(int cardPoolIndex, int value)
        {
            PokerCard[] deck;
            if (decks.TryGetValue(cardPoolIndex, out deck))
            {
                foreach (PokerCard card in deck)
                {
                    if (card.topValue == value)
                    {
                        if (card.inPool)
                        {
                            return null;
                        }
                        else
                        {
                            return card;
                        }
                    }
                }
                return null;
            }
            else
            {
                return null;
            }
        }

        private void ApplySuitColor(Suit suit)
        {
            if (meshRenderer.materials.Length > 1)
            {
                if (suit == Suit.DIAMOND || suit == Suit.HEART)
                {
                    meshRenderer.materials[1].SetFloat(redID, 1.0f);
                }
                else
                {
                    meshRenderer.materials[1].SetFloat(redID, 0.0f);
                }
            }
        }

        public void SetShowFrontValueModel(bool show)
        {
            if (show)
            {
                if (cachedMat == null)
                {
                    Debug.LogError("[Card] Could not hide material");
                    return;
                }
                meshRenderer.materials = new Material[] { meshRenderer.material, cachedMat };
                ApplySuitColor(GetCardSuit(topValue));
            }
            else
            {
                if (cachedMat == null && meshRenderer.materials.Length > 1)
                {
                    cachedMat = meshRenderer.materials[1];
                }
                meshRenderer.materials = new Material[] { meshRenderer.material };
            }
            foreach (PokerCard card in fanGroupChildren)
            {
                card.SetShowFrontValueModel(show);
            }
        }

        public List<int> GetCardGroupValues()
        {
            var values = new List<int>();
            values.AddRange(deckContent);
            foreach (PokerCard card in fanGroupChildren)
            {
                values.Add(card.topValue);
            }
            return values;
        }

        public bool DoesCardGroupHaveValue(int value)
        {
            foreach (int cardValue in deckContent)
            {
                if (cardValue == value)
                {
                    return true;
                }
            }
            foreach (PokerCard card in fanGroupChildren)
            {
                if (card.topValue == value)
                {
                    return true;
                }
            }
            return false;
        }

        public int? GetCardGroupValueByIndex(int index)
        {
            if (index < 0)
            {
                Debug.LogError("[Poker Card] index is negative!");
                return null;
            }
            //start counting from deckContent
            if (index < deckContent.Length)
            {
                return deckContent[index];
            }
            else
            {
                if (index >= deckContent.Length + fanGroupChildren.Count)
                {
                    return null;
                }
                return fanGroupChildren[index - deckContent.Length].topValue;
            }
        }

        public PokerCard ExtractCard(int value)
        {
            //search deck content
            for (int i = 0; i < deckContent.Length - 1; i++)
            {   //do not test with self topValue
                int cardValue = deckContent[i];
                if (cardValue == value)
                {
                    PokerCard pooledCard = RemoveFirstPooledCard(this);
                    if (pooledCard == null)
                    {
                        return null;
                    }

                    //setup new card with removed value
                    pooledCard.OverrideDeckContent(new int[] { value });

                    var copy = new List<int>(deckContent);
                    copy.RemoveAt(i);
                    OverrideDeckContent(copy.ToArray());

                    PlayFanGroupAnimationBlends();
                    return pooledCard;
                }
            }
            //test self topValue
            if (value == topValue)
            {
                HandState oldHandState = mainOccupyState as HandState;
                List<PokerCard> fanGroupCopy = DropAllFanGroupCards();
                if (mainOccupyState != null)
                {
                    mainOccupyState.AttemptDrop();
                }
                if (fanGroupCopy.Count > 0)
                {
                    //rebuild fan group with a new base card
                    PokerCard baseCard = fanGroupCopy[fanGroupCopy.Count - 1];
                    fanGroupCopy.RemoveAt(fanGroupCopy.Count - 1);
                    baseCard.AddToFanGroup(fanGroupCopy);

                    //restore back into hand
                    if (oldHandState != null)
                    {
                        oldHandState.GrabItemRigidBody(baseCard);
                    }
                }
                return this;
            }
            //search fan group
            foreach (PokerCard card in fanGroupChildren)
            {
                if (card.topValue == value)
                {
                    bool success = AttemptReleaseFanGroupChild(card);
                    if (success)
                    {
                        PlayFanGroupAnimationBlends();
                        return card;
                    }
                    break;
                }
            }
            return null;
        }

        public override bool IgnorePersistance()
        {
            if (!gameObject.activeSelf)
            {
                return true;
            }
            return base.IgnorePersistance(); ;
        }

        public static void RemoveDeckFromPokerGame(int cardPoolIndex)
        {

            PokerCard[] deck = null;
            if (!decks.TryGetValue(cardPoolIndex, out deck))
            {
                return;
            }
            if (deck == null)
            {
                return;
            }
            foreach (PokerCard card in deck)
            {
                card.m_parentGame = null;
                card.SetShowFrontValueModel(true);
                if (card.isFanGroupChild || card.mainOccupyState != null)
                {
                    continue;
                }
                card.ClearAttribute(Item.Attributes.DISABLE_PICKUP);
            }
        }

        public PokerGame CreatePokerGame(Character dealer)
        {
            //must not be in a game already
            if (parentGame != null)
            {
                return null;
            }
            //need to have a full deck
            // if( !hasFullDeck ){
            // 	return null;
            // }
            GameObject parentGameContainer = GameObject.Instantiate(pokerGameStation);
            PokerGame pokerGame = parentGameContainer.GetComponent<PokerGame>();
            if (!pokerGame.AttemptSetupGameInfo(dealer, this))
            {
                GameObject.Destroy(parentGameContainer);
                return null;
            }
            //assign game to entire deck
            foreach (PokerCard card in decks[cardPoolIndex])
            {
                card.m_parentGame = pokerGame;
            }
            Debug.Log("[Poker Card] Created Poker Game ");
            return m_parentGame;
        }

        private void UpdateThickness()
        {
            float cardSize = deckContent.Length / 52.0f;
            if (isAttached)
            {
                cardSize /= attachment.parent.transform.localScale.y;
            }

            Vector3 newScale = new Vector3(1.0f, cardSize, 1.0f);
            transform.localScale = newScale;

            //update box collider
            const float padding = 0.005f;
            boxCollider.size = new Vector3(0.08883947f, 0.02f + padding / newScale.y, 0.1333414f);
        }

        public float GetThickness()
        {
            return boxCollider.size.y - 0.004f;
        }

        private PokerCard RemoveFirstPooledCard(PokerCard parentSource)
        {
            if (cardPoolIndex == -1)
            {
                Debug.LogError("[Poker Card] ERROR cardPoolIndex is -1!");
                return null;
            }
            for (int i = 0; i < decks[cardPoolIndex].Length; i++)
            {
                var card = decks[cardPoolIndex][i];
                if (card.inPool)
                {
                    card.SetInPool(false);
                    // card.RemoveFanGroupChildProperties();	//reset properties
                    if (parentSource != null)
                    {
                        card.transform.position = parentSource.transform.position + parentSource.transform.up * -0.02f;
                        card.transform.rotation = parentSource.transform.rotation;
                    }
                    return card;
                }
            }
            return null;
        }

        private PokerCard CloneTopCard()
        {
            //cant clone top card if only 1 card left
            if (deckContent.Length <= 1)
            {
                return null;
            }
            //get first inactive card from pooled objects
            PokerCard pooledCard = RemoveFirstPooledCard(this);
            //create card if no pooled version found
            if (pooledCard == null)
            {
                // Debug.LogError("[Poker Card] No pooled object found!");
                return null;
            }
            //initalize pooled card object
            pooledCard.OverrideDeckContent(new int[] { topValue });

            //subtract top card
            var result = deckContent;
            Array.Resize(ref result, deckContent.Length - 1);
            OverrideDeckContent(result);

            return pooledCard;
        }

        public List<PokerCard> RemoveTopCards(int count)
        {
            List<PokerCard> topCards = new List<PokerCard>();
            for (int i = 0; i < count; i++)
            {
                var card = CloneTopCard();
                if (card == null)
                {
                    break;
                }
                topCards.Add(card);
            }
            //finalize self changes with top value card
            RebuildCardMesh();

            return topCards;
        }

        private PokerCard[] CreateCardPool(int newCardPoolIndex)
        {
            //create 54 pooled objects (Expensive)
            Debug.Log("[Poker Card] Creating new card pool " + newCardPoolIndex + "...");
            GameObject prefab = GameDirector.instance.FindItemPrefabByName("pokerCard");
            if (prefab == null)
            {
                return null;
            }
            PokerCard[] deck = new PokerCard[54];
            for (var i = 0; i < 54; i++)
            {
                GameObject newCardContainer = GameObject.Instantiate(prefab, transform.position, transform.rotation);
                var newCard = newCardContainer.GetComponent<PokerCard>() as PokerCard;

                //assign pool index and store
                newCard.cardPoolIndex = newCardPoolIndex;
                newCard.SetInPool(true);
                newCard.OverrideDeckContent(new int[] { i });
                deck[i] = newCard;
            }
            decks.Add(newCardPoolIndex, deck);
            return deck;
        }

        private void SetInPool(bool pooled)
        {
            if (pooled == m_inPool)
            {
                return;
            }
            gameObject.SetActive(!pooled);
            m_inPool = pooled;
            if (isAttached)
            {
                Debug.LogError("CARD WAS RETURNED IN ATTACHED MODE");
                Detach();
            }
        }

        private bool AttemptRegisterToCardPool()
        {
            if (cardPoolIndex == -1)
            {
                return false;
            }
            //card pools will have a copy of every card mesh even if they have groups of stacked cards in deckContent
            PokerCard[] targetDeck = null;
            if (!decks.ContainsKey(cardPoolIndex))
            {
                targetDeck = CreateCardPool(cardPoolIndex);
            }
            else
            {
                targetDeck = decks[cardPoolIndex];
            }
            //delete duplicate in favor of this card
            ///TODO: Prevent this?
            for (int i = 0; i < targetDeck.Length; i++)
            {
                var oldDuplicate = targetDeck[i];
                if (oldDuplicate.topValue == topValue)
                {
                    GameObject.Destroy(oldDuplicate.gameObject);
                    targetDeck[i] = this;
                    break;
                }
            }
            return true;
        }

        protected override void OnItemAwake()
        {

            //default prefab value of -1 will always fail to prevent instantiate Awake()
            if (AttemptRegisterToCardPool())
            {
                RebuildCardMesh();
            }
            name = sessionReferenceName;
        }

        public static Suit GetCardSuit(int value)
        {
            if (value < 0 || value / 13 >= System.Enum.GetValues(typeof(Suit)).Length)
            {
                return Suit.JOKER;  //default
            }
            return (Suit)(value / 13);
        }

        public static int GetCardType(int value)
        {
            if (value < 0 || value >= 54)
            {
                return -1;
            }
            if (value > 51)
            {
                return 13;  //joker
            }
            return value % 13;
        }

        private void RebuildCardMesh()
        {

            Suit suit = GetCardSuit(topValue);
            int number = GetCardType(topValue);
            int suitValue = (int)suit;

            //build mesh, combine cardDeckMesh with new front submesh
            Mesh newMesh = new Mesh();
            int quads;
            if (number <= 10)
            {
                quads = 4 + number + 1; //one quad per suit
            }
            else if (number <= 12)
            {
                quads = 5;  //specials 
            }
            else
            {
                quads = 3;//joker
            }
            Vector3[] vertices = new Vector3[cardDeckMesh.vertices.Length + quads * 2 * 3];
            Vector2[] uvs = new Vector2[vertices.Length];

            //add base deck mesh
            Array.Copy(cardDeckMesh.vertices, vertices, cardDeckMesh.vertices.Length);
            Array.Copy(cardDeckMesh.uv, uvs, cardDeckMesh.uv.Length);

            int index = cardDeckMesh.vertices.Length;
            //add corner data
            MeshBufferUtil.BufferXZQuad(numberQuadTLUV, numberQuadSize, 5, number, new Vector3(-0.04f, maxDeckHeight, 0.06f), new Vector2(0.015f, 0.015f), vertices, uvs, ref index);
            MeshBufferUtil.BufferXZQuad(numberQuadTLUV, numberQuadSize, 5, number, new Vector3(0.04f, maxDeckHeight, -0.06f), new Vector2(-0.015f, -0.015f), vertices, uvs, ref index);
            if (suit != Suit.JOKER)
            {
                MeshBufferUtil.BufferXZQuad(suitQuadTLUV, suitQuadSize, 2, suitValue, new Vector3(-0.0375f, maxDeckHeight, 0.045f), new Vector2(0.01f, 0.01f), vertices, uvs, ref index);
                MeshBufferUtil.BufferXZQuad(suitQuadTLUV, suitQuadSize, 2, suitValue, new Vector3(0.0375f, maxDeckHeight, -0.045f), new Vector2(-0.01f, -0.01f), vertices, uvs, ref index);
            }
            float suitSize = 0.018f;
            switch (number)
            {
                case 0:
                    break;
                case 1:
                    MeshBufferUtil.BufferXZQuad(suitQuadTLUV, suitQuadSize, 2, suitValue, new Vector3(-suitSize * 0.5f, maxDeckHeight, suitSize * 2.5f), new Vector2(suitSize, suitSize), vertices, uvs, ref index);
                    MeshBufferUtil.BufferXZQuad(suitQuadTLUV, suitQuadSize, 2, suitValue, new Vector3(-suitSize * -0.5f, maxDeckHeight, suitSize * -2.5f), new Vector2(-suitSize, -suitSize), vertices, uvs, ref index);
                    break;
                case 2:
                    MeshBufferUtil.BufferXZQuad(suitQuadTLUV, suitQuadSize, 2, suitValue, new Vector3(-suitSize * 0.5f, maxDeckHeight, suitSize * 0.5f), new Vector2(suitSize, suitSize), vertices, uvs, ref index);
                    MeshBufferUtil.BufferXZQuad(suitQuadTLUV, suitQuadSize, 2, suitValue, new Vector3(-suitSize * 0.5f, maxDeckHeight, suitSize * 2.5f), new Vector2(suitSize, suitSize), vertices, uvs, ref index);
                    MeshBufferUtil.BufferXZQuad(suitQuadTLUV, suitQuadSize, 2, suitValue, new Vector3(-suitSize * -0.5f, maxDeckHeight, suitSize * -2.5f), new Vector2(-suitSize, -suitSize), vertices, uvs, ref index);
                    break;
                case 3:
                    MeshBufferUtil.BufferXZQuad(suitQuadTLUV, suitQuadSize, 2, suitValue, new Vector3(-suitSize * 1.5f, maxDeckHeight, suitSize * 2.5f), new Vector2(suitSize, suitSize), vertices, uvs, ref index);
                    MeshBufferUtil.BufferXZQuad(suitQuadTLUV, suitQuadSize, 2, suitValue, new Vector3(-suitSize * -0.5f, maxDeckHeight, suitSize * 2.5f), new Vector2(suitSize, suitSize), vertices, uvs, ref index);
                    MeshBufferUtil.BufferXZQuad(suitQuadTLUV, suitQuadSize, 2, suitValue, new Vector3(-suitSize * 0.5f, maxDeckHeight, suitSize * -2.5f), new Vector2(-suitSize, -suitSize), vertices, uvs, ref index);
                    MeshBufferUtil.BufferXZQuad(suitQuadTLUV, suitQuadSize, 2, suitValue, new Vector3(-suitSize * -1.5f, maxDeckHeight, suitSize * -2.5f), new Vector2(-suitSize, -suitSize), vertices, uvs, ref index);
                    break;
                case 4:
                    MeshBufferUtil.BufferXZQuad(suitQuadTLUV, suitQuadSize, 2, suitValue, new Vector3(-suitSize * 0.5f, maxDeckHeight, suitSize * 0.5f), new Vector2(suitSize, suitSize), vertices, uvs, ref index);
                    MeshBufferUtil.BufferXZQuad(suitQuadTLUV, suitQuadSize, 2, suitValue, new Vector3(-suitSize * 1.5f, maxDeckHeight, suitSize * 2.5f), new Vector2(suitSize, suitSize), vertices, uvs, ref index);
                    MeshBufferUtil.BufferXZQuad(suitQuadTLUV, suitQuadSize, 2, suitValue, new Vector3(-suitSize * -0.5f, maxDeckHeight, suitSize * 2.5f), new Vector2(suitSize, suitSize), vertices, uvs, ref index);
                    MeshBufferUtil.BufferXZQuad(suitQuadTLUV, suitQuadSize, 2, suitValue, new Vector3(-suitSize * 0.5f, maxDeckHeight, suitSize * -2.5f), new Vector2(-suitSize, -suitSize), vertices, uvs, ref index);
                    MeshBufferUtil.BufferXZQuad(suitQuadTLUV, suitQuadSize, 2, suitValue, new Vector3(-suitSize * -1.5f, maxDeckHeight, suitSize * -2.5f), new Vector2(-suitSize, -suitSize), vertices, uvs, ref index);
                    break;
                case 5:
                    MeshBufferUtil.BufferXZQuad(suitQuadTLUV, suitQuadSize, 2, suitValue, new Vector3(-suitSize * 1.5f, maxDeckHeight, suitSize * 2.5f), new Vector2(suitSize, suitSize), vertices, uvs, ref index);
                    MeshBufferUtil.BufferXZQuad(suitQuadTLUV, suitQuadSize, 2, suitValue, new Vector3(-suitSize * -0.5f, maxDeckHeight, suitSize * 2.5f), new Vector2(suitSize, suitSize), vertices, uvs, ref index);
                    MeshBufferUtil.BufferXZQuad(suitQuadTLUV, suitQuadSize, 2, suitValue, new Vector3(-suitSize * 1.5f, maxDeckHeight, suitSize * 0.5f), new Vector2(suitSize, suitSize), vertices, uvs, ref index);
                    MeshBufferUtil.BufferXZQuad(suitQuadTLUV, suitQuadSize, 2, suitValue, new Vector3(-suitSize * -0.5f, maxDeckHeight, suitSize * 0.5f), new Vector2(suitSize, suitSize), vertices, uvs, ref index);
                    MeshBufferUtil.BufferXZQuad(suitQuadTLUV, suitQuadSize, 2, suitValue, new Vector3(-suitSize * 0.5f, maxDeckHeight, suitSize * -2.5f), new Vector2(-suitSize, -suitSize), vertices, uvs, ref index);
                    MeshBufferUtil.BufferXZQuad(suitQuadTLUV, suitQuadSize, 2, suitValue, new Vector3(-suitSize * -1.5f, maxDeckHeight, suitSize * -2.5f), new Vector2(-suitSize, -suitSize), vertices, uvs, ref index);
                    break;
                case 6:
                    MeshBufferUtil.BufferXZQuad(suitQuadTLUV, suitQuadSize, 2, suitValue, new Vector3(-suitSize * 1.5f, maxDeckHeight, suitSize * 2.5f), new Vector2(suitSize, suitSize), vertices, uvs, ref index);
                    MeshBufferUtil.BufferXZQuad(suitQuadTLUV, suitQuadSize, 2, suitValue, new Vector3(-suitSize * -0.5f, maxDeckHeight, suitSize * 2.5f), new Vector2(suitSize, suitSize), vertices, uvs, ref index);
                    MeshBufferUtil.BufferXZQuad(suitQuadTLUV, suitQuadSize, 2, suitValue, new Vector3(-suitSize * 1.5f, maxDeckHeight, suitSize * 0.5f), new Vector2(suitSize, suitSize), vertices, uvs, ref index);
                    MeshBufferUtil.BufferXZQuad(suitQuadTLUV, suitQuadSize, 2, suitValue, new Vector3(-suitSize * -0.5f, maxDeckHeight, suitSize * 0.5f), new Vector2(suitSize, suitSize), vertices, uvs, ref index);
                    MeshBufferUtil.BufferXZQuad(suitQuadTLUV, suitQuadSize, 2, suitValue, new Vector3(-suitSize * 0.5f, maxDeckHeight, suitSize * 1.5f), new Vector2(suitSize, suitSize), vertices, uvs, ref index);
                    MeshBufferUtil.BufferXZQuad(suitQuadTLUV, suitQuadSize, 2, suitValue, new Vector3(-suitSize * 0.5f, maxDeckHeight, suitSize * -2.5f), new Vector2(-suitSize, -suitSize), vertices, uvs, ref index);
                    MeshBufferUtil.BufferXZQuad(suitQuadTLUV, suitQuadSize, 2, suitValue, new Vector3(-suitSize * -1.5f, maxDeckHeight, suitSize * -2.5f), new Vector2(-suitSize, -suitSize), vertices, uvs, ref index);
                    break;
                case 7:
                    MeshBufferUtil.BufferXZQuad(suitQuadTLUV, suitQuadSize, 2, suitValue, new Vector3(-suitSize * 1.5f, maxDeckHeight, suitSize * 2.5f), new Vector2(suitSize, suitSize), vertices, uvs, ref index);
                    MeshBufferUtil.BufferXZQuad(suitQuadTLUV, suitQuadSize, 2, suitValue, new Vector3(-suitSize * -0.5f, maxDeckHeight, suitSize * 2.5f), new Vector2(suitSize, suitSize), vertices, uvs, ref index);
                    MeshBufferUtil.BufferXZQuad(suitQuadTLUV, suitQuadSize, 2, suitValue, new Vector3(-suitSize * 1.5f, maxDeckHeight, suitSize * 1.16f), new Vector2(suitSize, suitSize), vertices, uvs, ref index);
                    MeshBufferUtil.BufferXZQuad(suitQuadTLUV, suitQuadSize, 2, suitValue, new Vector3(-suitSize * -0.5f, maxDeckHeight, suitSize * 1.16f), new Vector2(suitSize, suitSize), vertices, uvs, ref index);
                    MeshBufferUtil.BufferXZQuad(suitQuadTLUV, suitQuadSize, 2, suitValue, new Vector3(-suitSize * 0.5f, maxDeckHeight, suitSize * -1.16f), new Vector2(-suitSize, -suitSize), vertices, uvs, ref index);
                    MeshBufferUtil.BufferXZQuad(suitQuadTLUV, suitQuadSize, 2, suitValue, new Vector3(-suitSize * -1.5f, maxDeckHeight, suitSize * -1.16f), new Vector2(-suitSize, -suitSize), vertices, uvs, ref index);
                    MeshBufferUtil.BufferXZQuad(suitQuadTLUV, suitQuadSize, 2, suitValue, new Vector3(-suitSize * 0.5f, maxDeckHeight, suitSize * -2.5f), new Vector2(-suitSize, -suitSize), vertices, uvs, ref index);
                    MeshBufferUtil.BufferXZQuad(suitQuadTLUV, suitQuadSize, 2, suitValue, new Vector3(-suitSize * -1.5f, maxDeckHeight, suitSize * -2.5f), new Vector2(-suitSize, -suitSize), vertices, uvs, ref index);
                    break;
                case 8:
                    MeshBufferUtil.BufferXZQuad(suitQuadTLUV, suitQuadSize, 2, suitValue, new Vector3(-suitSize * 1.5f, maxDeckHeight, suitSize * 2.5f), new Vector2(suitSize, suitSize), vertices, uvs, ref index);
                    MeshBufferUtil.BufferXZQuad(suitQuadTLUV, suitQuadSize, 2, suitValue, new Vector3(-suitSize * -0.5f, maxDeckHeight, suitSize * 2.5f), new Vector2(suitSize, suitSize), vertices, uvs, ref index);
                    MeshBufferUtil.BufferXZQuad(suitQuadTLUV, suitQuadSize, 2, suitValue, new Vector3(-suitSize * 1.5f, maxDeckHeight, suitSize * 1.16f), new Vector2(suitSize, suitSize), vertices, uvs, ref index);
                    MeshBufferUtil.BufferXZQuad(suitQuadTLUV, suitQuadSize, 2, suitValue, new Vector3(-suitSize * -0.5f, maxDeckHeight, suitSize * 1.16f), new Vector2(suitSize, suitSize), vertices, uvs, ref index);
                    MeshBufferUtil.BufferXZQuad(suitQuadTLUV, suitQuadSize, 2, suitValue, new Vector3(-suitSize * 0.5f, maxDeckHeight, suitSize * -1.16f), new Vector2(-suitSize, -suitSize), vertices, uvs, ref index);
                    MeshBufferUtil.BufferXZQuad(suitQuadTLUV, suitQuadSize, 2, suitValue, new Vector3(-suitSize * -1.5f, maxDeckHeight, suitSize * -1.16f), new Vector2(-suitSize, -suitSize), vertices, uvs, ref index);
                    MeshBufferUtil.BufferXZQuad(suitQuadTLUV, suitQuadSize, 2, suitValue, new Vector3(-suitSize * 0.5f, maxDeckHeight, suitSize * -2.5f), new Vector2(-suitSize, -suitSize), vertices, uvs, ref index);
                    MeshBufferUtil.BufferXZQuad(suitQuadTLUV, suitQuadSize, 2, suitValue, new Vector3(-suitSize * -1.5f, maxDeckHeight, suitSize * -2.5f), new Vector2(-suitSize, -suitSize), vertices, uvs, ref index);
                    MeshBufferUtil.BufferXZQuad(suitQuadTLUV, suitQuadSize, 2, suitValue, new Vector3(-suitSize * 0.5f, maxDeckHeight, suitSize * 0.5f), new Vector2(suitSize, suitSize), vertices, uvs, ref index);
                    break;
                case 9:
                    MeshBufferUtil.BufferXZQuad(suitQuadTLUV, suitQuadSize, 2, suitValue, new Vector3(-suitSize * 1.5f, maxDeckHeight, suitSize * 2.5f), new Vector2(suitSize, suitSize), vertices, uvs, ref index);
                    MeshBufferUtil.BufferXZQuad(suitQuadTLUV, suitQuadSize, 2, suitValue, new Vector3(-suitSize * -0.5f, maxDeckHeight, suitSize * 2.5f), new Vector2(suitSize, suitSize), vertices, uvs, ref index);
                    MeshBufferUtil.BufferXZQuad(suitQuadTLUV, suitQuadSize, 2, suitValue, new Vector3(-suitSize * 1.5f, maxDeckHeight, suitSize * 1.16f), new Vector2(suitSize, suitSize), vertices, uvs, ref index);
                    MeshBufferUtil.BufferXZQuad(suitQuadTLUV, suitQuadSize, 2, suitValue, new Vector3(-suitSize * -0.5f, maxDeckHeight, suitSize * 1.16f), new Vector2(suitSize, suitSize), vertices, uvs, ref index);
                    MeshBufferUtil.BufferXZQuad(suitQuadTLUV, suitQuadSize, 2, suitValue, new Vector3(-suitSize * 0.5f, maxDeckHeight, suitSize * -1.16f), new Vector2(-suitSize, -suitSize), vertices, uvs, ref index);
                    MeshBufferUtil.BufferXZQuad(suitQuadTLUV, suitQuadSize, 2, suitValue, new Vector3(-suitSize * -1.5f, maxDeckHeight, suitSize * -1.16f), new Vector2(-suitSize, -suitSize), vertices, uvs, ref index);
                    MeshBufferUtil.BufferXZQuad(suitQuadTLUV, suitQuadSize, 2, suitValue, new Vector3(-suitSize * 0.5f, maxDeckHeight, suitSize * -2.5f), new Vector2(-suitSize, -suitSize), vertices, uvs, ref index);
                    MeshBufferUtil.BufferXZQuad(suitQuadTLUV, suitQuadSize, 2, suitValue, new Vector3(-suitSize * -1.5f, maxDeckHeight, suitSize * -2.5f), new Vector2(-suitSize, -suitSize), vertices, uvs, ref index);
                    MeshBufferUtil.BufferXZQuad(suitQuadTLUV, suitQuadSize, 2, suitValue, new Vector3(-suitSize * 0.5f, maxDeckHeight, suitSize * 1.5f), new Vector2(suitSize, suitSize), vertices, uvs, ref index);
                    MeshBufferUtil.BufferXZQuad(suitQuadTLUV, suitQuadSize, 2, suitValue, new Vector3(-suitSize * -0.5f, maxDeckHeight, suitSize * -1.5f), new Vector2(-suitSize, -suitSize), vertices, uvs, ref index);
                    break;
                default:
                    float royaltySize = 0.055f;
                    float aspect = 1.572f;
                    MeshBufferUtil.BufferXZQuad(royaltyQuadTopLeftUV, royaltyQuadSize, 3, number - 10, new Vector3(royaltySize * -0.5f, maxDeckHeight, suitSize * 1.5f * aspect), new Vector2(royaltySize, royaltySize * aspect), vertices, uvs, ref index);
                    break;
            }

            ApplySuitColor(suit);

            int[] indices = new int[quads * 2 * 3];
            MeshBufferUtil.BuildTrianglesFromQuadPoints(indices, quads, 0, cardDeckMesh.vertices.Length);

            //build regular normals
            Vector3[] normals = new Vector3[vertices.Length];

            Array.Copy(cardDeckMesh.normals, normals, cardDeckMesh.normals.Length);
            index = cardDeckMesh.normals.Length;
            for (int quad = 0; quad < quads; quad++)
            {
                normals[index++] = Vector3.up;
                normals[index++] = Vector3.up;
                normals[index++] = Vector3.up;
                normals[index++] = Vector3.up;
            }

            newMesh.vertices = vertices;
            newMesh.uv = uvs;
            newMesh.normals = normals;
            newMesh.subMeshCount = 2;
            newMesh.SetTriangles(cardDeckMesh.triangles, 0);
            newMesh.SetTriangles(indices, 1);

            meshFilter.mesh = newMesh;
            UpdateThickness();
        }

        public void AddToFanGroup(List<PokerCard> newCards)
        {
            if (newCards == null)
            {
                return;
            }
            if (newCards.Contains(this))
            {
                Debug.Log("[Card] fan group additions contain self!");
                return;
            }
            //combine into new array
            bool addedNewCard = false;
            foreach (PokerCard card in newCards)
            {
                if (fanGroupChildren.Contains(card))
                {
                    continue;
                }
                if (card.mainOccupyState != null)
                {
                    card.mainOccupyState.AttemptDrop();
                }
                fanGroupChildren.Add(card);
                card.ApplyFanGroupChildProperties(this);

                addedNewCard = true;
                //remove deck content cards
                if (card.deckContent.Length > 1)
                {
                    var subCards = card.RemoveTopCards(card.deckContent.Length - 1);
                    foreach (var subCard in subCards)
                    {
                        subCard.ApplyFanGroupChildProperties(this);
                    }
                    fanGroupChildren.AddRange(subCards);
                }
            }
            if (addedNewCard)
            {
                OpenCardFan();
                PlayFanGroupAnimationBlends();
            }
        }

        private void PlaySpringFlourishAnimationBlends()
        {
            HandState handState = mainOccupyState as HandState;
            if (handState == null)
            {
                return;
            }
            //flourish only if not animating any cards
            if (newCardParentingBlends.Count > 0)
            {
                return;
            }
            HandState otherHandState = handState.otherHandState;
            PokerCard otherCard = otherHandState.GetItemIfHeld<PokerCard>();
            if (otherCard)
            {
                //other card must not be animating any cards either
                if (otherCard.newCardParentingBlends.Count > 0)
                {
                    return;
                }
                //must have more cards or rightside
                if (cardGroupSize < otherCard.cardGroupSize)
                {
                    return;
                }
                if (cardGroupSize == otherCard.cardGroupSize && handState.rightSide)
                {
                    return;
                }
                //do not flourish if other hand is not a card or empty
            }
            else if (otherHandState.occupied)
            {
                return;
            }
            if (fanGroupSize > 0)
            {
                return;
            }

            //fly flourish into hand with least cards
            if (otherCard == null)
            {
                otherCard = ExtractCard(deckContent[0]);
                otherCard.ignoreNextKeyboardAddToOtherHand = true;
                otherHandState.GrabItemRigidBody(otherCard);
            }
            PlayerHandState playerHandState = handState as PlayerHandState;
            PlayerHandState playerOtherHandState = otherHandState as PlayerHandState;
            if (playerHandState && playerOtherHandState)
            {
                Player.Animation targetAnim;
                if (playerOtherHandState.rightSide)
                {
                    targetAnim = Player.Animation.CARD_SPRING_FLOURISH_RIGHT;
                }
                else
                {
                    targetAnim = Player.Animation.CARD_SPRING_FLOURISH_LEFT;
                }
                playerHandState.animSys.SetTargetAnimation(targetAnim);
                playerOtherHandState.animSys.SetTargetAnimation(targetAnim);
            }
            newCardParentingBlends.Clear();
            GameDirector.instance.StartCoroutine(SpringCardPlayAnimation(this, otherCard));
        }

        private IEnumerator SpringCardPlayAnimation(PokerCard srcCard, PokerCard destCard)
        {

            float cardsPerSecond = 0.6f / srcCard.cardGroupSize;
            int cardsAnimated = 0;

            SoundManager.main.RequestHandle(transform.position).PlayOneShot(springFlushSound);
            while (true)
            {
                if (srcCard == null || destCard == null)
                {
                    break;
                }
                if (destCard.mainOccupyState == null || srcCard.mainOccupyState == null)
                {
                    break;
                }
                PokerCard newFlyCard = srcCard.ExtractCard(srcCard.deckContent[0]);
                if (newFlyCard)
                {
                    destCard.AddToSpringAnimationGroup(newFlyCard);
                    // newFlyCard.ZeroOutCardModelTransform();
                    if (newFlyCard == srcCard)
                    {
                        break;
                    }
                }
                cardsAnimated++;
                if (cardsAnimated > 20)
                {
                    //impress nearby lolis
                    List<Character> characters = GameDirector.instance.FindCharactersInSphere(
                        (int)Character.Type.LOLI,
                        destCard.transform.position,
                        2.0f
                    );
                    foreach (Character character in characters)
                    {
                        Loli loli = character as Loli;
                        loli.active.idle.AttemptImpress();
                    }
                }

                yield return new WaitForSeconds(cardsPerSecond);
            }
        }

        private void AddToSpringAnimationGroup(PokerCard card)
        {
            //must be held to work
            if (mainOccupyState == null || card == this)
            {
                return;
            }
            if (fanGroupChildren.Contains(card))
            {
                return;
            }
            fanGroupChildren.Add(card);
            card.ApplyFanGroupChildProperties(this);

            var parentingBlend = new TransformBlend();
            parentingBlend.SetTarget(true, card.transform, true, true, 0.0f, 1.0f, 0.5f);
            newCardParentingBlends.Add(new Tuple<int, TransformBlend>(0, parentingBlend));
        }

        private void PlayFanGroupAnimationBlends()
        {
            newCardParentingBlends.Clear();
            int slotIndexHalf = fanGroupChildren.Count / -2;

            //add self fan card
            var selfBlend = new TransformBlend();
            selfBlend.SetTarget(true, transform, true, true, 0.0f, 1.0f, 0.5f);
            newCardParentingBlends.Add(new Tuple<int, TransformBlend>(slotIndexHalf, selfBlend));

            for (int i = 0; i < fanGroupChildren.Count; i++)
            {
                var card = fanGroupChildren[i];

                var parentingBlend = new TransformBlend();
                parentingBlend.SetTarget(true, card.transform, true, true, 0.0f, 1.0f, 0.5f);
                int fanGroupIndex = newCardParentingBlends.Count + slotIndexHalf + 1;
                if (fanGroupIndex <= 0)
                {
                    fanGroupIndex -= 2;
                }
                newCardParentingBlends.Add(new Tuple<int, TransformBlend>(fanGroupIndex, parentingBlend));
            }
        }

        private bool UpdateParentingBlend(Tuple<int, TransformBlend> parentingBlend)
        {

            //ensure target is still active
            if (parentingBlend._2.target == null || parentingBlend._2.target.parent != transform)
            {
                return false;
            }
            int slotIndex = parentingBlend._1;
            if (slotIndex < 0)
            {
                slotIndex++;
            }

            Vector3 pivot = new Vector3(0.0f, 0.0f, -0.1f);
            Quaternion localRot = Quaternion.Euler(0.0f, -slotIndex * 6f, 0.0f);
            Vector3 localPos = -pivot;
            localPos = localRot * -pivot + pivot;
            localPos.y = -0.002f * slotIndex;   //scale by self

            parentingBlend._2.Blend(localPos, localRot);
            return !parentingBlend._2.blend.finished;
        }

        private void AddToDeckContent(PokerCard card)
        {
            if (card == null || card == this)
            {
                return;
            }
            foreach (int cardValue in deckContent)
            {
                if (cardValue == card.topValue)
                {
                    // Debug.LogError("[Card] Card "+this.name+" already has value! "+card.topValue);
                    return;
                }
            }
            card.SetInPool(true);

            var newContent = new int[deckContent.Length + 1];
            Array.Copy(deckContent, newContent, deckContent.Length);
            newContent[newContent.Length - 1] = card.topValue;
            OverrideDeckContent(newContent);

            fanGroupChildren.Remove(card);
        }

        private bool IsFirstHeldCard(Character character)
        {
            if (character != null)
            {
                PokerCard rightPokerCard = character.rightHandState.GetItemIfHeld<PokerCard>();
                PokerCard leftPokerCard = character.leftHandState.GetItemIfHeld<PokerCard>();
                return (rightPokerCard == null || leftPokerCard == null);
            }
            return false;
        }

        public List<PokerCard> DropAllFanGroupCards()
        {
            var fanGroupCopy = new List<PokerCard>(fanGroupChildren);
            foreach (PokerCard childCard in fanGroupChildren)
            {
                childCard.RemoveFanGroupChildProperties();
            }
            fanGroupChildren.Clear();
            newCardParentingBlends.Clear();
            return fanGroupCopy;
        }

        private bool AttemptReleaseFanGroupChild(PokerCard childCard)
        {
            if (childCard == null)
            {
                return false;
            }
            int index = fanGroupChildren.IndexOf(childCard);
            //if not in the fan group
            if (index == -1)
            {
                //not allowed to release card if no child fan cards available
                return false;
            }
            else
            {
                fanGroupChildren.RemoveAt(index);
                childCard.RemoveFanGroupChildProperties();
            }
            return true;
        }

        private void RemoveFanGroupChildProperties()
        {
            Detach();
            UpdateThickness();
        }

        private void ApplyFanGroupChildProperties(PokerCard baseCard)
        {
            AttachTo(baseCard.transform, baseCard);
            m_lastOwner = baseCard.lastOwner;
            UpdateThickness();
        }

        private void FindAndAddToProximalFanGroup(List<PokerCard> cards)
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, 0.125f, Instance.itemsMask, QueryTriggerInteraction.Ignore);
            foreach (Collider collider in colliders)
            {
                PokerCard card = collider.GetComponent<PokerCard>();
                if (card == this)
                {   //ignore self
                    continue;
                }
                if (card != null && card.mainOccupyState != null && !card.isFanGroupChild)
                {
                    card.AddToFanGroup(cards);
                    break;
                }
            }
        }

        public override void OnPreDrop()
        {
            if (isPickingCard)
            {
                EndPlayerPickCard(null);
            }
            SetHighlightPokerCard(null);
            RestoreHighlightColoring();

            var cards = new List<PokerCard>(fanGroupChildren);
            DropAllFanGroupCards();
            //add self and children to group of cards
            if (mainOccupyState.owner.characterType == Character.Type.PLAYER)
            {
                cards.Add(this);
                GameDirector.instance.StartCoroutine(PostDropNewFan(cards));
            }
        }

        public IEnumerator PostDropNewFan(List<PokerCard> cards)
        {
            yield return new WaitForFixedUpdate();
            FindAndAddToProximalFanGroup(cards);
        }

        public override void OnPostPickup()
        {
            SetLastOwner(mainOwner);
            //fire drawnFromDeck if applicable
            if (drawnFromDeck)
            {
                drawnFromDeck = false;

                if (parentGame != null)
                {
                    parentGame.OnDrawnFromMasterDeck(this);
                    SoundManager.main.RequestHandle(transform.position).PlayOneShot(deckDrawSound);
                }
            }
            Player player = mainOwner as Player;
            if (player != null)
            {
                if (player.controls == Player.ControlType.KEYBOARD)
                {
                    if (!ignoreNextKeyboardAddToOtherHand)
                    {
                        AddToOtherPlayerHandCard(player, mainOccupyState as HandState);
                    }
                    else
                    {
                        ignoreNextKeyboardAddToOtherHand = false;
                    }
                }
                else
                {
                    if (cardGroupSize == 1)
                    {
                        TutorialManager.main.DisplayObjectHint(this, "hint_vacuumCards", HintFanCards);
                    }
                }
            }
            TutorialManager.main.DisplayObjectHint(this, "hint_offerDeck", HintListenOfferDeck);
        }

        public void SetLastOwner(Character newLastOwner)
        {
            m_lastOwner = newLastOwner;
            //set fan group children as same owner
            foreach (PokerCard fanGroupChild in fanGroupChildren)
            {
                fanGroupChild.m_lastOwner = newLastOwner;
            }
        }

        private IEnumerator HintListenOfferDeck(Item source)
        {
            while (true)
            {
                PokerCard card = source as PokerCard;
                if (card == null || card.parentGame != null)
                {
                    TutorialManager.main.StopHint();
                    yield break;
                }
                yield return null;
            }
        }

        public override bool CanBePickedUp(OccupyState occupyState)
        {
            //disable picking up by other players when in game
            if (parentGame != null)
            {
                if (mainOwner != null && occupyState.owner != mainOwner)
                {
                    return false;
                }
            }
            return base.CanBePickedUp(occupyState);
        }

        public override bool OnPrePickupInterrupt(HandState handState)
        {
            //-1 means can pickup whole deck
            int cardsToDraw = -1;

            //ask game how many allowed to pickup
            bool isMasterDeck = false;
            if (parentGame != null)
            {
                cardsToDraw = parentGame.RequestCardDrawsAllowed(this, handState.owner);
                isMasterDeck = this == parentGame.masterDeck;
            }
            //do not draw any cards
            if (cardsToDraw == 0)
            {
                return true;
            }
            //allow pickup of whole deck, do not interrupt
            if (cardsToDraw < 0)
            {
                return false;
            }
            //if insufficient cards, include and pickup card itself
            if (deckContent.Length <= cardsToDraw)
            {
                drawnFromDeck = isMasterDeck;
                return false;
            }
            //remove a certain amount of cards
            List<PokerCard> cards = RemoveTopCards(cardsToDraw);
            PokerCard baseCard = cards[0];
            cards.RemoveAt(0);
            baseCard.AddToFanGroup(cards);
            baseCard.drawnFromDeck = isMasterDeck;
            handState.GrabItemRigidBody(baseCard);
            return true;
        }

        private void AddToOtherPlayerHandCard(Player player, HandState handState)
        {
            PokerCard otherPokerCard = handState.otherHandState.GetItemIfHeld<PokerCard>();
            if (otherPokerCard && otherPokerCard.isFanGroupParent)
            {
                //add to other hand
                Player.Animation addAnim;
                if (handState.rightSide)
                {
                    addAnim = Player.Animation.ADD_CARD_TO_LEFT;
                }
                else
                {
                    addAnim = Player.Animation.ADD_CARD_TO_RIGHT;
                }
                player.rightPlayerHandState.animSys.SetTargetAnimation(addAnim);
                player.leftPlayerHandState.animSys.SetTargetAnimation(addAnim);
                TutorialManager.main.DisplayObjectHint(this, "hint_howToFanCards", HintFanCards);
            }
        }

        private IEnumerator HintFanCards(Item source)
        {
            PokerCard card = source as PokerCard;
            if (card == null)
            {
                yield break;
            }
            while (true)
            {
                if (card == null || card.inPool)
                {
                    TutorialManager.main.StopHint();
                    yield break;
                }
                yield return null;
            }
        }

        private bool AttemptPlayerActionToggleFan()
        {
            PlayerHandState handState = mainOccupyState as PlayerHandState;
            if (handState == null)
            {
                return false;
            }
            if (handState.actionState.isUp && !isPickingCard)
            {
                if (Time.time - handState.actionState.lastDownTime > 0.4f)
                {
                    if (isFanningCards)
                    {
                        CloseCardFan();
                    }
                    else
                    {
                        OpenCardFan();
                    }
                    return true;
                }
                else
                {
                    HandState.ButtonState targetState;
                    PlayerHandState otherHandState = handState.otherPlayerHandState;
                    if (GameDirector.player.controls == Player.ControlType.KEYBOARD)
                    {
                        if (otherHandState.occupied)
                        {
                            targetState = otherHandState.actionState;
                        }
                        else
                        {
                            targetState = otherHandState.gripState;
                        }
                    }
                    else
                    {
                        targetState = otherHandState.actionState;
                    }
                    if (targetState.isUp && Time.time - targetState.lastDownTime < 0.4f)
                    {
                        PlaySpringFlourishAnimationBlends();
                        return true;
                    }
                }
            }
            if (handState.actionState.isHeldDown)
            {
                AddNearbyCardsToFanGroup();
            }
            return false;
        }

        public void Shuffle()
        {
            for (int i = deckContent.Length; i-- > 0;)
            {
                int randomIndex = UnityEngine.Random.Range(0, i);
                int old = deckContent[i];
                deckContent[i] = deckContent[randomIndex];
                deckContent[randomIndex] = old;
            }
        }

        private void AddNearbyCardsToFanGroup()
        {
            List<PokerCard> newCards = new List<PokerCard>();
            Collider[] colliders = Physics.OverlapSphere(transform.position, 0.15f, Instance.itemsMask, QueryTriggerInteraction.Ignore);
            foreach (Collider collider in colliders)
            {
                PokerCard card = collider.GetComponent<PokerCard>();
                if (card == null)
                {
                    continue;
                }
                if (card.mainOccupyState != null)
                {
                    continue;
                }
                if (card.HasAttribute(Item.Attributes.DISABLE_PICKUP))
                {
                    continue;
                }
                //not allowed to suck up master deck
                if (parentGame != null && card == parentGame.masterDeck)
                {
                    continue;
                }
                newCards.Add(card);
                card.PlayShortShuffleSound();
            }
            AddToFanGroup(newCards);
        }

        public void PlayShortShuffleSound()
        {
            SoundManager.main.RequestHandle(transform.position).PlayOneShot(shortShuffleSound);
        }

        private void OnExitKeyboardPickCardCallback()
        {
            isPickingCard = false;
            if (lastHighlightedCard)
            {
                lastHighlightedCard.RestoreHighlightColoring();
            }
            RestoreKeyboardHandStateAnim();
        }

        private void RestoreKeyboardHandStateAnim()
        {
            if (mainOccupyState != null)
            {
                PlayerHandState handState = mainOccupyState as PlayerHandState;
                if (mainOccupyState)
                {
                    handState.animSys.SetTargetAnimation(handState.animSys.idleAnimation);
                    PlayerHandState otherPlayerHandState = handState.rightSide ? mainOwner.leftHandState as PlayerHandState : mainOwner.rightHandState as PlayerHandState;
                    otherPlayerHandState.animSys.SetTargetAnimation(handState.animSys.idleAnimation);
                }
            }
        }

        private void RestoreHighlightColoring()
        {
            if (meshRenderer.materials.Length > 1)
            {
                meshRenderer.materials[1].SetFloat(highlightedID, 0.0f);
            }
            meshRenderer.material.color = Color.white;
        }

        private void CheckPlayerVRPickCard()
        {
            PlayerHandState handState = mainOccupyState as PlayerHandState;
            if (handState == null)
            {
                return;
            }
            //toggle pick out card menu
            Player player = mainOwner as Player;
            PlayerHandState otherHandState = handState.otherPlayerHandState;

            if (otherHandState.heldItem != null)
            {
                EndPlayerPickCard(null);
                return;
            }
            Vector3 fingerTip = otherHandState.fingerAnimator.fingers[0].position;  //thumb tip
            float side = System.Convert.ToInt32(otherHandState.rightSide) * 2 - 1;
            Ray ray = new Ray(fingerTip, otherHandState.fingerAnimator.hand.right);
            PokerCard newHighlightedCard = FindNearestRadianFanCard(ray, 0.015f, 0.08f);

            SetHighlightPokerCard(newHighlightedCard);
            if (otherHandState.gripState.isDown)
            {
                EndPlayerPickCard(newHighlightedCard);
            }
        }

        private void CheckPlayerKeyboardPickCard()
        {
            PlayerHandState handState = mainOccupyState as PlayerHandState;
            if (handState == null)
            {
                return;
            }
            //toggle pick out card menu
            Player player = mainOwner as Player;
            HandState.ButtonState listenButton;
            if (isPickingCard)
            {
                if (handState.owner.leftHandState.occupied)
                {
                    listenButton = player.leftPlayerHandState.actionState;
                }
                else
                {
                    listenButton = player.leftPlayerHandState.gripState;
                }
            }
            else
            {
                listenButton = handState.actionState;
            }
            if (listenButton.isUp && Time.time - listenButton.lastDownTime <= 0.4f)
            {
                listenButton.Consume();
                if (isPickingCard)
                {
                    EndPlayerPickCard(lastHighlightedCard);
                }
                else
                {

                    GameDirector.instance.SetEnableControls(GameDirector.ControlsAllowed.HAND_INPUT_ONLY, OnExitKeyboardPickCardCallback);
                    isPickingCard = true;
                    OpenCardFan();
                    PlayerHandState playerHandState = handState as PlayerHandState;
                    PlayerHandState playerOtherHandState;
                    Player.Animation selectAnim;
                    if (playerHandState.rightSide)
                    {
                        selectAnim = Player.Animation.SELECT_CARD_RIGHT;
                        playerOtherHandState = handState.owner.leftHandState as PlayerHandState;
                    }
                    else
                    {
                        selectAnim = Player.Animation.SELECT_CARD_LEFT;
                        playerOtherHandState = handState.owner.rightHandState as PlayerHandState;
                    }
                    playerHandState.animSys.SetTargetAnimation(selectAnim);
                    playerOtherHandState.animSys.SetTargetAnimation(selectAnim);
                }
            }
            else if (isPickingCard)
            {
                Ray ray = GameDirector.instance.mainCamera.ScreenPointToRay(player.mousePosition);
                SetHighlightPokerCard(FindNearestRadianFanCard(ray, 0.0f, Mathf.Infinity));
            }
        }

        private void SetHighlightPokerCard(PokerCard newHighlightedCard)
        {
            if (lastHighlightedCard != newHighlightedCard)
            {
                if (lastHighlightedCard != null)
                {
                    lastHighlightedCard.RestoreHighlightColoring();
                }
                if (newHighlightedCard != null)
                {
                    newHighlightedCard.meshRenderer.materials[1].SetFloat(highlightedID, 1.0f);
                    newHighlightedCard.meshRenderer.material.color = new Color(0.5f, 0.5f, 1.0f);
                    GameDirector.instance.helperIndicator.gameObject.SetActive(true);
                }
                else
                {
                    GameDirector.instance.helperIndicator.gameObject.SetActive(false);
                }
                lastHighlightedCard = newHighlightedCard;
            }
        }

        private PokerCard FindNearestRadianFanCard(Ray ray, float minimumSqDist, float maxRayDist)
        {

            if (!isFanningCards && cardGroupSize > 1)
            {
                return null;
            }
            //find nearest radian card to target radian
            Plane fanPlane = new Plane(transform.up, transform.position);
            float fanCenterZOffset = 0.1f;

            float enter;
            if (!fanPlane.GetSide(ray.origin) || !fanPlane.Raycast(ray, out enter))
            {
                return null;
            }
            Vector3 intersection = ray.origin + ray.direction * enter;
            if (Vector3.SqrMagnitude(intersection - ray.origin) > maxRayDist)
            {
                return null;
            }
            Vector3 localIntersection = transform.InverseTransformPoint(intersection);
            GameDirector.instance.helperIndicator.position = ray.origin + ray.direction * enter;

            float fanCenterSqDist = Vector3.SqrMagnitude(new Vector3(localIntersection.x, 0.0f, localIntersection.z + 0.1f));
            if (fanCenterSqDist < minimumSqDist || fanCenterSqDist > 0.065f)
            {
                return null;
            }
            PokerCard nearestCard = null;
            float minSqDist = 0.173f * 2.0f;
            if (fanCenterSqDist < minSqDist * minSqDist)
            {
                //add offset for accurate card screen
                float mouseDeg = Mathf.Atan2(localIntersection.x + 0.028f, localIntersection.z + fanCenterZOffset) * Mathf.Rad2Deg + 180.0f;
                float shortest = 30.0f;

                foreach (PokerCard childCard in fanGroupChildren)
                {
                    float childDeg = Mathf.Atan2(
                        childCard.transform.localPosition.x,
                        childCard.transform.localPosition.z + fanCenterZOffset) * Mathf.Rad2Deg;
                    float distance = Mathf.DeltaAngle(mouseDeg, childDeg);
                    if (distance < shortest)
                    {
                        shortest = distance;
                        nearestCard = childCard;
                    }
                }

                //finally test against self
                float selfDeg = Mathf.Atan2(
                    transform.localPosition.x,
                    transform.localPosition.z + fanCenterZOffset) * Mathf.Rad2Deg;
                if (Mathf.DeltaAngle(mouseDeg, selfDeg) < shortest)
                {
                    nearestCard = this;
                }
            }
            return nearestCard;
        }

        private void EndPlayerPickCard(PokerCard targetCard)
        {
            HandState handState = mainOccupyState as HandState;
            if (handState == null)
            {
                return;
            }
            RestoreKeyboardHandStateAnim();
            isPickingCard = false;

            //restore controls
            GameDirector.instance.SetEnableControls(GameDirector.ControlsAllowed.ALL);
            if (lastHighlightedCard)
            {
                lastHighlightedCard.RestoreHighlightColoring();
            }

            if (targetCard == null)
            {
                return;
            }
            PokerCard extractedCard = ExtractCard(targetCard.topValue);
            if (extractedCard)
            {

                // add to other card if other card in hand exists
                HandState otherHandState = handState.otherHandState;
                PokerCard otherHandCard = otherHandState.GetItemIfHeld<PokerCard>();
                extractedCard.ignoreNextKeyboardAddToOtherHand = true;
                if (otherHandCard)
                {
                    otherHandCard.AddToFanGroup(new List<PokerCard>() { extractedCard });
                }
                else
                {
                    //else pick up as a new item
                    otherHandState.GrabItemRigidBody(extractedCard);
                }
            }
            GameDirector.instance.helperIndicator.gameObject.SetActive(false);
        }

        public override void OnItemLateUpdate()
        {
            Player player = mainOwner as Player;
            if (player != null)
            {
                //accumulate cards to one hand automatically in keyboard mode
                if (!AttemptPlayerActionToggleFan())
                {
                    HandState otherHandState = (mainOccupyState as HandState).otherHandState;
                    PokerCard otherCard = otherHandState.GetItemIfHeld<PokerCard>();
                    if (otherCard && otherCard.isPickingCard)
                    {
                    }
                    else
                    {
                        if (player.controls == Player.ControlType.KEYBOARD)
                        {
                            CheckPlayerKeyboardPickCard();
                        }
                        else
                        {
                            CheckPlayerVRPickCard();
                        }
                    }
                }
            }
            //update card parenting blends
            for (int i = newCardParentingBlends.Count; i-- > 0;)
            {
                var parentingBlend = newCardParentingBlends[i];
                parentingBlend._2.blend.Update(Time.deltaTime);
                if (!UpdateParentingBlend(parentingBlend))
                {
                    newCardParentingBlends.RemoveAt(i);

                    if (parentingBlend._1 == 0)
                    {
                        AddToDeckContent(parentingBlend._2.target.GetComponent<PokerCard>());
                    }
                }
            }
        }

        public void OverrideDeckContent(int[] newDeckContent)
        {
            m_deckContent = newDeckContent;
            if (!inPool)
            {
                RebuildCardMesh();
            }
        }

        private void OpenCardFan()
        {
            if (deckContent.Length == 1)
            {
                return;
            }
            //remove all but 1 from deck content
            var newCards = RemoveTopCards(deckContent.Length - 1);
            foreach (var card in newCards)
            {
                card.transform.position = transform.position;
                card.transform.rotation = transform.rotation;
            }
            AddToFanGroup(newCards);
            OverrideDeckContent(new int[] { deckContent[0] });
        }

        public void CloseCardFan()
        {
            TutorialManager.main.DisplayObjectHint(this, "hint_howToFanCards", HintFanCards);

            //collapse deckContent
            List<int> newValues = new List<int>();
            newValues.Add(topValue);

            var droppedCards = DropAllFanGroupCards();
            foreach (var droppedCard in droppedCards)
            {
                newValues.Add(droppedCard.topValue);
                //store back into pool
                droppedCard.SetInPool(true);
            }
            OverrideDeckContent(newValues.ToArray());
        }
    }

}