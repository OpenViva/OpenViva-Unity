using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using viva;


public class Snack: VivaScript{

	private readonly Character character;
	private Item targetSnack;

	public Snack( Character _character ){
		character = _character;

		SetupAnimations();

		character.biped.lookTarget.onChanged.AddListener( this, OnLookTargetChanged );

        Achievement.Add( "Feed a strawberry directly", "Feed a strawberry to someone by hand. Hold close to a character's mouth" );
	}

	private void OnLookTargetChanged(){
		CheckCanSnack( character.biped.vision.GetMostRelevantItem() );
	}

	private void SetupAnimations(){

		var stand = character.animationSet.GetBodySet("stand");

		var standSnackOut = stand.Single( "snack out", "stand_feed_out", false );
		standSnackOut.nextState = stand["idle"];

		var standSnackLoop = stand.Single( "snack loop", "stand_feed_loop", true );
		standSnackLoop.curves.Add( BipedRagdoll.headID, new Curve( 0.3f ) );

		var standSnackIn = stand.Single( "snack in", "stand_feed_in", false );
		standSnackIn.curves.Add( BipedRagdoll.headID, new Curve( 0.0f ) );
		standSnackIn.nextState = standSnackLoop;

		var standSnackSuccess = stand.Single( "snack success", "stand_feed_success", false );
		standSnackSuccess.curves.Add( BipedRagdoll.headID, new Curve(0) );
		standSnackSuccess.nextState = stand["idle"];
		standSnackSuccess.AddEvent( Event.Voice(0,"eat"));
		standSnackSuccess.AddEvent( Event.Function( 0.1f, this, EatSnack ));
	}

	private void EatSnack(){
		if( !targetSnack ) return;
		Viva.Destroy( targetSnack );

		AchievementManager.main.CompleteAchievement( "Feed a strawberry directly", true );
	}

	private bool InRangeForEating( Item item, float rangeMult ){
		if( !item ) return false;
		var headRadius = character.biped.CalculateMuscleBoundingSphere( character.biped.head );
		var head = character.biped.head.rigidBody;
		var headDistance = Vector3.Distance( head.worldCenterOfMass, item.rigidBody.worldCenterOfMass );
		headDistance -= headRadius;
		if( headDistance > headRadius*rangeMult ) return false;
		return Vector3.Dot( head.transform.forward, (item.rigidBody.worldCenterOfMass-head.worldCenterOfMass).normalized ) > 0.7f;
	}

	private void CheckCanSnack( Item item ){
		if( !item ) return;
		//make sure current animation can play snack animation
		if( character.mainAnimationLayer.currentBodySet["snack in"] == null ) return;
		if( character.IsGrabbing( item ) ) return;
		if( !item.HasAttribute("snack") ) return;
		if( !character.autonomy.HasTag("idle") ) return;
		if( character.autonomy.FindTask("snack") != null ) return;

		targetSnack = item;
		
		var waitForInitialProximity = new Task( character.autonomy );

		waitForInitialProximity.onFixedUpdate += delegate{
			if( InRangeForEating( targetSnack, 2.5f ) ){
				character.autonomy.RemoveTask( waitForInitialProximity );

				var leanInForSnackAnim = new PlayAnimation( character.autonomy, null, "snack loop", true, 0, false );

				var waitForSnack = new PlayAnimation( character.autonomy, null, "snack loop", true, -1 );
				waitForSnack.AddRequirement( leanInForSnackAnim );

				var faceSnack = new FaceTargetBody( character.autonomy );
				faceSnack.target.SetTargetRigidBody( targetSnack.rigidBody );
				waitForSnack.AddPassive( faceSnack );

				var outOfRangeTimer = 0f;
				waitForSnack.onFixedUpdate += delegate{
					if( !InRangeForEating( targetSnack, 3f ) ){
						outOfRangeTimer += Time.deltaTime;
						if( outOfRangeTimer > 0.5f ){
							waitForSnack.Fail("snack too far");

							var exitSnackAnim = new PlayAnimation( character.autonomy, null, "snack out", true, 0, false );
							exitSnackAnim.Start( this, "stop snack in" );
						}
					}else{
						outOfRangeTimer = 0f;
						if( InRangeForEating( targetSnack, 0.5f ) ){
							waitForSnack.Succeed();
							
							var exitSnackAnim = new PlayAnimation( character.autonomy, null, "snack success", true, 0, false );
							exitSnackAnim.transitionTime = 0.05f;
							exitSnackAnim.tags.Add("disable poke");
							exitSnackAnim.tags.Add("disable headpat");
							exitSnackAnim.Start( this, "stop snack in" );
						}
					}
				};

				waitForSnack.tags.Add("disable poke");
				waitForSnack.Start(this,"snack");
			}
		};

		waitForInitialProximity.StartConstant( this, "wait for initial snack dist");
	}
}  