using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace viva{

using LoliAnimationEvent = AnimationEvent<float[]>;

public partial class Loli : Character{
    
	public class LocomotionInfo{

		public float maxSpeed;
		public float acceleration;
		public float faceYawMaxVel;
		public float faceYawAcc;

		public LocomotionInfo( float _maxSpeed, float _acceleration, float _faceYawMaxVel, float _faceYawAcc ){
			maxSpeed = _maxSpeed;
			acceleration = _acceleration;
			faceYawMaxVel = _faceYawMaxVel;
			faceYawAcc = _faceYawAcc;
		}
	}
	
	private static void RegisterLegSpeedInfo( string legStateName, float speed, float acc, float faceYawMaxVel, float faceYawAcc ){
		
		int stateHash = Animator.StringToHash( legStateName );
		// if( !animator.HasState(0,stateHash) ){
		// 	Debug.LogError("###ERROR### LegSpeedInfo does not exist! "+legStateName);
		// 	return;
		// }
		legSpeedInfos.Add( stateHash, new LocomotionInfo( speed*1.4f, acc, faceYawMaxVel, faceYawAcc ) );
	}

	public static LocomotionInfo GetLegSpeedInfo( Animation anim ){
		
		LocomotionInfo info = null;
		if( legSpeedInfos.TryGetValue( animationInfos[anim].legsStateID, out info ) ){
			return info;
		}
		return null;
	}

	public static Dictionary<Animation,AnimationInfo> animationInfos;
	public static Dictionary<int,LocomotionInfo> legSpeedInfos = new Dictionary<int,LocomotionInfo>();
    public static List<int> holdAnimationStates = new List<int>();
	
    
	private static void RegisterAnimation( Animation animation, AnimationInfo info, bool createMirroredLeft = false ){
		
		animationInfos.Add( animation, info );

		if( createMirroredLeft ){
			TransitionHandle transitionHandle;
			if( info.transitionHandle != null ){
				switch( info.transitionHandle.type ){
				case TransitionHandle.TransitionType.DEFAULT:
					transitionHandle = new DefaultTransition( GetMirroredLeftEnum( ((DefaultTransition)info.transitionHandle).nextAnim ) );
					break;
				default:
					transitionHandle = info.transitionHandle;
					break;
				}
			}else{
				transitionHandle = null;
			}
			animationInfos.Add( GetMirroredLeftEnum( animation ),
				new AnimationInfo(transitionHandle,
					info.torsoStateName.Replace("_right","_left"),
					info.legsStateName.Replace("_right","_left"),
					info.faceStateName.Replace("_right","_left"),
					info.priority,
					GetMirroredBodyState( info.conditionBodyState ),
					GetMirroredBodyState( info.newBodyState ),
					info.transitionTime,info.animLogicInfo,
					GetMirroredAnimationEvents( info.animationEvents ),
					info.flags
				)
			);
		}
	}
    
	private static Animation GetMirroredLeftEnum( Animation animation ){
		Animation nextEnum = (Animation)((int)animation+1);
		if( !nextEnum.ToString().EndsWith("_LEFT") ){
			// Debug.LogError("BAD ORDER. NEXT ENUM NOT LEFT: "+animation);
			return animation;
		}
		return nextEnum;
	}

	private static BodyState GetMirroredBodyState( BodyState bodyState ){
		int mirroredBodyState;
		if( ((BodyState)bodyState).ToString().Contains("_RIGHT") ){
			mirroredBodyState = (int)bodyState+1;
		}else if( ((BodyState)bodyState).ToString().Contains("_LEFT") ){
			mirroredBodyState = (int)bodyState-1;
		}else{
			mirroredBodyState = (int)bodyState;
		}
		return (BodyState)mirroredBodyState;
	}

	private static LoliAnimationEvent[] GetMirroredAnimationEvents( LoliAnimationEvent[] events ){
		if( events == null ){
			return null;
		}
		LoliAnimationEvent[] mirroredEvents = new LoliAnimationEvent[ events.Length ];
		for( int i=0; i<mirroredEvents.Length; i++ ){

			LoliAnimationEvent animEvent = events[i];
			int mirroredName;
			if( ((AnimationEventName)animEvent.nameID).ToString().Contains("_RIGHT") ){
				mirroredName = (int)animEvent.nameID+1;
			}else if( ((AnimationEventName)animEvent.nameID).ToString().Contains("_LEFT") ){
				mirroredName = (int)animEvent.nameID-1;
			}else{
				mirroredName = animEvent.nameID;
			}
			mirroredEvents[i] = new LoliAnimationEvent(
				animEvent.fireTimeNormalized,
				mirroredName,
				animEvent.parameter
			);
		}

		return mirroredEvents;
	}

    public static void GenerateAnimations(){

        if( animationInfos == null ){
            animationInfos = new Dictionary<Animation, AnimationInfo>();
        }

		for( int i=0; i<System.Enum.GetValues(typeof(Loli.HoldFormAnimation)).Length; i++ ){
			holdAnimationStates.Add( Animator.StringToHash( "LOLI_HOLD_FORM_"+((Loli.HoldFormAnimation)i) ) );
		}

		Loli.TransitionToIdle idleTransition = new Loli.TransitionToIdle();
		
		RegisterAnimation( Loli.Animation.NONE, new AnimationInfo(
			idleTransition,
			"APOSE","APOSE","APOSE",
			Priority.NONE, BodyState.STAND, BodyState.STAND, 0.01f, new AnimLogicInfo(), null
		));
		
		//Add entire animation list
		RegisterAnimation(Loli.Animation.BATHTUB_TEST_WATER_IN_RIGHT,
			new Loli.AnimationInfo(new BathingBehavior.TransitionToBathtubAnim(),
			"bathtub_test_water_in_right","bathtub_test_water_in_right","bathtub_test_water_in_right",
			Loli.Priority.MEDIUM,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(1,1.0f,0.5f,0,0.5f),
			null
		));
		RegisterAnimation(Loli.Animation.BATHTUB_TEST_WATER_IN_LEFT,
			new Loli.AnimationInfo(new BathingBehavior.TransitionToBathtubAnim(),
			"bathtub_test_water_in_left","bathtub_test_water_in_left","bathtub_test_water_in_left",
			Loli.Priority.MEDIUM,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(1,1.0f,0.5f,0,0.5f),
			null
		));
		RegisterAnimation(Loli.Animation.BATHTUB_TEST_WATER_IN_COLD_RIGHT,
			new Loli.AnimationInfo(idleTransition,
			"bathtub_test_water_in_cold_right","bathtub_test_water_in_cold_right","bathtub_test_water_in_cold_right",
			Loli.Priority.LOW,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(0,1.0f,0.5f,0,0.5f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.STARTLE_SHORT, 0.0f } )
			}
		));
		RegisterAnimation(Loli.Animation.BATHTUB_TEST_WATER_IN_COLD_LEFT,
			new Loli.AnimationInfo(idleTransition,
			"bathtub_test_water_in_cold_left","bathtub_test_water_in_cold_left","bathtub_test_water_in_cold_left",
			Loli.Priority.LOW,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(0,1.0f,0.5f,0,0.5f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.STARTLE_SHORT, 0.0f } )
			}
		));
		RegisterAnimation(Loli.Animation.BATHTUB_TEST_WATER_LUKEWARM_RIGHT,
			new Loli.AnimationInfo(idleTransition,
			"bathtub_test_water_lukewarm_right","bathtub_test_water_lukewarm_right","bathtub_test_water_lukewarm_right",
			Loli.Priority.LOW,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(0,1.0f,0.5f,0,0.5f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.MISC_IDLE, 0.0f } ),
				new LoliAnimationEvent( 0.5f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.MISC_IDLE, 0.0f } )
			}
		));
		RegisterAnimation(Loli.Animation.BATHTUB_TEST_WATER_LUKEWARM_LEFT,
			new Loli.AnimationInfo(idleTransition,
			"bathtub_test_water_lukewarm_left","bathtub_test_water_lukewarm_left","bathtub_test_water_lukewarm_left",
			Loli.Priority.LOW,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(0,1.0f,0.5f,0,0.5f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.MISC_IDLE, 0.0f } ),
				new LoliAnimationEvent( 0.5f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.MISC_IDLE, 0.0f } )
			}
		));
		RegisterAnimation(Loli.Animation.BATHTUB_TEST_WATER_IN_HOT_RIGHT,
			new Loli.AnimationInfo(idleTransition,
			"bathtub_test_water_in_hot_right","bathtub_test_water_in_hot_right","bathtub_test_water_in_hot_right",
			Loli.Priority.LOW,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(0,1.0f,0.5f,0,0.2f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.SCREAMING, 0.0f } )
			}
		));
		RegisterAnimation(Loli.Animation.BATHTUB_TEST_WATER_IN_HOT_LEFT,
			new Loli.AnimationInfo(idleTransition,
			"bathtub_test_water_in_hot_left","bathtub_test_water_in_hot_left","bathtub_test_water_in_hot_left",
			Loli.Priority.LOW,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(0,1.0f,0.5f,0,0.2f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.SCREAMING, 0.0f } )
			}
		));
		RegisterAnimation(Loli.Animation.STAND_POINT_OUT_IN_RIGHT,
			new Loli.AnimationInfo(new Loli.DefaultTransition(Loli.Animation.STAND_POINT_OUT_LOOP_RIGHT),
			"stand_point_out_in_right","stand_point_out_in_right","stand_point_out_in_right",
			Loli.Priority.MEDIUM,BodyState.STAND,BodyState.STAND,
			0.2f,new Loli.AnimLogicInfo(1,1.0f,0.4f,7,0.3f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.4f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.HUMPH, 0.0f } ),
			}
		),true);

		RegisterAnimation(Loli.Animation.STAND_POINT_OUT_LOOP_RIGHT,
			new Loli.AnimationInfo(null,
			"stand_point_out_loop_right","stand_point_out_loop_right","stand_point_out_loop_right",
			Loli.Priority.MEDIUM,BodyState.STAND,BodyState.STAND,
			0.2f,new Loli.AnimLogicInfo(1,1.0f,0.4f,7,0.3f),
			null,
			(int)Loli.AnimationInfo.Flag.IDLE_STATE
		),true);

		RegisterAnimation(Loli.Animation.STAND_POINT_OUT_SOUND_2_3_RIGHT,
			new Loli.AnimationInfo(new Loli.DefaultTransition(Loli.Animation.STAND_POINT_OUT_LOOP_RIGHT),
			"stand_point_out_sound_2_3_right","stand_point_out_sound_2_3_right","stand_point_out_sound_2_3_right",
			Loli.Priority.MEDIUM,BodyState.STAND,BodyState.STAND,
			0.2f,new Loli.AnimLogicInfo(1,1.0f,0.4f,12,0.3f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.GET_OUT_2_3, 1.0f } ),
			}
		),true );
		RegisterAnimation(Loli.Animation.STAND_POINT_OUT_TO_WAIT_RIGHT,
			new Loli.AnimationInfo(new Loli.DefaultTransition(Loli.Animation.STAND_WAIT_ANNOYED_LOOP),
			"stand_point_out_to_wait_right","stand_point_out_to_wait_right","stand_point_out_to_wait_right",
			Loli.Priority.MEDIUM,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(1,1.0f,0.4f,7,0.3f),
			null
		),true);
		RegisterAnimation(Loli.Animation.STAND_WAIT_ANNOYED_LOOP,
			new Loli.AnimationInfo(null,
			"stand_wait_annoyed_loop","stand_wait_annoyed_loop","stand_wait_annoyed_loop",
			Loli.Priority.MEDIUM,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(3,1.0f,0.5f,7,0.3f),
			null,
			(int)Loli.AnimationInfo.Flag.IDLE_STATE
		));
		RegisterAnimation(Loli.Animation.BATHTUB_RELAX_LOOP,
			new Loli.AnimationInfo(null,
			"bathtub_relax_loop","bathtub_relax_loop","bathtub_relax_loop",
			Loli.Priority.LOW,BodyState.BATHING_RELAX,BodyState.BATHING_RELAX,
			0.1f,new Loli.AnimLogicInfo(0,1.0f,0.5f,0,0.3f),
			null,
			(int)Loli.AnimationInfo.Flag.IDLE_STATE
		));
		RegisterAnimation(Loli.Animation.BATHTUB_RELAX_TO_HAPPY_IDLE,
			new Loli.AnimationInfo(new Loli.DefaultTransition(Loli.Animation.BATHTUB_HAPPY_IDLE_LOOP),
			"bathtub_relax_to_happy_idle","bathtub_relax_to_happy_idle","bathtub_relax_to_happy_idle",
			Loli.Priority.HIGH,BodyState.BATHING_RELAX,BodyState.BATHING_IDLE,
			0.1f,new Loli.AnimLogicInfo(0,1.0f,1.0f,0,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.BATHTUB_PLAY_SOUND, new float[]{ (float)Bathtub.SoundType.WATER_MOVEMENT } ),
				new LoliAnimationEvent( 0.4f, (int)Loli.AnimationEventName.BATHTUB_PLAY_SOUND, new float[]{ (float)Bathtub.SoundType.WATER_KERPLUNK } ),
				new LoliAnimationEvent( 0.7f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 12.0f,0.5f } ),
				new LoliAnimationEvent( 0.7f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 1.0f,1.0f, 0.5f } ),
			}
		));
		RegisterAnimation(Loli.Animation.BATHTUB_RELAX_TO_ANGRY_IDLE,
			new Loli.AnimationInfo(new Loli.DefaultTransition(Loli.Animation.BATHTUB_ANGRY_IDLE_LOOP),
			"bathtub_relax_to_angry_idle","bathtub_relax_to_angry_idle","bathtub_relax_to_angry_idle",
			Loli.Priority.HIGH,BodyState.BATHING_RELAX,BodyState.BATHING_IDLE,
			0.1f,new Loli.AnimLogicInfo(0,1.0f,0.6f,0,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.1f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.ANGRY_SHORT, 1.0f } ),
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.BATHTUB_PLAY_SOUND, new float[]{ (float)Bathtub.SoundType.WATER_MOVEMENT } ),
				new LoliAnimationEvent( 0.4f, (int)Loli.AnimationEventName.BATHTUB_PLAY_SOUND, new float[]{ (float)Bathtub.SoundType.WATER_KERPLUNK } ),
				new LoliAnimationEvent( 0.7f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 12.0f,0.5f } ),
				new LoliAnimationEvent( 0.7f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 1.0f,1.0f, 0.5f } ),
			}
		));
		RegisterAnimation(Loli.Animation.BATHTUB_HAPPY_IDLE_LOOP,
			new Loli.AnimationInfo(null,
			"bathtub_happy_idle_loop","bathtub_happy_idle_loop","bathtub_happy_idle_loop",
			Loli.Priority.LOW,BodyState.BATHING_IDLE,BodyState.BATHING_IDLE,
			0.3f,new Loli.AnimLogicInfo(3,1.0f,0.5f,7,0.5f),
			null,
			(int)Loli.AnimationInfo.Flag.IDLE_STATE
		));
		RegisterAnimation(Loli.Animation.BATHTUB_HAPPY_IDLE2,
			new Loli.AnimationInfo(idleTransition,
			"bathtub_happy_idle2","bathtub_happy_idle2","bathtub_happy_idle2",
			Loli.Priority.LOW,BodyState.BATHING_IDLE,BodyState.BATHING_IDLE,
			0.3f,new Loli.AnimLogicInfo(0,1.0f,0.6f,0,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.41f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 12.0f,0.25f } ),
				new LoliAnimationEvent( 0.55f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 0.0f,0.25f } ),
				new LoliAnimationEvent( 0.9f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 1.0f,1.0f, 0.5f } ),
				new LoliAnimationEvent( 0.9f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 12.0f,0.5f } ),
			}
		));
		RegisterAnimation(Loli.Animation.BATHTUB_HAPPY_SWITCH_SIDES,
			new Loli.AnimationInfo(new Loli.DefaultTransition( Loli.Animation.BATHTUB_HAPPY_IDLE_TO_ON_KNEES ),
			"bathtub_happy_switch_sides","bathtub_happy_switch_sides","bathtub_happy_switch_sides",
			Loli.Priority.HIGH,BodyState.BATHING_IDLE,BodyState.NONE,
			0.35f,new Loli.AnimLogicInfo(0,1.0f,0.5f,0,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.BATHTUB_PLAY_SOUND, new float[]{ (float)Bathtub.SoundType.WATER_MOVEMENT } ),
				new LoliAnimationEvent( 0.8f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 12.0f,0.5f } ),
				new LoliAnimationEvent( 0.8f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 1.0f,1.0f, 0.5f } ),
			}
		));
		RegisterAnimation(Loli.Animation.BATHTUB_ANGRY_IDLE_LOOP,
			new Loli.AnimationInfo(idleTransition,
			"bathtub_angry_idle_loop","bathtub_angry_idle_loop","bathtub_angry_idle_loop",
			Loli.Priority.LOW,BodyState.BATHING_IDLE,BodyState.BATHING_IDLE,
			0.3f,new Loli.AnimLogicInfo(1,0.6f,0.7f,7,0.5f),
			null,
			(int)Loli.AnimationInfo.Flag.IDLE_STATE
		));
		RegisterAnimation(Loli.Animation.BATHTUB_SINK_ANGRY,
			new Loli.AnimationInfo(new Loli.DefaultTransition(Loli.Animation.BATHTUB_ANGRY_IDLE_LOOP),
			"bathtub_sink_angry","bathtub_sink_angry","bathtub_sink_angry",
			Loli.Priority.MEDIUM,BodyState.BATHING_IDLE,BodyState.BATHING_IDLE,
			0.2f,new Loli.AnimLogicInfo(0,1.0f,0.3f,12,0.3f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.06f, (int)Loli.AnimationEventName.BATHTUB_PLAY_SOUND, new float[]{ (float)Bathtub.SoundType.WATER_SPLASH } ),
			}
		));
		RegisterAnimation(Loli.Animation.BATHTUB_HAPPY_IDLE3,
			new Loli.AnimationInfo(idleTransition,
			"bathtub_happy_idle3","bathtub_happy_idle3","bathtub_happy_idle3",
			Loli.Priority.LOW,BodyState.BATHING_IDLE,BodyState.BATHING_IDLE,
			0.4f,new Loli.AnimLogicInfo(0,1.0f,0.5f,0,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.BATHTUB_PLAY_SOUND, new float[]{ (float)Bathtub.SoundType.WATER_MOVEMENT } ),
				new LoliAnimationEvent( 0.27f, (int)Loli.AnimationEventName.BATHTUB_PLAY_SOUND, new float[]{ (float)Bathtub.SoundType.WATER_SPLASH } ),
				new LoliAnimationEvent( 0.1f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.MISC_IDLE, 1.0f } ),
				new LoliAnimationEvent( 0.82f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 12.0f,0.5f } ),
				new LoliAnimationEvent( 0.82f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 1.0f,1.0f, 0.5f } ),
				new LoliAnimationEvent( 0.84f, (int)Loli.AnimationEventName.BATHTUB_PLAY_SOUND, new float[]{ (float)Bathtub.SoundType.WATER_SPLASH } ),
			}
		));
		
		RegisterAnimation(Loli.Animation.STAND_POINT_OUT_ANTSY_RIGHT,
			new Loli.AnimationInfo(idleTransition,
			"stand_point_out_antsy_right","stand_point_out_antsy_right","stand_point_out_antsy_right",
			Loli.Priority.MEDIUM,BodyState.STAND,BodyState.STAND,
			0.4f,new Loli.AnimLogicInfo(1,1.0f,0.8f,12,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.07f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.ANGRY_GRUMBLE_LONG, 1.0f } ),
			}
		), true);

		RegisterAnimation(Loli.Animation.BATHTUB_HAPPY_IDLE4,
			new Loli.AnimationInfo(idleTransition,
			"bathtub_happy_idle4","bathtub_happy_idle4","bathtub_happy_idle4",
			Loli.Priority.LOW,BodyState.BATHING_IDLE,BodyState.BATHING_IDLE,
			0.4f,new Loli.AnimLogicInfo(0,1.0f,0.5f,0,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.BATHTUB_PLAY_SOUND, new float[]{ (float)Bathtub.SoundType.WATER_MOVEMENT } ),
				new LoliAnimationEvent( 0.15f, (int)Loli.AnimationEventName.BATHTUB_PLAY_SOUND, new float[]{ (float)Bathtub.SoundType.WATER_KERPLUNK } ),
				new LoliAnimationEvent( 0.82f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 12.0f,0.5f } ),
				new LoliAnimationEvent( 0.82f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 1.0f,1.0f, 0.5f } ),
				new LoliAnimationEvent( 0.27f, (int)Loli.AnimationEventName.BATHTUB_PLAY_SOUND, new float[]{ (float)Bathtub.SoundType.WATER_SPLASH } ),
				new LoliAnimationEvent( 0.5f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.RELIEF, 1.0f } ),
			}
		));
		RegisterAnimation(Loli.Animation.BATHTUB_WAVE_HAPPY_RIGHT,
			new Loli.AnimationInfo(idleTransition,
			"bathtub_wave_happy_right","bathtub_wave_happy_right","bathtub_wave_happy_right",
			Loli.Priority.LOW,BodyState.BATHING_IDLE,BodyState.BATHING_IDLE,
			0.3f,new Loli.AnimLogicInfo(3,0.7f,0.5f,0,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.BATHTUB_PLAY_SOUND, new float[]{ (float)Bathtub.SoundType.WATER_MOVEMENT } ),
				new LoliAnimationEvent( 0.11f, (int)Loli.AnimationEventName.BATHTUB_PLAY_SOUND, new float[]{ (float)Bathtub.SoundType.WATER_SPLASH } ),
				new LoliAnimationEvent( 0.125f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.GIGGLE, 1.0f } ),
				new LoliAnimationEvent( 0.69f, (int)Loli.AnimationEventName.BATHTUB_PLAY_SOUND, new float[]{ (float)Bathtub.SoundType.WATER_SPLASH } ),
			}
		));
		RegisterAnimation(Loli.Animation.BATHTUB_RELAX_HUMMING,
			new Loli.AnimationInfo(new Loli.DefaultTransition(Loli.Animation.BATHTUB_RELAX_LOOP),
			"bathtub_relax_humming","bathtub_relax_humming","bathtub_relax_humming",
			Loli.Priority.LOW,BodyState.BATHING_RELAX,BodyState.BATHING_RELAX,
			0.3f,new Loli.AnimLogicInfo(0,1.0f,0.4f,0,0.5f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.HUMMING, 1.0f } ),
			}
		));
		RegisterAnimation(Loli.Animation.BATHTUB_HAPPY_IDLE_TO_ON_KNEES,
			new Loli.AnimationInfo(new Loli.DefaultTransition(Loli.Animation.BATHTUB_ON_KNEES_LOOP),
			"bathtub_happy_idle_to_on_knees","bathtub_happy_idle_to_on_knees","bathtub_happy_idle_to_on_knees",
			Loli.Priority.LOW,BodyState.BATHING_IDLE,BodyState.BATHING_ON_KNEES,
			0.4f,new Loli.AnimLogicInfo(0,1.0f,0.6f,0,0.5f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.4f, (int)Loli.AnimationEventName.BATHTUB_PLAY_SOUND, new float[]{ (float)Bathtub.SoundType.WATER_SPLASH } ),
				new LoliAnimationEvent( 0.1f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.MISC_IDLE, 1.0f } ),
				new LoliAnimationEvent( 0.6f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 1.0f, 0.9f, 0.5f } ),
				new LoliAnimationEvent( 0.6f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 12.0f, 0.5f } ),
			}
		));
		RegisterAnimation(Loli.Animation.BATHTUB_ON_KNEES_TO_HAPPY_IDLE,
			new Loli.AnimationInfo(new Loli.DefaultTransition(Loli.Animation.BATHTUB_HAPPY_IDLE_LOOP),
			"bathtub_on_knees_to_happy_idle","bathtub_on_knees_to_happy_idle","bathtub_on_knees_to_happy_idle",
			Loli.Priority.MEDIUM,BodyState.BATHING_ON_KNEES,BodyState.BATHING_IDLE,
			0.3f,new Loli.AnimLogicInfo(0,1.0f,0.6f,0,0.5f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.1f, (int)Loli.AnimationEventName.BATHTUB_PLAY_SOUND, new float[]{ (float)Bathtub.SoundType.WATER_SPLASH } ),
				new LoliAnimationEvent( 0.5f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.MISC_IDLE, 1.0f } ),
				new LoliAnimationEvent( 0.8f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 12.0f,0.5f } ),
				new LoliAnimationEvent( 0.8f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 1.0f,0.7f, 0.6f } ),
			}
		));
		RegisterAnimation(Loli.Animation.BATHTUB_ON_KNEES_LOOP,
			new Loli.AnimationInfo(null,
			"bathtub_on_knees_loop","bathtub_on_knees_loop","bathtub_on_knees_loop",
			Loli.Priority.LOW,BodyState.BATHING_ON_KNEES,BodyState.BATHING_ON_KNEES,
			0.3f,new Loli.AnimLogicInfo(1,0.3f,0.7f,0,0.4f),
			null,
			(int)Loli.AnimationInfo.Flag.IDLE_STATE
		));

		RegisterAnimation(Loli.Animation.BATHTUB_HAPPY_BEG_LOOP,
			new Loli.AnimationInfo(null,
			"bathtub_happy_beg","bathtub_happy_beg","bathtub_happy_beg",
			Loli.Priority.LOW,BodyState.BATHING_ON_KNEES,BodyState.BATHING_ON_KNEES,
			0.3f,new Loli.AnimLogicInfo(3,0.9f,0.7f,7,0.4f)
		));
		
		RegisterAnimation(Loli.Animation.BATHTUB_ON_KNEES_TO_TOWEL_IDLE,
			new Loli.AnimationInfo(new Loli.DefaultTransition( Loli.Animation.BATHTUB_TOWEL_IDLE_LOOP ),
			"bathtub_on_knees_to_towel_idle","bathtub_on_knees_to_towel_idle","bathtub_on_knees_to_towel_idle",
			Loli.Priority.MEDIUM,BodyState.BATHING_ON_KNEES,BodyState.BATHING_STAND,
			0.3f,new Loli.AnimLogicInfo(1,0.8f,0.7f,7,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.1f, (int)Loli.AnimationEventName.BATHTUB_PLAY_SOUND, new float[]{ (float)Bathtub.SoundType.WATER_SPLASH } ),
				new LoliAnimationEvent( 0.6f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 3.0f, 0.7f, 0.5f } ),
			}
		));

		RegisterAnimation(Loli.Animation.BATHTUB_TOWEL_OUT_SOUND_2_3,
			new Loli.AnimationInfo(new Loli.DefaultTransition(Loli.Animation.BATHTUB_TOWEL_IDLE_LOOP),
			"bathtub_towel_out_sound_2_3","bathtub_towel_out_sound_2_3","bathtub_towel_out_sound_2_3",
			Loli.Priority.MEDIUM,BodyState.BATHING_STAND,BodyState.BATHING_STAND,
			0.2f,new Loli.AnimLogicInfo(3,1.0f,0.4f,12,0.3f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.GET_OUT_2_3, 1.0f } )
			}
		));

		RegisterAnimation(Loli.Animation.BATHTUB_TOWEL_IDLE_LOOP,
			new Loli.AnimationInfo(null,
			"bathtub_towel_idle_loop","bathtub_towel_idle_loop","bathtub_towel_idle_loop",
			Loli.Priority.LOW,BodyState.BATHING_STAND,BodyState.BATHING_STAND,
			0.3f,new Loli.AnimLogicInfo(3,0.8f,0.7f,12,0.4f),
			null,
			(int)Loli.AnimationInfo.Flag.IDLE_STATE
		));
		
		RegisterAnimation(Loli.Animation.BATHTUB_TOWEL_EMBARRASSED_TO_BATHTUB_IDLE,
			new Loli.AnimationInfo(new Loli.DefaultTransition(Loli.Animation.BATHTUB_ANGRY_IDLE_LOOP),
			"bathtub_towel_embarrassed_to_bathtub_idle","bathtub_towel_embarrassed_to_bathtub_idle","bathtub_towel_embarrassed_to_bathtub_idle",
			Loli.Priority.HIGH,BodyState.BATHING_STAND,BodyState.BATHING_IDLE,
			0.3f,new Loli.AnimLogicInfo(1,0.6f,0.3f,12,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.SCREAMING, 1.0f } ),
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.BATHTUB_PLAY_SOUND, new float[]{ (float)Bathtub.SoundType.WATER_MOVEMENT } ),
				new LoliAnimationEvent( 0.7f, (int)Loli.AnimationEventName.BATHTUB_PLAY_SOUND, new float[]{ (float)Bathtub.SoundType.WATER_KERPLUNK } ),
			}
		));

		RegisterAnimation(Loli.Animation.STAND_HAPPY_BEG_START,
			new Loli.AnimationInfo(new Loli.DefaultTransition(Loli.Animation.STAND_HAPPY_BEG_LOCOMOTION),
			"stand_happy_beg_start","stand_happy_beg_start","stand_happy_beg_start",
			Loli.Priority.MEDIUM,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(1,1.0f,0.5f,12,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.000f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.IMPRESSED, 0.0f } ),
			}
		));

		RegisterAnimation(Loli.Animation.STAND_HAPPY_BEG_LOCOMOTION,
			new Loli.AnimationInfo(null,
			"stand_happy_beg_loop","stand_locomotion_spine_stable","stand_happy_beg_loop",
			Loli.Priority.LOW,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(1,0.8f,0.5f,12,0.32f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.141f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 0.0f, 0.6f } ),
				new LoliAnimationEvent( 0.185f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 12.0f, 0.5f } ),
				new LoliAnimationEvent( 0.280f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 0.0f, 1.0f, 0.7f } ),
				new LoliAnimationEvent( 0.280f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 0.0f, 0.7f } ),
				new LoliAnimationEvent( 0.361f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 1.0f, 1.0f, 0.6f } ),
				new LoliAnimationEvent( 0.361f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 12.0f, 0.6f } ),
				new LoliAnimationEvent( 0.400f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 0.0f, 1.0f, 0.6f } ),
				new LoliAnimationEvent( 0.400f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 0.0f, 0.6f } ),
				new LoliAnimationEvent( 0.551f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 1.0f, 1.0f, 0.6f } ),
				new LoliAnimationEvent( 0.551f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 12.0f, 0.6f } ),
				new LoliAnimationEvent( 0.811f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 0.0f, 1.0f, 0.7f } ),
				new LoliAnimationEvent( 0.811f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 0.0f, 0.5f } ),
				new LoliAnimationEvent( 0.890f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 1.0f, 1.0f, 1.0f } ),
				new LoliAnimationEvent( 0.890f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 12.0f, 0.4f } ),

				new LoliAnimationEvent( 0.000f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.WHINE_SOFT, 1.0f } ),
				new LoliAnimationEvent( 0.356f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.WHINE, 1.0f } ),
				new LoliAnimationEvent( 0.800f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.WHINE_SOFT, 1.0f } ),
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.FOOTSTEP_SOUND, null ),
				new LoliAnimationEvent( 0.5f, (int)Loli.AnimationEventName.FOOTSTEP_SOUND, null ),
			}
		));

		RegisterAnimation(Loli.Animation.STAND_HAPPY_BEG_END,
			new Loli.AnimationInfo(idleTransition,
			"stand_happy_beg_end","stand_happy_beg_end","stand_happy_beg_end",
			Loli.Priority.MEDIUM,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(3,0.6f,0.5f,7,0.27997f),
			null
		));

		RegisterAnimation(Loli.Animation.STAND_HAPPY_IDLE1,
			new Loli.AnimationInfo(null,"stand_happy_loop","stand_happy_loop","stand_happy_idle",
			Loli.Priority.LOW,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(3,1.0f,0.5f,7,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.FOOTSTEP_SOUND, null ),
				new LoliAnimationEvent( 0.5f, (int)Loli.AnimationEventName.FOOTSTEP_SOUND, null ),
			},
			(int)Loli.AnimationInfo.Flag.IDLE_STATE
		));
		animationInfos[ Loli.Animation.STAND_HAPPY_IDLE1 ].setTorsoSyncFloatID( Animator.StringToHash("sync_locomotion_happy") );
			
		RegisterAnimation(Loli.Animation.STAND_HAPPY_IDLE2,
			new Loli.AnimationInfo(new Loli.DefaultTransition(Loli.Animation.STAND_HAPPY_IDLE1),
			"stand_idle2_happy","stand_locomotion_spine_stable","stand_idle2_happy",
			Loli.Priority.LOW,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(3,1.0f,0.5f,7,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.FOOTSTEP_SOUND, null ),
				new LoliAnimationEvent( 0.5f, (int)Loli.AnimationEventName.FOOTSTEP_SOUND, null ),
			},
			(int)Loli.AnimationInfo.Flag.IDLE_STATE
		));
		
		RegisterAnimation(Loli.Animation.STAND_HAPPY_IDLE3,
			new Loli.AnimationInfo(new Loli.DefaultTransition(Loli.Animation.STAND_HAPPY_IDLE1),
			"stand_happy_idle3","stand_locomotion_spine_stable","stand_happy_idle3",
			Loli.Priority.LOW,BodyState.STAND,BodyState.STAND,
			0.1f,new Loli.AnimLogicInfo(1,0.3f,0.5f,12,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.FOOTSTEP_SOUND, null ),
				new LoliAnimationEvent( 0.5f, (int)Loli.AnimationEventName.FOOTSTEP_SOUND, null ),
				new LoliAnimationEvent( 0.6f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 3.0f, 0.9f, 1.1f } ),
				new LoliAnimationEvent( 0.99f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 1.0f, 0.3f, 1.1f } ),
			},
			(int)Loli.AnimationInfo.Flag.IDLE_STATE
		));
		
		RegisterAnimation(Loli.Animation.STAND_CONFUSED,
			new Loli.AnimationInfo(idleTransition,
			"stand_confused","stand_confused","stand_confused",
			Loli.Priority.LOW,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(1,0.6f,0.3f,12,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.1f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.CONFUSED, 0.0f } ),
			}
		));
		
		RegisterAnimation(Loli.Animation.STAND_HAPPY_SOCIAL1,
			new Loli.AnimationInfo(idleTransition,
			"stand_happy_social1","stand_happy_social1","stand_happy_social1",
			Loli.Priority.LOW,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(1,0.6f,0.3f,12,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.1f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.MISC_IDLE, 0.0f } ),
				new LoliAnimationEvent( 0.4f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.MISC_IDLE, 0.0f } ),
				new LoliAnimationEvent( 0.7f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.MISC_IDLE, 0.0f } ),
			},
			(int)Loli.AnimationInfo.Flag.IDLE_STATE
		));

		RegisterAnimation(Loli.Animation.STAND_ANGRY_IDLE1,
			new Loli.AnimationInfo(null,"stand_angry_loop","stand_angry_loop","stand_angry_idle",
			Loli.Priority.LOW,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(3,1.0f,0.5f,7,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.FOOTSTEP_SOUND, null ),
				new LoliAnimationEvent( 0.5f, (int)Loli.AnimationEventName.FOOTSTEP_SOUND, null ),
			},
			(int)Loli.AnimationInfo.Flag.IDLE_STATE
		));
		animationInfos[ Loli.Animation.STAND_ANGRY_IDLE1 ].setTorsoSyncFloatID( Animator.StringToHash("sync_locomotion_angry") );

        RegisterAnimation(Loli.Animation.STAND_AGREE,
			new Loli.AnimationInfo(idleTransition,
			"stand_agree","stand_locomotion_spine_stable","floor_sit_agree",
			Loli.Priority.LOW,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(1,0.5f,0.3f,12,0.3f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.1f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.GIGGLE, 0.0f } ),
			}
		));

		RegisterAnimation(Loli.Animation.STAND_REFUSE,
			new Loli.AnimationInfo(idleTransition,
			"stand_angry_refuse","stand_angry_refuse","stand_angry_refuse",
			Loli.Priority.MEDIUM,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(0,1.0f,0.65f,12,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.REFUSE, 0.0f } ),
				new LoliAnimationEvent( 0.7f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 3.0f, 1.0f, 1.1f } ),
			}
		));

		RegisterAnimation(Loli.Animation.STAND_TIRED_REFUSE,
			new Loli.AnimationInfo(idleTransition,
			"stand_tired_refuse","stand_tired_refuse","stand_tired_refuse",
			Loli.Priority.MEDIUM,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(0,1.0f,0.65f,12,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.SLEEP_DISTURBED_SHORT, 0.0f } ),
				new LoliAnimationEvent( 0.7f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 3.0f, 0.5f, 0.7f } ),
			}
		));

		RegisterAnimation(Loli.Animation.STAND_STRETCH,
			new Loli.AnimationInfo(idleTransition,
			"stand_stretch","stand_stretch","stand_stretch",
			Loli.Priority.MEDIUM,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(0,1.0f,0.5f,0,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.SLEEP_DISTURBED_SHORT, 0.0f } ),
				new LoliAnimationEvent( 0.8f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 3.0f, 0.5f, 0.5f } ),
				new LoliAnimationEvent( 0.8f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 12.0f, 0.4f } ),
			},
			(int)Loli.AnimationInfo.Flag.IDLE_STATE
		));

		RegisterAnimation(Loli.Animation.STAND_POSE_PEACE_IN,
			new Loli.AnimationInfo(new Loli.DefaultTransition(Loli.Animation.STAND_POSE_PEACE_LOOP),
			"stand_pose_peace_in","stand_pose_peace_in","stand_pose_peace_in",
			Loli.Priority.LOW,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(0,1.0f,0.55f,0,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.1f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.PEACE, 0.0f } ),
			}
		));
		
		RegisterAnimation(Loli.Animation.STAND_POSE_PEACE_LOOP,
			new Loli.AnimationInfo(null,
			"stand_pose_peace_loop","stand_pose_peace_loop","stand_pose_peace_loop",
			Loli.Priority.LOW,BodyState.STAND,BodyState.STAND,
			0.2f,new Loli.AnimLogicInfo(0,1.0f,0.55f,0,0.4f),null
		));

		RegisterAnimation(Loli.Animation.STAND_HAPPY_TAKE_PHOTO_IN,
			new Loli.AnimationInfo(new Loli.DefaultTransition(Loli.Animation.STAND_HAPPY_TAKE_PHOTO_LOOP),
			"stand_happy_take_photo_in","stand_happy_take_photo_in","stand_happy_take_photo_in",
			Loli.Priority.MEDIUM,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(0,1.0f,0.5f,0,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{(float)Loli.VoiceLine.MISC_IDLE,0.0f} ),
			}
		));
		
		RegisterAnimation(Loli.Animation.STAND_HAPPY_TAKE_PHOTO_LOOP,
			new Loli.AnimationInfo(null,
			"stand_happy_take_photo_loop","stand_happy_take_photo_loop","stand_happy_take_photo_loop",
			Loli.Priority.LOW,BodyState.STAND,BodyState.STAND,
			0.4f,new Loli.AnimLogicInfo(1,0.1f,1.0f,7,1.0f)
		));

		RegisterAnimation(Loli.Animation.STAND_HAPPY_TAKE_PHOTO_OUT_RIGHT,
			new Loli.AnimationInfo(null,
			"stand_happy_take_photo_out_right_nm","stand_happy_take_photo_out_right_nm","stand_happy_take_photo_out_right_nm",
			Loli.Priority.LOW,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(3,1.0f,0.6f,7,0.5f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{(float)Loli.VoiceLine.THINKING_SHORT,0.0f} ),
			}
		));

		RegisterAnimation(Loli.Animation.STAND_HAPPY_TAKE_PHOTO_OUT_LEFT,
			new Loli.AnimationInfo(null,
			"stand_happy_take_photo_out_left_nm","stand_happy_take_photo_out_left_nm","stand_happy_take_photo_out_left_nm",
			Loli.Priority.LOW,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(3,1.0f,0.6f,7,0.5f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{(float)Loli.VoiceLine.THINKING_SHORT,0.0f} ),
			}
		));

		RegisterAnimation(Loli.Animation.STAND_CATTAIL_IDLE1_RIGHT,
			new Loli.AnimationInfo(idleTransition,
			"stand_cattail_idle1_right","stand_cattail_idle1_right","stand_cattail_idle1_right",
			Loli.Priority.MEDIUM,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(0,1.0f,0.4f,0,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.000f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.HUMMING, 1.0f } ),
				new LoliAnimationEvent( 0.272f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.CONFUSED, 1.0f } ),
				new LoliAnimationEvent( 0.606f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.TROUBLED, 1.0f } ),
			}
		),true);
		
		RegisterAnimation(Loli.Animation.STAND_CATTAIL_SWING_RIGHT,
			new Loli.AnimationInfo(idleTransition,
			"stand_cattail_swing_right","stand_cattail_swing_right","stand_cattail_swing_right",
			Loli.Priority.HIGH,BodyState.STAND,BodyState.STAND,
			0.35f,new Loli.AnimLogicInfo(1,0.4f,0.5f,12,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.05f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.EFFORT_ANGRY, 0.0f } ),
				new LoliAnimationEvent( 0.45f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.EFFORT_ANGRY, 0.0f } ),
				new LoliAnimationEvent( 0.349f, (int)Loli.AnimationEventName.ATTEMPT_CATTAIL_HIT, null ),
				new LoliAnimationEvent( 0.651f, (int)Loli.AnimationEventName.ATTEMPT_CATTAIL_HIT, null ),
			}
		),true);

		RegisterAnimation(Loli.Animation.FLOOR_SIT_CHOPSTICKS_IN,
			new Loli.AnimationInfo(new Loli.DefaultTransition( Loli.Animation.FLOOR_SIT_CHOPSTICKS_IDLE),
			"sit_floor_chopsticks_in","sit_floor_chopsticks_in","sit_floor_chopsticks_idle",
			Loli.Priority.MEDIUM, BodyState.FLOOR_SIT,BodyState.FLOOR_SIT,
			0.2f,new Loli.AnimLogicInfo(1,0.6f,0.5f,7,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.MISC_IDLE, 0.0f } )
			}
		));

		RegisterAnimation(Loli.Animation.FLOOR_SIT_CHOPSTICKS_IDLE,
			new Loli.AnimationInfo(null,
			"sit_floor_chopsticks_idle","sit_floor_chopsticks_idle","sit_floor_chopsticks_idle",
			Loli.Priority.MEDIUM, BodyState.FLOOR_SIT,BodyState.FLOOR_SIT,
			0.2f,new Loli.AnimLogicInfo(1,0.6f,1.0f,7,0.6f),
			null
		));

		RegisterAnimation(Loli.Animation.FLOOR_SIT_HYPE,
			new Loli.AnimationInfo(new Loli.DefaultTransition(Loli.Animation.FLOOR_SIT_LOCOMOTION_HAPPY),
			"sit_floor_hype","sit_floor_hype","sit_floor_hype",
			Loli.Priority.MEDIUM, BodyState.FLOOR_SIT,BodyState.FLOOR_SIT,
			0.2f,new Loli.AnimLogicInfo(1,0.5f,0.5f,12,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.HEADPAT_HAPPY, 0.0f } )
			}
		));

		RegisterAnimation(Loli.Animation.FLOOR_SIT_CHOPSTICKS_WORRIED,
			new Loli.AnimationInfo(new Loli.DefaultTransition( Loli.Animation.FLOOR_SIT_CHOPSTICKS_IDLE),
			"sit_floor_chopsticks_worried","sit_floor_chopsticks_worried","sit_floor_chopsticks_worried",
			Loli.Priority.MEDIUM, BodyState.FLOOR_SIT,BodyState.FLOOR_SIT,
			0.2f,new Loli.AnimLogicInfo(0,1.0f,0.5f,0,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.WORRY, 0.0f } )
			}
		));

		RegisterAnimation(Loli.Animation.FLOOR_SIT_CHOPSTICKS_NEUTRAL,
			new Loli.AnimationInfo(new Loli.DefaultTransition( Loli.Animation.FLOOR_SIT_CHOPSTICKS_IDLE),
			"sit_floor_chopsticks_neutral","sit_floor_chopsticks_neutral","sit_floor_chopsticks_neutral",
			Loli.Priority.MEDIUM, BodyState.FLOOR_SIT,BodyState.FLOOR_SIT,
			0.25f,new Loli.AnimLogicInfo(0,1.0f,0.5f,0,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.05f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.THINKING_LONG, 0.0f } ),
				new LoliAnimationEvent( 0.5f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.THINKING_LONG, 0.0f } ),
				new LoliAnimationEvent( 0.75f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 1.0f, 1.0f, 0.6f } ),
				new LoliAnimationEvent( 0.75f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 12.0f, 0.6f } )
			}
		));

		RegisterAnimation(Loli.Animation.FLOOR_SIT_CHOPSTICKS_CONFIDENT,
			new Loli.AnimationInfo(new Loli.DefaultTransition( Loli.Animation.FLOOR_SIT_CHOPSTICKS_IDLE),
			"sit_floor_chopsticks_confident","sit_floor_chopsticks_confident","sit_floor_chopsticks_confident",
			Loli.Priority.MEDIUM, BodyState.FLOOR_SIT,BodyState.FLOOR_SIT,
			0.2f,new Loli.AnimLogicInfo(1,0.8f,0.5f,12,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.CONFIDENT, 0.0f } )
			}
		));

		RegisterAnimation(Loli.Animation.FLOOR_SIT_CHOPSTICKS_LOSE,
			new Loli.AnimationInfo(new Loli.DefaultTransition( Loli.Animation.FLOOR_SIT_TO_STAND),
			"sit_floor_chopsticks_lose","sit_floor_chopsticks_lose","sit_floor_chopsticks_lose",
			Loli.Priority.MEDIUM, BodyState.FLOOR_SIT,BodyState.FLOOR_SIT,
			0.2f,new Loli.AnimLogicInfo(0,1.0f,0.5f,12,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.LOSE, 0.0f } )
			}
		));

		RegisterAnimation(Loli.Animation.FLOOR_SIT_CHOPSTICKS_WIN,
			new Loli.AnimationInfo(new Loli.DefaultTransition( Loli.Animation.FLOOR_SIT_LOCOMOTION_HAPPY),
			"sit_floor_chopsticks_win","sit_floor_chopsticks_win","sit_floor_chopsticks_win",
			Loli.Priority.MEDIUM, BodyState.FLOOR_SIT,BodyState.FLOOR_SIT,
			0.2f,new Loli.AnimLogicInfo(0,1.0f,0.5f,0,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.WIN, 0.0f } ),
				new LoliAnimationEvent( 0.7f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 1.0f, 1.0f, 0.8f } ),
				new LoliAnimationEvent( 0.7f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 12.0f, 0.3f } ),
			}
		));
			
		RegisterAnimation(Loli.Animation.FLOOR_SIT_CHOPSTICKS_RECEIVE_RIGHT,
		new Loli.AnimationInfo(new Loli.DefaultTransition( Loli.Animation.FLOOR_SIT_CHOPSTICKS_IDLE),
			"sit_floor_chopsticks_receive_right","sit_floor_chopsticks_idle","sit_floor_chopsticks_idle",
			Loli.Priority.MEDIUM, BodyState.FLOOR_SIT,BodyState.FLOOR_SIT,
			0.2f,null,
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.MISC_IDLE, 0.0f } )
			}
		));

		RegisterAnimation(Loli.Animation.FLOOR_SIT_CHOPSTICKS_RECEIVE_LEFT,
			new Loli.AnimationInfo(new Loli.DefaultTransition( Loli.Animation.FLOOR_SIT_CHOPSTICKS_IDLE),
			"sit_floor_chopsticks_receive_left","sit_floor_chopsticks_idle","sit_floor_chopsticks_idle",
			Loli.Priority.MEDIUM, BodyState.FLOOR_SIT,BodyState.FLOOR_SIT,
			0.2f,null,
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.MISC_IDLE, 0.0f } )
			}
		));

		RegisterAnimation(Loli.Animation.FLOOR_SIT_CHOPSTICKS_REDISTRIBUTE,
			new Loli.AnimationInfo(new Loli.DefaultTransition( Loli.Animation.FLOOR_SIT_CHOPSTICKS_IDLE),
			"sit_floor_chopsticks_redistribute","sit_floor_chopsticks_idle","sit_floor_chopsticks_idle",
			Loli.Priority.MEDIUM, BodyState.FLOOR_SIT,BodyState.FLOOR_SIT,
			0.2f,null,
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.35f, (int)Loli.AnimationEventName.REDISTRIBUTE_CHOPSTICKS_FINGERS, null ),
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.MISC_IDLE, 0.0f } )
			}
		));

		RegisterAnimation(Loli.Animation.STAND_HAPPY_JOY_JUMPS,
			new Loli.AnimationInfo(new Loli.DefaultTransition( Loli.Animation.STAND_HAPPY_IDLE1),
			"stand_happy_joy_jumps","stand_happy_joy_jumps","stand_happy_joy_jumps",
			Loli.Priority.LOW,BodyState.STAND,BodyState.STAND,
			0.4f,new Loli.AnimLogicInfo(0,1.0f,0.75f,0,0.4f),null
		));

		RegisterAnimation(Loli.Animation.STAND_HAPPY_DISAPPOINTMENT,	//todo: separate animation?
			new Loli.AnimationInfo(idleTransition,
			"stand_headpat_happy_wanted_more","stand_happy_loop","stand_headpat_happy_wanted_more",
			Loli.Priority.MEDIUM,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(3,1.0f,0.5f,0,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.DISAPPOINTED_STARTLE, 0.0f } ),
				new LoliAnimationEvent( 0.3f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.DISAPPOINTED_SIGH, 0.0f } ),
				new LoliAnimationEvent( 0.3f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 0.0f, 1.0f, 0.8f } ),
				new LoliAnimationEvent( 0.3f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 0.0f, 0.3f } ),
				new LoliAnimationEvent( 0.8f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 3.0f, 1.0f, 0.8f } ),
				new LoliAnimationEvent( 0.9f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 12.0f, 0.3f } ),
			}
		));
		
		RegisterAnimation(Loli.Animation.STAND_WAVE_HAPPY_RIGHT,
			new Loli.AnimationInfo(idleTransition,
			"stand_wave_happy_right","stand_wave_happy_right","stand_wave_happy_right",
			Loli.Priority.MEDIUM,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(1,0.5f,0.65f,12,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.1f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.GIGGLE, 0.0f } ),
			}
		));
		
		RegisterAnimation(Loli.Animation.STAND_WAVE_HAPPY_LEFT,
			new Loli.AnimationInfo(idleTransition,
			"stand_wave_happy_left","stand_wave_happy_left","stand_wave_happy_left",
			Loli.Priority.MEDIUM,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(1,0.5f,0.65f,12,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.1f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.GIGGLE, 0.0f } ),
			}
		));
		
		RegisterLegSpeedInfo( "stand_giddy_loop", 2.4f, 2.0f, 255.0f, 30.0f );


		RegisterAnimation(Loli.Animation.STAND_REACH_OUT_RIGHT,
			new Loli.AnimationInfo(new Loli.DefaultTransition(Loli.Animation.STAND_REACH_OUT_END_RIGHT),
			"stand_reach_out_right","stand_reach_out_right","stand_reach_out_right",
			Loli.Priority.LOW,BodyState.STAND,BodyState.STAND,
			0.1f,new Loli.AnimLogicInfo(3,0.6f,0.5f,7,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.554f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 0.0f, 0.4f } ),
				new LoliAnimationEvent( 0.554f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 0.0f, 1.0f, 0.6f } ),
				new LoliAnimationEvent( 0.679f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 7.0f, 0.4f } ),
				new LoliAnimationEvent( 0.679f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 3.0f, 0.6f, 0.6f } ),
			}
		),true);

		RegisterAnimation(Loli.Animation.STAND_REACH_OUT_END_RIGHT,
			new Loli.AnimationInfo(idleTransition,
			"stand_reach_out_end_right","stand_reach_out_end_right","stand_reach_out_end_right",
			Loli.Priority.HIGH,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(3,0.6f,0.5f,7,0.4f),
			null
		),true);

		RegisterAnimation(Loli.Animation.STAND_TO_HORSEBACK_IDLE_RIGHT,
			new Loli.AnimationInfo(idleTransition,
			"stand_to_horseback_Idle_right","stand_to_horseback_Idle_right","stand_to_horseback_Idle_right",
			Loli.Priority.MEDIUM,BodyState.STAND,BodyState.HORSEBACK,
			0.3f,new Loli.AnimLogicInfo(0,1.0f,0.3f,12,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.2f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.EFFORT, 0.0f } ),
				new LoliAnimationEvent( 0.8f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 3.0f, 0.9f, 0.5f } ),
				new LoliAnimationEvent( 0.8f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 7.0f, 0.4f } ),
				new LoliAnimationEvent( 0.9f, (int)Loli.AnimationEventName.BEGIN_MOUNT, null )
			}
		), true );

		RegisterAnimation(Loli.Animation.HORSEBACK_IDLE_LOOP,
			new Loli.AnimationInfo(null,
			"horseback_Idle_loop","horseback_Idle_loop","horseback_Idle_loop",
			Loli.Priority.LOW,BodyState.HORSEBACK,BodyState.HORSEBACK,
			0.3f,new Loli.AnimLogicInfo(3,0.9f,0.5f,7,0.4f),null,
			(int)Loli.AnimationInfo.Flag.IDLE_STATE
		) );

		RegisterAnimation(Loli.Animation.HORSEBACK_HARD_STOP,
			new Loli.AnimationInfo(idleTransition,
			"horseback_hard_stop","horseback_hard_stop","horseback_hard_stop",
			Loli.Priority.HIGH,BodyState.HORSEBACK,BodyState.HORSEBACK,
			0.3f,new Loli.AnimLogicInfo(0,0.9f,0.1f,12,0.1f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.SCARED_SHORT, 0.0f } ),
			}
		) );

		RegisterAnimation(Loli.Animation.HORSEBACK_JOY,
			new Loli.AnimationInfo(idleTransition,
			"horseback_joy","horseback_joy","horseback_joy",
			Loli.Priority.HIGH,BodyState.HORSEBACK,BodyState.HORSEBACK,
			0.3f,new Loli.AnimLogicInfo(0,0.9f,0.4f,12,0.1f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.1f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.ECSTATIC, 0.0f } ),
			}
		) );
		
		RegisterAnimation(Loli.Animation.HORSEBACK_TO_STAND,
			new Loli.AnimationInfo(idleTransition,
			"horseback_to_stand","horseback_to_stand","stand_happy_idle",
			Loli.Priority.LOW,BodyState.HORSEBACK,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(0,0.9f,0.4f,12,0.1f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.STOP_SPINE_ANCHOR, null ),
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.DISABLE_STAND_ON_GROUND, null ),
				new LoliAnimationEvent( 0.1f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.MISC_IDLE, 0.0f } ),
			}
		) );

		RegisterAnimation(Loli.Animation.STAND_ANGRY_THROW_RIGHT,
			new Loli.AnimationInfo(idleTransition,
			"stand_angry_throw_right","stand_angry_throw_right","stand_angry_throw_right",
			Loli.Priority.HIGH,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(1,0.4f,0.5f,12,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.400f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.EFFORT, 0.0f } ),
				new LoliAnimationEvent( 0.483f, (int)Loli.AnimationEventName.THROW_RIGHT_HAND_OBJECT, null ),
			}
		), true);

		RegisterAnimation(Loli.Animation.STAND_HAPPY_SMELL_ITEM_RIGHT,
			new Loli.AnimationInfo(idleTransition,
			"stand_happy_donut_smell_right","stand_happy_donut_smell_right","stand_happy_donut_smell_right",
			Loli.Priority.LOW,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(0,1.0f,0.45f,0,0.36f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.200f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.DONUT_SMELL_A, 1.0f } ),
				new LoliAnimationEvent( 0.573f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.DONUT_SMELL_B, 1.0f } ),
				new LoliAnimationEvent( 0.927f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 3.0f, 1.0f, 0.6f } ),
				new LoliAnimationEvent( 0.927f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 12.0f, 0.4f } ),
			}
		),true);

		RegisterAnimation(Loli.Animation.STAND_HAPPY_EAT_ITEM_RIGHT,
			new Loli.AnimationInfo(idleTransition
			,"stand_happy_donut_eat_right","stand_locomotion_spine_stable","stand_happy_donut_eat_right",
			Loli.Priority.LOW,BodyState.STAND,BodyState.STAND,
			0.4f,new Loli.AnimLogicInfo(0,1.0f,0.55f,0,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.080f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.EAT, 1.0f } ),
				new LoliAnimationEvent( 0.129f, (int)Loli.AnimationEventName.EAT_CURRENT_INTEREST, null ),
				new LoliAnimationEvent( 0.800f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 3.0f, 1.0f, 1.0f } ),
				new LoliAnimationEvent( 0.818f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 7.0f, 0.7f } ),
			}
		),true);

		RegisterAnimation(Loli.Animation.STAND_HAPPY_DONUT_LAST_BITE_RIGHT,
			new Loli.AnimationInfo(idleTransition
			,"stand_happy_donut_last_bite_right","stand_locomotion_spine_stable","stand_happy_donut_last_bite_right",
			Loli.Priority.LOW,BodyState.STAND,BodyState.STAND,
			0.4f,new Loli.AnimLogicInfo(0,1.0f,0.5f,0,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.080f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.EAT, 1.0f } ),
				new LoliAnimationEvent( 0.127f, (int)Loli.AnimationEventName.EAT_CURRENT_INTEREST, null )
			}
		),true);

		RegisterAnimation(Loli.Animation.STAND_SHINOBU_YUMMY,
			new Loli.AnimationInfo(idleTransition
			,"stand_shinobu_yummy","stand_shinobu_yummy","stand_shinobu_yummy",
			Loli.Priority.LOW,BodyState.STAND,BodyState.STAND,
			0.4f,new Loli.AnimLogicInfo(0,1.0f,0.5f,0,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.080f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.YUMMY, 1.0f } ),
			}
		));

		RegisterAnimation(Loli.Animation.STAND_WEAR_SUNHAT_RIGHT,
			new Loli.AnimationInfo(idleTransition,
			"stand_wear_sunhat_right","stand_wear_sunhat_right","stand_wear_sunhat_right",
			Loli.Priority.MEDIUM,BodyState.STAND,BodyState.STAND,
			0.35f,new Loli.AnimLogicInfo(0,1.0f,0.5f,0,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.10f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.MISC_IDLE, 1.0f } ),
				new LoliAnimationEvent( 0.65f, (int)Loli.AnimationEventName.WEAR_HAT_RIGHT_HAND, null ),
			}
		), true );
		
		RegisterAnimation(Loli.Animation.STAND_POLAROID_FRAME_REACT_IN_RIGHT,
			new Loli.AnimationInfo(new IdleBehavior.TransitionToPolaroidFrameReact(),
			"stand_polaroid_frame_react_in_right","stand_locomotion_spine_stable","stand_polaroid_frame_react_in_right",
			Loli.Priority.LOW,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(0,1.0f,0.55f,0,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.000f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.THINKING_SHORT, 0.0f } ),
			}
		),true);
		

		RegisterAnimation(Loli.Animation.STAND_POLAROID_FRAME_REACT_NORMAL_RIGHT,
			new Loli.AnimationInfo(idleTransition,
			"stand_polaroid_frame_react_normal_right","stand_locomotion_spine_stable","stand_polaroid_frame_react_normal_right",
			Loli.Priority.LOW,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(0,1.0f,0.55f,0,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.250f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.THINKING_LONG, 1.0f } ),
				new LoliAnimationEvent( 0.850f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 3.0f, 1.0f, 1.0f } ),
				new LoliAnimationEvent( 0.850f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 7.0f, 1.0f } ),
			}
		),true);

		RegisterAnimation(Loli.Animation.STAND_POLAROID_FRAME_REACT_PANTY_RIGHT,
			new Loli.AnimationInfo(new Loli.DefaultTransition(Loli.Animation.STAND_POLAROID_FRAME_REACT_RIP),
			"stand_polaroid_frame_react_panty_right","stand_polaroid_frame_react_panty_right","stand_polaroid_frame_react_panty_right",
			Loli.Priority.LOW,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(0,1.0f,0.55f,0,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.000f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.SCARED_SHORT, 0.0f } ),
				new LoliAnimationEvent( 0.400f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.ANGRY_GRUMBLE_LONG, 1.0f } ),
				new LoliAnimationEvent( 0.500f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 1.0f, 1.0f, 0.4f } ),
				new LoliAnimationEvent( 0.500f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 12.0f, 0.4f } ),
			}
		),true);
		
		RegisterAnimation(Loli.Animation.STAND_POLAROID_FRAME_REACT_RIP,
			new Loli.AnimationInfo(idleTransition,
			"stand_polaroid_frame_rip","stand_polaroid_frame_rip","stand_polaroid_frame_rip",
			Loli.Priority.MEDIUM,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(0,1.0f,0.55f,0,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.000f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.RIP, 1.0f } ),
				new LoliAnimationEvent( 0.167f, (int)Loli.AnimationEventName.SPAWN_POLAROID_FRAME_RIPPED_FX, null ),
				new LoliAnimationEvent( 0.283f, (int)Loli.AnimationEventName.SPAWN_POLAROID_FRAME_RIPPED_FX, null ),
				new LoliAnimationEvent( 0.400f, (int)Loli.AnimationEventName.SPAWN_POLAROID_FRAME_RIPPED_FX, null ),
				new LoliAnimationEvent( 0.500f, (int)Loli.AnimationEventName.SPAWN_POLAROID_FRAME_RIPPED_FX, null ),
				new LoliAnimationEvent( 0.700f, (int)Loli.AnimationEventName.SPAWN_POLAROID_FRAME_RIPPED_FX, null ),
				new LoliAnimationEvent( 0.800f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 3.0f, 1.0f, 1.1f } ),
				new LoliAnimationEvent( 0.800f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 7.0f, 1.0f } ),
			}
		));

		RegisterAnimation(Loli.Animation.STAND_CHASE_LOCOMOTION,
			new Loli.AnimationInfo(null,
			"stand_jealous_loop","stand_jealous_loop","stand_jealous_loop",
			Loli.Priority.LOW,BodyState.STAND,BodyState.STAND,
			0.35f,new Loli.AnimLogicInfo(),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.FOOTSTEP_SOUND, null ),
				new LoliAnimationEvent( 0.5f, (int)Loli.AnimationEventName.FOOTSTEP_SOUND, null ),
			},
			(int)Loli.AnimationInfo.Flag.IDLE_STATE
		));
		animationInfos[ Loli.Animation.STAND_CHASE_LOCOMOTION ].setTorsoSyncFloatID( Animator.StringToHash("sync_locomotion_jealous") );


		RegisterAnimation(Loli.Animation.STAND_WEAR_BAG_RIGHT,
			new Loli.AnimationInfo(idleTransition,
			"stand_wear_bag_right_nm","stand_happy_loop","stand_happy_idle",
			Loli.Priority.LOW,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(0,1.0f,0.5f,0,0.3f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.000f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.MISC_IDLE, 1.0f } ),
				new LoliAnimationEvent( 0.675f, (int)Loli.AnimationEventName.ATTACH_BAG_SHOULDER, null ),
				new LoliAnimationEvent( 0.800f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 3.0f, 1.0f, 0.5f } ),
				new LoliAnimationEvent( 0.800f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 7.0f, 0.5f } ),
			}
		),true);

		RegisterAnimation(Loli.Animation.STAND_REMOVE_BAG_RIGHT,
			new Loli.AnimationInfo(idleTransition,
			"stand_remove_bag_right_nm","stand_happy_loop","stand_happy_idle",
			Loli.Priority.LOW,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(0,1.0f,0.5f,0,0.3f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.000f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.MISC_IDLE, 1.0f } ),
				new LoliAnimationEvent( 0.410f, (int)Loli.AnimationEventName.REMOVE_BAG_SHOULDER, null ),
				new LoliAnimationEvent( 0.800f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 3.0f, 1.0f, 0.5f } ),
				new LoliAnimationEvent( 0.800f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 7.0f, 0.5f } ),
			}
		),true);
		
		RegisterAnimation(Loli.Animation.STAND_BAG_PUT_IN_RIGHT,
			new Loli.AnimationInfo(idleTransition,
			"stand_bag_put_in_right","stand_happy_loop","stand_happy_idle",
			Loli.Priority.LOW,BodyState.STAND,BodyState.STAND,
			0.4f,new Loli.AnimLogicInfo(0,1.0f,0.7f,0,0.3f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.000f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.MISC_IDLE, 1.0f } ),
				new LoliAnimationEvent( 0.3f, (int)Loli.AnimationEventName.OPEN_BAG_RIGHT, null ),
				new LoliAnimationEvent( 0.700f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 3.0f, 1.0f, 0.5f } ),
				new LoliAnimationEvent( 0.700f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 7.0f, 0.5f } ),
				new LoliAnimationEvent( 0.55f, (int)Loli.AnimationEventName.PUT_ITEM_IN_BAG_RIGHT, null ),
			}
		),true);

		RegisterAnimation(Loli.Animation.STAND_LOCOMOTION_JEALOUS,
			new Loli.AnimationInfo(null,
			"stand_jealous_loop","stand_jealous_loop","stand_jealous_loop",
			Loli.Priority.LOW,BodyState.STAND,BodyState.STAND,
			0.35f,new Loli.AnimLogicInfo(),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.FOOTSTEP_SOUND, null ),
				new LoliAnimationEvent( 0.5f, (int)Loli.AnimationEventName.FOOTSTEP_SOUND, null ),
			},
			(int)Loli.AnimationInfo.Flag.IDLE_STATE
		));
		animationInfos[ Loli.Animation.STAND_LOCOMOTION_JEALOUS ].setTorsoSyncFloatID( Animator.StringToHash("sync_locomotion_jealous") );

		RegisterAnimation(Loli.Animation.STAND_ANGRY_TIP_TOE_REACH,
			new Loli.AnimationInfo(new Loli.DefaultTransition(Loli.Animation.STAND_ANGRY_TIP_TOE_REACH_END),
			"stand_angry_tip_toe_reach","stand_angry_tip_toe_reach","stand_angry_tip_toe_reach",
			Loli.Priority.MEDIUM,BodyState.STAND,BodyState.STAND,
			0.4f,new Loli.AnimLogicInfo(3,1.0f,0.5f,12,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.05f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.ANGRY_LONG, 1.0f } ),
				new LoliAnimationEvent( 0.100f, (int)Loli.AnimationEventName.TOGGLE_FACE_YAW_SUM_FOR_ANIMATION, null ),
				new LoliAnimationEvent( 0.220f, (int)Loli.AnimationEventName.TOGGLE_FACE_YAW_SUM_FOR_ANIMATION, null ),
				new LoliAnimationEvent( 0.312f, (int)Loli.AnimationEventName.TOGGLE_FACE_YAW_SUM_FOR_ANIMATION, null ),
				new LoliAnimationEvent( 0.532f, (int)Loli.AnimationEventName.TOGGLE_FACE_YAW_SUM_FOR_ANIMATION, null ),
				new LoliAnimationEvent( 0.578f, (int)Loli.AnimationEventName.TOGGLE_FACE_YAW_SUM_FOR_ANIMATION, null ),
				new LoliAnimationEvent( 0.716f, (int)Loli.AnimationEventName.TOGGLE_FACE_YAW_SUM_FOR_ANIMATION, null ),
			}
		));

		RegisterAnimation(Loli.Animation.STAND_ANGRY_TIP_TOE_REACH_END,
			new Loli.AnimationInfo(new Loli.DefaultTransition(Loli.Animation.STAND_LOCOMOTION_JEALOUS),
			"stand_angry_tip_toe_reach_end","stand_angry_tip_toe_reach_end","stand_angry_tip_toe_reach_end",
			Loli.Priority.MEDIUM,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(1,1.0f,0.5f,12,0.4f),null
		));
		RegisterAnimation(Loli.Animation.STAND_ANGRY_JEALOUS_STEAL_RIGHT,
			new Loli.AnimationInfo(idleTransition,
			"stand_angry_jealous_steal_right","stand_angry_jealous_steal_right","stand_angry_jealous_steal_right",
			Loli.Priority.HIGH,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(0,1.0f,0.5f,12,0.52f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.200f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.EFFORT_ANGRY, 0.0f } ),
				// new LoliAnimationEvent( 0.460f, (int)Loli.AnimationEventName.STEAL_CURRENT_INTEREST, null ),
				new LoliAnimationEvent( 0.460f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 3.0f, 0.8f, 0.5f } ),
			}
		), true );
		
        RegisterAnimation(Loli.Animation.STAND_ANGRY_JEALOUS,
			new Loli.AnimationInfo(new Loli.DefaultTransition(Loli.Animation.STAND_LOCOMOTION_JEALOUS),
			"stand_angry_jealous","stand_angry_jealous","stand_angry_jealous",
			Loli.Priority.MEDIUM,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(3,1.0f,0.5f,12,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.062f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.DISAPPOINTED_STARTLE, 0.0f } ),
				new LoliAnimationEvent( 0.175f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 0.0f, 1.0f, 0.4f } ),
				new LoliAnimationEvent( 0.200f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 0.0f, 0.4f } ),
				new LoliAnimationEvent( 0.325f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.ANGRY_POUT, 0.0f } ),
				new LoliAnimationEvent( 0.830f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 3.0f, 1.0f, 0.4f } ),
				new LoliAnimationEvent( 0.830f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 12.0f, 0.4f } ),
			}
		));
		
		RegisterLegSpeedInfo( "stand_jealous_loop", 2.4f, 2.0f,255.0f, 30.0f );
		
		
        RegisterAnimation(Loli.Animation.CRAWL_TIRED_TO_STAND,
			new Loli.AnimationInfo(idleTransition,
			"crawl_tired_to_stand","crawl_tired_to_stand","stand_happy_idle",
			Loli.Priority.MEDIUM,BodyState.CRAWL_TIRED,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(0,1.0f,0.3f,12,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{(float)Loli.VoiceLine.MISC_IDLE,0.1f} ),
			},
			(int)AnimationInfo.Flag.DISABLE_RAGDOLL_CHECK
		));

        RegisterAnimation(Loli.Animation.STAND_TIRED_TO_BED_CRAWL_TIRED,
			new Loli.AnimationInfo(new Loli.DefaultTransition(Loli.Animation.CRAWL_TIRED_IDLE),
			"stand_tired_to_bed_crawl_tired","stand_tired_to_bed_crawl_tired","stand_tired_to_bed_crawl_tired",
			Loli.Priority.MEDIUM,BodyState.STAND,BodyState.CRAWL_TIRED,
			0.3f,new Loli.AnimLogicInfo(0,1.0f,0.3f,12,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{(float)Loli.VoiceLine.MISC_IDLE,0.0f} ),
				new LoliAnimationEvent( 0.7f, (int)Loli.AnimationEventName.JUMP_ON_BED, null )
			},
			(int)AnimationInfo.Flag.DISABLE_RAGDOLL_CHECK
		));

		RegisterAnimation(Loli.Animation.CRAWL_TIRED_IDLE,
			new Loli.AnimationInfo(null,
			"crawl_tired_idle","crawl_tired_idle","crawl_tired_idle",
			Loli.Priority.LOW,BodyState.CRAWL_TIRED,BodyState.CRAWL_TIRED,
			0.3f,new Loli.AnimLogicInfo(1,0.5f,0.3f,7,0.4f),
			null,
			(int)Loli.AnimationInfo.Flag.IDLE_STATE
		));

		RegisterAnimation(Loli.Animation.CRAWL_TIRED_TO_LAY_PILLOW_SIDE_HAPPY_RIGHT,
			new Loli.AnimationInfo(new Loli.DefaultTransition( Loli.Animation.AWAKE_PILLOW_SIDE_HAPPY_IDLE_RIGHT ),
			"crawl_tired_to_lay_side_pillow_happy_right","crawl_tired_to_lay_side_pillow_happy_right","crawl_tired_to_lay_side_pillow_happy_right",
			Loli.Priority.LOW,BodyState.CRAWL_TIRED,BodyState.AWAKE_PILLOW_SIDE_RIGHT,
			0.3f,new Loli.AnimLogicInfo(0,0.5f,0.3f,0,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.ROLL_ON_BED, null )
			}
		),true);


		RegisterAnimation(Loli.Animation.AWAKE_PILLOW_SIDE_HAPPY_IDLE_RIGHT,
			new Loli.AnimationInfo(null,
			"lay_side_pillow_happy_idle_loop_right","lay_side_pillow_happy_idle_loop_right","lay_side_pillow_happy_idle_loop_right",
			Loli.Priority.LOW,BodyState.AWAKE_PILLOW_SIDE_RIGHT,BodyState.AWAKE_PILLOW_SIDE_RIGHT,
			0.3f,new Loli.AnimLogicInfo(0,0.5f,0.3f,7,0.4f),
			null,
			(int)Loli.AnimationInfo.Flag.IDLE_STATE
		),true);
		
		RegisterAnimation(Loli.Animation.AWAKE_PILLOW_SIDE_YAWN_LONG_RIGHT,
			new Loli.AnimationInfo(new Loli.DefaultTransition(Loli.Animation.AWAKE_PILLOW_SIDE_HAPPY_IDLE_RIGHT),
			"lay_side_pillow_yawn_long_right","lay_side_pillow_yawn_long_right","lay_side_pillow_yawn_long_right",
			Loli.Priority.MEDIUM,BodyState.AWAKE_PILLOW_SIDE_RIGHT,BodyState.AWAKE_PILLOW_SIDE_RIGHT,
			0.3f,new Loli.AnimLogicInfo(0,0.5f,0.3f,0,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.05f, (int)Loli.AnimationEventName.SPEAK, new float[]{(float)Loli.VoiceLine.YAWN_LONG,1.0f} ),
			}
		),true);

		RegisterAnimation(Loli.Animation.AWAKE_PILLOW_SIDE_TO_SLEEP_PILLOW_SIDE_RIGHT,
			new Loli.AnimationInfo(new Loli.DefaultTransition(Loli.Animation.SLEEP_PILLOW_SIDE_IDLE_RIGHT),
			"lay_side_pillow_to_sleep_side_pillow_right","lay_side_pillow_to_sleep_side_pillow_right","lay_side_pillow_to_sleep_side_pillow_right",
			Loli.Priority.MEDIUM,BodyState.AWAKE_PILLOW_SIDE_RIGHT,BodyState.SLEEP_PILLOW_SIDE_RIGHT,
			0.3f,new Loli.AnimLogicInfo(0,0.5f,0.3f,0,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.05f, (int)Loli.AnimationEventName.SPEAK, new float[]{(float)Loli.VoiceLine.MISC_IDLE,0.0f} ),
			}
		),true);

		RegisterAnimation(Loli.Animation.AWAKE_PILLOW_SIDE_SOUND_GOODNIGHT_RIGHT,
			new Loli.AnimationInfo(idleTransition,
			"lay_side_pillow_sound_goodnight_right","lay_side_pillow_sound_goodnight_right","lay_side_pillow_sound_goodnight_right",
			Loli.Priority.MEDIUM,BodyState.AWAKE_PILLOW_SIDE_RIGHT,BodyState.AWAKE_PILLOW_SIDE_RIGHT,
			0.3f,new Loli.AnimLogicInfo(0,0.2f,0.3f,12,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.05f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.GOODNIGHT, 1.0f } ),
			}
		),true);


		RegisterAnimation(Loli.Animation.SLEEP_PILLOW_SIDE_IDLE_RIGHT,
			new Loli.AnimationInfo(null,
			"sleep_side_pillow_idle_loop_right","sleep_side_pillow_idle_loop_right","sleep_side_pillow_idle_loop_right",
			Loli.Priority.LOW,BodyState.SLEEP_PILLOW_SIDE_RIGHT,BodyState.SLEEP_PILLOW_SIDE_RIGHT,
			0.3f,new Loli.AnimLogicInfo(0,0.5f,0.3f,0,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.5f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.SLEEP_BREATHING, 1.0f } )
			},
			(int)Loli.AnimationInfo.Flag.IDLE_STATE
		),true);

		RegisterAnimation(Loli.Animation.SLEEP_PILLOW_SIDE_TO_SLEEP_PILLOW_UP_RIGHT,
			new Loli.AnimationInfo(idleTransition,
			"sleep_side_pillow_to_sleep_pillow_up_right_nm","sleep_side_pillow_to_sleep_pillow_up_right_nm","sleep_side_pillow_to_sleep_pillow_up_right_nm",
			Loli.Priority.MEDIUM,BodyState.SLEEP_PILLOW_SIDE_RIGHT,BodyState.SLEEP_PILLOW_UP,
			0.1f,new Loli.AnimLogicInfo(0,0.5f,0.3f,0,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.SLEEP_DISTURBED_LONG, 0.0f } ),
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.ROLL_ON_BED, null )
			}
		),true);

		RegisterAnimation(Loli.Animation.SLEEP_PILLOW_UP_IDLE,
			new Loli.AnimationInfo(null,
			"sleep_pillow_up_idle_loop","sleep_pillow_up_idle_loop","sleep_pillow_up_idle_loop",
			Loli.Priority.LOW,BodyState.SLEEP_PILLOW_UP,BodyState.SLEEP_PILLOW_UP,
			0.1f,new Loli.AnimLogicInfo(0,0.5f,0.3f,0,0.4f),
			new LoliAnimationEvent[]{	
				new LoliAnimationEvent( 0.5f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.SLEEP_BREATHING, 1.0f } )
			},
			(int)Loli.AnimationInfo.Flag.IDLE_STATE
		));

		RegisterAnimation(Loli.Animation.SLEEP_PILLOW_UP_TO_SLEEP_PILLOW_SIDE_RIGHT,
			new Loli.AnimationInfo(new Loli.DefaultTransition( Loli.Animation.SLEEP_PILLOW_SIDE_IDLE_RIGHT ),
			"sleep_pillow_up_to_sleep_side_pillow_right_nm","sleep_pillow_up_to_sleep_side_pillow_right_nm","sleep_pillow_up_to_sleep_side_pillow_right_nm",
			Loli.Priority.MEDIUM,BodyState.SLEEP_PILLOW_UP,BodyState.SLEEP_PILLOW_SIDE_RIGHT,
			0.1f,new Loli.AnimLogicInfo(0,0.5f,0.3f,0,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.SLEEP_DISTURBED_LONG, 0.0f } ),
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.ROLL_ON_BED, null )
			}
		),true);


		RegisterAnimation(Loli.Animation.SLEEP_PILLOW_SIDE_HEADPAT_START_RIGHT,
			new Loli.AnimationInfo(new Loli.DefaultTransition( Loli.Animation.SLEEP_PILLOW_SIDE_IDLE_RIGHT),
			"sleep_side_pillow_headpat_start_right","sleep_side_pillow_headpat_start_right","sleep_side_pillow_headpat_start_right",
			Loli.Priority.LOW,BodyState.SLEEP_PILLOW_SIDE_RIGHT,BodyState.SLEEP_PILLOW_SIDE_RIGHT,
			0.3f,new Loli.AnimLogicInfo(0,0.5f,0.3f,0,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.4f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.HEADPAT_RETURN, 1.0f } )
			}
		),true);

		RegisterAnimation(Loli.Animation.SLEEP_PILLOW_UP_TO_AWAKE_HAPPY_PILLOW_UP,
			new Loli.AnimationInfo(idleTransition,
			"sleep_pillow_up_to_awake_happy_pillow_up","sleep_pillow_up_to_awake_happy_pillow_up","sleep_pillow_up_to_awake_happy_pillow_up",
			Loli.Priority.MEDIUM,BodyState.SLEEP_PILLOW_UP,BodyState.AWAKE_PILLOW_UP,
			0.3f,new Loli.AnimLogicInfo(0,0.5f,0.3f,0,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.YAWN_SHORT, 1.0f } ),
				new LoliAnimationEvent( 0.7f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.MISC_IDLE, 0.0f } ),
				new LoliAnimationEvent( 0.55f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 12.0f, 0.4f } ),
				new LoliAnimationEvent( 0.7f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 1.0f, 0.65f, 0.5f } )
			}
		));

		RegisterAnimation(Loli.Animation.SLEEP_PILLOW_UP_TO_AWAKE_ANGRY_PILLOW_UP,
			new Loli.AnimationInfo(idleTransition,
			"sleep_pillow_up_to_awake_angry_pillow_up","sleep_pillow_up_to_awake_angry_pillow_up","sleep_pillow_up_to_awake_angry_pillow_up",
			Loli.Priority.MEDIUM,BodyState.SLEEP_PILLOW_UP,BodyState.AWAKE_PILLOW_UP,
			0.3f,new Loli.AnimLogicInfo(0,0.5f,0.3f,0,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.MISC_IDLE, 1.0f } ),
				new LoliAnimationEvent( 0.5f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.ANGRY_POUT, 0.0f } ),
				new LoliAnimationEvent( 0.55f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 12.0f, 0.4f } ),
				new LoliAnimationEvent( 0.7f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 1.0f, 0.65f, 0.5f } )
			}
		));

		RegisterAnimation(Loli.Animation.SLEEP_PILLOW_SIDE_TO_AWAKE_HAPPY_PILLOW_UP_RIGHT,
			new Loli.AnimationInfo(idleTransition,
			"sleep_side_pillow_to_awake_happy_pillow_up_right_nm","sleep_side_pillow_to_awake_happy_pillow_up_right_nm","sleep_side_pillow_to_awake_happy_pillow_up_right_nm",
			Loli.Priority.MEDIUM,BodyState.SLEEP_PILLOW_SIDE_RIGHT,BodyState.AWAKE_PILLOW_UP,
			0.3f,new Loli.AnimLogicInfo(0,0.5f,0.3f,0,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.03f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.MISC_IDLE, 1.0f } ),
				new LoliAnimationEvent( 0.3f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.TROUBLED, 1.0f } ),
				new LoliAnimationEvent( 0.5f, (int)Loli.AnimationEventName.ROLL_ON_BED, null ),
				new LoliAnimationEvent( 0.4f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 12.0f, 0.4f } ),
				new LoliAnimationEvent( 0.8f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 1.0f, 0.65f, 0.5f } ),
			}
		),true);

		RegisterAnimation(Loli.Animation.SLEEP_PILLOW_SIDE_TO_AWAKE_ANGRY_PILLOW_UP_RIGHT,
			new Loli.AnimationInfo(idleTransition,
			"sleep_side_pillow_to_awake_angry_pillow_up_right_nm","sleep_side_pillow_to_awake_angry_pillow_up_right_nm","sleep_side_pillow_to_awake_angry_pillow_up_right_nm",
			Loli.Priority.MEDIUM,BodyState.SLEEP_PILLOW_SIDE_RIGHT,BodyState.AWAKE_PILLOW_UP,
			0.3f,new Loli.AnimLogicInfo(0,0.5f,0.3f,0,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.MISC_IDLE, 0.0f } ),
				new LoliAnimationEvent( 0.5f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.ANGRY_POUT, 0.0f } ),
				new LoliAnimationEvent( 0.5f, (int)Loli.AnimationEventName.ROLL_ON_BED, null ),
				new LoliAnimationEvent( 0.4f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 12.0f, 0.4f } ),
				new LoliAnimationEvent( 0.8f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 1.0f, 0.65f, 0.5f } ),
			}
		),true);

		RegisterAnimation(Loli.Animation.AWAKE_HAPPY_PILLOW_UP_IDLE,
			new Loli.AnimationInfo(null,
			"awake_happy_pillow_up_idle_loop","awake_happy_pillow_up_idle_loop","awake_happy_pillow_up_idle_loop",
			Loli.Priority.LOW,BodyState.AWAKE_PILLOW_UP,BodyState.AWAKE_PILLOW_UP,
			0.3f,new Loli.AnimLogicInfo(1,0.6f,0.5f,7,0.4f),
			null,
			(int)Loli.AnimationInfo.Flag.IDLE_STATE
		));

		RegisterAnimation(Loli.Animation.AWAKE_ANGRY_PILLOW_UP_IDLE,
			new Loli.AnimationInfo(null,
			"awake_angry_pillow_up_idle_loop","awake_angry_pillow_up_idle_loop","awake_angry_pillow_up_idle_loop",
			Loli.Priority.LOW,BodyState.AWAKE_PILLOW_UP,BodyState.AWAKE_PILLOW_UP,
			0.3f,new Loli.AnimLogicInfo(1,0.6f,0.5f,7,0.4f),
			null,
			(int)Loli.AnimationInfo.Flag.IDLE_STATE
		));

		RegisterAnimation(Loli.Animation.AWAKE_HAPPY_PILLOW_UP_HEADPAT_LOOP,
			new Loli.AnimationInfo(null,
			"awake_happy_pillow_up_headpat_idle","awake_happy_pillow_up_headpat_idle","awake_happy_pillow_up_headpat_idle",
			Loli.Priority.LOW,BodyState.AWAKE_PILLOW_UP,BodyState.AWAKE_PILLOW_UP,
			0.5f,new Loli.AnimLogicInfo(0,0.5f,0.3f,12,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.45f, (int)Loli.AnimationEventName.HEADPAT_PROPER_SOUND, null )
			}
		));

		

		RegisterAnimation(Loli.Animation.FLOOR_SIT_TO_CRAWL,
			new Loli.AnimationInfo(idleTransition,
			"floor_sit_to_crawl","floor_sit_to_crawl","stand_to_horseback_Idle_right",
			Loli.Priority.MEDIUM,BodyState.FLOOR_SIT,BodyState.CRAWL_TIRED,
			0.3f,new Loli.AnimLogicInfo(0,0.5f,0.3f,0,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.1f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.MISC_IDLE, 0.0f } ),
				new LoliAnimationEvent( 0.4f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.EFFORT, 0.0f } ),
			}
		));

		RegisterAnimation(Loli.Animation.AWAKE_PILLOW_SIDE_TO_AWAKE_PILLOW_UP_RIGHT,
			new Loli.AnimationInfo(idleTransition,
			"awake_side_pillow_to_awake_pillow_up_right","awake_side_pillow_to_awake_pillow_up_right","stand_happy_idle",
			Loli.Priority.MEDIUM,BodyState.AWAKE_PILLOW_SIDE_RIGHT,BodyState.AWAKE_PILLOW_UP,
			0.3f,new Loli.AnimLogicInfo(0,0.5f,0.3f,0,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.4f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.MISC_IDLE, 0.0f } ),
			}
		),true);

		RegisterAnimation(Loli.Animation.AWAKE_PILLOW_UP_TO_CRAWL_TIRED,
			new Loli.AnimationInfo(idleTransition,
			"awake_pillow_up_to_crawl","awake_pillow_up_to_crawl","awake_pillow_up_to_crawl",
			Loli.Priority.MEDIUM,BodyState.AWAKE_PILLOW_UP,BodyState.CRAWL_TIRED,
			0.3f,new Loli.AnimLogicInfo(0,0.5f,0.3f,0,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.4f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.EFFORT, 0.0f } ),
				new LoliAnimationEvent( 0.5f, (int)Loli.AnimationEventName.JUMP_ON_BED, null ),
				new LoliAnimationEvent( 0.95f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.MISC_IDLE, 0.0f } ),
			}
		));

		RegisterAnimation(Loli.Animation.CRAWL_BED_TO_STAND,
			new Loli.AnimationInfo(idleTransition,
			"crawl_bed_to_stand","crawl_bed_to_stand","crawl_bed_to_stand",
			Loli.Priority.MEDIUM,BodyState.CRAWL_TIRED,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(0,0.5f,0.3f,0,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.4f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.EFFORT, 0.0f } ),
				new LoliAnimationEvent( 0.2f, (int)Loli.AnimationEventName.ROLL_ON_BED, null ),
				new LoliAnimationEvent( 0.7f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 12.0f, 0.4f } ),
				new LoliAnimationEvent( 0.7f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 1.0f, 0.65f, 0.5f } ),
			},
			(int)AnimationInfo.Flag.DISABLE_RAGDOLL_CHECK
		));


		RegisterAnimation(Loli.Animation.STAND_SOUND_GOOD_MORNING,
			new Loli.AnimationInfo(idleTransition,
			"stand_sound_goodmorning_short","stand_sound_goodmorning_short","stand_sound_goodmorning_short",
			Loli.Priority.MEDIUM,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(3,0.7f,0.3f,12,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.05f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.GOODMORNING, 1.0f } ),
			}
		));

		RegisterLegSpeedInfo( "crawl_tired_idle", 0.65f, 1.0f, 100.0f, 7.0f );

        
		RegisterAnimation(Loli.Animation.STAND_GIDDY_SURPRISE,
			new Loli.AnimationInfo(idleTransition,
			"stand_giddy_surprise","stand_giddy_surprise","stand_giddy_surprise",
			Loli.Priority.MEDIUM,BodyState.STAND,BodyState.STAND,
			0.2f,new Loli.AnimLogicInfo(3,0.6f,0.5f,12,1.2f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.080f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.IMPRESSED_VERY, 0.0f } ),
			}
		));

		RegisterAnimation(Loli.Animation.STAND_GIDDY_LOCOMOTION,
			new Loli.AnimationInfo(null,
			"stand_giddy_loop","stand_giddy_loop","stand_giddy_loop",
			Loli.Priority.LOW,BodyState.STAND,BodyState.STAND,
			0.4f,new Loli.AnimLogicInfo(1,1.0f,0.5f,7,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.FOOTSTEP_SOUND, null ),
				new LoliAnimationEvent( 0.5f, (int)Loli.AnimationEventName.FOOTSTEP_SOUND, null ),
			},
			(int)Loli.AnimationInfo.Flag.IDLE_STATE
		));
		animationInfos[ Loli.Animation.STAND_GIDDY_LOCOMOTION ].setTorsoSyncFloatID( Animator.StringToHash("sync_giddy_locomotion") );
        
		RegisterAnimation(Loli.Animation.STAND_PICKUP_RIGHT,
			new Loli.AnimationInfo( idleTransition,
			"stand_pickup_right","stand_pickup_right","stand_happy_idle",
			Loli.Priority.MEDIUM,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(0,0.6f,0.5f,7,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.MISC_IDLE, 0.0f } ),
			}
		),true);
		
		RegisterAnimation(Loli.Animation.STAND_SEARCH_RIGHT,
			new Loli.AnimationInfo(idleTransition,
			"stand_search_right","stand_search_right","stand_search_right",
			Loli.Priority.LOW,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(0,1.0f,0.4f,0,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.DISAPPOINTED_STARTLE, 1.0f } ),
				new LoliAnimationEvent( 0.5f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.TROUBLED, 1.0f } ),
				new LoliAnimationEvent( 0.8f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 3.0f, 1.0f, 1.0f } )
			},
			(int)Loli.AnimationInfo.Flag.IDLE_STATE
		));

		RegisterAnimation(Loli.Animation.STAND_HAPPY_CHANGE_ITEM_HANDS,
			new Loli.AnimationInfo(idleTransition,
			"stand_change_item_hands","stand_happy_loop","stand_happy_idle",
			Loli.Priority.LOW,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(3,0.5f,0.4f,7,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.MISC_IDLE, 0.0f } ),
			}
		));

		RegisterAnimation(Loli.Animation.STAND_ANGRY_CHANGE_ITEM_HANDS,
			new Loli.AnimationInfo(idleTransition,
			"stand_change_item_hands","stand_angry_loop","stand_angry_idle",
			Loli.Priority.LOW,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(3,0.5f,0.4f,7,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.MISC_IDLE, 0.0f } ),
			}
		));

		
		RegisterAnimation(Loli.Animation.STAND_TO_SIT_FLOOR,
			new Loli.AnimationInfo(new Loli.DefaultTransition(Loli.Animation.FLOOR_SIT_LOCOMOTION_HAPPY),"stand_to_sit_floor","stand_to_sit_floor",null,
			Loli.Priority.MEDIUM,BodyState.STAND,BodyState.FLOOR_SIT,
			0.3f,new Loli.AnimLogicInfo(0,1.0f,0.6f,0,0.6f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.6f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 1.0f, 1.0f, 0.8f } ),
				new LoliAnimationEvent( 0.6f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 12.0f, 0.4f } )
			}
		));

		RegisterAnimation(Loli.Animation.FLOOR_SIT_LOCOMOTION_HAPPY,
			new Loli.AnimationInfo(null,"floor_sit_locomotion_happy","floor_sit_locomotion_happy","stand_happy_idle",
			Loli.Priority.LOW,BodyState.FLOOR_SIT,BodyState.FLOOR_SIT,
			0.5f,new Loli.AnimLogicInfo(1,0.6f,0.5f,7,0.4f),null,
			(int)Loli.AnimationInfo.Flag.IDLE_STATE
		));

		RegisterAnimation(Loli.Animation.FLOOR_SIT_LOCOMOTION_ANGRY,
			new Loli.AnimationInfo(null,"floor_sit_locomotion_angry","floor_sit_locomotion_angry","stand_angry_idle",
			Loli.Priority.LOW,BodyState.FLOOR_SIT,BodyState.FLOOR_SIT,
			0.5f,new Loli.AnimLogicInfo(1,0.6f,0.5f,7,0.4f),null,
			(int)Loli.AnimationInfo.Flag.IDLE_STATE
		));

		RegisterAnimation(Loli.Animation.FLOOR_SIT_TO_STAND,
			new Loli.AnimationInfo(idleTransition,"sit_floor_to_stand","sit_floor_to_stand",null,
			Loli.Priority.MEDIUM,BodyState.FLOOR_SIT,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(0,1.0f,0.5f,0,0.4f),null));

		RegisterLegSpeedInfo( "stand_happy_loop", 1.0f, 2.0f, 255.0f, 30.0f );
		RegisterLegSpeedInfo( "stand_angry_loop", 1.1f, 2.0f, 255.0f, 30.0f );
		RegisterLegSpeedInfo( "stand_locomotion_spine_stable", 1.0f, 2.0f, 255.0f, 30.0f );
		RegisterLegSpeedInfo( "floor_sit_locomotion_happy", 0.6f, 1.6f, 100.0f, 10.0f );
		RegisterLegSpeedInfo( "floor_sit_locomotion_angry", 0.6f, 1.6f, 100.0f, 10.0f );
		RegisterLegSpeedInfo( "stand_happy_social1", 1.0f, 2.0f, 255.0f, 30.0f );

		RegisterAnimation(Loli.Animation.PHOTOSHOOT_1,
			new Loli.AnimationInfo(null,
			"photoshoot_1","photoshoot_1","photoshoot_1",
			Loli.Priority.VERY_HIGH,BodyState.STAND,BodyState.STAND,
			0.2f,new Loli.AnimLogicInfo(0,1.0f,0.0f,0,0.0f)
		));

		RegisterAnimation(Loli.Animation.PHOTOSHOOT_2,
			new Loli.AnimationInfo(null,
			"photoshoot_2","photoshoot_2","photoshoot_2",
			Loli.Priority.VERY_HIGH,BodyState.STAND,BodyState.STAND,
			0.2f,new Loli.AnimLogicInfo(0,1.0f,0.0f,0,0.0f)
		));

		RegisterAnimation(Loli.Animation.STAND_OUTFIT_LIKE,
			new Loli.AnimationInfo(idleTransition,
			"stand_outfit_like","stand_outfit_like","stand_outfit_like",
			Loli.Priority.MEDIUM,BodyState.STAND,BodyState.STAND,
			0.2f,new Loli.AnimLogicInfo(0,1.0f,0.3f,0,0.3f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.2f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.IMPRESSED_VERY, 1.0f } ),
				new LoliAnimationEvent( 0.8f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 7.0f, 0.5f } ),
				new LoliAnimationEvent( 0.8f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 3.0f, 1.0f, 0.6f } ),
			}
		));
		
		RegisterAnimation(Loli.Animation.STAND_HANDHOLD_ANGRY_REFUSE_RIGHT,
			new Loli.AnimationInfo(idleTransition,
			"stand_handhold_angry_refuse_right","stand_handhold_angry_refuse_right","stand_handhold_angry_refuse_right",
			Loli.Priority.MEDIUM,BodyState.STAND,BodyState.STAND,
			0.2f,new Loli.AnimLogicInfo(0,1.0f,0.6f,0,1.2f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.ANGRY_SHORT, 0.0f } ),
				new LoliAnimationEvent( 0.78f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 1.0f, 0.5f, 1.0f } ),
				new LoliAnimationEvent( 0.78f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 12.0f, 0.4f } ),
				new LoliAnimationEvent( 0.65f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.ANGRY_GRUMBLE_SHORT, 0.0f } ),
			}
		));
		RegisterAnimation(Loli.Animation.STAND_HANDHOLD_ANGRY_REFUSE_LEFT,
			new Loli.AnimationInfo(idleTransition,
			"stand_handhold_angry_refuse_left","stand_handhold_angry_refuse_left","stand_handhold_angry_refuse_left",
			Loli.Priority.MEDIUM,BodyState.STAND,BodyState.STAND,
			0.2f,new Loli.AnimLogicInfo(0,1.0f,0.6f,0,1.2f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.ANGRY_SHORT, 0.0f } ),
				new LoliAnimationEvent( 0.78f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 1.0f, 0.5f, 1.0f } ),
				new LoliAnimationEvent( 0.78f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 12.0f, 0.4f } ),
				new LoliAnimationEvent( 0.65f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.ANGRY_GRUMBLE_SHORT, 0.0f } ),
			}
		));

		RegisterAnimation(Loli.Animation.STAND_HANDHOLD_HAPPY_EMBARRASSED_RIGHT,
			new Loli.AnimationInfo(new Loli.DefaultTransition(Loli.Animation.STAND_HANDHOLD_HAPPY_PULL_RIGHT),
			"stand_handhold_happy_embarrassed_right","stand_handhold_happy_embarrassed_right","stand_handhold_happy_embarrassed_right",
			Loli.Priority.MEDIUM,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(0,1.0f,0.5f,12,1.2f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.EMBARRASSED_HANDHOLD, 1.0f } ),

				new LoliAnimationEvent( 0.075f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 0.0f, 0.5f } ),
				new LoliAnimationEvent( 0.075f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 0.0f, 1.0f, 0.6f } ),

				new LoliAnimationEvent( 0.655f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 3.0f, 0.5f, 0.6f } ),
				new LoliAnimationEvent( 0.655f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 12.0f, 0.5f } ),

				new LoliAnimationEvent( 0.735f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 0.0f, 1.0f, 0.6f } ),
				new LoliAnimationEvent( 0.735f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 0.0f, 0.5f } ),

				new LoliAnimationEvent( 0.88f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 3.0f, 0.5f, 0.6f } ),
				new LoliAnimationEvent( 0.88f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 12.0f, 0.5f } ),
			}
		),true);

		RegisterAnimation(Loli.Animation.STAND_HANDHOLD_HAPPY_PULL_RIGHT,
			new Loli.AnimationInfo(null,
			"stand_handhold_happy_pull_hard_right","stand_handhold_happy_pull_hard_right","stand_handhold_happy_pull_hard_right",
			Loli.Priority.HIGH,BodyState.STAND,BodyState.STAND,
			0.4f,new Loli.AnimLogicInfo(1,0.5f,0.5f,7,1.2f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.FOOTSTEP_SOUND, null ),
				new LoliAnimationEvent( 0.5f, (int)Loli.AnimationEventName.FOOTSTEP_SOUND, null ),
			}
		));
		RegisterAnimation(Loli.Animation.STAND_HANDHOLD_HAPPY_PULL_LEFT,
			new Loli.AnimationInfo(null,
			"stand_handhold_happy_pull_hard_left","stand_handhold_happy_pull_hard_left","stand_handhold_happy_pull_hard_left",
			Loli.Priority.HIGH,BodyState.STAND,BodyState.STAND,
			0.4f,new Loli.AnimLogicInfo(1,0.5f,0.5f,7,1.2f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.FOOTSTEP_SOUND, null ),
				new LoliAnimationEvent( 0.5f, (int)Loli.AnimationEventName.FOOTSTEP_SOUND, null ),
			}
		));

		RegisterLegSpeedInfo( "stand_handhold_happy_pull_hard_right", 2.4f, 1.0f, 255.0f, 30.0f );
		RegisterLegSpeedInfo( "stand_handhold_happy_pull_hard_left", 2.4f, 1.0f, 255.0f, 30.0f );

        RegisterAnimation(Loli.Animation.STAND_TIRED_HANDHOLD_PULL_RIGHT,
            new Loli.AnimationInfo(null,
            "stand_tired_handhold_right","stand_tired_handhold_right","stand_tired_handhold_right",
            Loli.Priority.MEDIUM,BodyState.STAND,BodyState.STAND,
            0.4f,new Loli.AnimLogicInfo(1,0.7f,0.5f,7,1.2f),
            new LoliAnimationEvent[]{
                new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.FOOTSTEP_SOUND, null ),
                new LoliAnimationEvent( 0.5f, (int)Loli.AnimationEventName.FOOTSTEP_SOUND, null ),
            }
        ));
        RegisterAnimation(Loli.Animation.STAND_TIRED_HANDHOLD_PULL_LEFT,
            new Loli.AnimationInfo(null,
            "stand_tired_handhold_left","stand_tired_handhold_left","stand_tired_handhold_left",
            Loli.Priority.MEDIUM,BodyState.STAND,BodyState.STAND,
            0.4f,new Loli.AnimLogicInfo(1,0.7f,0.5f,7,1.2f),
            new LoliAnimationEvent[]{
                new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.FOOTSTEP_SOUND, null ),
                new LoliAnimationEvent( 0.5f, (int)Loli.AnimationEventName.FOOTSTEP_SOUND, null ),
            }
        ));

		RegisterAnimation(Loli.Animation.BATHTUB_HEADPAT_BRUSH_AWAY,
			new Loli.AnimationInfo(idleTransition,
			"bathtub_headpat_brush_away","bathtub_headpat_brush_away","bathtub_headpat_brush_away",
			Loli.Priority.HIGH,BodyState.BATHING_IDLE,BodyState.BATHING_IDLE,
			0.2f,new Loli.AnimLogicInfo(0,1.0f,0.3f,0,0.3f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.1f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.ANGRY_LONG, 1.0f } ),
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.BATHTUB_PLAY_SOUND, new float[]{ (float)Bathtub.SoundType.WATER_MOVEMENT } ),
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.BATHTUB_PLAY_SOUND, new float[]{ (float)Bathtub.SoundType.WATER_SPLASH } ),
			}
		));

        RegisterAnimation(Loli.Animation.BATHTUB_HEADPAT_ANGRY_IDLE,
			new Loli.AnimationInfo(null,
			"bathtub_headpat_angry_idle","bathtub_headpat_angry_idle","bathtub_headpat_angry_idle",
			Loli.Priority.LOW,BodyState.BATHING_IDLE,BodyState.BATHING_IDLE,
			0.3f,new Loli.AnimLogicInfo(0,1.0f,0.3f,7,0.3f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.1f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.ANGRY_POUT, 1.0f } ),
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.BATHTUB_PLAY_SOUND, new float[]{ (float)Bathtub.SoundType.WATER_MOVEMENT } ),
			}
		));

        RegisterAnimation(Loli.Animation.BATHTUB_HEADPAT_HAPPY_IDLE,
			new Loli.AnimationInfo(null,
			"bathtub_headpat_happy_idle","bathtub_headpat_happy_idle","bathtub_headpat_happy_idle",
			Loli.Priority.LOW,BodyState.BATHING_IDLE,BodyState.BATHING_IDLE,
			0.3f,new Loli.AnimLogicInfo(3,1.0f,0.5f,12,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.05f, (int)Loli.AnimationEventName.HEADPAT_PROPER_SOUND, null ),
				new LoliAnimationEvent( 0.45f, (int)Loli.AnimationEventName.HEADPAT_PROPER_SOUND, null ),
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.BATHTUB_PLAY_SOUND, new float[]{ (float)Bathtub.SoundType.WATER_MOVEMENT } ),
			}
		));

		RegisterAnimation(Loli.Animation.BATHTUB_HEADAPAT_ANGRY_PROPER_TO_HAPPY,
			new Loli.AnimationInfo(new Loli.DefaultTransition(Loli.Animation.BATHTUB_HEADPAT_HAPPY_IDLE),
			"bathtub_headpat_angry_proper_to_happy","bathtub_headpat_angry_proper_to_happy","bathtub_headpat_angry_proper_to_happy",
			Loli.Priority.LOW,BodyState.BATHING_IDLE,BodyState.BATHING_IDLE,
			0.3f,new Loli.AnimLogicInfo(0,1.0f,0.5f,0,0.3f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.1f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.ANGRY_HEADPAT_B, 1.0f } ),
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.BATHTUB_PLAY_SOUND, new float[]{ (float)Bathtub.SoundType.WATER_MOVEMENT } ),
			}
		));

		
        RegisterAnimation(Loli.Animation.BATHTUB_ON_KNEES_HEADPAT_IDLE,
			new Loli.AnimationInfo(null,
			"bathtub_on_knees_headpat_idle","bathtub_on_knees_headpat_idle","bathtub_on_knees_headpat_idle",
			Loli.Priority.MEDIUM,BodyState.BATHING_ON_KNEES,BodyState.BATHING_ON_KNEES,
			0.3f,new Loli.AnimLogicInfo(1,0.2f,0.5f,12,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.05f, (int)Loli.AnimationEventName.HEADPAT_PROPER_SOUND, null ),
				new LoliAnimationEvent( 0.45f, (int)Loli.AnimationEventName.HEADPAT_PROPER_SOUND, null ),
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.BATHTUB_PLAY_SOUND, new float[]{ (float)Bathtub.SoundType.WATER_MOVEMENT } ),
			}
		));

		RegisterAnimation(Loli.Animation.BATHTUB_ON_KNEES_HEADPAT_BRUSH_AWAY_TO_ANGRY_IDLE,
			new Loli.AnimationInfo(idleTransition,
			"bathtub_on_knees_headpat_brush_away_to_angry_idle","bathtub_on_knees_headpat_brush_away_to_angry_idle","bathtub_on_knees_headpat_brush_away_to_angry_idle",
			Loli.Priority.HIGH,BodyState.BATHING_ON_KNEES,BodyState.BATHING_IDLE,
			0.2f,new Loli.AnimLogicInfo(0,1.0f,0.3f,0,0.3f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.1f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.ANGRY_LONG, 0.0f } ),
				new LoliAnimationEvent( 0.2f, (int)Loli.AnimationEventName.BATHTUB_PLAY_SOUND, new float[]{ (float)Bathtub.SoundType.WATER_KERPLUNK } ),
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.BATHTUB_PLAY_SOUND, new float[]{ (float)Bathtub.SoundType.WATER_MOVEMENT } ),
			}
		));
		
		RegisterAnimation(Loli.Animation.STAND_HEADPAT_HAPPY_START,
			new Loli.AnimationInfo(new Loli.DefaultTransition(Loli.Animation.STAND_HEADPAT_HAPPY_LOOP),
			"stand_headpat_happy_start","stand_happy_loop","stand_headpat_happy_start",
			Loli.Priority.MEDIUM,BodyState.STAND,BodyState.STAND,
			0.4f,new Loli.AnimLogicInfo(2,1.0f,0.5f,12,0.4f),
			new LoliAnimationEvent[]{			
				new LoliAnimationEvent( 0.05f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.DISAPPOINTED_STARTLE, 0.0f } )
			}
		));

		RegisterAnimation(Loli.Animation.STAND_HEADPAT_HAPPY_AFTER_MORE,
			new Loli.AnimationInfo(new Loli.DefaultTransition(Loli.Animation.STAND_HEADPAT_HAPPY_LOOP),
			"stand_headpat_happy_after_more","stand_happy_loop","stand_headpat_happy_after_more",
			Loli.Priority.MEDIUM,BodyState.STAND,BodyState.STAND,
			0.2f,new Loli.AnimLogicInfo(2,1.0f,0.5f,12,0.4f),
			
			new LoliAnimationEvent[]{			
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.HEADPAT_RETURN, 0.0f } ),
			}
		));

		RegisterAnimation(Loli.Animation.STAND_HEADPAT_HAPPY_WANTED_MORE,
			new Loli.AnimationInfo(idleTransition,
			"stand_headpat_happy_wanted_more","stand_happy_loop","stand_headpat_happy_wanted_more",
			Loli.Priority.MEDIUM,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(3,1.0f,0.5f,6,0.4f),
			new LoliAnimationEvent[]{			
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.DISAPPOINTED_STARTLE, 0.0f } ),
				new LoliAnimationEvent( 0.3f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.DISAPPOINTED_SIGH, 0.0f } ),
				new LoliAnimationEvent( 0.3f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 0.0f, 1.0f, 0.8f } ),
				new LoliAnimationEvent( 0.3f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 0.0f, 0.3f } ),
				new LoliAnimationEvent( 0.8f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 3.0f, 1.0f, 0.8f } ),
				new LoliAnimationEvent( 0.9f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 12.0f, 0.3f } ),
			}
		));

		RegisterAnimation(Loli.Animation.STAND_HEADPAT_HAPPY_IDLE_SAD_RIGHT,
			new Loli.AnimationInfo(new Loli.DefaultTransition(Loli.Animation.STAND_HEADPAT_HAPPY_LOOP),
			"stand_headpat_happy_idle_sad_right","stand_headpat_happy_idle_sad_right","stand_headpat_happy_idle_sad_right",
			Loli.Priority.MEDIUM,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(1,0.4f,0.5f,12,0.5f),
			new LoliAnimationEvent[]{			
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.WHINE_SOFT, 0.0f } ),
				new LoliAnimationEvent( 0.34f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 0.0f, 1.0f, 0.6f } ),
				new LoliAnimationEvent( 0.34f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 0.0f, 0.3f } ),
				new LoliAnimationEvent( 0.85f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 12.0f, 0.3f } ),
			}
		),true);

		RegisterAnimation(Loli.Animation.STAND_HEADPAT_HAPPY_SATISFACTION,
			new Loli.AnimationInfo(idleTransition,
			"stand_headpat_satisfaction","stand_headpat_satisfaction","stand_headpat_satisfaction",
			Loli.Priority.MEDIUM,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(0,1.0f,0.5f,0,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.ECSTATIC, 0.0f } ),
			}
		));

		RegisterAnimation(Loli.Animation.STAND_HEADPAT_HAPPY_LOOP,
			new Loli.AnimationInfo(null,
			"stand_headpat_happy_loop","stand_happy_loop","stand_headpat_happy_loop",
			Loli.Priority.LOW,BodyState.STAND,BodyState.STAND,
			0.5f,new Loli.AnimLogicInfo(0,0.5f,0.5f,12,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.05f, (int)Loli.AnimationEventName.HEADPAT_PROPER_SOUND, null ),
				new LoliAnimationEvent( 0.45f, (int)Loli.AnimationEventName.HEADPAT_PROPER_SOUND, null )
			}
		));

		RegisterAnimation(Loli.Animation.STAND_HEADPAT_ANGRY_BRUSH_AWAY,
			new Loli.AnimationInfo(idleTransition,
			"stand_headpat_angry_brush_away","stand_angry_loop","stand_headpat_angry_brush_away",
			Loli.Priority.HIGH,BodyState.STAND,BodyState.STAND,
			0.2f,new Loli.AnimLogicInfo(0,1.0f,0.5f,0,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.1f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.ANGRY_LONG, 1.0f } )
			}
		));

		RegisterAnimation(Loli.Animation.STAND_HEADPAT_ANGRY_LOOP,
			new Loli.AnimationInfo(null,
			"stand_headpat_angry_start","stand_angry_loop","stand_headpat_angry_start",
			Loli.Priority.MEDIUM,BodyState.STAND,BodyState.STAND,
			0.2f,new Loli.AnimLogicInfo(0,0.5f,0.5f,7,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.1f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.ANGRY_POUT, 0.0f } )
			}
		));
		
		RegisterAnimation(Loli.Animation.STAND_HEADPAT_ANGRY_END,
			new Loli.AnimationInfo(idleTransition,
			"stand_headpat_angry_end","stand_headpat_angry_end","stand_headpat_angry_end",
			Loli.Priority.MEDIUM,BodyState.STAND,BodyState.STAND,
			0.2f,new Loli.AnimLogicInfo(0,1.0f,0.55f,12,0.4f),
			new LoliAnimationEvent[]{			
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.ANGRY_GRUMBLE_LONG, 0.0f } ),
				new LoliAnimationEvent( 0.46f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 0.0f, 1.1f } ),
				new LoliAnimationEvent( 0.76f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 3.0f, 1.0f, 0.7f } ),
				new LoliAnimationEvent( 0.76f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 12.0f, 1.0f } )
			}
		));

		RegisterAnimation(Loli.Animation.STAND_HEADPAT_CLIMAX_TO_CANCEL_RIGHT,
			new Loli.AnimationInfo(idleTransition,
			"stand_headpat_climax_to_cancel_right","stand_angry_loop","stand_headpat_climax_to_cancel_right",
			Loli.Priority.HIGH,BodyState.STAND,BodyState.STAND,
			0.15f,new Loli.AnimLogicInfo(0,1.0f,0.85f,0,0.4f),
			new LoliAnimationEvent[]{			
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.ANGRY_LONG, 0.0f } ),
				new LoliAnimationEvent( 0.4f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 12.0f, 1.0f } )
			}
		),true);

		RegisterAnimation(Loli.Animation.STAND_HEADPAT_CLIMAX_TO_HAPPY,
			new Loli.AnimationInfo(new Loli.DefaultTransition(Loli.Animation.STAND_HEADPAT_HAPPY_LOOP),
			"stand_headpat_climax_to_happy","stand_angry_loop","stand_headpat_climax_to_happy",
			Loli.Priority.HIGH,BodyState.STAND,BodyState.STAND,
			0.3f,null,
			new LoliAnimationEvent[]{			
				new LoliAnimationEvent( 0.1f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.ANGRY_HEADPAT_B, 0.0f } ),
			}
		));

		RegisterAnimation(Loli.Animation.STAND_HEADPAT_INTERRUPT,
			new Loli.AnimationInfo(idleTransition,
			"stand_headpat_angry_interrupt","stand_angry_loop","stand_headpat_angry_interrupt",
			Loli.Priority.MEDIUM,BodyState.STAND,BodyState.STAND,
			0.2f,new Loli.AnimLogicInfo(0,1.0f,0.5f,0,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.1f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.HUMPH, 0.0f } )
			}	
		));

		RegisterAnimation(Loli.Animation.STAND_TIRED_HEADPAT_IDLE,
			new Loli.AnimationInfo(null,
			"stand_tired_headpat_idle_loop","stand_tired_headpat_idle_loop","stand_tired_headpat_idle_loop",
			Loli.Priority.MEDIUM,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(3,0.4f,1.5f,0,0.4f),null
		));

		RegisterAnimation(Loli.Animation.STAND_FACE_PROX_ANGRY_SURPRISE,
			new Loli.AnimationInfo(idleTransition,
			"stand_face_prox_angry_surprise","stand_angry_loop","stand_face_prox_angry_surprise",
			Loli.Priority.HIGH,BodyState.STAND,BodyState.STAND,
			0.2f,new Loli.AnimLogicInfo(3,0.2f,1.5f,4,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.SCARED_SHORT, 0.0f } )
			}
			));

		RegisterAnimation(Loli.Animation.STAND_FACE_PROX_ANGRY_LOOP,
			new Loli.AnimationInfo(null,
			"stand_face_prox_angry_loop","stand_angry_loop","stand_face_prox_angry_loop",
			Loli.Priority.LOW,BodyState.STAND,BodyState.STAND,
			0.25f,new Loli.AnimLogicInfo(1,0.6f,0.5f,12,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.112f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.ANGRY_GRUMBLE_SHORT, 0.0f } ),
				new LoliAnimationEvent( 0.412f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.ANGRY_GRUMBLE_LONG, 0.0f } ),
				new LoliAnimationEvent( 0.837f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.ANGRY_GRUMBLE_LONG, 0.0f } )
			},
			(int)Loli.AnimationInfo.Flag.IDLE_STATE
		));

		RegisterAnimation(Loli.Animation.STAND_KISS_ANGRY_CHEEK_RIGHT,
			new Loli.AnimationInfo(new KissingBehavior.TransitionToPostKiss(),
			"stand_kiss_angry_cheek_right","stand_angry_loop","stand_kiss_angry_cheek_right",
			Loli.Priority.HIGH,BodyState.STAND,BodyState.STAND,
			0.2f,new Loli.AnimLogicInfo(0,1.0f,2.0f,0,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.112f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.KISSED_ANGRY, 1.0f } ),
			}
		));

		RegisterAnimation(Loli.Animation.STAND_KISS_ANGRY_CHEEK_LEFT,
			new Loli.AnimationInfo(new KissingBehavior.TransitionToPostKiss(),
			"stand_kiss_angry_cheek_left","stand_angry_loop","stand_kiss_angry_cheek_left",
			Loli.Priority.HIGH,BodyState.STAND,BodyState.STAND,
			0.2f,new Loli.AnimLogicInfo(0,1.0f,2.0f,0,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.112f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.KISSED_ANGRY, 1.0f } ),
			}
		));

		RegisterAnimation(Loli.Animation.STAND_KISS_ANGRY_LEFT_TO_HAPPY,
			new Loli.AnimationInfo(idleTransition,
			"stand_kiss_angry_left_to_happy","stand_angry_loop","stand_kiss_angry_left_to_happy",
			Loli.Priority.LOW,BodyState.STAND,BodyState.STAND,
			0.2f,new Loli.AnimLogicInfo(0,1.0f,0.5f,0,0.2f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.16f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 12.0f, 0.3f } ),
				new LoliAnimationEvent( 0.16f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 3.0f, 0.3f, 0.5f } ),
				new LoliAnimationEvent( 0.26f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 0.0f, 0.3f } ),
				new LoliAnimationEvent( 0.35f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 12.0f, 0.3f } ),
				new LoliAnimationEvent( 0.35f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 3.0f, 1.0f, 0.5f } ),
				new LoliAnimationEvent( 0.55f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 0.0f, 0.3f } ),
				new LoliAnimationEvent( 0.52f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 0.0f, 1.0f, 0.5f } ),
				new LoliAnimationEvent( 0.80f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 12.0f, 0.3f } ),
				new LoliAnimationEvent( 0.80f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 3.0f, 1.0f, 0.7f } ),
				new LoliAnimationEvent( 0.5f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.RELIEF, 1.0f } ),
			}
		));

		RegisterAnimation(Loli.Animation.STAND_KISS_ANGRY_RIGHT_TO_HAPPY,
			new Loli.AnimationInfo(idleTransition,
			"stand_kiss_angry_right_to_happy","stand_angry_loop","stand_kiss_angry_right_to_happy",
			Loli.Priority.LOW,BodyState.STAND,BodyState.STAND,
			0.2f,new Loli.AnimLogicInfo(0,1.0f,0.5f,0,0.2f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.16f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 12.0f, 0.3f } ),
				new LoliAnimationEvent( 0.16f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 3.0f, 0.3f, 0.5f } ),
				new LoliAnimationEvent( 0.26f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 0.0f, 0.3f } ),
				new LoliAnimationEvent( 0.35f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 12.0f, 0.3f } ),
				new LoliAnimationEvent( 0.35f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 3.0f, 1.0f, 0.5f } ),
				new LoliAnimationEvent( 0.55f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 0.0f, 0.3f } ),
				new LoliAnimationEvent( 0.52f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 0.0f, 1.0f, 0.5f } ),
				new LoliAnimationEvent( 0.80f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 12.0f, 0.3f } ),
				new LoliAnimationEvent( 0.80f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 3.0f, 1.0f, 0.7f } ),
				new LoliAnimationEvent( 0.5f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.RELIEF, 1.0f } ),
			}
		));

		RegisterAnimation(Loli.Animation.STAND_FACE_PROX_HAPPY_SURPRISE,
			new Loli.AnimationInfo(idleTransition,
			"stand_face_prox_happy_surprise","stand_happy_loop","stand_face_prox_happy_surprise",
			Loli.Priority.HIGH,BodyState.STAND,BodyState.STAND,
			0.2f,new Loli.AnimLogicInfo(3,1.0f,0.7f,12,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.SCARED_SHORT, 0.0f } ),
			}
		));

		RegisterAnimation(Loli.Animation.STAND_FACE_PROX_HAPPY_LOOP,
			new Loli.AnimationInfo(idleTransition,
			"stand_face_prox_happy_loop","stand_happy_loop","stand_face_prox_happy_loop",
			Loli.Priority.LOW,BodyState.STAND,BodyState.STAND,
			0.25f,new Loli.AnimLogicInfo(1,0.6f,0.5f,12,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.075f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.EMBARRASSED, 1.0f } ),
				new LoliAnimationEvent( 0.5f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.MISC_IDLE, 0.0f } ),
			},
			(int)Loli.AnimationInfo.Flag.IDLE_STATE
		));

		RegisterAnimation(Loli.Animation.STAND_KISS_HAPPY_CHEEK_RIGHT,
			new Loli.AnimationInfo(idleTransition,
			"stand_kiss_happy_cheek_right","stand_kiss_happy_cheek_right","stand_kiss_happy_cheek_right",
			Loli.Priority.MEDIUM,BodyState.STAND,BodyState.STAND,
			0.2f,new Loli.AnimLogicInfo(0,1.0f,1.5f,12,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.KISSED_HAPPY, 1.0f } ),
			}
		));

		RegisterAnimation(Loli.Animation.STAND_KISS_HAPPY_CHEEK_LEFT,
			new Loli.AnimationInfo(idleTransition
			,"stand_kiss_happy_cheek_left","stand_kiss_happy_cheek_left","stand_kiss_happy_cheek_left",
			Loli.Priority.MEDIUM,BodyState.STAND,BodyState.STAND,
			0.2f,new Loli.AnimLogicInfo(0,1.0f,1.5f,12,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.KISSED_HAPPY, 1.0f } ),
			}
		));

		RegisterAnimation(Loli.Animation.STAND_KISS_ANGRY_LEFT_TO_ANGRY,
			new Loli.AnimationInfo(idleTransition
			,"stand_kiss_angry_left_to_angry","stand_angry_loop","stand_kiss_angry_left_to_angry",
			Loli.Priority.LOW,BodyState.STAND,BodyState.STAND,
			0.25f,new Loli.AnimLogicInfo(0,1.0f,1.5f,12,0.8f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.112f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.ANGRY_SHORT, 1.0f } ),
			}
		));

		RegisterAnimation(Loli.Animation.STAND_KISS_ANGRY_RIGHT_TO_ANGRY,
			new Loli.AnimationInfo(idleTransition
			,"stand_kiss_angry_right_to_angry","stand_angry_loop","stand_kiss_angry_right_to_angry",
			Loli.Priority.LOW,BodyState.STAND,BodyState.STAND,
			0.25f,new Loli.AnimLogicInfo(0,1.0f,1.5f,12,0.8f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.112f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.ANGRY_SHORT, 1.0f } ),
			}
		));

		RegisterAnimation(Loli.Animation.STAND_REACT_PERV_FRONT_IN,
			new Loli.AnimationInfo(new Loli.DefaultTransition(Loli.Animation.STAND_REACT_PERV_FRONT_IDLE),
			"stand_react_perv_front_in","stand_react_perv_front_in","stand_react_perv_front_in",
			Loli.Priority.HIGH,BodyState.STAND,BodyState.STAND,
			0.15f,new Loli.AnimLogicInfo(1,0.5f,1.5f,12,1.0f),null));

		RegisterAnimation(Loli.Animation.STAND_REACT_PERV_FRONT_IDLE,
			new Loli.AnimationInfo(null,
			"stand_react_perv_front_idle_loop","stand_react_perv_front_idle_loop","stand_react_perv_front_idle_loop",
			Loli.Priority.HIGH,BodyState.STAND,BodyState.STAND,
			0.2f,new Loli.AnimLogicInfo(1,0.5f,1.5f,12,1.0f),new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.112f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.WHAT_YOU_DOING, 3.0f } ),
			}
		));

		RegisterAnimation(Loli.Animation.STAND_REACT_PERV_FRONT_OUT,
			new Loli.AnimationInfo(idleTransition,
			"stand_react_perv_front_out","stand_react_perv_front_out","stand_react_perv_front_out",
			Loli.Priority.HIGH,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(3,0.5f,1.5f,7,1.0f),null));
		
		RegisterAnimation(Loli.Animation.STAND_TIRED_LOCOMOTION,
			new Loli.AnimationInfo(null,
			"stand_tired_idle","stand_tired_idle","stand_tired_idle",
			Loli.Priority.LOW,BodyState.STAND,BodyState.STAND,
			0.15f,new Loli.AnimLogicInfo(3,0.5f,1.5f,7,1.0f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.BIND_ANIMATION_TO_VIEW_MODE, new float[]{ (float)Loli.AwarenessMode.TIRED } ),
				
			},
			(int)Loli.AnimationInfo.Flag.IDLE_STATE
		));
		
		RegisterAnimation(Loli.Animation.STAND_SCARED_LOCOMOTION,
			new Loli.AnimationInfo(null,
			"stand_scared_locomotion","stand_scared_locomotion","stand_scared_locomotion",
			Loli.Priority.LOW,BodyState.STAND,BodyState.STAND,
			0.4f,new Loli.AnimLogicInfo(0,1.0f,0.1f,12,1.0f),
			null,
			(int)Loli.AnimationInfo.Flag.IDLE_STATE
		));
		RegisterLegSpeedInfo( "stand_scared_locomotion", 2.0f, 2.5f, 350.0f, 45.0f );
		
		RegisterAnimation(Loli.Animation.STAND_SCARED_STARTLE,
			new Loli.AnimationInfo(new Loli.DefaultTransition(Loli.Animation.STAND_SCARED_LOCOMOTION),
			"stand_scared_startled","stand_scared_startled","stand_scared_startled",
			Loli.Priority.LOW,BodyState.STAND,BodyState.STAND,
			0.15f,new Loli.AnimLogicInfo(0,1.0f,0.1f,12,1.0f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.SCARED_SHORT, 0.0f } ),
			}
		));

        RegisterAnimation(Loli.Animation.STAND_TO_STAND_TIRED,
			new Loli.AnimationInfo(new Loli.DefaultTransition(Loli.Animation.STAND_TIRED_LOCOMOTION),
			"stand_to_stand_tired","stand_to_stand_tired","stand_to_stand_tired",
			Loli.Priority.MEDIUM,BodyState.STAND,BodyState.STAND,
			0.2f,new Loli.AnimLogicInfo(0,1.0f,1.0f,0,0.5f),
            new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.05f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.YAWN_LONG, 1.0f } ),
				new LoliAnimationEvent( 0.7f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 3, 0.5f, 0.8f } ),
			}
        ));

		RegisterAnimation(Loli.Animation.STAND_TIRED_RUB_EYES_RIGHT,
			new Loli.AnimationInfo(new Loli.DefaultTransition(Loli.Animation.STAND_TIRED_LOCOMOTION),
			"stand_tired_rub_eyes_right","stand_tired_rub_eyes_right","stand_tired_rub_eyes_right",
			Loli.Priority.MEDIUM,BodyState.STAND,BodyState.STAND,
			0.2f,new Loli.AnimLogicInfo(0,1.0f,0.5f,0,0.6f),
            new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.4f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.YAWN_SHORT, 1.0f } ),
				new LoliAnimationEvent( 0.7f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 3, 0.5f, 0.8f } ),
			},
			(int)Loli.AnimationInfo.Flag.IDLE_STATE
        ));

        RegisterAnimation(Loli.Animation.STAND_TIRED_RUB_EYES_LEFT,
			new Loli.AnimationInfo(new Loli.DefaultTransition(Loli.Animation.STAND_TIRED_LOCOMOTION),
			"stand_tired_rub_eyes_left","stand_tired_rub_eyes_left","stand_tired_rub_eyes_left",
			Loli.Priority.MEDIUM,BodyState.STAND,BodyState.STAND,
			0.2f,new Loli.AnimLogicInfo(0,1.0f,0.5f,0,0.6f),
            new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.4f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.YAWN_SHORT, 1.0f } ),
				new LoliAnimationEvent( 0.7f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 3, 0.5f, 0.8f } ),
			},
			(int)Loli.AnimationInfo.Flag.IDLE_STATE
        ));

		RegisterLegSpeedInfo( "stand_tired_idle", 0.95f, 2.0f, 255.0f, 30.0f );
		PokeBehavior.TransitionToPostFacePokeAnim postFacePokeAnimTransition = new PokeBehavior.TransitionToPostFacePokeAnim();
        RegisterAnimation(Loli.Animation.BATHTUB_RELAX_FACE_POKE_RIGHT,
			new Loli.AnimationInfo(postFacePokeAnimTransition,
			"bathtub_relax_face_poke_right_nm","bathtub_relax_face_poke_right_nm","bathtub_relax_face_poke_right_nm",
			Loli.Priority.HIGH,BodyState.BATHING_RELAX,BodyState.BATHING_RELAX,
			0.1f,new Loli.AnimLogicInfo(1,0.4f,0.2f,12,0.4f),
			new LoliAnimationEvent[]{			
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.STARTLE_SHORT, 0.0f } ),
				new LoliAnimationEvent( 0.4f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 1.0f, 1.0f, 0.5f } ),
			}
		),true);
		
		RegisterAnimation(Loli.Animation.BATHTUB_IDLE_FACE_POKE_1_RIGHT,
			new Loli.AnimationInfo(postFacePokeAnimTransition,
			"bathtub_idle_face_poke_1_right_nm","bathtub_idle_face_poke_1_right_nm","bathtub_idle_face_poke_1_right_nm",
			Loli.Priority.HIGH,BodyState.BATHING_IDLE,BodyState.BATHING_IDLE,
			0.15f,new Loli.AnimLogicInfo(1,0.4f,0.2f,12,0.4f),
			new LoliAnimationEvent[]{			
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.STARTLE_SHORT, 0.0f } ),
				new LoliAnimationEvent( 0.4f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 1.0f, 1.0f, 0.5f } ),
			}
		),true);
		RegisterAnimation(Loli.Animation.BATHTUB_IDLE_FACE_POKE_2_RIGHT,
			new Loli.AnimationInfo(postFacePokeAnimTransition,
			"bathtub_idle_face_poke_2_right_nm","bathtub_idle_face_poke_2_right_nm","bathtub_idle_face_poke_2_right_nm",
			Loli.Priority.HIGH,BodyState.BATHING_IDLE,BodyState.BATHING_IDLE,
			0.15f,new Loli.AnimLogicInfo(1,0.4f,0.2f,12,0.4f),
			new LoliAnimationEvent[]{			
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.STARTLE_SHORT, 0.0f } ),
				new LoliAnimationEvent( 0.4f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 1.0f, 1.0f, 0.5f } ),
			}
		),true);

        RegisterAnimation(Loli.Animation.STAND_TIRED_POKE_RIGHT,
			new Loli.AnimationInfo(postFacePokeAnimTransition,
			"stand_tired_poke_right","stand_tired_poke_right","stand_tired_poke_right",
			Loli.Priority.HIGH,BodyState.STAND,BodyState.STAND,
			0.1f,new Loli.AnimLogicInfo(1,0.2f,0.2f,12,0.4f),
			new LoliAnimationEvent[]{			
				new LoliAnimationEvent( 0.4f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.WHIMPER_SHORT, 0.0f } ),
				new LoliAnimationEvent( 0.4f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 1.0f, 1.0f, 1.5f } ),
			}
		),true);

		RegisterAnimation(Loli.Animation.SLEEP_PILLOW_SIDE_BOTHER_RIGHT,
			new Loli.AnimationInfo(postFacePokeAnimTransition,
			"sleep_side_pillow_bother_right","sleep_side_pillow_bother_right","sleep_side_pillow_bother_right",
			Loli.Priority.MEDIUM,BodyState.SLEEP_PILLOW_SIDE_RIGHT,BodyState.SLEEP_PILLOW_SIDE_RIGHT,
			0.1f,new Loli.AnimLogicInfo(0,0.5f,0.3f,0,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.SNORE, 0.0f } )
			}
		),true);

		RegisterAnimation(Loli.Animation.SLEEP_PILLOW_UP_BOTHER_RIGHT,
			new Loli.AnimationInfo(postFacePokeAnimTransition,
			"sleep_pillow_up_bother_right_nm","sleep_pillow_up_bother_right_nm","sleep_pillow_up_bother_right_nm",
			Loli.Priority.MEDIUM,BodyState.SLEEP_PILLOW_UP,BodyState.SLEEP_PILLOW_UP,
			0.1f,new Loli.AnimLogicInfo(0,0.5f,0.3f,0,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.SLEEP_DISTURBED_SHORT, 0.0f } )
			}
		),true);


        RegisterAnimation(Loli.Animation.AWAKE_PILLOW_UP_FACE_POKE_RIGHT,
			new Loli.AnimationInfo( postFacePokeAnimTransition,
			"awake_pillow_up_face_poke_right_nm","awake_pillow_up_face_poke_right_nm","awake_pillow_up_face_poke_right_nm",
			Loli.Priority.HIGH,BodyState.AWAKE_PILLOW_UP,BodyState.AWAKE_PILLOW_UP,
			0.1f,new Loli.AnimLogicInfo(0,0.2f,0.2f,12,0.4f),
			new LoliAnimationEvent[]{			
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.STARTLE_SHORT, 0.0f } ),
			}
		),true);

		RegisterAnimation(Loli.Animation.STAND_POKE_FACE_1_RIGHT,
			new Loli.AnimationInfo(postFacePokeAnimTransition,
			"stand_poke_face_1_right","stand_poke_face_1_right","stand_poke_face_1_right",
			Loli.Priority.HIGH,BodyState.STAND,BodyState.STAND,
			0.15f,new Loli.AnimLogicInfo(1,0.4f,0.3f,12,0.4f),
			new LoliAnimationEvent[]{			
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.STARTLE_SHORT, 0.0f } ),
				new LoliAnimationEvent( 0.4f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 3.0f, 1.0f, 0.5f } ),
			}
		));
		
		RegisterAnimation(Loli.Animation.STAND_POKE_FACE_1_LEFT,
			new Loli.AnimationInfo(postFacePokeAnimTransition,
			"stand_poke_face_1_left","stand_poke_face_1_left","stand_poke_face_1_left",
			Loli.Priority.HIGH,BodyState.STAND,BodyState.STAND,
			0.15f,new Loli.AnimLogicInfo(1,0.4f,0.3f,12,0.4f),
			new LoliAnimationEvent[]{			
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.STARTLE_SHORT, 0.0f } ),
				new LoliAnimationEvent( 0.4f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 3.0f, 1.0f, 0.5f } ),
			}
		));
		RegisterAnimation(Loli.Animation.STAND_POKE_FACE_2_RIGHT,
			new Loli.AnimationInfo(postFacePokeAnimTransition,
			"stand_poke_face_2_right","stand_poke_face_2_right","stand_poke_face_2_right",
			Loli.Priority.HIGH,BodyState.STAND,BodyState.STAND,
			0.15f,new Loli.AnimLogicInfo(1,0.4f,0.3f,12,0.4f),
			new LoliAnimationEvent[]{			
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.STARTLE_SHORT, 0.0f } ),
				new LoliAnimationEvent( 0.5f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 3.0f, 1.0f, 0.7f} ),
			}
		));
		RegisterAnimation(Loli.Animation.STAND_POKE_FACE_2_LEFT,
			new Loli.AnimationInfo(postFacePokeAnimTransition,
			"stand_poke_face_2_left","stand_poke_face_2_left","stand_poke_face_2_left",
			Loli.Priority.HIGH,BodyState.STAND,BodyState.STAND,
			0.15f,new Loli.AnimLogicInfo(1,0.4f,0.3f,12,0.4f),
			new LoliAnimationEvent[]{			
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.STARTLE_SHORT, 0.0f } ),
				new LoliAnimationEvent( 0.5f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 3.0f, 1.0f, 0.7f} ),
			}
		));
		RegisterAnimation(Loli.Animation.STAND_POKE_FACE_3_RIGHT,
			new Loli.AnimationInfo(postFacePokeAnimTransition,
			"stand_poke_face_3_right","stand_poke_face_3_right","stand_poke_face_3_right",
			Loli.Priority.HIGH,BodyState.STAND,BodyState.STAND,
			0.15f,new Loli.AnimLogicInfo(1,0.4f,0.3f,12,0.4f),
			new LoliAnimationEvent[]{			
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.STARTLE_SHORT, 0.0f } ),
				new LoliAnimationEvent( 0.5f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 3.0f, 1.0f, 0.7f} ),
			}
		));
		RegisterAnimation(Loli.Animation.STAND_POKE_FACE_3_LEFT,
			new Loli.AnimationInfo(postFacePokeAnimTransition,
			"stand_poke_face_3_left","stand_poke_face_3_left","stand_poke_face_3_left",
			Loli.Priority.HIGH,BodyState.STAND,BodyState.STAND,
			0.15f,new Loli.AnimLogicInfo(1,0.4f,0.3f,12,0.4f),
			new LoliAnimationEvent[]{			
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.STARTLE_SHORT, 0.0f } ),
				new LoliAnimationEvent( 0.5f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 3.0f, 1.0f, 0.7f} ),
			}
		));

		RegisterAnimation(Loli.Animation.STAND_WIPE_CHEEK_RIGHT,
			new Loli.AnimationInfo(idleTransition,
			"stand_wipe_cheek_right","stand_wipe_cheek_right","stand_wipe_cheek_right",
			Loli.Priority.LOW,BodyState.STAND,BodyState.STAND,
			0.2f,new Loli.AnimLogicInfo(0,1.0f,0.3f,0,0.3f),
			new LoliAnimationEvent[]{			
				new LoliAnimationEvent( 0.45f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 12.0f, 0.4f } ),
				new LoliAnimationEvent( 0.45f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 3.0f, 1.0f, 0.4f} ),
				new LoliAnimationEvent( 0.45f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.ANGRY_GRUMBLE_SHORT, 0.0f } ),
				new LoliAnimationEvent( 0.71f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.ANGRY_HEADPAT_A, 0.0f } ),
			}
		));

		RegisterAnimation(Loli.Animation.STAND_WIPE_CHEEK_LEFT,
			new Loli.AnimationInfo(idleTransition,
			"stand_wipe_cheek_left","stand_wipe_cheek_left","stand_wipe_cheek_left",
			Loli.Priority.LOW,BodyState.STAND,BodyState.STAND,
			0.2f,new Loli.AnimLogicInfo(0,1.0f,0.3f,0,0.3f),
			new LoliAnimationEvent[]{			
				new LoliAnimationEvent( 0.45f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 12.0f, 0.4f } ),
				new LoliAnimationEvent( 0.45f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 3.0f, 1.0f, 0.4f} ),
				new LoliAnimationEvent( 0.45f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.ANGRY_GRUMBLE_SHORT, 0.0f } ),
				new LoliAnimationEvent( 0.71f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.ANGRY_HEADPAT_A, 0.0f } ),
			}
		));

		RegisterAnimation(Loli.Animation.STAND_ANGRY_BLOCK_RIGHT,
			new Loli.AnimationInfo(idleTransition,"stand_angry_block_right","stand_angry_block_right","stand_angry_block_right",
			Loli.Priority.HIGH,BodyState.STAND,BodyState.STAND,
			0.15f,new Loli.AnimLogicInfo(0,1.0f,0.4f,12,0.4f),
			new LoliAnimationEvent[]{			
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.ANGRY_LONG, 0.0f } ),
				new LoliAnimationEvent( 0.3f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 12.0f, 0.8f } ),
				new LoliAnimationEvent( 0.3f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 1.0f, 1.0f, 1.0f} ),
			}
		));

		RegisterAnimation(Loli.Animation.STAND_ANGRY_BLOCK_LEFT,
			new Loli.AnimationInfo(idleTransition,"stand_angry_block_left","stand_angry_block_left","stand_angry_block_left",
			Loli.Priority.HIGH,BodyState.STAND,BodyState.STAND,
			0.15f,new Loli.AnimLogicInfo(0,1.0f,0.4f,12,0.4f),
			new LoliAnimationEvent[]{			
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.ANGRY_LONG, 0.0f } ),
				new LoliAnimationEvent( 0.3f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 12.0f, 0.8f } ),
				new LoliAnimationEvent( 0.3f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 1.0f, 1.0f, 1.0f} ),
			}
		));

		RegisterAnimation(Loli.Animation.STAND_POKED_TUMMY_IN,
			new Loli.AnimationInfo(new PokeBehavior.TransitionToPostTummyPokeAnim()
			,"stand_poked_tummy_in","stand_poked_tummy_in","stand_poked_tummy_in",
			Loli.Priority.HIGH,BodyState.STAND,BodyState.STAND,
			0.25f,new Loli.AnimLogicInfo(0,1.0f,0.3f,0,0.3f),
			null
		));

		RegisterAnimation(Loli.Animation.STAND_POKED_TUMMY_LOOP,
			new Loli.AnimationInfo(null,"stand_poked_tummy_loop","stand_poked_tummy_loop","stand_poked_tummy_loop",
			Loli.Priority.MEDIUM,BodyState.STAND,BodyState.STAND,
			0.2f,new Loli.AnimLogicInfo(0,1.0f,0.4f,12,0.4f),
			null
		));
		
		RegisterAnimation(Loli.Animation.STAND_POKED_TUMMY_OUT,
			new Loli.AnimationInfo(idleTransition
			,"stand_poked_tummy_out","stand_poked_tummy_out","stand_poked_tummy_out",
			Loli.Priority.MEDIUM,BodyState.STAND,BodyState.STAND,
			0.4f,new Loli.AnimLogicInfo(0,1.0f,0.3f,0,0.3f),
			new LoliAnimationEvent[]{			
				new LoliAnimationEvent( 0.2f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 12.0f, 0.4f } ),
				new LoliAnimationEvent( 0.2f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 3.0f, 1.0f, 1.8f } ),
				new LoliAnimationEvent( 0.68f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.RELIEF, 0.0f } ),
			}
		));

		RegisterAnimation(Loli.Animation.STAND_CHASE_LOW_LOCOMOTION,
			new Loli.AnimationInfo(null,"stand_chase_low_locomotion","stand_chase_low_locomotion","stand_jealous_loop",
			Loli.Priority.LOW,BodyState.STAND,BodyState.STAND,
			0.4f,new Loli.AnimLogicInfo(3,1.0f,0.5f,7,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.FOOTSTEP_SOUND, null ),
				new LoliAnimationEvent( 0.5f, (int)Loli.AnimationEventName.FOOTSTEP_SOUND, null ),
			}
		));
		animationInfos[ Loli.Animation.STAND_CHASE_LOW_LOCOMOTION ].setTorsoSyncFloatID( Animator.StringToHash("sync_chase_low_locomotion") );
		RegisterLegSpeedInfo( "stand_chase_low_locomotion", 2.6f, 3.0f, 255.0f, 30.0f );

		RegisterAnimation(Loli.Animation.STAND_MORTAR_AND_PESTLE_GRIND_LOOP_RIGHT,
			new Loli.AnimationInfo(null,"stand_mortar_and_pestle_grind_loop_right_nm","stand_locomotion_spine_stable","stand_happy_idle",
			Loli.Priority.LOW,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(3,0.5f,0.4f,7,0.4f),null
		),true);

		RegisterAnimation(Loli.Animation.STAND_WHEAT_INTO_MORTAR_RIGHT,
			new Loli.AnimationInfo(idleTransition,"stand_wheat_into_mortar_right_nm","stand_locomotion_spine_stable","stand_happy_idle",
			Loli.Priority.LOW,BodyState.STAND,BodyState.STAND,
			0.4f,new Loli.AnimLogicInfo(0,0.5f,0.4f,7,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.5f, (int)Loli.AnimationEventName.DROP_LEFT_HAND_ITEM, null ),
			}
		),true);
		
		RegisterAnimation(Loli.Animation.STAND_SPLASHED_START_RIGHT,
			new Loli.AnimationInfo(new Loli.DefaultTransition(Loli.Animation.STAND_SPLASHED_END_RIGHT),
			"stand_splashed_start_right","stand_locomotion_spine_stable","stand_splashed_start_right",
			Loli.Priority.MEDIUM,BodyState.STAND,BodyState.STAND,
			0.25f,new Loli.AnimLogicInfo(0,1.0f,0.55f,12,1.0f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.SCREAMING, 1.0f } )
			}
		),true);
		
		RegisterAnimation(Loli.Animation.STAND_SPLASHED_LOOP,
			new Loli.AnimationInfo(idleTransition,"stand_splashed_loop","stand_locomotion_spine_stable","stand_splashed_loop",
			Loli.Priority.MEDIUM,BodyState.STAND,BodyState.STAND,
			0.25f,new Loli.AnimLogicInfo(0,1.0f,0.55f,12,1.2f),
			null
		));

		RegisterAnimation(Loli.Animation.STAND_SPLASHED_END_RIGHT,
			new Loli.AnimationInfo(idleTransition,"stand_splashed_end_right","stand_splashed_end_right","stand_splashed_end_right",
			Loli.Priority.MEDIUM,BodyState.STAND,BodyState.STAND,
			0.4f,new Loli.AnimLogicInfo(0,1.0f,0.5f,0,0.8f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.781f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 3.0f, 1.0f, 1.0f } ),
				new LoliAnimationEvent( 0.781f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 12.0f, 0.4f } ),
			}
		),true);

		RegisterAnimation(Loli.Animation.STAND_MIXING_BOWL_MIX_LOOP_RIGHT,
			new Loli.AnimationInfo(null,"stand_mixing_bowl_mix_right_nm_loop","stand_locomotion_spine_stable","stand_happy_idle",
			Loli.Priority.LOW,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(1,0.5f,0.4f,7,0.4f),null
		),true);
		
		RegisterAnimation(Loli.Animation.STAND_POUR_MORTAR_INTO_MIXING_BOWL_RIGHT,
			new Loli.AnimationInfo(idleTransition,"stand_pour_mortar_into_mixing_bowl_right_nm","stand_locomotion_spine_stable","stand_happy_idle",
			Loli.Priority.LOW,BodyState.STAND,BodyState.STAND,
			0.4f,new Loli.AnimLogicInfo(1,0.5f,0.4f,7,0.4f),null
		),true);
		
		RegisterAnimation(Loli.Animation.STAND_EGG_INTO_MIXING_BOWL_RIGHT,
			new Loli.AnimationInfo(idleTransition,"stand_egg_into_mixing_bowl_right_nm","stand_locomotion_spine_stable","stand_happy_idle",
			Loli.Priority.LOW,BodyState.STAND,BodyState.STAND,
			0.4f,new Loli.AnimLogicInfo(1,0.5f,0.4f,7,0.4f),null
		),true);
		RegisterAnimation(Loli.Animation.BATHTUB_SPLASH_REACT_RIGHT,
			new Loli.AnimationInfo(idleTransition,"bathtub_splash_react_right_nm","bathtub_splash_react_right_nm","bathtub_splash_react_right_nm",
			Loli.Priority.MEDIUM,BodyState.BATHING_IDLE,BodyState.BATHING_IDLE,
			0.2f,new Loli.AnimLogicInfo(0,1.0f,1.0f,12,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.1f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.STARTLE_SHORT, 0.0f } ),
				new LoliAnimationEvent( 0.8f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 1.0f, 1.0f, 0.7f } ),
			}
		),true);

		RegisterAnimation(Loli.Animation.BATHTUB_HAPPY_SPLASH,
			new Loli.AnimationInfo(idleTransition,"bathtub_happy_splash","bathtub_happy_splash","bathtub_happy_splash",
			Loli.Priority.HIGH,BodyState.BATHING_IDLE,BodyState.BATHING_IDLE,
			0.3f,new Loli.AnimLogicInfo(1,0.5f,0.4f,12,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.1f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.LAUGH_SHORT, 1.0f } ),
				new LoliAnimationEvent( 0.3f, (int)Loli.AnimationEventName.BATHTUB_PLAY_SOUND, new float[]{ (float)Bathtub.SoundType.WATER_SPLASH } ),
				new LoliAnimationEvent( 0.3f, (int)Loli.AnimationEventName.BATHTUB_SPLASH, null ),
			}
		));

		RegisterAnimation(Loli.Animation.FLOOR_SIT_HEADPAT_HAPPY_START,
			new Loli.AnimationInfo(new Loli.DefaultTransition(Loli.Animation.FLOOR_SIT_HEADPAT_HAPPY_LOOP),
			"floor_sit_headpat_happy_start","floor_sit_headpat_happy_start","stand_headpat_happy_start",
			Loli.Priority.LOW,BodyState.FLOOR_SIT,BodyState.FLOOR_SIT,
			0.4f,new Loli.AnimLogicInfo(2,1.0f,0.5f,12,0.4f),
			new LoliAnimationEvent[]{			
				new LoliAnimationEvent( 0.05f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.DISAPPOINTED_STARTLE, 0.0f } )
			}
		));

		RegisterAnimation(Loli.Animation.FLOOR_SIT_HEADPAT_HAPPY_WANTED_MORE,
			new Loli.AnimationInfo(idleTransition,
			"floor_sit_headpat_happy_wanted_more","floor_sit_headpat_happy_wanted_more","stand_headpat_happy_wanted_more",
			Loli.Priority.MEDIUM,BodyState.FLOOR_SIT,BodyState.FLOOR_SIT,
			0.3f,new Loli.AnimLogicInfo(3,1.0f,0.5f,6,0.4f),
			new LoliAnimationEvent[]{			
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.DISAPPOINTED_STARTLE, 0.0f } ),
				new LoliAnimationEvent( 0.35f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.DISAPPOINTED_SIGH, 0.0f } ),
				new LoliAnimationEvent( 0.3f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 0.0f, 1.0f, 0.8f } ),
				new LoliAnimationEvent( 0.3f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 0.0f, 0.3f } ),
				new LoliAnimationEvent( 0.8f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 3.0f, 1.0f, 0.8f } ),
				new LoliAnimationEvent( 0.9f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 12.0f, 0.3f } ),
			}
		));

        RegisterAnimation(Loli.Animation.FLOOR_SIT_HEADPAT_HAPPY_LOOP,
			new Loli.AnimationInfo(null,
			"floor_sit_headpat_happy_loop","floor_sit_headpat_happy_loop","stand_headpat_happy_loop",
			Loli.Priority.LOW,BodyState.FLOOR_SIT,BodyState.FLOOR_SIT,
			0.5f,new Loli.AnimLogicInfo(0,0.5f,0.5f,12,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.05f, (int)Loli.AnimationEventName.HEADPAT_PROPER_SOUND, null ),
				new LoliAnimationEvent( 0.45f, (int)Loli.AnimationEventName.HEADPAT_PROPER_SOUND, null )
			}
		));

        RegisterAnimation(Loli.Animation.FLOOR_SIT_HEADPAT_ANGRY_LOOP,
			new Loli.AnimationInfo(null,
			"floor_sit_headpat_angry_loop","floor_sit_headpat_angry_loop","stand_angry_idle",
			Loli.Priority.LOW,BodyState.FLOOR_SIT,BodyState.FLOOR_SIT,
			0.3f,new Loli.AnimLogicInfo(0,1.0f,0.3f,7,0.3f)
		));

		RegisterAnimation(Loli.Animation.FLOOR_SIT_HEADPAT_ANGRY_BRUSH_AWAY,
			new Loli.AnimationInfo(idleTransition,
			"floor_sit_headpat_angry_brush_away","floor_sit_headpat_angry_brush_away","stand_headpat_angry_brush_away",
			Loli.Priority.HIGH,BodyState.FLOOR_SIT,BodyState.FLOOR_SIT,
			0.2f,new Loli.AnimLogicInfo(0,1.0f,0.5f,0,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.1f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.ANGRY_LONG, 1.0f } )
			}
		));

		RegisterAnimation(Loli.Animation.FLOOR_SIT_HEADPAT_ANGRY_CLIMAX_TO_HAPPY,
			new Loli.AnimationInfo(new Loli.DefaultTransition(Loli.Animation.FLOOR_SIT_HEADPAT_HAPPY_LOOP),
			"floor_sit_headpat_climax_to_happy","floor_sit_headpat_climax_to_happy","stand_headpat_climax_to_happy",
			Loli.Priority.HIGH,BodyState.FLOOR_SIT,BodyState.FLOOR_SIT,
			0.3f,null,
			new LoliAnimationEvent[]{			
				new LoliAnimationEvent( 0.1f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.ANGRY_HEADPAT_B, 0.0f } ),
			}
		));
		
		RegisterAnimation(Loli.Animation.STAND_HUG_FAR_TO_STAND,
			new Loli.AnimationInfo(idleTransition,
			"stand_hug_far_to_stand","stand_locomotion_spine_stable","stand_to_stand_hug_far_happy",
			Loli.Priority.MEDIUM,BodyState.STANDING_HUG,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(0,1.0f,0.5f,12,0.4f)
		));

		RegisterAnimation(Loli.Animation.STAND_TO_STAND_HUG_FAR_HAPPY,
			new Loli.AnimationInfo(new DefaultTransition( Loli.Animation.STAND_HUG_HAPPY_LOOP ),
			"stand_to_stand_hug_far_happy","stand_locomotion_spine_stable","stand_to_stand_hug_far_happy",
			Loli.Priority.HIGH,BodyState.STAND,BodyState.STANDING_HUG,
			0.3f,new Loli.AnimLogicInfo(0,1.0f,0.5f,12,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.1f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.GIGGLE, 0.0f } ),
			}
		));
		
		RegisterAnimation(Loli.Animation.STAND_HUG_HAPPY_LOOP,
			new Loli.AnimationInfo(null,
			"stand_hug_happy_loop","stand_locomotion_spine_stable","stand_hug_happy_loop",
			Loli.Priority.LOW,BodyState.STANDING_HUG,BodyState.STANDING_HUG,
			0.3f,new Loli.AnimLogicInfo(0,1.0f,0.5f,12,0.4f),
			null,
			(int)Loli.AnimationInfo.Flag.IDLE_STATE
		));
		
		RegisterAnimation(Loli.Animation.STAND_TO_STAND_HUG_FAR_ANGRY,
			new Loli.AnimationInfo(new DefaultTransition( Loli.Animation.STAND_HUG_ANGRY_LOOP ),
			"stand_to_stand_hug_far_angry","stand_locomotion_spine_stable","stand_to_stand_hug_far_angry",
			Loli.Priority.HIGH,BodyState.STAND,BodyState.STANDING_HUG,
			0.3f,new Loli.AnimLogicInfo(0,1.0f,0.5f,12,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.1f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.ANGRY_POUT, 0.0f } ),
			}
		));
		
		RegisterAnimation(Loli.Animation.STAND_HUG_ANGRY_LOOP,
			new Loli.AnimationInfo(null,
			"stand_hug_angry_loop","stand_locomotion_spine_stable","stand_hug_angry_loop",
			Loli.Priority.HIGH,BodyState.STANDING_HUG,BodyState.STANDING_HUG,
			0.3f,new Loli.AnimLogicInfo(0,1.0f,0.5f,0,0.4f),
			null,
			(int)Loli.AnimationInfo.Flag.IDLE_STATE
		));

        RegisterAnimation(Loli.Animation.FLOOR_SIT_AGREE,
			new Loli.AnimationInfo(idleTransition,
			"floor_sit_agree","floor_sit_agree","floor_sit_agree",
			Loli.Priority.LOW,BodyState.FLOOR_SIT,BodyState.FLOOR_SIT,
			0.3f,new Loli.AnimLogicInfo(1,0.5f,0.3f,12,0.3f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.1f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.GIGGLE, 0.0f } ),
			}
		));

        RegisterAnimation(Loli.Animation.FLOOR_SIT_REFUSE,
			new Loli.AnimationInfo(idleTransition,
			"floor_sit_refuse","floor_sit_refuse","floor_sit_refuse",
			Loli.Priority.LOW,BodyState.FLOOR_SIT,BodyState.FLOOR_SIT,
			0.3f,new Loli.AnimLogicInfo(1,0.0f,0.3f,12,0.3f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.1f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.REFUSE, 0.0f } ),
			}
		));

        RegisterAnimation(Loli.Animation.FLOOR_SIT_BEG_IN_RIGHT,
			new Loli.AnimationInfo(new DefaultTransition( Loli.Animation.FLOOR_SIT_BEG_LOOP_RIGHT),
			"floor_sit_beg_in_right","floor_sit_beg_in_right","floor_sit_beg_in_right",
			Loli.Priority.LOW,BodyState.FLOOR_SIT,BodyState.FLOOR_SIT,
			0.3f,new Loli.AnimLogicInfo(1,0.8f,0.4f,7,0.3f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.1f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.MISC_IDLE, 0.0f } ),
			}
		),true);

        RegisterAnimation(Loli.Animation.FLOOR_SIT_BEG_LOOP_RIGHT,
			new Loli.AnimationInfo(new DefaultTransition( Loli.Animation.FLOOR_SIT_BEG_LOOP_RIGHT),
			"floor_sit_beg_loop_right","floor_sit_beg_loop_right","floor_sit_beg_loop_right",
			Loli.Priority.LOW,BodyState.FLOOR_SIT,BodyState.FLOOR_SIT,
			0.3f,new Loli.AnimLogicInfo(1,0.8f,0.4f,7,0.3f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.1f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.MISC_IDLE, 0.0f } ),
			}
		),true);

        RegisterAnimation(Loli.Animation.FLOOR_SIT_BEG_TO_CARD_FAN_START_RIGHT,
			new Loli.AnimationInfo(new DefaultTransition( Loli.Animation.FLOOR_SIT_BEG_LOOP_RIGHT),
			"floor_sit_beg_to_card_fan_start_right","floor_sit_beg_to_card_fan_start_right","floor_sit_beg_to_card_fan_start_right",
			Loli.Priority.LOW,BodyState.FLOOR_SIT,BodyState.FLOOR_SIT,
			0.3f,new Loli.AnimLogicInfo(0,0.8f,0.2f,0,0.3f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.1f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.MISC_IDLE, 0.0f } ),
			}
		),true);
	
        RegisterAnimation(Loli.Animation.FLOOR_SIT_REACH_RIGHT,
			new Loli.AnimationInfo(idleTransition,
			"floor_sit_reach_right","floor_sit_reach_right","floor_sit_reach_right",
			Loli.Priority.MEDIUM,BodyState.FLOOR_SIT,BodyState.FLOOR_SIT,
			0.3f,new Loli.AnimLogicInfo(0,0.8f,0.2f,0,0.3f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.1f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.MISC_IDLE, 0.0f } ),
				new LoliAnimationEvent( 0.5f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 1.0f, 0.8f, 0.7f } ),
				new LoliAnimationEvent( 0.5f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 12.0f, 0.5f } ),
			}
		),true);

        RegisterAnimation(Loli.Animation.FLOOR_SIT_PLACE_CARD_RIGHT,
			new Loli.AnimationInfo(idleTransition,
			"floor_sit_place_card_right","floor_sit_place_card_right","floor_sit_place_card_right",
			Loli.Priority.LOW,BodyState.FLOOR_SIT,BodyState.FLOOR_SIT,
			0.3f,new Loli.AnimLogicInfo(0,0.8f,0.2f,12,0.3f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.1f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.EFFORT, 0.0f } ),
				new LoliAnimationEvent( 0.4f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 1.0f, 0.8f, 0.5f } ),
				new LoliAnimationEvent( 0.4f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 12.0f, 0.5f } ),
			}
		),true);
		
		RegisterAnimation(Loli.Animation.FLOOR_SIT_PICK_CARD_RIGHT,
			new Loli.AnimationInfo(idleTransition,
			"floor_sit_pick_card_right","floor_sit_pick_card_right","floor_sit_pick_card_right",
			Loli.Priority.LOW,BodyState.FLOOR_SIT,BodyState.FLOOR_SIT,
			0.3f,new Loli.AnimLogicInfo(1,0.8f,0.2f,12,0.3f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.BLEND_CONTROLLER_RIGHT, new float[]{ 0.0f } ),
				new LoliAnimationEvent( 0.1f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.MISC_IDLE, 0.0f } ),
				new LoliAnimationEvent( 0.5f, (int)Loli.AnimationEventName.EXECUTE_PICK_CARD_RIGHT, null ),
			}
		),true);
		
		RegisterAnimation(Loli.Animation.STAND_IMPRESSED1,
			new Loli.AnimationInfo(idleTransition,
			"stand_impressed1","stand_impressed1","stand_impressed1",
			Loli.Priority.LOW,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(1,0.8f,0.2f,12,0.3f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.IMPRESSED_VERY, 1.0f } ),
				new LoliAnimationEvent( 0.2f, (int)Loli.AnimationEventName.PLAY_CLAP_SOUND, null ),
				new LoliAnimationEvent( 0.26f, (int)Loli.AnimationEventName.PLAY_CLAP_SOUND, null ),
				new LoliAnimationEvent( 0.34f, (int)Loli.AnimationEventName.PLAY_CLAP_SOUND, null ),
				new LoliAnimationEvent( 0.42f, (int)Loli.AnimationEventName.PLAY_CLAP_SOUND, null ),
				new LoliAnimationEvent( 0.50f, (int)Loli.AnimationEventName.PLAY_CLAP_SOUND, null ),
				new LoliAnimationEvent( 0.58f, (int)Loli.AnimationEventName.PLAY_CLAP_SOUND, null ),
				new LoliAnimationEvent( 0.66f, (int)Loli.AnimationEventName.PLAY_CLAP_SOUND, null ),
				new LoliAnimationEvent( 0.74f, (int)Loli.AnimationEventName.PLAY_CLAP_SOUND, null ),
			}
		));
		
		RegisterAnimation(Loli.Animation.FLOOR_SIT_IMPRESSED1,
			new Loli.AnimationInfo(idleTransition,
			"floor_sit_impressed1","floor_sit_impressed1","stand_impressed1",
			Loli.Priority.LOW,BodyState.FLOOR_SIT,BodyState.FLOOR_SIT,
			0.3f,new Loli.AnimLogicInfo(1,0.8f,0.2f,12,0.3f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.IMPRESSED_VERY, 1.0f } ),
				new LoliAnimationEvent( 0.2f, (int)Loli.AnimationEventName.PLAY_CLAP_SOUND, null ),
				new LoliAnimationEvent( 0.26f, (int)Loli.AnimationEventName.PLAY_CLAP_SOUND, null ),
				new LoliAnimationEvent( 0.34f, (int)Loli.AnimationEventName.PLAY_CLAP_SOUND, null ),
				new LoliAnimationEvent( 0.42f, (int)Loli.AnimationEventName.PLAY_CLAP_SOUND, null ),
				new LoliAnimationEvent( 0.50f, (int)Loli.AnimationEventName.PLAY_CLAP_SOUND, null ),
				new LoliAnimationEvent( 0.58f, (int)Loli.AnimationEventName.PLAY_CLAP_SOUND, null ),
				new LoliAnimationEvent( 0.66f, (int)Loli.AnimationEventName.PLAY_CLAP_SOUND, null ),
				new LoliAnimationEvent( 0.74f, (int)Loli.AnimationEventName.PLAY_CLAP_SOUND, null ),
			}
		));
		
		RegisterAnimation(Loli.Animation.PICKED_UP,
			new Loli.AnimationInfo(null,
			"picked_up_loop","picked_up_loop","picked_up_loop",
			Loli.Priority.LOW,BodyState.OFFBALANCE,BodyState.OFFBALANCE,
			0.3f,new Loli.AnimLogicInfo(1,1.0f,0.5f,12,0.4f),
			null
		));
		
		RegisterAnimation(Loli.Animation.FALLING_LOOP,
			new Loli.AnimationInfo(null,
			"falling_loop","falling_loop","falling_loop",
			Loli.Priority.LOW,BodyState.OFFBALANCE,BodyState.OFFBALANCE,
			0.3f,new Loli.AnimLogicInfo(0,0.6f,0.5f,12,0.4f),
			null
		));
		
		RegisterAnimation(Loli.Animation.FLOOR_CURL_LOOP,
			new Loli.AnimationInfo(null,
			"floor_curl_loop","floor_curl_loop","floor_curl_loop",
			Loli.Priority.LOW,BodyState.OFFBALANCE,BodyState.OFFBALANCE,
			0.35f,new Loli.AnimLogicInfo(1,0.6f,0.5f,0,0.4f),
			null
		));
		
		RegisterAnimation(Loli.Animation.FLOOR_FACE_DOWN_TO_STAND,
			new Loli.AnimationInfo(idleTransition,
			"floor_face_down_to_stand","floor_face_down_to_stand","floor_face_down_to_stand",
			Loli.Priority.LOW,BodyState.OFFBALANCE,BodyState.STAND,
			0.0f,new Loli.AnimLogicInfo(0,0.6f,0.5f,0,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.2f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.MISC_IDLE, 0.0f } ),
				new LoliAnimationEvent( 0.8f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 3.0f, 0.8f, 0.5f } ),
				new LoliAnimationEvent( 0.8f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 7.0f, 0.5f } ),
			}
		));
		
		RegisterAnimation(Loli.Animation.FLOOR_FACE_UP_TO_STAND,
			new Loli.AnimationInfo(idleTransition,
			"floor_face_up_to_stand","floor_face_up_to_stand","floor_face_up_to_stand",
			Loli.Priority.LOW,BodyState.OFFBALANCE,BodyState.STAND,
			0.0f,new Loli.AnimLogicInfo(0,0.6f,0.5f,0,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.2f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.MISC_IDLE, 0.0f } ),
				new LoliAnimationEvent( 0.8f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 3.0f, 0.8f, 0.5f } ),
				new LoliAnimationEvent( 0.8f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 7.0f, 0.5f } ),
			}
		));
		
		RegisterAnimation(Loli.Animation.FLOOR_FACE_UP_IDLE,
			new Loli.AnimationInfo(idleTransition,
			"floor_face_up_idle","floor_face_up_idle","floor_face_up_idle",
			Loli.Priority.LOW,BodyState.OFFBALANCE,BodyState.OFFBALANCE,
			0.4f,new Loli.AnimLogicInfo(0,0.6f,0.5f,12,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.2f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.MISC_IDLE, 0.0f } ),
			}
		));
		
		RegisterAnimation(Loli.Animation.FLOOR_FACE_SIDE_TO_STAND_RIGHT,
			new Loli.AnimationInfo(idleTransition,
			"floor_face_side_to_stand_right","floor_face_side_to_stand_right","floor_face_side_to_stand_right",
			Loli.Priority.LOW,BodyState.OFFBALANCE,BodyState.STAND,
			0.0f,new Loli.AnimLogicInfo(0,0.6f,0.5f,0,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.1f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.MISC_IDLE, 0.0f } ),
				new LoliAnimationEvent( 0.85f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 3.0f, 0.8f, 0.5f } ),
				new LoliAnimationEvent( 0.85f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 7.0f, 0.5f } ),
			}
		),true);

        RegisterAnimation(Loli.Animation.STAND_MERCHANT_IDLE1,
			new Loli.AnimationInfo(idleTransition,
			"stand_merchant_idle1","stand_merchant_idle1","stand_merchant_idle1",
			Loli.Priority.LOW,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(1,0.5f,0.3f,0,0.3f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.1f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.MISC_IDLE, 0.0f } ),
				new LoliAnimationEvent( 0.5f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 0.0f, 1.0f, 0.5f } ),
				new LoliAnimationEvent( 0.5f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 7.0f, 0.5f } ),
				new LoliAnimationEvent( 1.0f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 3.0f, 1.0f, 0.5f } ),
				new LoliAnimationEvent( 1.0f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 7.0f, 0.5f } ),
			}
		));
		RegisterAnimation(Loli.Animation.STAND_BOW,
			new Loli.AnimationInfo(idleTransition,
			"stand_bow","stand_bow","stand_bow",
			Loli.Priority.MEDIUM,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(0,0.3f,0.3f,0,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.1f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.MISC_IDLE, 0.0f } ),
				new LoliAnimationEvent( 0.7f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 3.0f, 1.0f, 0.4f } ),
				new LoliAnimationEvent( 0.6f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 12.0f, 0.4f } ),
			}
		));
		RegisterAnimation(Loli.Animation.STAND_FOLLOW_ME_RIGHT,
			new Loli.AnimationInfo(idleTransition,
			"stand_follow_me_right","stand_follow_me_right","stand_follow_me_right",
			Loli.Priority.MEDIUM,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(0,0.3f,0.3f,0,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.1f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.ECSTATIC, 0.0f } ),
			}
		));
		RegisterAnimation(Loli.Animation.STAND_SLIDINGDOOR_RIGHT,
			new Loli.AnimationInfo(new DefaultTransition( Loli.Animation.STAND_SLIDINGDOOR_IDLE_LOOP_RIGHT),
			"stand_slidingDoor_right","stand_slidingDoor_right","stand_happy_idle",
			Loli.Priority.MEDIUM,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(1,0.7f,0.3f,12,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.1f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.MISC_IDLE, 0.0f } ),
			}
		),true);
		
		RegisterAnimation(Loli.Animation.STAND_SLIDINGDOOR_IDLE_LOOP_RIGHT,
			new Loli.AnimationInfo(null,
			"stand_slidingDoor_idle_right_loop","stand_locomotion_spine_stable","stand_happy_idle",
			Loli.Priority.LOW,BodyState.STAND,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(1,0.0f,0.3f,7,0.4f)
		),true);

		RegisterAnimation(Loli.Animation.SQUAT_LOCOMOTION_HAPPY,
			new Loli.AnimationInfo(null,
			"squat_locomotion_happy","squat_locomotion_happy","stand_happy_idle",
			Loli.Priority.LOW,BodyState.SQUAT,BodyState.SQUAT,
			0.3f,new Loli.AnimLogicInfo(1,1.0f,0.3f,7,0.4f),
			null,
			(int)Loli.AnimationInfo.Flag.IDLE_STATE
		));
		
		RegisterAnimation(Loli.Animation.SQUAT_LOCOMOTION_ANGRY,
			new Loli.AnimationInfo(null,
			"squat_locomotion_happy","squat_locomotion_happy","stand_angry_idle",
			Loli.Priority.LOW,BodyState.SQUAT,BodyState.SQUAT,
			0.3f,new Loli.AnimLogicInfo(1,1.0f,0.3f,7,0.4f),
			null,
			(int)Loli.AnimationInfo.Flag.IDLE_STATE
		));

		RegisterLegSpeedInfo( "squat_locomotion_happy", 0.75f, 0.3f, 100.0f, 20.0f );

		RegisterAnimation(Loli.Animation.STAND_TO_SQUAT,
			new Loli.AnimationInfo(idleTransition,
			"stand_to_squat","stand_to_squat","stand_happy_idle",
			Loli.Priority.LOW,BodyState.STAND,BodyState.SQUAT,
			0.3f,new Loli.AnimLogicInfo(1,0.3f,0.3f,0,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.1f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.MISC_IDLE, 0.0f } ),
			}
		));

		RegisterAnimation(Loli.Animation.SQUAT_TO_RELAX,
			new Loli.AnimationInfo(idleTransition,
			"squat_to_relax","squat_to_relax","squat_to_relax",
			Loli.Priority.LOW,BodyState.SQUAT,BodyState.RELAX,
			0.3f,new Loli.AnimLogicInfo(1,0.3f,0.3f,0,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.3f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.RELIEF, 0.0f } ),
			}
		));
		
		RegisterAnimation(Loli.Animation.RELAX_IDLE_LOOP,
			new Loli.AnimationInfo(null,
			"relax_idle_loop","relax_idle_loop","relax_idle_loop",
			Loli.Priority.LOW,BodyState.RELAX,BodyState.RELAX,
			0.3f,new Loli.AnimLogicInfo(1,0.3f,0.3f,12,0.4f),
			null,
			(int)Loli.AnimationInfo.Flag.IDLE_STATE
		));
		
		RegisterAnimation(Loli.Animation.RELAX_TO_SQUAT,
			new Loli.AnimationInfo(idleTransition,
			"relax_to_squat","relax_to_squat","relax_to_squat",
			Loli.Priority.LOW,BodyState.RELAX,BodyState.SQUAT,
			0.3f,new Loli.AnimLogicInfo(0,0.3f,0.3f,12,0.4f)
		));
		
		RegisterAnimation(Loli.Animation.RELAX_TO_SQUAT_STARTLE,
			new Loli.AnimationInfo(idleTransition,
			"relax_to_squat_startle","relax_to_squat_startle","relax_to_squat_startle",
			Loli.Priority.MEDIUM,BodyState.RELAX,BodyState.SQUAT,
			0.3f,new Loli.AnimLogicInfo(1,0.8f,0.3f,0,0.3f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.1f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.SCARED_SHORT, 0.0f } ),
			}
		));
		
		RegisterAnimation(Loli.Animation.SQUAT_TO_STAND,
			new Loli.AnimationInfo(idleTransition,
			"squat_to_stand","squat_to_stand","stand_happy_idle",
			Loli.Priority.LOW,BodyState.SQUAT,BodyState.STAND,
			0.3f,new Loli.AnimLogicInfo(1,0.3f,0.3f,0,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.1f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.MISC_IDLE, 0.0f } ),
			}
		));

		RegisterAnimation(Loli.Animation.SQUAT_HEADPAT_HAPPY_START,
			new Loli.AnimationInfo(new Loli.DefaultTransition(Loli.Animation.SQUAT_HEADPAT_HAPPY_LOOP),
			"squat_headpat_happy_start","squat_headpat_happy_start","squat_headpat_happy_start",
			Loli.Priority.MEDIUM,BodyState.SQUAT,BodyState.SQUAT,
			0.4f,new Loli.AnimLogicInfo(2,1.0f,0.5f,12,0.4f),
			new LoliAnimationEvent[]{			
				new LoliAnimationEvent( 0.05f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.DISAPPOINTED_STARTLE, 0.0f } )
			}
		));

		RegisterAnimation(Loli.Animation.SQUAT_HEADPAT_HAPPY_WANTED_MORE,
			new Loli.AnimationInfo(idleTransition,
			"squat_headpat_happy_wanted_more","squat_headpat_happy_wanted_more","squat_headpat_happy_wanted_more",
			Loli.Priority.MEDIUM,BodyState.SQUAT,BodyState.SQUAT,
			0.3f,new Loli.AnimLogicInfo(3,1.0f,0.5f,6,0.4f),
			new LoliAnimationEvent[]{			
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.DISAPPOINTED_STARTLE, 0.0f } ),
				new LoliAnimationEvent( 0.3f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.DISAPPOINTED_SIGH, 0.0f } ),
				new LoliAnimationEvent( 0.3f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 0.0f, 1.0f, 0.8f } ),
				new LoliAnimationEvent( 0.3f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 0.0f, 0.3f } ),
				new LoliAnimationEvent( 0.8f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 3.0f, 1.0f, 0.8f } ),
				new LoliAnimationEvent( 0.9f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 12.0f, 0.3f } ),
			}
		));

		RegisterAnimation(Loli.Animation.SQUAT_HEADPAT_HAPPY_IDLE_SAD_RIGHT,
			new Loli.AnimationInfo(new Loli.DefaultTransition(Loli.Animation.SQUAT_HEADPAT_HAPPY_LOOP),
			"squat_headpat_happy_idle_sad_right","squat_headpat_happy_idle_sad_right","squat_headpat_happy_idle_sad_right",
			Loli.Priority.MEDIUM,BodyState.SQUAT,BodyState.SQUAT,
			0.3f,new Loli.AnimLogicInfo(1,0.4f,0.5f,12,0.5f),
			new LoliAnimationEvent[]{			
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.WHINE_SOFT, 0.0f } ),
				new LoliAnimationEvent( 0.34f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 0.0f, 1.0f, 0.6f } ),
				new LoliAnimationEvent( 0.34f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 0.0f, 0.3f } ),
				new LoliAnimationEvent( 0.85f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 12.0f, 0.3f } ),
			}
		),true);

		// RegisterAnimation(Loli.Animation.SQUAT_HEADPAT_HAPPY_SATISFACTION,
		// 	new Loli.AnimationInfo(idleTransition,
		// 	"squat_headpat_satisfaction","squat_headpat_satisfaction","squat_headpat_satisfaction",
		// 	Loli.Priority.MEDIUM,BodyState.SQUAT,BodyState.SQUAT,
		// 	0.3f,new Loli.AnimLogicInfo(0,1.0f,0.5f,0,0.4f),
		// 	new LoliAnimationEvent[]{
		// 		new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.ECSTATIC, 0.0f } ),
		// 	}
		// ));

		RegisterAnimation(Loli.Animation.SQUAT_HEADPAT_HAPPY_LOOP,
			new Loli.AnimationInfo(null,
			"squat_headpat_happy_loop","squat_headpat_happy_loop","squat_headpat_happy_loop",
			Loli.Priority.LOW,BodyState.SQUAT,BodyState.SQUAT,
			0.5f,new Loli.AnimLogicInfo(0,0.5f,0.5f,12,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.05f, (int)Loli.AnimationEventName.HEADPAT_PROPER_SOUND, null ),
				new LoliAnimationEvent( 0.45f, (int)Loli.AnimationEventName.HEADPAT_PROPER_SOUND, null )
			}
		));

		RegisterAnimation(Loli.Animation.SQUAT_HEADPAT_ANGRY_BRUSH_AWAY,
			new Loli.AnimationInfo(idleTransition,
			"squat_headpat_angry_brush_away","squat_headpat_angry_brush_away","stand_headpat_angry_brush_away",
			Loli.Priority.HIGH,BodyState.SQUAT,BodyState.SQUAT,
			0.2f,new Loli.AnimLogicInfo(0,1.0f,0.5f,0,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.1f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.ANGRY_LONG, 1.0f } )
			}
		));

		RegisterAnimation(Loli.Animation.SQUAT_HEADPAT_ANGRY_START,
			new Loli.AnimationInfo(null,
			"squat_headpat_angry_start","squat_headpat_angry_start","squat_headpat_angry_start",
			Loli.Priority.MEDIUM,BodyState.SQUAT,BodyState.SQUAT,
			0.2f,new Loli.AnimLogicInfo(0,0.5f,0.5f,7,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.1f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.ANGRY_POUT, 0.0f } )
			}
		));
		
		RegisterAnimation(Loli.Animation.SQUAT_HEADPAT_ANGRY_LOOP,
			new Loli.AnimationInfo(null,
			"squat_headpat_angry_loop","squat_headpat_angry_loop","squat_headpat_angry_loop",
			Loli.Priority.MEDIUM,BodyState.SQUAT,BodyState.SQUAT,
			0.2f,new Loli.AnimLogicInfo(0,0.5f,0.5f,7,0.4f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.1f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.ANGRY_POUT, 0.0f } )
			}
		));

		RegisterAnimation(Loli.Animation.SQUAT_HEADPAT_CLIMAX_TO_CANCEL_RIGHT,
			new Loli.AnimationInfo(idleTransition,
			"squat_headpat_climax_to_cancel_right","squat_headpat_climax_to_cancel_right","stand_headpat_climax_to_cancel_right",
			Loli.Priority.HIGH,BodyState.SQUAT,BodyState.SQUAT,
			0.15f,new Loli.AnimLogicInfo(0,1.0f,0.85f,0,0.4f),
			new LoliAnimationEvent[]{			
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.ANGRY_LONG, 0.0f } ),
				new LoliAnimationEvent( 0.4f, (int)Loli.AnimationEventName.SET_EYE_FLAGS, new float[]{ 12.0f, 1.0f } )
			}
		),true);

		RegisterAnimation(Loli.Animation.SQUAT_HEADPAT_CLIMAX_TO_HAPPY,
			new Loli.AnimationInfo(new Loli.DefaultTransition(Loli.Animation.SQUAT_HEADPAT_HAPPY_LOOP),
			"squat_headpat_climax_to_happy","squat_headpat_climax_to_happy","stand_headpat_climax_to_happy",
			Loli.Priority.HIGH,BodyState.SQUAT,BodyState.SQUAT,
			0.3f,null,
			new LoliAnimationEvent[]{			
				new LoliAnimationEvent( 0.1f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.ANGRY_HEADPAT_B, 0.0f } ),
			}
		));
		
		RegisterAnimation(Loli.Animation.SQUAT_FACE_POKE_1_RIGHT,
			new Loli.AnimationInfo(postFacePokeAnimTransition,
			"squat_poke_face_1_right","squat_poke_face_1_right","stand_poke_face_1_right",
			Loli.Priority.HIGH,BodyState.SQUAT,BodyState.SQUAT,
			0.15f,new Loli.AnimLogicInfo(1,0.4f,0.2f,12,0.4f),
			new LoliAnimationEvent[]{			
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.STARTLE_SHORT, 0.0f } ),
				new LoliAnimationEvent( 0.4f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 1.0f, 1.0f, 0.5f } ),
			}
		),true);
		RegisterAnimation(Loli.Animation.SQUAT_FACE_POKE_2_RIGHT,
			new Loli.AnimationInfo(postFacePokeAnimTransition,
			"squat_poke_face_2_right","squat_poke_face_2_right","stand_poke_face_2_right",
			Loli.Priority.HIGH,BodyState.SQUAT,BodyState.SQUAT,
			0.15f,new Loli.AnimLogicInfo(1,0.4f,0.2f,12,0.4f),
			new LoliAnimationEvent[]{			
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.STARTLE_SHORT, 0.0f } ),
				new LoliAnimationEvent( 0.4f, (int)Loli.AnimationEventName.SET_BODY_FLAGS, new float[]{ 1.0f, 1.0f, 0.5f } ),
			}
		),true);

		RegisterAnimation(Loli.Animation.SQUAT_IMPRESSED1,
			new Loli.AnimationInfo(idleTransition,
			"squat_impressed1","squat_impressed1","stand_impressed1",
			Loli.Priority.LOW,BodyState.SQUAT,BodyState.SQUAT,
			0.3f,new Loli.AnimLogicInfo(1,0.8f,0.2f,12,0.3f),
			new LoliAnimationEvent[]{
				new LoliAnimationEvent( 0.0f, (int)Loli.AnimationEventName.SPEAK, new float[]{ (float)Loli.VoiceLine.IMPRESSED_VERY, 1.0f } ),
				new LoliAnimationEvent( 0.2f, (int)Loli.AnimationEventName.PLAY_CLAP_SOUND, null ),
				new LoliAnimationEvent( 0.26f, (int)Loli.AnimationEventName.PLAY_CLAP_SOUND, null ),
				new LoliAnimationEvent( 0.34f, (int)Loli.AnimationEventName.PLAY_CLAP_SOUND, null ),
				new LoliAnimationEvent( 0.42f, (int)Loli.AnimationEventName.PLAY_CLAP_SOUND, null ),
				new LoliAnimationEvent( 0.50f, (int)Loli.AnimationEventName.PLAY_CLAP_SOUND, null ),
				new LoliAnimationEvent( 0.58f, (int)Loli.AnimationEventName.PLAY_CLAP_SOUND, null ),
				new LoliAnimationEvent( 0.66f, (int)Loli.AnimationEventName.PLAY_CLAP_SOUND, null ),
				new LoliAnimationEvent( 0.74f, (int)Loli.AnimationEventName.PLAY_CLAP_SOUND, null ),
			}
		));
	}
}

}