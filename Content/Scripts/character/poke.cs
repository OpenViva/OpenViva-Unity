using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using viva;


public class Poke: VivaScript{

	private readonly Character character;
	private PlayAnimation lastPokeTask = null;
	private float lastPokeTime = 0;
	private List<Collider> ignore = new List<Collider>();

	public Poke( Character _character ){
		character = _character;

		SetupAnimations();

        character.biped.onCollisionEnter.AddListener( this, ListenForPoke );
	}

	private bool IsPartOfGrabbedBody( Rigidbody rigidbody, Grabber grabber ){
		for( int i=0; i<grabber.contextCount; i++ ){
			var context = grabber.GetGrabContext(i);
			if( context.grabbable.parentItem ){
				if( context.grabbable.parentItem.rigidBody == rigidbody ) return true;
			}
		}
		return false;
	}

	private void ListenForPoke( BipedBone ragdollBone, Collision collision ){
		//only poke from moving rigidBodies
		if( Util.IsImmovable( collision.collider ) ) return;
		if( collision.contactCount == 0 ) return;
		var contactPoint = collision.GetContact(0);
		//must not be in ignore list
		if( ignore.Contains( contactPoint.otherCollider ) ) return;

		//collision must only touching either of the following
		Rigidbody receivingBody;
		switch( ragdollBone ){
		case BipedBone.HEAD:
			receivingBody = character.biped.head.rigidBody;
			break;
		case BipedBone.UPPER_SPINE:
		case BipedBone.LOWER_SPINE:
		case BipedBone.HIPS:
			receivingBody = character.biped.upperSpine.rigidBody;
			break;
		default:
			return;	//ignore the rest
		}
		
		//cannot poke with items held
		if( IsPartOfGrabbedBody( collision.rigidbody, character.biped.rightHandGrabber ) ||
			IsPartOfGrabbedBody( collision.rigidbody, character.biped.leftHandGrabber ) ){
			return;
		}
		
		float minPokeStrength = 0.15f;
		var sourceCharacter = Util.GetCharacter( collision.rigidbody );
		if( sourceCharacter != null ){
			if( sourceCharacter == character ) return; //cannot poke self
			if( !sourceCharacter.isPossessed ){
				minPokeStrength = 16.0f;
			}else{
				Grabber sourceGrabber = null;
				if( sourceCharacter.isBiped && sourceCharacter.biped.rightHandGrabber.rigidBody == collision.rigidbody ){
					sourceGrabber = sourceCharacter.biped.rightHandGrabber;
				}else if( sourceCharacter.isBiped && sourceCharacter.biped.leftHandGrabber.rigidBody == collision.rigidbody ){
					sourceGrabber = sourceCharacter.biped.leftHandGrabber;
				}
				//cannot poke with a hand that recently dropped an item (prevents accidental poke when dropped)
				if( sourceGrabber && Time.time-sourceGrabber.timeSinceLastRelease < 1f ) return;
			}
		}else{
			if( collision.rigidbody.GetComponent<Item>() ){
				minPokeStrength = 8.0f;
			}
		}

		//poke must be going fast enough
		if( collision.relativeVelocity.sqrMagnitude < minPokeStrength ) return;

		//collision body must be the one with the force (cant run into pokes)
		if( collision.rigidbody.velocity.sqrMagnitude < receivingBody.velocity.sqrMagnitude ) return;
		
		switch( ragdollBone ){
		case BipedBone.HEAD:
			if( !character.autonomy.HasTag("disable poke") ){
				PokeHead( contactPoint );
			}
			break;
		case BipedBone.UPPER_SPINE:
		case BipedBone.LOWER_SPINE:
		case BipedBone.HIPS:
			PokeTorso( contactPoint );
			break;
		}
	}

	private void PokeTorso( ContactPoint contact ){
		if( Time.time-lastPokeTime < 0.4f ) return;
		lastPokeTime = Time.time;

		string tummyPokeAnim;
		if( character.mainAnimationLayer.IsPlaying("idle") ||
			character.mainAnimationLayer.IsPlaying("poke_tummy_out") ){
			tummyPokeAnim = "poke_tummy_in";
		}else if( character.mainAnimationLayer.IsPlaying("poked_tummy_loop") ||
				  character.mainAnimationLayer.IsPlaying("poked_tummy_side") ||
				  character.mainAnimationLayer.IsPlaying("poke_tummy_in") ){
			tummyPokeAnim = "poked_tummy_side";
			var localHitPos = character.biped.lowerSpine.rigidBody.transform.InverseTransformPoint( contact.point );
			var pokeSide = localHitPos.x<0;
			character.GetWeight("poke right").value = System.Convert.ToSingle( pokeSide );
			character.GetWeight("poke left").value = 1.0f-System.Convert.ToSingle( pokeSide );
		}else{
			return;
		}
		if( character.mainAnimationLayer.currentBodySet[ tummyPokeAnim ] != null ){
			//play poke animation
			if( lastPokeTask != null ){
				character.autonomy.RemoveTask( lastPokeTask );
			}
			lastPokeTask = new PlayAnimation( character.autonomy, null, tummyPokeAnim, true, 0, false );
			lastPokeTask.onEnterAnimation += delegate{
				if( contact.otherCollider ) character.biped.lookTarget.SetTargetTransform( contact.otherCollider.transform );
				lastPokeTask.Succeed();
			};
			lastPokeTask.onSuccess += delegate{
				lastPokeTask = null;
			};
			lastPokeTask.Start( this, "poked torso" );
		}
	}

	private void PokeHead( ContactPoint contact ){
		//collision must be below where forehead starts (compatibility for headpat)

		var localHitPos = character.biped.head.rigidBody.transform.InverseTransformPoint( contact.point );
		if( localHitPos.y > character.model.bipedProfile.localHeadForeheadY*character.scale ) return;
		
		if( Time.time-lastPokeTime < 0.25f ) return;
		lastPokeTime = Time.time;
				
		var pokeSide = localHitPos.x<0 ? "left" : "right";

		var pokeAnim = "poke_face_"+pokeSide+" "+Random.Range(1,3);
		if( character.mainAnimationLayer.currentBodySet[ pokeAnim ] != null ){
			//play poke animation
			if( lastPokeTask != null && !lastPokeTask.finished ){
				character.autonomy.RemoveTask( lastPokeTask );
			}
			lastPokeTask = new PlayAnimation( character.autonomy, null, pokeAnim, true, 0, false );
			float timePoked = Time.time;
			lastPokeTask.onRegistered += delegate{
				if( Time.time-timePoked > 0.3f ){
					lastPokeTask.Fail("timeout");
				}
			};
			lastPokeTask.onEnterAnimation += delegate{
				if( contact.otherCollider ) character.biped.lookTarget.SetTargetTransform( contact.otherCollider.transform );
				lastPokeTask.Succeed();
			};
			lastPokeTask.onSuccess += delegate{
				lastPokeTask = null;
			};
			lastPokeTask.Start( this, "poked face" );
		}
	}

	public void SetIgnoreCollider( Collider collider, bool ignoreFromPoking ){
		if( collider == null ) return;
		if( ignoreFromPoking ){
			if( !ignore.Contains( collider ) ) ignore.Add( collider );
		}else{
			ignore.Remove( collider );
		}
	}

	private void SetupAnimations(){

		//setup headpat animations int the stand BodySet
		var stand = character.animationSet.GetBodySet("stand");
		var standIdle = stand["idle"];
		SetupToIdleAnim( "stand tired", "poke_face_right 1", "stand_tired_poke_right", 0 )
			.AddEvent( Event.Voice(0,"startle short") );
		SetupToIdleAnim( "stand tired", "poke_face_left 1",  "stand_tired_poke_left", 0 )
			.AddEvent( Event.Voice(0,"startle short") );
		SetupToIdleAnim( "stand", "poke_face_right 1", "stand_poke_face_1_right", 0 )
			.AddEvent( Event.Voice(0,"startle short") );
		SetupToIdleAnim( "stand", "poke_face_right 2", "stand_poke_face_2_right", 0 )
			.AddEvent( Event.Voice(0,"startle short") );
		SetupToIdleAnim( "stand", "poke_face_right 3", "stand_poke_face_3_right", 0 )
			.AddEvent( Event.Voice(0,"startle short") );
		SetupToIdleAnim( "stand", "poke_face_left 1", "stand_poke_face_1_left", 0 )
			.AddEvent( Event.Voice(0,"startle short") );
		SetupToIdleAnim( "stand", "poke_face_left 2", "stand_poke_face_2_left", 0 )
			.AddEvent( Event.Voice(0,"startle short") );
		SetupToIdleAnim( "stand", "poke_face_left 3", "stand_poke_face_3_left" ,0 )
		
			.AddEvent( Event.Voice(0,"startle short") );
		SetupToIdleAnim( "stand", "poke_tummy_out", "stand_poked_tummy_out", 0.3f, 0, 1 );
		SetupToIdleAnim( "stand", "poke_tummy_in", "stand_poked_tummy_in", 0.1f, 1, 0, false, "poke_tummy_out" )
			.AddEvent( Event.Voice(0,"laugh short") );
		SetupToIdleAnim( "stand", "poked_tummy_loop", "stand_poked_tummy_center_loop", 0.3f, 0, 0, false, "poke_tummy_out" )
			.AddEvent( Event.Voice(0,"laugh long") );

		var tummyPokeSide = stand.Mixer( "poked_tummy_side", new AnimationNode[]{
			new AnimationSingle( viva.Animation.Load( "stand_poked_tummy_right" ), character, false ),
			new AnimationSingle( viva.Animation.Load( "stand_poked_tummy_left" ), character, false ),
		},
		new Weight[]{
			character.GetWeight("poke left"),
			character.GetWeight("poke right")
		});

		tummyPokeSide.nextState = stand["poked_tummy_loop"];
		tummyPokeSide.defaultTransitionTime = 0.2f;
		tummyPokeSide.AddEvent( Event.Voice(0,"laugh short") );
		tummyPokeSide.curves[BipedRagdoll.headID] = new Curve( new CurveKey[]{
			new CurveKey(0,0),new CurveKey(0.5f,0.3f),new CurveKey(1,1)
		});
	}

	private AnimationNode SetupToIdleAnim(  string bodySetName, string animGroup, string animName, float transitionTime, float startHeadLook=1, float endHeadLook=1, bool loop=false, string nextAnimState="idle" ){
		var bodySet = character.animationSet.GetBodySet( bodySetName );
		var anim = bodySet.Single( animGroup, animName, false );
		anim.nextState = bodySet[nextAnimState];
		anim.defaultTransitionTime = transitionTime;
		anim.curves[BipedRagdoll.headID] = new Curve( new CurveKey[]{
			new CurveKey(0,startHeadLook),new CurveKey(0.3f,0.3f),new CurveKey(1,endHeadLook)
		});
		return anim;
	}
}  