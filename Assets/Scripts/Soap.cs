using UnityEngine;


namespace viva
{


    public class Soap : Item
    {

        [SerializeField]
        private GameObject soapGeneratedBubblesPrefab;

        [SerializeField]
        private GameObject bubbleMeterPrefab;

        private Vector3 vrLastOtherHandAbsoluteLocalPos = Vector3.zero;
        private Vector3 vrLastMainHandAbsoluteLocalPos = Vector3.zero;
        private float vrTimeRubbed = 0.0f;

        public override void OnItemLateUpdatePostIK()
        {
            if (mainOwner == null)
            {
                return;
            }
            switch (mainOwner.characterType)
            {
                case Character.Type.PLAYER:
                    Player player = mainOwner as Player;
                    if (player.controls == Player.ControlType.KEYBOARD)
                    {
                        UpdatePlayerKeyboardSoapInteraction(player, (PlayerHandState)mainOccupyState);
                    }
                    else
                    {
                        UpdatePlayerVRSoapInteraction(player, (PlayerHandState)mainOccupyState);
                    }
                    break;
            }
        }

        public override void OnPostPickup()
        {
            string hintMessage;
            if (GameDirector.player.controls == Player.ControlType.KEYBOARD)
            {
                hintMessage = "Press MOUSE to make your hands soapy. Remember you can drop items afterward by holding SHIFT and then MOUSE";
            }
            else
            {
                hintMessage = "Physically rub your hands together to make your hands soapy to wash the loli's hair";
            }
            TutorialManager.main.DisplayHint(
                transform,
                Vector3.up * 0.25f,
                hintMessage,
                null,
                0.5f
            );
        }

        public override void OnPreDrop()
        {

            switch (mainOwner.characterType)
            {
                case Character.Type.PLAYER:
                    Player player = mainOwner as Player;
                    player.EndVRAnimatorBlend();
                    break;
            }
        }

        private void UpdatePlayerVRSoapInteraction(Player player, PlayerHandState mainHoldState)
        {

            PlayerHandState otherHoldState = mainHoldState == player.rightHandState
                ? player.leftHandState as PlayerHandState : player.rightHandState as PlayerHandState;
            if (otherHoldState.holdType != HoldType.NULL)
            {
                return;
            }

            Transform rightHandAbsolute = player.rightPlayerHandState.absoluteHandTransform;
            Transform leftHandAbsolute = player.leftPlayerHandState.absoluteHandTransform;
            //if hands are close together
            //pad minimum distance with vrTimeRubbed so it provides leeway while rubbing
            if (Vector3.SqrMagnitude(rightHandAbsolute.position - leftHandAbsolute.position) < 0.02f + vrTimeRubbed * 0.04f)
            {
                player.AttemptBeginVRAnimatorBlend(Player.Animation.VR_SOAP_GENERATE_BUBBLES, new Vector3(0.0f, -0.07f, 0.0f));
                //detect if hands are rubbing together
                Vector3 otherHandDelta = CalculateAbsoluteLocalDeltaPos(player, otherHoldState.absoluteHandTransform, ref vrLastOtherHandAbsoluteLocalPos);
                Vector3 mainHandDelta = CalculateAbsoluteLocalDeltaPos(player, mainHoldState.absoluteHandTransform, ref vrLastMainHandAbsoluteLocalPos);
                const float minSqVel = 0.00002f;
                float dot = Vector3.Dot(otherHandDelta, mainHandDelta);
                //if both hands are moving fast enough and in the opposite direction
                if (Vector3.SqrMagnitude(otherHandDelta) > minSqVel && Vector3.SqrMagnitude(mainHandDelta) > minSqVel && dot < 0.0f)
                {
                    vrTimeRubbed = Mathf.Min(vrTimeRubbed + Time.deltaTime * 10.0f, 1.5f);
                    if (vrTimeRubbed == 1.5f)
                    {
                        otherHoldState.GenerateBubbles(soapGeneratedBubblesPrefab, otherHoldState == player.rightHandState);
                        mainHoldState.GenerateBubbles(soapGeneratedBubblesPrefab, mainHoldState == player.rightHandState);
                    }
                }
                else
                {
                    vrTimeRubbed = Mathf.Clamp01(vrTimeRubbed - Time.deltaTime * 3.0f);
                }
                player.GetAnimator().SetFloat(WorldUtil.vrSoapRubID, Mathf.Clamp01(vrTimeRubbed * 1.5f));
            }
            else
            {
                player.EndVRAnimatorBlend();
                vrTimeRubbed = 0.0f;
            }
        }

        //calulate and store change in local position
        private Vector3 CalculateAbsoluteLocalDeltaPos(Player player, Transform absoluteTransform, ref Vector3 vrLastHandAbsoluteLocalPos)
        {

            Vector3 current = player.transform.InverseTransformPoint(absoluteTransform.position);
            Vector3 deltaPos = current - vrLastHandAbsoluteLocalPos;
            vrLastHandAbsoluteLocalPos = current;
            return deltaPos;
        }

        private void UpdatePlayerKeyboardSoapInteraction(Player player, PlayerHandState mainHoldState)
        {
            if (mainHoldState.animSys.currentAnim != mainHoldState.animSys.idleAnimation)
            {
                return;
            }
            if (mainHoldState.actionState.isDown)
            {

                //play generate bubbles animation if other hand is empty
                Player.Animation generateBubblesAnimation;
                bool otherHandIsEmpty;
                PlayerHandState otherHoldState;
                if (mainHoldState == player.rightHandState)
                {
                    otherHandIsEmpty = player.leftHandState.heldItem == null;
                    generateBubblesAnimation = Player.Animation.SOAP_GENERATE_BUBBLES_RIGHT;
                    otherHoldState = player.leftHandState as PlayerHandState;
                }
                else
                {
                    otherHandIsEmpty = player.rightHandState.heldItem == null;
                    generateBubblesAnimation = Player.Animation.SOAP_GENERATE_BUBBLES_LEFT;
                    otherHoldState = player.rightHandState as PlayerHandState;
                }
                if (otherHandIsEmpty)
                {
                    Player.HandAnimationSystem rightHandAnimSys = otherHoldState.animSys;
                    Player.HandAnimationSystem leftHandAnimSys = mainHoldState.animSys;
                    rightHandAnimSys.SetTargetAnimation(generateBubblesAnimation);
                    leftHandAnimSys.SetTargetAnimation(generateBubblesAnimation);
                    otherHoldState.GenerateBubbles(soapGeneratedBubblesPrefab, otherHoldState == player.rightHandState);
                    mainHoldState.GenerateBubbles(soapGeneratedBubblesPrefab, mainHoldState == player.rightHandState);
                }
            }
        }
    }

}