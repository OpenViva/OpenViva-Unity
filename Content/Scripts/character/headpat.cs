using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using viva;


public class Headpat: VivaScript{

	private readonly Character character;
	private Task headpatTask = null;
	private ArmIK activeArmIK = null;
	private IKHandle activeHandle = null;
	private SpineIK spineIK = null;
	private float roughHeadpatDuration = 0;
	private float lastRoughHeadpatTime = 0;
	private Vector3 lastLocalHandTouchPos;
	private float headpatStartTime = 0;
	private float happyNoiseTimer = 0;

	private WeightManager1D headpatAnimController;

	public Headpat( Character _character ){
		character = _character;

		SetupAnimations();

        character.biped.onCollisionEnter.AddListener( this, ListenForHeadpatTouch );

        Achievement.Add( "Headpat a character", "Smoothly pat the top of her head" );
	}

	private void ListenForHeadpatTouch( BipedBone ragdollBone, Collision collision ){
		if( character.autonomy.HasTag("disable headpat") ) return;
		var armIK = GetHeadpatArmFromCollision( ragdollBone, collision );
		if( armIK != null && Time.time-lastRoughHeadpatTime > 1.0f ){
			BeginHeadpat( armIK, collision.GetContact(0).point );
		}
	}

	private ArmIK GetHeadpatArmFromCollision( BipedBone ragdollBone, Collision collision ){
		if( collision.contactCount == 0 ) return null;
		
		//collision must be above forehead point so it's a headpat
		var contact = collision.GetContact(0);
		var localHitPos = character.biped.head.rigidBody.transform.InverseTransformPoint( contact.point );
		if( localHitPos.y < character.model.bipedProfile.localHeadForeheadY*character.scale ) return null;

		if( ragdollBone == BipedBone.HEAD ){
			var sourceCharacter = Util.GetCharacter( collision.rigidbody );
			if( sourceCharacter == character || sourceCharacter == null || !sourceCharacter.isPossessed ) return null;
			if( sourceCharacter.biped.rightHand.rigidBody == collision.rigidbody ){
				if( sourceCharacter.biped.rightHandGrabber.grabbing ) return null;
				if( Time.time-sourceCharacter.biped.rightHandGrabber.timeSinceLastRelease < 1f ) return null;
				return sourceCharacter.biped.rightArmIK;
			}else if( sourceCharacter.biped.leftHand.rigidBody == collision.rigidbody ){
				if( sourceCharacter.biped.leftHandGrabber.grabbing ) return null;
				if( Time.time-sourceCharacter.biped.leftHandGrabber.timeSinceLastRelease < 1f ) return null;
				return sourceCharacter.biped.leftArmIK;
			}
		}
		return null;
	}
	
	private void ListenForHeadpatTouchDistanceEnd(){
		if( activeArmIK == null ) return;
		if( Time.time-headpatStartTime < 0.75f ) return;
		var lastTouchWorldPos = activeArmIK.hand.TransformPoint( lastLocalHandTouchPos );
		Vector3 closestPoint = character.biped.head.GetClosestColliderPoint( lastTouchWorldPos );
		if( Vector3.SqrMagnitude( lastTouchWorldPos-closestPoint ) > 0.0025f ){
			EndHeadpat();
		}
	}

	private void EndHeadpat(){
		if( activeArmIK == null ) return;

		//stop the arm from grabbing the head
		character.biped.head.IterateGrabContexts( delegate( GrabContext context ){
			if( context.source == activeArmIK.character ){
				Viva.Destroy( context );
			}
		});
		SetIgnoreArmFromPokingFace( false );
		activeHandle?.Kill();
		activeArmIK = null;
		spineIK?.Kill();
		character.biped.headLookAt.strength.Animate( "headpats", 0, 1, 0.65f );

		character.autonomy.onFixedUpdate.RemoveListener( this, ListenForHeadpatTouchDistanceEnd );
		character.autonomy.onFixedUpdate.RemoveListener( this, CheckRoughHeadpats );

		character.autonomy.RemoveTask( headpatTask );
		headpatTask = null;

		var returnToIdle = new PlayAnimation( character.autonomy, null, "headpat wanted more", true, 0 );
		returnToIdle.Start( this, "return from headpats" );
	}

	private void UpdateHappyNoiseTimer(){
		happyNoiseTimer -= Time.deltaTime;
		if( happyNoiseTimer < 0 ){
			happyNoiseTimer = 2+Random.value*3;
			character.PlayVoiceGroup("happy");
		}
	}

	private void CheckRoughHeadpats(){
		if( activeArmIK == null ) return;
		// slowly ease into new velocity force
		float position = headpatAnimController.position;
		float sqVel = activeArmIK.rigidBody.velocity.sqrMagnitude;
		float newTarget;
		if( sqVel < 0.005f ){
			newTarget = 0.0f;	//none
		}else if( sqVel < 0.25f ){
			newTarget = 1.0f;	//proper
			UpdateHappyNoiseTimer();
		}else{
			newTarget = 2.0f;	//annoyed
		}
		position += ( newTarget-position )*Time.deltaTime*(newTarget+2);
		headpatAnimController.SetPosition( position );

		//increase/decrease duration of rough headpat based on newTarget
		roughHeadpatDuration = Mathf.Max( 0, roughHeadpatDuration+(newTarget-1)*Time.deltaTime );
		//end headpat as rough if headpat was rough for 0.5 seconds
		if( roughHeadpatDuration > 0.75f ){
			
			lastRoughHeadpatTime = Time.time;
			if( headpatTask.registered ){
				
				var playHeadpatAnim = new PlayAnimation( character.autonomy, null, "brushAway"+Random.Range(1,3), true, 0, false );
				playHeadpatAnim.Start( this, "angry headpats" );
			};
			EndHeadpat();
		}
	}

	private void UpdateLocalHandContactPos( Vector3 worldHeadContactPos ){
		lastLocalHandTouchPos = activeArmIK.hand.InverseTransformPoint( worldHeadContactPos );
	}

	private void SetIgnoreArmFromPokingFace( bool ignore ){
		if( activeArmIK.rightSide ){
			character.scriptManager.CallOnScript("poke","SetIgnoreCollider", new object[]{
				activeArmIK.character.biped.rightHand.colliders[0],
				ignore } );
			character.scriptManager.CallOnScript("poke","SetIgnoreCollider", new object[]{
				activeArmIK.character.biped.rightArm.colliders[0],
				ignore } );
		}else{
			character.scriptManager.CallOnScript("poke","SetIgnoreCollider", new object[]{
				activeArmIK.character.biped.leftHand.colliders[0],
				ignore } );
			character.scriptManager.CallOnScript("poke","SetIgnoreCollider", new object[]{
				activeArmIK.character.biped.leftArm.colliders[0],
				ignore } );
		}
	}

	private void BeginHeadpat( ArmIK armIK, Vector3 worldHeadContactPos ){
		if( activeArmIK != null ){
			if( activeArmIK == armIK ) UpdateLocalHandContactPos( worldHeadContactPos );
			return;
		}
		EndHeadpat();
		//setup variables
		activeArmIK = armIK;
		roughHeadpatDuration = 0;
		headpatStartTime = Time.time;
		UpdateLocalHandContactPos( worldHeadContactPos );
		
		character.biped.headLookAt.strength.Animate( "headpats", 1, 0, 0.65f );
		SetIgnoreArmFromPokingFace( true );

		AchievementManager.main.CompleteAchievement( "Headpat a character", true );

		character.autonomy.onFixedUpdate.AddListener( this, ListenForHeadpatTouchDistanceEnd );
		character.autonomy.onFixedUpdate.AddListener( this, CheckRoughHeadpats );

		headpatTask = new Task( character.autonomy );
		
		var playHeadpatAnim = new PlayAnimation( character.autonomy, null, "headpat start" );
		headpatTask.AddPassive( playHeadpatAnim );
		headpatTask.Start( this, "headpats" );
		playHeadpatAnim.onEnterAnimation += playHeadpatAnim.Succeed;

		//animate the hand headpatting
		if( armIK.character.isPossessed && armIK.character.possessor.isUsingKeyboard ){
			activeArmIK.AddRetargeting( delegate( out Vector3 target, out Vector3 pole, out Quaternion headRotation ){
				var headPos = character.biped.head.rigidBody.worldCenterOfMass;
				var headRadius = character.biped.CalculateMuscleBoundingSphere( character.biped.head );
				var handPos = armIK.rigidBody.position;
				var oldHandY = armIK.hand.transform.position.y;
				target = headPos+(handPos-headPos).normalized*headRadius;
				pole = headPos+Vector3.down;

				target.y = oldHandY;
				
				headRotation = Quaternion.LookRotation( handPos-headPos, Vector3.up )*Quaternion.Euler( 0.0f, 90.0f*armIK.sign, -30.0f*armIK.sign );
			}, out activeHandle );
			activeHandle.maxWeight = 0.8f;
		}

		//animate the head being dragged by the hand
        spineIK = SpineIK.CreateSpineIK( character.model.bipedProfile );
		spineIK.strength.Add( "headpats", 0.6f );
        // spineIK.AddRetargeting( delegate( out Vector3 target, out Vector3 pole, out Quaternion headRotation ){
        //     target = activeArmIK.hand.transform.position;
		// 	pole = character.biped.hips.target.position-character.biped.hips.target.forward;
		// 	var head = character.biped.head.target;
		// 	// headRotation = head.transform.rotation;
        //     headRotation = Quaternion.LookRotation( head.transform.forward, activeArmIK.hand.TransformPoint( lastLocalHandTouchPos )-head.position );
        // }, out IKHandle handle );
        character.AddIK( spineIK );
	}

	private void SetupAnimations(){
		//Create weight controller to drive blending of animations along a 1D line   0 ~ 1 ~ 2  = annoyed ~ idle ~ proper
		headpatAnimController = new WeightManager1D(
            new Weight[]{
                character.GetWeight("idle"),
                character.GetWeight("proper"),
                character.GetWeight("annoyed")
            }, new float[]{ 0.0f, 1.0f, 2.0f }
        );

		//setup headpat animations int the stand BodySet
		var stand = character.animationSet.GetBodySet("stand");
		var standStartState = stand.Single( "headpat start", "stand_headpat_happy_start", false );
		standStartState.AddEvent( Event.Voice(0,"startle soft") );
		standStartState.curves[BipedRagdoll.headID] = new Curve(0);

		var standWanterMore = stand.Single( "headpat wanted more", "stand_headpat_happy_wanted_more", false );
		standWanterMore.AddEvent( Event.Voice(0.5f,"disappointed") );
		standWanterMore.curves[BipedRagdoll.headID] = new Curve(
			new CurveKey[]{
				new CurveKey(0.38f,0.8f),new CurveKey(0.45f,0)
			}
		);
		standWanterMore.curves[BipedRagdoll.eyesID] = new Curve(
			new CurveKey[]{
				new CurveKey(0.38f,1f),new CurveKey(0.45f,0)
			}
		);
		standWanterMore.nextState = stand["idle"];
        
        var standLoopState = stand.Mixer( "headpat", new AnimationNode[]{
            new AnimationSingle( viva.Animation.Load( "stand_headpat_happy_idle_loop" ), character, true ),
            new AnimationSingle( viva.Animation.Load( "stand_headpat_happy_proper_loop" ), character, true ),
            new AnimationSingle( viva.Animation.Load( "stand_headpat_annoyed_loop" ), character, true ),
        }, headpatAnimController.weights );
		standLoopState.curves[BipedRagdoll.headID] = new Curve(0);

		standStartState.nextState = standLoopState;

		var brushAway1 = stand.Single( "brushAway1", "stand_headpat_angry_brush_away1", false );
		brushAway1.AddEvent( Event.Voice(0,"angry long") );
		brushAway1.nextState = stand["idle"];
		brushAway1.curves[BipedRagdoll.headID] = new Curve(0);
		var brushAway2 = stand.Single( "brushAway2", "stand_headpat_angry_brush_away2", false );
		brushAway2.AddEvent( Event.Voice(0,"angry long") );
		brushAway2.nextState = stand["idle"];
		brushAway2.curves[BipedRagdoll.headID] = new Curve(0);
	}
}  