using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace viva{

using LoliAnimationEvent = AnimationEvent<float[]>;

public partial class Loli : Character {

	// RIGHT enums must be before LEFT
	public enum AnimationEventName{
		BLEND_CONTROLLER_RIGHT,
		BLEND_CONTROLLER_LEFT,
		ATTEMPT_CATTAIL_HIT,
		REDISTRIBUTE_CHOPSTICKS_FINGERS,
		SET_BODY_FLAGS,
		SET_EYE_FLAGS,
		WEAR_HAT_RIGHT_HAND,
		WEAR_HAT_LEFT_HAND,
		EAT_CURRENT_INTEREST,
		TOGGLE_FACE_YAW_SUM_FOR_ANIMATION,
		THROW_RIGHT_HAND_OBJECT,
		THROW_LEFT_HAND_OBJECT,
		SPAWN_POLAROID_FRAME_RIPPED_FX,
		SPEAK,
		SPEAK_INTERVALS,
		HEADPAT_PROPER_SOUND,
		FOOTSTEP_SOUND,
		BATHTUB_PLAY_SOUND,
		BIND_ANIMATION_TO_VIEW_MODE,
		JUMP_ON_BED,
		ROLL_ON_BED,
		BEGIN_MOUNT,
		ATTACH_BAG_SHOULDER,
		REMOVE_BAG_SHOULDER,
		OPEN_BAG_RIGHT,
		OPEN_BAG_LEFT,
		PUT_ITEM_IN_BAG_RIGHT,
		PUT_ITEM_IN_BAG_LEFT,
		DROP_RIGHT_HAND_ITEM,
		DROP_LEFT_HAND_ITEM,
		BATHTUB_SPLASH,
		EXECUTE_PICK_CARD_RIGHT,
		EXECUTE_PICK_CARD_LEFT,
		PLAY_CLAP_SOUND,
		STOP_SPINE_ANCHOR,
		DISABLE_STAND_ON_GROUND
	}

	public class AnimLogicInfo{

		public readonly int bodyFlags;
		public readonly float bodyFlagPercent;
		public readonly float bodyFlagDuration;
		public readonly int eyeFlags;
		public readonly float eyeFlagDuration;

		public AnimLogicInfo(){
			bodyFlags = 3;
			bodyFlagPercent = 1.0f;
			bodyFlagDuration = 1.0f;
			eyeFlags = 7;
			eyeFlagDuration = 1.0f;
		}

		public AnimLogicInfo( int _bodyFlags, float _bodyFlagPercent, float _bodyFlagDuration, int _eyeFlags, float _eyeFlagDuration ){
			bodyFlags = _bodyFlags;
			bodyFlagPercent = _bodyFlagPercent;
			bodyFlagDuration = _bodyFlagDuration;
			eyeFlags = _eyeFlags;
			eyeFlagDuration = _eyeFlagDuration;
		}
	}

	public abstract class TransitionHandle{

		public enum TransitionType{
			DEFAULT,
			NO_MIRROR,
		}
		public readonly TransitionType type;

		public TransitionHandle(TransitionType _type){
			type = _type;
		}
		public abstract void Transition( Loli shinobu );
	}

	public sealed class DefaultTransition: TransitionHandle{

		public readonly Animation nextAnim;

		public DefaultTransition(Loli.Animation _nextAnim):base(TransitionType.DEFAULT){
			nextAnim = _nextAnim;
		}
		public override void Transition( Loli shinobu ){
			shinobu.UpdateAnimationTransition( nextAnim );
		}
	}

	public class TransitionToIdle: TransitionHandle{
		
		public TransitionToIdle():base(TransitionType.NO_MIRROR){
		}
		public override void Transition(Loli shinobu){
			shinobu.UpdateAnimationTransition( shinobu.GetLastReturnableIdleAnimation() );
		}
	}

	public class AnimationInfo{
		
		public enum Flag{
			NONE=0,
			IDLE_STATE=1,
			DISABLE_RAGDOLL_CHECK=2
		}

		public readonly Loli.Animation nextAnim;
		public readonly float nextAnimWaitTimeNorm;
		public readonly int legsStateID;
		public readonly int torsoStateID;
		public readonly int faceStateID;
		public readonly Priority priority;
		public readonly BodyState conditionBodyState;
		public readonly BodyState newBodyState;
		public readonly float transitionTime;
		public readonly AnimLogicInfo animLogicInfo;
		public readonly int flags;
		public readonly LoliAnimationEvent[] animationEvents;
		private int torsoSyncFloatID = 0;
		public readonly TransitionHandle transitionHandle = null;
		public readonly bool loopAnimationEvents;
		public readonly string legsStateName;
		public readonly string torsoStateName;
		public readonly string faceStateName;

		public AnimationInfo( TransitionHandle _transitionHandle ,
							  string _torsoStateName,
							  string _legsStateName,
							  string _faceStateName,
							  Priority _priority,
							  BodyState _conditionBodyState,
							  BodyState _newBodyState, 
							  float _transitionTime,
							  AnimLogicInfo _animLogicInfo,
							  LoliAnimationEvent[] _animEvents=null,
							  int _flags=0 ){
			torsoStateID = Animator.StringToHash( _torsoStateName );
			legsStateID = Animator.StringToHash( _legsStateName );	
			faceStateID = Animator.StringToHash( _faceStateName );

			loopAnimationEvents = _torsoStateName.Contains("_loop");
			
			transitionTime = _transitionTime;
			flags = _flags;
			transitionHandle = _transitionHandle;
			animationEvents = _animEvents;

			legsStateName = _legsStateName;
			torsoStateName = _torsoStateName;
			faceStateName = _faceStateName;
			priority = _priority;
			conditionBodyState = _conditionBodyState;
			newBodyState = _newBodyState;
			animLogicInfo = _animLogicInfo;
		}
		public bool HasFlag( Flag flag ){
			return (flags&(int)flag) != 0;
		}
		public void setTorsoSyncFloatID( int id ){
			torsoSyncFloatID = id;
		}
		public int GetTorsoSyncFloatID(){
			return torsoSyncFloatID;
		}
		public void Validate( Loli shinobu ){
			if( legsStateName != null ){
				if( !shinobu.animator.HasState(0,legsStateID) ){
					Debug.LogError("Legs state "+legsStateName+" doesn't exist");
				}
			}
			if( torsoStateName != null ){
				if( !shinobu.animator.HasState(1,torsoStateID) ){
					Debug.LogError("Torso state "+torsoStateName+" doesn't exist");
				}
			}
			if( faceStateName != null ){
				if( !shinobu.animator.HasState(2,faceStateID) ){
					Debug.LogError("Face state "+faceStateName+" doesn't exist");
				}
			}
		}
	}

	private void FixedUpdateAnimationEvents(){
		
		if( locomotion == null ){
			return;
		}
		rightLoliHandState.UpdateAnimationHoldBlendMap();
		leftLoliHandState.UpdateAnimationHoldBlendMap();
		//animation events only for torso
		AnimationInfo currAnimInfo = animationInfos[ m_currentAnim ];
		animationEventContext.UpdateAnimationEvents( currAnimInfo.animationEvents, currentAnimationLoops, GetLayerAnimNormTime(1) );
	}

	private void DebugAssert( LoliAnimationEvent animEvent, int minSize ){
#if UNITY_EDITOR
		if( minSize <= 0 ){
			if( animEvent.parameter != null ){
				Debug.LogError("Warning event parameters not null for "+animEvent.nameID);
			}
		}else{
			if( animEvent.parameter.Length != minSize ){
				Debug.LogError("ERROR event parameters not "+minSize+" for "+animEvent.nameID);
			}
		}
#endif
	}

	private void BindAwarenessModeToAnimation( AwarenessMode mode ){
		awarenessMode = mode;
		currentAnimAwarenessModeBinded = true;
	}

	public void HandleAnimationEvent( LoliAnimationEvent animEvent ){
		float[] parameters = animEvent.parameter;
		switch( animEvent.nameID ){
		case (int)AnimationEventName.BLEND_CONTROLLER_RIGHT:
			rightLoliHandState.SetupAnimationHoldBlendMap( parameters );
			break;
		case (int)AnimationEventName.BLEND_CONTROLLER_LEFT:
			leftLoliHandState.SetupAnimationHoldBlendMap( parameters );
			break;
		case (int)AnimationEventName.ATTEMPT_CATTAIL_HIT:
			DebugAssert( animEvent, -1 );
			active.cattail.SmackWithHandObject();
			break;
		case (int)AnimationEventName.REDISTRIBUTE_CHOPSTICKS_FINGERS:
			DebugAssert( animEvent, -1 );
			active.chopsticks.RedistributeChopsticksFingers();
			break;
		case (int)AnimationEventName.SET_BODY_FLAGS:
			DebugAssert( animEvent, 3 );
			setBodyVariables( (int)parameters[0], parameters[1], parameters[2] );
			break;
		case (int)AnimationEventName.SET_EYE_FLAGS:
			DebugAssert( animEvent, 2 );
			SetEyeVariables( (int)parameters[0], parameters[1] );
			break;
		case (int)AnimationEventName.WEAR_HAT_RIGHT_HAND:
			DebugAssert( animEvent, -1 );
			active.idle.AttachHandHat( (LoliHandState)rightHandState );
			break;
		case (int)AnimationEventName.WEAR_HAT_LEFT_HAND:
			DebugAssert( animEvent, -1 );
			active.idle.AttachHandHat( (LoliHandState)leftHandState );
			break;
		case (int)AnimationEventName.EAT_CURRENT_INTEREST:
			DebugAssert( animEvent, -1 );
			active.idle.EatTargetEdible();
			break;
		case (int)AnimationEventName.TOGGLE_FACE_YAW_SUM_FOR_ANIMATION:
			DebugAssert( animEvent, -1 );
			if( !currentAnimFaceYawToggle ){
				ApplyDisableFaceYaw( ref currentAnimFaceYawToggle );
			}else{
				RemoveDisableFaceYaw( ref currentAnimFaceYawToggle );
			}
			break;
		case (int)AnimationEventName.THROW_RIGHT_HAND_OBJECT:
			DebugAssert( animEvent, -1 );
			active.idle.ThrowHandObject( (LoliHandState)rightHandState );
			break;
		case (int)AnimationEventName.THROW_LEFT_HAND_OBJECT:
			DebugAssert( animEvent, -1 );
			active.idle.ThrowHandObject( (LoliHandState)leftHandState );
			break;
		case (int)AnimationEventName.SPAWN_POLAROID_FRAME_RIPPED_FX:
			DebugAssert( animEvent, -1 );
			active.idle.SpawnPolaroidRippedFX();
			break;
		case (int)AnimationEventName.SPEAK:
			DebugAssert( animEvent, 2 );
			Speak( (VoiceLine)parameters[0], parameters[1] == 1.0f );
			break;
		case (int)AnimationEventName.SPEAK_INTERVALS:
			DebugAssert( animEvent, 3 );
			SpeakAtRandomIntervals( (VoiceLine)parameters[0], parameters[1], parameters[2] );
			break;
		case (int)AnimationEventName.HEADPAT_PROPER_SOUND:
			DebugAssert( animEvent, -1 );
			passive.headpat.PlayProperHeadpatSound();
			break;
		case (int)AnimationEventName.FOOTSTEP_SOUND:
			DebugAssert( animEvent, -1 );
			if( locomotion.isMoveToActive() ){
				PlayFootstep();
			}
			break;
		case (int)AnimationEventName.BATHTUB_PLAY_SOUND:
			DebugAssert( animEvent, 1 );
			active.bathing.PlayBathtubSound( (Bathtub.SoundType)parameters[0] );
			break;
		case (int)AnimationEventName.BIND_ANIMATION_TO_VIEW_MODE:
			DebugAssert( animEvent, 1 );
			BindAwarenessModeToAnimation( (Loli.AwarenessMode)parameters[0] );
			break;
		// case (int)AnimationEventName.JUMP_ON_BED:
		// 	DebugAssert( animEvent, -1 );
		// 	active.sleeping.OnJumpOnBed();
		// 	break;
		// case (int)AnimationEventName.ROLL_ON_BED:
		// 	DebugAssert( animEvent, -1 );
		// 	active.sleeping.OnRollOnBed();
		// 	break;
		case (int)AnimationEventName.BEGIN_MOUNT:
			DebugAssert( animEvent, -1 );
			active.horseback.BeginMount();
			break;
		case (int)AnimationEventName.ATTACH_BAG_SHOULDER:
			DebugAssert( animEvent, -1 );
			active.idle.AttachBagOnShoulder();
			break;
		case (int)AnimationEventName.REMOVE_BAG_SHOULDER:
			DebugAssert( animEvent, -1 );
			active.idle.RemoveBagOnShoulder();
			break;
		case (int)AnimationEventName.OPEN_BAG_RIGHT:
			DebugAssert( animEvent, -1 );
			active.idle.OpenBag( true );
			break;
		case (int)AnimationEventName.OPEN_BAG_LEFT:
			DebugAssert( animEvent, -1 );
			active.idle.OpenBag( false );
			break;
		case (int)AnimationEventName.PUT_ITEM_IN_BAG_RIGHT:
			DebugAssert( animEvent, -1 );
			active.idle.PutItemInBag( true );
			break;
		case (int)AnimationEventName.PUT_ITEM_IN_BAG_LEFT:
			DebugAssert( animEvent, -1 );
			active.idle.PutItemInBag( false );
			break;
		case (int)AnimationEventName.DROP_RIGHT_HAND_ITEM:
			DebugAssert( animEvent, -1 );
			rightHandState.AttemptDrop();
			break;
		case (int)AnimationEventName.DROP_LEFT_HAND_ITEM:
			DebugAssert( animEvent, -1 );
			leftHandState.AttemptDrop();
			break;
		case (int)AnimationEventName.BATHTUB_SPLASH:
			DebugAssert( animEvent, -1 );
			active.bathing.Splash();
			break;
		case (int)AnimationEventName.EXECUTE_PICK_CARD_RIGHT:
			DebugAssert( animEvent, -1 );
			active.poker.PickSelectedCard( true );
			break;
		case (int)AnimationEventName.EXECUTE_PICK_CARD_LEFT:
			DebugAssert( animEvent, -1 );
			active.poker.PickSelectedCard( false );
			break;
		case (int)AnimationEventName.PLAY_CLAP_SOUND:
			DebugAssert( animEvent, -1 );
			PlayClapSound();
			break;
		case (int)AnimationEventName.STOP_SPINE_ANCHOR:
			DebugAssert( animEvent, -1 );
			StopAnchorSpineTransition();
			StopActiveAnchor();
			break;
		case (int)AnimationEventName.DISABLE_STAND_ON_GROUND:
			DebugAssert( animEvent, -1 );
			enableStandOnGround = false;
			break;
		}
	}
}

}