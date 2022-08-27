using System;
using System.Collections.Generic;
using UnityEngine;




namespace viva
{


    [System.Serializable]
    public abstract class CardGameRules : ScriptableObject
    {

        public abstract bool AllowedToPlayCard(PokerCard card, PokerGame game);
        public abstract bool IsCardPlayValid(int cardValue, CardPile cardPile);
        public abstract void PlayCardEffect(int value, PokerGame game);
    }

    public partial class PokerGame : Mechanism
    {

        public enum Phase
        {
            NONE,
            DEALER_INITIALIZE_DECK,
            WAIT_FOR_PREPARE,
            PLAY
        }

        public enum Action
        {
            QUIT_GAME,
            INITIALIZE_DECK,
            WAIT,
            DRAW_CARDS,
            PLAY_CARD,
            LOSE,
            WIN
        }

        [SerializeField]
        private Mesh placeDeckSymbol;
        [SerializeField]
        private Mesh placeDeckSymbol_ring;
        [SerializeField]
        private GameObject inGameIndicatorPrefab = null;
        [SerializeField]
        private MeshRenderer deckMeshRenderer = null;
        [SerializeField]
        private MeshFilter deckMeshFilter = null;
        [SerializeField]
        private MeshRenderer cardPileRenderer = null;
        [SerializeField]
        private MeshRenderer pickupCardRenderer = null;
        [SerializeField]
        private CardPile m_cardPile = null;
        [SerializeField]
        private AudioSource soundSource = null;
        [SerializeField]
        private AudioClip gameWinSound = null;
        [SerializeField]
        private Texture validPlacementTexture = null;
        [SerializeField]
        private Texture invalidPlacementTexture = null;
        [SerializeField]
        private CardGameRules m_rules = null;
        public CardGameRules rules { get { return m_rules; } }
        [SerializeField]
        public CardPile cardPile { get { return m_cardPile; } }
        public bool gameIsActive { get { return gamePhase != Phase.NONE; } }

        //first player is always dealer
        private PokerCard m_masterDeck;
        public PokerCard masterDeck { get { return m_masterDeck; } }
        private readonly List<Player> players = new List<Player>();
        private Player dealer;
        private int turnIndex = 0;
        private int nextTurnIndex = 1;
        private Phase gamePhase = Phase.NONE;
        private const int cardsPerPlayer = 5;
        private Set<Character> drawTargetsAllowed = new Set<Character>();
        private int totalTurns = 0;
        public FilterUse turnFilter { get; } = new FilterUse();
        public float playerCount { get { return players.Count; } }


        public void PlaySound(AudioClip clip)
        {
            soundSource.PlayOneShot(clip);
        }

        public bool AttemptSetupGameInfo(Character _dealer, PokerCard _masterDeck)
        {
            if (dealer != null || masterDeck != null)
            {
                return false;
            }
            if (_dealer == null || _masterDeck == null)
            {
                Debug.Log("[Poker Game] Missing dealer or masterDeck");
                return false;
            }
            m_masterDeck = _masterDeck;
            //add dealer as player
            dealer = new Player(_dealer);
            players.Add(dealer);
            gamePhase = Phase.DEALER_INITIALIZE_DECK;

            deckMeshFilter.mesh = placeDeckSymbol_ring;
            OnMechanismAwake();
            return true;
        }

        public override bool AttemptCommandUse(Loli targetLoli, Character commandSource)
        {
            int index = FindPlayerByCharacter(targetLoli);
            if (index != -1)
            {
                SendPlayerTurnAction(index);
                return true;
            }
            return false;
        }

        public override void OnMechanismAwake()
        {
            GameDirector.mechanisms.Add(this);
        }

        public override void EndUse(Character targetCharacter)
        {
        }

        public override void OnDestroy()
        {
            GameDirector.mechanisms.Remove(this);
        }

        public bool AttemptAddPlayer(Character character)
        {
            if (character == null)
            {
                Debug.LogError("[Poker Game] Cannot add null character");
                return false;
            }
            if (FindPlayerByCharacter(character) != -1)
            {
                Debug.LogError("[Poker Game] Character already exists in game");
                return false;
            }
            else
            {
                if (gamePhase != Phase.DEALER_INITIALIZE_DECK)
                {
                    Debug.LogError("[Poker Game] Game already in session");
                    return false;   //cannot join game mid game
                }
                var newPlayer = new Player(character);
                newPlayer.inGameIndicator = GameObject.Instantiate(inGameIndicatorPrefab, Vector3.up * 0.4f, Quaternion.identity);
                newPlayer.inGameIndicator.transform.SetParent(character.head, false);
                players.Add(newPlayer);
                Debug.Log("[Poker Game] Added player " + character.name);
                return true;
            }
        }

        public int FindPlayerByCharacter(Character character)
        {
            for (var i = 0; i < players.Count; i++)
            {
                if (players[i].character == character)
                {
                    return i;
                }
            }
            return -1;
        }

        public Player GetPlayerByIndex(int index)
        {
            return players[index];
        }

        public void RemoveFromGame(Character character)
        {
            int index = FindPlayerByCharacter(character);
            if (index != -1)
            {
                var player = players[index];
                players.RemoveAt(index);
                Destroy(player.inGameIndicator);

                if (players.Count == 1)
                {
                    EndGame();
                }
            }
        }

        public void CycleNextPlayerTurn()
        {
            nextTurnIndex = (nextTurnIndex + 1) % players.Count;
        }

        public Player GetCurrentPlayer()
        {
            return players[turnIndex];
        }

        public void RandomizePlayerCardsInHand()
        {
            //make list of active card values
            List<int> activeValues = new List<int>();
            foreach (Player player in players)
            {
                var cardsOwned = PokerCard.FindAllCardsLastOwnedByCharacter(masterDeck.cardPoolIndex, player.character);
                foreach (PokerCard cardOwned in cardsOwned)
                {
                    foreach (int value in cardOwned.deckContent)
                    {
                        if (activeValues.Contains(value))
                        {
                            Debug.LogError("[Poker Game] ERROR duplicate card value found!");
                            return;
                        }
                        activeValues.Add(value);
                    }
                }
            }
            //Find all cards with last owner set to player
            foreach (Player player in players)
            {
                var cardsOwned = PokerCard.FindAllCardsLastOwnedByCharacter(masterDeck.cardPoolIndex, player.character);
                foreach (PokerCard cardOwned in cardsOwned)
                {
                    //create new randomized deck content for each card
                    int[] randomizedContent = new int[cardOwned.deckContent.Length];
                    for (int i = 0; i < randomizedContent.Length; i++)
                    {
                        if (activeValues.Count == 0)
                        {
                            Debug.LogError("[Poker Game] ERROR not enough active values to randomize!");
                            return;
                        }
                        int randomIndex = UnityEngine.Random.Range(0, activeValues.Count - 1);
                        randomizedContent[i] = activeValues[randomIndex];
                        activeValues.RemoveAt(randomIndex);
                    }
                    cardOwned.OverrideDeckContent(randomizedContent);
                }
            }
        }

        public Player GetNextPlayer()
        {
            return players[nextTurnIndex];
        }

        private void EndTurn()
        {
            Debug.Log("[Poker Game] Ended player turn: " + turnIndex);
            var lastPlayer = players[turnIndex];

            //check if player won by placing all of his cards
            var cards = PokerCard.FindAllCardsLastOwnedByCharacter(masterDeck.cardPoolIndex, lastPlayer.character);
            if (cards.Count == 0)
            {
                SendWinAndLoseStates(lastPlayer);
                return;
            }

            turnIndex = nextTurnIndex;
            CycleNextPlayerTurn();
            totalTurns++;

            //tell new player to place 1 card
            var currPlayer = players[turnIndex];
            currPlayer.requiredPlayCards = 1;
        }

        public void EndGame()
        {
            Debug.Log("[Poker Game] Ended game");
            foreach (Player player in players)
            {
                if (player.character.characterType == Character.Type.LOLI)
                {
                    Loli loli = player.character as Loli;
                    if (loli.active.IsTaskActive(loli.active.poker))
                    {
                        loli.active.SetTask(loli.active.idle, true);
                    }
                }
                if (player.inGameIndicator)
                {
                    Destroy(player.inGameIndicator);
                }
            }
            players.Clear();
            gamePhase = Phase.NONE;

            if (masterDeck != null)
            {
                PokerCard.RemoveDeckFromPokerGame(masterDeck.cardPoolIndex);
            }

            cardPile.ReleaseAllCards();
            Destroy(gameObject, 2.0f);
            masterDeck.rigidBody.isKinematic = false;
            masterDeck.SetItemLayer(WorldUtil.itemsLayer);
        }

        private bool AllPlayersFinished()
        {
            bool prepared = true;
            foreach (var player in players)
            {
                if (player.requiredDrawCards != 0 || player.requiredPlayCards != 0)
                {
                    prepared = false;
                    Debug.Log("Player " + player.character + " missing " + player.requiredPlayCards + ":" + player.requiredDrawCards);
                    break;
                }
            }
            return prepared;
        }

        private bool PrepareGame()
        {
            Debug.Log("[Poker Game] Started game. Players: " + players.Count);
            if (players.Count < 2)
            {
                return false;
            }
            if (masterDeck == null)
            {
                return false;
            }
            masterDeck.SetLastOwner(null);
            masterDeck.SetItemLayer(WorldUtil.heldItemsLayer);
            deckMeshFilter.mesh = placeDeckSymbol;

            TutorialManager.main.DisplayObjectHint(masterDeck, "hint_howToPlayOne");
            GameDirector.player.pauseMenu.SetTargetOnOpenMenuTab(PauseMenu.Menu.MANUAL);

            //shuffle deck
            masterDeck.Shuffle();
            gamePhase = Phase.WAIT_FOR_PREPARE;

            //send pickup cards action to all characters
            for (var i = 0; i < players.Count; i++)
            {
                players[i].requiredDrawCards = cardsPerPlayer;
                SendPokerGameAction(players[i].character, Action.DRAW_CARDS);
            }
            return true;
        }

        public override sealed void OnMechanismLateUpdate()
        {
            switch (gamePhase)
            {
                case Phase.DEALER_INITIALIZE_DECK:
                    UpdateDealerPlaceDeck();
                    break;
            }
            UpdateStatusMeshAnimation();
        }

        private void UpdateDealerPlaceDeck()
        {

            if (dealer == null || masterDeck == null)
            {   //there must be a dealer in the initialization phase
                EndGame();
                return;
            }
            //start game when master deck is placed on floor
            if (masterDeck.mainOwner == null)
            {
                if (deckMeshRenderer.material.mainTexture == validPlacementTexture && Vector3.SqrMagnitude(masterDeck.transform.position - transform.position) < 0.01f)
                {

                    deckMeshRenderer.enabled = false;
                    masterDeck.transform.position = transform.position + Vector3.up * PokerCard.maxDeckHeight;
                    masterDeck.transform.rotation = transform.rotation * Quaternion.Euler(180.0f, 0.0f, 0.0f);
                    masterDeck.rigidBody.isKinematic = true;
                    PrepareGame();
                }
                return;
            }
            //wait for dealer to place deck on floor
            if (!GamePhysics.GetRaycastInfo(masterDeck.transform.position, Vector3.down, 0.5f, WorldUtil.wallsMask, QueryTriggerInteraction.Ignore, 0.0f))
            {
                deckMeshRenderer.enabled = false;
                return;
            }
            deckMeshRenderer.enabled = true;
            transform.position = GamePhysics.result().point + Vector3.up * 0.015f;

            transform.rotation = Quaternion.LookRotation(Tools.FlatForward(dealer.character.head.forward), Vector3.up);

            if (LocomotionBehaviors.isOnWalkableFloor(transform.position) &&
                LocomotionBehaviors.isOnWalkableFloor(transform.position + transform.forward) &&
                LocomotionBehaviors.isOnWalkableFloor(transform.position - transform.forward) &&
                LocomotionBehaviors.isOnWalkableFloor(transform.position + transform.right) &&
                LocomotionBehaviors.isOnWalkableFloor(transform.position - transform.right))
            {
                deckMeshRenderer.material.mainTexture = validPlacementTexture;
            }
            else
            {
                deckMeshRenderer.material.mainTexture = invalidPlacementTexture;
            }
        }

        private void SendPokerGameAction(Character character, Action action)
        {
            if (character.characterType == Character.Type.LOLI)
            {
                Loli loli = character as Loli;
                loli.active.poker.OnPokerGameAction(action);
            }
            else
            {
                cardPileRenderer.enabled = action == Action.PLAY_CARD;
                pickupCardRenderer.enabled = action == Action.DRAW_CARDS;
            }
            switch (action)
            {
                case Action.DRAW_CARDS:
                    drawTargetsAllowed.Add(character);
                    break;
                case Action.WAIT:
                    drawTargetsAllowed.Remove(character);
                    break;
            }
        }

        public int RequestCardDrawsAllowed(PokerCard card, Character character)
        {

            int index = FindPlayerByCharacter(character);
            if (index == -1)
            {
                //don't let other non-game character pick any up
                return 0;
            }
            if (card == masterDeck)
            {
                if (gamePhase == Phase.DEALER_INITIALIZE_DECK)
                {
                    return -1;
                }
                //always allow player to draw 1 card if it's their turn or just starting
                if (index != turnIndex && totalTurns >= players.Count)
                {
                    return 0;
                }
                return Math.Max(1, players[index].requiredDrawCards);
            }
            //not allowed to pick up other player's cards
            if (card.mainOwner != null && card.mainOwner != character)
            {
                return 0;
            }
            //can pick up self owned or un-owned cards whole
            return -1;
        }

        public void OnDrawnFromMasterDeck(PokerCard baseCard)
        {
            int index = FindPlayerByCharacter(baseCard.mainOwner);
            if (index == -1)
            {
                return;
            }
            if (masterDeck == null)
            {
                return;
            }
            //end game if picked up card included last card in masterdeck
            if (baseCard.DoesCardGroupHaveValue(masterDeck.bottomValue))
            {
                //winner is player with the least cards
                Player leastCardsPlayer = null;
                int leastCardCount = 10000; //large integer number
                int uniqueCardCounts = 0;
                foreach (Player player in players)
                {
                    int ownedCards = PokerCard.FindAllCardsLastOwnedByCharacter(masterDeck.cardPoolIndex, player.character).Count;
                    if (ownedCards < leastCardCount)
                    {
                        leastCardCount = ownedCards;
                        leastCardsPlayer = player;
                        uniqueCardCounts = 1;
                    }
                    else if (ownedCards == leastCardCount)
                    {
                        uniqueCardCounts++;
                    }
                }
                //tie if nobody has least cards
                if (uniqueCardCounts > 1)
                {
                    leastCardsPlayer = null;
                }
                SendWinAndLoseStates(leastCardsPlayer);
            }
            else
            {
                Player player = players[index];

                //prevent main player from peeking at cards
                if (player.character.characterType == Character.Type.LOLI)
                {
                    baseCard.SetShowFrontValueModel(false);
                }
                if (player.requiredDrawCards - baseCard.cardGroupSize == 0 && cardPile.playedCards.Count > 0)
                {
                    PokerCard lastPlayed = cardPile.playedCards[cardPile.playedCards.Count - 1];
                    PokerCard.Suit lastPlayedSuit = PokerCard.GetCardSuit(lastPlayed.topValue);
                    PokerCard.GetCardType(lastPlayed.topValue);

                    SetStatusMesh((PokerGame.Status)((int)PokerGame.Status.HEART + Math.Min(4, (int)lastPlayedSuit)));
                }
                //reduce required cards
                player.requiredDrawCards = Mathf.Max(0, player.requiredDrawCards - baseCard.cardGroupSize);

                if (gamePhase == Phase.WAIT_FOR_PREPARE)
                {
                    if (AllPlayersFinished())
                    {
                        turnIndex = 0;
                        totalTurns = 0;
                        nextTurnIndex = 1;
                        gamePhase = Phase.PLAY;

                        //initialize first turn
                        players[0].requiredPlayCards = 1;

                        SendTurnActionsToAllPlayers();
                    }
                    else
                    {
                        //wait for others to prepare
                        SendPokerGameAction(player.character, Action.WAIT);
                    }
                }
                else
                {
                    if (AllPlayersFinished())
                    {
                        EndTurn();
                    }
                    SendTurnActionsToAllPlayers();
                }

                Debug.Log("[Poker Game] Drew +" + baseCard.cardGroupSize + " for " + index + " req: " + player.requiredDrawCards);
            }
        }

        private void SendWinAndLoseStates(Player winner)
        {

            if (winner != null)
            {
                Loli winnerLoli = winner.character as Loli;
                if (winnerLoli)
                {
                    winnerLoli.active.poker.OnPokerGameAction(Action.WIN);
                }
                SetStatusMesh(Status.WINNER);
            }
            else
            {
                SetStatusMesh(Status.TIE);
            }
            foreach (Player player in players)
            {
                if (player == winner)
                {
                    continue;
                }
                Loli loserLoli = player.character as Loli;
                if (loserLoli)
                {
                    loserLoli.active.poker.OnPokerGameAction(Action.LOSE);
                }
            }

            soundSource.PlayOneShot(gameWinSound);
            EndGame();
        }

        public void OnCardPlayed(PokerCard card)
        {

            if (card == null)
            {
                Debug.LogError("[Poker Game] Played card is null!");
                return;
            }
            int index = FindPlayerByCharacter(card.lastOwner);
            if (index == -1)
            {
                return;
            }
            Player player = players[index];

            //card no longer owned by anyone
            card.SetLastOwner(null);

            //restore main player peeking at card
            if (player.character.characterType == Character.Type.LOLI)
            {
                card.SetShowFrontValueModel(true);
            }
            if (player.requiredPlayCards != 0)
            {
                SetStatusMesh(Status.NONE);
            }

            player.requiredPlayCards -= card.cardGroupSize;
            if (AllPlayersFinished())
            {
                rules.PlayCardEffect(card.topValue, this);
                EndTurn();
            }
            else
            {
                rules.PlayCardEffect(card.topValue, this);
            }
            SendTurnActionsToAllPlayers();

            Debug.Log("[Poker Game] Played " + card.cardGroupSize + " req: " + player.requiredPlayCards);
        }

        public bool IsDealer(Character character)
        {
            if (character == null)
            {
                return false;
            }
            if (dealer == null)
            {
                return false;
            }
            return dealer.character == character;
        }

        private void SendTurnActionsToAllPlayers()
        {
            for (int i = 0; i < players.Count; i++)
            {
                SendPlayerTurnAction(i);
            }
        }

        private void SendPlayerTurnAction(int index)
        {
            Player player = players[index];
            Character character = player.character;
            switch (gamePhase)
            {
                case Phase.DEALER_INITIALIZE_DECK:
                    if (index == turnIndex)
                    {
                        SendPokerGameAction(character, Action.INITIALIZE_DECK);
                    }
                    else
                    {
                        SendPokerGameAction(character, Action.WAIT);
                    }
                    break;
                case Phase.PLAY:
                    if (index == turnIndex)
                    {
                        //player must first pick up enough cards before placing
                        if (player.requiredDrawCards > 0)
                        {
                            SendPokerGameAction(character, Action.DRAW_CARDS);
                        }
                        else
                        {
                            if (player.requiredPlayCards > 0)
                            {
                                SendPokerGameAction(character, Action.PLAY_CARD);
                            }
                            else
                            {
                                SendPokerGameAction(character, Action.WAIT);
                            }
                        }
                    }
                    else
                    {
                        //wait if it's not my turn
                        SendPokerGameAction(character, Action.WAIT);
                    }
                    break;
            }
        }
    }

}