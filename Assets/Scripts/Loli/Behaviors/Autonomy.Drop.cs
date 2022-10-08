using UnityEngine;


namespace viva
{


    public class AutonomyDrop : AutonomyItemMove
    {

        private Vector3 targetPos;
        public Item targetItem { get; private set; }
        public OnGenericCallback onDropped;

        public AutonomyDrop(Autonomy _autonomy, string _name, Item _targetItem, Vector3 _targetPos) : base(_autonomy, _name)
        {

            targetItem = _targetItem;
            targetPos = _targetPos;
            playTargetAnim.onSuccess += CheckIfDroppedItem;

            SetItem(targetItem);
            playTargetAnim.SetupAnimationEvents(
                new AnimationEvent<OnAnimationCallback>[]{
                new AnimationEvent<OnAnimationCallback>( 0.6f, 0, DropTargetItem )
                }
            );
            playTargetAnim.onRegistered += delegate
            {
                if (targetItem != null)
                {
                    new BlendController(targetItem.mainOccupyState as LoliHandState, playTargetAnim.entryAnimation, OnAnimationIKControl, 0.5f);
                }
            };
        }

        public void SetItem(Item newItem)
        {
            if (targetItem != null)
            {
                targetItem.onMainOccupyStateChanged -= OnItemOccupyStateChanged;
            }
            targetItem = newItem;
            if (targetItem != null)
            {
                targetItem.onMainOccupyStateChanged += OnItemOccupyStateChanged;
                onRemovedFromQueue += delegate { targetItem.onMainOccupyStateChanged -= OnItemOccupyStateChanged; };
            }
        }

        private void DropTargetItem()
        {
            if (targetItem != null && targetItem.mainOccupyState != null && targetItem.mainOccupyState.owner == self)
            {
                targetItem.mainOccupyState.AttemptDrop();
                onDropped?.Invoke();
            }
        }

        private void OnItemOccupyStateChanged(OccupyState oldOccupyState, OccupyState newOccupyState)
        {
            if (newOccupyState == null)
            {
                RemoveAllPassivesAndRequirements();
            }
        }

        protected override void ReadTargetLocation(TaskTarget target)
        {
            target.SetTargetPosition(targetPos);
        }

        private void CheckIfDroppedItem()
        {
            if (targetItem.mainOccupyState == null)
            {
                return;
            }
            CheckIfShouldPlayAgain();
        }

        private float OnAnimationIKControl(BlendController blendController)
        {
            if (targetItem == null)
            {
                return 0.0f;
            }
            LoliHandState targetHandState = targetItem.mainOccupyState as LoliHandState;
            if (targetHandState == null)
            {
                return 0.0f;
            }

            if (Vector3.SqrMagnitude(targetItem.transform.position - targetPos) < 0.01f)
            {
                targetHandState.AttemptDrop();
            }
            Tools.DrawCross(targetPos, Color.green, 0.1f);

            float pickupHeight = targetPos.y - self.floorPos.y;
            pickupHeight = Mathf.Clamp01(pickupHeight);

            self.animator.SetFloat(WorldUtil.pickupHeightID, pickupHeight);
            float lerp = Mathf.Clamp01((0.5f - Mathf.Abs(0.5f - self.GetLayerAnimNormTime(1))) * 2.0f);

            if (targetHandState != null)
            {
                blendController.armIK.OverrideWorldRetargetingTransform(
                    blendController.retargetingInfo,
                    targetPos + Vector3.up * 0.05f, //pad a bit higher to properly drop item
                    AutonomyPickup.CalculatePickupPole(blendController.armIK.sign, self),
                    null
                );
            }
            return 1.0f;
        }

        public override bool? Progress()
        {
            if (targetItem == null)
            {
                return false;
            }
            if (targetItem.mainOccupyState == null)
            {
                return true;
            }
            return null;
        }

        protected override Loli.Animation GetTargetAnimation()
        {
            if (targetItem == null)
            {
                return Loli.Animation.NONE;
            }
            LoliHandState targetHandState = targetItem.mainOccupyState as LoliHandState;
            if (targetHandState == null)
            {
                return Loli.Animation.NONE;
            }
            switch (self.bodyState)
            {
                case BodyState.STAND:
                    if (targetHandState.rightSide)
                    {
                        return Loli.Animation.STAND_PICKUP_RIGHT;
                    }
                    else
                    {
                        return Loli.Animation.STAND_PICKUP_LEFT;
                    }
                case BodyState.FLOOR_SIT:
                    if (targetHandState.rightSide)
                    {
                        return Loli.Animation.FLOOR_SIT_REACH_RIGHT;
                    }
                    else
                    {
                        return Loli.Animation.FLOOR_SIT_REACH_LEFT;
                    }
            }
            return Loli.Animation.NONE;
        }
    }

}