using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace viva{


public abstract class HandState: OccupyState{

	public enum Attribute{
		NONE,
		SOAPY,
	}
	
	public class ButtonState{
		private int lastEvent = 0;
		public bool state { get; private set; } = false;
		public float lastDownTime { get; private set; }
		public bool isHeldDown { get{ return lastEvent<=Time.frameCount && state; } }
		public bool isDown { get{ return lastEvent==Time.frameCount && state; } }
		public bool isUp { get{ return lastEvent==Time.frameCount && !state; } }
		public OnGenericCallback onDown;
		public OnGenericCallback onUp;

		public void UpdateState( bool newState ){
			if( newState != state ){
				if( newState ){
					onDown?.Invoke();
				}else{
					onUp?.Invoke();
				}
				lastEvent = Time.frameCount;
			}
			if( newState ){
				lastDownTime = Time.time;
			}
			state = newState;
		}
        public void Consume(){
            lastEvent = Time.frameCount-1;
        }
	}

	[SerializeField]
	private FingerAnimator m_fingerAnimator;
	public FingerAnimator fingerAnimator { get{ return m_fingerAnimator; } }
	[SerializeField]
	protected Item m_selfItem; //item of the hand itself
	public Item selfItem { get{ return m_selfItem; } }
	[SerializeField]
	private bool maintainGrabColliderTransform = false;
	
	private Attribute attribute = Attribute.NONE;
	private Vector3 cachedTargetPos = Vector3.zero;
	private Quaternion cachedTargetRot = Quaternion.identity;
	public HandState otherHandState { get{ return rightSide ? owner.leftHandState : owner.rightHandState; } }
	

	private void ApplyHoldingTransformCapsuleCollider( CapsuleCollider cc, Item item ){
		Vector3 up;
		Vector3 center = cc.transform.TransformPoint( cc.center );
		switch( cc.direction ){
		case 0:
			up = cc.transform.right;
			break;
		case 1:
			up = cc.transform.up;
			break;
		default:
			up = cc.transform.forward;
			break;
		}

		float height = cc.height-cc.radius*2.0f;
		Vector3 cc0 = center-up*height*0.5f;
		Vector3 cc1 = center+up*height*0.5f;
		float seg = Tools.PointOnRayRatio( cc0, cc1, fingerAnimator.targetBone.position );
		seg = Mathf.Clamp01( seg );

		Vector3 nearestCCPoint = cc0+( cc1-cc0 )*seg;
		float tbToNearestCCPointLength = ( fingerAnimator.targetBone.position-nearestCCPoint ).magnitude;
		Vector3 tbToCCNorm = -fingerAnimator.targetBone.up;
		if( tbToNearestCCPointLength > 0.0f ){
			tbToCCNorm = ( fingerAnimator.targetBone.position-nearestCCPoint )/tbToNearestCCPointLength;
		}
		//move to nearest point
		item.transform.position += tbToCCNorm*( tbToNearestCCPointLength-cc.radius );

		//fix blender animated axis
		float sign = System.Convert.ToInt32(rightSide)*2-1;
		fingerAnimator.targetBone.rotation *= Quaternion.Euler( -90.0f, -90.0f*sign, 0.0f );
		
		//rotate around pivot
		Transform oldParent = item.transform.parent;
		GameDirector.utilityTransform.position = fingerAnimator.targetBone.position;
		GameDirector.utilityTransform.rotation = Quaternion.LookRotation( -tbToCCNorm, fingerAnimator.targetBone.up );	//surface rotation
		item.transform.SetParent( GameDirector.utilityTransform, true );
		GameDirector.utilityTransform.rotation = fingerAnimator.targetBone.rotation;
		item.transform.SetParent( oldParent, true );
	}
	private void ApplyHoldingTransformSphereCollider( SphereCollider sc, Item item ){
		Vector3 up = sc.transform.up;
		Vector3 center = sc.transform.TransformPoint( sc.center );

		Vector3 scToTB = fingerAnimator.targetBone.position-center;
		float scToTBLength = scToTB.magnitude;
		Vector3 scToTBNorm = Vector3.zero;
		if( scToTBLength > 0.0f ){
			scToTBNorm = scToTB/scToTBLength;
		}
		//move to nearest point
		item.transform.position += scToTBNorm*( scToTBLength-sc.radius );

		//fix blender animated axis
		float sign = System.Convert.ToInt32(rightSide)*2-1;
		fingerAnimator.targetBone.rotation *= Quaternion.Euler( -90.0f, -90.0f*sign, 0.0f );
		
		//rotate around pivot
		Transform oldParent = item.transform.parent;
		GameDirector.utilityTransform.position = fingerAnimator.targetBone.position;
		GameDirector.utilityTransform.rotation = Quaternion.LookRotation( -scToTBNorm, fingerAnimator.targetBone.up );	//surface rotation
		item.transform.SetParent( GameDirector.utilityTransform, true );
		GameDirector.utilityTransform.rotation = fingerAnimator.targetBone.rotation;
		item.transform.SetParent( oldParent, true );
	}

	public void GrabItemRigidBody( Item targetItem ){
		if( targetItem == null ){
			// Debug.LogError("[HandState] Cannot setup grab, item is null!");
			return;
		}
		//check if should interrupt and not pick up
		if( targetItem.OnPrePickupInterrupt( this ) ){
			return;
		}
		if( targetItem.rigidBody == null ){
			return;
		}
		if( !Pickup( targetItem ) ){
			return;
		}
		OnPreApplyHoldingTransform( targetItem );
		ApplyItemAnimationGrab( targetItem );
		BeginRigidBodyGrab( targetItem.rigidBody, selfItem.rigidBody, true, HoldType.OBJECT, 0.6f );
		OnPostApplyHoldingTransform( targetItem.transform );

		targetItem.rigidBody.position = targetItem.transform.position;
		targetItem.rigidBody.rotation = targetItem.transform.rotation;
	}

	private void ApplyItemAnimationGrab( Item targetItem ){
		cachedTargetPos = targetItem.transform.position;
		cachedTargetRot = targetItem.transform.rotation;

		targetItem.transform.position = fingerAnimator.targetBone.position;
		targetItem.transform.rotation = fingerAnimator.targetBone.rotation;
	}

	public void GrabGenericRigidBody( Collider collider, Vector3 pos, Vector3 normal ){
		if( collider == null ){
			Debug.LogError("[HandState] Cannot setup grab, collider is null!");
			return;
		}
        Item targetItem = Tools.SearchTransformAncestors<Item>( collider.transform );
		if( targetItem && targetItem.settings.usePickupAnimation ){
			GrabItemRigidBody( targetItem );
		}else{
        	Rigidbody targetBody = Tools.SearchTransformAncestors<Rigidbody>( collider.transform );
			if( targetBody == null ){
				Debug.LogError("[HandState] Cannot setup grab, targetBody is null!");
				return;
			}
			OnPreApplyHoldingTransform( targetItem );
			ApplyColliderAnimationGrab( collider.transform, pos, normal );
			BeginRigidBodyGrab( targetBody, selfItem.rigidBody, true, HoldType.OBJECT, 0.6f );
			OnPostApplyHoldingTransform( collider.transform );
		}
	}

	private void ApplyColliderAnimationGrab( Transform colliderTransform, Vector3 pos, Vector3 normal ){

		cachedTargetPos = colliderTransform.position;
		cachedTargetRot = colliderTransform.rotation;
		
		//move to nearest point
		colliderTransform.position += fingerAnimator.targetBone.position-pos;

		//fix blender animated axis
		float sign = System.Convert.ToInt32(rightSide)*2-1;
		fingerAnimator.targetBone.rotation *= Quaternion.Euler( -90.0f, -90.0f*sign, 0.0f );
		
		//rotate around pivot
		Transform oldParent = colliderTransform.parent;
		GameDirector.utilityTransform.position = fingerAnimator.targetBone.position;
		GameDirector.utilityTransform.rotation = Quaternion.LookRotation( -normal, fingerAnimator.targetBone.up );	//surface rotation
		colliderTransform.SetParent( GameDirector.utilityTransform, true );
		GameDirector.utilityTransform.rotation = fingerAnimator.targetBone.rotation;
		colliderTransform.SetParent( oldParent, true );

		//restore visual transform
		selfItem.transform.position = fingerAnimator.wrist.position;
		selfItem.transform.rotation = fingerAnimator.wrist.rotation;
	}

	protected abstract void OnPreApplyHoldingTransform( Item item );

    protected virtual void OnPostApplyHoldingTransform( Transform grabTransform ){
		
		Transform oldParent = selfItem.transform.parent;
		selfItem.transform.SetParent( grabTransform, true );

		if( maintainGrabColliderTransform ){
			grabTransform.position = cachedTargetPos;
			grabTransform.rotation = cachedTargetRot;
		}

		selfItem.transform.SetParent( oldParent, true );
    }

	protected override void OnPostPickupItem(){
		selfItem.SetItemLayer( Instance.heldItemsLayer );
		if( heldItem.rigidBody ){
			heldItem.rigidBody.useGravity = false;
		}
		if( heldItem.settings.pickupSound ){
			SoundManager.main.RequestHandle( transform.position ).PlayOneShot( heldItem.settings.pickupSound.GetRandomAudioClip() );
		}
	}
	protected override void OnPreDropItem(){
		selfItem.RestoreItemLayer();
		if( heldItem.rigidBody ){
			heldItem.rigidBody.useGravity = true;
		}
	}

	public void SetAttribute( Attribute _attribute ){
		attribute = _attribute;
	}

	public bool HasAttribute( Attribute _attribute ){
		return ((int)attribute&(int)_attribute)!=0;
	}
	
	public T GetItemIfHeld<T>(){
		try{
			return (T)System.Convert.ChangeType( heldItem, typeof(T) );
		}catch{
			return default(T);
		}
	}
}

}