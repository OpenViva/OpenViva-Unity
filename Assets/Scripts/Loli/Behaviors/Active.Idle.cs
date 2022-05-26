using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace viva{


public partial class IdleBehavior: ActiveBehaviors.ActiveTask{

	private float nextIdleVariationTime = 3.0f;
	private Loli.Animation nextStandIdleAnim = Loli.Animation.STAND_HAPPY_IDLE1;
	private float idleRootFacingTargetTimer = 0.0f;
	private float checkForInterestsTimer = 0.0f;
	private float ignoreDesirableItemsTimer = 0.0f;
	private int idleVersion = 0;
	public bool enableFaceTargetTimer;
	public bool hasSaidGoodMorning = true;
	private float lastYummyReactTime = 0.0f;

	public IdleBehavior( Loli _self ):base(_self,ActiveBehaviors.Behavior.IDLE,null){
	
		enableFaceTargetTimer = true;

		InitItemSubBehaviors();
		InitBagAnimations();
	}

	public void PlayAvailableRefuseAnimation(){
		if( self.IsTired() ){
			self.SetTargetAnimation( Loli.Animation.STAND_TIRED_REFUSE );
		}else{
			self.SetTargetAnimation( Loli.Animation.STAND_REFUSE );
		}
	}

	public void PlayAvailableDisinterestedAnimation(){
		
		switch( self.bodyState ){
		case BodyState.STAND:
			if( Random.value > 0.5f ){
				self.SetTargetAnimation( Loli.Animation.STAND_HEADPAT_INTERRUPT );
			}else{				
				self.SetTargetAnimation( Loli.Animation.STAND_HEADPAT_ANGRY_END );
			}
			break;
		}
	}

	public bool AttemptImpress(){
		if( !self.IsCurrentAnimationIdle() ){
			return false;
		}
		if( !self.IsHappy() ){
			PlayAvailableDisinterestedAnimation();
			return false;
		}
		self.SetViewAwarenessTimeout(1.0f);
		switch( self.bodyState ){
		case BodyState.STAND:
			if( self.IsTired() ){
				self.SetTargetAnimation( Loli.Animation.STAND_TIRED_REFUSE );
				return false;
			}
			self.SetTargetAnimation( Loli.Animation.STAND_IMPRESSED1 );
			break;
		case BodyState.FLOOR_SIT:
			self.SetTargetAnimation( Loli.Animation.FLOOR_SIT_IMPRESSED1 );
			break;
		}
		return true;
	}

	public Loli.Animation GetAvailableWaveAnimation(){
		switch( self.bodyState ){
		case BodyState.STAND:
			if( self.IsHappy() ){
				if( self.rightHandState.holdType == HoldType.NULL || self.leftHandState.holdType == HoldType.NULL ){
					if( self.rightHandState.holdType == HoldType.NULL ){
						return Loli.Animation.STAND_WAVE_HAPPY_RIGHT;
					}else{
						return Loli.Animation.STAND_WAVE_HAPPY_LEFT;
					}
				}
			}else{
				if( Random.value > 0.5f ){
					return Loli.Animation.STAND_HEADPAT_INTERRUPT;	//reuses animation
				}else{				
					return Loli.Animation.STAND_HEADPAT_ANGRY_END;	//reuses animation
				}
			}
			return Loli.Animation.NONE;
		case BodyState.BATHING_IDLE:
			if( self.IsHappy() ){
				return Loli.Animation.BATHTUB_WAVE_HAPPY_RIGHT;
			}else{
				return Loli.Animation.NONE;
			}
		default:
			return Loli.Animation.NONE;
		}
	}
    
	public override bool OnGesture( Item source, ObjectFingerPointer.Gesture gesture ){
		if( gesture == ObjectFingerPointer.Gesture.HELLO ){
			if( self.IsCurrentAnimationIdle() &&
				self.CanSeePoint( source.transform.position ) ){
				
				Loli.Animation waveAnimation = GetAvailableWaveAnimation();
				if( waveAnimation != Loli.Animation.NONE ){

					self.SetTargetAnimation( waveAnimation );
					self.SetLookAtTarget( source.transform );
					// self.SetRootFacingTarget( source.transform.position, 100.0f, 10.0f, 15.0f );
					self.autonomy.Interrupt(new AutonomyFaceDirection( self.autonomy, "face direction", delegate(TaskTarget target){
                        target.SetTargetPosition( source.transform.position );
                    }, 2.0f ) );
					return true;
				}
			}
		}
		return false;
	}


	public override void OnUpdate(){
		UpdateIdleRootFacingTargetTimer();
		
		//update shoulder items lolic
		if( self.rightShoulderState.occupied ){
			UpdateShoulderItemInteraction( self.rightShoulderState );
		}else{
			UpdateShoulderItemInteraction( self.leftShoulderState );
		}
		if( self.IsCurrentAnimationIdle() ){
			if( Random.value > 0.5f ){	//update random hand item
				UpdateIdleHoldItemInteraction( self.rightLoliHandState );
			}else{
				UpdateIdleHoldItemInteraction( self.leftLoliHandState );
			}
			CheckForVisibleNewInterests();
			CheckToSayGoodMorning();
			UpdateIdleVariations();
		}
	}

	private void CheckToSayGoodMorning(){
		//shinobu has no good morning voice line
		//TODO: Move after waking up instead of checking constantly
		if( self.headModel.voiceIndex != (byte)Voice.VoiceType.SHINOBU ){
			if( !hasSaidGoodMorning && self.IsHappy() ){
				Player player = GameDirector.instance.FindNearbyPlayer( self.head.position, 3.0f );
				if( self.GetCurrentLookAtItem() != null && self.GetCurrentLookAtItem().mainOwner == player ){
					self.SetTargetAnimation( Loli.Animation.STAND_SOUND_GOOD_MORNING );
				}
			}
		}
	}

	public void IgnoreDesirableItems( float duration ){
		ignoreDesirableItemsTimer = duration;
	}

	public void CheckForVisibleNewInterests(){	
		//Debug.Log("V:"+self.GetViewResultCount());
		if( self.bodyState != BodyState.STAND ){
			return;
		}
		//do not check for new interests if in a polling state
		if( self.active.isPolling ){
			return;
		}
		checkForInterestsTimer -= Time.deltaTime;
		ignoreDesirableItemsTimer -= Time.deltaTime;
		if( checkForInterestsTimer > 0.0f ){
			return;
		}
		checkForInterestsTimer = 0.4f;
		List<Item> candidates = new List<Item>();
		for( int i=0; i<self.GetViewResultCount(); i++ ){
			Item item = self.GetViewResult(i);
			
			if( item == null ){
				continue;
			}
			if( item.settings.itemType == Item.Type.REINS ){	//DO NOT ALLOW PICKING UP HORSE REIN
				continue;
			}
			if( item.HasPickupReason( Item.PickupReasons.BEING_PRESENTED ) ){
			}else if( item.HasPickupReason( Item.PickupReasons.HIGHLY_DESIRABLE ) ){
				if( self.ShouldIgnore( item ) ){
					continue;
				}
				if( item.HasAttribute( Item.Attributes.DISABLE_PICKUP ) ){
					continue;
				}
				if( ignoreDesirableItemsTimer > 0.0f ){
					continue;
				}
			}else{
				continue;
			}
			candidates.Add( item );
		}
		if( candidates.Count > 0 ){
			float minDist = Mathf.Infinity;
			Item closest = null;
			for( int i=0; i<candidates.Count; i++ ){
				float dist = Vector3.SqrMagnitude( candidates[i].transform.position-self.head.position );
				if( dist < minDist ){
					minDist = dist;
					closest = candidates[i];
				}
			} 
			//cannot be interested of an already owned item
 			if( closest.mainOwner == self ){
 				return;
	 		}
			self.autonomy.SetAutonomy( new AutonomyPickup( self.autonomy, "pickup interest", closest, self.GetPreferredHandState( closest ), true ));
			//self.active.pickup.AttemptGoAndPickup( closest, self.active.pickup.FindPreferredHandState( closest ) );
		}
	}

	public Loli.Animation GetAvailableIdleAnimation(){
		if( self.IsHappy() && !self.IsTired() ){
			idleVersion = (idleVersion+1)%3;	//cycle
			switch( self.bodyState ){
			case BodyState.STAND:
				switch( idleVersion ){
				case 0:
					return Loli.Animation.NONE;
				case 1:
					return Loli.Animation.STAND_HAPPY_IDLE2;
				case 2:
					return Loli.Animation.STAND_HAPPY_IDLE3;
				}
				break;
			}
		}
		return Loli.Animation.NONE;
	}

	private void UpdateIdleVariations(){
		
		//allow only if not holding anything (disables with items and handholding)
		if( self.rightHandState.heldItem != null || self.leftHandState.heldItem != null ){
			return;
		}
		if( nextIdleVariationTime-Time.time < 0.0f ){
			nextIdleVariationTime = Time.time+10.0f+Random.value*15.0f;	//10~25 sec. wait
			
			var anim = GetAvailableIdleAnimation();
			if( anim != Loli.Animation.NONE ){
				self.SetTargetAnimation( anim );
			}
		}
	}

	private void UpdateIdleRootFacingTargetTimer(){
		if( self.currentLookAtTransform != null && !self.locomotion.isMoveToActive() ){
			idleRootFacingTargetTimer -= Time.deltaTime*System.Convert.ToInt32( enableFaceTargetTimer );
			if( idleRootFacingTargetTimer < 0.0f ){
				idleRootFacingTargetTimer = 4.0f+Random.value*4.0f;
				if( self.active.RequestPermission( ActiveBehaviors.Permission.ALLOW_ROOT_FACING_TARGET_CHANGE ) ){
					self.autonomy.Interrupt(new AutonomyFaceDirection( self.autonomy, "face direction", delegate(TaskTarget target){
                	       target.SetTargetPosition( self.currentLookAtTransform.position );
                    } ) );
					//self.SetRootFacingTarget( self.currentLookAtTransform.position, 200.0f, 15.0f, 30.0f );
				}
			}
		}
	}
	public override void OnAnimationChange( Loli.Animation oldAnim, Loli.Animation newAnim ){

		if( newAnim == Loli.Animation.STAND_SOUND_GOOD_MORNING ){
			hasSaidGoodMorning = true;
		}
		switch( oldAnim ){
		case Loli.Animation.STAND_HAPPY_DONUT_LAST_BITE_RIGHT:
		case Loli.Animation.STAND_HAPPY_DONUT_LAST_BITE_LEFT:
			if( Time.time-lastYummyReactTime > 20.0f ){
				lastYummyReactTime = Time.time;
				if( self.headModel.voiceIndex == (byte)Voice.VoiceType.SHINOBU ){
					self.SetTargetAnimation( Loli.Animation.STAND_SHINOBU_YUMMY );
				}
			}
			break;
		}
	}
}

}