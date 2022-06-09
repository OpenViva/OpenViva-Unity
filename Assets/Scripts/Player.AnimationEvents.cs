using UnityEngine;


namespace viva
{


    public partial class Player : Character
    {

        public enum AnimationEventName
        {
            OPEN_BAG,
            STORE_ITEM_IN_BAG,
            TAKE_OUT_OF_BAG,
            FIRE_GESTURE,
            ADD_CARD_TO_RIGHT_HAND,
            ADD_CARD_TO_LEFT_HAND,
        }


        public delegate void OnAnimationChangeListener(bool rightHand, Animation oldAnim, Animation newAnim);
        private OnAnimationChangeListener OnAnimationChange;

        public void AddOnAnimationChangeListener(OnAnimationChangeListener listener)
        {
            OnAnimationChange -= listener;
            OnAnimationChange += listener;
        }

        public void RemoveOnAnimationChangeListener(OnAnimationChangeListener listener)
        {
            OnAnimationChange -= listener;
        }

        private void DebugAssert(AnimationEvent<float[]> animEvent, int minSize)
        {
#if UNITY_EDITOR
            if (minSize <= 0)
            {
                if (animEvent.parameter != null)
                {
                    Debug.LogError("Warning event parameter not null for " + animEvent.nameID);
                }
            }
            else
            {
                if (animEvent.parameter.Length != minSize)
                {
                    Debug.LogError("ERROR event parameter not " + minSize + " for " + animEvent.nameID);
                }
            }
#endif
        }

        public void HandleAnimationEvent(AnimationEvent<float[]> animEvent)
        {
            float[] parameter = animEvent.parameter;
            switch (animEvent.nameID)
            {
                case (int)AnimationEventName.OPEN_BAG:
                    DebugAssert(animEvent, 1);
                    AnimEventOpenBag((int)parameter[0]);
                    break;
                case (int)AnimationEventName.STORE_ITEM_IN_BAG:
                    DebugAssert(animEvent, 1);
                    AnimEventPlaceBagInsideItem((int)parameter[0]);
                    break;
                case (int)AnimationEventName.TAKE_OUT_OF_BAG:
                    DebugAssert(animEvent, 1);
                    AnimEventTakeItemOutofBag((int)parameter[0]);
                    break;
                case (int)AnimationEventName.FIRE_GESTURE:
                    DebugAssert(animEvent, 2);
                    objectFingerPointer.FireGesture(parameter[0] == 1 ? objectFingerPointer.rightGestureHand : objectFingerPointer.leftGestureHand, (ObjectFingerPointer.Gesture)(parameter[1]));
                    break;
                case (int)AnimationEventName.ADD_CARD_TO_RIGHT_HAND:
                    DebugAssert(animEvent, 0);
                    AnimEventAddCardToCardGroup(leftHandState.GetItemIfHeld<PokerCard>(), rightHandState.GetItemIfHeld<PokerCard>());
                    break;
                case (int)AnimationEventName.ADD_CARD_TO_LEFT_HAND:
                    DebugAssert(animEvent, 0);
                    AnimEventAddCardToCardGroup(rightHandState.GetItemIfHeld<PokerCard>(), leftHandState.GetItemIfHeld<PokerCard>());
                    break;
            }
        }

        private void AnimEventAddCardToCardGroup(PokerCard source, PokerCard dest)
        {
            if (source != null && dest != null)
            {
                var dropped = source.DropAllFanGroupCards();
                dropped.Add(source);
                dest.AddToFanGroup(dropped);
            }
        }

        public void AnimEventOpenBag(int inRightHand)
        {
            Bag bag;
            if (inRightHand == 1)
            {
                bag = rightHandState.heldItem as Bag;
            }
            else
            {
                bag = leftHandState.heldItem as Bag;
            }
            if (bag == null)
            {
                return;
            }
            bag.PlayOpenBagShapeKeyAnimation();
        }

        public void AnimEventPlaceBagInsideItem(int intoRightBag)
        {
            Bag bag;
            Item targetItem;
            if (intoRightBag == 1)
            {
                bag = rightHandState.heldItem as Bag;
                targetItem = leftHandState.heldItem;
            }
            else
            {
                bag = leftHandState.heldItem as Bag;
                targetItem = rightHandState.heldItem;
            }
            if (bag == null)
            {
                return;
            }
            bag.StoreItem(targetItem);
        }

        public void AnimEventTakeItemOutofBag(int fromRightBag)
        {
            Bag bag;
            if (fromRightBag == 1)
            {
                bag = rightHandState.heldItem as Bag;
            }
            else
            {
                bag = leftHandState.heldItem as Bag;
            }
            if (bag == null)
            {
                return;
            }
            bag.TakeOutNextItem();
        }

        public void OnGlobalAnimationChange(bool rightHand, Animation oldAnim, Animation newAnim)
        {
            switch (oldAnim)
            {
                case Animation.POINT:
                    break;
                case Animation.GESTURE_PRESENT_RIGHT:
                    objectFingerPointer.FireGesture(objectFingerPointer.rightGestureHand, ObjectFingerPointer.Gesture.PRESENT_END);
                    break;
                case Animation.GESTURE_PRESENT_LEFT:
                    objectFingerPointer.FireGesture(objectFingerPointer.leftGestureHand, ObjectFingerPointer.Gesture.PRESENT_END);
                    break;
            }
        }
    }

}