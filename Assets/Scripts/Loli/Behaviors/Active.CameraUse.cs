using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace viva{


public class CameraUseBehavior : ActiveBehaviors.ActiveTask {

	private enum Phase{
		NONE,
		TAKING_PICTURE,
		TOOK_PICTURE,
		GRABBED_PICTURE
	}

	private Phase currentPhase = Phase.NONE;
	private Item cameraTarget = null;
	private float cameraPitchDir = 0.0f;
	private float takingPictureTimeout = 0.0f;
	private float cameraTargetStandingStillTimer = 0.0f;
	private float cameraPrepared = 0.0f;
	private bool stillTimerHasReset = true;

	public CameraUseBehavior( Loli _self ):base(_self,ActiveBehaviors.Behavior.CAMERA_USE,null){
    }

	// public override void OnActivate(){
	// 	cameraPitchDir = 0.0f;
	// 	currentPhase = Phase.NONE;
	// 	takingPictureTimeout = 6.0f;
	// 	stillTimerHasReset = true;
	// 	cameraPrepared = 1.0f;
	// }

	public override void OnDeactivate(){
	}

	public override void OnLateUpdate(){
		switch( currentPhase ){
		case Phase.NONE:
			if( self.IsCurrentAnimationIdle() ){
				self.SetTargetAnimation( Loli.Animation.STAND_HAPPY_TAKE_PHOTO_IN );
			}
			break;
		case Phase.TAKING_PICTURE:
			LateUpdateTakingPicture();
			break;
		case Phase.TOOK_PICTURE:
			LateUpdateTookPicture();
			break;
		}
	}

	private void LateUpdateTakingPicture(){
		if( self.currentAnim == Loli.Animation.STAND_HAPPY_TAKE_PHOTO_LOOP ){

			PolaroidCamera camera = GetHeldCamera();
			if( cameraTarget == null || camera == null ){
				EndCameraUseBehavior( true, false );
				return;
			}
			float pitch = Tools.Pitch( self.head, cameraTarget.transform );
			UpdateCameraPitchDir( pitch/90.0f );

			// self.SetRootFacingTarget( cameraTarget.transform.position, 200.0f, 12.0f, 10.0f );

			if( IsCameraTargetPrepared( camera ) && stillTimerHasReset ){
				
				self.SetEyeVariables( 0, 0.5f );
				self.active.idle.ResetPolaroidFrameInspectTimer();

				cameraPrepared += Time.deltaTime*2.0f;
				cameraTargetStandingStillTimer += Time.deltaTime;
				if( cameraTargetStandingStillTimer > 2.0f ){
					camera.SnapPhoto( OnNewPolaroidPhotoCreated, PickupNewPolaroidPhoto );
					cameraTargetStandingStillTimer = 0.0f;
					currentPhase = Phase.TOOK_PICTURE;
				}
			}else{
				self.SetEyeVariables( 12, 0.5f );

				cameraTargetStandingStillTimer = 0.0f;
				cameraPrepared -= Time.deltaTime*2.0f;
				if( cameraPrepared <= 0.0f ){
					stillTimerHasReset = true;
				}else{
					stillTimerHasReset = false;
				}
			}
			cameraPrepared = Mathf.Clamp01( cameraPrepared );
			self.animator.SetFloat( Instance.cameraPrepared, Tools.EaseInOutQuad( cameraPrepared ) );
		}
	}

	private void OnNewPolaroidPhotoCreated( PolaroidFrame polaroidFrame ){
		if( polaroidFrame == null ){
			return;
		}
		self.SetLookAtTarget( polaroidFrame.transform, 1.5f );
	}

	private void PickupNewPolaroidPhoto( PolaroidFrame polaroidFrame ){
		if( !self.active.IsTaskActive( this ) ){
			return;
		}
		if( polaroidFrame == null ){
			return;
		}
		//pick up new photo with a free hand
		if( self.rightHandState.heldItem == null ){
			self.rightHandState.GrabItemRigidBody( polaroidFrame );
		}else if( self.leftHandState.heldItem == null ){
			self.leftHandState.GrabItemRigidBody( polaroidFrame );
		}

		EndCameraUseBehavior( true, true );
	}

	private void UpdateCameraPitchDir( float targetPitchDir ){

		cameraPitchDir = Mathf.Clamp( cameraPitchDir+targetPitchDir*Time.deltaTime*4.0f, -1.0f, 1.0f );
		self.animator.SetFloat( Instance.cameraPitchDir, cameraPitchDir );
	}

	private void LateUpdateTookPicture(){

		PolaroidCamera camera = GetHeldCamera();
		if( camera == null ){
			EndCameraUseBehavior( true, false );
			return;
		}

		UpdateCameraPitchDir( 0.0f );
		if( Mathf.Abs( cameraPitchDir ) < 0.2f ){
			if( self.FindOccupyStateByHeldItem( camera ) == self.rightHandState ){
				self.SetTargetAnimation( Loli.Animation.STAND_HAPPY_TAKE_PHOTO_OUT_RIGHT );
			}else{
				self.SetTargetAnimation( Loli.Animation.STAND_HAPPY_TAKE_PHOTO_OUT_LEFT );
			}
		}
	}

	private PolaroidCamera GetHeldCamera(){
		PolaroidCamera camera = self.rightHandState.GetItemIfHeld<PolaroidCamera>();
		if( camera != null ){
			return camera;
		}
		return self.leftHandState.GetItemIfHeld<PolaroidCamera>();
	}

	private bool IsCameraTargetPrepared( PolaroidCamera camera ){

		//if target is not moving
		if( Mathf.Abs( cameraTarget.rigidBody.velocity.magnitude ) > 0.4f ){
			cameraTargetStandingStillTimer = 0.0f;
			return false;
		}
		//target must be looking at camera if it's a character
		if( cameraTarget.settings.itemType == Item.Type.CHARACTER ){
			Item headItem = cameraTarget.mainOwner.head.GetComponent<Item>();
			if( headItem != null ){
				float distanceFromTarget = Vector3.Magnitude( camera.transform.position-headItem.transform.position );
				float lookAtDist = Tools.PointToSegmentDistance(
					headItem.transform.position,
					headItem.transform.position+headItem.transform.forward*12.0f,
					self.head.position
				);
				float minLookAtDist = Tools.RemapClamped( 0.3f, 6.0f, 0.2f, 2.2f, distanceFromTarget );
				if( lookAtDist < minLookAtDist ){
					return true;
				}
			}
		}else{
			return false;
		}
		return false;
	}
	
	public void EndCameraUseBehavior( bool returnToLastIdleAnim, bool? succeededTask ){
		if( returnToLastIdleAnim ){
			self.SetTargetAnimation( self.GetLastReturnableIdleAnimation() );
		}
		self.active.SetTask( self.active.idle, succeededTask );
	}

	public override bool OnGesture( Item source, ObjectFingerPointer.Gesture gesture ){
		if( gesture == ObjectFingerPointer.Gesture.HELLO ){
			if( source.settings.itemType == Item.Type.CHARACTER ){
				Item headItem = source.mainOwner.head.GetComponent<Item>();
				if( headItem != null ){
					return AttemptTakePicture( headItem );
				}else{
					Debug.LogError("Could not find head for source character item!");
				}
			}
			return AttemptTakePicture( source );
		}
		return false;
	}

	public bool AttemptTakePicture( Item target ){
		if( target == null ){
			return false;
		}
		PolaroidCamera camRight = self.rightHandState.GetItemIfHeld<PolaroidCamera>();
		PolaroidCamera camLeft = self.leftHandState.GetItemIfHeld<PolaroidCamera>();
		if( ( camRight!=null ) == ( camLeft!=null ) ){	//must hold 1 camera only
			return false;
		}
		if( camRight != null ){
			self.IgnoreItem( self.leftHandState.heldItem, 1.0f );
			self.leftHandState.AttemptDrop();
		}else{
			self.IgnoreItem( self.rightHandState.heldItem, 1.0f );
			self.rightHandState.AttemptDrop();
		}
		//must be idling or following to begin
		if( !self.active.IsTaskActive( self.active.idle ) && !self.active.IsTaskActive( self.active.follow ) ){
			return false;
		}
		//must be able to see camera
		if( !self.IsCurrentAnimationIdle() || !self.CanSeePoint( target.transform.position ) ){
			return false;
		}

		// self.SetRootFacingTarget( target.transform.position, 200.0f, 12.0f, 10.0f );
		if( !self.IsHappy() || self.passive.tired.tired ){	//must be happy and not tired
			self.active.idle.PlayAvailableRefuseAnimation();
			return true;
		}

		cameraTarget = target;
		self.active.SetTask( this, null );
		return true;
	}

	public override void OnAnimationChange( Loli.Animation oldAnim, Loli.Animation newAnim ){
		
		if( newAnim == Loli.Animation.STAND_HAPPY_TAKE_PHOTO_LOOP ){
			currentPhase = Phase.TAKING_PICTURE;
		}
		switch( newAnim ){
		case Loli.Animation.STAND_HAPPY_TAKE_PHOTO_IN:
		case Loli.Animation.STAND_HAPPY_TAKE_PHOTO_LOOP:
		case Loli.Animation.STAND_HAPPY_TAKE_PHOTO_OUT_RIGHT:
		case Loli.Animation.STAND_HAPPY_TAKE_PHOTO_OUT_LEFT:
			break;
		default:	//INTERRUPTED BY PASSIVE BEHAVIOR DETECTED
			EndCameraUseBehavior( false, false );
			break;
		}
	}
}

}