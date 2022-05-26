using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


namespace viva{


public partial class SleepingBehavior : ActiveBehaviors.ActiveTask {
	
	// public enum SleepingPhase{
	// 	NONE,
	// 	WALKING_TO_BED,
	// 	TRANSITIONING_ONTO_BED,
	// 	CRAWLING_ON_BED,
	// 	ABOUT_TO_SLEEP,
	// 	SLEEPING,
	// 	AWAKE_ON_BED,
	// 	CRAWLING_OFF_BED,
	// 	TRANSITIONING_OFF_BED,
	// }

	private Bed bed = null;

	private const float minimumDayPercentSleep = 0.6f;
	private const int botherResistanceCount = 10;
	private const float awakeTimeTillSleep = 10;
	private bool? layingOnRightSide = null;
	private bool saidGoodnight;
	private Vector3 sleepPos;


	public SleepingBehavior( Loli _self ):base(_self,ActiveBehaviors.Behavior.SLEEPING,null){
    }

	public bool AttemptBeginSleeping( Bed _bed ){
		
		if( _bed == null || bed == _bed ){
			return false;
		}
		if( self.passive.scared.scared ){
			var playAnim = new AutonomyPlayAnimation( self.autonomy, "refuse sleeping", self.GetAnimationFromSet( AnimationSet.REFUSE ) );
			self.autonomy.Interrupt( playAnim );
			return false;
		}

		if( !_bed.CanHost( self ) ){
			var playAnim = LoliUtility.CreateSpeechAnimation( self, AnimationSet.REFUSE, SpeechBubble.FULL );
			self.autonomy.Interrupt( playAnim );
			return false;
		}
		
		bed = _bed;
		bed.filterUse.SetOwner( self );
		self.active.SetTask( this, null );
		GameDirector.player.objectFingerPointer.selectedLolis.Remove( self );
		GoToBed();
		return true;
	}

	public override void OnActivate(){
		saidGoodnight = false;
		GameDirector.player.objectFingerPointer.selectedLolis.Remove( self );
		self.characterSelectionTarget.OnUnselected();
	}

	public override void OnDeactivate(){
		bed.filterUse.RemoveOwner( self );
		bed = null;
		layingOnRightSide = null;
	}

	public override bool RequestPermission( ActiveBehaviors.Permission permission){
		switch( permission ){
		case ActiveBehaviors.Permission.ALLOW_ROOT_FACING_TARGET_CHANGE:
		case ActiveBehaviors.Permission.ALLOW_IMPULSE_ANIMATION:
			return false;
		}
		return true;
	}

	public override bool OnGesture( Item source, ObjectFingerPointer.Gesture gesture ){
		// if( gesture == ObjectFingerPointer.Gesture.FOLLOW ){
		// 	if( phase == SleepingPhase.AWAKE_ON_BED ){
		// 		if( self.CanSeePoint( source.transform.position ) ){
		// 			getUpTimer = 0.0f;
		// 			return true;
		// 		}
		// 	}else if( phase == SleepingPhase.WALKING_TO_BED ){
		// 		self.active.follow.AttemptFollow( source );
		// 	}
		// }
		return false;
	}

	private void ConfuseAndEnd(){
		var playAnim = LoliUtility.CreateSpeechAnimation( self, AnimationSet.CONFUSED, SpeechBubble.INTERROGATION );
		self.autonomy.Interrupt( playAnim );
		self.active.SetTask( null );
	}

	private void GoToBed(){
		if( bed == null ){
			ConfuseAndEnd();
			return;
		}

		var moveToBed = GenerateMoveOnBed();
		moveToBed.onSuccess += LayDownOnBed;

		self.autonomy.SetAutonomy( moveToBed );
	}

	private AutonomyMoveTo GenerateMoveOnBed(){
		bed.GetRandomSleepingTransform( out sleepPos, out Vector3 sleepForward );
		return new AutonomyMoveTo( self.autonomy, "move to bed", delegate( TaskTarget target ){
			target.SetTargetPosition( sleepPos );
		}, 0.0f, BodyState.CRAWL_TIRED,
		delegate( TaskTarget target ){
			target.SetTargetPosition( sleepPos+sleepForward );
		} );
	}

	private AutonomySphereBoundary GenerateEnsureNearBed(){
		var ensureNearBed = new AutonomySphereBoundary( self.autonomy, 
		delegate( TaskTarget source ){
			source.SetTargetPosition( sleepPos );
		},
		delegate( TaskTarget target ){
			target.SetTargetPosition( self.floorPos );
		}, 0.5f );
		ensureNearBed.onRegistered += GoToBed;
		return ensureNearBed;
	}

	private void LayDownOnBed(){
		Loli.Animation beforeSleepAnim;
		Loli.Animation yawnAnim;
		Loli.Animation goodNightAnim;		
		switch( Random.Range(0,3) ){
		case 0:
			beforeSleepAnim = Loli.Animation.AWAKE_HAPPY_PILLOW_UP_IDLE;
			yawnAnim = Loli.Animation.AWAKE_PILLOW_SIDE_YAWN_LONG_RIGHT;
			goodNightAnim = Loli.Animation.AWAKE_PILLOW_SIDE_SOUND_GOODNIGHT_RIGHT;
			break;
		case 1:
			beforeSleepAnim = Loli.Animation.AWAKE_PILLOW_SIDE_HAPPY_IDLE_LEFT;
			yawnAnim = Loli.Animation.AWAKE_PILLOW_SIDE_YAWN_LONG_LEFT;
			goodNightAnim = Loli.Animation.AWAKE_PILLOW_SIDE_SOUND_GOODNIGHT_LEFT;
			break;
		default:
			beforeSleepAnim = Loli.Animation.AWAKE_PILLOW_SIDE_HAPPY_IDLE_RIGHT;
			yawnAnim = Loli.Animation.AWAKE_PILLOW_SIDE_YAWN_LONG_RIGHT;
			goodNightAnim = Loli.Animation.AWAKE_PILLOW_SIDE_SOUND_GOODNIGHT_RIGHT;
			break;
		}
		
		var awakeToSleeptimer = new AutonomyWait( self.autonomy, "time till sleep", 2.0f );

		var playLayDown = new AutonomyPlayAnimation( self.autonomy, "lay down anim", beforeSleepAnim );	
	
		var yawnanim = new AutonomyPlayAnimation(self.autonomy, "yawn anim", yawnAnim);
		
		var goodnightanim = new AutonomyPlayAnimation(self.autonomy, "say good night", goodNightAnim);
		awakeToSleeptimer.AddRequirement( playLayDown );
		awakeToSleeptimer.AddRequirement( yawnanim );
		//shinobu has no good night voice line
		if( self.headModel.voiceIndex != (byte)Voice.VoiceType.SHINOBU ){
			awakeToSleeptimer.AddRequirement( goodnightanim );
		}	
		awakeToSleeptimer.AddRequirement( GenerateEnsureNearBed() );

		//yawnanim.AddRequirement(playLayDown);
		//yawnanim.AddRequirement(goodnightanim);
		
		self.autonomy.SetAutonomy( awakeToSleeptimer );
		awakeToSleeptimer.onSuccess += FallAsleep;		
	}

	private void FallAsleep(){
		
		Loli.Animation sleepAnim;
		switch( Random.Range(0, 3 ) ){
		case 0:
			sleepAnim = Loli.Animation.SLEEP_PILLOW_UP_IDLE;
			break;
		case 1:
			sleepAnim = Loli.Animation.SLEEP_PILLOW_SIDE_IDLE_LEFT;
			break;
		default:
			sleepAnim = Loli.Animation.SLEEP_PILLOW_SIDE_IDLE_RIGHT;
			break;
		}

		//wake up in morning
		float PI_2 = Mathf.PI*2.0f;
		float morning = PI_2+0.1f;
		float dayCycleDuration = Mathf.Min( morning-GameDirector.settings.worldTime%PI_2, 0.5f );
		var sleepNightTimer = new AutonomyWaitDayCycle( self.autonomy, "sleep night", dayCycleDuration );
		var playSleepAnim = new AutonomyPlayAnimation( self.autonomy, "play sleep anim", sleepAnim );
		sleepNightTimer.AddRequirement( playSleepAnim );
		playSleepAnim.AddRequirement( GenerateEnsureNearBed() );

		self.autonomy.SetAutonomy( sleepNightTimer );

		sleepNightTimer.onSuccess += WakeUp;
	}

	private void WakeUp(){
		var playWakeAnim = new AutonomyPlayAnimation( self.autonomy, "play wake up anim", Loli.Animation.AWAKE_HAPPY_PILLOW_UP_IDLE );
		self.autonomy.SetAutonomy( playWakeAnim );

		playWakeAnim.onSuccess += EndSleeping;
	}

	private void EndSleeping(){
		self.active.SetTask( self.active.idle );
		self.active.idle.hasSaidGoodMorning = false;
		self.Tired = false;
	}

	public override void OnAnimationChange(Loli.Animation oldAnim, Loli.Animation newAnim){
		
		//change layingOnRightSide based on animation
		switch( newAnim ){
		case Loli.Animation.AWAKE_PILLOW_SIDE_HAPPY_IDLE_LEFT:
		case Loli.Animation.SLEEP_PILLOW_SIDE_IDLE_LEFT:
			layingOnRightSide = false;
			Debug.Log("on left side");
			break;
		case Loli.Animation.AWAKE_PILLOW_SIDE_SOUND_GOODNIGHT_RIGHT:
		case Loli.Animation.AWAKE_PILLOW_SIDE_SOUND_GOODNIGHT_LEFT:
			saidGoodnight = true;
			break;
		case Loli.Animation.AWAKE_PILLOW_SIDE_HAPPY_IDLE_RIGHT:
		case Loli.Animation.SLEEP_PILLOW_SIDE_IDLE_RIGHT:
			layingOnRightSide = true;
			Debug.Log("on right side");
			break;
		case Loli.Animation.SLEEP_PILLOW_UP_IDLE:
		case Loli.Animation.SLEEP_PILLOW_SIDE_TO_SLEEP_PILLOW_UP_RIGHT:
		case Loli.Animation.SLEEP_PILLOW_SIDE_TO_SLEEP_PILLOW_UP_LEFT:
		case Loli.Animation.SLEEP_PILLOW_SIDE_TO_AWAKE_HAPPY_PILLOW_UP_RIGHT:
		case Loli.Animation.SLEEP_PILLOW_SIDE_TO_AWAKE_HAPPY_PILLOW_UP_LEFT:
		case Loli.Animation.SLEEP_PILLOW_SIDE_TO_AWAKE_ANGRY_PILLOW_UP_RIGHT:
		case Loli.Animation.SLEEP_PILLOW_SIDE_TO_AWAKE_ANGRY_PILLOW_UP_LEFT:
			layingOnRightSide = null;	//no longer laying on a side
			break;
		case Loli.Animation.AWAKE_PILLOW_UP_TO_CRAWL_TIRED:
			self.outfitInstance.SetDynamicBoneModificationsStiffness(1.0f);
			self.outfitInstance.SetDynamicBoneModificationsCollider( bed.GetDynamicBonePlane(), false );
			break;
		}
	}

}
}