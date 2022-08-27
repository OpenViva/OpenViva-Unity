

namespace viva
{


    public partial class Player : Character
    {

        public void OnPlayerPickupEnd(OccupyState source, Item olditem, Item newItem)
        {
            EndVRAnimatorBlend();
        }

        public void DisableGrabbing()
        {
            grabbingEnabled = false;
            HideNearbyHandIndicators();
        }

        public void HideNearbyHandIndicators()
        {
            rightPlayerHandState.indicatorMR.gameObject.SetActive(false);
            leftPlayerHandState.indicatorMR.gameObject.SetActive(false);
        }

        public void EnableGrabbing()
        {
            grabbingEnabled = true;
        }

        private void LateUpdatePostIKItemInteraction(PlayerHandState handState)
        {
            if (!grabbingEnabled)
            {
                return;
            }

            //if hand is busy
            if (handState.holdType != HoldType.NULL)
            {

                bool useToggle;
                if (controls == ControlType.KEYBOARD)
                {
                    if (handState.heldItem)
                    {
                        useToggle = handState.heldItem.settings.toggleHolding;
                    }
                    else
                    {
                        useToggle = false;
                    }
                }
                else
                {
                    useToggle = !GameSettings.main.disableGrabToggle;
                }

                //drop item, Index does NOT use grab toggling
                bool drop;
                if (useToggle)
                {
                    drop = handState.gripState.isDown;
                }
                else
                {
                    drop = handState.gripState.isUp;
                }
                if (drop)
                {
                    handState.gripState.Consume();
                    handState.AttemptDrop();

                    //store in a nearby bag if available
                    Bag bag = Tools.FindClosestToSphere<Bag>(handState.fingerAnimator.hand.position, 0.15f, WorldUtil.visionMask);
                    if (bag != null)
                    {
                        bag.StoreItem(handState.heldItem);
                    }
                }
                //if hand isn't busy and pressed grip
            }
            else if (handState.gripState.isDown)
            {
                if (handState.AttemptGrabNearby())
                {
                    handState.actionState.Consume();
                    handState.gripState.Consume();
                    HideNearbyHandIndicators();
                }
                else
                {
                    //if player didn't grab anything
                    if (handState.animSys.currentAnim == Player.Animation.IDLE)
                    {
                        if (handState.animSys.idleAnimation != Player.Animation.KEYBOARD_HANDS_DOWN)
                        {     
                            handState.animSys.SetTargetAndIdleAnimation(Player.Animation.POINT);                         
                        }
                    }
                        
                }
                //if hand isn't busy and released grip
            }
            else if (handState.gripState.isUp)
            {
                if (handState.animSys.currentAnim == Player.Animation.POINT)
                {
                    if (handState.animSys.idleAnimation != Player.Animation.KEYBOARD_HANDS_DOWN)
                    {
                        handState.animSys.SetTargetAndIdleAnimation(Player.Animation.IDLE);
                    }
                }
                //if hand isn't doing anything
            }
            else if (GameDirector.instance.physicsFrame)
            {
                handState.FixedUpdateNearestItemIndicatorTimer();
            }
        }
    }

}