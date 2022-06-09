using System.Collections;
using UnityEngine;
using UnityEngine.XR.Management;


namespace viva
{


    public partial class Player : Character
    {
        public enum VRControlType
        {
            TRACKPAD,
            TELEPORT
        }
        

        private bool pressToTurnReset = true;
        private Vector3 lastTouchpadAccel = Vector3.zero;
        private Vector2 lastTouchpadPos = Vector2.zero;
        [Header("VR")]
        [SerializeField]
        private GameObject teleportRing;
        [SerializeField]
        private Material teleportMat;
        [SerializeField]
        private Texture2D validTeleportTex;
        [SerializeField]
        private Texture2D invalidTeleportTex;

        private const int teleportMaxQuads = 20;
        [SerializeField]
        private MeshFilter teleportMF;
        [SerializeField]
        private MeshRenderer teleportMR;
        private Mesh teleportMesh;
        private Vector3[] teleportVertices = new Vector3[teleportMaxQuads * 2 + 2];
        private Vector2[] teleportUV = new Vector2[teleportMaxQuads * 2 + 2];
        private int[] teleportIndices;
        private RaycastHit teleportTest = new RaycastHit();
        private int teleportTestSide = 0;
        private Coroutine blackFadeCoroutine = null;
        private bool teleportLocationValid = false;
        private bool disableVRTransforms = false;
        [SerializeField]
        private Vector3 m_absoluteVRPositionOffset;
        [VivaFileAttribute]
        public Vector3 absoluteVRPositionOffset { get { return m_absoluteVRPositionOffset; } protected set { m_absoluteVRPositionOffset = value; } }
        [SerializeField]
        private Vector3 m_absoluteVRRotationEulerOffset;
        [VivaFileAttribute]
        public Vector3 absoluteVRRotationEulerOffset { get { return m_absoluteVRRotationEulerOffset; } protected set { m_absoluteVRRotationEulerOffset = value; } }

        private void SetEnableVRControls(bool enable)
        {
            if (enable)
            {
                if (XRGeneralSettings.Instance.Manager.activeLoader == null)
                {
                    Debug.Log("#VR Enabled " + (XRGeneralSettings.Instance.Manager.activeLoader == null));
                    XRGeneralSettings.Instance.Manager.InitializeLoaderSync();

                    if (XRGeneralSettings.Instance.Manager.activeLoader == null)
                    {
                        Debug.LogError("Failed to initialize VR");
                        return;
                    }
                    else
                    {
                        XRGeneralSettings.Instance.Manager.StartSubsystems();
                    }
                }
                head.localPosition = Vector3.zero;
                head.localRotation = Quaternion.identity;
                // SteamVR.enabled = true;
                // rightPlayerHandState.StartDeprecatedXRInput();
                // leftPlayerHandState.StartDeprecatedXRInput();

                crosshair.SetActive(false);
                InitVRTeleportVariables();
                GameDirector.instance.mainCamera.stereoTargetEye = StereoTargetEyeMask.Both;
            }
            else
            {
                if (XRGeneralSettings.Instance.Manager.isInitializationComplete)
                {
                    Debug.LogError("#VR Disabled " + XRGeneralSettings.Instance.Manager.isInitializationComplete);
                    XRGeneralSettings.Instance.Manager.StopSubsystems();
                    XRGeneralSettings.Instance.Manager.DeinitializeLoader();
                }
                // SteamVR.enabled = false;
                // rightPlayerHandState.UnbindSteamVRInput();
                // leftPlayerHandState.UnbindSteamVRInput();
            }
            rightPlayerHandState.behaviourPose.enabled = enable;
            leftPlayerHandState.behaviourPose.enabled = enable;
            playerHeadState.behaviourPose.enabled = enable;
        }

        public void SetFreezeVRHandTransforms(bool freeze)
        {
            disableVRTransforms = freeze;

            if (freeze)
            {
                rightPlayerHandState.CacheRawAnimationTransform();
                leftPlayerHandState.CacheRawAnimationTransform();
            }
        }

        private void InitVRTeleportVariables()
        {
            teleportMesh = new Mesh();
            teleportMF.sharedMesh = teleportMesh;

            Vector2 uv = Vector2.zero;
            teleportUV[0] = uv;
            uv.x = 1.0f;
            teleportUV[1] = uv;
            for (int quadIndex = 0; quadIndex < teleportMaxQuads; quadIndex++)
            {
                uv = Vector2.zero;
                teleportUV[quadIndex * 2 + 2] = uv;
                uv.x = 1.0f;
                teleportUV[quadIndex * 2 + 3] = uv;
            }
        }

        [SerializeField]
        public Vector3 debugVar = Vector3.zero;

        public void ApplyVRHandsToAnimation()
        {

            if (disableVRTransforms)
            {
                rightPlayerHandState.ApplyCachedRawTransform();
                leftPlayerHandState.ApplyCachedRawTransform();
                return;
            }

            //cache raw animation local position and local rotation
            rightPlayerHandState.CacheRawAnimationTransform();
            leftPlayerHandState.CacheRawAnimationTransform();

            Transform rightWrist = rightHandState.fingerAnimator.hand.parent;
            Transform leftWrist = leftHandState.fingerAnimator.hand.parent;
            Transform rightHandAbsoluteTransform = rightPlayerHandState.absoluteHandTransform;
            Transform leftHandAbsoluteTransform = leftPlayerHandState.absoluteHandTransform;

            if (vrAnimatorBlend.value > 0.0f)
            {

                Transform root = rightWrist.parent;
                Vector3 midpoint = (rightHandAbsoluteTransform.position + leftHandAbsoluteTransform.position) * 0.5f;
                Vector3 midDir = (rightHandAbsoluteTransform.up + leftHandAbsoluteTransform.up).normalized;
                Quaternion midQuat = Quaternion.LookRotation(midDir, (rightHandAbsoluteTransform.right).normalized);
                midQuat *= Quaternion.Euler(-90.0f, 0.0f, 0.0f);    //rotate from animation base to hands local

                root.rotation = midQuat;
                root.position = midpoint + root.TransformDirection(vrAnimationPosOffset);
                // root.position = midpoint+root.TransformDirection( debugVar );

                Vector3 animRightPos = rightWrist.position;
                Quaternion animRightRot = rightWrist.rotation;
                Vector3 animLeftPos = leftWrist.position;
                Quaternion animLeftRot = leftWrist.rotation;

                rightWrist.rotation = rightHandAbsoluteTransform.rotation;
                rightWrist.position = rightHandAbsoluteTransform.position;
                leftWrist.rotation = leftHandAbsoluteTransform.rotation;
                leftWrist.position = leftHandAbsoluteTransform.position;

                float blendAmount = vrAnimatorBlend.value * (1.0f - vrAnimatorBlendAllowHands.value);

                rightWrist.position = Vector3.LerpUnclamped(
                    rightWrist.position,
                    animRightPos,
                    blendAmount
                );
                rightWrist.rotation = Quaternion.LerpUnclamped(
                    rightWrist.rotation,
                    animRightRot,
                    blendAmount
                );
                leftWrist.position = Vector3.LerpUnclamped(
                    leftWrist.position,
                    animLeftPos,
                    blendAmount
                );
                leftWrist.rotation = Quaternion.LerpUnclamped(
                    leftWrist.rotation,
                    animLeftRot,
                    blendAmount
                );

            }
            else
            {
                rightWrist.rotation = rightHandAbsoluteTransform.rotation;
                rightWrist.position = rightHandAbsoluteTransform.position;
                leftWrist.rotation = leftHandAbsoluteTransform.rotation;
                leftWrist.position = leftHandAbsoluteTransform.position;
            }
        }

        public void UpdateTrackpadBodyRotation()
        {
            bool turn = false;
            var targetHand = !GameDirector.settings.trackpadMovementUseRight ? rightPlayerHandState : leftPlayerHandState;
            if (GameDirector.settings.pressToTurn)
            {
                if (targetHand.trackpadButtonState.isDown)
                {
                    if (pressToTurnReset)
                    {
                        turn = true;
                    }
                    pressToTurnReset = false;
                }
                else
                {
                    pressToTurnReset = true;
                }
            }
            else
            {  //require only touch
                if (Mathf.Abs(targetHand.trackpadPos.x) < 0.5f)
                {
                    pressToTurnReset = true;
                }
                else if (pressToTurnReset)
                {
                    turn = true;
                    pressToTurnReset = false;
                }
            }
            if (turn)
            {
                float dir;
                if (targetHand.trackpadPos.x > 0.0f)
                {
                    dir = 1.0f;
                }
                else
                {
                    dir = -1.0f;
                }
                transform.RotateAround(floorPos, Vector3.up, 45.0f * dir);
            }
        }

        public void LateUpdateVRInputTeleportationMovement()
        {
            if (GameDirector.instance.controlsAllowed == GameDirector.ControlsAllowed.NONE || GameDirector.settings.vrControls != Player.VRControlType.TELEPORT)
            {
                return;
            }
            var targetHand = GameDirector.settings.trackpadMovementUseRight ? rightPlayerHandState : leftPlayerHandState;
            if (teleportTestSide == 0)
            {

                if (targetHand.trackpadButtonState.isDown && blackFadeCoroutine == null)
                {
                    if (GameDirector.settings.trackpadMovementUseRight)
                    {
                        teleportTestSide = 1;
                    }
                    else
                    {
                        teleportTestSide = -1;
                    }
                    BeginVRTeleportationSampling();
                }
            }
            if (teleportTestSide != 0)
            {
                if (targetHand.trackpadButtonState.isUp)
                {
                    TeleportToLastTestedPosition();
                }
                else
                {
                    if (GameDirector.settings.trackpadMovementUseRight)
                    {
                        SampleVRTeleportationLocation(rightHandState.fingerAnimator.hand);
                    }
                    else
                    {
                        SampleVRTeleportationLocation(leftHandState.fingerAnimator.hand);
                    }
                }
            }
        }

        private void BeginVRTeleportationSampling()
        {
            teleportLocationValid = false;
            teleportMR.enabled = true;
        }

        private void TeleportToLastTestedPosition()
        {

            if (teleportLocationValid)
            {

                if (blackFadeCoroutine != null)
                {
                    GameDirector.instance.StopCoroutine(blackFadeCoroutine);
                }
                blackFadeCoroutine = GameDirector.instance.StartCoroutine(FadeVRTeleport());
            }
            else
            {
                teleportTestSide = 0;
            }
            teleportRing.SetActive(false);
            rigidBody.velocity = Vector3.zero;
            rigidBody.angularVelocity = Vector3.zero;
            teleportMR.enabled = false;
        }

        private IEnumerator FadeVRTeleport()
        {
            pauseMenu.fadePanel.SetActive(true);
            Material fadeMaterial = pauseMenu.fadePanel.GetComponent<MeshRenderer>().material;

            float timer = 0.0f;
            while (timer < 0.4f)
            {
                timer += Time.deltaTime;
                fadeMaterial.SetFloat("_Alpha", timer / 0.4f);
                yield return null;
            }

            Vector3 headFloorPos = base.head.localPosition;
            headFloorPos.y = 0.0f;

            teleportTestSide = 0;
            transform.position = teleportTest.point - Vector3.up * 0.05f - transform.rotation * headFloorPos;
            teleportRing.SetActive(false);

            while (timer > 0.0f)
            {
                timer -= Time.deltaTime;
                fadeMaterial.SetFloat("_Alpha", timer / 0.4f);
                yield return null;
            }
            pauseMenu.fadePanel.SetActive(false);
            blackFadeCoroutine = null;
        }

        private void SampleVRTeleportationLocation(Transform source)
        {

            //curve physics ray trace
            const float raySubLength = 0.45f;
            Vector3 rayPos = source.position + source.up * 0.1f;
            Vector3 rayNorm = source.up * raySubLength;

            Vector3 outDir = Vector3.Cross((rayPos - head.position).normalized, source.up) * 0.02f;

            teleportVertices[0] = transform.InverseTransformPoint(rayPos + outDir);
            teleportVertices[1] = transform.InverseTransformPoint(rayPos - outDir);

            teleportTest.normal = Vector3.right; //fail teleport to
            teleportLocationValid = false;
            teleportRing.SetActive(false);

            int quadCount = 1;
            for (; quadCount < teleportMaxQuads; quadCount++)
            {

                if (Physics.Raycast(rayPos, rayNorm, out teleportTest, rayNorm.magnitude, Instance.wallsMask))
                {

                    teleportVertices[quadCount * 2] = transform.InverseTransformPoint(teleportTest.point + outDir);
                    teleportVertices[quadCount * 2 + 1] = transform.InverseTransformPoint(teleportTest.point - outDir);

                    if (Vector3.Distance(teleportTest.normal, Vector3.up) < 0.4f)
                    {
                        teleportRing.SetActive(true);
                        teleportRing.transform.position = teleportTest.point;
                        teleportRing.transform.rotation = Quaternion.LookRotation(Vector3.up, (teleportTest.point - head.position).normalized) * Quaternion.Euler(0.0f, 0.0f, 90.0f);

                        if (!Physics.CheckCapsule(teleportTest.point + Vector3.up * (characterCC.radius + 0.15f), teleportTest.point + Vector3.up * 1.35f, characterCC.radius, Instance.wallsMask))
                        {
                            teleportLocationValid = true;
                        }
                        Debug.DrawLine(teleportTest.point + Vector3.up * (characterCC.radius + 0.1f), teleportTest.point + Vector3.up * 1.35f, Color.red, 5.0f);
                    }
                    break;
                }

                rayPos += rayNorm;
                rayNorm -= Vector3.up * 0.08f;

                teleportVertices[quadCount * 2] = transform.InverseTransformPoint(rayPos + outDir);
                teleportVertices[quadCount * 2 + 1] = transform.InverseTransformPoint(rayPos - outDir);
            }

            teleportIndices = new int[quadCount * 2 * 3];
            for (int quadIndex = 0; quadIndex < quadCount; quadIndex++)
            {
                teleportIndices[quadIndex * 6] = quadIndex * 2;
                teleportIndices[quadIndex * 6 + 1] = quadIndex * 2 + 1;
                teleportIndices[quadIndex * 6 + 2] = quadIndex * 2 + 2;

                teleportIndices[quadIndex * 6 + 3] = quadIndex * 2 + 1;
                teleportIndices[quadIndex * 6 + 4] = quadIndex * 2 + 2;
                teleportIndices[quadIndex * 6 + 5] = quadIndex * 2 + 3;
            }
            teleportMesh.vertices = teleportVertices;
            teleportMesh.SetIndices(teleportIndices, MeshTopology.Triangles, 0);
            teleportMesh.uv = teleportUV;
            teleportMesh.RecalculateBounds();

            if (teleportLocationValid)
            {
                teleportMat.mainTexture = validTeleportTex;
            }
            else
            {
                teleportMat.mainTexture = invalidTeleportTex;
            }
        }

        public void UpdateVRTrackpadMovement()
        {
            var targetHand = GameDirector.settings.trackpadMovementUseRight ? rightPlayerHandState : leftPlayerHandState;
            Vector2 touchpadPos = targetHand.trackpadPos;
            touchpadPos.y = -touchpadPos.y;

            if (Vector2.SqrMagnitude(touchpadPos - lastTouchpadPos) > 0.02f)
            {
                lastTouchpadPos = touchpadPos;

                lastTouchpadAccel = base.head.transform.right * touchpadPos.x;
                lastTouchpadAccel -= base.head.transform.forward * touchpadPos.y;
                lastTouchpadAccel = lastTouchpadAccel.normalized;
            }

            float touchpadAccel = Mathf.Clamp01(touchpadPos.magnitude);
            touchpadAccel *= touchpadAccel;
            if (targetHand.trackpadButtonState.isHeldDown)
            {
                touchpadAccel *= 2.8f;
            }
            moveVel += lastTouchpadAccel * touchpadAccel * walkSpeed;
            moveVel *= 0.85f;
            moveVel.y = rigidBody.velocity.y;
            rigidBody.velocity = moveVel;
        }
    }

}