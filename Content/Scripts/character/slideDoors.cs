using System.Collections;
using UnityEngine;
using viva;


public class SlideDoors: VivaScript{

	private readonly Character character;


	public SlideDoors( Character _character ){
		character = _character;

		character.autonomy.onTaskRegistered.AddListener( this, ListenForMoveToTask );

		SetupAnimations();
	}

	private void SetupAnimations(){

		var stand = character.animationSet.GetBodySet("stand");

		var slideDoorRight = stand.Single( "slide door right", "stand_slide_door_right", true, 0.8f );
		slideDoorRight.nextState = stand["idle"];

		var slideDoorLeft = stand.Single( "slide door left", "stand_slide_door_left", true, 0.8f );
		slideDoorLeft.nextState = stand["idle"];
	}

	private void ListenForMoveToTask( Task task ){
		var moveTo = task as MoveTo;
		if( moveTo == null ) return;

		moveTo.onItemBlockingPath.AddListener( this, ListenForSlidingDoor );
	}

	private bool ListenForSlidingDoor( Item item ){
		if( character.autonomy.FindTask("walk through door") != null ) return false;

		if( item.HasAttribute("sliding_door") ){
			SlideOpenDoor( item );
			return true;
		}
		return false;
	}

	private void SlideOpenDoor( Item item ){
		var bounds = item.model.meshFilter.sharedMesh.bounds;
        var halfExtents = bounds.size*0.5f;
        halfExtents.x *= item.model.rootTransform.lossyScale.x;
        halfExtents.y *= item.model.rootTransform.lossyScale.y;
        halfExtents.z *= item.model.rootTransform.lossyScale.z;

		//calculate slot door starts at
        var slidePos = (Vector3)item.customVariables.Get( this, "slide+" ).value;
        var slideNeg = (Vector3)item.customVariables.Get( this, "slide-" ).value;
		var slots = Mathf.RoundToInt( Vector3.Distance( slidePos, slideNeg )/( halfExtents.x*2 ) );

		var flatToItem = character.biped.hips.rigidBody.worldCenterOfMass-item.transform.position;
		var flatForward = item.transform.forward;
		var charSide = Vector3.Dot( flatForward, flatToItem ) >= 0 ? 1 : -1;

		var charPos = character.biped.hips.rigidBody.worldCenterOfMass;
		float charSlotRatio = Tools.PointOnRayRatio( slidePos, slideNeg, charPos );
		var charSlot = Mathf.RoundToInt( Mathf.Clamp01( charSlotRatio )*slots );
		
		float doorSlotRatio = Mathf.Clamp01( Tools.PointOnRayRatio( slidePos, slideNeg, item.model.rootTransform.TransformPoint( bounds.center ) ) );
		bool slideRight;
		if( charSlot == 0 ){
			slideRight = true;
		}else if( charSlot == slots ){
			slideRight = false;
		}else{
			var doorSlot = doorSlotRatio*slots;
			slideRight = doorSlot > charSlot;
		}
		if( charSide < 0 ) slideRight = !slideRight;
		
		var targetOpeningPos = slidePos+( slideNeg-slidePos )*( (float)charSlot/slots );
		var armLength = charSide*character.scale*character.model.bipedProfile.armLength;

		var occupyVariable = item.customVariables.Get( this, "slideOccupied" );
		var waitForUse = new Task( character.autonomy );
		waitForUse.onFixedUpdate += delegate(){
            var occupant = occupyVariable.value as Character;
			if( occupant == null ){
				occupyVariable.value = character;
				waitForUse.Succeed();
			}
        };
		var waitAtADistance = new MoveTo( character.autonomy, 0f );

		float charWaitDist = 4+Random.value*5;
		waitAtADistance.target.SetTargetPosition( Vector3.LerpUnclamped( slidePos, slideNeg, charSlotRatio )+item.transform.forward*armLength*charWaitDist );

		// waitForUse.AddPassive( waitAtADistance );

		var waitFacing = new FaceTargetBody( character.autonomy, 1, 25 );
		waitFacing.target.SetTargetPosition( targetOpeningPos );

		waitAtADistance.onSuccess += delegate{
			waitAtADistance.AddPassive( waitFacing );
		};

		waitForUse.Start( this, "wait for sliding door not occupied");

		waitForUse.onSuccess += delegate{

			bool doorAlreadyOpen = IsDoorOpen( item, bounds, targetOpeningPos );

			var faceOpening = new FaceTargetBody( character.autonomy, 1, 25, 0.1f );
			faceOpening.target.SetTargetPosition( targetOpeningPos+item.transform.forward*armLength*-1000 );
			
			faceOpening.onAutonomyExit += delegate{
				if( occupyVariable.value as Character == character ){
					occupyVariable.value = null;
				}
			};

			var moveToOpening = new MoveTo( character.autonomy, 0f );
			moveToOpening.name = "move to opening";
			moveToOpening.target.SetTargetPosition( targetOpeningPos+item.transform.forward*armLength*( doorAlreadyOpen ? 1 : 0.7f ) );

			faceOpening.AddRequirement( moveToOpening );

			if( doorAlreadyOpen ){
				faceOpening.Start( this, "walk through door" );
				return;
			}

			var playSlideAnimation = new PlayAnimation( character.autonomy, null, "slide door "+(slideRight?"right":"left"), true, -1 );
			playSlideAnimation.onEnterAnimation += delegate{
				character.biped.rightHand.SetPhysicMaterial( BuiltInAssetManager.main.ragdollStickyHandPhysicMaterial );
				character.biped.leftHand.SetPhysicMaterial( BuiltInAssetManager.main.ragdollStickyHandPhysicMaterial );
			};
			playSlideAnimation.onExitAnimation += delegate{
				character.biped.rightHand.SetPhysicMaterial( BuiltInAssetManager.main.ragdollHandPhysicMaterial );
				character.biped.leftHand.SetPhysicMaterial( BuiltInAssetManager.main.ragdollHandPhysicMaterial );
			};
			playSlideAnimation.onFixedUpdate += delegate{
				if( IsDoorOpen( item, bounds, targetOpeningPos ) ){
					playSlideAnimation.Succeed();
				}
			};

			playSlideAnimation.AddRequirement( faceOpening );

			playSlideAnimation.Start( this, "walk through door" );
		};
	}

	private bool IsDoorOpen( Item item, Bounds bounds, Vector3 targetOpeningPos ){
        var halfExtents = bounds.size*0.5f;
        halfExtents.x *= item.model.rootTransform.lossyScale.x;
		var currentPos = item.model.rootTransform.TransformPoint( bounds.center );
		var currentDist = Vector3.Distance( targetOpeningPos, currentPos );
		float shoulderWidth = character.model.bipedProfile.shoulderWidth*character.scale+0.05f;	//0.05 of padding
		return currentDist-halfExtents.x > shoulderWidth;
	}
}
