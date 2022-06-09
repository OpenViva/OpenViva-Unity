using UnityEngine;


namespace viva
{

    public class HorseVRControls : HorseControls
    {

        private float rootYawOffset = 0.0f;
        private bool pressToTurnResetY = true;

        public HorseVRControls(Horse _horse) : base(_horse)
        {
        }

        public override void OnEnter(Player player)
        {
            base.OnEnter(player);
            float playerHeadRadian = Mathf.Atan2(-player.head.forward.z, player.head.forward.x);
            rootYawOffset = (Mathf.Atan2(-player.transform.forward.z, player.transform.forward.x) - playerHeadRadian) * Mathf.Rad2Deg + 180.0f;

            playerBlend.blend.reset(1.0f);
        }

        public override void OnExit(Player player)
        {
            base.OnExit(player);
            player.transform.SetParent(null, true);
        }

        public override void OnFixedUpdateControl(Player player)
        {

            player.rightPlayerHandState.UpdateSteamVRInput();
            player.leftPlayerHandState.UpdateSteamVRInput();

            if (GameDirector.instance.controlsAllowed != GameDirector.ControlsAllowed.ALL || GameDirector.settings.vrControls != Player.VRControlType.TRACKPAD)
            {
                return;
            }
            player.UpdateVRTrackpadMovement();
            player.FixedUpdatePlayerCapsule(player.head.localPosition.y);
            WrangleHorse(player);

            player.ApplyVRHandsToAnimation();
        }

        public override void OnLateUpdateControl(Player player)
        {
            BlendPlayerTransform(horse.vrPlayerMountOffset, rootYawOffset, 0.5f);
        }

        private void WrangleHorse(Player player)
        {

            Vector2 wrangleDirection = Vector2.zero;
            wrangleDirection = UpdateReinWrangleDirection(wrangleDirection, player, player.rightHandState);
            wrangleDirection = UpdateReinWrangleDirection(wrangleDirection, player, player.leftHandState);

            if (wrangleDirection.y >= 0.7f)
            {
                horse.ShiftSpeed(1);
            }
            else if (wrangleDirection.y <= -0.7f)
            {
                horse.ShiftSpeed(-2, -1);
            }
            if (Mathf.Abs(wrangleDirection.x) >= 0.5f)
            {
                horse.targetSide = Mathf.Sign(wrangleDirection.x);
            }
            else
            {
                horse.targetSide = 0;
            }
        }

        private Vector2 UpdateReinWrangleDirection(Vector2 currentDir, Player player, HandState handState)
        {
            if (handState.heldItem && handState.heldItem.settings.itemType == Item.Type.REINS)
            {

                var targetHand = handState.rightSide ? player.rightPlayerHandState : player.leftPlayerHandState;
                if (GameDirector.settings.pressToTurn)
                {
                    if (targetHand.trackpadButtonState.isDown)
                    {
                        currentDir.y = targetHand.trackpadPos.y;
                    }
                    if (targetHand.trackpadButtonState.isHeldDown)
                    {
                        currentDir.x = targetHand.trackpadPos.x;
                    }
                }
                else
                {
                    if (Mathf.Abs(targetHand.trackpadPos.y) > 0.5f)
                    {
                        if (pressToTurnResetY)
                        {
                            pressToTurnResetY = false;
                            currentDir.y = targetHand.trackpadPos.y;
                        }
                    }
                    else
                    {
                        pressToTurnResetY = true;
                    }
                    if (Mathf.Abs(targetHand.trackpadPos.x) > 0.5f)
                    {
                        currentDir.x = targetHand.trackpadPos.x;
                    }
                }
            }
            return currentDir;
        }
    }

}