using UnityEngine;


namespace viva
{


    public partial class HeadpatBehavior : PassiveBehaviors.PassiveTask
    {

        private bool wantsMoreHeadpats = false;

        private Loli.TwoBoneIK spineIK;
        private Loli.ArmIK.RetargetingInfo spineInfo = new Loli.ArmIK.RetargetingInfo();
        private Tools.EaseBlend headpatBlend = new Tools.EaseBlend();
        private Vector3 headpatStartOffset;
        private float headpatRoughnessTolerance = 0.0f;
        private float headpatProper = 0.0f;
        private float headpatProperCooldownMult = 0.0f;
        private float headpatRoughness = 0.0f;
        private float headpatRoughnessCooldownMult = 0.0f;
        private float headpatProperTotal = 0.0f;
        private HandState headpatSourceHoldState = null;
        private Vector3 headpatSourceForce = Vector3.zero;
        private float lastHeadpatIdleSadTime = -Mathf.Infinity;
        private float acceptHeadpatsAfterTime = 0.0f;
        private bool refuseGoingHappy = false;
        private bool floorSitDisableYaw = false;

        public HandState GetLastHeadpatSourceHoldState()
        {
            return headpatSourceHoldState;
        }

        private void SecondaryHeadpatOnPostRigHierarchyChange()
        {

            Transform spine3 = self.spine3;
            Transform neck = spine3.Find("neck");
            float spine3Length = Vector3.Distance(spine3.position, neck.position);
            float neckLength = Vector3.Distance(neck.position, self.head.position);

            spineIK = new Loli.TwoBoneIK(spine3, spine3Length, Quaternion.Euler(0, 0, 0), neck, neckLength, Quaternion.Euler(0, 0, 0));
        }

        public HeadpatBehavior(Loli _self) : base(_self, 0.0f)
        {

            //call as an init
            SecondaryHeadpatOnPostRigHierarchyChange();

            self.AttachOnPostRigHierarchyChangeListener(SecondaryHeadpatOnPostRigHierarchyChange);
        }

        public void PlayProperHeadpatSound()
        {

            if (headpatProperCooldownMult < 0.4 && headpatRoughnessCooldownMult == 1.0f)
            {
                self.Speak(Loli.VoiceLine.HEADPAT_HAPPY);
            }
        }

        public bool IsHeadpatActive()
        {
            return headpatBlend.getTarget() == 1.0f;
        }

        public bool AttemptBeginHeadpat(Item sourceColliderItem)
        {

            if (sourceColliderItem == null || sourceColliderItem.mainOwner == null)
            {
                return false;
            }
            Player player = sourceColliderItem.mainOwner as Player;
            if (player == null)
            {
                return false;
            }
            PlayerHandState sourceHandState = player.GetHandStateFromSelfItem(sourceColliderItem);
            if (sourceHandState == null)
            {
                return false;
            }
            if (!AllowedToBeginHeadpat(sourceColliderItem))
            {
                return false;
            }
            //only headpat if start animation is available
            Loli.Animation headpatStartAnimation = GetHeadpatStartAnimation();
            if (headpatStartAnimation == Loli.Animation.NONE)
            {
                return false;
            }
            if (!player.AttemptApplyHeadpatAsSource(sourceHandState))
            {
                return false;
            }
            InitializeHeadpat(headpatStartAnimation, sourceHandState);
            return true;
        }

        private bool AllowedToBeginHeadpat(Item sourceColliderItem)
        {

            if (sourceColliderItem == null)
            {
                return false;
            }
            if (sourceColliderItem.settings.itemType != Item.Type.CHARACTER)
            {
                return false;
            }
            //hand must be going towards head
            if (Vector3.Dot(sourceColliderItem.rigidBody.velocity, sourceColliderItem.transform.position - self.head.position) > 0.0f)
            {
                return false;
            }
            //disable headpats during a BodyState change
            if (self.IsAnimationChangingBodyState())
            {
                return false;
            }
            //ask permission from current active behavior
            if (!self.active.RequestPermission(ActiveBehaviors.Permission.BEGIN_HEADPAT))
            {
                return false;
            }
            //Check if it's the top half of the head sphere
            Vector3 localPos = self.head.InverseTransformPoint(sourceColliderItem.transform.position);
            if (localPos.y < 0.05f)
            {
                return false;
            }
            //fail if already being headpatted
            if (IsHeadpatActive())
            {
                return false;
            }
            return true;
        }

        private void InitializeHeadpat(Loli.Animation headpatStartAnimation, PlayerHandState newHeadpatSourceHoldState)
        {
            headpatSourceHoldState = newHeadpatSourceHoldState;
            headpatSourceHoldState.owner.AddModifyAnimationCallback(AnimateHeadpatSourceHand);
            self.AddModifyAnimationCallback(AnimateReceiveHeadpat);
            //begin headpat, now the player can cancel the headpat from his end through a callback

            headpatBlend.StartBlend(1.0f, 0.8f);

            headpatRoughnessTolerance = 0.5f;
            headpatProper = 0.0f;
            headpatProperCooldownMult = 0.0f;
            headpatRoughness = 0.0f;
            headpatRoughnessCooldownMult = 0.0f;
            headpatProperTotal = 0.0f;

            self.OverrideClearAnimationPriority();
            self.SetTargetAnimation(headpatStartAnimation);

            self.SetViewAwarenessTimeout(0.5f);

            //hint that soapy hands are needed for bathing
            if (self.active.bathing.GetBathingPhase() == BathingBehavior.BathingPhase.BATHING)
            {
                if (newHeadpatSourceHoldState.HasAttribute(HandState.Attribute.SOAPY))
                {
                    GameDirector.player.pauseMenu.DisplayHUDMessage("Your hands must be soapy to wash her hair. Find a soap bar", true, PauseMenu.HintType.HINT_NO_IMAGE);
                }
            }

            if (self.bodyState == BodyState.FLOOR_SIT)
            {
                self.ApplyDisableFaceYaw(ref floorSitDisableYaw);
            }
        }

        private void EndHeadpatAsRough()
        {

            wantsMoreHeadpats = false;
            self.ShiftHappiness(-2);
            StopHeadpat(GetHeadpatEndRoughAnimation(), 1.0f);
            GameDirector.player.CompleteAchievement(Player.ObjectiveType.MAKE_ANGRY_WITH_HEADPAT);
        }

        private void SucceedInProperHeadpat()
        {
            //50-50 chance of making happy
            if (!self.IsHappy())
            {

                headpatProperTotal = 0.0f;  //reset
                                            //refuse going happy
                if (refuseGoingHappy)
                {
                    GameDirector.player.pauseMenu.DisplayHUDMessage("Keep trying. She will eventually give in and become happy", true, PauseMenu.HintType.HINT_NO_IMAGE);
                    StopHeadpat(GetHeadpatCancelSuccessAnimation(), 1.0f);
                    //accept going happy and continue headpat
                }
                else
                {
                    Loli.Animation successAnimation = GetHeadpatSucceededAnimation();
                    if (successAnimation == Loli.Animation.NONE)
                    {
                        self.SetTargetAnimation(self.GetLastReturnableIdleAnimation());
                    }
                    else
                    {
                        self.SetTargetAnimation(successAnimation);
                    }
                    self.ShiftHappiness(4);
                    GameDirector.player.CompleteAchievement(Player.ObjectiveType.MAKE_HAPPY_WITH_HEADPAT);
                }
                refuseGoingHappy = !refuseGoingHappy;
            }
        }

        //end with optional forceHeadpatEndAnim
        private void StopHeadpat(Loli.Animation forceHeadpatEndAnim, float headpatAcceptTimeout)
        {
            if (headpatSourceHoldState == null)
            {
                return;
            }
            acceptHeadpatsAfterTime = Time.time + headpatAcceptTimeout;

            headpatBlend.StartBlend(0.0f, 0.4f);

            self.OverrideClearAnimationPriority();
            if (forceHeadpatEndAnim == Loli.Animation.NONE)
            {
                self.SetTargetAnimation(self.GetLastReturnableIdleAnimation());
            }
            else
            {
                self.SetTargetAnimation(forceHeadpatEndAnim);
            }
            if (self.bodyState == BodyState.FLOOR_SIT)
            {
                self.RemoveDisableFaceYaw(ref floorSitDisableYaw);
            }
            PlayerHandState playerHeadpatSourceHandState = headpatSourceHoldState as PlayerHandState;
            if (playerHeadpatSourceHandState != null)
            {
                if (playerHeadpatSourceHandState.holdType == HoldType.NULL)
                {
                    playerHeadpatSourceHandState.animSys.SetTargetAndIdleAnimation(Player.Animation.IDLE);
                }
            }
            headpatSourceHoldState.owner.RemoveModifyAnimationCallback(AnimateHeadpatSourceHand);
            self.RemoveModifyAnimationCallback(AnimateReceiveHeadpat);

            headpatSourceHoldState = null;
        }
        private void RespondToHeadpatForces()
        {

            if (headpatBlend.value == 0.0f || self.animationDelta == 0.0f)
            {
                return;
            }
            //decrease to 1.0 over time
            headpatProperCooldownMult = Mathf.Clamp01(headpatProperCooldownMult + self.animationDelta);
            headpatRoughnessCooldownMult = Mathf.Clamp01(headpatRoughnessCooldownMult + self.animationDelta * 1.333f);

            //react to force of headpat
            float headpatForceMag = headpatSourceForce.magnitude / 20.0f;
            if (headpatForceMag > 0.09f)
            {   //minimum for hard headpat
                IncreaseHeadpatRough(headpatForceMag);
                DecreaseHeadpatProper();
            }
            else if (headpatForceMag > 0.01f)
            {   //minimum for soft headpat
                IncreaseHeadpatProper();
            }
            else
            {
                DecreaseHeadpatProper();
                if (headpatProper == 0.0f && headpatRoughness == 0.0f && headpatProperTotal > 0.0f)
                {
                    if (Time.time - lastHeadpatIdleSadTime > 7.0f)
                    {
                        lastHeadpatIdleSadTime = Time.time;
                        if (self.IsHappy())
                        {
                            if (self.rightHandState.holdType == HoldType.NULL)
                            {
                                self.SetTargetAnimation(Loli.Animation.STAND_HEADPAT_HAPPY_IDLE_SAD_RIGHT);
                            }
                            else if (self.leftHandState.holdType == HoldType.NULL)
                            {
                                self.SetTargetAnimation(Loli.Animation.STAND_HEADPAT_HAPPY_IDLE_SAD_LEFT);
                            }
                        }
                        self.SetLookAtTarget(headpatSourceHoldState.fingerAnimator.hand);
                        self.SetViewAwarenessTimeout(1.0f);
                    }
                }
            }
            headpatRoughness = Mathf.Clamp01(headpatRoughness - self.animationDelta * headpatRoughnessCooldownMult);

            float appliedHeadpatRoughness = Mathf.Clamp01(headpatRoughness);
            appliedHeadpatRoughness = 1.0f - Mathf.Pow(1.0f - appliedHeadpatRoughness, 2.0f);
            //update animation floats
            self.animator.SetFloat(WorldUtil.headpatProperID, Tools.EaseInOutQuad(Mathf.Clamp01(headpatProper)));
            self.animator.SetFloat(WorldUtil.headpatRoughnessID, appliedHeadpatRoughness);
        }
        private void DecreaseHeadpatProper()
        {
            headpatProper = Mathf.Max(headpatProper - self.animationDelta * 4.0f * headpatProperCooldownMult, 0.0f);
        }
        private void IncreaseHeadpatProper()
        {
            //increase headpat proper time
            headpatProper = Mathf.Min(headpatProper + self.animationDelta * 2.0f, 1.1f);
            headpatProperCooldownMult = 0.0f;
            if (headpatProper >= 1.0f)
            {

                headpatProperTotal += self.animationDelta;
                if (headpatProperTotal > 2.0f)
                {
                    SucceedInProperHeadpat();
                }
            }
            //generate soap bubbles if applicable

            if (headpatSourceHoldState != null)
            {
                if (headpatSourceHoldState.HasAttribute(HandState.Attribute.SOAPY))
                {
                    self.active.bathing.OnSoapyHeadpat();
                }
            }
        }
        private void IncreaseHeadpatRough(float headpatForceMag)
        {

            headpatRoughness += headpatForceMag;
            headpatRoughnessCooldownMult = 0.0f;
            headpatProperCooldownMult = 1.0f;

            //immediately interrupt and go to rough headpat anim
            headpatRoughnessTolerance -= self.animationDelta;
            if (headpatRoughnessTolerance < 0.0f)
            {
                EndHeadpatAsRough();
            }

            if (headpatSourceHoldState != null)
            {
                self.SetLookAtTarget(headpatSourceHoldState.fingerAnimator.hand, 1.2f);
            }
        }
        public override void OnFixedUpdate()
        {
            headpatBlend.Update(self.animationDelta);
            if (headpatBlend.value == 0.0f)
            {
                headpatSourceHoldState = null;
                return;
            }
            RespondToHeadpatForces();

            //if fully in a headpat
            if (IsHeadpatActive())
            {
                Loli.Animation headpatIdleAnim = GetHeadpatIdleAnimation(); //should contain rough headpat animation blend
                if (headpatIdleAnim != Loli.Animation.NONE)
                {
                    if (headpatRoughness > 0.5f)
                    {
                        AttemptRoughHeadpatAnimation(headpatIdleAnim);
                    }
                    else if (self.IsSpeaking(Loli.VoiceLine.SCREAMING))
                    {
                        self.StopSpeaking();
                    }
                    //ensure idle animation is the headpat idle loop
                    if (self.IsCurrentAnimationIdle())
                    {
                        if (self.currentAnim != headpatIdleAnim)
                        {
                            self.OverrideClearAnimationPriority();
                            self.SetTargetAnimation(headpatIdleAnim);
                        }
                    }
                }
            }
        }

        private void AttemptRoughHeadpatAnimation(Loli.Animation idleHeadpatAnim)
        {

            switch (self.bodyState)
            {   //do not allow rough headpat, treat as a poke
                // case BodyState.SLEEP_PILLOW_SIDE:
                // 	StopHeadpat( self.active.sleeping.GetSleepSidePillowPostFacePokeAnimation(), 1.0f );
                // 	break;
                // case BodyState.SLEEP_PILLOW_UP:		//do not allow rough headpat, treat as a poke
                // 	StopHeadpat( self.active.sleeping.GetSleepPillowUpPostFacePokeAnimation(), 1.0f );
                // 	break;
                default:
                    if (idleHeadpatAnim != Loli.Animation.NONE)
                    {
                        if (!self.IsSpeaking(Loli.VoiceLine.SCREAMING))
                        {
                            self.Speak(Loli.VoiceLine.SCREAMING, true);
                        }
                    }
                    //override to idle loop immediately if rough headpat
                    if (self.currentAnim != idleHeadpatAnim)
                    {
                        self.OverrideClearAnimationPriority();
                        self.SetTargetAnimation(idleHeadpatAnim);
                    }
                    break;
            }
        }

        private void AnimateHeadpatSourceHand()
        {

            if (self.animationDelta == 0.0f)
            {
                return;
            }
            if (headpatSourceHoldState == null)
            {
                return;
            }
            //cache hand position
            headpatSourceForce = (headpatSourceHoldState.selfItem.rigidBody.velocity) * headpatBlend.value;
            headpatSourceForce.y = 0.0f;

            //project source hand position to head
            Transform sourceWrist = headpatSourceHoldState.fingerAnimator.hand.parent;
            Transform sourceTargetBone = headpatSourceHoldState.fingerAnimator.targetBone;
            SphereCollider headSC = self.GetColliderBodyPart(Loli.BodyPart.HEAD_SC) as SphereCollider;
            Vector3 headCenter = headSC.transform.TransformPoint(headSC.center);
            float headWorldRadius = headSC.transform.lossyScale.x * headSC.radius + 0.01f;

            //Check if hand is too far
            float currentHeadDistance = (sourceTargetBone.position - headCenter).magnitude;
            if (currentHeadDistance > headWorldRadius + 0.085f)
            {
                if (headpatBlend.finished)
                {
                    StopHeadpat(GetRegularHeadpatEndAnim(), 0.0f);
                    return;
                }
            }

            Vector3 headToSource = (sourceTargetBone.position - headCenter).normalized;

            Quaternion sourceWristRotation = Quaternion.LookRotation(headToSource, sourceTargetBone.right);
            sourceWristRotation *= Quaternion.Euler(0.0f, 8.0f, -90.0f);

            sourceWrist.rotation = Quaternion.LerpUnclamped(
                sourceWrist.rotation,
                sourceWristRotation,
                headpatBlend.value
            );

            Vector3 sourceTargetBonePos = headCenter + headToSource * headWorldRadius;
            Vector3 sourceWristPos = sourceWrist.position + (sourceTargetBonePos - sourceTargetBone.position);
            //check if hand is too far below
            if (self.head.InverseTransformPoint(sourceWristPos).y < 0.0f)
            {
                if (headpatBlend.finished)
                {
                    StopHeadpat(GetRegularHeadpatEndAnim(), 0.0f);
                    return;
                }
            }
            sourceWrist.position = Vector3.LerpUnclamped(
                sourceWrist.position,
                sourceWristPos,
                headpatBlend.value
            );
            Debug.DrawLine(headCenter, headCenter + headToSource, Color.blue, 0.1f);

        }

        private Vector3 headpatSpine3EulerOffset = new Vector3(-90.0f, 180.0f, 0.0f);
        private Vector3 headpatNeckEulerOffset = new Vector3(120.0f, 0.0f, 0.0f);

        private void AnimateReceiveHeadpat()
        {

            if (headpatSourceHoldState == null)
            {
                return;
            }
            Vector3 targetPos = headpatSourceHoldState.fingerAnimator.targetBone.position;
            Quaternion oldSpine3Rot = spineIK.p0.rotation;
            Quaternion oldNeckRot = spineIK.p1.rotation;

            spineInfo.target = targetPos;
            spineInfo.pole = spineIK.p1.position - self.head.forward;   //po1e is near neck

            //if player moves hand too low on head sphere, cancel headpat
            Vector3 localTargetPos = self.head.InverseTransformPoint(targetPos) * headpatBlend.value;
            if (localTargetPos.y < -1.0f)
            {
                StopHeadpat(GetRegularHeadpatEndAnim(), 0.0f);
            }
            //bend towards headpat source
            Vector3 currentSpine3EulerOffset = headpatSpine3EulerOffset;
            currentSpine3EulerOffset.y += Mathf.Clamp(localTargetPos.x * -200.0f, -30.0f, 30.0f);
            currentSpine3EulerOffset.x += Mathf.Clamp(localTargetPos.z * 200.0f, -30.0f, 30.0f);
            spineIK.offset0 = Quaternion.Euler(currentSpine3EulerOffset);
            Vector3 currentNeckEulerOffset = headpatNeckEulerOffset;
            currentNeckEulerOffset.y += Mathf.Clamp(localTargetPos.x * 200.0f, -30.0f, 30.0f);
            currentNeckEulerOffset.x += Mathf.Clamp(localTargetPos.z * 200.0f, -30.0f, 30.0f);
            spineIK.offset1 = Quaternion.Euler(currentNeckEulerOffset);
            spineIK.Solve(spineInfo.target, spineInfo.pole);

            float spineBlend = headpatBlend.value * (0.1f + headpatRoughness * 0.3f);
            spineIK.p0.rotation = Quaternion.RotateTowards(
                oldSpine3Rot,
                Quaternion.LerpUnclamped(
                    oldSpine3Rot,
                    spineIK.p0.rotation,
                    spineBlend
                ),
                30.0f
            );

            Quaternion newNeckRot = spineIK.p1.rotation;
            float neckBlend = headpatBlend.value * (0.5f + headpatRoughness * 0.5f);
            spineIK.p1.rotation = Quaternion.RotateTowards(
                oldNeckRot,
                Quaternion.LerpUnclamped(
                    oldNeckRot,
                    newNeckRot,
                    neckBlend
                ),
                45.0f
            );
            Quaternion oldHeadRotation = self.head.rotation;
            self.head.rotation = Quaternion.LookRotation(
                (targetPos - self.head.position).normalized,
                spineIK.p0.right
            );
            self.head.rotation *= Quaternion.Euler(new Vector3(0.0f, 90.0f, 90.0f));
            self.head.rotation = Quaternion.RotateTowards(
                oldHeadRotation,
                Quaternion.LerpUnclamped(
                    oldHeadRotation,
                    self.head.rotation,
                    neckBlend
                ),
                50.0f
            );
        }

        public override void OnAnimationChange(Loli.Animation oldAnim, Loli.Animation newAnim)
        {
            switch (newAnim)
            {
                case Loli.Animation.STAND_HEADPAT_HAPPY_WANTED_MORE:
                case Loli.Animation.STAND_HEADPAT_ANGRY_LOOP:
                    if (headpatSourceHoldState != null)
                    {
                        self.SetLookAtTarget(headpatSourceHoldState.fingerAnimator.targetBone);
                        self.SetViewAwarenessTimeout(2.0f);
                    }
                    return;
            }
            switch (oldAnim)
            {
                case Loli.Animation.STAND_HEADPAT_HAPPY_WANTED_MORE:
                case Loli.Animation.STAND_HEADPAT_ANGRY_LOOP:
                    self.SetViewAwarenessTimeout(0.0f);
                    return;
            }
        }
    }

}