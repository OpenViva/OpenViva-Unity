using UnityEngine;


namespace viva
{

    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Animator))]
    public class Horse : Mechanism
    {

        [SerializeField]
        private Transform rootMotionFix;
        [SerializeField]
        private Transform orientation;
        [SerializeField]
        private Transform m_spine1;
        public Transform spine1 { get { return m_spine1; } }
        [SerializeField]
        private Transform m_spine0;
        public Transform spine0 { get { return m_spine0; } }
        public float mountHeight = 0.0f;
        public Vector3 mountRot = Vector3.zero;
        public Vector3 vrPlayerMountOffset = Vector3.zero;
        public Vector3 keyboardPlayerMountOffset = Vector3.zero;
        public RootAnimationOffset keyboardPlayerRigOffset;
        [Range(0.01f, 0.2f)]
        public float playerMaxMountYoffset;
        [Range(1.0f, 2.0f)]
        private float floorSampleDistance = 1.0f;
        [Range(1.0f, 3.0f)]
        [SerializeField]
        private float wallSampleDistance = 1.5f;
        [Range(-0.2f, 0.2f)]
        [SerializeField]
        private float floorStandOffset = 1.0f;
        [SerializeField]
        private FootstepInfo footstepInfo;
        [SerializeField]
        private CapsuleCollider bodyCC;
        [SerializeField]
        private Vector3 groundRotationOffset = new Vector3(-90.0f, 90.0f, 90.0f);
        [SerializeField]
        private Vector3 floorSampleFrontOffset = Vector3.zero;
        [SerializeField]
        private Vector3 floorSampleBackOffset = Vector3.zero;
        [SerializeField]
        private Vector3 obstacleSampleDir = Vector3.right;
        [SerializeField]
        private Vector3 obstacleSamplePos = Vector3.zero;
        [SerializeField]
        private Transform hoovesSource;
        [SerializeField]
        private Transform mouthSource;
        [SerializeField]
        private PhysicMaterial groundMaterial;
        [SerializeField]
        private PhysicMaterial airMaterial;

        private readonly float turnSpeed = 3.5f;

        private Animator animator;
        private int riderStates = 0;
        private Player driver = null;
        private Vector3? lastPos = null;
        public Rigidbody rigidBody { get; private set; }
        private float hardStopTimer = 0;
        private bool horseHintPlayed = false;
        private float[,] clopPaceFrames = {
    {
        9.0f/32.0f,
        15.0f/32.0f,
        25.0f/32.0f,
        31.0f/32.0f,
    },{
        0.0f/24.0f,
        0.1f/24.0f,
        12.0f/24.0f,
        12.1f/24.0f,
    },{
        3.0f/18.0f,
        11.0f/18.0f,
        14.0f/18.0f,
        17.0f/18.0f,
    },{
        2.0f/16.0f,
        11.0f/16.0f,
        14.0f/16.0f,
        17.0f/16.0f,
    },};

        [SerializeField]
        private AudioClip[] neighSounds;
        [SerializeField]
        private AudioClip noseSound;
        [SerializeField]
        private AudioClip breathingSound;
        [SerializeField]
        private AudioClip driverMountingSound;
        [SerializeField]
        private AudioClip passengerMountingSound;

        public readonly int sideID = Animator.StringToHash("side");
        public readonly int speedID = Animator.StringToHash("speed");
        public readonly int locomotionID = Animator.StringToHash("locomotion");
        public readonly int neighID = Animator.StringToHash("neigh");
        public readonly int fallingID = Animator.StringToHash("falling");
        public readonly int sleepInID = Animator.StringToHash("sleep_in");
        public readonly int sleepLoopID = Animator.StringToHash("sleep_loop");
        public readonly int sleepOutID = Animator.StringToHash("sleep_out");
        public readonly int lookRightID = Animator.StringToHash("idle_look_right");
        public readonly int lookLeftID = Animator.StringToHash("idle_look_left");
        private float realSide = 0;
        private float smoothSide = 0;
        public float targetSide = 0;
        private float realSpeed = 0;
        private float targetSpeed = 0;
        private float lastVelocity = 0.0f;
        private float m_acceleration = 0.0f;
        public float acceleration { get { return m_acceleration; } }
        private float paceCloppingFrame = 0.0f;
        private float lastPaceCloppingFrame = 0.0f;
        private float walkPaceSoundTimer = 0.0f;
        public FilterUse backSeat { get; private set; } = new FilterUse();
        Vector3? averageFloorNormal = null;
        private bool m_sleeping = false;
        public bool sleeping { get { return m_sleeping; } }

        protected void PlayBreatheSound()
        {
            SoundManager.main.RequestHandle(mouthSource.position).PlayOneShot(breathingSound);
        }

        protected void ExecuteBeginSleep()
        {
            m_sleeping = true;
            rigidBody.isKinematic = true;
        }

        protected void ExecuteWakeUp()
        {
            m_sleeping = false;
            rigidBody.isKinematic = false;
        }

        public void WakeUp()
        {
            if (sleeping)
            {
                AttemptCrossFade(sleepOutID, 0.2f, false);
            }
        }


        private void SetEnableRootMotion(bool enable)
        {
            if (animator.applyRootMotion == enable)
            {
                return;
            }
            animator.applyRootMotion = enable;
            rootMotionFix.localEulerAngles = Vector3.up * System.Convert.ToInt32(!enable) * -90.0f;
            if (enable)
            {
                bodyCC.material = groundMaterial;
                bodyCC.center = new Vector3(-0.6f, 0.6f, 0.0f);
                bodyCC.direction = 0;
            }
            else
            {
                bodyCC.material = airMaterial;
                bodyCC.center = new Vector3(0.0f, 0.6f, 0.6f);
                bodyCC.direction = 2;
            }
        }
        // SoundManager.main.RequestHandle( hoovesSource.position ).PlayOneShot( passengerMountingSound );


        public void AddDriverHand(Player newDriver)
        {
            if (riderStates == 0)
            {
                MountDriver(newDriver);
            }
            riderStates++;
        }

        public void RemoveDriverHand()
        {
            riderStates--;
        }

        private void Sleep()
        {
            if (!IsCurrentAnimation(locomotionID) || realSpeed > 0.5f)
            {
                return;
            }
            AttemptCrossFade(sleepInID, 0.2f, true);
        }

        private void MountDriver(Player newDriver)
        {

            driver = newDriver;
            var handle = SoundManager.main.RequestHandle(hoovesSource.position);
            handle.PlayOneShot(driverMountingSound);
            GameDirector.mechanisms.Add(this);

            driver.rigidBody.isKinematic = true;
            driver.rightHandState.selfItem.SetAttribute(Item.Attributes.DO_NOT_LOOK_AT);
            driver.leftHandState.selfItem.SetAttribute(Item.Attributes.DO_NOT_LOOK_AT);

            if (driver.controls == Player.ControlType.KEYBOARD)
            {
                driver.SetInputController(new HorseKeyboardControls(this));
            }
            else
            {
                driver.SetInputController(new HorseVRControls(this));
            }
            handle.volume = 0.125f;
        }

        private void DismountDriver()
        {

            GameDirector.mechanisms.Remove(this);
            lastPos = null;
            driver.rigidBody.isKinematic = false;
            driver.transform.gameObject.layer = Instance.playerMovementLayer;

            driver.rightHandState.selfItem.ClearAttribute(Item.Attributes.DO_NOT_LOOK_AT);
            driver.leftHandState.selfItem.ClearAttribute(Item.Attributes.DO_NOT_LOOK_AT);

            driver.SetInputController(null);
            driver = null;

            if (GameDirector.skyDirector.daySegment == SkyDirector.DaySegment.NIGHT)
            {
                Sleep();
            }
            else
            {
                AttemptCrossFade(locomotionID, 0.2f);
            }
        }

        public override void OnMechanismUpdate()
        {
            realSide = Mathf.MoveTowards(realSide, targetSide, Time.deltaTime * turnSpeed);
            smoothSide = Mathf.LerpUnclamped(smoothSide, realSide, Time.deltaTime * 2.5f);
            realSpeed = Mathf.MoveTowards(realSpeed, targetSpeed, Time.deltaTime * turnSpeed);
            animator.SetFloat(sideID, smoothSide);
            animator.SetFloat(speedID, realSpeed);
            if (riderStates <= 0)
            {
                targetSide = 0;
                targetSpeed = 0;

                //end and disable Horse
                if (realSpeed == 0)
                {
                    DismountDriver();
                }
            }
            if (!animator.applyRootMotion)
            {
                rigidBody.AddForce(transform.right * -realSpeed * 1000.0f, ForceMode.Force);
            }

            //update clopping sound
            paceCloppingFrame = animator.GetCurrentAnimatorStateInfo(0).normalizedTime % 1.0f;
            if (paceCloppingFrame == 0 && !animator.IsInTransition(0) && IsCurrentAnimation(locomotionID))
            {
                walkPaceSoundTimer += Time.deltaTime;
                if (walkPaceSoundTimer > 30.0f)
                {
                    walkPaceSoundTimer = 0.0f;
                    SoundManager.main.RequestHandle(mouthSource.position).PlayOneShot(noseSound);
                }
            }
            if (lastVelocity > 0.003f && averageFloorNormal.HasValue)
            {
                int clopPaceIndex = Mathf.Clamp(Mathf.RoundToInt(realSpeed), 1, 4) - 1;
                if (paceCloppingFrame < lastPaceCloppingFrame)
                {
                    PlayClopPaceFrame(clopPaceIndex, lastPaceCloppingFrame, 1.0f);
                    PlayClopPaceFrame(clopPaceIndex, 0.0f, paceCloppingFrame);
                }
                else
                {
                    PlayClopPaceFrame(clopPaceIndex, lastPaceCloppingFrame, paceCloppingFrame);
                }
            }
            lastPaceCloppingFrame = paceCloppingFrame;
        }

        private void PlayClopPaceFrame(int clopPaceIndex, float prev, float next)
        {
            for (int i = 0; i < 4; i++)
            {
                float clopPace = clopPaceFrames[clopPaceIndex, i];
                AudioClip sound;
                int index = i;
                if (i % 2 == 1)
                {
                    index += 2;
                }
                sound = footstepInfo.sounds[(int)footstepInfo.currentType].sounds[i];
                if (clopPace > prev && clopPace <= next)
                {
                    SoundManager.main.RequestHandle(hoovesSource.position).PlayOneShot(sound);
                }
            }
        }

        protected void PlayRandomClopSound()
        {
            int randomIndex = Random.Range(0, 3);
            SoundManager.main.RequestHandle(hoovesSource.position).PlayOneShot(footstepInfo.sounds[(int)footstepInfo.currentType].sounds[randomIndex]);
        }

        public override void OnMechanismFixedUpdate()
        {
            SetEnableRootMotion(!(IsCurrentAnimation(fallingID)));

            if (!lastPos.HasValue)
            {
                lastPos = transform.position;
            }
            float velocity = (transform.position - lastPos.Value).magnitude;
            m_acceleration = velocity - lastVelocity;
            if (m_acceleration < -0.07f)
            {
                hardStopTimer += Time.deltaTime;
                if (hardStopTimer > 0.3f)
                {
                    hardStopTimer = 0.0f;
                    OnHardStop();
                }
            }
            else
            {
                hardStopTimer = 0.0f;
            }
            lastVelocity = velocity;
            lastPos = transform.position;
        }

        private void OnHardStop()
        {
            if (!IsCurrentAnimation(locomotionID) || IsNextAnimation(neighID))
            {
                return;
            }
            AttemptCrossFade(neighID, 0.2f);
            targetSpeed = 0;
            SoundManager.main.RequestHandle(mouthSource.position).PlayOneShot(neighSounds[(int)Random.Range(0.0f, neighSounds.Length - 1)]);
            if (backSeat.owner != null)
            {
                Loli loli = backSeat.owner as Loli;
                if (loli)
                {
                    loli.active.horseback.OnHardStop();
                }
            }
        }

        private RaycastHit? SampleFloorNormal(Vector3 pos, Vector3 dir, float distance)
        {

            if (GamePhysics.GetRaycastInfo(pos, dir, distance, Instance.wallsMask))
            {
                Debug.DrawLine(pos, pos + dir.normalized * distance, Color.green, 0.1f);
                return GamePhysics.result();
            }
            Debug.DrawLine(pos, pos + dir.normalized * distance, Color.red, 0.1f);
            return null;
        }

        public override void OnMechanismLateUpdate()
        {
            //trace down
            RaycastHit? floorHitFront = SampleFloorNormal(transform.TransformPoint(floorSampleFrontOffset), -transform.up, floorSampleDistance);
            RaycastHit? floorHitBehind = SampleFloorNormal(transform.TransformPoint(floorSampleBackOffset), -transform.up, floorSampleDistance);
            averageFloorNormal = null;
            if (floorHitFront.HasValue)
            {
                averageFloorNormal = floorHitFront.Value.normal;
            }
            if (floorHitBehind.HasValue)
            {
                if (averageFloorNormal.HasValue)
                {
                    averageFloorNormal = (floorHitFront.Value.normal + floorHitBehind.Value.normal).normalized;
                }
                else
                {
                    averageFloorNormal = floorHitBehind.Value.normal;
                }
            }
            Quaternion floorRotation;
            if (!averageFloorNormal.HasValue)
            {
                AttemptCrossFade(fallingID, 0.4f);
                targetSpeed = 0;
                floorRotation = Quaternion.LookRotation(Vector3.up, transform.forward);
            }
            else
            {
                if ((IsCurrentAnimation(fallingID) && !IsNextAnimation(locomotionID)) || IsNextAnimation(fallingID))
                {
                    if (IsCurrentAnimation(fallingID))
                    {
                        AttemptCrossFade(locomotionID, 0.1f, true);
                    }
                    else
                    {
                        AttemptCrossFade(locomotionID, 0.5f, true);
                    }
                }
                if (averageFloorNormal.Value.y < 0.7f)
                {
                    targetSpeed = Mathf.Min(targetSpeed, 0.0f);
                }
                floorRotation = Quaternion.RotateTowards(
                    Quaternion.LookRotation(Vector3.up, transform.forward),
                    Quaternion.LookRotation(averageFloorNormal.Value, transform.forward),
                    30.0f
                );
            }
            transform.rotation = Quaternion.LerpUnclamped(transform.rotation, floorRotation * Quaternion.Euler(groundRotationOffset), Time.deltaTime * 2.0f);

            float obstaclesDetected = 0;
            for (int x = -2; x <= 2; x++)
            {
                RaycastHit? obstacleSample = SampleFloorNormal(
                    transform.TransformPoint(obstacleSamplePos + Vector3.forward * x * 0.1f),
                    transform.TransformDirection(obstacleSampleDir + Vector3.forward * x * 0.2f),
                    wallSampleDistance
                );
                if (obstacleSample.HasValue)
                {
                    if (obstacleSample.Value.normal.y < 0.63f)
                    {
                        obstaclesDetected += 0.5f;
                    }
                    if (obstacleSample.Value.distance < 0.2f)
                    {
                        obstaclesDetected += 1.0f;
                    }
                }
                else
                {
                    obstaclesDetected += 1.0f;
                }
            }
            if (obstaclesDetected >= 2)
            {
                if (targetSpeed > 2.0f)
                {
                    OnHardStop();
                }
                else if (targetSpeed > 0.0f)
                {
                    targetSpeed = 0;
                }
            }
        }

        public bool IsCurrentAnimation(int hash)
        {
            return animator.GetCurrentAnimatorStateInfo(0).shortNameHash == hash;
        }

        public bool IsNextAnimation(int hash)
        {
            return animator.GetNextAnimatorStateInfo(0).shortNameHash == hash;
        }

        public void AttemptCrossFade(int hash, float transitionTime, bool force = false)
        {

            if (animator.IsInTransition(0) && !force)
            {
                return;
            }
            animator.CrossFade(hash, transitionTime / GetLayerAnimLength(0), 0, 0.0f);
        }

        public float GetLayerAnimLength(int layer)
        {
            if (animator.IsInTransition(layer))
            {
                return animator.GetNextAnimatorStateInfo(layer).length;
            }
            else
            {
                return animator.GetCurrentAnimatorStateInfo(layer).length;
            }
        }


        public void ShiftSpeed(int amount, int min = -1, int max = 4)
        {
            targetSpeed = Mathf.Clamp(targetSpeed + amount, min, max);
        }

        public override void OnMechanismAwake()
        {
            base.OnMechanismAwake();
            animator = GetComponent<Animator>();
            rigidBody = GetComponent<Rigidbody>();
            GameDirector.skyDirector.AddDayNightCycleCallback(new SkyDirector.DayNightCycleCallback(Sleep, 6.0f));
        }

        public override void OnMechanismTriggerEnter(MechanismCollisionCallback self, Collider collider)
        {
            Character character = Tools.SearchTransformAncestors<Character>(collider.transform);
            if (character == null)
            {
                return;
            }
            if (!horseHintPlayed)
            {
                if (character.characterType == Character.Type.PLAYER)
                {
                    horseHintPlayed = false;
                    TutorialManager.main.DisplayHint(
                        transform,
                        Vector3.up * 1.8f,
                        "Grab ONE horse rein to mount the horse. Point to the horse to tell your loli to ride it.",
                        null,
                        0.8f
                    );
                }
            }
            ReactToPosition(character.transform.position);
        }

        private void ReactToPosition(Vector3 position)
        {
            if (!IsCurrentAnimation(locomotionID) || realSpeed > 0.5f)
            {
                return;
            }
            float bearing = Tools.Bearing(orientation, position);
            if (Mathf.Abs(bearing) < 75.0f)
            {
                return;
            }
            if (bearing >= 0)
            {
                AttemptCrossFade(lookRightID, 0.1f);
            }
            else
            {
                AttemptCrossFade(lookLeftID, 0.1f);
            }
        }

        public override void OnMechanismTriggerExit(MechanismCollisionCallback self, Collider collider)
        {
        }
        public override bool AttemptCommandUse(Loli targetLoli, Character commandSource)
        {

            if (targetLoli == null)
            {
                return false;
            }
            return targetLoli.active.horseback.AttemptRideHorsePassenger(this);
        }

        public override void EndUse(Character targetCharacter)
        {
        }
    }

}
