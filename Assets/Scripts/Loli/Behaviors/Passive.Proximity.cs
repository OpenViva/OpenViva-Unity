using UnityEngine;


namespace viva
{


    public class ProximityBehavior : PassiveBehaviors.PassiveTask
    {

        private float proximityStartTimer = 0.0f;
        private float proximityHangTimer = 0.0f;
        private Vector3 lastCheckedHeadPos = Vector3.zero;
        private float lastHeadDist = 0.0f;
        private Vector3 cheekOffset = Vector3.zero;
        private bool animBusy = false;
        private float waitToFaceTimer = 0.0f;
        private Item lastCheckedItem = null;
        private bool proximityActive = false;

        public ProximityBehavior(Loli _self) : base(_self, 0.0f)
        {

            cheekOffset = new Vector3(0.019f, 0.016f, 0.018f);
        }


        private void checkStartleProximity()
        {
            //disable during handholding
            if (self.rightHandState.holdType == HoldType.OBJECT ||
                self.leftHandState.holdType == HoldType.OBJECT)
            {
                endStartleProximity(false);
                return;
            }
            //disable during hugging
            if (self.active.IsTaskActive(self.passive.hug))
            {
                return;
            }
            //disable during headpat
            if (self.passive.headpat.IsHeadpatActive())
            {
                endStartleProximity(false);
                return;
            }
            //must be checking foreign objects
            Item lookAtItem = self.GetCurrentLookAtItem();
            if (lookAtItem == null)
            {
                endStartleProximity(true);
                return;
            }
            //make sure its not an object loli is holding
            if (lookAtItem.settings.itemType != Item.Type.CHARACTER)
            {
                endStartleProximity(true);
                return;
            }
            float headDist = Vector3.Distance(self.head.position, lookAtItem.transform.position);
            Vector3 currentCheckedPositon = self.transform.InverseTransformPoint(lookAtItem.transform.position);
            if (lastCheckedItem != lookAtItem)
            {   //detect item change
                lastCheckedItem = lookAtItem;
                lastCheckedHeadPos = currentCheckedPositon;
                lastHeadDist = headDist;
            }
            Vector3 localHeadPos = self.head.InverseTransformPoint(lookAtItem.transform.transform.position);
            if (localHeadPos.z > -0.2f && headDist < 0.5f)
            {
                if (headDist - lastHeadDist < 0.004f && (currentCheckedPositon - lastCheckedHeadPos).sqrMagnitude > 0.01f)
                {
                    StartProximityBehaviorFast(lookAtItem);
                }
                else if (self.IsCurrentAnimationIdle() && !proximityActive)
                {
                    proximityStartTimer += Time.deltaTime;
                    if (proximityStartTimer > 0.5f)
                    {
                        StartProximityBehaviorSlow(lookAtItem);
                    }
                }
                proximityHangTimer = 0.6f;
            }
            else if (proximityActive)
            {
                proximityHangTimer -= Time.deltaTime;
                if (proximityHangTimer <= 0.0f)
                {
                    endStartleProximity(true);
                }
            }
            else
            {
                proximityStartTimer = 0.0f;
            }
            lastHeadDist = headDist;
            lastCheckedHeadPos = currentCheckedPositon;
        }

        private void endStartleProximity(bool returnToIdle)
        {
            proximityActive = false;
            lastCheckedItem = null;
            if (returnToIdle)
            {
                if (self.currentAnim == Loli.Animation.STAND_FACE_PROX_HAPPY_LOOP ||
                    self.currentAnim == Loli.Animation.STAND_FACE_PROX_ANGRY_LOOP)
                {
                    self.SetTargetAnimation(self.GetLastReturnableIdleAnimation());
                }
            }
        }

        private void StartProximityBehaviorFast(Item item)
        {
            proximityActive = true;
            if (self.IsHappy())
            {
                self.SetTargetAnimation(Loli.Animation.STAND_FACE_PROX_HAPPY_SURPRISE);
            }
            else
            {
                self.SetTargetAnimation(Loli.Animation.STAND_FACE_PROX_ANGRY_SURPRISE);
            }
            if (self.active.RequestPermission(ActiveBehaviors.Permission.ALLOW_ROOT_FACING_TARGET_CHANGE))
            {
                if (self.bodyState != BodyState.AWAKE_PILLOW_UP && self.bodyState != BodyState.AWAKE_PILLOW_SIDE_LEFT && self.bodyState != BodyState.AWAKE_PILLOW_SIDE_RIGHT)
                {
                    //self.SetRootFacingTarget( item.transform.position, 200.0f, 40.0f, 10.0f );
                    self.autonomy.Interrupt(new AutonomyFaceDirection(self.autonomy, "fast proximity direction", delegate (TaskTarget target)
                    {
                        target.SetTargetPosition(item.transform.position);
                    }, 2.0f));
                }         
            }
            Vector3 surprisePush = self.transform.position - item.transform.position;
            surprisePush.y = 0.0f;
            surprisePush = surprisePush.normalized * 1.5f;
            self.locomotion.PlayForce(surprisePush, 0.3f);
        }
        private void StartProximityBehaviorSlow(Item item)
        {
            proximityStartTimer = 0.0f;
            proximityActive = true;
            if (self.IsHappy())
            {
                self.SetTargetAnimation(Loli.Animation.STAND_FACE_PROX_HAPPY_LOOP);
            }
            else
            {
                self.SetTargetAnimation(Loli.Animation.STAND_FACE_PROX_ANGRY_LOOP);
            }
            if (self.bodyState != BodyState.AWAKE_PILLOW_UP && self.bodyState != BodyState.AWAKE_PILLOW_SIDE_LEFT && self.bodyState != BodyState.AWAKE_PILLOW_SIDE_RIGHT)
            {
                if (self.active.RequestPermission(ActiveBehaviors.Permission.ALLOW_ROOT_FACING_TARGET_CHANGE))
                {
                    //self.SetRootFacingTarget( item.transform.position, 200.0f, 10.0f, 10.0f );
                    self.autonomy.Interrupt(new AutonomyFaceDirection(self.autonomy, "slow proximity direction", delegate (TaskTarget target)
                    {
                        target.SetTargetPosition(item.transform.position);
                    }, 1.0f));
                }
            }
        }

        private bool IsNearCheek(float side)
        {
            cheekOffset.x = Mathf.Abs(cheekOffset.x) * side;
            Vector3 cheekPos = self.head.TransformPoint(cheekOffset);

            if (Vector3.Distance(GameDirector.player.head.transform.position, cheekPos) < 0.22f)
            {
                return true;
            }
            return false;
        }

        public override void OnUpdate()
        {

            //disable during hugging
            if (self.active.IsTaskActive(self.passive.hug))
            {
                return;
            }
            checkStartleProximity();

            if (self.currentAnim == Loli.Animation.STAND_FACE_PROX_HAPPY_LOOP)
            {
                waitToFaceTimer -= Time.deltaTime;
                if (waitToFaceTimer < 0.0f)
                {
                    waitToFaceTimer = 10.0f;
                    self.SetLookAtTarget(GameDirector.player.head);
                }
            }
        }

        public override void OnAnimationChange(Loli.Animation oldAnim, Loli.Animation newAnim)
        {
            switch (newAnim)
            {
                case Loli.Animation.STAND_FACE_PROX_ANGRY_SURPRISE:
                case Loli.Animation.STAND_FACE_PROX_HAPPY_SURPRISE:
                    self.SetViewAwarenessTimeout(2.0f);
                    break;
            }
            switch (oldAnim)
            {
                case Loli.Animation.STAND_FACE_PROX_ANGRY_SURPRISE:
                case Loli.Animation.STAND_FACE_PROX_HAPPY_SURPRISE:
                    animBusy = false;
                    break;
            }
            switch (newAnim)
            {
                case Loli.Animation.STAND_FACE_PROX_ANGRY_SURPRISE:
                case Loli.Animation.STAND_FACE_PROX_HAPPY_SURPRISE:
                    animBusy = true;
                    break;
            }
        }
    }

}