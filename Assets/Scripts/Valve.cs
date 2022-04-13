using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace viva{

public class Valve : Item {

	[SerializeField]
	public Mechanism parentMechanism;

	[SerializeField]
	private float maxRotation = 360.0f;

	private float currTwist = 0.0f;

	[SerializeField]
	private bool disableTwist = false;

	[SerializeField]
	private bool disablePlayerMouseLookRotation = true;

	private Quaternion restPosition;
	private Tools.EaseBlend rotationOffsetBlend = new Tools.EaseBlend();
	private Vector3 lastVRHandForward = Vector3.zero;

	protected void Start(){
		restPosition = transform.localRotation;
	}
    public override void OnPostPickup(){
		
		PlayerHandState handState = mainOccupyState as PlayerHandState;
		if( handState == null ){
			return;
		}
		switch( mainOwner.characterType ){
		case Character.Type.PLAYER:
			Player player = mainOwner as Player;
			if( player == null ){
				Debug.LogError("ERROR Character is not a player!");
				return;
			}
			if( player.controls == Player.ControlType.KEYBOARD ){
				if( disablePlayerMouseLookRotation ){
					player.SetKeyboardMouseRotationMult( 0.0f );
				}
			}
			lastVRHandForward = handState.absoluteHandTransform.forward;
			float newOffsetTarget = Mathf.Round( -currTwist/90.0f )*90.0f;
			rotationOffsetBlend.reset( newOffsetTarget );
			break;
		}
		parentMechanism.OnItemGrabbed( this );
	}

	public override void OnPreDrop(){
		
		switch( mainOwner.characterType ){
		case Character.Type.PLAYER:
			Player player = mainOwner as Player;
			if( player == null ){
				Debug.LogError("ERROR Character is not a player!");
				return;
			}
			if( disablePlayerMouseLookRotation ){
				player.SetKeyboardMouseRotationMult( 1.0f );
			}
			break;
		}
		parentMechanism.OnItemReleased( this );
	}

	public override void OnItemLateUpdatePostIK(){

		if( mainOwner == null ){
			return;
		}
		
		switch( mainOwner.characterType ){
		case Character.Type.PLAYER:
			Player player = mainOwner as Player;
			if( player == null ){
				Debug.LogError("ERROR Character is not a player!");
				return;
			}
			PlayerHandState playerHoldState = mainOccupyState as PlayerHandState;
			if( playerHoldState == null ){
				Debug.LogError("ERROR HandState is not a PlayerHoldState!");
				return;
			}

			if( player.controls == Player.ControlType.KEYBOARD ){
				UpdateKeyboardPostIKUsage( player, playerHoldState );
			}else{
				UpdateVRPostIKUsage( player, playerHoldState );
			}
			//automatically drop valve if head is too far away
			if( Vector3.SqrMagnitude( mainOwner.head.position-transform.position ) > 1.0f ){
				// mainHoldState.Drop( Vector3.zero );
			}
			break;
		}
	}

	private void UpdateVRPostIKUsage( Player player, PlayerHandState mainHoldState ){

		Plane plane = new Plane( lastVRHandForward, Vector3.zero );
		float pointDistance = plane.GetDistanceToPoint( mainHoldState.absoluteHandTransform.right );
		lastVRHandForward = mainHoldState.absoluteHandTransform.forward;
		ApplyTwist( pointDistance*80.0f );
	}

	private void UpdateKeyboardPostIKUsage( Player player, PlayerHandState mainHoldState ){
		
		//do not apply twist if hand animation is transitioning
		if( player.GetAnimator().IsInTransition(mainHoldState.animSys.GetLayer() ) ){
			return;
		}
		
		//do not apply twist if rotation hasn't reset
		rotationOffsetBlend.Update( Time.deltaTime );
		if( rotationOffsetBlend.value != rotationOffsetBlend.getTarget() ){
			return;
		}
		if( disablePlayerMouseLookRotation ){
			ApplyTwist( player.CalculateMouseMovement().y*4.0f );
		}
	}

	private void ApplyTwist( float radians ){
		
		if( disableTwist ){
			return;
		}
		if( radians == 0.0f ){
			return;
		}
		//calculate final change in twist
		float deltaTwist = currTwist;
		currTwist = Mathf.Clamp( currTwist-radians, 0.0f, maxRotation );
		deltaTwist = currTwist-deltaTwist;

		transform.localRotation = restPosition*Quaternion.Euler( currTwist, 0.0f, 0.0f );
		parentMechanism.OnItemRotationChange( this, currTwist/maxRotation );
	}
}

}