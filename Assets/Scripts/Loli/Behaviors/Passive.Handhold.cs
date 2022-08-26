using UnityEngine;


namespace viva
{


    public partial class HandholdBehavior : PassiveBehaviors.PassiveTask
    {

        private int rightLimbGrabs = 0;
        private int leftLimbGrabs = 0;
        private float slowHandholdTimer = 0.0f;
        private Tools.EaseBlend matchWalkEase = new Tools.EaseBlend();
        private float matchWalkSide = 1.0f;
        private float currentOrthoDirDeg = 0.0f;
        private Quaternion matchWalkEaseHandFix = Quaternion.identity;
        public bool anyHandBeingHeld { get { return rightLimbGrabs > 0 || leftLimbGrabs > 0; } }

        public HandholdBehavior(Loli _self) : base(_self, 0.0f)
        {
        }

        public override void OnFixedUpdate()
        {

            if (rightLimbGrabs > 0)
            {
                FixedUpdateHandhold((LoliHandState)self.rightHandState);
            }
            if (leftLimbGrabs > 0)
            {
                FixedUpdateHandhold((LoliHandState)self.leftHandState);
            }
        }

        private float clampDegWithinRange(float value, float center, float radius)
        {
            if (Mathf.Abs(Mathf.DeltaAngle(value, center)) > radius)
            {

                if (Mathf.Abs(Mathf.DeltaAngle(value, center + radius)) <
                    Mathf.Abs(Mathf.DeltaAngle(value, center - radius)))
                {
                    value = center + radius;
                }
                else
                {
                    value = center - radius;
                }
            }
            return value;
        }

        private float InitCurrentMatchWalkOrthoDeg()
        {

            Vector3 diff = GameDirector.player.transform.position - self.floorPos;
            return Mathf.Atan2(diff.z, diff.x) * Mathf.Rad2Deg;
        }

        private float UpdateTargetMatchWalkOrthoDeg()
        {

            Vector3 orthoDir = Vector3.Cross(GameDirector.player.rigidBody.velocity, Vector3.up) * matchWalkSide;
            float orthoDirDeg = Mathf.Atan2(orthoDir.z, orthoDir.x) * Mathf.Rad2Deg;
            //ensure she stays within GameDirector.player's walking side half circle
            Vector3 headSideRight = -GameDirector.player.head.right * matchWalkSide;
            float headForwardXZDeg = Mathf.Atan2(headSideRight.z, headSideRight.x) * Mathf.Rad2Deg;
            orthoDirDeg = clampDegWithinRange(orthoDirDeg, headForwardXZDeg, 60.0f);
            currentOrthoDirDeg = clampDegWithinRange(currentOrthoDirDeg, headForwardXZDeg, 60.0f);
            return orthoDirDeg;
        }

        private void FixedUpdateHandhold(LoliHandState targetHandState)
        {
            // joint.massScale = targetHandState.selfItem.occupyState.blendProgress;

            HandState sourceHandState = targetHandState.selfItem.mainOccupyState as HandState;

            if (GameDirector.player.rigidBody.velocity.sqrMagnitude > 0.2f)
            {
                if (matchWalkEase.value != 0.0f && self.bodyState == BodyState.STAND)
                {

                    Vector3 targetFaceYawDir = Vector3.Cross((GameDirector.player.transform.position - self.floorPos).normalized, Vector3.up);
                    self.autonomy.Interrupt(new AutonomyFaceDirection(self.autonomy, "face direction", delegate (TaskTarget target)
                    {
                        target.SetTargetPosition(self.floorPos - targetFaceYawDir * matchWalkSide * matchWalkEase.value);
                    }, 2.0f));
                    //self.SetRootFacingTarget( self.floorPos-targetFaceYawDir*matchWalkSide*matchWalkEase.value, 260.0f, 20.0f, 45.0f );
                }

                //pull Shinobu towards the GameDirector.player's side ortho angle
                //if( matchWalkEase.value != 0.0f ){
                //currentOrthoDirDeg = Mathf.MoveTowardsAngle( currentOrthoDirDeg, UpdateTargetMatchWalkOrthoDeg(), 120.0f*Time.deltaTime );

                //Vector3 orthoDir = new Vector3( Mathf.Cos( currentOrthoDirDeg*Mathf.Deg2Rad ), 0.0f, Mathf.Sin( currentOrthoDirDeg*Mathf.Deg2Rad ) );
                //Vector3 matchWalkTargetPos = hhInfo.sourcePullBody.transform.position;
                //matchWalkTargetPos.y = GameDirector.player.floorPos.y;
                //matchWalkTargetPos -= orthoDir*0.5f;
                //Vector3 targetVel = ( matchWalkTargetPos-self.floorPos )+GameDirector.player.rigidBody.velocity;
                //self.rigidBody.velocity = Vector3.LerpUnclamped( self.rigidBody.velocity, targetVel, matchWalkEase.value*0.4f );
                //}
            }
            CheckHandHoldIntegrity(targetHandState, sourceHandState);
        }

        public override void OnLateUpdate()
        {
            if (rightLimbGrabs > 0)
            {
                LateUpdateHandhold((LoliHandState)self.rightHandState, 1.0f);
                // Debug.DrawLine( self.transform.TransformPoint( rightHandJoint.anchor ), self.transform.TransformPoint( rightHandJoint.connectedAnchor ), Color.cyan, 0.1f );
            }
            if (leftLimbGrabs > 0)
            {
                LateUpdateHandhold((LoliHandState)self.leftHandState, -1.0f);
                // Debug.DrawLine( self.transform.TransformPoint( leftHandJoint.anchor ), self.transform.TransformPoint( leftHandJoint.connectedAnchor ), Color.cyan, 0.1f );
            }
        }

        private Vector3 CalculatePickupPole(LoliHandState handState)
        {
            Loli.ArmIK armIK = handState.holdArmIK;
            float sign = -1.0f + (float)System.Convert.ToInt32(handState == self.rightHandState) * 2.0f;
            return armIK.shoulder.position + armIK.shoulder.right * -0.3f * sign + armIK.shoulder.up * 0.4f + armIK.shoulder.forward * -0.3f;
        }

        private void LateUpdateHandhold(LoliHandState targetHandState, float side)
        {
            Vector3 front = self.spine2.position + self.transform.right * side * 0.3f +
                            self.transform.forward * 0.75f +
                            self.transform.up * 0.2f;
            HandState sourceHandState = targetHandState.selfItem.mainOccupyState as HandState;

            //allow matching handhold speed only if not matching hands and only 1 handhold active
            if (rightLimbGrabs == 0 || leftLimbGrabs == 0)
            {
                if (targetHandState.rightSide != sourceHandState.rightSide)
                {
                    UpdateMatchHandholdSpeed(-side);
                }
            }
            if (matchWalkEase.value == 0.0f)
            {   //if not matching walk
                LateUpdatePostIKNonMatchWalkHandold(targetHandState, sourceHandState);
            }
        }

        private void UpdateMatchHandholdSpeed(float side)
        {

            float playerSpeed = GameDirector.player.rigidBody.velocity.sqrMagnitude;
            float loliSpeed = self.animator.GetFloat(Instance.speedID);
            if (loliSpeed > 4.0f || playerSpeed > 4.0f)
            {   //immediately cancel match walking speed
                if (slowHandholdTimer != 0.0f)
                {
                    slowHandholdTimer = 0.0f;
                    matchWalkEase.StartBlend(0.0f, 0.3f);
                }
            }
            else if (loliSpeed > 1.5f && loliSpeed < 3.5f)
            {
                //only increase if GameDirector.player isn't walking backwards
                if (GameDirector.player.head.InverseTransformDirection(GameDirector.player.rigidBody.velocity).z > 0.0f)
                {

                    slowHandholdTimer = Mathf.Min(1.0f, slowHandholdTimer + Time.deltaTime * 0.75f);
                    if (slowHandholdTimer == 1.0f && matchWalkEase.value == 0.0f)
                    {

                        GameDirector.player.CompleteAchievement(Player.ObjectiveType.HOLD_HANDS_AND_WALK);
                        matchWalkEase.StartBlend(1.0f, 0.6f);
                        matchWalkSide = side;
                        currentOrthoDirDeg = InitCurrentMatchWalkOrthoDeg();
                        matchWalkEaseHandFix = Quaternion.Euler(0.0f, -matchWalkSide * 90.0f, 0.0f);
                        self.OverrideClearAnimationPriority();
                        self.SetTargetAnimation(Loli.Animation.STAND_HAPPY_IDLE1);
                    }
                }
            }
            else
            {
                slowHandholdTimer = Mathf.Max(0.0f, slowHandholdTimer - Time.deltaTime * 0.7f);
                if (slowHandholdTimer == 0.0f && matchWalkEase.value == 1.0f)
                {
                    matchWalkEase.StartBlend(0.0f, 0.3f);
                }
            }
            matchWalkEase.Update(Time.deltaTime);
        }

        private void LateUpdatePostIKNonMatchWalkHandold(LoliHandState targetHandState, HandState sourceHandState)
        {
            float speed = self.animator.GetFloat(Instance.speedID);
            float armStretchSqDist = Vector3.SqrMagnitude(targetHandState.selfItem.rigidBody.position - sourceHandState.selfItem.rigidBody.position);
            float armStretchMax = (targetHandState.holdArmIK.ik.r0 + targetHandState.holdArmIK.ik.r1) * 0.9f;
            //if going fast or arm is stretching or pulling source hand
            if (speed > 1.3f || armStretchSqDist > armStretchMax * armStretchMax)
            {

                bool bothHands = rightLimbGrabs > 0 && leftLimbGrabs > 0;
                if (bothHands)
                {   //if both hands are being pulled
                    bool bothConstrained = rightLimbGrabs > 0 && leftLimbGrabs > 0;
                    if (bothConstrained)
                    {
                        Vector3 targetFaceYawPos = (self.rightHandState.selfItem.transform.position + self.leftHandState.selfItem.transform.position) / 2.0f;
                        targetFaceYawPos -= Vector3.Cross(Vector3.up, self.rightHandState.selfItem.transform.position - self.leftHandState.selfItem.transform.position);
                        if (self.bodyState == BodyState.STAND)
                        {
                            self.autonomy.Interrupt(new AutonomyFaceDirection(self.autonomy, "face direction", delegate (TaskTarget target)
                            {
                                target.SetTargetPosition(targetFaceYawPos);
                            }, 4.0f));
                            // self.SetRootFacingTarget( targetFaceYawPos, 260.0f, 40.0f, 25.0f );
                        }
                        if (Time.time % 1.0f > 0.8f)
                        {   //Fire ~20% of the time
                            self.SetLookAtTarget(GameDirector.player.head);
                        }
                    }
                }
                else
                {
                    if (rightLimbGrabs > 0 || leftLimbGrabs > 0)
                    {
                        if (self.bodyState == BodyState.STAND)
                        {
                            self.autonomy.Interrupt(new AutonomyFaceDirection(self.autonomy, "face direction", delegate (TaskTarget target)
                            {
                                target.SetTargetPosition(sourceHandState.selfItem.rigidBody.position);
                            }, 4.0f));
                        }
                        // self.SetRootFacingTarget( sourceHandState.selfItem.rigidBody.position, 260.0f, 40.0f, 25.0f );
                        if (Time.time % 1.0f > 0.8f)
                        {   //Fire ~20% of the time
                            self.SetLookAtTarget(sourceHandState.selfItem.transform);
                        }
                    }
                }
                self.SetViewAwarenessTimeout(1.0f);
                if (speed > 1.5f)
                {
                    if (bothHands)
                    {
                        self.OverrideClearAnimationPriority();
                        self.SetTargetAnimation(Loli.Animation.STAND_GIDDY_LOCOMOTION);
                    }
                    else
                    {
                        PlayHandholdAnimation(targetHandState == self.rightHandState);
                    }
                }
            }
        }

        // private void LateUpdatePostIKHandhold( HandholdInfo hhInfo, LoliHandState handTargetHoldState ){

        // 	HandState sourceHandState = handTargetHoldState.selfItem.occupyState as HandState;
        // 	if( sourceHandState == null ){
        // 		return;
        // 	}
        // 	Transform sourceHand = sourceHandState.fingerAnimator.hand;
        // 	Transform targetHand = handTargetHoldState.armIK.hand;
        // }

        private void CheckHandHoldIntegrity(HandState targetHandState, HandState sourceHandState)
        {
            if (sourceHandState.finishedBlending)
            {
                Transform targetHand = targetHandState.fingerAnimator.hand;
                Transform sourceHand = sourceHandState.fingerAnimator.hand;
                Vector3 checkValidDir = targetHand.position - sourceHand.position;
                float checkValidLength = checkValidDir.magnitude;
                //break match walk ease if moved too far or physics in the way
                if (checkValidLength > 0.5f || Physics.Raycast(sourceHand.position, checkValidDir, checkValidLength, Instance.wallsMask))
                {

                    // handTargetHoldState.ForceInitHoldBlend( 0.0f, 0.2f );
                    matchWalkEase.StartBlend(0.0f, 0.2f);
                }
            }
        }

        private void PlayHandholdAnimation(bool rightSide)
        {
            if (self.IsTired())
            {
                if (rightSide)
                {
                    self.SetTargetAnimation(Loli.Animation.STAND_TIRED_HANDHOLD_PULL_RIGHT);
                }
                else
                {
                    self.SetTargetAnimation(Loli.Animation.STAND_TIRED_HANDHOLD_PULL_LEFT);
                }
            }
            else
            {
                if (rightSide)
                {
                    self.SetTargetAnimation(Loli.Animation.STAND_HANDHOLD_HAPPY_PULL_RIGHT);
                }
                else
                {
                    self.SetTargetAnimation(Loli.Animation.STAND_HANDHOLD_HAPPY_PULL_LEFT);
                }
            }
        }

        public void AddHandhold(bool rightSide)
        {

            if (rightSide)
            {
                self.SetTargetAnimation(Loli.Animation.STAND_HANDHOLD_HAPPY_EMBARRASSED_RIGHT);
                rightLimbGrabs++;
            }
            else
            {
                self.SetTargetAnimation(Loli.Animation.STAND_HANDHOLD_HAPPY_EMBARRASSED_LEFT);
                leftLimbGrabs++;
            }
            slowHandholdTimer = 0.0f;

            self.locomotion.StopMoveTo();
        }

        public void RemoveHandHold(bool rightHand)
        {

            int limbGrabs = rightHand ? rightLimbGrabs : leftLimbGrabs;
            if (limbGrabs == 0)
            {
                return;
            }
            if (rightHand)
            {
                rightLimbGrabs--;
            }
            else
            {
                leftLimbGrabs--;
            }

            if (rightLimbGrabs == 0 && leftLimbGrabs == 0)
            {

                slowHandholdTimer = 0.0f;
                matchWalkEase.reset(0.0f);
                if (self.hasBalance)
                {
                    self.OverrideClearAnimationPriority();
                    self.SetTargetAnimation(self.GetLastReturnableIdleAnimation());
                }
            }
        }
    }

}