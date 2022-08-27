using System;
using UnityEngine;


namespace viva
{


    public class HugListenerBehavior : PassiveBehaviors.PassiveTask
    {


        private Vector3? hugSmoothPos = null;
        private float hugAnimSide = 0.0f;
        private Tools.EaseBlend rightPlayerHugBlend = new Tools.EaseBlend();
        private Tools.EaseBlend leftPlayerHandBlend = new Tools.EaseBlend();
        private Vector3? hugDirSmoothFlatForward = null;
        private Tools.EaseBlend hugStartSmooth = new Tools.EaseBlend();
        private AutonomyEmpty hugLogic = null;


        public HugListenerBehavior(Loli _self) : base(_self, Mathf.Infinity)
        {
        }

        public override void OnFixedUpdate()
        {
            foreach (var character in self.passive.nearbyCharacters.objects)
            {
                UpdateNearbyCharacter(character);
            }
        }

        private void UpdateNearbyCharacter(Character character)
        {
            Player player = character as Player;
            if (player == null)
            {
                return;
            }
            bool isHugging = IsAttemptingToHug(player);
            if (hugLogic == null)
            {

                if (isHugging)
                {
                    var hugAnim = new AutonomyEnforceBodyState(self.autonomy, "hug", BodyState.STANDING_HUG);
                    hugAnim.onSuccess += delegate
                    {
                        if (IsAttemptingToHug(player))
                        {
                            InitHug(player);
                        }
                        else
                        {
                            ReturnToIdle();
                        }
                    };
                    hugAnim.onFail += ReturnToIdle;

                    var facePlayer = new AutonomyFaceDirection(self.autonomy, "face hugger", delegate (TaskTarget target)
                    {
                        target.SetTargetPosition(player.head.position);
                    }, 2.0f);
                    hugAnim.AddPassive(facePlayer);

                    self.autonomy.Interrupt(hugAnim);
                }
            }
            else if (!isHugging)
            {
                hugLogic.FlagForFailure();
            }
        }

        private void InitHug(Character targetCharacter)
        {

            hugSmoothPos = null;
            hugDirSmoothFlatForward = null;
            hugStartSmooth.reset(0.0f);
            hugStartSmooth.StartBlend(1.0f, 1.0f);
            hugAnimSide = 0.0f;

            rightPlayerHugBlend.reset(0.0f);
            leftPlayerHandBlend.reset(0.0f);

            FixedUpdateHuggingPlayer(targetCharacter);

            self.AddOnRagdollModeBeginCallback(StopHug);

            hugLogic = new AutonomyEmpty(self.autonomy, "hug logic");
            hugLogic.onFixedUpdate += delegate { FixedUpdateHuggingPlayer(targetCharacter); };
            var facePlayer = new AutonomyFaceDirection(self.autonomy, "face hugger", delegate (TaskTarget target)
            {
                target.SetTargetPosition(targetCharacter.head.position);
            }, 2.0f);
            hugLogic.AddRequirement(facePlayer);

            hugLogic.onFail += StopHug;

            self.autonomy.Interrupt(hugLogic);
        }

        public bool IsHandWithinHugDistance(Transform hand, float padding)
        {
            return hand.position.y < self.head.position.y - padding;
        }
        private void FixedUpdateHuggingPlayer(Character activeTarget)
        {
            Player player = activeTarget as Player;
            if (player == null)
            {
                StopHug();
                return;
            }

            //push away head if too closeby
            float headBearing = Tools.Bearing(self.head, player.head.position);
            float playerProximity = Vector3.Distance(player.floorPos, self.floorPos);
            float proximityAnimDist = 1.0f - Tools.GetClampedRatio(
                self.passive.settings.hugPlayerAnimSideMinDistance,
                self.passive.settings.hugPlayerAnimSideMaxDistance,
                playerProximity
            );

            float newHugAnimSide = proximityAnimDist * Mathf.Sign(headBearing);
            //lock hugAnimSide if close enough
            if (Mathf.Abs(newHugAnimSide) > 0.8f)
            {
                newHugAnimSide = Math.Sign(newHugAnimSide);
                if (self.IsHappy())
                {
                    self.SpeakAtRandomIntervals(Loli.VoiceLine.RELIEF, 3.0f, 4.0f);
                }
                else
                {
                    self.SpeakAtRandomIntervals(Loli.VoiceLine.ANGRY_GRUMBLE_LONG, 3.0f, 4.0f);
                }
            }
            hugAnimSide = Mathf.LerpUnclamped(hugAnimSide, newHugAnimSide, Time.deltaTime * 8.0f * hugStartSmooth.value);
            self.animator.SetFloat(WorldUtil.hugSideID, hugAnimSide);

            self.SetLookAtTarget(player.head);

            Vector3 flatToPlayerHead = player.head.position - self.head.position;
            flatToPlayerHead.y = 0.0f;
            float flatToPlayerHeadL = flatToPlayerHead.magnitude;
            if (flatToPlayerHeadL > 0.0f)
            {
                flatToPlayerHead /= flatToPlayerHeadL;

                float pitchOffsetBlend = Tools.GetClampedRatio(self.passive.settings.hugPlayerHeadMinProximityDistance, self.passive.settings.hugPlayerHeadMaxProximityDistance, flatToPlayerHeadL);
                float rollOffsetBlend = Mathf.Clamp(headBearing / 90.0f, -1.0f, 1.0f);
                //smooth lerp the head side!
                Vector3 proximityOffset = new Vector3(self.passive.settings.hugPlayerPitchProximityOffset, 0.0f, rollOffsetBlend * self.passive.settings.hugPlayerRollProximityOffset) * pitchOffsetBlend;
                self.head.parent.localRotation *= Quaternion.Euler(proximityOffset);
                self.head.localRotation *= Quaternion.Euler(proximityOffset);
            }
        }

        private Vector3 CalculatePlayerHandInfluence(Transform spine3, Transform head, Transform hand, float rightSideOffset, Tools.EaseBlend blend, ref float validCounts)
        {
            blend.Update(Time.deltaTime);
            //remove influence if hand is above neck
            if (IsHandWithinHugDistance(hand, 0.1f))
            {
                if (blend.getTarget() != 1.0f)
                {
                    blend.StartBlend(1.0f, (1.0f - blend.value) * 0.8f);
                }
            }
            else if (blend.getTarget() != 0.0f)
            {
                blend.StartBlend(0.0f, blend.value * 0.8f);
            }
            validCounts += blend.value;
            //add side offset to prevent running into torso physics
            Vector3 spineToHead = head.position - spine3.position;
            spineToHead.y = 0.001f;
            spineToHead.Normalize();

            Vector3 spineToHeadRight = Vector3.Cross(Vector3.up, spineToHead);

            return (head.position - head.forward * 0.1f - hand.position + spineToHeadRight * rightSideOffset) * blend.value;
        }

        public void StopHug()
        {
            ReturnToIdle();
            self.RemoveOnRagdollModeBeginCallback(StopHug);

            hugLogic = null;
        }

        private void ReturnToIdle()
        {
            var returnToIdle = new AutonomyEnforceBodyState(self.autonomy, "idle", BodyState.STAND);
            self.autonomy.Interrupt(returnToIdle);

        }

        private bool IsAttemptingToHug(Player player)
        {
            Transform spine3 = self.spine3RigidBody.transform;
            Vector3 headToSpine3 = spine3.position - player.head.position;
            headToSpine3.y = 0.0f;
            Vector3 adjSpine3Vec = Vector3.Cross(headToSpine3, Vector3.up);
            float headToSpine3SqDist = Vector3.SqrMagnitude(headToSpine3);
            if (headToSpine3SqDist == 0.0f)
            {
                return false;
            }
            //player head must be above spine position
            if (player.head.position.y < spine3.position.y - 0.45f)
            {
                return false;
            }
            Transform playerRightHand = player.rightHandState.fingerAnimator.hand;
            Transform playerLeftHand = player.leftHandState.fingerAnimator.hand;
            if (!IsHandWithinHugDistance(playerRightHand, 0.2f) ||
                !IsHandWithinHugDistance(playerLeftHand, 0.2f))
            {
                return false;
            }
            Vector3 rightHandToHead = player.head.position - playerRightHand.position;
            rightHandToHead.y = 0.0f;
            Vector3 leftHandToHead = player.head.position - playerLeftHand.position;
            leftHandToHead.y = 0.0f;

            //if facing head in opposite ways
            if (Vector3.Dot(player.head.forward, self.head.forward) < 0.0f &&
                // if hands are on other side of spine3
                Vector3.SqrMagnitude(rightHandToHead) > headToSpine3SqDist &&
                 Vector3.SqrMagnitude(leftHandToHead) > headToSpine3SqDist &&
                // and on different sides
                Vector3.Dot(adjSpine3Vec, rightHandToHead) != Vector3.Dot(adjSpine3Vec, leftHandToHead))
            {
                return true;
            }
            return false;
        }
    }

}