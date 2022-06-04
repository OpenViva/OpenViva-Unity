using System.Collections.Generic;
using UnityEngine;


namespace viva
{

    using PlayerAnimationEvent = AnimationEvent<float[]>;

    public partial class Player : Character
    {

        public class HandAnimationSystem
        {

            private Player player;
            private Animation m_currentAnim = Animation.IDLE;
            public Animation currentAnim { get { return m_currentAnim; } }
            private Animation targetAnim = Animation.IDLE;
            private Animation m_lastAnim = Animation.IDLE;
            public Animation lastAnim { get { return m_lastAnim; } }
            public Animation idleAnimation { get; private set; }
            private Animation idleOverrideAnimation;
            private int layer;
            private bool finishedLooping = false;
            private Priority currAnimPriority = Priority.LOW;
            private readonly Dictionary<Animation, PlayerAnimationInfo> animationInfos;


            public HandAnimationSystem(Player _player, int _layer, Dictionary<Animation, PlayerAnimationInfo> _animationInfos)
            {
                player = _player;
                layer = _layer;
                idleAnimation = Animation.IDLE;
                animationInfos = _animationInfos;
            }

            public int GetLayer()
            {
                return layer;
            }

            public void SetTargetAnimation(Animation anim)
            {
                if (anim == Animation.IDLE)
                {
                    if (idleOverrideAnimation != Animation.NONE)
                    {
                        anim = idleOverrideAnimation;
                    }
                    else
                    {
                        anim = idleAnimation;
                    }
                }
                if (m_currentAnim != anim)
                {
                    if ((int)currAnimPriority > (int)player.animationInfos[anim].priority)
                    {
                        return;
                    }
                    InitializeNewAnimation(anim);
                }
            }

            private void InitializeNewAnimation(Animation anim)
            {
                currAnimPriority = player.animationInfos[anim].priority;
                targetAnim = anim;
                finishedLooping = false;    //reset finished flag
            }

            public void SetTargetAndIdleAnimation(Animation anim)
            {
                idleAnimation = anim;
                SetTargetAnimation(Animation.IDLE);
            }

            public void SetTargetAndIdleOverrideAnimation(Animation anim)
            {
                idleOverrideAnimation = anim;
                SetTargetAnimation(Animation.IDLE);
            }

            public void SetAnimationImmediate(Animator animator, int layer, Animation anim)
            {
                InitializeNewAnimation(anim);
                animator.CrossFade(animationInfos[anim].id, 0.0f, layer);
                animator.Update(0.0001f);
                targetAnim = anim;
            }

            public void FixedUpdateAnimationEvents(PlayerAnimationEvent.Context context, Animator animator)
            {

                float currNormTime;
                if (animator.IsInTransition(layer))
                {
                    currNormTime = animator.GetNextAnimatorStateInfo(layer).normalizedTime;
                }
                else
                {
                    currNormTime = animator.GetCurrentAnimatorStateInfo(layer).normalizedTime;
                }
                context.UpdateAnimationEvents(animationInfos[m_currentAnim].animationEvents, false, currNormTime);
            }

            public void FixedUpdateAnimationSystem(PlayerAnimationEvent.Context context, Animator animator)
            {

                float currNormTime;
                if (animator.IsInTransition(layer))
                {
                    currNormTime = animator.GetNextAnimatorStateInfo(layer).normalizedTime;
                }
                else
                {
                    currNormTime = animator.GetCurrentAnimatorStateInfo(layer).normalizedTime;
                }

                if (animator.IsInTransition(layer))
                {
                    return;
                }

                if (targetAnim == Animation.NONE)
                { //default to next anim

                    if (currNormTime < 1.0f)
                    {
                        return;
                    }
                    targetAnim = animationInfos[m_currentAnim].nextAnim;
                    if (targetAnim == Animation.IDLE)
                    {
                        targetAnim = idleAnimation;
                    }
                }
                else if (currNormTime < animationInfos[m_currentAnim].minWait)
                {
                    return;
                }

                if (targetAnim != m_currentAnim)
                {

                    CrossFadeAnimation(animator, targetAnim);
                    context.ResetCounters();
                }
                else if (currNormTime >= 1.0f)
                {
                    if (!finishedLooping)
                    {
                        finishedLooping = true;
                        if (animationInfos[m_currentAnim].onFinishAnimation != null)
                        {
                            animationInfos[m_currentAnim].onFinishAnimation(this == player.rightPlayerHandState.animSys);
                        }
                    }
                }
            }

            public void CrossFadeAnimation(Animator animator, Animation anim)
            {
                PlayerAnimationInfo newAnimInfo = animationInfos[anim];
                float length = animator.GetCurrentAnimatorStateInfo(layer).length;

                player.OnAnimationChange(this == player.rightPlayerHandState.animSys, m_currentAnim, anim);

                animator.CrossFade(newAnimInfo.id, newAnimInfo.transitionTime / length, layer);
                m_lastAnim = m_currentAnim;
                m_currentAnim = anim;
                targetAnim = Animation.NONE;
                currAnimPriority = player.animationInfos[currentAnim].priority;
            }
        }
    }

}