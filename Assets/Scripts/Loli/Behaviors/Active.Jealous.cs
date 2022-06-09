// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.AI;


// namespace viva{


// public class JealousBehavior : ActiveBehaviors.ActiveTask {

// 	private float stealCurrentFlagTime = -1.0f;
// 	private Item currentInterest = null;


// 	public JealousBehavior( Loli _self ):base(_self,ActiveBehaviors.Behavior.JEALOUS,null){
// 	}


// 	public static void jobJealousCallback( Character self, Occupation occupation, OccupyType replace ){
// 		Debug.Log("JEALOUS "+occupation+","+Time.frameCount+":"+replace);
// 		Loli loli = self as Loli;
// 		switch( replace ){
// 		case HoldType.OBJECT:
// 			if( loli.currentAnim == Loli.Animation.STAND_ANGRY_TIP_TOE_REACH ){
// 				if( loli.IsFaceYawAnimationEnabled()){	//if currently on floor
// 					loli.AttemptChangeBodyState( Loli.BodyState.STAND );
// 					loli.SetTargetAnimation(loli.GetLastReturnableIdleAnimation());
// 				}else{
// 					loli.OverrideClearAnimationPriority();
// 					loli.SetTargetAnimation( Loli.Animation.STAND_ANGRY_TIP_TOE_REACH_END );
// 				}
// 			}
// 			loli.active.pickup.SetPostPickupAnimationByItemType( loli.active.jealous.currentInterest.settings.itemType );
// 			loli.SetLookAtTarget(null);

// 			loli.active.SetTask( loli.active.idle, true );
// 			break;
// 		}
// 	}

// 	public bool AttemptJobJealous( Item item ){
// 		if( item == null ){
// 			return false;
// 		}
// 		//cannot be jealous of an already owned item
// 		if( item.mainOwner == self ){
// 			return false;
// 		}
// 		if( self.rightHandState.holdType == HoldType.NULL || 
// 			self.leftHandState.holdType == HoldType.NULL ){

// 			self.active.SetTask( self.active.jealous, null );
// 			currentInterest = item;
// 			self.active.pickup.SetPostPickupAnimation( self.GetLastReturnableIdleAnimation() );
// 			self.SetRootFacingTarget( item.transform.position, 120.0f, 20.0f, 10.0f );
// 			self.SetLookAtTarget( item.transform, 1.3f );
// 			self.SetViewAwarenessTimeout( 1.0f );

// 			if( Random.value > 0.4f ){
// 				self.SetTargetAnimation( Loli.Animation.STAND_ANGRY_JEALOUS );
// 			}else{
// 				self.SetTargetAnimation( Loli.Animation.STAND_LOCOMOTION_JEALOUS );
// 			}
// 			return true;
// 		}
// 		return false;
// 	}

// 	public override void OnDeactivate(){

// 		self.IgnoreItem( currentInterest, 1.0f );
// 		self.locomotion.StopMoveTo();
// 		//ensure she is not stuck looping in a job specific animation
// 		switch( self.currentAnim ){
// 		case Loli.Animation.STAND_LOCOMOTION_JEALOUS:
// 		case Loli.Animation.STAND_ANGRY_TIP_TOE_REACH:
// 			self.OverrideClearAnimationPriority();
// 			self.SetTargetAnimation( self.GetLastReturnableIdleAnimation() );
// 			break;
// 		}
// 	}

// 	public override void OnUpdate(){

// 		if( currentInterest == null ){
// 			self.active.SetTask( self.active.idle, false );
// 			self.SetTargetAnimation( self.GetLastReturnableIdleAnimation() );
// 			return;
// 		}
// 		if( !currentInterest.IsPickedUp() ){	//was dropped
// 			if( !self.active.pickup.AttemptGoAndPickup( currentInterest ) ){
// 				self.active.SetTask( self.active.idle, false );
// 			}
// 			return;
// 		}
// 		//if can pickup or is already registered
// 		bool canPickupWithRight = self.rightHandState.heldItem == null;
// 		bool canPickupWithLeft = self.leftHandState.heldItem == null;

// 		if( !canPickupWithRight && !canPickupWithLeft ){
// 			self.active.SetTask( self.active.idle, false );
// 			self.SetTargetAnimation( self.GetLastReturnableIdleAnimation() );
// 			return;
// 		}
// 		if( self.currentAnim == Loli.Animation.STAND_ANGRY_JEALOUS_STEAL_RIGHT ||
// 			self.currentAnim == Loli.Animation.STAND_ANGRY_JEALOUS_STEAL_LEFT ||
// 			self.currentAnim == Loli.Animation.STAND_ANGRY_TIP_TOE_REACH ){

// 			if( self.rightHandState.IsCurrentlyRegistered( jobJealousCallback ) ){
// 				Loli.LoliHandState rightHand = (Loli.LoliHandState)self.rightHandState;
// 				self.rightLoliHandState.overrideRetargeting.SetupRetargeting(
// 					CalculateTipToeReachTarget( self.rightHandState ),
// 					self.active.pickup.calculateTipToeReachArmPole( self.rightHandState ),
// 					rightHand.armIK.ik.p1.rotation
// 				);
// 			}
// 			if( self.leftHandState.IsCurrentlyRegistered( jobJealousCallback ) ){
// 				Loli.LoliHandState leftHand = (Loli.LoliHandState)self.leftHandState;
// 				self.leftLoliHandState.overrideRetargeting.SetupRetargeting(
// 					CalculateTipToeReachTarget( self.leftLoliHandState ),
// 					self.active.pickup.calculateTipToeReachArmPole( self.leftLoliHandState ),
// 					leftHand.armIK.ik.p1.rotation
// 				);
// 			}
// 			if( self.currentAnim == Loli.Animation.STAND_ANGRY_TIP_TOE_REACH ){

// 				if( self.IsFaceYawAnimationEnabled() ){	//is on floor with both feet
// 					if( !IsInTipToeRange( currentInterest ) ){
// 						self.SetTargetAnimation( Loli.Animation.STAND_LOCOMOTION_JEALOUS );
// 					}
// 				}
// 				self.SetRootFacingTarget( currentInterest.transform.position, 240.0f, 25.0f, 2.0f );
// 			}else{
// 				self.SetRootFacingTarget( currentInterest.transform.position, 240.0f, 35.0f, 15.0f );
// 			}
// 		}else if( Time.time-self.active.follow.followRefreshTime > 0.4f ){
// 			self.active.follow.followRefreshTime = Time.time;

// 			//check if can steal object
// 			float bearing = Tools.Bearing( self.transform, currentInterest.transform.position );
// 			if( self.active.pickup.IsAtArmsLengthOfItem( Tools.CalculateCenterAndBoundingHeight( currentInterest.gameObject, 0.04f ), PickupBehavior.minFarPickupDistance ) ){
// 				self.SetRootFacingTarget( currentInterest.transform.position, 240.0f, 35.0f, 15.0f );
// 				if( Mathf.Abs( bearing ) < 35.0f ){
// 					if( IsInTipToeRange( currentInterest ) ){
// 						self.SetTargetAnimation( Loli.Animation.STAND_ANGRY_TIP_TOE_REACH );
// 					}else{
// 						//set random steal hand
// 						int stealHand = -1;
// 						if( canPickupWithRight && canPickupWithLeft ){
// 							stealHand = Random.Range(0,2);
// 						}else if( canPickupWithRight ){
// 							stealHand = 0;
// 						}else if( canPickupWithLeft ){
// 							stealHand = 1;
// 						}
// 						switch( stealHand ){
// 						case 0:
// 							self.SetTargetAnimation( Loli.Animation.STAND_ANGRY_JEALOUS_STEAL_RIGHT );
// 							break;
// 						case 1:
// 							self.SetTargetAnimation( Loli.Animation.STAND_ANGRY_JEALOUS_STEAL_LEFT );
// 							break;
// 						default:
// 							break;
// 						}

// 						self.SetLookAtTarget( currentInterest.transform );
// 					}
// 					self.active.pickup.SetPostPickupAnimation( Loli.Animation.STAND_LOCOMOTION_JEALOUS );
// 				}
// 			}else if( self.currentAnim == Loli.Animation.STAND_ANGRY_TIP_TOE_REACH ){
// 				self.SetTargetAnimation( Loli.Animation.STAND_ANGRY_TIP_TOE_REACH_END);
// 			}else{
// 				Vector3? nearest = self.locomotion.FindNearestWalkablePoint( currentInterest.transform.position, 0.45f, 0.0f, 2.0f, 0 );
// 				if( nearest.HasValue ){

// 					Vector3[] newPath = self.locomotion.GetNavMeshPath( nearest.Value );
// 					if( newPath != null ){
// 						self.locomotion.FollowPath( newPath );
// 					}
// 				}
// 				self.SetTargetAnimation( Loli.Animation.STAND_LOCOMOTION_JEALOUS );
// 				if( !self.IsSpeakingAtAll() && self.currentAnim == Loli.Animation.STAND_LOCOMOTION_JEALOUS ){
// 					self.SpeakAtRandomIntervals( Loli.VoiceLine.ANGRY_LONG, 2.5f, 5.0f );
// 				}
// 			}
// 		}
// 	}

// 	private bool IsInTipToeRange( Item item ){
// 		if( currentInterest.transform.position.y-self.head.position.y < 0.4f ){
// 			return false;
// 		}
// 		if( Vector3.SqrMagnitude( currentInterest.transform.position-self.head.position ) > 2.25f ){	//too far
// 			return false;
// 		}
// 		return true;
// 	}

// 	public override void OnLateUpdatePostIK(){

// 		if( self.currentAnim == Loli.Animation.STAND_ANGRY_TIP_TOE_REACH ){
// 			if( self.active.pickup.AttemptPhysicallyPickupItem( self.rightHandState, currentInterest, 0.45f ) ){
// 				//endJobJealous();
// 			}else if( self.active.pickup.AttemptPhysicallyPickupItem( self.leftHandState, currentInterest, 0.45f ) ){
// 				//endJobJealous();
// 			}
// 		}else if( Time.time-stealCurrentFlagTime == 0.0f ){
// 			if( self.currentAnim == Loli.Animation.STAND_ANGRY_JEALOUS_STEAL_RIGHT ){
// 				AttemptStealItem( self.rightHandState );
// 			}else if( self.currentAnim == Loli.Animation.STAND_ANGRY_JEALOUS_STEAL_LEFT ){
// 				AttemptStealItem( self.leftHandState );
// 			}
// 		}
// 	}

// 	private Vector3 CalculateTipToeReachTarget( OccupyState handState ){

// 		float sign = -1.0f+(float)System.Convert.ToInt32( handState == self.rightHandState )*2.0f;

// 		float varyX = 1.0f+Mathf.Sin( Time.time*11.0f+sign );
// 		Vector3 local = currentInterest.transform.position;
// 		local += self.transform.right*0.035f*sign*varyX;
// 	 	local = self.spine2.transform.InverseTransformPoint( local );
// 		if( sign == 1.0f ){
// 			local.x = Mathf.Max( 0.0f, local.x );
// 		}else{
// 			local.x = Mathf.Min( 0.0f, local.x );
// 		}
// 		local.z = Mathf.Max( 0.05f, local.z );
// 		return self.spine2.transform.TransformPoint( local );
// 	}

// 	public void FlagStealCurrentInterest(){
// 		stealCurrentFlagTime = Time.time;
// 	}
// 	private void AttemptStealItem( HandState handState ){
// 		if( currentInterest == null ){
// 			Debug.LogError("Cant steal null Item!");
// 			return;
// 		}
// 		if( Vector3.SqrMagnitude(currentInterest.transform.position-handState.fingerAnimator.hand.position) < 0.06f ){

// 			handState.GrabItemRigidBody( currentInterest );
// 			self.active.pickup.SetPostPickupAnimation( Loli.Animation.STAND_ANGRY_IDLE1 );
// 		}
// 		self.locomotion.PlayForce( Random.insideUnitSphere*Random.value*0.5f, 0.3f );
// 	}
// }

// }