namespace viva
{


    public partial class IdleBehavior : ActiveBehaviors.ActiveTask
    {

        private void InitBagAnimations()
        {

        }

        private void UpdateShoulderItemInteraction(ShoulderState shoulderState)
        {

            if (shoulderState.heldItem == null)
            {
                return;
            }
            switch (shoulderState.heldItem.settings.itemType)
            {
                case Item.Type.BAG:
                    UpdateShoulderBagInteraction(shoulderState);
                    break;
            }
        }

        private void UpdateShoulderBagInteraction(ShoulderState shoulderState)
        {

            Bag bag = shoulderState.heldItem as Bag;
            if (bag == null)
            {
                return;
            }
            LoliHandState targetHandState;
            if (shoulderState.rightSide)
            {
                targetHandState = self.leftLoliHandState;
            }
            else
            {
                targetHandState = self.rightLoliHandState;
            }
            //store item after item has been fully picked up
            if (!targetHandState.finishedBlending)
            {
                return;
            }
            //do not store if task polling is happening (might interrupt tasks like cooking)
            if (self.active.isPolling)
            {
                return;
            }
            //do not store unless task is idling or following
            if (!self.active.IsTaskActive(self.active.idle) &&
                !self.active.IsTaskActive(self.active.follow))
            {
                return;
            }
            if (bag.CanStoreItem(targetHandState.heldItem))
            {
                if (shoulderState.rightSide)
                {
                    self.SetTargetAnimation(Loli.Animation.STAND_BAG_PUT_IN_RIGHT);
                }
                else
                {
                    self.SetTargetAnimation(Loli.Animation.STAND_BAG_PUT_IN_LEFT);
                }
            }
            else
            {
                //check if the other hand has a valid storable item
                if (shoulderState.rightSide)
                {
                    targetHandState = self.rightLoliHandState;
                }
                else
                {
                    targetHandState = self.leftLoliHandState;
                }
                if (bag.CanStoreItem(targetHandState.heldItem))
                {
                    self.autonomy.SetAutonomy(new AutonomyPickup(self.autonomy, "pickup bag item", targetHandState.heldItem, self.GetPreferredHandState(targetHandState.heldItem), false));
                    // self.active.pickup.AttemptGoAndPickup( targetHandState.heldItem, self.active.pickup.FindPreferredHandState( targetHandState.heldItem ) );
                }
            }
        }

        public void OpenBag(bool rightSide)
        {
            Bag bag;
            if (rightSide)
            {
                bag = self.rightShoulderState.heldItem as Bag;
            }
            else
            {
                bag = self.leftShoulderState.heldItem as Bag;
            }
            if (bag == null)
            {
                return;
            }
            bag.PlayOpenBagShapeKeyAnimation();
        }

        public void PutItemInBag(bool rightSide)
        {
            Bag bag;
            Item targetItem;
            if (rightSide)
            {
                bag = self.rightShoulderState.heldItem as Bag;
                targetItem = self.leftLoliHandState.heldItem;
            }
            else
            {
                bag = self.leftShoulderState.heldItem as Bag;
                targetItem = self.rightLoliHandState.heldItem;
            }
            if (bag == null)
            {
                return;
            }
            bag.StoreItem(targetItem);
        }

    }

}