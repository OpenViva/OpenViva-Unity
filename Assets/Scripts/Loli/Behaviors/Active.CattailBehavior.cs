using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace viva{


public class CattailBehavior : ActiveBehaviors.ActiveTask {

	private float cameraPoseTimer = 0.0f;

	public CattailBehavior( Loli _self ):base(_self,ActiveBehaviors.Behavior.CATTAIL,null){
    }
	
	public void SmackWithHandObject(){
		if( Vector3.SqrMagnitude( self.floorPos-GameDirector.player.floorPos ) < 2.25f ){
			if( Mathf.Abs( Tools.Bearing( self.transform, GameDirector.player.floorPos ) ) < 30.0f ){
				GameDirector.instance.postProcessing.DisplayScreenEffect( GamePostProcessing.Effect.HURT );
				self.active.SetTask( self.active.idle, true );
				if( self.currentAnim == Loli.Animation.STAND_CATTAIL_SWING_RIGHT ){
					AttemptPlaySmackSound( self.rightHandState );
				}else{
					AttemptPlaySmackSound( self.leftHandState );
				}
				GameDirector.player.CompleteAchievement( Player.ObjectiveType.WATER_REED_SMACK );
				if( Random.value > 0.5f && self.happiness == Loli.Happiness.VERY_ANGRY ){
					self.ShiftHappiness(1);
				}
			}
		}
	}

	public override void OnDeactivate(){
		self.locomotion.StopMoveTo();
	}

	private void AttemptPlaySmackSound( OccupyState mainHoldState ){
		Cattail cattail = self.rightHandState.heldItem as Cattail;
		if( cattail ){
			cattail.PlaySmackSound();
		}
	}
	
	public override void OnUpdate(){
		
		if( Vector3.SqrMagnitude( GameDirector.player.floorPos-self.floorPos ) > 2.25f ){	//1.5f squared
			// self.active.follow.AttemptFollowRefresh( GameDirector.player.floorPos, true, 0.55f );
			self.SetTargetAnimation( Loli.Animation.STAND_CHASE_LOCOMOTION );
			if( !self.IsSpeakingAtAll() && self.currentAnim == Loli.Animation.STAND_CHASE_LOCOMOTION ){
				self.SpeakAtRandomIntervals( Loli.VoiceLine.ANGRY_LONG, 2.5f, 5.0f );
			}
		}else{
			// self.SetRootFacingTarget( GameDirector.player.floorPos, 140.0f, 25.0f, 20.0f );
			self.autonomy.SetAutonomy(new AutonomyFaceDirection( self.autonomy, "face direction", delegate(TaskTarget target){
                        target.SetTargetPosition( GameDirector.player.floorPos );
                    }, 25.0f ) );

			//if facing player under 35 degrees bearing
			if( Mathf.Abs( Tools.Bearing( self.transform, GameDirector.player.floorPos ) ) < 35.0f &&
				self.IsCurrentAnimationIdle() ){
			
				int availableAttackMove = 0;
				if( self.rightHandState.heldItem != null &&
					self.rightHandState.heldItem.settings.itemType == Item.Type.WATER_REED ){
					availableAttackMove++;
				}
				if( self.leftHandState.heldItem != null &&
					self.leftHandState.heldItem.settings.itemType == Item.Type.WATER_REED ){
					availableAttackMove += 2;
				}
				self.SetLookAtTarget( GameDirector.player.head );
				self.locomotion.StopMoveTo();
				switch( availableAttackMove ){
				case 1:
					self.SetTargetAnimation( Loli.Animation.STAND_CATTAIL_SWING_RIGHT );
					break;
				case 2:
					self.SetTargetAnimation( Loli.Animation.STAND_CATTAIL_SWING_LEFT );
					break;
				case 3:
					float baseAnim = (float)Loli.Animation.STAND_CATTAIL_SWING_RIGHT;
					//increment to next SWING_LEFT if random value > 0.5
					self.SetTargetAnimation( (Loli.Animation)( baseAnim+0.5f+Random.value ) );
					break;
				}
			}
		}
	}
}

}