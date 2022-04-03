using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


namespace viva{


/// <summary>Performs pathfinding on the character to drop an item at a destination. The character must be in an AnimationState that applies forward motion to move along the waypoints.</summary>
public partial class Drop : Task {


	public Grabbable grabbable { get; private set; }
	public Grabber grabber { get; private set; }
	public MoveTo moveTo { get; private set; }
	public PlayAnimation playDrop { get; private set; }
	public FaceTargetBody faceTargetDropOffZone { get; private set; }
	public Zone dropOffZone { get; private set; }
	private Quaternion finalHandRotation;
	private IKHandle activeHandle;
	private float? approximateRadius;
	private Task waitForDropZone;

	/// <summary>
	/// Attempts to drop the specified item at the specified location. Logic includes moving to the spot and the built in pickup animations. Task will fail if task starts without holding the item.
	/// </summary>
	/// <param name="_autonomy">The Autonomy of the parent character. Exception will be thrown if it is null.</param>
	/// <param name="_grabbable">The Rigidbody to pickup. Rigidbody must be specified or else an exception is thrown.</param>
	/// <param name="dropOffZone">The optional dropZone location to place the grabbable</param>
	public Drop( Autonomy _autonomy, Grabbable _grabbable, Zone _dropOffZone=null ):base(_autonomy){
		name = "drop";
		dropOffZone = _dropOffZone;
		
		if( dropOffZone == null ){
			Fail( "DropZone is null" );
			return;
		}
		if( _grabbable == null ){
			Fail( "Grabbable is null" );
			return;
		}
		name = "drop "+_grabbable.name;

		if( _grabbable.rigidBody == null ){
			Fail( "Grabbable's rigidBody is null" );
			return;
		}
		var contexts = _grabbable.GetGrabContextsByCharacter( self );
		if( contexts.Count != 1 ){
			Fail( "Grabbable must be held by 1 grab context" );
			return;
		}
		grabber = contexts.Count > 0 ? contexts[0].grabber : null;
		if( grabber == null ){
			Fail( "Grabbable must start being held by the character" );
			return;
		}

		grabbable = _grabbable;
		approximateRadius = grabbable.parentItem ? grabbable.parentItem.CalculateApproximateStandingRadius() : grabber.width;
		if( !approximateRadius.HasValue ){
			Fail( "Grabbable missing model to approximate standing radius" );
			return;
		}

		playDrop = new PlayAnimation( self.autonomy, null, "pickup "+( grabber == self.biped.rightHandGrabber ? "right" : "left" ), true );
		playDrop.name = "drop play anim";
		moveTo = new MoveTo( self.autonomy, self.model.bipedProfile.armLength*self.scale*1.15f );
		moveTo.pathRequest = GeneratePickupPathRequest;
		moveTo.name = "go to target "+_grabbable.name+" drop off zone";
		moveTo.useToClosestPoint = true;

		playDrop.AddRequirement( new Condition( self.autonomy, delegate{ return grabbable!=null; } ) );
		playDrop.AddRequirement( moveTo );
		
		AddRequirement( new Condition( self.autonomy, delegate{ return grabbable!=null; } ) );
		AddRequirement( GenerateWaitForDropZoneTask() );
		AddRequirement( playDrop );
		_internalReqInsertOffset = 3;

		playDrop.onExitAnimation += delegate{
			if( !succeeded ){
				playDrop.Reset();	//play animation again till it wins
			}
			activeHandle?.Kill();
		};
		playDrop.onEnterAnimation += InitializeArmIK;

		faceTargetDropOffZone = new FaceTargetBody( _autonomy, 1, 10 );
		faceTargetDropOffZone.name = "pickup face target";
		playDrop.AddPassive( faceTargetDropOffZone );

		onUnregistered += delegate{
			activeHandle?.Kill();
		};

		onSuccess += delegate{
			activeHandle?.Kill();
		};
	}

	private Task GenerateWaitForDropZoneTask(){
		waitForDropZone = new Task( autonomy );
		waitForDropZone.name = "wait for drop zone";
		waitForDropZone.onFixedUpdate += delegate{
			if( !dropOffZone ){
				Fail("Drop Zone was destroyed");
				return;
			}
			if( dropOffZone._InternalFindEmptySpot( approximateRadius.Value, OnDropZonePositionFound ) ){
			}
		};
		return waitForDropZone;
	}

	private void OnDropZonePositionFound( Vector3? position ){
		if( !position.HasValue ){
			Fail("Could not find an empty spot on the DropZone");
			return;
		}
		waitForDropZone.Succeed();

		moveTo.target.SetTargetPosition( position.Value+Vector3.up*0.1f );
		faceTargetDropOffZone.target.SetTargetPosition( position.Value );
	}

	private Nav.PathRequest[] GeneratePickupPathRequest( Vector3 targetPos ){
		if( grabbable == null ) return null;
		float? additionalRadius = grabbable.parentItem?grabbable.parentItem.CalculateApproximateRadius() : 0;
		if( !additionalRadius.HasValue ) return null;
		return new Nav.PathRequest[]{
			new Nav.SearchCircle( targetPos, MoveTo.minNodeDist+additionalRadius.Value, self.model.bipedProfile.floorToHeadHeight*self.scale, 8, self.model.bipedProfile.armLength*self.scale+MoveTo.minNodeDist )
		};
	}

	private void InitializeArmIK(){
		if( !grabbable || succeeded ) return;
		var contexts = grabbable.GetGrabContextsByCharacter( self );
		if( contexts.Count != 1 ){
			Fail( "Grabbable must be held by 1 grab context" );
			return;
		}
		var grabber = contexts[0].grabber;
		if( grabber == null ){
			Fail("Grabbable was dropped");
			return;
		}
		if( !self.ragdoll.surface.HasValue ){
			Fail("Character is not on the floor");
			return;
		}
		var targetDropOffZoneRead = moveTo.target.Read();
		if( !targetDropOffZoneRead.HasValue ){
			Fail("Could not find an empty spot on the DropZone");
			return;
		}

		var grabDeltaRatio = ( targetDropOffZoneRead.Value.y-self.ragdoll.surface.Value.y )/( self.model.bipedProfile.floorToHeadHeight*0.5f*self.scale );
		
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
		var armIK = grabber == self.biped.rightHandGrabber ? self.biped.rightArmIK : self.biped.leftArmIK;

		armIK.AddRetargeting( delegate( out Vector3 target, out Vector3 pole, out Quaternion handRotation ){
			activeHandle.maxWeight = Tools.RemapClamped( 0.4f, 0.7f, 0.0f, 1.0f, playDrop.GetPlayedNormalizedTime() );
			target = targetDropOffZoneRead.Value;
			pole = self.model.armature.TransformPoint( ( Vector3.right*grabber.sign )/self.scale );

			Tools.DrawCross( target, Color.yellow, 0.4f, Time.fixedDeltaTime );

			handRotation = armIK.hand.rotation;

			if( !finished && activeHandle.maxWeight >= 1.0f ){
				grabber.ReleaseAll();
				Succeed();
				RemoveAllPassivesAndRequirements();
			}
        }, out activeHandle );
	}

	/// <summary>Ensures the character is still considered at the destination.</summary>
	/// <returns>true: Still at the destination. false: moved away from destination.</returns>
    public override bool OnRequirementValidate(){
		return true;
    }
}

}