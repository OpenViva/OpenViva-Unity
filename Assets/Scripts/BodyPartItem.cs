using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


namespace viva{


public class BodyPartItem : Item{


	[SerializeField]
	private int[] limbMuscleIndices = new int[]{};
	[SerializeField]
	private bool rightSide = false;
	[SerializeField]
	private bool partOfAnArm = false;
	[SerializeField]
	private bool isHand = false;

	private RootMotion.Dynamics.Muscle.Props.Multiplier multiplier = null;
	private Loli self { get{ return mainOwner as Loli; } }
	private ConfigurableJoint joint;


	public override void OnItemFixedUpdate(){
		base.OnItemFixedUpdate();

		if( joint && mainOccupyState != null ){
			joint.anchor = joint.transform.InverseTransformPoint( self.puppetMaster.muscles[ limbMuscleIndices[0] ].rigidbody.transform.position );
		}
	}

	public override bool OnPrePickupInterrupt( HandState handState ){
		if( !self.IsHappy() ){
			if( handState.rightSide ){
				self.SetTargetAnimation( Loli.Animation.STAND_HANDHOLD_ANGRY_REFUSE_RIGHT );
			}else{
				self.SetTargetAnimation( Loli.Animation.STAND_HANDHOLD_ANGRY_REFUSE_LEFT );
			}
			return true;
		}
		return false;
	}
    
	public override bool CanBePickedUp( OccupyState sourceOccupyState ){
		if( self == null ){
			return false;
		}

		if( partOfAnArm ){
			HandState targetHandState;
			if( rightSide ){
				targetHandState = self.rightHandState;
				if( !self.active.RequestPermission( ActiveBehaviors.Permission.BEGIN_RIGHT_HANDHOLD ) ){
					return false;
				}
			}else{
				targetHandState = self.leftHandState;
				if( !self.active.RequestPermission( ActiveBehaviors.Permission.BEGIN_LEFT_HANDHOLD ) ){
					return false;
				}
			}
			if( targetHandState.holdType != HoldType.NULL ){
				return false;
			}
		}
		//request permission from active behavior
		return base.CanBePickedUp( sourceOccupyState );
	}

	private void SetLimbMusclesBeingHeld( bool marked ){
		
		if( self == null || !partOfAnArm ){
			return;
		}
		var upperArmMuscle = self.puppetMaster.muscles[ limbMuscleIndices[0] ];
		if( marked ){
			multiplier = new RootMotion.Dynamics.Muscle.Props.Multiplier( 0.0f, 0.0f );
			RegisterPropMultiplierAlongEntireLimb( multiplier );
		}else{
			UnregisterPropMultiplierAlongEntireLimb( multiplier );
		}
	}

	public void RegisterPropMultiplierAlongEntireLimb( RootMotion.Dynamics.Muscle.Props.Multiplier multiplier ){
		foreach( int muscleIndex in limbMuscleIndices ){
			self.puppetMaster.muscles[ muscleIndex ].props.RegisterMultiplier( multiplier );
		}
	}

	public void UnregisterPropMultiplierAlongEntireLimb( RootMotion.Dynamics.Muscle.Props.Multiplier multiplier ){
		foreach( int muscleIndex in limbMuscleIndices ){
			self.puppetMaster.muscles[ muscleIndex ].props.UnregisterMultiplier( multiplier );
		}
	}
	
	public override Player.Animation GetPreferredPlayerHeldAnimation( PlayerHandState playerHandState ){
		if( !isHand ){
			return base.GetPreferredPlayerHeldAnimation( playerHandState );
		}
		if( playerHandState.rightSide != rightSide ){
			return Player.Animation.HAND;
		}else{
			return Player.Animation.HAND_ALT;
		}
	}
	
	public override void OnPostPickup(){

		if( self == null ){
			return;
		}
		if( isHand ){
			self.passive.handhold.AddHandhold( rightSide );
		}
		SetLimbMusclesBeingHeld( true );
		Rigidbody rb = mainOccupyState.gameObject.GetComponent<Rigidbody>();
		if( rb && partOfAnArm ){
			joint = self.spine1RigidBody.gameObject.AddComponent<ConfigurableJoint>();
			joint.connectedBody = rb;
			joint.autoConfigureConnectedAnchor = false;
			joint.massScale = 1000.0f;
			joint.connectedMassScale = 10.0f;
			joint.xMotion = ConfigurableJointMotion.Limited;
			joint.yMotion = ConfigurableJointMotion.Limited;
			joint.zMotion = ConfigurableJointMotion.Limited;
			var lm = joint.linearLimit;
			lm.limit = 0.4f;
			joint.linearLimit = lm;
			joint.anchor = Vector3.zero;
			joint.connectedAnchor = Vector3.zero;
			joint.projectionMode = JointProjectionMode.PositionAndRotation;
		}
	}
	public override void OnPreDrop(){

		if( self == null ){
			return;
		}
		if( partOfAnArm ){
			self.passive.handhold.RemoveHandHold( rightSide );
		}
		SetLimbMusclesBeingHeld( false );

		if( joint ){
			GameObject.Destroy( joint );
			joint = null;
		}
	}
}

}