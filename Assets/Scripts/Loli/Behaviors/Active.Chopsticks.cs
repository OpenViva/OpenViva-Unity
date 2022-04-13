using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace viva{


public class ChopsticksBehavior : ActiveBehaviors.ActiveTask {

	enum GameState{
		NONE,
		SHINOBU_TURN,
		PLAYER_TURN
	}

	protected class UserInfo{

		public float[] targetFingerRest = {0.0f,0.0f,0.0f,0.0f,0.0f, 0.0f,0.0f,0.0f,0.0f,0.0f};
		public float[] realFingerRest = {0.0f,0.0f,0.0f,0.0f,0.0f, 0.0f,0.0f,0.0f,0.0f,0.0f};
		public int leftFingerCount = 1;
		public int rightFingerCount = 1;
		
		private FingerAnimator leftHand;
		private FingerAnimator rightHand;
		public UserInfo( FingerAnimator left, FingerAnimator right ){
			leftHand = left;
			rightHand = right;
			updateTargetFingers();
		}

		public bool AttemptRedistributeFingers(){

			int realLeftCount = leftFingerCount;
			if( realLeftCount > 4 ){
				realLeftCount = 0;
			}
			int realRightCount = rightFingerCount;
			if( realRightCount > 4 ){
				realRightCount = 0;
			}
			if( realLeftCount == realRightCount ){ //can't if both hands are the same
				return false;
			}
			if( Mathf.Abs( realLeftCount-realRightCount ) == 1 ){ //can't mirror
				return false;
			}

			int half = (realLeftCount+realRightCount)/2;
			rightFingerCount = half;
			leftFingerCount = half;
			if( half*2 != realLeftCount+realRightCount ){
				leftFingerCount++;
			}
			updateTargetFingers();
			return true;
		}
		public void updateTargetFingers(){
			for( int i=0; i<targetFingerRest.Length; i++ ){
				targetFingerRest[i] = 0.0f;
			}
			if( leftFingerCount <= 4 ){
				for( int i=1; i<=leftFingerCount; i++ ){
					targetFingerRest[i] = 1.0f;
				}
			}
			if( rightFingerCount <= 4 ){
				for( int i=6; i<=5+rightFingerCount; i++ ){
					targetFingerRest[i] = 1.0f;
				}
			}
			string d = "";
			for( int i=0; i<targetFingerRest.Length; i++ ){
				d += targetFingerRest[i]+",";
			}
		}
	}

	private float animTargetIKBlend = 0.0f;
	private LoliHandState targetHandState;
	private Transform tappingFinger;
	private int animIKSide = 0;	//-1 left, 0 none, 1 right
	private float appliedTurnTimer = 0.0f;
	private float handDir = 0.0f;
	private FingerAnimator targetFingers = null;
	private GameState currentState = GameState.NONE;
	private UserInfo playerInfo = null;
	private UserInfo shinobuInfo = null;
	private bool showedHype = false;
	private bool waitForChopsticksIdle = false;
	private int waitTurnsUntilReachNeutral = 3;
	private bool[] movesAvailable = new bool[4];
	private int shinobuReactPlayerConfident = 0;	//stores 2 bools [0]:left [1]:right
	private int shinobuReactPlayerWorried = 0;	//stores 2 bools [0]:left [1]:right
	private float validGameTimer = 0.0f;
	private Tools.EaseBlend playerKeyboardDistributeEase = new Tools.EaseBlend();
	private bool disableFaceYawChopsticks = false;
	private bool chopsticksActive = false;


	public ChopsticksBehavior( Loli _self ):base(_self,ActiveBehaviors.Behavior.CHOPSTICKS,null){
	}

	private void BeginChopsticksGame(){
		if( !chopsticksActive ){
			chopsticksActive = true;
			GameDirector.player.rightHandState.AttemptDrop();
			GameDirector.player.leftHandState.AttemptDrop();
			self.rightHandState.AttemptDrop();
			self.leftHandState.AttemptDrop();

			GameDirector.player.rightHandState.GrabItemRigidBody( null );

			GameDirector.player.DisableGrabbing();
		}
		
		playerInfo = new UserInfo( GameDirector.player.leftHandState.fingerAnimator, GameDirector.player.rightHandState.fingerAnimator );
		shinobuInfo = new UserInfo( self.leftHandState.fingerAnimator, self.rightHandState.fingerAnimator );
		waitForChopsticksIdle = true;
		currentState = GameState.SHINOBU_TURN; //Shinobu always goes first
	}

	private void FinishGame(){
		currentState = GameState.NONE;
		showedHype = false;
		targetHandState = null;
		animTargetIKBlend = 0.0f;
		animIKSide = 0;
	}

	private void ExitChopsticksBehavior(){
		//drop dummy override objects
		GameDirector.player.rightHandState.AttemptDrop();
		GameDirector.player.leftHandState.AttemptDrop();

		GameDirector.player.EnableGrabbing();

		chopsticksActive = false;
		self.active.SetTask( self.active.idle, true );
	}

	private void StartFinishingChopsticksBehavior(){
		self.SetTargetAnimation( Loli.Animation.FLOOR_SIT_TO_STAND );
	}

	public bool IsPlayerWithinChopsticksRange(){
		float sqDist = Vector3.SqrMagnitude( self.floorPos-GameDirector.player.floorPos );
		return sqDist < 2.4f && sqDist > 0.15f;
	}

	public bool IsPlayerSittingOnFloor( bool testBearings ){
		//both players must be facing each other
		if( testBearings ){
			if( Mathf.Abs( Tools.Bearing( self.transform, GameDirector.player.floorPos ) ) > 40.0f ||
				Mathf.Abs( Tools.Bearing( GameDirector.player.head, self.floorPos ) ) > 40.0f ){
				return false;
			}
		}
		return GameDirector.player.head.position.y-GameDirector.player.floorPos.y < 0.8f;
	}

	public override void OnLateUpdate(){
		if( !chopsticksActive ){
			switch( self.bodyState ){
			case BodyState.STAND:
				self.SetTargetAnimation(Loli.Animation.STAND_TO_SIT_FLOOR);
				// self.SetRootFacingTarget( GameDirector.player.floorPos, 130.0f, 15.0f, 20.0f );
				break;
			case BodyState.FLOOR_SIT:
				if( self.currentAnim == Loli.Animation.FLOOR_SIT_LOCOMOTION_HAPPY ){
					if( !showedHype ){
						self.SetTargetAnimation( Loli.Animation.FLOOR_SIT_HYPE );
					}else{
						self.SetTargetAnimation( Loli.Animation.FLOOR_SIT_CHOPSTICKS_IN );
					}
				}
				break;
			default:
				self.active.SetTask( self.active.idle, false );
				break;
			}
		}else{
			if( PlayerLeftChopsticksLocation() ){
				StartFinishingChopsticksBehavior();
			}else{
				LateUpdateGameState();
			}
		}
	}

	private bool PlayerEnteredChopsticksLocation(){
		if( IsPlayerSittingOnFloor( true ) && IsPlayerWithinChopsticksRange() ){
			
			validGameTimer += Time.deltaTime;
			if( validGameTimer > 2.0f ){
				validGameTimer = 0.0f;
				return true;
			}
		}else{
			validGameTimer = 0;	
		}
		return false;
	}

	private bool PlayerLeftChopsticksLocation(){
		if( !IsPlayerSittingOnFloor( false ) || !IsPlayerWithinChopsticksRange() ){
			
			validGameTimer += Time.deltaTime;
			if( validGameTimer > 2.0f ){
				validGameTimer = 0.0f;
				return true;
			}
		}else{
			validGameTimer = 0;			
		}
		return false;
	}

	private void LateUpdateGameState(){
		switch( currentState ){
		case GameState.NONE:
			break;
		case GameState.SHINOBU_TURN:
			UpdateChopsticksAnim();
			if( animIKSide == 0 ){
				if( appliedTurnTimer == 0.0f ){
					if( !waitForChopsticksIdle ){
						if( waitTurnsUntilReachNeutral-- == 0 ){
							waitTurnsUntilReachNeutral = 4+(int)(Random.value*6);
							self.SetTargetAnimation( Loli.Animation.FLOOR_SIT_CHOPSTICKS_NEUTRAL );
							waitForChopsticksIdle = true;
						}else{
							WaitUntilTapHandShinobuTurn();			
						}
					}
				}else{
					ReactConfident();
					EndTurn();
				}
			}
			UpdateReachToPlayerHandAnim();
			break;

		case GameState.PLAYER_TURN:
			UpdateChopsticksAnim();
			
			if( appliedTurnTimer == 0.0f ){
				WaitUntilTapHandPlayerTurn();
			}else{
				appliedTurnTimer += Time.deltaTime;
				if( appliedTurnTimer > 0.5f ){
					if( !PlayerHandNearShinobuHands() && !waitForChopsticksIdle ){
						ReactWorried();
						EndTurn();
					}
				}
			}
			break;
		}
	}

	private void UpdatePlayerKeyboardDistributeControls(){
		//move hands close to each other if user is in keyboard mode
		if( GameDirector.player.controls == Player.ControlType.KEYBOARD ){
			playerKeyboardDistributeEase.Update( Time.deltaTime );
			Vector3 center = (GameDirector.player.rightHandState.fingerAnimator.hand.parent.position+GameDirector.player.leftHandState.fingerAnimator.hand.parent.position)*0.5f;
			Vector3 offset = GameDirector.player.head.right;
            GameDirector.player.rightHandState.fingerAnimator.hand.parent.position = Vector3.LerpUnclamped(
				GameDirector.player.rightHandState.fingerAnimator.hand.parent.position,
				center+offset*0.03f,
				playerKeyboardDistributeEase.value
			);
			GameDirector.player.leftHandState.fingerAnimator.hand.parent.position = Vector3.LerpUnclamped(
				GameDirector.player.leftHandState.fingerAnimator.hand.parent.position,
				center-offset*0.05f,
				playerKeyboardDistributeEase.value
			);
		}
	}

	private void UpdateChopsticksAnim(){
		
		// playerInfo.ApplyChopsticksAnim(
		// 	GameDirector.player.getUtilityHoldForm(Player.UtilityHoldForms.PALM_EXTEND_LEFT),
		// 	GameDirector.player.getUtilityHoldForm(Player.UtilityHoldForms.PALM_EXTEND_RIGHT)
		// );
		UpdatePlayerKeyboardDistributeControls();
	}

	public void EndTurn(){
		if( currentState == GameState.SHINOBU_TURN ){
			if( CheckShinobuWon() ){
				self.SetTargetAnimation( Loli.Animation.FLOOR_SIT_CHOPSTICKS_WIN );
				FinishGame();
			}else{
				currentState = GameState.PLAYER_TURN;
			}
		}else if( currentState == GameState.PLAYER_TURN ){
			if( CheckPlayerWon() ){
				self.SetTargetAnimation( Loli.Animation.FLOOR_SIT_CHOPSTICKS_LOSE );
				// GameDirector.player.CompleteAchievement(Player.ObjectiveType.BEAT_AT_CHOPSTICKS);
				FinishGame();
			}else{
				currentState = GameState.SHINOBU_TURN;
			}
		}
		appliedTurnTimer = 0.0f;
	}
	public void WaitUntilTapHandShinobuTurn(){

		if( Vector3.Distance( self.floorPos, GameDirector.player.floorPos ) > 1.8f ){
			return;
		}
		//attempt redistribute first
		if( shinobuInfo.rightFingerCount > 4 || shinobuInfo.leftFingerCount > 4 ){
			if( ( shinobuInfo.rightFingerCount > 1 && shinobuInfo.rightFingerCount <= 4 ) || (shinobuInfo.leftFingerCount > 1 && shinobuInfo.leftFingerCount <= 4 ) ){
				self.SetTargetAnimation( Loli.Animation.FLOOR_SIT_CHOPSTICKS_REDISTRIBUTE );
				waitForChopsticksIdle = true;
				if( shinobuInfo.rightFingerCount > 4 ){
					self.SetLookAtTarget( self.leftHandState.fingerAnimator.fingers[3] );
				}else{
					self.SetLookAtTarget( self.rightHandState.fingerAnimator.fingers[3] );
				}
				return;
			}
		}
		//attack
		for( int i=0; i<4; i++ ){
			movesAvailable[i] = false;
		}
		bool killMove = false;
		if( shinobuInfo.leftFingerCount <= 4  ){
			if( playerInfo.leftFingerCount+shinobuInfo.leftFingerCount > 4 && playerInfo.leftFingerCount <= 4 ){
				movesAvailable[0] = true;
				killMove = true;
			}
			if( playerInfo.rightFingerCount+shinobuInfo.leftFingerCount > 4 && playerInfo.rightFingerCount <= 4 ){
				movesAvailable[1] = true;
				killMove = true;
			}
		}
		if( shinobuInfo.rightFingerCount <= 4 ){
			if( playerInfo.leftFingerCount+shinobuInfo.rightFingerCount > 4 && playerInfo.leftFingerCount <= 4 ){
				movesAvailable[2] = true;
				killMove = true;
			}
			if( playerInfo.rightFingerCount+shinobuInfo.rightFingerCount > 4 && playerInfo.rightFingerCount <= 4 ){
				movesAvailable[3] = true;
				killMove = true;
			}
		}
		if( killMove ){	//prioritize removing GameDirector.player hand moves
			ChoseRandomMoveAvailable();	
		}else{
			for( int i=0; i<4; i++ ){
				movesAvailable[i] = false;
			}
			if( shinobuInfo.leftFingerCount <= 4 ){
				if( playerInfo.leftFingerCount <= 4 ){
					movesAvailable[0] = true;
				}
				if( playerInfo.rightFingerCount <= 4 ){
					movesAvailable[1] = true;
				}
			}
			if( shinobuInfo.rightFingerCount <= 4 ){
				if( playerInfo.leftFingerCount <= 4 ){
					movesAvailable[2] = true;
				}
				if( playerInfo.rightFingerCount <= 4 ){
					movesAvailable[3] = true;
				}
			}
			ChoseRandomMoveAvailable();
		}
	}

	public void ChoseRandomMoveAvailable(){	//4 possibilities
		int randomIndex = (int)(Random.value*4.0f);
		for( int i=0; i<4; i++ ){
			if( movesAvailable[ (randomIndex+i)%4 ] ){
				randomIndex = (randomIndex+i)%4;
				break;
			}
		}
		string d ="";
		for( int i=0; i<4; i++ ){
			d += movesAvailable[i]+",";
		}
		PlayReachToPlayerHandAnim( randomIndex>=2, (randomIndex%2)==1 );
	}

	public bool IsNearShinobuHand( Vector3 position, FingerAnimator shinobuHand, float minDist = 0.025f ){
		int offset = (shinobuHand==self.leftHandState.fingerAnimator)?0:5;
		for( int i=0; i<5; i++ ){	//test if near any active fingers
			if( shinobuInfo.targetFingerRest[i+offset] == 1.0f ){
				if( Vector3.Distance( position, shinobuHand.fingers[i*3].position ) < minDist ){
					return true;
				}
				if( Vector3.Distance( position, shinobuHand.fingers[i*3+1].position ) < minDist ){
					return true;
				}
				if( Vector3.Distance( position, shinobuHand.fingers[i*3+2].position ) < minDist ){
					return true;
				}
			}
		}
		return false;
	}

	public Vector3 GetPlayerAverageFrontFingerPosition( FingerAnimator playerHand ){
		int offset = (playerHand==GameDirector.player.leftHandState.fingerAnimator)?0:5;
		int valid = 0;
		Vector3 sum = Vector3.zero;
		for( int i=0; i<5; i++ ){
			if( playerInfo.targetFingerRest[i+offset] == 1.0f ){
				sum += playerHand.fingers[i*3].position;
				valid++;
			}
		}
		return sum/(float)valid;
	}

	public void WaitUntilTapHandPlayerTurn(){
	
		FingerAnimator playerRightFingers = GameDirector.player.rightHandState.fingerAnimator;
		FingerAnimator playerLeftFingers = GameDirector.player.leftHandState.fingerAnimator;
		Vector3 frontRightFingers = GetPlayerAverageFrontFingerPosition( playerRightFingers );
		Vector3 frontLeftFingers = GetPlayerAverageFrontFingerPosition( playerLeftFingers );
		
		// if( InputOLD.GetKeyDown(KeyCode.Space) ){
		// 	playerKeyboardDistributeEase.StartBlend( 1.0f, 0.3f );
		// }

		if( playerInfo.rightFingerCount <= 4 && IsNearShinobuHand( frontRightFingers, self.rightHandState.fingerAnimator ) ){
			ApplyPlayerTurn( playerRightFingers, self.rightHandState.fingerAnimator );
		}else if( playerInfo.rightFingerCount <= 4 && IsNearShinobuHand( frontRightFingers, self.leftHandState.fingerAnimator ) ){
			ApplyPlayerTurn( playerRightFingers, self.leftHandState.fingerAnimator );
		}else if( playerInfo.leftFingerCount <= 4 && IsNearShinobuHand( frontLeftFingers, self.rightHandState.fingerAnimator ) ){
			ApplyPlayerTurn( playerLeftFingers, self.rightHandState.fingerAnimator );
		}else if( playerInfo.leftFingerCount <= 4 && IsNearShinobuHand( frontLeftFingers, self.leftHandState.fingerAnimator ) ){
			ApplyPlayerTurn( playerLeftFingers, self.leftHandState.fingerAnimator );
		}else if( Vector3.SqrMagnitude( playerRightFingers.hand.position-playerLeftFingers.hand.position ) <= 0.01f ){
			playerKeyboardDistributeEase.StartBlend( 0.0f, 0.4f );
			if( playerInfo.AttemptRedistributeFingers() ){
				appliedTurnTimer = Time.deltaTime;
				shinobuReactPlayerConfident = 0;
			}
		}
	}

	public bool PlayerHandNearShinobuHands(){
		Vector3 frontRightFingers = GetPlayerAverageFrontFingerPosition( GameDirector.player.rightHandState.fingerAnimator );
		Vector3 frontLeftFingers = GetPlayerAverageFrontFingerPosition( GameDirector.player.leftHandState.fingerAnimator );
		if( IsNearShinobuHand( frontRightFingers, self.rightHandState.fingerAnimator, 0.06f ) ||
			IsNearShinobuHand( frontRightFingers, self.leftHandState.fingerAnimator, 0.06f ) ||
			IsNearShinobuHand( frontLeftFingers, self.rightHandState.fingerAnimator, 0.06f ) ||
			IsNearShinobuHand( frontLeftFingers, self.leftHandState.fingerAnimator, 0.06f ) ){
			return true;
		}
		return false;
	}

	public void ApplyPlayerTurn( FingerAnimator playerHand, FingerAnimator shinobuHand ){

		if( shinobuHand == self.rightHandState.fingerAnimator ){
			if( playerHand == GameDirector.player.rightHandState.fingerAnimator ){
				shinobuInfo.rightFingerCount += playerInfo.rightFingerCount;
			}else{
				shinobuInfo.rightFingerCount += playerInfo.leftFingerCount;
			}
			self.SetTargetAnimation( Loli.Animation.FLOOR_SIT_CHOPSTICKS_RECEIVE_RIGHT );
		}else{
			if( playerHand == GameDirector.player.rightHandState.fingerAnimator ){
				shinobuInfo.leftFingerCount += playerInfo.rightFingerCount;
			}else{
				shinobuInfo.leftFingerCount += playerInfo.leftFingerCount;
			}
			self.SetTargetAnimation( Loli.Animation.FLOOR_SIT_CHOPSTICKS_RECEIVE_LEFT );
		}
		shinobuInfo.updateTargetFingers();
		self.SetLookAtTarget( shinobuHand.hand.transform );

		appliedTurnTimer = Time.deltaTime;
	}

	public bool CheckPlayerWon(){
		return ( shinobuInfo.rightFingerCount > 4 && shinobuInfo.leftFingerCount > 4 );
	}
	public bool CheckShinobuWon(){
		return ( playerInfo.rightFingerCount > 4 && playerInfo.leftFingerCount > 4 );
	}

	public void ReactWorried(){
		if( Random.value < 0.4 ){
			return;
		}
		if( shinobuInfo.rightFingerCount >= 4 && (shinobuReactPlayerWorried&2) == 0 ){
			shinobuReactPlayerWorried += 2;
			self.SetTargetAnimation( Loli.Animation.FLOOR_SIT_CHOPSTICKS_WORRIED );
			waitForChopsticksIdle = true;
		}else if( shinobuInfo.leftFingerCount >= 4 && (shinobuReactPlayerWorried&1) == 0 ){
			shinobuReactPlayerWorried += 1;
			self.SetTargetAnimation( Loli.Animation.FLOOR_SIT_CHOPSTICKS_WORRIED );
			waitForChopsticksIdle = true;
		}
	}

	public void ApplyShinobuTurn(){
		if( targetFingers == GameDirector.player.rightHandState.fingerAnimator ){
			if( animIKSide == -1 ){
				playerInfo.rightFingerCount += shinobuInfo.leftFingerCount;
			}else{
				playerInfo.rightFingerCount += shinobuInfo.rightFingerCount;
			}
		}else{
			if( animIKSide == -1 ){
				playerInfo.leftFingerCount += shinobuInfo.leftFingerCount;
			}else{
				playerInfo.leftFingerCount += shinobuInfo.rightFingerCount;
			}
		}
		playerInfo.updateTargetFingers();
		appliedTurnTimer = Time.deltaTime;
	}

	public void ReactConfident(){
		if( Random.value < 0.4 ){
			return;
		}
		if( playerInfo.rightFingerCount > 4 && (shinobuReactPlayerConfident&2) == 0 ){
			shinobuReactPlayerConfident += 2;
			self.SetTargetAnimation( Loli.Animation.FLOOR_SIT_CHOPSTICKS_CONFIDENT );
			waitForChopsticksIdle = true;
		}else if( playerInfo.leftFingerCount > 4 && (shinobuReactPlayerConfident&1) == 0 ){
			shinobuReactPlayerConfident += 1;
			self.SetTargetAnimation( Loli.Animation.FLOOR_SIT_CHOPSTICKS_CONFIDENT );
			waitForChopsticksIdle = true;
		}
	}

	public void UpdateReachToPlayerHandAnim(){
		if( animIKSide == 0 ){
			return;
		}

		float tapSpeed = 2.0f-Mathf.Max( 0.0f, Vector3.Distance( targetHandState.fingerAnimator.hand.position, targetFingers.hand.position ) );
		animTargetIKBlend += Time.deltaTime*tapSpeed*handDir;
		if( animTargetIKBlend >= 1.0f ){
			animTargetIKBlend = 1.0f;
			handDir = -0.75f;
			ApplyShinobuTurn();
		}else if( animTargetIKBlend <= 0.0f ){
			animTargetIKBlend = 0.0f;
			handDir = 0.0f;
			animIKSide = 0;
		}

		Vector3 targetPos = targetFingers.hand.position+targetFingers.hand.up*0.15f+targetFingers.hand.forward*0.05f;
		Vector3 handToTarget = ( targetPos-targetHandState.holdArmIK.ik.p0.position ).normalized;

		self.animator.SetFloat( Instance.chopsticksReachID, Tools.EaseInOutQuad( animTargetIKBlend ) );
		
		float offsetBlend = 0.0f;
		if( animTargetIKBlend < 0.5f ){
			offsetBlend = Tools.EaseInOutQuad( animTargetIKBlend*2.0f );
		}else{
			offsetBlend = Tools.EaseOutQuad( 1.0f-( animTargetIKBlend-0.5f )*2.0f );
		}
		
		Vector3 handEuler = Vector3.zero;
		handEuler.x = Mathf.Lerp( 217.1f, -37.1f, (1.0f+animIKSide)*0.5f );
		handEuler.y = 90.0f+10.0f*animIKSide+20.0f*animIKSide*offsetBlend;
		handEuler.z = 90.0f-40.0f*animIKSide*offsetBlend;
		
		// targetHandState.overrideRetargeting.SetupRetargeting(
		// 	Vector3.LerpUnclamped( targetHandState.fingerAnimator.hand.position, targetPos-handToTarget*0.14f, targetHandState.overrideRetargeting.blend.value ),
		// 	targetHandState.armIK.ik.p1.position+( targetHandState.armIK.ik.p0.up-targetHandState.armIK.ik.p1.up )*0.5f,
		// 	Quaternion.LookRotation( handToTarget, targetHandState.armIK.spine2RigidBodyTransform.up )*Quaternion.Euler( handEuler ),
		// 	Tools.EaseInOutQuad( animTargetIKBlend )
		// );

		tappingFinger.localRotation = tappingFinger.localRotation*Quaternion.Euler( 8.0f*offsetBlend, 0.0f, -12.0f*animIKSide*offsetBlend );
	}

	private void taskChopsticksCallback( Character self, Occupation oldReg, OccupyState action ){
		// if( action == HoldType.NONE ){
			StartFinishingChopsticksBehavior();
		// }
	}


	public void PlayReachToPlayerHandAnim( bool shinobuRightHand, bool playerRightHand ){
		if( shinobuRightHand ){
			animIKSide = 1;
			targetHandState = self.rightLoliHandState;
			tappingFinger = self.rightHandState.fingerAnimator.fingers[3];
		}else{
			animIKSide = -1;
			targetHandState = self.leftLoliHandState;
			tappingFinger = self.leftHandState.fingerAnimator.fingers[3];
		}
		if( playerRightHand ){
			targetFingers = GameDirector.player.rightHandState.fingerAnimator;
		}else{
			targetFingers = GameDirector.player.leftHandState.fingerAnimator;
		}
		animTargetIKBlend = 0.0f;
		handDir = 1.0f;
		self.SetLookAtTarget( targetFingers.hand.transform );
	}

	public void AttemptChopsticks(){
		if( self.bodyState != BodyState.STAND ){
			return;
		}
		if( !self.IsCurrentAnimationIdle() ){
			return;
		}
		if( !self.IsHappy() ){
			self.active.idle.PlayAvailableRefuseAnimation();
			return;
		}
		self.active.SetTask( this, null );
	}
	public void RedistributeChopsticksFingers(){
		if( shinobuInfo.AttemptRedistributeFingers() ){
			appliedTurnTimer = Time.deltaTime;
		}
	}
	
	public override void OnAnimationChange( Loli.Animation oldAnim, Loli.Animation newAnim ){

		if( oldAnim == Loli.Animation.FLOOR_SIT_HYPE ){
			showedHype = true;
		}
		switch( oldAnim ){
		case Loli.Animation.FLOOR_SIT_HYPE:
			showedHype = true;
			break;
		case Loli.Animation.STAND_TO_SIT_FLOOR:
			self.ApplyDisableFaceYaw( ref disableFaceYawChopsticks );
			break;
		case Loli.Animation.FLOOR_SIT_CHOPSTICKS_WIN:
			self.SetTargetAnimation( Loli.Animation.FLOOR_SIT_CHOPSTICKS_IN );
			break;
		}
		switch( newAnim ){
		case Loli.Animation.FLOOR_SIT_CHOPSTICKS_IDLE:
			waitForChopsticksIdle = false;
			break;
		case Loli.Animation.FLOOR_SIT_CHOPSTICKS_IN:
			BeginChopsticksGame();
			break;
		case Loli.Animation.FLOOR_SIT_TO_STAND:
			self.RemoveDisableFaceYaw( ref disableFaceYawChopsticks );
			ExitChopsticksBehavior();
			break;
		}
	}
}

}