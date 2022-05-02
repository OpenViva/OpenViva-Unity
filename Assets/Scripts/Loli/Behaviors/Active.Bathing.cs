using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;



namespace viva{

public class BathingBehavior : ActiveBehaviors.ActiveTask {
	
	public enum BathingPhase{
		NONE,
		TEST_WATER_TEMPERATURE,
		TESTING_WATER,
		COMMAND_PLAYER_TO_GET_OUT,
		BATHING,
		ON_KNEES,
	}

	private static int bubbleSizeID = Shader.PropertyToID("_BubbleSize");

	private BathingPhase bathingPhase = BathingPhase.NONE;
	private Bathtub bathtub = null;
	private bool shinobuIsOnRightSide;
	private Vector3? targetBathPos = null;
	private Loli.Animation bathingWaterTempAnim = Loli.Animation.NONE;
	private readonly float maxPointingBearing = 100.0f;
	private float bathroomSayGetOutTimer = 1.0f;
	private float refreshLookAtTargetTimer = 0.0f;
	private float bathtubIdleAnimTimer = 5.0f;
	private float hairWashedPercent = 0.0f;
	private float lastHeadScrubTime = -Mathf.Infinity;
	private float headScrubTime = 0.0f;
	private int splashBackCount = 0;
	private float splashBackTimer = 0.0f; 
    private GameObject bubbleMeter = null;
    private Coroutine bubbleMeterCoroutine = null;
	private bool disabledFalling = false;

	private List<GameObject> activeDynBoneBubbles = new List<GameObject>();

	public BathingBehavior( Loli _self ):base(_self,ActiveBehaviors.Behavior.BATHING,null){
	}

	public void TowelPickupCallback( OccupyState source, Item olditem, Item newItem ){
		if( newItem != null && newItem.settings.itemType == Item.Type.TOWEL ){
			self.SetLookAtTarget( null );
			self.SetViewAwarenessTimeout( 0.0f );
			self.SetTargetAnimation( Loli.Animation.BATHTUB_ON_KNEES_TO_TOWEL_IDLE );
		}
	}

	// public override void OnActivate(){
	// 	splashBackCount = 0;
	// 	splashBackTimer = 0.0f;
	// }

	public override void OnDeactivate(){
		bathingPhase = BathingPhase.NONE;
		StopBubbleMeter();
		
		self.StopActiveAnchor();

		// ResetIfTowelIsHeld( self.rightHandState.heldItem );
		// ResetIfTowelIsHeld( self.leftHandState.heldItem );

		switch( self.bodyState ){
		case BodyState.BATHING_IDLE:
		case BodyState.BATHING_ON_KNEES:
		case BodyState.BATHING_RELAX:
		case BodyState.BATHING_STAND:
			self.OverrideClearAnimationPriority();
			self.SetTargetAnimation( Loli.Animation.STAND_HAPPY_IDLE2 );
			self.SetOutfit( self.outfit );	Debug.LogError("NEED TO FIX");
			self.Teleport(
				bathtub.GetBathtubModelTransform().position+bathtub.GetBathtubModelTransform().forward*0.6f,
				self.transform.rotation
			);
			break;
		default:
			//was not int the bathtub yet
			break;
		}
		bathtub.EndUse(self);
		bathtub = null;
		
		GameDirector.instance.LockMusic( false );
		GameDirector.instance.SetMusic( GameDirector.instance.GetDefaultMusic() );

		self.RemoveDisableBalanceLogic( ref disabledFalling );
	}

	public void PlayBathtubSound( Bathtub.SoundType type ){
		if( bathtub != null ){
			SoundManager.main.RequestHandle( self.floorPos ).PlayOneShot( bathtub.GetNextAudioClip( type ) );
		}
	}

	public override void OnCharacterCollisionEnter( CharacterCollisionCallback ccc, Collision collision ){
		
		Item item = collision.collider.gameObject.GetComponent(typeof(Item)) as Item;
		if( item == null ){
			return;
		}
		switch( ccc.collisionPart ){
		case CharacterCollisionCallback.Type.RIGHT_FOOT:
		case CharacterCollisionCallback.Type.LEFT_FOOT:
			ReactToFootTouched( item );
			break;
		}
	}

	public class TransitionToBathtubAnim: Loli.TransitionHandle{
		
		public TransitionToBathtubAnim():base(Loli.TransitionHandle.TransitionType.NO_MIRROR){
		}
		public override void Transition(Loli self){
			self.UpdateAnimationTransition( self.active.bathing.bathingWaterTempAnim );
		}
	}

	private void ReactToFootTouched( Item item ){

		self.SetLookAtTarget( item.transform );
		self.SetViewAwarenessTimeout( 4.0f );
		switch( self.bodyState ){
		case BodyState.BATHING_RELAX:
			if( self.IsHappy() ){
				self.SetTargetAnimation( Loli.Animation.BATHTUB_RELAX_TO_HAPPY_IDLE );
			}else{
				self.SetTargetAnimation( Loli.Animation.BATHTUB_RELAX_TO_ANGRY_IDLE );
			}
			break;
		}
	}

	//optionalSource is who commanded the attempt
	public bool AttemptStartBathingBehavior( Bathtub _bathtub, Character commandSource ){
		if( bathtub != null ){	//using a bathtub already
			return false;
		}
		if( !self.IsCurrentAnimationIdle() ){
			return false;
		}
		//Cannot bathe her if she is tired
		if( self.IsTired() ){
			self.active.idle.PlayAvailableRefuseAnimation();
			return false;
		}

		self.active.SetTask( this, null );
		bathtub = _bathtub;
		bathingPhase = BathingPhase.TEST_WATER_TEMPERATURE;
		self.locomotion.StopMoveTo();

		if( commandSource != null ){
			self.SetLookAtTarget( commandSource.head );
		}

		self.rightLoliHandState.AttemptDrop();
		self.leftLoliHandState.AttemptDrop();
		self.rightShoulderState.AttemptDrop();
		self.leftShoulderState.AttemptDrop();

		//BeginActualBathing();	//DEBUG OVERRIDE
		
		return true;
	}

	public override void OnUpdate(){
		switch( bathingPhase ){
		case BathingPhase.TEST_WATER_TEMPERATURE:
			CheckToTestWaterTemperature();
			break;
		case BathingPhase.TESTING_WATER:
			break;
		case BathingPhase.COMMAND_PLAYER_TO_GET_OUT:
			UpdateWaitForPlayerToGetOut();
			break;
		case BathingPhase.BATHING:
			UpdateBathingIdle();
			break;
		case BathingPhase.ON_KNEES:
			UpdateOnKnees();
			break;
		}
	}

	public override void OnLateUpdatePostIK(){
		switch( bathingPhase ){
		case BathingPhase.ON_KNEES:
			LateUpdatePostIKOnKnees();
			break;
		}
	}

	private void UpdateOnKnees(){
		
		if( self.IsAnimationChangingBodyState() ){
			return;
		}
		Player player = GameDirector.instance.FindNearbyPlayer( self.head.position, 2.0f );
		if( !self.rightHandState.GetItemIfHeld<Towel>() ){
			//if no player nearby, go back to normal bathing pose
			if( player == null ){
				self.SetTargetAnimation( Loli.Animation.BATHTUB_ON_KNEES_TO_HAPPY_IDLE );
				return;
			}
			//begin towel beg if available
			Towel towel = IsPlayerOfferingATowel( player );
			if( towel != null ){
				self.SetTargetAnimation( Loli.Animation.BATHTUB_HAPPY_BEG_LOOP );
				UpdateBeggingForItem( towel );
			}else{
				
				//return to normal bathing pose if player moves too far away
				float bearing = Tools.Bearing( self.transform, player.head.position );
				if( Mathf.Abs( bearing ) < 90.0f ){
					self.SetTargetAnimation( Loli.Animation.BATHTUB_ON_KNEES_TO_HAPPY_IDLE );
				}
			}
		}
	}

	private void LateUpdatePostIKOnKnees(){
		
		if( self.currentAnim == Loli.Animation.BATHTUB_HAPPY_BEG_LOOP ){
			Item item = GameDirector.instance.FindPickupItemForCharacter(
				self,
				self.rightHandState.fingerAnimator.targetBone.position,
				0.07f,
				Item.Type.TOWEL
			);
			if( item == null ){
				return;
			}
			if( self.rightHandState.heldItem != null ){
				return;
			}
			if( item.settings.itemType == Item.Type.TOWEL ){
				self.rightHandState.GrabItemRigidBody( item );
			}
		}
	}

	private void UpdateBeggingForItem( Item begTarget ){

		float bearing = Tools.Bearing( self.transform, begTarget.transform.position );

		if( Mathf.Abs(bearing) > 110.0f ){
			return;
		}

		float currBegDirection = self.animator.GetFloat( Instance.begDirection );
		currBegDirection += ( bearing/120.0f-currBegDirection )*Time.deltaTime*6.0f;
		self.animator.SetFloat( Instance.begDirection, currBegDirection );

		Vector3 approxPos = begTarget.transform.position-self.rightLoliHandState.fingerAnimator.hand.right*0.1f;

		// self.rightLoliHandState.holdArmIK.OverrideWorldRetargetingTransform(
		// 	self.rightLoliHandState.freeHandRetargeting,
		// 	approxPos,
		// 	self.floorPos+self.transform.right+self.transform.forward,
		// 	self.rightLoliHandState.fingerAnimator.hand.rotation
		// );
	}

	private void FailBathtubWaterTest(){
		TutorialManager.main.DisplayHint(
			null,
			bathtub.GetHintSpawnPos(),
			"Water needs to be the right temperature. Turn the water valves and point to the tub to try again"
		);
		self.active.SetTask( self.active.idle, false );

	}

	private void SucceedBathtubWaterTest(){
		self.ShiftHappiness(-2);
		bathingPhase = BathingPhase.COMMAND_PLAYER_TO_GET_OUT;
		self.StopActiveAnchor();
	}

	private void OnAnchorFinishToStartWaterTest(){
		bool useRightAnim = Random.value>0.5f;
		useRightAnim=false;
		switch( bathtub.GetTemperature() ){
		case Bathtub.Temperature.COLD:
			bathingWaterTempAnim = Loli.Animation.BATHTUB_TEST_WATER_IN_COLD_RIGHT;
			break;
		case Bathtub.Temperature.LUKEWARM:
			bathingWaterTempAnim = Loli.Animation.BATHTUB_TEST_WATER_LUKEWARM_RIGHT;
			break;
		case Bathtub.Temperature.HOT:
			bathingWaterTempAnim = Loli.Animation.BATHTUB_TEST_WATER_IN_HOT_RIGHT;
			break;
		}

		if( useRightAnim ){
			self.SetTargetAnimation( Loli.Animation.BATHTUB_TEST_WATER_IN_RIGHT );
		}else{
			self.SetTargetAnimation( Loli.Animation.BATHTUB_TEST_WATER_IN_LEFT );
			//increment to left version
			bathingWaterTempAnim = (Loli.Animation)((int)bathingWaterTempAnim+1);
		}
	}

	private void CheckToTestWaterTemperature(){
		// if( targetBathPos == null || Vector3.SqrMagnitude( targetBathPos.Value-self.floorPos ) > 0.1f ){

		// 	if( !self.locomotion.isMoveToActive() ){
		// 		shinobuIsOnRightSide = bathtub.IsClosestEdgeRightSide( self.floorPos );
		// 		targetBathPos = bathtub.GetSideAnchorAnimationTransform( shinobuIsOnRightSide ).position;

		// 		Vector3[] path = self.locomotion.GetNavMeshPath( targetBathPos.Value );
		// 		if( path == null ){
		// 			self.active.SetTask( self.active.idle, false );
		// 			return;
		// 		}
		// 		self.locomotion.FollowPath( path );
		// 	}
		// //hasn't started anchoring transform animation
		// }else if( !self.isAnchorActive ){
		// 	Transform targetAnchorTransform = bathtub.GetSideAnchorAnimationTransform( shinobuIsOnRightSide );
		// 	// self.BeginAnchorTransformAnimation(
		// 	// 	targetBathPos.Value,
		// 	// 	targetAnchorTransform.rotation,
		// 	// 	1.0f,
		// 	// 	OnAnchorFinishToStartWaterTest,
		// 	// 	false
		// 	// );
		// }
	}

	private float GetBathroomDoorBearing(){
		Door door = bathtub.GetBathroomDoor(0);
		return Tools.Bearing( self.transform, door.transform.position );
	}

	private void CheckIfCanPointToDoor(){
		float doorBearing = GetBathroomDoorBearing();
		if( Mathf.Abs( doorBearing ) < maxPointingBearing ){
			if( doorBearing > 0.0f ){
				self.SetTargetAnimation( Loli.Animation.STAND_POINT_OUT_IN_RIGHT );
			}else{
				self.SetTargetAnimation( Loli.Animation.STAND_POINT_OUT_IN_LEFT );
			}
			self.SetLookAtTarget( GameDirector.player.head, 2.1f );
		}else{
			self.SetTargetAnimation( Loli.Animation.STAND_WAIT_ANNOYED_LOOP );
		}
	}

	private void CheckIfShouldSayGetOut( Loli.Animation sayAnim ){

		bathroomSayGetOutTimer -= Time.deltaTime;
		if( bathroomSayGetOutTimer < 0.0f ){
			bathroomSayGetOutTimer = 10.0f+Random.value*5.0f;
			self.SetTargetAnimation( sayAnim );
			self.SetLookAtTarget( GameDirector.player.head, 2.1f );
			self.SetViewAwarenessTimeout( 4.0f );
		}
	}

	private void SetPointOutToWaitAnimation(){
		switch( self.currentAnim ){
		case Loli.Animation.STAND_POINT_OUT_LOOP_LEFT:
			self.SetTargetAnimation( Loli.Animation.STAND_POINT_OUT_TO_WAIT_LEFT );
			break;
		case Loli.Animation.STAND_POINT_OUT_LOOP_RIGHT:
			self.SetTargetAnimation( Loli.Animation.STAND_POINT_OUT_TO_WAIT_RIGHT );
			break;
		}
	}

	private Loli.Animation GetPointAndSayOutAnimation( bool rightSide ){
		if( self.headModel.voiceIndex == (byte)Voice.VoiceType.SHINOBU ){
			if( rightSide ){
				return Loli.Animation.STAND_POINT_OUT_SOUND_2_3_RIGHT;
			}else{
				return Loli.Animation.STAND_POINT_OUT_SOUND_2_3_LEFT;
			}
		}else{
			if( rightSide ){
				return Loli.Animation.STAND_POINT_OUT_ANTSY_RIGHT;
			}else{
				return Loli.Animation.STAND_POINT_OUT_ANTSY_LEFT;
			}
		}
	}

	private void UpdateWaitForPlayerToGetOutAnimation(
				bool playerIsStillInBathroom,
				LoliHandState currentPointingHand,
				Loli.Animation sayGetOutAnimation ){
		
		UpdatePointingArmIK( currentPointingHand );
		if( playerIsStillInBathroom ){

			//if self has too much bearing away from door
			float doorBearing = GetBathroomDoorBearing();
			if( Mathf.Abs( doorBearing ) > maxPointingBearing ){
				SetPointOutToWaitAnimation();
			//if should switch hands
			}else{
				if( doorBearing > 0.0f ){
					if( self.currentAnim == Loli.Animation.STAND_POINT_OUT_LOOP_LEFT ){
						self.SetTargetAnimation( Loli.Animation.STAND_POINT_OUT_IN_RIGHT );
					}
				}else{
					if( self.currentAnim == Loli.Animation.STAND_POINT_OUT_LOOP_RIGHT ){
						self.SetTargetAnimation( Loli.Animation.STAND_POINT_OUT_IN_LEFT );
					}
				}
			}
			CheckIfShouldSayGetOut( sayGetOutAnimation );
		}else{
			SetPointOutToWaitAnimation();
		}
	}

	private void UpdateWaitForPlayerToGetOut(){

		//if self is outside of the room, end bathing
		// if( !bathtub.IsCharacterInBathroom(self) ){
		// 	self.active.SetTask( self.active.idle, false );
		// 	return;
		// }

		// bool playerIsInRoom = bathtub.IsCharacterInBathroom( GameDirector.player );

		// refreshLookAtTargetTimer -= Time.deltaTime;
		// if( refreshLookAtTargetTimer <= 0.0f ){
		// 	refreshLookAtTargetTimer = 4.0f;
		// 	self.SetRootFacingTarget( GameDirector.player.head.position, 200.0f, 25.0f, 30.0f );
		// }
		// switch( self.currentAnim ){
		// case Loli.Animation.STAND_POINT_OUT_IN_RIGHT:
		// case Loli.Animation.STAND_POINT_OUT_LOOP_RIGHT:
		// case Loli.Animation.STAND_POINT_OUT_ANTSY_RIGHT:
		// case Loli.Animation.STAND_POINT_OUT_SOUND_2_3_RIGHT:
		// 	UpdateWaitForPlayerToGetOutAnimation(
		// 		playerIsInRoom,
		// 		self.rightLoliHandState,
		// 		GetPointAndSayOutAnimation(true)
		// 	);
		// 	break;
		// case Loli.Animation.STAND_POINT_OUT_IN_LEFT:
		// case Loli.Animation.STAND_POINT_OUT_LOOP_LEFT:
		// case Loli.Animation.STAND_POINT_OUT_ANTSY_LEFT:
		// case Loli.Animation.STAND_POINT_OUT_SOUND_2_3_LEFT:
		// 	UpdateWaitForPlayerToGetOutAnimation(
		// 		playerIsInRoom,
		// 		self.leftLoliHandState,
		// 		GetPointAndSayOutAnimation(false)
		// 	);
		// 	break;
		// case Loli.Animation.STAND_WAIT_ANNOYED_LOOP:
		// 	if( playerIsInRoom ){
		// 		CheckIfCanPointToDoor();
		// 	}
		// 	break;
		
		// case Loli.Animation.BATHTUB_TOWEL_IDLE_LOOP:
		// 	//if towel was taken away
		// 	if( !self.rightHandState.GetItemIfHeld<Towel>() ){
		// 		self.SetTargetAnimation( Loli.Animation.BATHTUB_TOWEL_EMBARRASSED_TO_BATHTUB_IDLE );
		// 		self.SetLookAtTarget( GameDirector.player.head, 1.5f );
		// 		self.ShiftHappiness(-2);
		// 	}else if( playerIsInRoom ){
		// 		if( self.headModel.voiceIndex == (byte)Voice.VoiceType.SHINOBU ){
		// 			CheckIfShouldSayGetOut( Loli.Animation.BATHTUB_TOWEL_OUT_SOUND_2_3 );
		// 		}
		// 	}
		// 	break;
			
		// default:
		// 	if( self.IsCurrentAnimationIdle() ){
		// 		self.SetViewAwarenessTimeout(3.0f);
		// 		float playerBearing = Tools.Bearing( self.transform, GameDirector.player.head.position );
		// 		refreshLookAtTargetTimer = 0.0f;
		// 		if( Mathf.Abs( playerBearing ) < 20.0f ){
		// 			CheckIfCanPointToDoor();
		// 		}
		// 	}
		// 	break;
		// }
		// if( !playerIsInRoom && bathtub.AreAllBathroomDoorsClosed() ){
		// 	//Holding towel implies bathing is over, succeed bathing
		// 	if( self.rightHandState.GetItemIfHeld<Towel>() ){
		// 		self.active.SetTask( self.active.idle, true );
		// 	}else{
		// 		BeginActualBathing();
		// 	}
		// }
	}
	
	private void BeginActualBathing(){
		bathingPhase = BathingPhase.BATHING;
		self.ShiftHappiness(4);

		float segRatio;
		self.Teleport(
			bathtub.ProjectToAnchorSegment( self.floorPos, out segRatio ),
			bathtub.GetSideAnchorAnimationTransform( shinobuIsOnRightSide ).rotation
		);

		self.OverrideClearAnimationPriority();
		self.SetTargetAnimation( Loli.Animation.BATHTUB_RELAX_LOOP );
		Outfit outfit = Outfit.Create(
			new string[]{
				"bubble torso",
				"bubble pelvis",
			},
			true
		);
		self.SetOutfit( outfit );
		
		GameDirector.instance.SetMusic( GameDirector.Music.BATHING, 3.0f );
		GameDirector.instance.LockMusic( true );

		self.ApplyDisableBalanceLogic( ref disabledFalling );
	}

	private Towel IsPlayerOfferingATowel( Player player ){
		Towel towel = player.rightHandState.GetItemIfHeld<Towel>();
		if( towel == null ){
			towel = player.leftHandState.GetItemIfHeld<Towel>();
		}
		//beg for towel if any
		if( towel != null && self.CanSeePoint( towel.transform.position ) ){
			return towel;
		}
		return null;
	}

	private void UpdateBathingIdle(){
		//if idling
		if( !self.IsCurrentAnimationIdle() ){
			return;
		}
		if( !self.IsHappy() ){
			TutorialManager.main.DisplayHint(
				null,
				self.head.position+Vector3.up*0.5f,
				"If she is angry she won't let you help her. Headpat her softly to make her happy again",
				null,
				0.7f
			);
			return;
		}
		bathtubIdleAnimTimer -= Time.deltaTime;
		if( bathtubIdleAnimTimer < 0.0f ){
			PlayRandomBathingIdleAnimation();
		}else{
			Player player = GameDirector.instance.FindNearbyPlayer( self.head.position, 1.4f );
			if( !CheckIfPlayerIsOfferingTowel( player ) ){
				CheckSplashBackPlayer( player );
			}
		}
	}

	private void CheckSplashBackPlayer( Player player ){
		if( splashBackCount <= 0 ){
			return;
		}
		if( player == null ){
			return;
		}
		Loli.Animation splashBackAnim = GetSplashBackAnimation();
		if( splashBackAnim == Loli.Animation.NONE ){
			return;
		}
		if( splashBackTimer > 0.0f ){
			splashBackTimer -= Time.deltaTime;
			return;
		}
		splashBackTimer = 1.0f+Random.value;

		float bearing = Tools.Bearing( self.transform, player.head.position );
		if( Mathf.Abs( bearing ) < 120.0f ){
			self.SetLookAtTarget( player.head );
			self.SetViewAwarenessTimeout( 0.6f );
			if( self.IsHappy() ){
				self.SetTargetAnimation( splashBackAnim );
			}
			self.animator.SetFloat( Instance.splashDirID, Mathf.Clamp( bearing/60.0f, -1.0f, 1.0f ) );
		}
	}

	public void Splash(){

		Vector3 splashPos = ( self.rightHandState.transform.position+self.leftHandState.transform.position )/2.0f;
		Quaternion splashRot = Quaternion.LookRotation(
			(self.rightHandState.transform.up+self.leftHandState.transform.up)/2.0f,
			Vector3.up
		);
		GameDirector.instance.SplashWaterFXAt(
			splashPos,
			splashRot,
			1.0f,
			2.5f,
			10
		);
	}

	private Loli.Animation GetSplashBackAnimation(){
		if( self.IsHappy() ){
			switch( self.bodyState ){
			case BodyState.BATHING_IDLE:
				if( self.IsHappy() ){
					return Loli.Animation.BATHTUB_HAPPY_SPLASH;
				}
				break;
			}
		}
		return Loli.Animation.NONE;
	}

	private bool CheckIfPlayerIsOfferingTowel( Player player ){
		if( player == null ){
			return false;
		}
		Towel towel = IsPlayerOfferingATowel( player );
		if( towel == null ){
			return false;
		}
		self.SetLookAtTarget( towel.transform );
		self.SetViewAwarenessTimeout( 10.0f );
		bool playerIsOnRightSide = bathtub.IsClosestEdgeRightSide( player.transform.position );
		if( playerIsOnRightSide == shinobuIsOnRightSide ){
			self.SetTargetAnimation( Loli.Animation.BATHTUB_HAPPY_SWITCH_SIDES );
		}else{
			self.SetTargetAnimation( Loli.Animation.BATHTUB_HAPPY_IDLE_TO_ON_KNEES );
		}
		return true;
	}

	public override bool OnGesture( Item source, ObjectFingerPointer.Gesture gesture ){
		if( bathtub == null ){	//should be in NONE bathingPhase
			return false;
		}
		if( gesture == ObjectFingerPointer.Gesture.FOLLOW ){
			
			bool playerIsOnRightSide = bathtub.IsClosestEdgeRightSide( GameDirector.player.transform.position );
			if( playerIsOnRightSide != shinobuIsOnRightSide ){
				self.SetTargetAnimation( Loli.Animation.BATHTUB_HAPPY_SWITCH_SIDES );
			}else{
				self.SetTargetAnimation( Loli.Animation.BATHTUB_HAPPY_IDLE_TO_ON_KNEES );
			}
			return true;
		}
		return false;
	}

	private void PlayRandomBathingIdleAnimation(){
		switch( self.bodyState ){
			case BodyState.BATHING_IDLE:
				bathtubIdleAnimTimer = 5.0f;
				if( splashBackCount > 0 ){
					return;
				}
				if( self.IsHappy() ){
					if( Random.value > 0.66f ){
						self.SetTargetAnimation( Loli.Animation.BATHTUB_HAPPY_IDLE2 );
					}else if( Random.value > 0.5f ){
						self.SetTargetAnimation( Loli.Animation.BATHTUB_HAPPY_IDLE3 );
					}else{
						self.SetTargetAnimation( Loli.Animation.BATHTUB_HAPPY_IDLE4 );
					}
				}
				break;
			case BodyState.BATHING_RELAX:
				self.SetTargetAnimation( Loli.Animation.BATHTUB_RELAX_HUMMING );
				bathtubIdleAnimTimer = 10.0f;
				break;
			default:
				bathtubIdleAnimTimer = 5.0f;
				break;
			}
	}

	private void UpdatePointingArmIK( LoliHandState handState ){
		Door door = bathtub.GetBathroomDoor(0);
		Quaternion pointRotation = Quaternion.LookRotation( door.transform.position-handState.holdArmIK.hand.position, self.transform.up );
		// handState.holdArmIK.OverrideWorldRetargetingTransform(
		// 	handState.freeHandRetargeting,
		// 	door.transform.position+Vector3.up*1.25f,
		// 	self.floorPos+self.transform.right*0.5f,
		// 	pointRotation*handState.handRestPoseRotation
		// );
	}

	private IEnumerator GrowBubbles( Material instance ){
		
		float endSize = instance.GetFloat( bubbleSizeID );
		float timer = 0.0f;
		while( timer < 1.5f ){
			timer += Time.deltaTime;
			float ratio = timer/1.5f;
			instance.SetFloat( bubbleSizeID, ratio*endSize );
			yield return null;
		}
	}

	private GameObject GenerateBubbles( GameObject prefab, Transform target ){
		
		GameObject bubbles = GameObject.Instantiate(
			prefab, target
		);
		bubbles.name = "BUBBLES";
		activeDynBoneBubbles.Add( bubbles );
		
		MeshRenderer bubblesMR = bubbles.GetComponent(typeof(MeshRenderer)) as MeshRenderer;
		if( bubblesMR == null ){
			Debug.LogError("ERROR Bubbles prefab "+prefab.name+" must have a mesh renderer!");
		}else{
			GameDirector.instance.StartCoroutine( GrowBubbles( bubblesMR.material ) );
		}
		if( target == self.head ){
			//position bubbles above headpat sphere
			SphereCollider headSC = self.GetColliderBodyPart( Loli.BodyPart.HEAD_SC ) as SphereCollider;
			bubbles.transform.localPosition = headSC.center+Vector3.up*headSC.radius;
		}else{
			bubbles.transform.localEulerAngles = new Vector3( 90.0f, 0.0f, 0.0f );
		}
		return bubbles;
	}

	public void OnCompleteHairBoneWash( DynamicBoneGrab grabber ){
		
		if( grabber.targetBoneItem == null ){
			Debug.LogError("ERROR DynamicBoneGrabber isnt grabbing anything!");
			return;
		}
		bool firstBoneAlongStrand = true;
		//generate bubbles along every bone
		Transform boneTransform = grabber.targetBoneItem.transform;
		while( true ){
			//don't generate bubbles on top of bubbles
			if( TransformContainsBubbles( boneTransform ) ){
				break;
			}
			if( firstBoneAlongStrand ){
				IncreaseHairWashedPercent( 0.3f );
				firstBoneAlongStrand = false;
			}
			if( boneTransform.childCount == 0 ){
				//generate a bubbly tip since it's the end of a transform chain
				GenerateBubbles( self.active.settings.dynBoneShaftBubblesTip, boneTransform );
				break;
			}
			//generate long or short bubbles depending on the size of the dynamic bone
			Transform oldBoneTransform = boneTransform;
			boneTransform = boneTransform.GetChild(0);
			float boneSqLength = Vector3.SqrMagnitude( oldBoneTransform.position-boneTransform.position );

			GameObject bubbles;
			if( boneSqLength < 0.008f ){
				bubbles = GenerateBubbles( self.active.settings.dynBoneShaftBubblesShort, oldBoneTransform );
			}else{
				bubbles = GenerateBubbles( self.active.settings.dynBoneShaftBubblesLong, oldBoneTransform );
			}
			bubbles.transform.position = ( oldBoneTransform.position+boneTransform.position )/2.0f;
		}
	}

	private bool TransformContainsBubbles( Transform transform ){
		for( int i=0; i<transform.childCount; i++ ){
			if( transform.GetChild(i).name == "BUBBLES" ){
				return true;
			}
		}
		return false;
	}

	public void OnSoapyHeadpat(){
		
		//build up headScrub timer before spawning bubbles
		if( Time.time-lastHeadScrubTime > 3.0f ){
			headScrubTime = 0.0f;
			lastHeadScrubTime = Time.time;
			return;
		}
		headScrubTime += Time.deltaTime;
		lastHeadScrubTime = Time.time;

		if( headScrubTime > 1.0f ){
			if( TransformContainsBubbles( self.head ) ){
				return;
			}
			IncreaseHairWashedPercent( 0.1f );
			GameObject bubbles = GenerateBubbles( self.active.settings.headScrubGeneratedBubbles, self.head );
		}
	}

	private void IncreaseHairWashedPercent( float percentAmount ){
		hairWashedPercent += percentAmount;
		StartBubbleMeter();
		SetBubbleMeterPercent( hairWashedPercent );
	}

	private void StartBubbleMeter(){
        if( bubbleMeter != null ){
            return;
        }
        if( self.active.settings.bubbleMeterPrefab == null ){
            Debug.LogError("ERROR bubbleMeterPrefab not set!");
            return;
        }
        bubbleMeterCoroutine = GameDirector.instance.StartCoroutine( BubbleMeterAnimator() );
    }

    private void StopBubbleMeter(){
        if( bubbleMeter == null ){
            return;
        }

        GameObject.Destroy( bubbleMeter );
        bubbleMeter = null;
        GameDirector.instance.StopCoroutine( bubbleMeterCoroutine );
        bubbleMeterCoroutine = null;        
    }

    private IEnumerator BubbleMeterAnimator(){
        
        bubbleMeter = GameObject.Instantiate( self.active.settings.bubbleMeterPrefab, Vector3.zero, Quaternion.identity );
        SetBubbleMeterPercent( 0.0f );	//initialize percent

        Vector3 startScale = bubbleMeter.transform.localScale;
        float easeIn = 0.0f;
        while( true ){
            float wobbleUp = Mathf.Sin( Time.time*2.317f );
            bubbleMeter.transform.position = self.head.position+Vector3.up*( 0.425f+wobbleUp*0.015f );
            
            Quaternion lookAtCamera = Quaternion.LookRotation( bubbleMeter.transform.position-GameDirector.instance.mainCamera.transform.position, Vector3.up );
            bubbleMeter.transform.rotation = lookAtCamera;

            easeIn = Mathf.Min( easeIn+Time.deltaTime*2.0f, 1.0f );
            float sin = Mathf.Sin( Time.time*4.0f );
            Vector3 animatedScale = startScale*easeIn*( 1.0f+sin*0.05f );
            bubbleMeter.transform.localScale = animatedScale;
            
            yield return null;
        }
    }

	public BathingPhase GetBathingPhase(){
		return bathingPhase;
	}

    private void SetBubbleMeterPercent( float percent ){

        if( bubbleMeter == null ){
            return;
        }
        MeshRenderer bubbleMeterMR = bubbleMeter.GetComponent(typeof(MeshRenderer)) as MeshRenderer;
        if( bubbleMeterMR == null ){
            Debug.LogError("ERROR BubbleMeter does not have a MeshRenderer on root obj!");
            return;
        }
        if( percent >= 1.0f ){
			self.IncreaseDirt( -1.0f );
			GameDirector.player.CompleteAchievement( Player.ObjectiveType.WASH_HAIR );
            bubbleMeterMR.material = self.active.settings.bubbleMeterLevel3;
			TutorialManager.main.DisplayHint(
				null,
				self.head.position+Vector3.up*0.6f,
				"Now grab the green towel on the wall and hand it to her",
				null,
				0.6f
			);
        }else if( percent >= 0.1f ){
            bubbleMeterMR.material = self.active.settings.bubbleMeterLevel2;
			string hintMessage;
			if( GameDirector.player.controls == Player.ControlType.KEYBOARD ){
                hintMessage = "Press F to make her kneel to easily grab her long hair strands";
            }else{
                hintMessage = "Gesture 'come here' to make her kneel to easily an grab her long hair strands";
            }
		    TutorialManager.main.DisplayHint(
				null,
				self.head.position+Vector3.up*0.6f,
				hintMessage,
				null,
				0.6f
			);
			self.IncreaseDirt( -0.25f );
        }else{
            bubbleMeterMR.material = self.active.settings.bubbleMeterLevel1;
        }
    }

	public override void OnAnimationChange( Loli.Animation oldAnim, Loli.Animation newAnim ){

		switch( newAnim ){
		case Loli.Animation.BATHTUB_HAPPY_SPLASH:
			splashBackCount = Mathf.Max( 0, splashBackCount-1 );
			break;
		case Loli.Animation.BATHTUB_SPLASH_REACT_LEFT:
		case Loli.Animation.BATHTUB_SPLASH_REACT_RIGHT:
			splashBackCount = Mathf.Min( splashBackCount+2, 3 );
			break;
		case Loli.Animation.BATHTUB_HAPPY_SWITCH_SIDES:
			shinobuIsOnRightSide = !shinobuIsOnRightSide;
			// self.BeginAnchorTransformAnimation(
			// 	self.floorPos,
			// 	bathtub.GetSideAnchorAnimationTransform( shinobuIsOnRightSide ).rotation,
			// 	1.0f,
			// 	null,
			// 	false
			// );			
			break;		
		case Loli.Animation.BATHTUB_TEST_WATER_IN_RIGHT:
		case Loli.Animation.BATHTUB_TEST_WATER_IN_LEFT:
			bathingPhase = BathingPhase.TESTING_WATER;
			break;
		case Loli.Animation.BATHTUB_HAPPY_IDLE_TO_ON_KNEES:
			bathingPhase = BathingPhase.ON_KNEES;
			break;
		case Loli.Animation.BATHTUB_ON_KNEES_TO_HAPPY_IDLE:
		case Loli.Animation.BATHTUB_TOWEL_EMBARRASSED_TO_BATHTUB_IDLE:
		case Loli.Animation.BATHTUB_ON_KNEES_HEADPAT_BRUSH_AWAY_TO_ANGRY_IDLE:
			bathingPhase = BathingPhase.BATHING;
			break;
		case Loli.Animation.BATHTUB_ON_KNEES_TO_TOWEL_IDLE:
			bathingPhase = BathingPhase.COMMAND_PLAYER_TO_GET_OUT;
			break;
		}
		switch( oldAnim ){
		case Loli.Animation.BATHTUB_TEST_WATER_IN_COLD_RIGHT:
		case Loli.Animation.BATHTUB_TEST_WATER_IN_COLD_LEFT:
		case Loli.Animation.BATHTUB_TEST_WATER_IN_HOT_RIGHT:
		case Loli.Animation.BATHTUB_TEST_WATER_IN_HOT_LEFT:
			FailBathtubWaterTest();
			break;
		case Loli.Animation.BATHTUB_TEST_WATER_LUKEWARM_RIGHT:
		case Loli.Animation.BATHTUB_TEST_WATER_LUKEWARM_LEFT:
			SucceedBathtubWaterTest();
			break;
		case Loli.Animation.STAND_POINT_OUT_SOUND_2_3_RIGHT:
		case Loli.Animation.STAND_POINT_OUT_SOUND_2_3_LEFT:
			TutorialManager.main.DisplayHint(
				null,
				self.head.position+Vector3.up*0.5f,
				"Leave and close the door behind you to allow the loli to undress",
				null,
				0.6f
			);
			break;
		case Loli.Animation.BATHTUB_RELAX_TO_HAPPY_IDLE:
			TutorialManager.main.DisplayHint(
				null,
				self.head.position+Vector3.up*0.5f,
				"Wash different hairs and headpat her to complete bathing. A bubble meter will grow as you progress",
				null,
				0.6f
			);
			break;
		}
	}
}

}
