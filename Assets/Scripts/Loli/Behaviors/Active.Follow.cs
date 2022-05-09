using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


namespace viva{


public class FollowBehavior : ActiveBehaviors.ActiveTask {
	
	public FollowBehavior( Loli _self ):base(_self,ActiveBehaviors.Behavior.FOLLOW,null){
	}

	public override bool OnGesture( Item source, ObjectFingerPointer.Gesture gesture ){
		if( gesture == ObjectFingerPointer.Gesture.FOLLOW ){
			return AttemptFollow( source );
		}
		return false;
	}

	public bool AttemptFollow( Item source ){
		
		if( source == null ){
			return false;
		}
		//if the player isint in view dont follow
		if(!self.CanSeePoint(source.transform.position)){
			return false;
		}
		if( self.IsHappy() || self.IsTired() ){
			
			Character followTargetCharacter;
			if( source.settings.itemType == Item.Type.CHARACTER ){
				followTargetCharacter = source.mainOwner;
				if( AttemptFollowFromHorse( followTargetCharacter ) ){
					return true;
				}
			}else{
				followTargetCharacter = null;
			}

			BodyState targetBodyState;
			if( followTargetCharacter ){
				targetBodyState = followTargetCharacter.IsSittingOnFloor() ? BodyState.FLOOR_SIT : BodyState.STAND;
			}else{
				targetBodyState = BodyState.STAND;
			}
			
			var empty = new AutonomyEmpty( self.autonomy, "follow empty" );
			var follow = new AutonomyMoveTo( self.autonomy, "follow", delegate( TaskTarget target ){
					//target.SetTargetItem( source );
					target.SetTargetPosition(followTargetCharacter.floorPos + Vector3.up * 0.1f);
				},
			1.0f, targetBodyState );
			follow.keepDistance = false;
			follow.allowRunning = true;

			empty.AddPassive( follow );
			self.autonomy.SetAutonomy( empty );

			self.active.SetTask( self.active.follow, null );
			return true;
		}else{
			self.active.idle.PlayAvailableRefuseAnimation();
			if( self.bodyState == BodyState.STAND){ //Make sure to only face direction when standing
				self.autonomy.SetAutonomy(new AutonomyFaceDirection( self.autonomy, "face direction", delegate(TaskTarget target){
                    target.SetTargetPosition( source.transform.position );
                }, 15.0f ) );
			}
			// self.SetRootFacingTarget( source.transform.position, 100.0f, 15.0f, 40.0f );
		}
		return false;
	}

	private bool AttemptFollowFromHorse( Character owner ){
		Player player = owner as Player;
		if( player == null ){
			return false;
		}
		Horse horse = player.controller.vehicle as Horse;
		if( horse == null ){
			return false;
		}
		return self.active.horseback.AttemptRideHorsePassenger( horse );
	}
}

}