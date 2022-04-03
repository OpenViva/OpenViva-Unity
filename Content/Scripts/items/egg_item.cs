using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using viva;


public class EggItem: VivaScript{

	private Item item;
    private ItemUserListener itemUser;


	public EggItem( Item _item ){
        item = _item;

		itemUser = new ItemUserListener( item, this, BindCharacter, UnbindCharacter );

		item.onCollision.AddListener( this, OnCollision );
	}

	private void SetupAnimations( Character character ){
		var brushHead = character.animationSet.GetBodySet("stand").Single( "brush head", "stand_headpat_angry_brush_away2", false );
		brushHead.AddEvent( Event.Voice(0,"angry long") );
		brushHead.nextState = character.animationSet.GetBodySet("stand")["idle"];
		brushHead.curves[BipedRagdoll.headID] = new Curve(0);
	}

	private void OnCollision( Collision collision ){
		if( item.destroyed ) return;
		var force = collision.relativeVelocity.sqrMagnitude;
		if( force > 10f ){
			
			Sound.Create( item.rigidBody.worldCenterOfMass ).Play( "generic","egg","egg_break.wav" );
            Viva.Destroy( item );
			
			if( collision.rigidbody ){
				var character = Util.GetCharacter( collision.rigidbody );
				if( character && !character.isAnimal ){
					SetupAnimations( character );
					
					if( character.autonomy.HasTag("idle") ){
						character.mainAnimationLayer.player.Play( this, character.mainAnimationLayer.currentBodySet.name, "brush head" );
					}
				}
			}else{
				var contact = collision.GetContact(0);
				var spawnPos = contact.point-contact.normal*( contact.separation-0.0001f );
				SpawnSplat( spawnPos, contact.normal );
			}
		}
	}

	private void SpawnSplat( Vector3 pos, Vector3 normal ){
		var spawnRot = Quaternion.LookRotation( normal, Vector3.up );
		spawnRot *= Quaternion.Euler(90,0,0);

		var splat = Item.Spawn("eggSplat", pos, spawnRot );
		if( splat ) splat.transform.localRotation *= Quaternion.Euler(0,Random.Range(-180,180),0);
		Viva.Destroy( item, 12f);
	}

	private void BindCharacter( Character character, GrabContext context ){
		if( character.possessor && !character.possessor.isUsingKeyboard ){
			if( character.isBiped && context.grabber == character.biped.rightHandGrabber ){
				character.GetInput( Input.RightAction ).onDown.AddListener( this, BreakEgg );
			}else{
				character.GetInput( Input.LeftAction ).onDown.AddListener( this, BreakEgg );
			}
		}
	}

	private void UnbindCharacter( Character character, GrabContext context ){
		if( character.possessor && !character.possessor.isUsingKeyboard ){
			if( character.isBiped && context.grabber == character.biped.rightHandGrabber ){
				character.GetInput( Input.RightAction ).onDown.RemoveListener( this, BreakEgg );
			}else{
				character.GetInput( Input.LeftAction ).onDown.RemoveListener( this, BreakEgg );
			}
		}
	}

	private void BreakEgg(){
        if( item && !item.destroyed ){
            Viva.Destroy( item );
            Sound.Create( item.transform.position ).Play( "generic", "egg", "egg_crack.wav" );
            Spill.Create( new Attribute[]{ new Attribute("mixed egg",1)}, item.transform.position, 2, "yolk", item, itemUser.character );
        }
	}
}  