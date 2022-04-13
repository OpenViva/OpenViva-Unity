using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


namespace viva{


public class AutonomyPickup : AutonomyItemMove {

	protected Item targetItem { get; private set; }
	protected LoliHandState targetHandState { get; private set; }
	private BlendController lastRightBlendController;
	private BlendController lastLeftBlendController;

	//changes logic to keep holding item
	private bool maintainItem = false;
	private AutonomyPlayAnimation playBegStartAnim;
	private AutonomyPlayAnimation setBegLocomotion;
	private Vector3 begTargetPos = Vector3.zero;
	public bool begging { get; private set; } = false;
	private readonly bool allowBegging;
	public Loli.BodyStateAnimationSet overrideAnimationSets;
	
	
    public AutonomyPickup( Autonomy _autonomy, string _name, Item _targetItem, LoliHandState _targetHandState, bool _allowBegging = true ):base(_autonomy,_name){
		targetItem = _targetItem;

		SetTargetHandState( _targetHandState );
		allowBegging = _allowBegging;
		playTargetAnim.onAnimationEnter += delegate{ new BlendController( targetHandState, playTargetAnim.entryAnimation, OnAnimationIKControl ); };
		playTargetAnim.onSuccess += CheckIfPickedupItem;
		playTargetAnim.onCharacterTriggerEnter += HandleItemCollision;

		faceTarget.onCharacterTriggerEnter += HandleItemCollision;

		maintainItem = targetItem.mainOccupyState == targetHandState;

		targetItem.onMainOccupyStateChanged += OnTargetItemOccupyStateChanged;

		onRemovedFromQueue += delegate{ if( targetItem != null ){ targetItem.onMainOccupyStateChanged -= OnTargetItemOccupyStateChanged; } };

		//fire to initialize if begging
		OnTargetItemOccupyStateChanged( null, targetItem.mainOccupyState );
    }

	public void SetTargetHandState( LoliHandState newTargetHandState ){
		targetHandState = newTargetHandState;
		maintainItem = targetItem.mainOccupyState == targetHandState;
	}

	private void OnTargetItemOccupyStateChanged( OccupyState oldOccupyState, OccupyState newOccupyState ){
		if( newOccupyState != null ){
			if( newOccupyState.owner != self ){
				SwitchToBegMode();
			}
		}else{
			SwitchToNormalMode();
		}
	}

	protected override void ReadTargetLocation( TaskTarget target ){
		target.SetTargetItem( targetItem );
	}

	private void CheckIfPickedupItem(){
		if( targetItem.mainOccupyState == targetHandState ){
			return;
		}
		CheckIfShouldPlayAgain();
	}

	private float OnAnimationIKControl( BlendController blendController ){
		if( targetItem == null ){
			return 0.0f;
		}
		if( succeeded ){
			return 0.0f;
		}
		return SetupReachIKControl( blendController, targetItem.transform.position, self );
	}

	private void SwitchToNormalMode(){
		
		if( !begging ){
			return;
		}
		begging = false;

		if( setBegLocomotion != null ){
			RemoveRequirement( setBegLocomotion );
			AddRequirement( waitForIdle );
			AddRequirement( playTargetAnim );
		}
	}

	private void SwitchToBegMode(){
		if( begging || !allowBegging ){
			return;
		}	
		begging = true;
		if( playBegStartAnim == null ){
			playBegStartAnim = new AutonomyPlayAnimation( self.autonomy, "play beg start anim", Loli.Animation.STAND_HAPPY_BEG_START );
			playBegStartAnim.AddPassive( new AutonomyFaceDirection( self.autonomy, "face beg item", delegate(TaskTarget target){ target.SetTargetItem( targetItem ); }, 1.0f, 10.0f  ));

			playBegStartAnim.onSuccess += BeginBegLogic;
			playBegStartAnim.onCharacterTriggerEnter += HandleItemCollision;
			
			if( !self.rightHandState.occupied ){
				lastRightBlendController = new BlendController( self.rightLoliHandState, Loli.Animation.STAND_HAPPY_BEG_LOCOMOTION, OnBegIKControl, 0.7f );
			}
			if( !self.leftHandState.occupied ){
				lastLeftBlendController = new BlendController( self.leftLoliHandState, Loli.Animation.STAND_HAPPY_BEG_LOCOMOTION, OnBegIKControl, 0.7f );
			}
			PrependRequirement( playBegStartAnim );
		}else{
			BeginBegLogic();
		}
		begTargetPos = targetItem.transform.position-Vector3.up*0.1f;
		moveTo.onFixedUpdate += ValidateItemDistance;
		onFixedUpdate += ValidateItemDistance;

		//replace target anim with new play anim
		RemoveRequirement( waitForIdle );
		RemoveRequirement( playTargetAnim );
	}

	private void BeginBegLogic(){
		if( setBegLocomotion == null ){
			setBegLocomotion = new AutonomyPlayAnimation( self.autonomy, "proximity beg anim", GetBegLocomotionAnimation() );
			setBegLocomotion.onRegistered += delegate{ ValidateBegProximityAnimation( setBegLocomotion ); };
			setBegLocomotion.onCharacterTriggerEnter += HandleItemCollision;
			setBegLocomotion.onFixedUpdate += ValidateItemDistance;

			onFixedUpdate += delegate{ ValidateBegProximityAnimation( setBegLocomotion ); };

			if( lastRightBlendController != null ){
				setBegLocomotion.onRegistered += delegate{
					lastRightBlendController.Restore();
				};
			}
			if( lastLeftBlendController != null ){
				setBegLocomotion.onRegistered += delegate{
					lastLeftBlendController.Restore();
				};
			}
		}
		RemoveRequirement( playBegStartAnim );
		AddRequirement( setBegLocomotion );
	}
	
	private float OnBegIKControl( BlendController blendController ){
		if( blendController.targetHandState.occupied ){
			return 0;
		}
		Vector3 target = ClampInFrontSphere( begTargetPos-self.transform.right*0.05f*blendController.armIK.sign );
		
		float progress = self.GetLayerAnimNormTime(1);

		blendController.armIK.OverrideWorldRetargetingTransform(
			blendController.retargetingInfo,
			target+Vector3.up*0.1f, //pad a bit higher to properly reach item
			AutonomyPickup.CalculatePickupPole( blendController.armIK.sign, self ),
			null
		);
		return Mathf.Clamp01( sinPos( Time.time*2.3f )+sinPos( Time.time*1.3f )*0.5f-sinPos( Time.time*1.7f )*0.3f )*0.65f;
	}

	private Vector3 ClampInFrontSphere( Vector3 c ){
		Vector3 anchor = self.spine3RigidBody.transform.position+self.anchor.forward*0.4f;
		Vector3 diff = c-anchor;
		float d = diff.magnitude;
		d = Mathf.Max( d, 0.001f );
		return anchor+( diff/d )*Mathf.Min( d, 0.5f );
	}

	private float sinPos( float t ){
		return Mathf.Sin(t)*0.5f+0.5f;
	}
	private float cosPos( float t ){
		return Mathf.Cos(t)*0.5f+0.5f;
	}

	private Loli.Animation GetBegLocomotionAnimation(){
		
		float sqDist = Vector3.SqrMagnitude( self.floorPos-targetItem.transform.position );
		if( sqDist < 6.0f ){
			return Loli.Animation.STAND_HAPPY_BEG_LOCOMOTION;
		}else{
			return Loli.Animation.STAND_HAPPY_IDLE1;
		}
	}
	
	private void ValidateBegProximityAnimation( AutonomyPlayAnimation begAnim ){
		Loli.Animation overrideAnim = GetBegLocomotionAnimation();
		if( begAnim.entryAnimation != overrideAnim ){
			begAnim.Reset();
			begAnim.OverrideAnimations( overrideAnim );
		}
	}

	private void ValidateItemDistance(){
		if( targetItem == null ){
			FlagForFailure();
			return;
		}
		begTargetPos = Vector3.LerpUnclamped( begTargetPos, targetItem.transform.position, Time.deltaTime*2.5f );
		float sqDist = Vector3.SqrMagnitude( self.head.position-targetItem.transform.position );
		if( sqDist > 24.0f ){
			var failAnim = new AutonomyPlayAnimation( self.autonomy, "fail beg distance", Loli.Animation.STAND_HAPPY_DISAPPOINTMENT );
			self.autonomy.SetAutonomy( failAnim );
			self.active.SetTask( self.active.idle );
		}
	}

	public static float SetupReachIKControl( BlendController blendController, Vector3 targetPos, Loli self ){
		float pickupHeight = targetPos.y-self.floorPos.y;
		pickupHeight = Mathf.Clamp01( pickupHeight );

		self.animator.SetFloat( Instance.pickupHeightID, pickupHeight );
		self.animator.SetFloat( Instance.pickupReachID, 0.0f );
		float lerp = Mathf.Clamp01( ( 0.5f-Mathf.Abs( 0.5f-self.GetLayerAnimNormTime(1) ) )*2.0f );
		
		blendController.armIK.OverrideWorldRetargetingTransform(
			blendController.retargetingInfo,
			targetPos+Vector3.up*0.1f, //pad a bit higher to properly reach item
			CalculatePickupPole( blendController.armIK.sign, self ),
			null
		);
		return lerp;
	}
	
	public void HandleItemCollision( CharacterTriggerCallback ccc, Collider collider ){
		if( targetHandState == null || targetItem == null || maintainItem ){
			return;
		}
		Item item = Tools.SearchTransformAncestors<Item>( collider.transform );
		if( item == null ){
			return;
		}

		var pickupCollisionPart = targetHandState.rightSide ? CharacterTriggerCallback.Type.RIGHT_PALM : CharacterTriggerCallback.Type.LEFT_PALM;
		if( ccc.collisionPart == pickupCollisionPart ){
			if( item == targetItem ){
				targetHandState.GrabItemRigidBody( targetItem );
				//end previous requirements
				// RemoveRequirement( waitForIdle );
				// RemoveRequirement( moveTo );
				// RemoveRequirement( faceTarget );
				// RemoveRequirement( playTargetAnim );
				// if( begging ){
				// 	RemoveRequirement( setBegLocomotion );
				// 	RemoveRequirement( playBegStartAnim );
				// }
				viva.DevTools.LogExtended("begging successful, flagging for success", true, true);
				FlagForSuccess();
				playTargetAnim.FlagForSuccess();
				if( setBegLocomotion != null ){
					setBegLocomotion.FlagForSuccess();
				}
				maintainItem = true;
			}
		}
	}

	public override bool? Progress(){
		// apparently Progress() no longer in use ...
		if( targetHandState == null ){
			return false;
		}
		if( targetItem == null ){
			return false;
		}
		if( maintainItem ){
			return targetItem.mainOccupyState == targetHandState;
		}else{
			if( targetHandState.heldItem == targetItem ){
				return true;
			}else if( targetHandState.occupied ){
				return false;
			}
			if( !targetItem.CanBePickedUp( targetHandState ) ){
				return false;
			}
			return null;
		}
	}

	public static Vector3 CalculatePickupPole( float sign, Loli self ){
		return self.floorPos+self.transform.right*sign*0.4f-self.transform.forward*0.5f;
	}

	protected override Loli.Animation GetTargetAnimation(){
		if( targetHandState == null || targetItem == null ){
			return Loli.Animation.NONE;
		}
		if( targetItem.mainOwner != self){
			return self.bodyStateAnimationSets[(int)self.bodyState ].GetAnimationSet( AnimationSet.PICKUP_RIGHT_LEFT, System.Convert.ToInt32( !targetHandState.rightSide ) );
		}else{
			return self.bodyStateAnimationSets[(int)self.bodyState ].GetRandomAnimationSet( AnimationSet.SWAP );
		}
	}

	private Loli.Animation GetFirstValidAnimationSet( AnimationSet animSet ){
		var animation = Loli.Animation.NONE;
		if( overrideAnimationSets != null ){
			animation = overrideAnimationSets.GetRandomAnimationSet( animSet );
		}
		if( animation == Loli.Animation.NONE ){
			animation = self.GetAnimationFromSet( animSet );
		}
		return animation;
	}
}

}