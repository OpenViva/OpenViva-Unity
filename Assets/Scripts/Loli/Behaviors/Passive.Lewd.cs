using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace viva{


public class LewdBehavior : PassiveBehaviors.PassiveTask {

	private float pervTimer = 0.0f;

	public LewdBehavior( Loli _self):base(_self,0.0f){
	}

	public override void OnUpdate(){

		if( pervTimer <= 0.0f ){
			if( self.bodyState == BodyState.STAND && CheckIfLookingUpSkirt() ){
				BeginReactPervSkirtFront();
			}
		}else{
			if( !CheckIfLookingUpSkirt(1.5f) ){
				pervTimer -= Time.deltaTime;
				if( pervTimer < 0 ){
					self.SetTargetAnimation(Loli.Animation.STAND_REACT_PERV_FRONT_OUT);
				}
			}else{
				pervTimer = 1.0f;
			}
		}
	}

	public bool CheckIfLookingUpSkirt( float strictMult=1.0f ){
		if( self.GetCurrentLookAtItem() == null ){
			return false;
		}
		if( self.GetCurrentLookAtItem().settings.itemType != Item.Type.CHARACTER ){
			return false;
		}
		if( !self.active.IsTaskActive( self.active.idle ) && !self.active.IsTaskActive( self.active.follow ) ){
			return false;
		}
		Vector3 pubis = self.spine2.transform.position-self.spine2.transform.up*0.1f;
		if( GameDirector.player.head.position.y < self.spine2.position.y ){
			if( (GameDirector.player.head.position-pubis).sqrMagnitude < 1.0f ){
				Vector3 playerLookAt = GameDirector.player.floorPos+GameDirector.player.head.forward*4.0f;
				float lookAtValue = Tools.PointToSegmentDistance( GameDirector.player.floorPos, playerLookAt, pubis );
				if( lookAtValue < 0.15f*strictMult ){
					return true;
				}
			}
		}
		return false;
	}

	public void BeginReactPervSkirtFront(){
		
		//face a random direction away from player
		Vector3 toPlayer = GameDirector.player.floorPos-self.floorPos;
		toPlayer.y = 0.0f;
		Vector3 forward = toPlayer.normalized;
		Vector3 right = new Vector3( -forward.z, 0.0f, forward.x );
		float side = Mathf.Floor( Random.value*2.0f )*2.0f-1.0f;	//-1 or 1
		Vector3 facingPosition = self.floorPos+forward+right*side;
		// self.SetRootFacingTarget( facingPosition, 200.0f, 40.0f, 10.0f );
		self.autonomy.SetAutonomy(new AutonomyFaceDirection( self.autonomy, "face direction", delegate(TaskTarget target){
                        target.SetTargetPosition( facingPosition );
                    } ) );
		
		self.SetTargetAnimation(Loli.Animation.STAND_REACT_PERV_FRONT_IN);
		pervTimer = 2.0f;
		self.locomotion.StopMoveTo();
	}
	
	public override void OnAnimationChange( Loli.Animation oldAnim, Loli.Animation newAnim ){
		switch( oldAnim ){
		case Loli.Animation.STAND_REACT_PERV_FRONT_OUT:
			// self.registry.unregister( (int)Occupation.EVERYTHING, OccupyType.CLEAR );
			self.SetViewAwarenessTimeout( 0.0f );
			break;
		}
		switch( newAnim ){
		case Loli.Animation.STAND_REACT_PERV_FRONT_IN:
			GameDirector.player.CompleteAchievement(Player.ObjectiveType.LOOK_UP_SKIRT);
			// self.hold.dropAllHandObjectsIfAvailable();
			self.ShiftHappiness(-2);
			self.SetViewAwarenessTimeout( 5.0f );
			break;
		}
	}
}

}