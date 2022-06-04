using UnityEngine;


namespace viva
{


    public class ChickenChaseBehavior : ActiveBehaviors.ActiveTask
    {

        private enum ChaseState
        {
            NONE,
            WALK_TO_CIRCLE,
            CIRCLING,
            APPROACHING,
            CHASING
        }

        private ChaseState state = ChaseState.NONE;
        private Chicken targetChicken;
        private Character circleAnchorCharacter;
        private float keepRadius = 3.0f;
        private float tangentSpeed = 0.0f;
        private float chaseTimeout = 0.0f;

        public ChickenChaseBehavior(Loli _self) : base(_self, ActiveBehaviors.Behavior.CHICKEN_CHASE, null)
        {
        }

        public bool AttemptChaseChicken(Chicken newTargetChicken)
        {

            if (newTargetChicken == null)
            {
                return false;
            }
            if (!self.active.IsTaskActive(self.active.idle) &&
                !self.active.IsTaskActive(self.active.follow))
            {
                return false;
            }
            if (!self.IsHappy() || self.IsTired())
            {
                self.active.idle.PlayAvailableRefuseAnimation();
                return false;
            }

            circleAnchorCharacter = GameDirector.player;
            targetChicken = newTargetChicken;
            self.active.SetTask(self.active.chickenChase, null);
            return true;
        }

        // public override void OnActivate(){
        // 	state = ChaseState.WALK_TO_CIRCLE;
        // 	chaseTimeout = 0.0f;
        // 	tangentSpeed = 0.0f;
        // }

        public override void OnDeactivate()
        {
            if (self.currentAnim == Loli.Animation.STAND_CHASE_LOW_LOCOMOTION)
            {
                self.SetTargetAnimation(self.GetLastReturnableIdleAnimation());
            }
        }

        public override void OnUpdate()
        {

            if (targetChicken == null || targetChicken.chickenItem.tamed || targetChicken.chickenItem.mainOccupyState != null)
            {
                self.active.SetTask(self.active.idle, false);
                return;
            }
            switch (state)
            {
                case ChaseState.WALK_TO_CIRCLE:
                    UpdateWalkToCircle();
                    break;
                case ChaseState.APPROACHING:
                    UpdateApproaching();
                    break;
            }
            // self.SetRootFacingTarget( targetChicken.pelvis.position, 200.0f, 20.0f, 10.0f );
            self.autonomy.SetAutonomy(new AutonomyFaceDirection(self.autonomy, "look chicken", delegate (TaskTarget target)
            {
                target.SetTargetPosition(targetChicken.pelvis.position);
            }, 20.0f));
            self.SetLookAtTarget(targetChicken.transform);
        }

        public override void OnFixedUpdate()
        {

            switch (state)
            {
                case ChaseState.CIRCLING:
                    FixedUpdateCircling();
                    break;
            }
        }

        private void UpdateWalkToCircle()
        {
            Vector3? finalDest = self.locomotion.GetCurrentDestination();
            if (!finalDest.HasValue)
            {
                MoveToCircleIfPointIsOutsideKeepRadius(self.floorPos);
            }
            else
            {
                MoveToCircleIfPointIsOutsideKeepRadius(finalDest.Value);
            }
            if (self.currentAnim != Loli.Animation.STAND_GIDDY_LOCOMOTION)
            {
                self.SetTargetAnimation(Loli.Animation.STAND_GIDDY_LOCOMOTION);
            }
        }

        private void MoveToCircleIfPointIsOutsideKeepRadius(Vector3 point)
        {
            Vector3 chickenToPoint = point - targetChicken.pelvis.position;
            float chickenToPointLength = chickenToPoint.magnitude;
            // if not moving towards circle
            // of final destination does not see chicken
            if (chickenToPointLength > keepRadius + 0.5f ||
                !Physics.Raycast(point, chickenToPoint, chickenToPointLength, Instance.wallsMask))
            {

                self.locomotion.AttemptContinuousNavSearch(
                    new LocomotionBehaviors.PathRequest[]{
                    new LocomotionBehaviors.NavSearchCircle(
                        targetChicken.pelvis.position+Vector3.up*1.5f,
                        keepRadius,
                        3.0f,
                        8,
                        Instance.wallsMask
                    )
                    },
                    OnFindCircleStart
                );
            }
        }

        private void OnFindCircleStart(Vector3[] path, Vector3 navSearchPoint, Vector3 navPointDir)
        {
            if (!self.active.IsTaskActive(this))
            {
                return;
            }
            if (path == null)
            {
                Debug.LogError("NO CIRCLE FOUND");
                self.active.SetTask(self.active.idle, false);
                return;
            }
            self.locomotion.FollowPath(path, OnReachCircleStart);
        }

        private void OnReachCircleStart()
        {
            state = ChaseState.CIRCLING;
        }

        private void FixedUpdateCircling()
        {
            //circle chicken so player is on the receiving end
            Vector3 chickenToSelf = self.head.position - targetChicken.pelvis.position;
            float chickenToSelfLength = chickenToSelf.magnitude;
            if (chickenToSelfLength > keepRadius + 0.5f)
            {
                state = ChaseState.WALK_TO_CIRCLE;
                return;
            }

            if (self.currentAnim != Loli.Animation.STAND_CHASE_LOW_LOCOMOTION)
            {
                self.SetTargetAnimation(Loli.Animation.STAND_CHASE_LOW_LOCOMOTION);
            }
            //normalize for future use
            chickenToSelf /= chickenToSelfLength;

            //Reach other side (PI) relative to player
            Vector3 chickenToAnchor = (circleAnchorCharacter.head.position - targetChicken.pelvis.position).normalized;

            float selfRelativeDeg = Mathf.Atan2(chickenToSelf.x, chickenToSelf.z) * Mathf.Rad2Deg;
            float anchorRelativeDeg = Mathf.Atan2(chickenToAnchor.x, chickenToAnchor.z) * Mathf.Rad2Deg;

            float angleDiff = Mathf.DeltaAngle(selfRelativeDeg, anchorRelativeDeg);
            if (Mathf.Abs(angleDiff) > 150.0f)
            {   //reached other side
                state = ChaseState.APPROACHING;
                self.Speak(Loli.VoiceLine.ANGRY_GRUMBLE_LONG, false);
                return;
            }
            //move along circle tangent until reached other side
            Loli.LocomotionInfo legSpeedInfo = Loli.GetLegSpeedInfo(self.currentAnim);
            if (legSpeedInfo == null)
            {
                return;
            }
            Vector3 tangent = Vector3.Cross(chickenToSelf, Vector3.up);
            int dirSign = (int)Mathf.Sign(angleDiff);
            tangentSpeed = Mathf.Clamp(tangentSpeed + legSpeedInfo.acceleration * Time.fixedDeltaTime * dirSign, -legSpeedInfo.maxSpeed, legSpeedInfo.maxSpeed);
            tangentSpeed *= 0.98f;

            //check if running into wall
            Vector3 wallTestStart = self.spine1RigidBody.transform.position;
            bool wallObstacleHit = GamePhysics.GetRaycastInfo(wallTestStart, tangent * dirSign, 0.3f, Instance.wallsMask, QueryTriggerInteraction.Ignore);
            bool stuck = self.spine1RigidBody.velocity.sqrMagnitude < 0.008f;
            if (wallObstacleHit || stuck)
            {
                chaseTimeout += Time.fixedDeltaTime;
                //if obstructed, switch to approach phase
                if (chaseTimeout > 1.0f)
                {
                    state = ChaseState.APPROACHING;
                    self.Speak(Loli.VoiceLine.ANGRY_GRUMBLE_LONG, false);
                    return;
                }
            }
            else
            {
                //ensure timer can go up if fails in between frames
                chaseTimeout = Mathf.Max(0.0f, chaseTimeout - Time.fixedDeltaTime * 0.5f);
            }
            self.spine1RigidBody.velocity = Vector3.LerpUnclamped(self.spine1RigidBody.velocity, tangent * tangentSpeed, Time.fixedDeltaTime * 10.0f);
        }

        private void UpdateApproaching()
        {

            //actually chase chicken
            Vector3? finalDest = self.locomotion.GetCurrentDestination();
            if (!finalDest.HasValue)
            {
                MoveToChickenIfPointIsOutsideChaseRadius(self.floorPos);
            }
            else
            {
                MoveToChickenIfPointIsOutsideChaseRadius(finalDest.Value);
            }

            if (self.currentAnim != Loli.Animation.STAND_CHASE_LOW_LOCOMOTION)
            {
                self.SetTargetAnimation(Loli.Animation.STAND_CHASE_LOW_LOCOMOTION);
            }
            chaseTimeout += Time.deltaTime;
            if (chaseTimeout > 15.0f)
            {
                self.active.SetTask(self.active.idle, false);
                return;
            }
            //attempt pickup
            if (Vector3.SqrMagnitude(targetChicken.transform.position - self.floorPos) < 0.5f)
            {
                self.Speak(Loli.VoiceLine.YA_ANNOYED, false);

                if (!self.rightHandState.occupied)
                {
                    self.rightHandState.GrabItemRigidBody(targetChicken.chickenItem);
                    self.active.SetTask(self.active.idle, true);
                }
                else if (!self.leftHandState.occupied)
                {
                    self.leftHandState.GrabItemRigidBody(targetChicken.chickenItem);
                    self.active.SetTask(self.active.idle, true);
                }
                else
                {
                    self.active.SetTask(self.active.idle, false);
                }
            }
        }

        private void MoveToChickenIfPointIsOutsideChaseRadius(Vector3 point)
        {
            Vector3 chickenToPoint = point - targetChicken.pelvis.position;
            float chickenToPointLength = chickenToPoint.magnitude;
            // if not moving towards circle
            // of final destination does not see chicken
            if (chickenToPointLength > 0.5f)
            {

                Vector3[] path = self.locomotion.GetNavMeshPath(targetChicken.pelvis.position);
                if (path == null)
                {
                    Debug.LogError("no path");
                    self.active.SetTask(self.active.idle, false);
                    return;
                }
                chickenToPoint /= chickenToPointLength;
                //chickenToPoint
                self.locomotion.FollowPath(path);
            }
        }
    }

}