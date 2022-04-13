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
		
		GameDirector.player.objectFingerPointer.selectedLolis.Remove( self );
		self.characterSelectionTarget.OnUnselected();
	}

	public override void OnDeactivate(){
		bed.filterUse.RemoveOwner( self );
		bed = null;
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
		switch( Random.Range(0,3) ){
		case 0:
			beforeSleepAnim = Loli.Animation.AWAKE_HAPPY_PILLOW_UP_IDLE;
			break;
		case 1:
			beforeSleepAnim = Loli.Animation.AWAKE_PILLOW_SIDE_HAPPY_IDLE_LEFT;
			break;
		default:
			beforeSleepAnim = Loli.Animation.AWAKE_PILLOW_SIDE_HAPPY_IDLE_RIGHT;
			break;
		}
		
		var awakeToSleeptimer = new AutonomyWait( self.autonomy, "time till sleep", 1.0f );
		awakeToSleeptimer.onSuccess += FallAsleep;

		var playLayDown = new AutonomyPlayAnimation( self.autonomy, "lay down anim", beforeSleepAnim );

		awakeToSleeptimer.AddRequirement( playLayDown );
		awakeToSleeptimer.AddRequirement( GenerateEnsureNearBed() );
		
		self.autonomy.SetAutonomy( awakeToSleeptimer );
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
		var playSleepAnim = new AutonomyPlayAnimation( self.autonomy, "play wake up anim", Loli.Animation.AWAKE_HAPPY_PILLOW_UP_IDLE );
		self.autonomy.SetAutonomy( playSleepAnim );

		playSleepAnim.onSuccess += EndSleeping;
	}

	private void EndSleeping(){
		self.active.SetTask( self.active.idle );
	}
}

}