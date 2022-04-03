using System.Collections;
using UnityEngine;
using viva;


public class Follow: VivaScript{

	private readonly Character character;
	private Character lastCaller;
	private Task follow;
	private Timer waitToTryAgain = null;	


	public Follow( Character _character ){
		character = _character;

		character.onGesture.AddListener( this, ListenForFollowGesture );
	}

	private void ListenForFollowGesture( string gesture, Character caller ){
		if( gesture == "follow" ){
			lastCaller = caller;
			StartFollow( caller );
		}else if( gesture == "stop" ){
			StopFollowing();
		}
	}

	private void StopFollowing(){
		if( follow != null ){
			character.autonomy.RemoveTask( follow );
			follow = null;

			if( waitToTryAgain != null && waitToTryAgain.inAutonomy ) character.autonomy.RemoveTask( waitToTryAgain );
			waitToTryAgain = null;

			Save( "LoadFollow", new Character[]{null} );
		}
	}

	private void LoadFollow( object[] savedObj ){
		if( savedObj == null || savedObj.Length == 0 ) return;
		StartFollow( savedObj[0] as Character );
	}

	private void StartFollow( Character caller ){
		StopFollowing();
		if( caller == null ) return;

		//current bodySet must support movement
		var currentAnim = character.mainAnimationLayer.currentBodySet["idle"] as AnimationMixer;
		if( currentAnim == null ) return;

		if( !currentAnim.HasWeight( character.GetWeight("walking") ) ) return;

		follow = new Task( character.autonomy );
		follow.tags.Add("idle");

		var moveTo = new MoveTo( character.autonomy, 1.0f );
		moveTo.target.SetTargetCharacter( caller );
		moveTo.onRegistered += delegate{
			var callerRagdoll = moveTo.target.target as BipedRagdoll;
			if( callerRagdoll ){
				if( character.isBiped ) character.biped.lookTarget.SetTargetRigidBody( callerRagdoll.head.rigidBody );
			}
		};

		//random chance agree animation
		if( Random.value > 0.5f && character.altAnimationLayer.currentBodySet["agree"] != null ){
			var agree = new PlayAnimation( character.autonomy, null, "agree" );
			agree.onSuccess += delegate{
				follow.RemoveRequirement( agree );
			};
			follow.AddRequirement( agree );
		}

		follow.AddRequirement( moveTo );
		follow.onFail += TryAgainLater;
		follow.Start( this, "follow" );
		Save( "LoadFollow", new object[]{ caller } );
	}

	private void TryAgainLater(){
		waitToTryAgain = new Timer( character.autonomy, 1.0f );
		waitToTryAgain.onSuccess += delegate{
			waitToTryAgain.Reset();
			StartFollow( lastCaller );
		};
		waitToTryAgain.Start( this, "follow try again" );
	}
}  