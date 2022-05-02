using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace viva{


public class TiredBehavior : PassiveBehaviors.PassiveTask {

	
	public bool tired { get{ return self.Tired; } }
	private bool hasShownTired = false;
	private float rubEyesTimer = 10.0f;
	public const float tiredSunPitchRadianStart = 4.3f;
	public const float tiredSunPitchRadianEnd = 5.0f;

	public TiredBehavior( Loli _self):base(_self,0.0f){
	}

	public override void OnUpdate(){
		
		if( !tired ){
			if( GameDirector.skyDirector.sunPitchRadian > tiredSunPitchRadianStart && GameDirector.skyDirector.sunPitchRadian < tiredSunPitchRadianEnd ){
				//SetTired( true );
				BecomeTired();

			}
		}else if( !hasShownTired ){
			if( self.IsCurrentAnimationIdle() ){
				self.SetTargetAnimation( Loli.Animation.STAND_TO_STAND_TIRED );
			}
		}else{
			UpdateTired();
		}
	}

	private void UpdateTired(){
		
		//rub eyes occasionally
		if( !self.IsCurrentAnimationIdle() ){
			return;
		}
		if( self.bodyState == BodyState.STAND ){
			rubEyesTimer -= Time.deltaTime;
			if( rubEyesTimer < 0.0f ){
				if( self.rightHandState.heldItem != null ){
					self.SetTargetAnimation( Loli.Animation.STAND_TIRED_RUB_EYES_RIGHT );
				}else{
					self.SetTargetAnimation( Loli.Animation.STAND_TIRED_RUB_EYES_LEFT );
				}
			}
		}
	}

	public void BecomeTired(){

		if( tired ){
			return;
		}
		self.ShiftHappiness(4);	//treat as happy
		self.Tired = true;
		hasShownTired = false;
	}
	//public void SetTired( bool _tired ){
	//	if( tired == _tired ){
	//		return;
	//	}
	//	self.Tired = _tired;
	//	if( tired ){
	//		self.ShiftHappiness(4);
	//		hasShownTired = false;
	//		//self.OverrideIdleAnimations( BodyState.STAND, Loli.Animation.STAND_TIRED_LOCOMOTION, Loli.Animation.STAND_TIRED_LOCOMOTION );
	//	}else{
	//		// self.OverrideIdleAnimations( BodyState.STAND, Loli.Animation.STAND_HAPPY_IDLE1, Loli.Animation.STAND_ANGRY_IDLE1 );
	//	}
	//}

	public override void OnAnimationChange( Loli.Animation oldAnim, Loli.Animation newAnim ){
		
		switch( newAnim ){
		case Loli.Animation.STAND_TIRED_LOCOMOTION:
			hasShownTired = true;
			break;
		case Loli.Animation.STAND_TIRED_RUB_EYES_RIGHT:
		case Loli.Animation.STAND_TIRED_RUB_EYES_LEFT:
			rubEyesTimer = 45.0f+Random.value*15.0f;
			break;
		}
	}
}

}