using System.Collections;
using UnityEngine;


namespace viva
{


    public partial class Player : Character
    {


        public float keyboardCurrentHeight = 1.4f;
        public float keyboardTargetHeight = 1.4f;
        public float keyboardStandingHeight = 1.4f;
        public float keyboardFloorHeight = 0.5f;
        private float keyboardMaxHeightOverride = 1.4f;
        private float enableMouseRotationMult = 1.0f;
        private Coroutine halfCrouchCoroutine = null;


        private void SetEnableKeyboardControls(bool enable)
        {
            if (enable)
            {
                vivaControls.keyboard.Enable();
                crosshair.SetActive(true);
                head.localPosition = Vector3.up * keyboardCurrentHeight;
                GameDirector.instance.mainCamera.stereoTargetEye = StereoTargetEyeMask.None;
                GameDirector.instance.mainCamera.fieldOfView = 65.0f;
            }
            else
            {
                vivaControls.keyboard.Disable();
            }
            keyboardHelperItemDetector.SetActive(enable);
        }

        public float GetKeyboardCurrentHeight()
        {
            return keyboardCurrentHeight;
        }

        private void BeginKeyboardHalfCrouch(SphereCollider sc)
        {
            if (halfCrouchCoroutine != null)
            {
                return;
            }
            halfCrouchCoroutine = GameDirector.instance.StartCoroutine(PersistHalfCrouch(sc));
        }

        private void StopKeyboardHalfCrouch()
        {
            if (halfCrouchCoroutine == null)
            {
                return;
            }
            GameDirector.instance.StopCoroutine(halfCrouchCoroutine);
            keyboardTargetHeight = keyboardStandingHeight;
            halfCrouchCoroutine = null;
        }

        private IEnumerator PersistHalfCrouch(SphereCollider sc)
        {

            while (true)
            {
                float dist = Vector3.Distance(sc.transform.TransformPoint(sc.center), transform.position);
                float blend = 1.0f - dist / sc.radius;
                keyboardMaxHeightOverride = Mathf.Lerp(keyboardStandingHeight, 0.5f, blend);
                yield return new WaitForFixedUpdate();
            }
        }

        public void ApplyHeadTransformToArmature()
        {
            armature.position = head.position + head.forward * 0.5f;
            armature.rotation = head.rotation * Quaternion.Euler(-90.0f, 0.0f, 0.0f);
        }
        public void UpdateGUIKeyboardShortcuts()
        {
            // if( InputOLD.GetKeyDown(KeyCode.Tab) ){
            //     if( InputOLD.GetKey(KeyCode.LeftShift) ){
            //         pauseMenu.cycleButton(-1);
            //     }else{
            //         pauseMenu.cycleButton(1);
            //     }
            // }else if( InputOLD.GetKeyDown(KeyCode.Space) ){
            //     pauseMenu.clickCurrentCycledButton();
            // }
        }

        public Vector2 CalculateMouseMovement()
        {
            return new Vector2(-mouseVelocity.y, mouseVelocity.x) * GameDirector.settings.mouseSensitivity * Time.deltaTime * 0.01f;
        }

        public void SetKeyboardMouseRotationMult(float mult)
        {
            enableMouseRotationMult = mult;
        }

        public void OnInputTogglePresentHand(PlayerHandState handState)
        {
            if (handState.animSys.currentAnim == handState.animSys.idleAnimation)
            {
                if (handState.rightSide)
                {
                    handState.animSys.SetTargetAnimation(Animation.GESTURE_PRESENT_RIGHT);
                }
                else
                {
                    handState.animSys.SetTargetAnimation(Animation.GESTURE_PRESENT_LEFT);
                }
            }
            else
            {
                handState.animSys.SetTargetAnimation(handState.animSys.idleAnimation);
            }
        }

        private void OnInputFollowRightHand()
        {
            if (rightPlayerHandState.animSys.currentAnim == rightPlayerHandState.animSys.idleAnimation)
            {
                rightPlayerHandState.animSys.SetTargetAnimation(Animation.GESTURE_COME);
            }
        }

        private void OnInputWaveRightHand()
        {
            if (rightPlayerHandState.animSys.currentAnim == rightPlayerHandState.animSys.idleAnimation)
            {
                rightPlayerHandState.animSys.SetTargetAnimation(Animation.GESTURE_WAVE);
            }
        }

        private void FlipKeyboardHeight()
        {
            if (keyboardTargetHeight == keyboardFloorHeight)
            {
                keyboardTargetHeight = keyboardStandingHeight;
            }
            else
            {
                keyboardTargetHeight = keyboardFloorHeight;
            }
        }

        public void UpdateInputKeyboardCrouching()
        {
            keyboardCurrentHeight += (keyboardTargetHeight - keyboardCurrentHeight) * Time.deltaTime * 5.0f;
            keyboardCurrentHeight = Mathf.Min(keyboardCurrentHeight, keyboardMaxHeightOverride);
            head.localPosition = Vector3.up * keyboardCurrentHeight;
        }

        public void UpdateInputKeyboardRotateHead()
        {
            mouseVelocitySum += CalculateMouseMovement() * enableMouseRotationMult;

            //Fix head camera roll
            head.transform.rotation = Quaternion.LookRotation(head.forward, Vector3.up);
            head.transform.rotation = Quaternion.RotateTowards(Quaternion.LookRotation(new Vector3(head.forward.x, 0.0f, head.forward.z)), head.transform.rotation, 75.0f);
        }
    }

}