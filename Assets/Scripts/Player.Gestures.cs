namespace viva
{


    public partial class Player : Character
    {


        private void LateUpdatePostIKGestures()
        {

            if (controls == ControlType.KEYBOARD)
            {
                return; //GESTURES NOT ALLOWED FOR KEYBOARD MODE DUE TO LOWER PRECISION FROM LOWER FRAMERATE
            }
            CheckForGestures(rightPlayerHandState, objectFingerPointer.rightGestureHand);
            CheckForGestures(leftPlayerHandState, objectFingerPointer.leftGestureHand);
        }

        private void CheckForGestures(PlayerHandState handState, ObjectFingerPointer.GestureHand gestureHand)
        {
            if (handState.gripState.isDown)
            {
                gestureHand.ResetAll();
            }
            else
            {
                objectFingerPointer.UpdateGestureDetection(gestureHand, head);
            }
        }
    }

}