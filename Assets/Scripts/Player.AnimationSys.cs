using System.Collections.Generic;
using UnityEngine;


namespace viva
{

    using PlayerAnimationEvent = AnimationEvent<float[]>;


    public partial class Player : Character
    {

        public enum Priority
        {
            LOW,
            MED,
            HIGH,
        }

        public class PlayerAnimationInfo
        {


            public delegate void OnFinishedAnimationCallback(bool rightHand);

            public readonly int id;
            public readonly float transitionTime;
            public readonly Priority priority;
            public readonly Animation nextAnim;
            public readonly float minWait;
            public OnFinishedAnimationCallback onFinishAnimation = null;
            public readonly PlayerAnimationEvent[] animationEvents;

            public PlayerAnimationInfo(int _id, float _transitionTime, Priority _priority, Animation _nextAnim, float _minWait, PlayerAnimationEvent[] _animationEvents = null)
            {
                id = _id;
                transitionTime = _transitionTime;
                priority = _priority;
                nextAnim = _nextAnim;
                minWait = _minWait;
                animationEvents = _animationEvents;
            }
        }

        // private HandAnimationSystem rightHandAnimSys;
        // private HandAnimationSystem leftHandAnimSys;
        private Tools.EaseBlend vrAnimatorBlend = new Tools.EaseBlend();
        private Vector3 vrAnimationPosOffset = Vector3.zero;
        private Tools.EaseBlend vrAnimatorBlendAllowHands = new Tools.EaseBlend();
        private PlayerAnimationEvent.Context animationEventContext;
        private Dictionary<Animation, PlayerAnimationInfo> m_animationInfos = new Dictionary<Animation, PlayerAnimationInfo>();
        private Dictionary<Animation, PlayerAnimationInfo> animationInfos { get { return m_animationInfos; } }

        public bool AttemptBeginVRAnimatorBlend(Animation newAnim, Vector3 positionOffset)
        {

            //not allowed to blend while already active
            if (vrAnimatorBlend.value != 0.0f)
            {
                return false;
            }
            if (rightPlayerHandState.animSys.currentAnim == newAnim)
            {
                return false;
            }
            vrAnimatorBlend.StartBlend(1.0f, 0.3f);
            rightPlayerHandState.animSys.SetTargetAnimation(newAnim);
            leftPlayerHandState.animSys.SetTargetAnimation(newAnim);
            vrAnimationPosOffset = positionOffset;

            return true;
        }

        public void EndVRAnimatorBlend()
        {

            if (vrAnimatorBlend.getTarget() != 0.0f)
            {
                vrAnimatorBlend.StartBlend(0.0f, 0.3f);
                vrAnimatorBlendAllowHands.StartBlend(0.0f, 0.3f);
                rightPlayerHandState.animSys.SetTargetAnimation(rightPlayerHandState.animSys.idleAnimation);
                leftPlayerHandState.animSys.SetTargetAnimation(leftPlayerHandState.animSys.idleAnimation);
            }
        }

        private void FixedUpdateKeyboardHandAnimationEvents()
        {
            rightPlayerHandState.animSys.FixedUpdateAnimationEvents(animationEventContext, animator);
            leftPlayerHandState.animSys.FixedUpdateAnimationEvents(animationEventContext, animator);
        }

        private void FixedUpdateHandAnimationSystems()
        {
            rightPlayerHandState.animSys.FixedUpdateAnimationSystem(animationEventContext, animator);
            leftPlayerHandState.animSys.FixedUpdateAnimationSystem(animationEventContext, animator);
        }

        private void UpdateVRAnimationSystems()
        {
            vrAnimatorBlend.Update(Time.deltaTime);
            vrAnimatorBlendAllowHands.Update(Time.deltaTime);
        }

        public Animator GetAnimator()
        {
            return animator;
        }

        public void AllowVRAnimatorBlendHandPositions()
        {
            vrAnimatorBlendAllowHands.StartBlend(1.0f, 0.3f);
        }

        private void InitAnimations()
        {

            float duration = 0.1f;
            m_animationInfos[Animation.IDLE] = new PlayerAnimationInfo(Animator.StringToHash("player_idle_loop"), 0.15f, Priority.LOW, Animation.IDLE, 0.0f);
            m_animationInfos[Animation.GESTURE_COME] = new PlayerAnimationInfo(Animator.StringToHash("player_gesture_come"), duration, Priority.LOW, Animation.IDLE, 1.0f,
                new PlayerAnimationEvent[]{
                new PlayerAnimationEvent( 0.5f ,(int)AnimationEventName.FIRE_GESTURE, new float[]{ 1, (int)ObjectFingerPointer.Gesture.FOLLOW } )
                }
            );
            m_animationInfos[Animation.GESTURE_WAVE] = new PlayerAnimationInfo(Animator.StringToHash("player_gesture_wave"), duration, Priority.LOW, Animation.IDLE, 1.0f,
                new PlayerAnimationEvent[]{
                new PlayerAnimationEvent( 0.5f ,(int)AnimationEventName.FIRE_GESTURE, new float[]{ 1, (int)ObjectFingerPointer.Gesture.HELLO } )
                }
            );
            m_animationInfos[Animation.GESTURE_PRESENT_RIGHT] = new PlayerAnimationInfo(Animator.StringToHash("player_gesture_present"), duration, Priority.LOW, Animation.GESTURE_PRESENT_RIGHT, 0.0f,
                new PlayerAnimationEvent[]{
                new PlayerAnimationEvent( 1.0f ,(int)AnimationEventName.FIRE_GESTURE, new float[]{ 1, (int)ObjectFingerPointer.Gesture.PRESENT_START } )
                }
            );
            m_animationInfos[Animation.GESTURE_PRESENT_LEFT] = new PlayerAnimationInfo(Animator.StringToHash("player_gesture_present"), duration, Priority.LOW, Animation.GESTURE_PRESENT_LEFT, 0.0f,
                new PlayerAnimationEvent[]{
                new PlayerAnimationEvent( 1.0f ,(int)AnimationEventName.FIRE_GESTURE, new float[]{ 0, (int)ObjectFingerPointer.Gesture.PRESENT_START } )
                }
            );
            m_animationInfos[Animation.CAMERA] = new PlayerAnimationInfo(Animator.StringToHash("player_camera"), duration, Priority.LOW, Animation.CAMERA, 0.0f);
            m_animationInfos[Animation.POLAROID] = new PlayerAnimationInfo(Animator.StringToHash("player_polaroid"), duration, Priority.LOW, Animation.POLAROID, 0.0f);
            m_animationInfos[Animation.POLAROID_RIP_IN_RIGHT] = new PlayerAnimationInfo(Animator.StringToHash("player_polaroid_rip_in_right"), duration, Priority.LOW, Animation.POLAROID_RIP_IN_RIGHT, 0.1f);
            m_animationInfos[Animation.POLAROID_RIP_IN_LEFT] = new PlayerAnimationInfo(Animator.StringToHash("player_polaroid_rip_in_left"), duration, Priority.LOW, Animation.POLAROID_RIP_IN_LEFT, 0.1f);
            m_animationInfos[Animation.POLAROID_RIP] = new PlayerAnimationInfo(Animator.StringToHash("player_polaroid_rip"), 0.1f, Priority.LOW, Animation.IDLE, 0.0f);
            m_animationInfos[Animation.POKE] = new PlayerAnimationInfo(Animator.StringToHash("player_poke"), 0.1f, Priority.LOW, Animation.IDLE, 0.0f);
            m_animationInfos[Animation.HEADPAT] = new PlayerAnimationInfo(Animator.StringToHash("player_headpat"), duration, Priority.LOW, Animation.IDLE, 0.0f);
            m_animationInfos[Animation.HEADPAT_SCRUB] = new PlayerAnimationInfo(Animator.StringToHash("player_headpat_scrub"), duration, Priority.LOW, Animation.IDLE, 0.0f);
            m_animationInfos[Animation.VALVE] = new PlayerAnimationInfo(Animator.StringToHash("player_HOLD_FORM_VALVE"), 0.2f, Priority.LOW, Animation.VALVE, 0.0f);
            m_animationInfos[Animation.VALVE_REPOSITION] = new PlayerAnimationInfo(Animator.StringToHash("player_valve_reposition"), 0.05f, Priority.LOW, Animation.VALVE, 1.0f);
            m_animationInfos[Animation.POINT] = new PlayerAnimationInfo(Animator.StringToHash("player_point"), 0.1f, Priority.LOW, Animation.POINT, 0.0f);
            m_animationInfos[Animation.DOORKNOB] = new PlayerAnimationInfo(Animator.StringToHash("player_HOLD_DOORKNOB"), 0.1f, Priority.LOW, Animation.DOORKNOB, 0.0f);
            m_animationInfos[Animation.DOORKNOB_2_IDLE] = new PlayerAnimationInfo(Animator.StringToHash("player_doorknob_2_idle"), 0.1f, Priority.LOW, Animation.DOORKNOB_2_IDLE, 0.0f);
            m_animationInfos[Animation.SOAP_GENERATE_BUBBLES_RIGHT] = new PlayerAnimationInfo(Animator.StringToHash("player_soap_generate_bubbles_right"), duration, Priority.LOW, Animation.IDLE, 1.0f);
            m_animationInfos[Animation.SOAP_GENERATE_BUBBLES_LEFT] = new PlayerAnimationInfo(Animator.StringToHash("player_soap_generate_bubbles_left"), duration, Priority.LOW, Animation.IDLE, 1.0f);
            m_animationInfos[Animation.VR_SOAP_GENERATE_BUBBLES] = new PlayerAnimationInfo(Animator.StringToHash("player_vr_soap_generate_bubbles"), duration, Priority.LOW, Animation.VR_SOAP_GENERATE_BUBBLES, 1.0f);
            m_animationInfos[Animation.LANTERN] = new PlayerAnimationInfo(Animator.StringToHash("player_lantern"), duration, Priority.LOW, Animation.LANTERN, 1.0f);
            m_animationInfos[Animation.WALL_CANDLE] = new PlayerAnimationInfo(Animator.StringToHash("player_wall_candle"), duration, Priority.LOW, Animation.WALL_CANDLE, 1.0f);
            m_animationInfos[Animation.KEYBOARD_HANDS_DOWN] = new PlayerAnimationInfo(Animator.StringToHash("player_keyboard_hands_down"), 0.3f, Priority.LOW, Animation.KEYBOARD_HANDS_DOWN, 1.0f);
            m_animationInfos[Animation.CHICKEN] = new PlayerAnimationInfo(Animator.StringToHash("player_HOLD_FORM_CHICKEN"), duration, Priority.LOW, Animation.CHICKEN, 1.0f);
            m_animationInfos[Animation.BAG] = new PlayerAnimationInfo(Animator.StringToHash("player_HOLD_FORM_BAG"), duration, Priority.LOW, Animation.BAG, 1.0f);
            m_animationInfos[Animation.MORTAR] = new PlayerAnimationInfo(Animator.StringToHash("player_HOLD_FORM_MORTAR"), duration, Priority.LOW, Animation.MORTAR, 1.0f);
            m_animationInfos[Animation.MORTAR_AND_PESTLE_IN_RIGHT] = new PlayerAnimationInfo(Animator.StringToHash("player_mortar_and_pestle_in_right"), duration, Priority.LOW, Animation.MORTAR_AND_PESTLE_GRIND_RIGHT_LOOP, 1.0f);
            m_animationInfos[Animation.MORTAR_AND_PESTLE_IN_LEFT] = new PlayerAnimationInfo(Animator.StringToHash("player_mortar_and_pestle_in_left"), duration, Priority.LOW, Animation.MORTAR_AND_PESTLE_GRIND_LEFT_LOOP, 1.0f);
            m_animationInfos[Animation.MORTAR_AND_PESTLE_GRIND_RIGHT_LOOP] = new PlayerAnimationInfo(Animator.StringToHash("player_mortar_and_pestle_grind_right_loop"), duration, Priority.LOW, Animation.MORTAR_AND_PESTLE_GRIND_RIGHT_LOOP, 1.0f);
            m_animationInfos[Animation.MORTAR_AND_PESTLE_GRIND_LEFT_LOOP] = new PlayerAnimationInfo(Animator.StringToHash("player_mortar_and_pestle_grind_left_loop"), duration, Priority.LOW, Animation.MORTAR_AND_PESTLE_GRIND_LEFT_LOOP, 1.0f);
            m_animationInfos[Animation.MORTAR_EMPTY_CONTENTS] = new PlayerAnimationInfo(Animator.StringToHash("player_mortar_empty_contents"), duration, Priority.LOW, Animation.IDLE, 1.0f);
            m_animationInfos[Animation.EGG_CRACK] = new PlayerAnimationInfo(Animator.StringToHash("player_crack_egg"), 0.1f, Priority.LOW, Animation.IDLE, 1.0f);
            m_animationInfos[Animation.MIXING_BOWL] = new PlayerAnimationInfo(Animator.StringToHash("player_HOLD_FORM_MIXING_BOWL"), duration, Priority.LOW, Animation.MIXING_BOWL, 1.0f);
            m_animationInfos[Animation.JAR] = new PlayerAnimationInfo(Animator.StringToHash("player_HOLD_FORM_JAR"), duration, Priority.LOW, Animation.JAR, 1.0f);
            m_animationInfos[Animation.POT] = new PlayerAnimationInfo(Animator.StringToHash("player_HOLD_FORM_POT"), duration, Priority.LOW, Animation.POT, 1.0f);
            m_animationInfos[Animation.MIXING_SPOON] = new PlayerAnimationInfo(Animator.StringToHash("player_HOLD_FORM_MIXING_SPOON"), duration, Priority.LOW, Animation.MIXING_SPOON, 1.0f);
            m_animationInfos[Animation.MIXING_BOWL_AND_SPOON_IN_RIGHT] = new PlayerAnimationInfo(Animator.StringToHash("player_mix_batter_in_right"), duration, Priority.LOW, Animation.MIXING_BOWL_AND_SPOON_RIGHT_LOOP, 1.0f);
            m_animationInfos[Animation.MIXING_BOWL_AND_SPOON_IN_LEFT] = new PlayerAnimationInfo(Animator.StringToHash("player_mix_batter_in_left"), duration, Priority.LOW, Animation.MIXING_BOWL_AND_SPOON_LEFT_LOOP, 1.0f);
            m_animationInfos[Animation.MIXING_BOWL_AND_SPOON_RIGHT_LOOP] = new PlayerAnimationInfo(Animator.StringToHash("player_mix_batter_right_loop"), duration, Priority.LOW, Animation.MIXING_BOWL_AND_SPOON_RIGHT_LOOP, 1.0f);
            m_animationInfos[Animation.MIXING_BOWL_AND_SPOON_LEFT_LOOP] = new PlayerAnimationInfo(Animator.StringToHash("player_mix_batter_left_loop"), duration, Priority.LOW, Animation.MIXING_BOWL_AND_SPOON_LEFT_LOOP, 1.0f);
            m_animationInfos[Animation.MIXING_BOWL_EMPTY_CONTENTS] = new PlayerAnimationInfo(Animator.StringToHash("player_mixing_bowl_empty_contents"), duration, Priority.LOW, Animation.IDLE, 1.0f);
            m_animationInfos[Animation.JAR_EMPTY_CONTENTS] = new PlayerAnimationInfo(Animator.StringToHash("player_jar_empty_contents"), duration, Priority.LOW, Animation.IDLE, 1.0f);
            m_animationInfos[Animation.KNIFE] = new PlayerAnimationInfo(Animator.StringToHash("player_HOLD_FORM_KNIFE"), duration, Priority.LOW, Animation.KNIFE, 1.0f);
            m_animationInfos[Animation.MIXING_SPOON_SCOOP] = new PlayerAnimationInfo(Animator.StringToHash("player_mixing_spoon_scoop"), duration, Priority.LOW, Animation.IDLE, 1.0f);
            m_animationInfos[Animation.MORTAR_EMPTY_INTO_MIXING_BOWL_RIGHT] = new PlayerAnimationInfo(Animator.StringToHash("player_empty_mortar_into_mixing_bowl_right"), duration, Priority.LOW, Animation.IDLE, 1.0f);
            m_animationInfos[Animation.MORTAR_EMPTY_INTO_MIXING_BOWL_LEFT] = new PlayerAnimationInfo(Animator.StringToHash("player_empty_mortar_into_mixing_bowl_left"), duration, Priority.LOW, Animation.IDLE, 1.0f);
            m_animationInfos[Animation.POKER_CARD] = new PlayerAnimationInfo(Animator.StringToHash("player_HOLD_FORM_POKER_CARD"), duration, Priority.LOW, Animation.IDLE, 1.0f);
            m_animationInfos[Animation.SELECT_CARD_RIGHT] = new PlayerAnimationInfo(Animator.StringToHash("player_keyboard_select_card_right"), duration, Priority.LOW, Animation.SELECT_CARD_RIGHT, 1.0f);
            m_animationInfos[Animation.SELECT_CARD_LEFT] = new PlayerAnimationInfo(Animator.StringToHash("player_keyboard_select_card_left"), duration, Priority.LOW, Animation.SELECT_CARD_LEFT, 1.0f);
            m_animationInfos[Animation.CARD_SPRING_FLOURISH_RIGHT] = new PlayerAnimationInfo(Animator.StringToHash("player_spring_flourish_right"), duration, Priority.LOW, Animation.IDLE, 1.0f);
            m_animationInfos[Animation.CARD_SPRING_FLOURISH_LEFT] = new PlayerAnimationInfo(Animator.StringToHash("player_spring_flourish_left"), duration, Priority.LOW, Animation.IDLE, 1.0f);
            m_animationInfos[Animation.PESTLE] = new PlayerAnimationInfo(Animator.StringToHash("player_HOLD_FORM_PESTLE"), duration, Priority.LOW, Animation.PESTLE, 1.0f);
            m_animationInfos[Animation.PASTRY] = new PlayerAnimationInfo(Animator.StringToHash("player_HOLD_FORM_PASTRY"), duration, Priority.LOW, Animation.PASTRY, 1.0f);
            m_animationInfos[Animation.PEACH] = new PlayerAnimationInfo(Animator.StringToHash("player_HOLD_FORM_PEACH"), duration, Priority.LOW, Animation.PEACH, 1.0f);
            m_animationInfos[Animation.STRAWBERRY] = new PlayerAnimationInfo(Animator.StringToHash("player_HOLD_FORM_STRAWBERRY"), duration, Priority.LOW, Animation.STRAWBERRY, 1.0f);
            m_animationInfos[Animation.CAMERA_VR_ONLY] = new PlayerAnimationInfo(Animator.StringToHash("player_HOLD_FORM_CAMERA_VR_ONLY"), duration, Priority.LOW, Animation.CAMERA_VR_ONLY, 1.0f);
            m_animationInfos[Animation.CANTALOUPE] = new PlayerAnimationInfo(Animator.StringToHash("player_HOLD_FORM_CANTALOUPE"), duration, Priority.LOW, Animation.CANTALOUPE, 1.0f);
            m_animationInfos[Animation.CATTAIL] = new PlayerAnimationInfo(Animator.StringToHash("player_HOLD_FORM_CATTAIL"), duration, Priority.LOW, Animation.CATTAIL, 1.0f);
            m_animationInfos[Animation.EGG] = new PlayerAnimationInfo(Animator.StringToHash("player_HOLD_FORM_EGG"), duration, Priority.LOW, Animation.EGG, 1.0f);
            m_animationInfos[Animation.HAIR] = new PlayerAnimationInfo(Animator.StringToHash("player_HOLD_FORM_HAIR"), duration, Priority.LOW, Animation.HAIR, 1.0f);
            m_animationInfos[Animation.CONTAINER_LID] = new PlayerAnimationInfo(Animator.StringToHash("player_HOLD_FORM_jar_lid"), duration, Priority.LOW, Animation.CONTAINER_LID, 1.0f);
            m_animationInfos[Animation.REINS] = new PlayerAnimationInfo(Animator.StringToHash("player_HOLD_FORM_REINS"), duration, Priority.LOW, Animation.REINS, 1.0f);
            m_animationInfos[Animation.SOAP] = new PlayerAnimationInfo(Animator.StringToHash("player_HOLD_FORM_SOAP"), duration, Priority.LOW, Animation.SOAP, 1.0f);
            m_animationInfos[Animation.TOWEL] = new PlayerAnimationInfo(Animator.StringToHash("player_HOLD_FORM_TOWEL"), duration, Priority.LOW, Animation.TOWEL, 1.0f);
            m_animationInfos[Animation.WHEAT_SPIKE] = new PlayerAnimationInfo(Animator.StringToHash("player_HOLD_FORM_WHEAT_SPIKE"), duration, Priority.LOW, Animation.WHEAT_SPIKE, 1.0f);
            m_animationInfos[Animation.HAND] = new PlayerAnimationInfo(Animator.StringToHash("player_HOLD_HAND"), duration, Priority.LOW, Animation.HAND, 1.0f);
            m_animationInfos[Animation.HAND_ALT] = new PlayerAnimationInfo(Animator.StringToHash("player_HOLD_HAND_ALT"), duration, Priority.LOW, Animation.HAND_ALT, 1.0f);
            m_animationInfos[Animation.GENERIC] = new PlayerAnimationInfo(Animator.StringToHash("player_HOLD_GENERIC"), duration, Priority.LOW, Animation.GENERIC, 1.0f);
            m_animationInfos[Animation.FIREWORK] = new PlayerAnimationInfo(Animator.StringToHash("player_HOLD_FORM_FIREWORK"), duration, Priority.LOW, Animation.FIREWORK, 1.0f);
            m_animationInfos[Animation.RUBBER_DUCKY] = new PlayerAnimationInfo(Animator.StringToHash("player_HOLD_DUCK"), duration, Priority.LOW, Animation.RUBBER_DUCKY, 1.0f);
            m_animationInfos[Animation.FLASHLIGHT] = new PlayerAnimationInfo(Animator.StringToHash("player_HOLD_FORM_FLASHLIGHT"), duration, Priority.LOW, Animation.FLASHLIGHT, 1.0f);

            m_animationInfos[Animation.ADD_CARD_TO_RIGHT] = new PlayerAnimationInfo(Animator.StringToHash("player_poker_add_card_right"), duration, Priority.LOW, Animation.IDLE, 1.0f,
            new PlayerAnimationEvent[]{
            new PlayerAnimationEvent( 0.5f, (int)AnimationEventName.ADD_CARD_TO_RIGHT_HAND, null )
            });
            m_animationInfos[Animation.ADD_CARD_TO_LEFT] = new PlayerAnimationInfo(Animator.StringToHash("player_poker_add_card_left"), duration, Priority.LOW, Animation.IDLE, 1.0f,
            new PlayerAnimationEvent[]{
            new PlayerAnimationEvent( 0.5f, (int)AnimationEventName.ADD_CARD_TO_LEFT_HAND, null )
            });

            m_animationInfos[Animation.BAG_PLACE_INSIDE_RIGHT] = new PlayerAnimationInfo(
                Animator.StringToHash("player_bag_place_inside_right"), duration, Priority.LOW, Animation.IDLE, 1.0f,
                new PlayerAnimationEvent[]{
                new PlayerAnimationEvent( 0.0f, (int)AnimationEventName.OPEN_BAG, new float[]{ 1.0f } ),
                new PlayerAnimationEvent( 0.5f ,(int)AnimationEventName.STORE_ITEM_IN_BAG, new float[]{ 1.0f } )
                }
            );
            m_animationInfos[Animation.BAG_PLACE_INSIDE_LEFT] = new PlayerAnimationInfo(
                Animator.StringToHash("player_bag_place_inside_left"), duration, Priority.LOW, Animation.IDLE, 1.0f,
                new PlayerAnimationEvent[]{
                new PlayerAnimationEvent( 0.0f, (int)AnimationEventName.OPEN_BAG, new float[]{ 0.0f } ),
                new PlayerAnimationEvent( 0.5f ,(int)AnimationEventName.STORE_ITEM_IN_BAG, new float[]{ 0.0f } )
                }
            );
            m_animationInfos[Animation.BAG_TAKE_OUT_RIGHT] = new PlayerAnimationInfo(
                Animator.StringToHash("player_bag_place_inside_right"), duration, Priority.MED, Animation.IDLE, 1.0f,
                new PlayerAnimationEvent[]{
                new PlayerAnimationEvent( 0.0f, (int)AnimationEventName.OPEN_BAG, new float[]{ 1.0f } ),
                new PlayerAnimationEvent( 0.5f ,(int)AnimationEventName.TAKE_OUT_OF_BAG, new float[]{ 1.0f } )
                }
            );
            m_animationInfos[Animation.BAG_TAKE_OUT_LEFT] = new PlayerAnimationInfo(
                Animator.StringToHash("player_bag_place_inside_left"), duration, Priority.MED, Animation.IDLE, 1.0f,
                new PlayerAnimationEvent[]{
                new PlayerAnimationEvent( 0.0f, (int)AnimationEventName.OPEN_BAG, new float[]{ 0.0f } ),
                new PlayerAnimationEvent( 0.5f ,(int)AnimationEventName.TAKE_OUT_OF_BAG, new float[]{ 0.0f } )
                }
            );
            animationEventContext = new PlayerAnimationEvent.Context(HandleAnimationEvent);

            //listen to animations
            OnAnimationChange += OnGlobalAnimationChange;
        }
    }

}