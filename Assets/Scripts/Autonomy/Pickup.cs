using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


namespace viva{

public delegate Task HandleDropReturnFunc( Grabber grabber );

/// <summary>Performs pathfinding on the character to reach a destination and pickup an item. The character must be in an AnimationState that applies forward motion to move along the waypoints.</summary>
public partial class Pickup : Task {


	public Grabbable grabbable { get; private set; }
	public Grabber grabber { get; private set; }
	public MoveTo moveTo { get; private set; }
	private FaceTargetBody faceTargetItem;
	private Condition ensureItemExists;
	public PlayAnimation playPickup { get; private set; }
	public Vector3 nearestGrabPosition { get; private set; }
	public AttributeRequest attributeRequest { get; private set; }
	private Quaternion finalHandRotation;
	private IKHandle activeHandle;
	public bool? preferRight { get; private set; }
	private HandleDropReturnFunc ifNeedsToDrop;
	private PlayAnimation returnToIdle;

	/// <summary>
	/// Attempts to pickup the item with by a random grabbable. Logic includes moving to the item and the built in pickup animations.
	/// </summary>
	/// <param name="_autonomy">The Autonomy of the parent character. Exception will be thrown if it is null.</param>
	/// <param name="grabbable">The Rigidbody to pickup. Rigidbody must be specified or else an exception is thrown.</param>
	/// <param name="ifNeedsToDrop">If specified, the task will ask you for a drop task to handle making room for the target grabbable. Otherwise task will fail if preferred grabber is already occupied</param>
	/// <param name="preferRight">Do you prefer using the right hand (true)? Or left hand (false)? Or neither (null).</param>
	public Pickup( Autonomy _autonomy, Item item, HandleDropReturnFunc _ifNeedsToDrop=null, bool? _preferRight=null ):base(_autonomy){
		name = "pickup Item";
		ifNeedsToDrop = _ifNeedsToDrop;
		
		if( item == null ){
			Fail( "Item is null" );
			return;
		}
		//find grabbable already being grabbed, else use a random one
		Grabbable _grabbable;
		var context = self.biped.rightHandGrabber.IsGrabbing( item );
		if( context ){
			_grabbable = context.grabbable;
			_preferRight = true;
		}else{
			context = self.biped.leftHandGrabber.IsGrabbing( item );
			if( context ){
				_grabbable = context.grabbable;
				_preferRight = false;
			}else{
				_grabbable = item.GetRandomGrabbable();
			}
		}
		preferRight = _preferRight;
		InitVariables();

		if( _grabbable == null ){
			Fail( "Item "+item.name+" has no grabbables" );
			return;
		}
		if( _grabbable.rigidBody == null ){
			Fail( "Grabbable "+_grabbable.name+" does not have a RigidBody" );
			return;
		}
		SetupVariablesForGrabbable( _grabbable, true );
	}

	/// <summary>
	/// Attempts to pickup the specified grabbable. Logic includes moving to the item and the built in pickup animations.
	/// </summary>
	/// <param name="_autonomy">The Autonomy of the parent character. Exception will be thrown if it is null.</param>
	/// <param name="grabbable">The Rigidbody to pickup. Rigidbody must be specified or else an exception is thrown.</param>
	/// <param name="ifNeedsToDrop">If specified, the task will ask you for a drop task to handle making room for the target grabbable. Otherwise task will fail if preferred grabber is already occupied</param>
	/// <param name="preferRight">Do you prefer using the right hand (true)? Or left hand (false)? Or neither (null).</param>
	public Pickup( Autonomy _autonomy, Grabbable _grabbable, HandleDropReturnFunc _ifNeedsToDrop=null, bool? _preferRight=null ):base(_autonomy){
		name = "pickup Grabbable";
		preferRight = _preferRight;
		ifNeedsToDrop = _ifNeedsToDrop;
		InitVariables();
		
		if( _grabbable == null ){
			Fail( "Grabbable is null" );
			return;
		}
		if( _grabbable.rigidBody == null ){
			Fail( "Grabbable "+_grabbable.name+" does not have a RigidBody" );
			return;
		}
		SetupVariablesForGrabbable( _grabbable, true );
	}

	/// <summary>
	/// Attempts to find and pickup any item with the specified attribute. Logic includes moving to the item and the built in pickup animations.
	/// </summary>
	/// <param name="_autonomy">The Autonomy of the parent character. Exception will be thrown if it is null.</param>
	/// <param name="attribute">The attribute of the item. These are found in the .item of all items.</param>
	/// <param name="ifNeedsToDrop">If specified, the task will ask you for a drop task to handle making room for the target grabbable. Otherwise task will fail if preferred grabber is already occupied</param>
	/// <param name="preferRight">Do you prefer using the right hand (true)? Or left hand (false)? Or neither (null).</param>
	public Pickup( Autonomy _autonomy, AttributeRequest _attributeRequest, HandleDropReturnFunc _ifNeedsToDrop=null, bool? _preferRight=null ):base(_autonomy){
		name = "pickup with attributes \""+_attributeRequest.ToString();

		preferRight = _preferRight;
		attributeRequest = _attributeRequest;
		ifNeedsToDrop = _ifNeedsToDrop;
		onRegistered += delegate{
			if( !finished && !CheckIfAlreadyPickedUpAttribute() ){
				ChangeVariablesForGrabbable( null );
			}
		};
		
		SetupTaskForGrabbableSeen( this );
		InitVariables();

		CheckIfAlreadyPickedUpAttribute();
	}

	private void InitVariables(){
		onAutonomyExit += delegate{
			if( grabbable ){
				grabbable.onGrabbed._InternalRemoveListener( CheckIfGrabbedBySelf );
			}
		};
		moveTo = new MoveTo( self.autonomy, self.model.bipedProfile.armLength*self.scale*1.8f );
	}
	
	private void ReturnToIdleAndSucceed(){
		if( returnToIdle != null ) return;
		Succeed();
		// returnToIdle = new PlayAnimation( autonomy, null, "idle", false, 0 );
		// AddRequirement( returnToIdle );
		// returnToIdle.onSuccess += Succeed;
	}

	private bool CheckIfAlreadyPickedUpAttribute(){
		var rightHandGrabbableContext = self.biped.rightHandGrabber.IsGrabbing( attributeRequest );
		if( rightHandGrabbableContext ){
			grabbable = rightHandGrabbableContext.grabbable;
			grabber = rightHandGrabbableContext.grabber;
			ReturnToIdleAndSucceed();	//already grabbing it, exit logic
			return true;
		}
		var leftHandGrabbableContext = self.biped.leftHandGrabber.IsGrabbing( attributeRequest );
		if( leftHandGrabbableContext ){
			grabbable = leftHandGrabbableContext.grabbable;
			grabber = leftHandGrabbableContext.grabber;
			ReturnToIdleAndSucceed();	//already grabbing it, exit logic
			return true;
		}
		return false;
	}

	private void SetupTaskForGrabbableSeen( Task task ){
		task.onRegistered += delegate{
			self.biped.vision.onItemSeen._InternalAddListener( CheckItemAttribute );
		};

		task.onUnregistered += delegate{
			self.biped.vision.onItemSeen._InternalRemoveListener( CheckItemAttribute );
		};
	}

	private void CheckItemAttribute( Item newItem ){
		var newGrabbable = newItem.GetRandomGrabbable();
		if( !newGrabbable ) return;
		if( newItem.HasAttributes( attributeRequest ) ){
			if( grabbable != null ){
				//check if it's closer than the last one
				float currentSqDist = grabbable.GetNearbyDistance( grabber.rigidBody.worldCenterOfMass );
				float newSqDist = newGrabbable.GetNearbyDistance( grabber.rigidBody.worldCenterOfMass );

				if( newSqDist < currentSqDist ){
					ChangeVariablesForGrabbable( newGrabbable );
				}
			}else{
				SetupVariablesForGrabbable( newGrabbable, false );
			}
		}
	}

	private void SetupVariablesForGrabbable( Grabbable newGrabbable, bool allowConvertToWhileHolding ){
		if( grabbable != null ) return;	//already found it
		grabbable = newGrabbable;
		if( grabber == null ) grabber = GetTargetGrabber();

		if( grabbable.parentItem ) self.biped.lookTarget.SetTargetRigidBody( grabbable.parentItem.rigidBody );

		//ensure item exists is most important and at the front
		ensureItemExists = new Condition( self.autonomy, delegate{ return grabbable!=null; } );
		ensureItemExists.name = "ensure "+grabbable.name+" exists";

		AddRequirement( ensureItemExists );

		Task dropTask = null;
		if( grabber.grabbing ){
			if( grabber.IsGrabbing( grabbable ) ){	//already grabbing
				if( allowConvertToWhileHolding ){
					onFixedUpdate += delegate{
						if( OnRequirementValidate() ){
							ReturnToIdleAndSucceed();
						}else if( grabbable ){
							Fail("Grabbable "+grabbable.name+" was dropped after holding");
						}else{
							Fail("Grabbable was destroyed while holding");
						}
					};
				}else{
					ReturnToIdleAndSucceed();	//already grabbing it, exit logic
				}
				return;
			}else{
				try{
					dropTask = ifNeedsToDrop?.Invoke( grabber );	//prevent null if script with callback goes null
				}catch{
					dropTask = null;
				}
				if( dropTask == null ){
					Fail("Grabber needs to drop and has no ifNeedsToDrop");
					return;
				}
			}
		}

		BuildRequirements( dropTask );
		ChangeVariablesForGrabbable( newGrabbable );
	}

	private void BuildRequirements( Task dropTask ){
		playPickup = new PlayAnimation( self.autonomy, null, "pickup "+( grabber == self.biped.rightHandGrabber ? "right" : "left" ) );
		playPickup.name = "pickup play anim";

		moveTo.pathRequest = GeneratePickupPathRequest; 
		moveTo.name = "go to item "+grabbable.name;
		moveTo.useToClosestPoint = true;
		moveTo.onFixedUpdate += UpdateGetNearestGrabPosition;
		SetupTaskForGrabbableSeen( moveTo );

		faceTargetItem = new FaceTargetBody( self.autonomy, 1, 10 );
		faceTargetItem.name = "pickup face "+grabbable.name;
		
		//build logic chain
		playPickup.AddRequirement( moveTo );
		playPickup.AddPassive( faceTargetItem );

		AddRequirement( playPickup );
		_internalReqInsertOffset = 2;
		if( dropTask != null ) AddRequirement( dropTask );

		var failures = 0;
		//ensure is playing animation before entering Pickup logic
		playPickup.onExitAnimation += delegate{
			if( !succeeded ){
				if( failures++ > 3 ) Fail("Could not pickup. Failure attempts out of range");
			}
			activeHandle?.Kill();
		};
		playPickup.onEnterAnimation += InitializeArmIK;
		UpdateGetNearestGrabPosition();

		moveTo.onRegistered += delegate{
			grabber.onGrabbableNearby._InternalAddListener( OnPrimaryNearby );
		};

		moveTo.onUnregistered += delegate{
			grabber.onGrabbableNearby._InternalRemoveListener( OnPrimaryNearby );
		};

		playPickup.onRegistered += delegate{
			grabber.onGrabbableNearby._InternalAddListener( OnPrimaryNearby );
			if( grabbable.parentItem ) self.biped.lookTarget.SetTargetRigidBody( grabbable.parentItem.rigidBody );
		};

		playPickup.onUnregistered += delegate{
			grabber.onGrabbableNearby._InternalRemoveListener( OnPrimaryNearby );
			activeHandle?.Kill();
			UpdateGetNearestGrabPosition();
		};
	}

	private void ChangeVariablesForGrabbable( Grabbable newGrabbable ){
		if( grabbable ) grabbable.onGrabbed._InternalRemoveListener( CheckIfGrabbedBySelf );
		
		grabbable = newGrabbable;

		if( grabbable ){
			grabbable.onGrabbed._InternalAddListener( CheckIfGrabbedBySelf );
			faceTargetItem.target.SetTargetRigidBody( grabbable.rigidBody );
		}
	}

	private Nav.PathRequest[] GeneratePickupPathRequest( Vector3 targetPos ){
		if( grabbable == null ) return null;
		float? additionalRadius = grabbable.parentItem?grabbable.parentItem.CalculateApproximateStandingRadius() : 0;
		if( !additionalRadius.HasValue ) return null;
		return new Nav.PathRequest[]{
			new Nav.SearchCircle( targetPos, MoveTo.minNodeDist, self.model.bipedProfile.floorToHeadHeight*self.scale+1f, 8, MoveTo.minNodeDist+additionalRadius.Value+grabber.width )
		};
	}

	private void UpdateGetNearestGrabPosition(){
		if( !grabbable ){
			Fail("Grabbable was destroyed");
			return;
		}
		if( !grabbable.GetBestGrabPose( out Vector3 grabPos, out Quaternion grabRot, grabber ) ) return;
		nearestGrabPosition = grabPos;
		finalHandRotation = grabRot;
		moveTo.target.SetTargetPosition( nearestGrabPosition );
	}

	private void CheckIfGrabbedBySelf( GrabContext grabContext ){
		if( grabContext.grabber.character == self ){
			ReturnToIdleAndSucceed();
		}else{
			Fail("Grabbed by someone else");
		}
		if( playPickup.hasAnimationControl && playPickup.GetPlayedNormalizedTime() < 0.125f ){
			playPickup.ForceSkipToNextState();
		}
		RemoveRequirement( ensureItemExists );
		RemoveRequirement( playPickup );
	}

	private void OnPrimaryNearby( Grabbable _grabbable ){
		if( grabbable && _grabbable == grabbable ) grabber.Grab( grabbable );
	}

	private Grabber GetTargetGrabber(){
		if( preferRight.HasValue ){
			if( preferRight.Value ){
				return self.biped.rightHandGrabber;
			}else{
				return self.biped.leftHandGrabber;
			}
		}

		List<Grabber> grabbers = new List<Grabber>(2);
		if( !self.biped.rightHandGrabber.grabbing ) grabbers.Add( self.biped.rightHandGrabber );
		if( !self.biped.leftHandGrabber.grabbing ) grabbers.Add( self.biped.leftHandGrabber );
		if( grabbers.Count == 0 ){
			if( Random.value > 0.5 ) return self.biped.rightHandGrabber;
			else return self.biped.leftHandGrabber;
		}
		return grabbers[ Random.Range( 0,grabbers.Count ) ];
	}

	private void InitializeArmIK(){
		if( !grabbable || succeeded ) return;
		var armIK = grabber == self.biped.rightHandGrabber ? self.biped.rightArmIK : self.biped.leftArmIK;
		grabber.SetGrip( 0.0f );

		if( !self.ragdoll.surface.HasValue ){
			return;
		}

		var grabDeltaRatio = ( nearestGrabPosition.y-self.ragdoll.surface.Value.y )/( (self.model.bipedProfile.floorToHeadHeight+self.model.bipedProfile.hipHeight)*0.5f*self.scale );
		
		var heightWeight = new WeightManager1D(
			new Weight[]{
				self.GetWeight("pickup_height_chest"),
				self.GetWeight("pickup_height_floor")
			},
			new float[]{
				0, 1
			}
		);
		heightWeight.SetPosition( 1.0f-grabDeltaRatio );

		float minRemap = 0.35f-0.2f*heightWeight.position;

		armIK.AddRetargeting( delegate( out Vector3 target, out Vector3 pole, out Quaternion handRotation ){
			activeHandle.maxWeight = Tools.RemapClamped( minRemap, 0.55f, 0.0f, 1.0f, playPickup.GetPlayedNormalizedTime() );
			UpdateGetNearestGrabPosition();
			target = nearestGrabPosition;
			pole = self.model.armature.TransformPoint( ( Vector3.right*grabber.sign )/self.scale );
			handRotation = finalHandRotation;

        }, out activeHandle );
	}

    public override bool OnRequirementValidate(){
		if( grabbable == null ) return false;
		return grabber.IsGrabbing( grabbable );
    }

	public static Task DropCurrentIfNecessary( Grabber grabber ){
		return new Drop( grabber.character.autonomy, grabber.GetRandomGrabbable() );
	}
}

}