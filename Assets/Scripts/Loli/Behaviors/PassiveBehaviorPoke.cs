using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace viva{


public partial class PokeBehavior : PassiveBehaviors.PassiveTask {

	private float lastPokeTime = 0.0f;
	public int pokeCount = 0;
	private Loli.Animation postFacePokeAnimation = Loli.Animation.NONE;
	private Loli.Animation postTummyPokeAnim = Loli.Animation.NONE;
	private Loli.Animation newFacePokedAnimation = Loli.Animation.NONE;
	private float lastPokeCheekWipeReactTime = -Mathf.Infinity;
	private float lastPokeBlockReactTime = -Mathf.Infinity;
	private Transform lastPokeSource = null;
	private Tools.EaseBlend tummyPokeXBlend = new Tools.EaseBlend();
	private float pokeLaughTimer = 0.0f;
	private float poleLaughRandomLookAtTimer = 1.0f;

	public PokeBehavior( Loli _self ):base(_self,0.0f){
	}

	public class TransitionToPostFacePokeAnim: Loli.TransitionHandle{
		
		public TransitionToPostFacePokeAnim():base(TransitionType.NO_MIRROR){
		}
		public override void Transition(Loli self){
			self.UpdateAnimationTransition( self.passive.poke.postFacePokeAnimation );
		}
	}

	public class TransitionToPostTummyPokeAnim: Loli.TransitionHandle{
		
		public TransitionToPostTummyPokeAnim():base(TransitionType.NO_MIRROR){
		}
		public override void Transition(Loli self){
			self.UpdateAnimationTransition( self.passive.poke.postTummyPokeAnim );
		}
	}

	public override void OnUpdate(){
		
		if( self.currentAnim == Loli.Animation.STAND_POKED_TUMMY_LOOP ){
			tummyPokeXBlend.Update( Time.deltaTime );
			self.animator.SetFloat( Instance.pokeTummyXID, tummyPokeXBlend.value );
			if( pokeLaughTimer <= 0.0f ){
				pokeLaughTimer = 0.0f;
				self.SetTargetAnimation( Loli.Animation.STAND_POKED_TUMMY_OUT );
			}else{
				pokeLaughTimer -= Time.deltaTime;
			}
			poleLaughRandomLookAtTimer -= Time.deltaTime;
			if( poleLaughRandomLookAtTimer < 0.0f ){
				poleLaughRandomLookAtTimer = 0.4f+Random.value*0.5f;
				self.SetLookAtTarget( GameDirector.player.head, 1.1f );
				if( Random.value > 0.5f ){
					self.setBodyVariables( 3, 1.0f, poleLaughRandomLookAtTimer*0.7f );
				}else{
					self.setBodyVariables( 0, 1.0f, poleLaughRandomLookAtTimer*0.7f );
				}
			}
		}
	}

	public bool AttemptTummyPoke( Item sourceItem ){
		viva.DevTools.LogExtended("Attempting TummyPoke", true, true);
		//tummy poking supported only while standing
		if( self.bodyState != BodyState.STAND ){
			return false;
		}
		//If moving fast enough
		if( !IsApproachingPointQuickly( self.spine2.position, sourceItem, 0.04f ) ){
			return false;
		}
		if( sourceItem.mainOwner != null && sourceItem.settings.itemType == Item.Type.CHARACTER && sourceItem.mainOwner.characterType == self.characterType ){
			return false;
		}
		lastPokeSource = sourceItem.transform;
		if( self.currentAnim == Loli.Animation.STAND_POKED_TUMMY_LOOP ){
			UpdateTummyPokeX();
		}else if( self.currentAnim == Loli.Animation.STAND_POKED_TUMMY_IN ){
			postTummyPokeAnim = Loli.Animation.STAND_POKED_TUMMY_LOOP;
			UpdateTummyPokeX();
		}else if( self.currentAnim == Loli.Animation.STAND_POKED_TUMMY_OUT ){
			if( self.GetLayerAnimNormTime(1) < 0.6f ){
				self.OverrideClearAnimationPriority();
				self.SetTargetAnimation( Loli.Animation.STAND_POKED_TUMMY_LOOP );
				UpdateTummyPokeX();
			}else{
				self.SetTargetAnimation( Loli.Animation.STAND_POKED_TUMMY_IN );
				self.Speak( Loli.VoiceLine.STARTLE_SHORT );
			}
		}else{
			self.SetTargetAnimation( Loli.Animation.STAND_POKED_TUMMY_IN );
			postTummyPokeAnim = Loli.Animation.STAND_POKED_TUMMY_OUT;
			self.Speak( Loli.VoiceLine.STARTLE_SHORT );
		}
		if( self.active.RequestPermission( ActiveBehaviors.Permission.ALLOW_ROOT_FACING_TARGET_CHANGE ) ){
			if( self.bodyState == BodyState.STAND){ //Make sure to only face direction when standing
				self.autonomy.SetAutonomy(new AutonomyFaceDirection( self.autonomy, "face direction", delegate(TaskTarget target){
                        target.SetTargetPosition(GameDirector.player.transform.position+lastPokeSource.position );
                    } ) );
			}		
			// self.SetRootFacingTarget( ( GameDirector.player.transform.position+lastPokeSource.position )/2.0f, 160.0f, 5.0f, 10.0f );
		}
		return true;
	}

	private void UpdateTummyPokeX(){
		float pokeX = Tools.Bearing( self.spine2, lastPokeSource.position );
		float newPokeX = Mathf.Clamp( Mathf.Round( pokeX/50.0f ), -1.0f, 1.0f );
		tummyPokeXBlend.StartBlend( newPokeX, 0.6f );
		if( self.IsSpeaking( Loli.VoiceLine.LAUGH_LONG ) ){
			return;
		}
		if( Random.value > 0.85f ){
			pokeLaughTimer = 1.3f;
			self.Speak( Loli.VoiceLine.LAUGH_LONG );
		}else{
			pokeLaughTimer = 0.45f;
			self.Speak( Loli.VoiceLine.LAUGH_SHORT );
		}
	}

	private bool IsApproachingPointQuickly( Vector3 center, Item item, float minSqSpeed ){
		if( Vector3.Dot( center-item.rigidBody.worldCenterOfMass, item.rigidBody.velocity ) < 0.0f ){
			return false;
		}
		return item.rigidBody.velocity.sqrMagnitude > minSqSpeed;
	}

	private Loli.Animation GetBlockFacePokeAnimation( int pokeSideIsLeft ){
		if( Random.value > 0.5f ){
			return (Loli.Animation)((int)Loli.Animation.STAND_HEADPAT_CLIMAX_TO_CANCEL_RIGHT+pokeSideIsLeft);
		}else{
			return (Loli.Animation)((int)Loli.Animation.STAND_ANGRY_BLOCK_RIGHT+pokeSideIsLeft);
		}
	}

	private Loli.Animation GetFacePokedAnimation( int pokeSideIsLeft ){
		//return self.bodyStateAnimationSets[ (int)self.bodyState ].GetAnimationSet( AnimationSet.POKE_FACE_SOFT_RIGHT, pokeSideIsLeft );
		//if( animations.)
		switch( self.bodyState ){
		case BodyState.STAND:
			if( self.IsTired() ){
				return self.GetAnimationFromSet( Loli.Animation.STAND_TIRED_POKE_RIGHT, pokeSideIsLeft );
			}else{
				return self.GetAnimationFromSet( Loli.Animation.STAND_POKE_FACE_1_RIGHT, pokeSideIsLeft, 2 );
			}
		case BodyState.BATHING_RELAX:
			return self.GetAnimationFromSet( Loli.Animation.BATHTUB_RELAX_FACE_POKE_RIGHT, pokeSideIsLeft );
		case BodyState.BATHING_IDLE:
			return GetBathtubIdleFacePokedAnimation( pokeSideIsLeft );
		// case BodyState.SLEEP_PILLOW_SIDE:
			// return self.active.sleeping.GetSleepSidePillowFacePokeAnimation( pokeSideIsLeft );
		case BodyState.SLEEP_PILLOW_UP:
			return self.GetAnimationFromSet( Loli.Animation.SLEEP_PILLOW_UP_BOTHER_RIGHT, pokeSideIsLeft );
		case BodyState.AWAKE_PILLOW_UP:
			return self.GetAnimationFromSet( Loli.Animation.AWAKE_PILLOW_UP_FACE_POKE_RIGHT, pokeSideIsLeft );
		case BodyState.RELAX:
			return Loli.Animation.RELAX_TO_SQUAT_STARTLE;
		case BodyState.SQUAT:
			return self.GetAnimationFromSet( Loli.Animation.SQUAT_FACE_POKE_1_RIGHT, pokeSideIsLeft, 2 );
		default:
			return Loli.Animation.NONE;
		}
	}

	private Loli.Animation GetPostFacePokeAnimation( int pokeSideIsLeft ){
		
		switch( self.bodyState ){
		case BodyState.STAND:
			if( pokeCount >= 2 && Time.time-lastPokeCheekWipeReactTime > 15.0f ){
				if( pokeSideIsLeft == 0 ){
					if( self.rightHandState.holdType == HoldType.NULL ){
						return Loli.Animation.STAND_WIPE_CHEEK_RIGHT;
					}
				}else if( self.leftHandState.holdType == HoldType.NULL ){
					return Loli.Animation.STAND_WIPE_CHEEK_LEFT;
				}
			}
			break;
		case BodyState.BATHING_RELAX:
			return self.GetAnimationFromSet( Loli.Animation.BATHTUB_RELAX_FACE_POKE_RIGHT, pokeSideIsLeft );
		case BodyState.BATHING_IDLE:
			return GetBathtubIdlePostFacePokedAnimation( pokeSideIsLeft );
		//case BodyState.SLEEP_PILLOW_SIDE_LEFT:
			//return self.active.sleeping.GetSleepSidePillowPostFacePokeAnimation();
		//case BodyState.SLEEP_PILLOW_UP:
			//return self.active.sleeping.GetSleepPillowUpPostFacePokeAnimation();
		}
		return self.GetLastReturnableIdleAnimation();
	}
	
	public bool AttemptFacePoke( Item sourceItem ){
		
		if( sourceItem == null || sourceItem.rigidBody == null ){
			return false;
		}
		//Disable poking while changing BodyState
		if( self.IsAnimationChangingBodyState() ){
			return false;
		}
		//sourceItem must not be part of her own body
		if( sourceItem.mainOwner == self ){
			return false;
		}
		//disable if sourceItem is head (prevents glitchy constant head x head poking)
		if( sourceItem.mainOwner != null && sourceItem == sourceItem.mainOwner.headItem ){
			return false;
		}
		//If moving towards face and fast enough
		if( !IsApproachingPointQuickly( self.headItem.rigidBody.worldCenterOfMass, sourceItem, 0.002f ) ){
			return false;
		}
		//prevent double poking, same objects must wait 0.5 seconds
		if( lastPokeSource == sourceItem.transform && Time.time-lastPokeTime < 0.5f ){
			return false;
		}
		//make sure it is not the headpat hand
		if( sourceItem.mainOwner != null ){
			OccupyState sourceHoldState = sourceItem.mainOwner.FindHandStateBySelfItem( sourceItem );
			if( sourceHoldState != null && sourceHoldState == self.passive.headpat.GetLastHeadpatSourceHoldState() ){
				return false;
			}
		}
		lastPokeSource = sourceItem.transform;
		//decrement pokeCount every 4 second
		int newPokeCount = Mathf.Clamp( pokeCount-(int)( Time.time-lastPokeTime )/4, 0, 6 );

		float bearing = Tools.Bearing( self.head, lastPokeSource.position );
		int pokeSideIsLeft = (int)System.Convert.ToInt32( bearing<0 );	//0 right or 1 left
		newFacePokedAnimation = Loli.Animation.NONE;

		if( pokeCount > 4 ){	//4 pokes and she becomes angry
			if( self.active.RequestPermission( ActiveBehaviors.Permission.ALLOW_ROOT_FACING_TARGET_CHANGE ) ){
				if( self.bodyState == BodyState.STAND){ //Make sure to only face direction when standing
					self.autonomy.SetAutonomy(new AutonomyFaceDirection( self.autonomy, "face direction", delegate(TaskTarget target){
                        target.SetTargetPosition( lastPokeSource.position );
                    } ) );
				}
				// self.SetRootFacingTarget( lastPokeSource.position, 200.0f, 15.0f, 50.0f );
			}
			if( Random.value < 0.4f && Time.time-lastPokeBlockReactTime > 2.0f ){
				newFacePokedAnimation = GetBlockFacePokeAnimation( pokeSideIsLeft );
				if( newFacePokedAnimation == Loli.Animation.NONE ){	//if no block poke animation
					newFacePokedAnimation = GetFacePokedAnimation( pokeSideIsLeft );
				}else{
					//set up block poke
					self.ShiftHappiness(-2);
					GameDirector.player.CompleteAchievement(Player.ObjectiveType.POKE_ANGRY);

					if( self.active.RequestPermission( ActiveBehaviors.Permission.ALLOW_IMPULSE_ANIMATION ) ){
						Vector3 push = self.head.position-lastPokeSource.position;
						push.y = 0.0f;
						self.locomotion.PlayForce( push.normalized*(0.2f+Random.value*0.8f), 0.2f+Random.value*0.3f );
					}
				}
			}else{
				newFacePokedAnimation = GetFacePokedAnimation( pokeSideIsLeft );
			}
		}else{
			newFacePokedAnimation = GetFacePokedAnimation( pokeSideIsLeft );
		}

		//no poke animation possible
		if( newFacePokedAnimation == Loli.Animation.NONE ){
			return false;
		}
		pokeCount = newPokeCount;
		self.SetTargetAnimation( newFacePokedAnimation );
		postFacePokeAnimation = GetPostFacePokeAnimation( pokeSideIsLeft );
		if( postFacePokeAnimation == Loli.Animation.NONE ){
			postFacePokeAnimation = self.GetLastReturnableIdleAnimation();
		}

		return true;
	}

	public override void OnAnimationChange( Loli.Animation oldAnim, Loli.Animation newAnim ){

		if( newAnim == newFacePokedAnimation ){
			pokeCount++;
			lastPokeTime = Time.time;
			LookAtLastPokeSource();
		}

		switch( newAnim ){
		case Loli.Animation.STAND_WIPE_CHEEK_RIGHT:
		case Loli.Animation.STAND_WIPE_CHEEK_LEFT:
		case Loli.Animation.STAND_HEADPAT_CLIMAX_TO_CANCEL_RIGHT:
			lastPokeCheekWipeReactTime = Time.time;
			self.SetLookAtTarget(GameDirector.player.head,1.2f);
			self.SetViewAwarenessTimeout(0.5f);
			break;
		case Loli.Animation.STAND_ANGRY_BLOCK_RIGHT:
		case Loli.Animation.STAND_ANGRY_BLOCK_LEFT:
			lastPokeBlockReactTime = Time.time;
			lastPokeTime = Time.time;
			LookAtLastPokeSource();
			break;
		case Loli.Animation.STAND_POKED_TUMMY_IN:
			tummyPokeXBlend.reset(0.0f);
			break;
		}

		switch( oldAnim ){
		case Loli.Animation.STAND_POKED_TUMMY_LOOP:
			tummyPokeXBlend.reset(0.0f);
			break;
		}
	}

	private void LookAtLastPokeSource(){
		if( lastPokeSource != null ){
			self.SetLookAtTarget(lastPokeSource,2.0f);
			self.SetViewAwarenessTimeout(0.5f);
		}
	}

	public bool AttemptFootPoke( Item sourceItem ){
		if( sourceItem.mainOwner == null || sourceItem.mainOwner.characterType != Character.Type.PLAYER ){
			return false;
		}
		/* simple laugh test
		if (pokeLaughTimer > 0f) {
			viva.DevTools.LogExtended("Attempting FootPoke, already in progress", true, true);
		}
		viva.DevTools.LogExtended("Attempting FootPoke", true, true);
		if( Random.value > 0.85f ){
			pokeLaughTimer = 1.3f;
			self.Speak( Loli.VoiceLine.LAUGH_LONG );
		}else{
			pokeLaughTimer = 0.45f;
			self.Speak( Loli.VoiceLine.LAUGH_SHORT );
		}
		return true;
		*/
		viva.DevTools.LogExtended("Attempting FootPoke");
		viva.DevTools.LogExtended("self.currentAnim: " + self.currentAnim);
		/*//tummy poking supported only while standing
		if( self.bodyState != BodyState.STAND ){
			return false;
		}
		//If moving fast enough
		if( !IsApproachingPointQuickly( self.spine2.position, sourceItem, 0.0025f ) ){
			return false;
		}*/
		lastPokeSource = sourceItem.transform;
		if( self.currentAnim == Loli.Animation.STAND_POKED_TUMMY_LOOP ){
			UpdateTummyPokeX();
		}else if( self.currentAnim == Loli.Animation.STAND_POKED_TUMMY_IN ){
			postTummyPokeAnim = Loli.Animation.STAND_POKED_TUMMY_LOOP;
			UpdateTummyPokeX();
		}else if( self.currentAnim == Loli.Animation.STAND_POKED_TUMMY_OUT ){
			viva.DevTools.LogExtended("self.GetLayerAnimNormTime(1)" + self.GetLayerAnimNormTime(1));
			if( self.GetLayerAnimNormTime(1) < 0.6f ){
				self.OverrideClearAnimationPriority();
				self.SetTargetAnimation( Loli.Animation.STAND_POKED_TUMMY_LOOP );
				UpdateTummyPokeX();
			}else{
				self.SetTargetAnimation( Loli.Animation.STAND_POKED_TUMMY_IN );
				self.Speak( Loli.VoiceLine.STARTLE_SHORT );
			}
		}else if(self.bodyState == BodyState.STAND){
			self.SetTargetAnimation( Loli.Animation.STAND_POKED_TUMMY_IN );
			postTummyPokeAnim = Loli.Animation.STAND_POKED_TUMMY_OUT;
			self.Speak( Loli.VoiceLine.STARTLE_SHORT );
		}
		else{
			if (pokeLaughTimer <= 0f) {
				UpdateTummyPokeX();
			}
		}
		if( self.active.RequestPermission( ActiveBehaviors.Permission.ALLOW_ROOT_FACING_TARGET_CHANGE ) ){
			if( self.bodyState == BodyState.STAND){ //Make sure to only face direction when standing 
					self.autonomy.SetAutonomy(new AutonomyFaceDirection( self.autonomy, "face direction", delegate(TaskTarget target){
                        target.SetTargetPosition( GameDirector.player.transform.position+lastPokeSource.position );
                    } ) );
			}
			// self.SetRootFacingTarget( ( GameDirector.player.transform.position+lastPokeSource.position )/2.0f, 160.0f, 5.0f, 10.0f );
		}
		//viva.DevTools.LogExtended("", true, true);
		return true;
	}
}

}