using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace viva{


public class KissingBehavior : PassiveBehaviors.PassiveTask {

	public Loli.Animation postKissAnim = Loli.Animation.NONE;
	private readonly float minStartleSqSpeed = 16.0f;

	public KissingBehavior( Loli _self ):base(_self,0.0f){

		self.onCharacterCollisionEnter += OnCharacterCollisionEnter;
	}

	public class TransitionToPostKiss: Loli.TransitionHandle{
		
		public TransitionToPostKiss():base(TransitionType.NO_MIRROR){
		}
		public override void Transition(Loli self){
			self.UpdateAnimationTransition( self.passive.kissing.postKissAnim );
		}
	}


	public override void OnFixedUpdate(){
		foreach( var itemEntry in self.passive.nearbyItems ){
			Item item = itemEntry._1;
			if( item == null || item.mainOwner == self || item.rigidBody == null ){
				continue;
			}
			var sqSpeed = item.rigidBody.velocity.sqrMagnitude;
			if( sqSpeed > minStartleSqSpeed ){
				//check if coming into opposite direction
				if( Vector3.Dot( item.rigidBody.velocity, self.head.forward ) < 0.0f ){
					if( self.CanSeePoint( item.transform.position ) ){
						Startle( item );
					}
				}
			}
		}
	}

	private void Kiss( bool rightCheek, Character source ){

		if( source.characterType == self.characterType ){
			return;
		}

		int side = System.Convert.ToInt32( rightCheek );
		Loli.Animation kissAnim;
		if( self.IsHappy() ){
			kissAnim = self.bodyStateAnimationSets[ (int)self.bodyState ].GetAnimationSet( AnimationSet.CHEEK_KISS_HAPPY_RIGHT_LEFT, side );
		}else{
			kissAnim = self.bodyStateAnimationSets[ (int)self.bodyState ].GetAnimationSet( AnimationSet.CHEEK_KISS_ANGRY_RIGHT_LEFT, side );
			if( Random.value > 0.5f ){
				postKissAnim = self.bodyStateAnimationSets[ (int)self.bodyState ].GetAnimationSet( AnimationSet.CHEEK_KISS_ANGRY_TO_HAPPY_RIGHT_LEFT, side );
				self.ShiftHappiness(2); //ACTUALLY MAKE HER HAPPY
				GameDirector.player.CompleteAchievement(Player.ObjectiveType.KISS_MAKE_HAPPY); 
			}else{
				postKissAnim = self.bodyStateAnimationSets[ (int)self.bodyState ].GetAnimationSet( AnimationSet.CHEEK_KISS_ANGRY_TO_ANGRY_RIGHT_LEFT, side );
				self.ShiftHappiness(-2);
				GameDirector.player.CompleteAchievement(Player.ObjectiveType.KISS_ANGRY_WIPE);
			}
			if( postKissAnim == Loli.Animation.NONE ){
				return;
			}
		}
		if( kissAnim == Loli.Animation.NONE ){
			return;
		}
		var playKissAnim = new AutonomyPlayAnimation( self.autonomy, "play kiss anim", kissAnim );

		var faceSource = new AutonomyFaceDirection( self.autonomy, "face startle", delegate( TaskTarget target ){
			target.SetTargetPosition( source.head.position );
		} );
		bool kissDisableFaceYaw = false;
		faceSource.onRegistered += delegate{ self.ApplyDisableFaceYaw( ref kissDisableFaceYaw ); };
		faceSource.onRegistered += delegate{ self.RemoveDisableFaceYaw( ref kissDisableFaceYaw ); };
		playKissAnim.AddPassive( faceSource );
		self.autonomy.Interrupt( playKissAnim );
	}

	private void Startle( Item sourceItem ){
		
		Loli.Animation startleAnim;
		if( self.IsHappy() ){
			startleAnim = self.bodyStateAnimationSets[ (int)self.bodyState ].GetRandomAnimationSet( AnimationSet.STARTLED_HAPPY );
		}else{
			startleAnim = self.bodyStateAnimationSets[ (int)self.bodyState ].GetRandomAnimationSet( AnimationSet.STARTLED_ANGRY );
		}
		if( startleAnim == Loli.Animation.NONE ){
			return;
		}

		var playStartleAnim = new AutonomyPlayAnimation( self.autonomy, "play startle", startleAnim );

		var faceSource = new AutonomyFaceDirection( self.autonomy, "face startle", delegate( TaskTarget target ){
			target.SetTargetItem( sourceItem );
		} );
		bool kissDisableFaceYaw = false;
		faceSource.onRegistered += delegate{ self.ApplyDisableFaceYaw( ref kissDisableFaceYaw ); };
		faceSource.onRegistered += delegate{ self.RemoveDisableFaceYaw( ref kissDisableFaceYaw ); };

		playStartleAnim.onAnimationEnter += delegate{
			Vector3 surprisePush = self.floorPos-sourceItem.transform.position;
			surprisePush.y = 0.0f;
			surprisePush = surprisePush.normalized*2.0f;
			self.locomotion.PlayForce( surprisePush, 0.2f );
		};

		playStartleAnim.AddPassive( faceSource );
		self.autonomy.Interrupt( playStartleAnim );
	}

	private void OnCharacterCollisionEnter( CharacterCollisionCallback ccc, Collision collision ){
		Item item = collision.collider.GetComponent<Item>();
		if( item != null && item.settings.itemType == Item.Type.CHARACTER && item.mainOwner.headItem == item ){
			var kissAveragePos = GamePhysics.AverageContactPosition( collision, 2 );
			if( kissAveragePos.HasValue ){
				Vector3 localKissPos = self.headItem.transform.InverseTransformPoint( kissAveragePos.Value );
				if( localKissPos.z > 0.0f ){
					Kiss( localKissPos.x > 0, item.mainOwner );
				}
			}
		}
	}


	private bool BehaviorIsAllowed(){
		//disable during headpat
		if( self.passive.headpat.IsHeadpatActive() ){
			return false;
		}
		if( self.IsTired() ){
			return false;
		}
		return true;
	}
}

}