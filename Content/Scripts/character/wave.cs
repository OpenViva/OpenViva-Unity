using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using viva;


public class Wave: VivaScript{

	private readonly Character character;
	private List<Character> alreadyGreeted = new List<Character>();

	public Wave( Character _character ){
		character = _character;

		// //setup animations
		var stand = character.animationSet.GetBodySet("stand");
		var standWaveRight = stand.Single( "wave right", "stand_wave_happy_right", false );
		standWaveRight.nextState = stand["idle"];
		standWaveRight.AddEvent( Event.Voice(0.1f,"giggle") );

		var standWaveLeft = stand.Single( "wave left", "stand_wave_happy_left", false );
		standWaveLeft.nextState = stand["idle"];
		standWaveLeft.AddEvent( Event.Voice(0.1f,"giggle") );

		character.onGesture.AddListener( this, ListenForWaveGesture );
		character.biped.vision.onCharacterSeen.AddListener( this, RespondToCharacter );
	}

	public void RespondToCharacter( Character otherCharacter ){
		if( AllowedToGreet( otherCharacter ) ){
			GreetCharacter( otherCharacter );
		}
	}

	private void ListenForWaveGesture( string gesture, Character caller ){
		if( gesture == "hello" ){
			GreetCharacter( caller );
		}
	}

	private bool AllowedToGreet( Character otherCharacter ){
		if( !otherCharacter ) return false;
		if( otherCharacter == character ) return false;
		if( alreadyGreeted.Contains( otherCharacter ) ) return false;
		Util.RemoveNulls( alreadyGreeted );

		alreadyGreeted.Add( otherCharacter );
		return true;
	}

	private void GreetCharacter( Character otherCharacter ){
		if( !otherCharacter || !otherCharacter.isBiped ) return;

		var callerHead = otherCharacter.biped.head.rigidBody;
		var faceCaller = new FaceTargetBody( character.autonomy, 1.25f, 25, 0 );
		faceCaller.target.SetTargetRigidBody( callerHead );
		faceCaller.onRegistered += delegate{
			character.biped.lookTarget.SetTargetRigidBody( callerHead );
		};

		faceCaller.onSuccess += delegate{
			var side = character.biped.rightHandGrabber.grabbing ? "left":"right";
			var playWave = new PlayAnimation( character.autonomy, null, "wave "+side, true, 0.6f, false );
			playWave.onSuccess += delegate{
				if( !otherCharacter.isPossessed ) otherCharacter.scriptManager.CallOnScript( "wave", "RespondToCharacter", new object[]{ character } );
			};

			playWave.Start( this, "waving hello" );
		};

		faceCaller.Start( this, "face waver" );
	}
}  