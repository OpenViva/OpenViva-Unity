using RootMotion.Dynamics;
using System.Collections;
using UnityEngine;


namespace viva
{


    public partial class Loli : Character
    {

        public delegate void OnRagdollCallback();

        [Header("Balance")]
        [Range(0.01f, 0.3f)]
        [SerializeField]
        public float balanceRadius = 0.1f;
        [Range(0.0f, 12.0f)]
        [SerializeField]
        public float standingForce = 10.0f;
        [Range(0.01f, 0.5f)]
        [SerializeField]
        public float maxStandingForce = 0.1f;

        private int balanceCheckCount = 15;
        private const int minUnbalanceChecks = 20;
        private float regainBalanceTimeout = 1.0f;
        private Coroutine regainBalanceCoroutine = null;
        public bool hasBalance { get; protected set; } = true;
        public bool isRegainingBalance { get { return getUpAnimation.HasValue; } }
        private Animation? getUpAnimation = null;
        private float floorHitAnimSetTime = 0.0f;
        private bool checkBalance = true;
        private bool checkBalanceDuringTransition = true;
        private int worldMask = 0;
        private Vector3 lastAnimatedSpineForward;
        public Rigidbody rightFootRigidBody { get { return puppetMaster.muscles[15].rigidbody; } }
        public Rigidbody leftFootRigidBody { get { return puppetMaster.muscles[12].rigidbody; } }
        public Rigidbody rightLegRigidBody { get { return puppetMaster.muscles[11].rigidbody; } }
        public Rigidbody leftLegRigidBody { get { return puppetMaster.muscles[14].rigidbody; } }
        public Rigidbody spine1RigidBody { get { return puppetMaster.muscles[0].rigidbody; } }
        public Rigidbody spine2RigidBody { get { return puppetMaster.muscles[1].rigidbody; } }
        public Rigidbody spine3RigidBody { get { return puppetMaster.muscles[2].rigidbody; } }
        public Rigidbody headRigidBody { get { return puppetMaster.muscles[3].rigidbody; } }
        public Muscle spine1Muscle { get { return puppetMaster.muscles[0]; } }
        public Muscle spine2Muscle { get { return puppetMaster.muscles[1]; } }
        public Muscle spine3Muscle { get { return puppetMaster.muscles[2]; } }
        public Muscle headMuscle { get { return puppetMaster.muscles[3]; } }
        public Muscle leftArmMuscle { get { return puppetMaster.muscles[4]; } }
        public Muscle leftForearmMuscle { get { return puppetMaster.muscles[5]; } }
        public Muscle leftHandMuscle { get { return puppetMaster.muscles[6]; } }
        public Muscle rightArmMuscle { get { return puppetMaster.muscles[7]; } }
        public Muscle rightForearmMuscle { get { return puppetMaster.muscles[8]; } }
        public Muscle rightHandMuscle { get { return puppetMaster.muscles[9]; } }
        private int standUpFrameWait = 0;
        public OnRagdollCallback onRagdollModeBegin = null;
        public float? groundHeight { get; private set; } = null;
        private int disableFallingCount = 0;
        private int activeConstraints = 0;
        public bool isConstrained { get { return activeConstraints > 0; } }
        private bool enableStandOnGround = false;

        private delegate IEnumerator BalanceCoroutine();


        public void AddOnRagdollModeBeginCallback(OnRagdollCallback callback)
        {
            onRagdollModeBegin -= callback;
            onRagdollModeBegin += callback;
        }

        public void RemoveOnRagdollModeBeginCallback(OnRagdollCallback callback)
        {
            onRagdollModeBegin -= callback;
        }

        public void RegisterConstraint()
        {
            activeConstraints++;
        }
        public void UnregisterConstraint()
        {
            activeConstraints--;

            //on stopped grabbed
            if (activeConstraints == 0 && !hasBalance && !isRegainingBalance)
            {
                if (groundHeight.HasValue)
                {
                    BeginRagdollMode(0.5f, Animation.FLOOR_CURL_LOOP);
                }
                else
                {
                    BeginRagdollMode(0.5f, Animation.FALLING_LOOP);
                }
            }
        }

        public void ApplyDisableBalanceLogic(ref bool source)
        {
            if (!source)
            {
                source = true;
                disableFallingCount++;
            }
        }
        public void RemoveDisableBalanceLogic(ref bool source)
        {
            if (source)
            {
                source = false;
                disableFallingCount--;
            }
        }

        private Vector3 CalculateCurrentFloorPosition()
        {

            Vector3 p = leftFootRigidBody.position + rightFootRigidBody.position;
            p.x += anchor.position.x;
            p.z += anchor.position.z;
            return new Vector3(p.x / 3, p.y / 2, p.z / 3);
        }

        private void UpdateRootTransform()
        {
            if (previewMode || anchorActive)
            {
                return;
            }
            anchor.position = spine1RigidBody.transform.position - (spine1.position - anchor.position);
        }

        private bool ShouldCheckBalance()
        {
            if (previewMode || disableFallingCount > 0)
            {
                return false;
            }
            if (animator.IsInTransition(0))
            {
                return checkBalanceDuringTransition;
            }
            else
            {
                return checkBalance;
            }
        }

        private void StandOnGround(float? groundHeight)
        {

            if (groundHeight.HasValue)
            {
                var spine1Muscle = puppetMaster.muscles[0];
                float localAnimatedSpineY = spine1Muscle.targetAnimatedY - bodyArmature.position.y;
                float localActualSpineY = spine1Muscle.rigidbody.worldCenterOfMass.y - (groundHeight.Value);
                float heightDiff = localAnimatedSpineY - localActualSpineY;

                float movingStrength = new Vector2(spine1RigidBody.velocity.x, spine1RigidBody.velocity.z).magnitude;
                float finalStandingForce = Mathf.Min(standingForce + movingStrength * 4.0f, 12.0f);

                heightDiff = Mathf.Clamp(heightDiff, 0.0f, maxStandingForce);
                float standForce = heightDiff * finalStandingForce;
                Vector3 finalForce = new Vector3(0, standForce, 0);
                finalForce.x -= spine1Muscle.rigidbody.velocity.x * puppetMaster.pinWeight;
                finalForce.z -= spine1Muscle.rigidbody.velocity.z * puppetMaster.pinWeight;
                finalForce *= spine1Muscle.props.pinWeight * puppetMaster.pinWeight;
                spine1Muscle.rigidbody.AddForce(
                    finalForce,
                    ForceMode.VelocityChange
                );
            }
        }

        private void RecalculateGroundHeight()
        {
            float? oldGroundHeight = groundHeight;
            Vector3 randomRadius = UnityEngine.Random.insideUnitSphere * balanceRadius;
            randomRadius.y = 0.0f;
            Vector3 rayCenter = spine1RigidBody.position + randomRadius;

            Vector3 spineVel = spine1RigidBody.velocity;
            float balanceRayDistance = spine1.localPosition.z + 0.2f + Mathf.Min(0.4f, (spineVel.x * spineVel.x + spineVel.z * spineVel.z) * 0.07f);    //add padding below feet
            if (GamePhysics.GetRaycastInfo(rayCenter, Vector3.down, balanceRayDistance, worldMask, QueryTriggerInteraction.Ignore, 0.125f))
            {
                groundHeight = GamePhysics.result().point.y;
                if (oldGroundHeight.HasValue)
                {
                    groundHeight = (oldGroundHeight.Value + groundHeight) * 0.5f;
                }
                Debug.DrawLine(rayCenter, rayCenter + Vector3.down * balanceRayDistance, Color.green, 0.025f);
            }
            else
            {
                groundHeight = null;
                Debug.DrawLine(rayCenter, rayCenter + Vector3.down * balanceRayDistance, Color.red, 0.025f);
            }
        }

        public void FixedUpdateBalanceCheck()
        {
            if (!ShouldCheckBalance())
            {
                return;
            }
            Vector3 randomRadius = UnityEngine.Random.insideUnitSphere * balanceRadius;
            randomRadius.y = 0.0f;
            if (groundHeight.HasValue)
            {
                if (!hasBalance)
                {
                    if (!isRegainingBalance)
                    {
                        CheckCanStandUp();
                    }
                    if (standUpFrameWait > 0)
                    {
                        if (standUpFrameWait++ == 2)
                        {
                            PlayBalanceCoroutine(RegainBalance);
                        }
                    }
                }
            }
            else
            {
                if (--balanceCheckCount == 0)
                {
                    LoseBalance();
                }
            }
            // if( InputOLD.GetKeyDown(KeyCode.F1)){
            //     BeginRagdollMode( 0.65f, Animation.FALLING_LOOP );
            // }
        }

        private bool AllowedToStandUp()
        {
            if (isConstrained)
            {
                return false;
            }
            bool bodyStoppedMoving = spine1RigidBody.velocity.sqrMagnitude < 0.05f;
            int changeSign = System.Convert.ToInt32(bodyStoppedMoving) * 2 - 1;
            balanceCheckCount = Mathf.Clamp(balanceCheckCount + changeSign, 1, minUnbalanceChecks);
            return bodyStoppedMoving;
        }

        private void CheckCanStandUp()
        {

            if (AllowedToStandUp())
            {
                if (balanceCheckCount == minUnbalanceChecks)
                {
                    regainBalanceTimeout -= Time.deltaTime;
                    if (regainBalanceTimeout <= 0.0f && !getUpAnimation.HasValue)
                    {
                        BeginStandingUp();
                    }
                }
                else if (Time.time - floorHitAnimSetTime > 0.5f)
                {
                    floorHitAnimSetTime = Time.time;
                    if (Mathf.Abs(spine1.forward.y) > 0.7f)
                    {
                        SetTargetAnimation(Animation.FLOOR_FACE_UP_IDLE);
                    }
                    else
                    {
                        SetTargetAnimation(Animation.FLOOR_CURL_LOOP);
                    }
                }
            }
        }

        private void BeginStandingUp()
        {
            getUpAnimation = CalculateGetUpAnimation();
            OverrideBodyState(BodyState.OFFBALANCE);
            OverrideClearAnimationPriority();
            SetTargetAnimation(getUpAnimation.Value);

            standUpFrameWait = 1;
        }

        private void OnHitHard(CharacterCollisionCallback ccc, Collision collision)
        {
            //completely knock out if it was hit on the head
            if (ccc.collisionPart == CharacterCollisionCallback.Type.HEAD)
            {
                if (!isRegainingBalance || collision.collider.gameObject.layer != WorldUtil.wallsStatic)
                {
                    if (collision.collider.transform != GameDirector.player.head)
                    {
                        BeginRagdollMode(0.4f, Animation.FALLING_LOOP);
                    }
                }
            }
            else
            {
                //curl as soon as you touch the floor
                if (bodyState == BodyState.OFFBALANCE)
                {
                    if (!getUpAnimation.HasValue)
                    {
                        PlayBalanceCoroutine(CurlOnFloor);
                    }
                    if (currentAnim == Animation.FALLING_LOOP)
                    {
                        SetTargetAnimation(Animation.FLOOR_CURL_LOOP);
                    }
                }
            }
        }

        private void SetEnableCharacterAngularLimits(bool enable)
        {
            foreach (var muscle in puppetMaster.muscles)
            {
                if (muscle.props.alwaysOnAngularLimits)
                {
                    continue;
                }
                if (enable)
                {
                    continue;
                }
                muscle.SetEnableAngularLimits(enable);
            }
        }

        private void ResetAnchorRotationAfter()
        {
            anchor.rotation = Quaternion.identity;
            onModifyAnimations -= ResetAnchorRotationAfter;
        }

        public void BeginRagdollMode(float muscleWeight, Loli.Animation animation)
        {

            if (anchor.transform.parent != transform)
            {
                anchor.transform.SetParent(transform, true);
            }

            OverrideBodyState(BodyState.OFFBALANCE);
            // active.SetTask( active.idle, false );
            // locomotion.StopMoveTo();
            regainBalanceTimeout = 1.0f;
            puppetMaster.pinWeight = 0.0f;
            puppetMaster.muscleWeight = muscleWeight;
            getUpAnimation = null;
            standUpFrameWait = 0;
            spine1RigidBody.useGravity = true;

            SetEnableCharacterAngularLimits(true);
            puppetMaster.SetEnableInternalCollisions(true);
            OverrideClearAnimationPriority();
            SetTargetAnimation(animation);
            StopRebalanceCoroutine();
            StopActiveAnchor();
            StopAnchorSpineTransition();

            onRagdollModeBegin?.Invoke();
        }

        private void EndRagdollMode()
        {
            spine1RigidBody.useGravity = false;
            regainBalanceCoroutine = null;
            getUpAnimation = null;
        }

        public void LoseBalance()
        {
            if (currentAnim == Animation.FALLING_LOOP)
            {
                SpeakAtRandomIntervals(VoiceLine.SCREAMING, 4.0f, 1.0f);
            }
            if (isConstrained)
            {
                BeginRagdollMode(0.05f, Animation.PICKED_UP);
            }
            else
            {
                BeginRagdollMode(0.3f, Animation.FALLING_LOOP);
            }
        }

        private void StopRebalanceCoroutine()
        {
            if (regainBalanceCoroutine != null)
            {
                GameDirector.instance.StopCoroutine(regainBalanceCoroutine);
                regainBalanceCoroutine = null;
            }
        }

        private void PlayBalanceCoroutine(BalanceCoroutine coroutine)
        {
            StopRebalanceCoroutine();
            regainBalanceCoroutine = GameDirector.instance.StartCoroutine(coroutine());
        }

        private IEnumerator CurlOnFloor()
        {
            SetEnableCharacterAngularLimits(false);
            float ease = 0.0f;
            const float duration = 0.5f;
            float startMuscleWeight = puppetMaster.muscleWeight;
            while (true)
            {
                ease = Mathf.Clamp01(ease + Time.fixedDeltaTime / duration);
                puppetMaster.muscleWeight = startMuscleWeight + (0.8f - startMuscleWeight) * ease;
                if (ease == 1.0f)
                {
                    break;
                }
                yield return new WaitForFixedUpdate();
            }
        }

        private Animation CalculateGetUpAnimation()
        {
            float fallDir = spine1RigidBody.transform.forward.y;
            if (fallDir > 0.707f)
            { //45 degrees
                return Animation.FLOOR_FACE_UP_TO_STAND;
            }
            else
            {
                if (spine1RigidBody.transform.right.y > 0.707f)
                {
                    return Animation.FLOOR_FACE_SIDE_TO_STAND_RIGHT;
                }
                else if (spine1RigidBody.transform.right.y < -0.707f)
                {
                    return Animation.FLOOR_FACE_SIDE_TO_STAND_LEFT;
                }
                return Animation.FLOOR_FACE_DOWN_TO_STAND;
            }
        }

        private void AlignAnimationTransformToPhysics(bool flipYaw)
        {
            lastAnimatedSpineForward = -spine1.up;
            // if( flipYaw ){
            //     lastAnimatedSpineForward = -lastAnimatedSpineForward;
            // }

            Vector3 physicsSpineForward = (spine1RigidBody.transform.up + spine2RigidBody.transform.up + spine3RigidBody.transform.up) / 3.0f;
            float yawDiff = 180.0f + Mathf.DeltaAngle(Tools.FlatXZVectorToDegrees(lastAnimatedSpineForward), Tools.FlatXZVectorToDegrees(physicsSpineForward));

            // Debug.Break();
            // Debug.DrawLine( spine1RigidBody.transform.position+Vector3.up*0.3f, spine1RigidBody.transform.position+lastAnimatedSpineForward+Vector3.up*0.3f, Color.red, 0.2f );
            // Debug.DrawLine( spine1RigidBody.transform.position+Vector3.up*0.3f, spine1RigidBody.transform.position+physicsSpineForward+Vector3.up*0.3f, Color.green, 0.2f );
            Vector3 oldSpine1Pos = spine1.position;
            Quaternion oldSpine1Rot = spine1.rotation;
            Vector3 oldPhysicsSpine1Pos = spine1RigidBody.transform.position;
            Quaternion oldPhysicsSpine1Rot = spine1RigidBody.transform.rotation;
            anchor.rotation *= Quaternion.Euler(0.0f, yawDiff, 0.0f);

            spine1RigidBody.transform.position = oldPhysicsSpine1Pos;
            spine1RigidBody.transform.rotation = oldPhysicsSpine1Rot;
            // spine1.position = oldSpine1Pos;
            // spine1.rotation = oldSpine1Rot;
        }

        private IEnumerator RegainBalance()
        {

            AlignAnimationTransformToPhysics(getUpAnimation == Animation.FLOOR_FACE_UP_TO_STAND);
            SoundManager.main.RequestHandle(floorPos).PlayOneShot(GameDirector.instance.loliSettings.getUpSound.GetRandomAudioClip());
            animationSpeed = 0.0f;

            float balance = 0.0f;
            const float duration = 0.2f;
            float startPinWeight = 0.0f;
            float startMuscleWeight = puppetMaster.muscleWeight;
            SetEnableCharacterAngularLimits(false);
            puppetMaster.SetEnableInternalCollisions(false);

            while (balance < 1.0f)
            {
                balance = Mathf.Clamp01(balance + Time.fixedDeltaTime / duration);
                puppetMaster.pinWeight = startPinWeight + (1.0f - startPinWeight) * Mathf.Pow(balance, 4.0f);
                puppetMaster.muscleWeight = startMuscleWeight + (1.0f - startMuscleWeight) * balance;
                yield return new WaitForFixedUpdate();
            }
            animationSpeed = 1.0f;

            while (!hasBalance)
            {
                yield return new WaitForFixedUpdate();
            }
            EndRagdollMode();
        }
    }

}