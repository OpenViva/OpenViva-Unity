using UnityEngine;


namespace viva
{

    public abstract class HorseControls : InputController
    {

        public HorseControls(Horse _horse) : base(_horse)
        {
            horse = _horse;
        }

        protected Horse horse;
        protected TransformBlend playerBlend = new TransformBlend();
        private float? smoothYaw = null;
        private float avgHorseSpine1Y = 0.0f;
        private static bool firstControlsHint = false;

        public override void OnEnter(Player player)
        {
            player.transform.SetParent(horse.spine1, true);

            playerBlend.SetTarget(true, player.transform, true, false, 0.0f, 1.0f, 1.0f);
            avgHorseSpine1Y = horse.spine1.position.y;
            if (!firstControlsHint)
            {
                firstControlsHint = true;
                player.pauseMenu.DisplayHUDMessage("You only need to grab one rein to control the horse.", true, PauseMenu.HintType.HINT_NO_IMAGE);
                if (player.controls == Player.ControlType.VR)
                {
                    player.pauseMenu.DisplayHUDMessage("Press the trackpad's up/down to shift horse speed", true, PauseMenu.HintType.HINT_NO_IMAGE);
                }
                else
                {
                    player.pauseMenu.DisplayHUDMessage("Press W and S to shift horse speed", true, PauseMenu.HintType.HINT_NO_IMAGE);
                }
            }
        }

        public override void OnExit(Player player)
        {
            player.transform.SetParent(null, true);
        }

        private float UpdateSmoothSpine1Y()
        {
            Vector3 spine1Pos = horse.spine1.position;
            avgHorseSpine1Y += (spine1Pos.y - avgHorseSpine1Y) * Time.deltaTime * 4.0f;
            avgHorseSpine1Y = Mathf.Clamp(avgHorseSpine1Y, spine1Pos.y - horse.playerMaxMountYoffset, spine1Pos.y + horse.playerMaxMountYoffset);
            return avgHorseSpine1Y;
        }


        protected void BlendPlayerTransform(Vector3 mountOffset, float yawOffset = -180.0f, float smoothYawSpeed = 2.0f)
        {

            Vector3 horseSpinePos = new Vector3(horse.spine1.position.x, UpdateSmoothSpine1Y(), horse.spine1.position.z);
            Vector3 mountLocalPos = horse.spine1.InverseTransformPoint(horseSpinePos) + mountOffset;

            float yaw = Mathf.Atan2(-horse.spine1.forward.z, horse.spine1.forward.x) * Mathf.Rad2Deg;
            if (!smoothYaw.HasValue)
            {
                smoothYaw = yaw;
            }
            else
            {
                smoothYaw += Mathf.DeltaAngle(smoothYaw.Value, yaw) * Time.deltaTime * smoothYawSpeed;
            }

            Quaternion mountRot = Quaternion.Euler(0.0f, smoothYaw.Value + yawOffset, 0.0f);
            playerBlend.Blend(mountLocalPos, mountRot);
        }
    }

}