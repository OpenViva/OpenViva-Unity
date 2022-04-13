using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;




namespace viva{

public class PokerBehavior: ActiveBehaviors.ActiveTask{

	private PokerGame activeGame = null;
	private PokerGame.Action currentAction = PokerGame.Action.WAIT;
	private int? selectedCardValue = null;
	private bool selectedFromRightCard = false;
	public bool isInGame { get{ return activeGame != null; } }

	public PokerBehavior( Loli loli ):base(loli,ActiveBehaviors.Behavior.POKER,null){
	}

	public bool AttemptJoinPokerGame( PokerCard pokerCard ){
		
		//must be happy
		if( !self.IsHappy() ){
			return false;
		}
		//cannot join while already in another game
		if( activeGame != null ){
			Debug.LogError("[Poker Game] Already in-game");
			return false;
		}
		//Create game if pokerCard has no game
		PokerGame pokerGame = pokerCard.parentGame;
		if( pokerGame == null ){
			pokerGame = pokerCard.CreatePokerGame( pokerCard.lastOwner );
			if( pokerGame == null ){
				Debug.LogError("[Poker Game] Could not create a game!");
				return false;
			}
		}
		if( !pokerGame.AttemptAddPlayer( self ) ){
			Debug.Log("[Poker Game] Could not join Poker");
			return false;
		}
		activeGame = pokerGame;
		GameDirector.player.objectFingerPointer.selectedLolis.Remove( self );
		return true;
	}
	
	public override void OnActivate(){
		
		GameDirector.player.objectFingerPointer.selectedLolis.Remove( self );
		self.characterSelectionTarget.OnUnselected();
	}

	private void QuitPokerGame(){
		Debug.Log("[Poker] Quit game");
		if( activeGame != null ){
			activeGame.RemoveFromGame( self );
			activeGame = null;
		}
		self.active.SetTask( self.active.idle );
	}

	public void PickSelectedCard( bool inRightHand ){
		Debug.Log(self.GetLayerAnimNormTime(1));
		PokerCard targetBaseCard;
		PokerCard otherHandCard;
		if( inRightHand ){
			targetBaseCard = self.rightHandState.GetItemIfHeld<PokerCard>();
			otherHandCard = self.leftHandState.GetItemIfHeld<PokerCard>();
		}else{
			targetBaseCard = self.leftHandState.GetItemIfHeld<PokerCard>();
			otherHandCard = self.rightHandState.GetItemIfHeld<PokerCard>();
		}
		if( targetBaseCard == null ){
			Debug.LogError("[Poker] base card not found while picking card");
			return;
		}

		//add all other hand cards to other hand if possible
		if( otherHandCard ){
			List<PokerCard> addCards = otherHandCard.DropAllFanGroupCards();
			otherHandCard.mainOccupyState.AttemptDrop();
			addCards.Add( otherHandCard );
			targetBaseCard.AddToFanGroup( addCards );
		}
		if( selectedCardValue.HasValue ){
			PokerCard card = targetBaseCard.ExtractCard( selectedCardValue.Value );
			if( card == null ){
				Debug.LogError("[Poker] No card found to extract");
				return;
			}
			if( inRightHand ){
				self.leftHandState.GrabItemRigidBody( card );
			}else{
				self.rightHandState.GrabItemRigidBody( card );
			}
			Debug.Log("[Poker] Picked "+selectedCardValue.Value);
		}else{
			Debug.Log("[Poker] Moved cards to other hand");
		}
	}

	private int? FindPlayableCardValue(){

		PokerCard rightCard = self.rightHandState.GetItemIfHeld<PokerCard>();
		PokerCard leftCard = self.leftHandState.GetItemIfHeld<PokerCard>();

		List<int> cardValues = new List<int>();
		PokerGame game = null;
		if( rightCard && rightCard.parentGame != null ){
			game = rightCard.parentGame;
			cardValues.AddRange( rightCard.GetCardGroupValues() );
		}
		if( leftCard && leftCard.parentGame != null ){
			if( game != null && game != leftCard.parentGame ){
				Debug.LogError("[Poker] Cards have different games!");
				return null;
			}
			game = leftCard.parentGame;
			cardValues.AddRange( leftCard.GetCardGroupValues() );
		}
		if( game == null ){
			Debug.LogError("[Poker] game in cards is null!");
			return null;
		}

		//reduce into playable picks
		for( int i=cardValues.Count; i-->0; ){
			if( !game.rules.IsCardPlayValid( cardValues[i], game.cardPile ) ){
				cardValues.RemoveAt(i);
			}
		}
		if( cardValues.Count == 0 ){
			Debug.Log("[Poker] no valid card values found");
			return null;
		}
		int result = cardValues[ UnityEngine.Random.Range( 0, cardValues.Count ) ];
		selectedFromRightCard = rightCard && rightCard.DoesCardGroupHaveValue( result );
		return result;
	}
	
	public void OnPokerGameAction( PokerGame.Action action ){
		// Debug.Log("[Poker] "+action+";"+param);
		if( action == currentAction ){
			return;
		}
		currentAction = action;
		selectedCardValue = null;
		switch( currentAction ){
		case PokerGame.Action.QUIT_GAME:	//quit poker behavior
			self.active.SetTask( self.active.idle, false );
			self.autonomy.SetAutonomy( new AutonomyEnforceBodyState( self.autonomy, "quit poker game", BodyState.STAND ) );
			return;
		case PokerGame.Action.INITIALIZE_DECK:
			break;
		case PokerGame.Action.WAIT:
			// var waitEmpty = new AutonomyEmpty( self.autonomy, "wait for turn empty" );
			// waitEmpty.AddRequirement( GenerateMoveToPlayerSpot() );
			// self.autonomy.SetAutonomy( waitEmpty );
			self.autonomy.SetAutonomy( GenerateMoveToPlayerSpot() );
			break;
		case PokerGame.Action.DRAW_CARDS:
			var drawCards = GenerateDrawCardsTask();
			drawCards.PrependRequirement( GenerateWaitForTurn() );
			self.autonomy.SetAutonomy( drawCards );
			break;
		case PokerGame.Action.PLAY_CARD:
		
			var playCard = GeneratePlayCardTask();
			playCard.PrependRequirement( GenerateWaitForTurn() );
			self.autonomy.SetAutonomy( playCard );
			break;
		case PokerGame.Action.WIN:
			self.ShiftHappiness( 1 );
			var winAnim = new AutonomyPlayAnimation( self.autonomy, "win anim", Loli.Animation.FLOOR_SIT_CHOPSTICKS_WIN );
			self.autonomy.SetAutonomy( winAnim );
			winAnim.onSuccess += QuitPokerGame;
			break;
			
		case PokerGame.Action.LOSE:
			self.ShiftHappiness( -1 );
			var loseAnim = new AutonomyPlayAnimation( self.autonomy, "lose anim", Loli.Animation.FLOOR_SIT_CHOPSTICKS_LOSE );
			self.autonomy.SetAutonomy( loseAnim );
			loseAnim.onSuccess += QuitPokerGame;
			break;
		}
	}

	private AutonomyMoveTo GenerateMoveToPlayerSpot(){
		var waitAtPlayerSpot = new AutonomyMoveTo( self.autonomy, "wait for turn", delegate(TaskTarget target){
			int playerIndex = activeGame.FindPlayerByCharacter( self );
			float ratio = (float)playerIndex/activeGame.playerCount;
			Vector3 waitPos = activeGame.masterDeck.transform.position+Quaternion.Euler( 0.0f, ratio*360.0f, 0.0f )*activeGame.transform.forward;

			target.SetTargetPosition( waitPos );
			Tools.DrawCross( waitPos, Color.cyan, 0.3f );

		}, 0.2f, BodyState.FLOOR_SIT, delegate( TaskTarget target ){
			target.SetTargetPosition( activeGame.masterDeck.transform.position );
		});
		return waitAtPlayerSpot;
	}

	private AutonomyFilterUse GenerateWaitForTurn(){
		var waitFilter = new AutonomyFilterUse( self.autonomy, "poker turn", activeGame.turnFilter, 1.5f );
		waitFilter.AddPassive( GenerateMoveToPlayerSpot() );
		return waitFilter;
	}

	private int? PickRandomCard( PokerCard rightCard, PokerCard leftCard, ref bool fromRightCard ){
		int cardTotal = 0;
		int rightCardCount = 0;
		if( rightCard ){
			cardTotal += rightCard.cardGroupSize;
			rightCardCount = rightCard.cardGroupSize;
		}
		if( leftCard ){
			cardTotal += leftCard.cardGroupSize;
		}
		if( cardTotal == 0 ){
			return null;
		}
		int randomIndex = UnityEngine.Random.Range( 0, cardTotal );
		if( rightCard && randomIndex < rightCardCount ){
			fromRightCard = true;
			return rightCard.GetCardGroupValueByIndex( randomIndex );
		}else if( leftCard ){
			fromRightCard = false;
			return leftCard.GetCardGroupValueByIndex( randomIndex-rightCardCount );
		}
		Debug.LogError("[Poker] all cards are null!");
		return null;
	}

	private AutonomyPickup GenerateDrawCardsTask(){
		

		var combineHandsTask = new AutonomyPlayAnimation( self.autonomy, "combine hands", Loli.Animation.NONE );

		var waitForIdle = new AutonomyWaitForIdle( self.autonomy, "wait for combine hands idle" );

		var drawCards = new AutonomyPickup( self.autonomy, "draw card", activeGame.masterDeck, self.rightLoliHandState );
		drawCards.moveTo.preferredBodyState = BodyState.FLOOR_SIT;
		drawCards.PrependRequirement( combineHandsTask );
		drawCards.PrependRequirement( waitForIdle );

		waitForIdle.onSuccess += delegate{
			PokerCard rightCard = self.rightHandState.GetItemIfHeld<PokerCard>();
			PokerCard leftCard = self.leftHandState.GetItemIfHeld<PokerCard>();
			LoliHandState pickupHandState = null;
			if( !rightCard ){
				pickupHandState = self.rightLoliHandState;
			}else if( !leftCard ){
				pickupHandState = self.leftLoliHandState;
			}

			if( pickupHandState == null ){
				Loli.Animation combineAnim;
				if( rightCard.cardGroupSize < leftCard.cardGroupSize ){
					combineAnim = Loli.Animation.FLOOR_SIT_PICK_CARD_LEFT;
					pickupHandState = self.rightLoliHandState;
				}else{
					combineAnim = Loli.Animation.FLOOR_SIT_PICK_CARD_RIGHT;
					pickupHandState = self.leftLoliHandState;
				}
				selectedCardValue = null;
				
				combineHandsTask.OverrideAnimations( combineAnim );
			}else{
				combineHandsTask.FlagForSuccess();
			}
			drawCards.SetTargetHandState( pickupHandState );

			self.autonomy.RestartValidationHierarchy();
		};

		return drawCards;
	}
	
	private AutonomyDrop GeneratePlayCardTask(){
		var playCardsContainer = new AutonomyDrop( self.autonomy, "play card", null, activeGame.cardPile.transform.position+Vector3.up*0.1f );
		playCardsContainer.moveTo.preferredBodyState = BodyState.FLOOR_SIT;
		playCardsContainer.onDropped += delegate{
			activeGame.cardPile.PlayCard( playCardsContainer.targetItem as PokerCard );
		};

		var ensureSelectedCardValue = new AutonomyEmpty( self.autonomy, "ensure card to play", delegate{
			if( selectedCardValue.HasValue ){
				return true;
			}
			return null;
		} );
		AutonomyPickup drawCards = null;

		ensureSelectedCardValue.onRegistered += delegate{
			if( drawCards != null ){
				ensureSelectedCardValue.RemoveRequirement( drawCards );
			}
			drawCards = GenerateDrawCardsTask();
			drawCards.onSuccess += delegate{
				self.autonomy.RestartValidationHierarchy();
				selectedCardValue = FindPlayableCardValue();
			};
			ensureSelectedCardValue.AddRequirement( drawCards );
		};
		ensureSelectedCardValue.onSuccess += delegate{
			ensureSelectedCardValue.RemoveRequirement( drawCards );
		};

		PokerCard targetPlayCard = null;
		var ensureHeldIn1Hand = new AutonomyEmpty( self.autonomy, "ensure held in 1 hand", delegate{
			PokerCard rightCard = self.rightHandState.GetItemIfHeld<PokerCard>();
			PokerCard leftCard = self.leftHandState.GetItemIfHeld<PokerCard>();
			bool holdingInRight = rightCard && rightCard.cardGroupSize == 1 && rightCard.topValue == selectedCardValue.Value;
			bool holdingInLeft = leftCard && leftCard.cardGroupSize == 1 && leftCard.topValue == selectedCardValue.Value;
			//if not holding selected card individually
			if( !holdingInRight && !holdingInLeft ){
				return null;
			}
			targetPlayCard = holdingInRight ? rightCard : leftCard;
			return true;
		} );
		var playEnsureHeldAnim = new AutonomyPlayAnimation( self.autonomy, "play ensure held anim", Loli.Animation.NONE );
		ensureHeldIn1Hand.AddPassive( playEnsureHeldAnim );
		playEnsureHeldAnim.onRegistered += delegate{
			Loli.Animation animation = selectedFromRightCard ? Loli.Animation.FLOOR_SIT_PICK_CARD_RIGHT : Loli.Animation.FLOOR_SIT_PICK_CARD_LEFT;
			playEnsureHeldAnim.OverrideAnimations( animation );
		};

		ensureHeldIn1Hand.onSuccess += delegate{
			playCardsContainer.SetItem( targetPlayCard );
		};

		playCardsContainer.PrependRequirement( ensureHeldIn1Hand );
		playCardsContainer.PrependRequirement( new AutonomyWaitForIdle( self.autonomy, "wait for idle" ) );
		playCardsContainer.PrependRequirement( ensureSelectedCardValue );

		
		return playCardsContainer;
	}
}

}