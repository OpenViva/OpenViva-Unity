using System.Collections.Generic;
using UnityEngine;


namespace viva
{

    using LoliAnimationEvent = AnimationEvent<float[]>;

    public partial class Loli : Character
    {


        private Animation m_lastAnim = Animation.NONE;
        public Animation lastAnim { get { return m_lastAnim; } }
        private Animation m_currentAnim = Animation.NONE;
        public Animation currentAnim { get { return m_currentAnim; } }
        private Animation m_targetAnim = Animation.NONE;
        public Animation targetAnim { get { return m_targetAnim; } }
        private Priority currentAnimPriority = Priority.NONE;
        private Priority highestTargetAnimPriority = Priority.NONE;
        private LoliAnimationEvent.Context animationEventContext;
        public bool currentAnimationLoops { get; private set; }
        public BodyStateAnimationSet[] bodyStateAnimationSets { get; private set; } = null;

        [SerializeField]
        public Animator animator = null;
        public Happiness happiness = Happiness.VERY_HAPPY;
        public bool Tired = false;
        private int lastLegsAnimID = -1;
        private int lastTorsoAnimID = -1;
        private int lastFaceAnimID = -1;
        private float torsoStartNormTime = 0.0f;
        private float lastTorsoChangeTime = 0.0f;
        public float lastTorsoNormTimePlayed { get; private set; }

        public bool IsTired()
        {
            return Tired;
        }
        public bool IsHappy()
        {
            return (int)happiness >= 0;
        }
        public void ShiftHappiness(int amount)
        {
            happiness = (Happiness)Mathf.Clamp((int)happiness + amount, (int)Happiness.VERY_ANGRY, (int)Happiness.VERY_HAPPY);
        }
        public bool FeelsAtLeast(Happiness minimum)
        {
            return (int)happiness >= (int)minimum;
        }

        public void SetTargetAnimation(Animation newTargetAnim)
        {
            AnimationInfo newTargetAnimInfo = animationInfos[newTargetAnim];

            if (newTargetAnim == Animation.NONE)
            {
                Debug.LogError("ERROR ANIMATION IS NONE!");
                //Debug.Break();
            }
            if (bodyState != newTargetAnimInfo.conditionBodyState || highestTargetAnimPriority > newTargetAnimInfo.priority)
            {
                if (highestTargetAnimPriority > newTargetAnimInfo.priority)
                {
                    // Debug.Log( "[BODYSTATE=/=]:" +highestTargetAnimPriority+" needs "+newTargetAnimInfo.priority );
                }
                if (bodyState != newTargetAnimInfo.conditionBodyState)
                {
                    // Debug.Log( "[BODYSTATE=/=]:" +newTargetAnim+" needs "+newTargetAnimInfo.conditionBodyState );
                }
                return;
            }
            //change if animation is not new target or targetAnim is different
            if (m_currentAnim != newTargetAnim && newTargetAnimInfo.priority >= highestTargetAnimPriority)
            {
                highestTargetAnimPriority = newTargetAnimInfo.priority;
                // Debug.LogError(name+":"+sessionReferenceName+"="+ newTargetAnim+" from "+m_currentAnim);
                m_targetAnim = newTargetAnim;
            }
        }

        public void SetHandAnimationImmediate(int layer, HoldFormAnimation anim)
        {
            int animID = holdAnimationStates[(int)anim];
            animator.CrossFade(animID, 0.0f, layer);
            animator.Update(0.0001f);
        }


        //safeOverride will check if animation is nt changing bodyStates
        public void OverrideClearAnimationPriority(Priority priority = Priority.NONE)
        {
            highestTargetAnimPriority = priority;
            currentAnimPriority = priority;
        }

        private void InitAnimationVariables()
        {

            bodyStateAnimationSets = GenerateBodyStateAnimationSets();
            animator.Update(0.1f);  //remove Tposing when spawned
            animationEventContext = new LoliAnimationEvent.Context(HandleAnimationEvent);
            lastPos = floorPos;

            OverrideBodyState(bodyState);
            m_targetAnim = GetLastReturnableIdleAnimation();
            AddModifyAnimationCallback(ApplyAnimationIK);
            InitializeBodyStateFunctions();
        }

        private void DebugValidateAnimationInfos()
        {

            foreach (KeyValuePair<Animation, AnimationInfo> entry in animationInfos)
            {
                entry.Value.Validate(this);
            }
        }

        private Vector3 lastPos = Vector3.zero;
        private Vector3[] speedHistory = new Vector3[4];

        private void ClearSpeedHistory()
        {
            lastPos = floorPos;
            for (int i = 0; i < speedHistory.Length; i++)
            {
                speedHistory[i] = Vector3.zero;
            }
        }

        private Vector3 CalculateSmoothSpeed()
        {
            //store X, Z, MAGNITUDE in the Vector3
            Vector3 newSpeedInfo = (floorPos - lastPos) / Time.deltaTime * WorldUtil.speedToAnim;
            newSpeedInfo.y = newSpeedInfo.z;
            newSpeedInfo.z = 1.0f + new Vector2(newSpeedInfo.x, newSpeedInfo.z).magnitude;
            lastPos = floorPos;
            //average out before saving into array
            for (int i = 0; i < speedHistory.Length - 1; i++)
            {
                Vector3 pastSpeed = speedHistory[i + 1];    //percolate values down
                speedHistory[i] = pastSpeed;
                newSpeedInfo += pastSpeed;
            }
            newSpeedInfo /= speedHistory.Length;
            speedHistory[speedHistory.Length - 1] = newSpeedInfo;   //push new speed
                                                                    //Continuously average out history
            for (int i = speedHistory.Length - 2; i-- > 1;)
            {
                speedHistory[i] = (speedHistory[i - 1] + speedHistory[i + 1]) / 2.0f;
            }
            return speedHistory[0]; //return oldest most averaged out value
        }

        private float lastSidewaysSign = 1;
        private float lastSidewaysFlipTime = 0;
        private Tools.EaseBlend sidewaysReverseEase = new Tools.EaseBlend();

        private void FixedUpdateAnimationGraph()
        {

            Vector3 speedInfo = CalculateSmoothSpeed();//Debug.Log(currSpeed.magnitude);
            Vector3 localVel = spine1RigidBody.transform.InverseTransformDirection(new Vector3(speedInfo.x, 0.0f, speedInfo.y));
            localVel.x = Mathf.Clamp(localVel.x, -1.0f, 1.0f);
            localVel.y = Mathf.Clamp(localVel.z, -1.0f, 1.0f);
            if (localVel.y < 0.0f)
            {
                if (Time.time - lastSidewaysFlipTime > 0.5f)
                {
                    lastSidewaysFlipTime = Time.time;
                    sidewaysReverseEase.StartBlend(1.0f, 0.5f);
                }
            }
            else
            {
                if (Time.time - lastSidewaysFlipTime > 0.5f)
                {
                    lastSidewaysFlipTime = Time.time;
                    sidewaysReverseEase.StartBlend(0.0f, 0.5f);
                }
            }
            sidewaysReverseEase.Update(Time.deltaTime);
            localVel.x *= 1.0f - sidewaysReverseEase.value * 2.0f;

            animator.SetFloat(WorldUtil.sidewaysSpeedID, localVel.x);
            animator.SetFloat(WorldUtil.forwardSpeedID, localVel.y);
            animator.SetFloat(WorldUtil.speedID, speedInfo.z);

            if (locomotion == null)
            {
                return;
            }
            ApplyAnimationSyncOptions();
            ApplyOnAnimationEndBodyState();
            AnimationInfo currAnimInfo = animationInfos[m_currentAnim];
            if (currAnimInfo.transitionHandle != null)
            {
                currAnimInfo.transitionHandle.Transition(this);
            }
            else
            {
                UpdateAnimationTransition(m_currentAnim);
            }
        }

        public float GetLayerAnimNormTime(int layer)
        {
            if (animator.IsInTransition(layer))
            {
                return animator.GetNextAnimatorStateInfo(layer).normalizedTime;
            }
            else
            {
                return animator.GetCurrentAnimatorStateInfo(layer).normalizedTime;
            }
        }
        public float getLayerAnimLength(int layer)
        {
            if (animator.IsInTransition(layer))
            {
                return animator.GetNextAnimatorStateInfo(layer).length;
            }
            else
            {
                return animator.GetCurrentAnimatorStateInfo(layer).length;
            }
        }



        public bool IsCurrentAnimationIdle()
        {
            return animationInfos[m_currentAnim].HasFlag(AnimationInfo.Flag.IDLE_STATE);
        }

        private void ApplyAnimationSyncOptions()
        {

            AnimationInfo animInfo = animationInfos[m_currentAnim];
            if (animator.IsInTransition(0))
            {
                AnimationInfo lastAnimInfo = animationInfos[m_lastAnim];
                if (lastAnimInfo.GetTorsoSyncFloatID() != 0)
                {
                    animator.SetFloat(lastAnimInfo.GetTorsoSyncFloatID(), animator.GetCurrentAnimatorStateInfo(0).normalizedTime);
                }
                AnimationInfo currAnimInfo = animationInfos[m_currentAnim];
                if (currAnimInfo.GetTorsoSyncFloatID() != 0)
                {
                    animator.SetFloat(currAnimInfo.GetTorsoSyncFloatID(), animator.GetNextAnimatorStateInfo(0).normalizedTime);
                }
            }
            else
            {
                AnimationInfo currAnimInfo = animationInfos[m_currentAnim];
                if (currAnimInfo.GetTorsoSyncFloatID() != 0)
                {
                    animator.SetFloat(currAnimInfo.GetTorsoSyncFloatID(), animator.GetCurrentAnimatorStateInfo(0).normalizedTime);
                }
            }
        }

        //should be used for immediate 1 frame poses
        public void ForceImmediatePose(Animation animation)
        {
            AnimationInfo animInfo;
            if (!animationInfos.TryGetValue(animation, out animInfo))
            {
                return;
            }
            animator.CrossFade(animInfo.legsStateID, 0.0f, 0);
            animator.CrossFade(animInfo.faceStateID, 0.0f, 2);
            animator.CrossFade(animInfo.torsoStateID, 0.0f, 1);
            animator.Update(0.0f);
        }

        private void ApplyOnAnimationEndBodyState()
        {
            if (onAnimationEndBodyState != BodyState.NONE && GetLayerAnimNormTime(1) >= 1.0f)
            {
                OverrideBodyState(onAnimationEndBodyState);
                onAnimationEndBodyState = BodyState.NONE;
            }
        }

        public void UpdateAnimationTransition(Animation defaultAnim)
        {

            float currTimeNorm = GetLayerAnimNormTime(1);
            AnimationInfo targetAnimInfo = animationInfos[m_targetAnim];

            //if no target animation is set
            if (m_targetAnim == Animation.NONE)
            {
                if (currTimeNorm < 1.0f)
                {
                    return;
                }
                m_targetAnim = defaultAnim;
                targetAnimInfo = animationInfos[defaultAnim];
            }
            else
            {
                //transition only if higher priority or past minTargetTransWait
                if (targetAnimInfo.priority < currentAnimPriority)
                {
                    return;
                }
            }
            if (m_targetAnim == m_currentAnim)
            {   //do not transition to same animation
                return;
            }
            if (targetAnimInfo != null)
            {
                currentAnimPriority = targetAnimInfo.priority;
                highestTargetAnimPriority = currentAnimPriority;
                AnimationInfo currentAnimInfo = animationInfos[m_currentAnim];
                AnimLogicInfo targetAnimLogicInfo = targetAnimInfo.animLogicInfo;

                if (lastLegsAnimID != targetAnimInfo.legsStateID)
                {
                    lastLegsAnimID = targetAnimInfo.legsStateID;
                    animator.CrossFade(targetAnimInfo.legsStateID, targetAnimInfo.transitionTime / getLayerAnimLength(0), 0, 0.0f);
                }
                if (targetAnimInfo.torsoStateID != -1)
                {
                    if (lastTorsoAnimID != targetAnimInfo.torsoStateID)
                    {
                        lastTorsoAnimID = targetAnimInfo.torsoStateID;

                        torsoStartNormTime = 0.0f;
                        if (targetAnimInfo.GetTorsoSyncFloatID() != 0)
                        {
                            torsoStartNormTime = GetLayerAnimNormTime(0);
                            animator.SetFloat(targetAnimInfo.GetTorsoSyncFloatID(), torsoStartNormTime);
                        }

                        lastTorsoNormTimePlayed = (Time.time - lastTorsoChangeTime) / getLayerAnimLength(1);

                        animator.CrossFade(targetAnimInfo.torsoStateID, targetAnimInfo.transitionTime / getLayerAnimLength(1), 1, torsoStartNormTime);
                        lastTorsoChangeTime = Time.time;
                    }
                }

                if (targetAnimInfo.faceStateID != -1)
                {
                    if (lastFaceAnimID != targetAnimInfo.faceStateID)
                    {
                        lastFaceAnimID = targetAnimInfo.faceStateID;
                        animator.CrossFade(targetAnimInfo.faceStateID, targetAnimInfo.transitionTime / getLayerAnimLength(2), 2);
                    }
                }
                if (targetAnimLogicInfo != null)
                {
                    setBodyVariables(
                        targetAnimLogicInfo.bodyFlags,
                        targetAnimLogicInfo.bodyFlagPercent,
                        targetAnimLogicInfo.bodyFlagDuration
                    );
                    SetEyeVariables(
                        targetAnimLogicInfo.eyeFlags,
                        targetAnimLogicInfo.eyeFlagDuration
                    );
                }
                ForceAnimateNextFrame();

                OnAnimationChange(m_currentAnim, m_targetAnim);
            }
            else
            {
                Debug.LogError("###ERROR### Unhandled animation state! " + m_targetAnim + " from " + m_currentAnim);
                Debug.Break();
            }
        }

        public LoliHandState GetPreferredHandState(Item item)
        {
            //prioritize using right hand when item is blank
            if (item == null)
            {
                if (rightHandState.occupied)
                {
                    if (leftHandState.occupied)
                    {
                        return null;
                    }
                    return leftLoliHandState;
                }
                return rightLoliHandState;
            }
            bool shouldUseRightHand = item.ShouldPickupWithRightHand(this);
            LoliHandState destHandState = null;
            //check if HandState is allowed
            if (shouldUseRightHand)
            {
                if (rightLoliHandState.heldItem == null)
                {
                    destHandState = rightLoliHandState;
                }
                else if (leftLoliHandState.heldItem == null)
                {
                    destHandState = leftLoliHandState;
                }
            }
            else
            {
                if (leftLoliHandState.heldItem == null)
                {
                    destHandState = leftLoliHandState;
                }
                else if (rightLoliHandState.heldItem == null)
                {
                    destHandState = rightLoliHandState;
                }
            }
            return destHandState;
        }

        public void OnAnimationChange(Animation oldAnim, Animation newAnim)
        {
            m_lastAnim = m_currentAnim;
            m_currentAnim = newAnim;
            if (m_currentAnim == Loli.Animation.NONE)
            {
                Debug.LogError("###ERROR### Animation is NONE coming from " + oldAnim);
                // Debug.Break();
            }
            // Debug.Log("[ANIM] "+sessionReferenceName+"="+newAnim+","+oldAnim);
            m_targetAnim = Animation.NONE;

            //reset Animation event variables
            animationEventContext.ResetCounters();

            //stop voice if binded to last Animation
            if (bindVoiceToCurrentAnim)
            {
                StopSpeaking();
                bindVoiceToCurrentAnim = false;
            }

            //reset animation binded properties
            RemoveDisableFaceYaw(ref currentAnimFaceYawToggle);
            if (currentAnimAwarenessModeBinded)
            {
                awarenessMode = AwarenessMode.NORMAL;   //reset
            }

            AnimationInfo currentAnimInfo = animationInfos[m_currentAnim];
            currentAnimationLoops = currentAnimInfo.transitionHandle == null;

            if (currentAnimInfo.conditionBodyState != currentAnimInfo.newBodyState)
            {
                OverrideBodyState(BodyState.NONE);
                onAnimationEndBodyState = currentAnimInfo.newBodyState;
            }
            HandleJobAnimationChange(oldAnim, newAnim);

            rightLoliHandState.SetupAnimationHoldBlendMap(null);
            leftLoliHandState.SetupAnimationHoldBlendMap(null);

            checkBalance = AnimationAllowsBalanceCheck(currentAnim);
            checkBalanceDuringTransition = AnimationAllowsBalanceCheck(lastAnim) && checkBalance;
            hasBalance = currentAnimInfo.newBodyState != BodyState.OFFBALANCE &&
                         animationInfos[m_currentAnim].conditionBodyState != BodyState.OFFBALANCE;

            enableStandOnGround = true;
        }
    }

}