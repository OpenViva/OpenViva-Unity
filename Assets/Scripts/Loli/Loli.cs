using RootMotion.Dynamics;
using UnityEngine;

namespace viva
{


    public partial class Loli : Character
    {


        public enum EyeLogicFlags
        {
            NONE = 0,
            BLINK = 1,
            EYELID_FOLLOW = 2,
            PUPIL_FOLLOW = 4,
            EYELID_PUPIL_CLAMP = 8,
            NORMAL = 7
            //7 EYELID_FOLLOW + PUPIL_FOLLOW + BLINK
            //6 EYELID_FOLLOW + PUPIL_FOLLOW
            //10 EYELID_FOLLOW + EYELID_PUPIL_CLAMP
            //12 PUPIL_FOLLOW + EYELID_PUPIL_CLAMP
            //13 PUPIL_FOLLOW + EYELID_PUPIL_CLAMP + BLINK
            //Do not use pupil follow+clamp at the same time
        }

        public enum BodyLogicFlags
        {
            NONE = 0,
            HEAD_FOLLOW = 1,
            SPINE2_FOLLOW = 2,
            NORMAL = 3
        }

        [SerializeField]
        private CharacterSelectionTarget m_characterSelectionTarget;
        public CharacterSelectionTarget characterSelectionTarget { get { return m_characterSelectionTarget; } }
        [SerializeField]
        private PuppetMaster m_puppetMaster;
        public PuppetMaster puppetMaster { get { return m_puppetMaster; } }
        [SerializeField]
        private Transform m_anchor;
        public Transform anchor { get { return m_anchor; } }
        [SerializeField]
        private Transform m_spine1;
        public Transform spine1 { get { return m_spine1; } }
        [SerializeField]
        private Transform m_spine2;
        public Transform spine2 { get { return m_spine2; } }
        [SerializeField]
        private Transform m_spine3;
        public Transform spine3 { get { return m_spine3; } }
        [SerializeField]
        private Transform m_foot_r;
        public Transform foot_r { get { return m_foot_r; } }
        [SerializeField]
        private Transform m_foot_l;
        public Transform foot_l { get { return m_foot_l; } }
        [SerializeField]
        private Transform m_shoulder_r;
        public Transform shoulder_r { get { return m_shoulder_r; } }
        [SerializeField]
        private Transform m_shoulder_l;
        public Transform shoulder_l { get { return m_shoulder_l; } }
        [SerializeField]
        private Transform m_bodyArmature;
        public Transform bodyArmature { get { return m_bodyArmature; } }
        [SerializeField]
        private float m_dirt;
        public float dirt { get { return m_dirt; } private set { m_dirt = value; } }
        [SerializeField]
        private MeshRenderer nametagMR;
        [SerializeField]
        private SpeechBubbleDisplay m_speechBubbleDisplay;
        public SpeechBubbleDisplay speechBubbleDisplay { get { return m_speechBubbleDisplay; } }

        private Outfit m_outfit = null;
        [VivaFileAttribute]
        public Outfit outfit { get { return m_outfit; } private set { m_outfit = value; } }


        private LookAtBone headLookAt = null;
        private LookAtBone spine2LookAt = null;
        private VivaModel m_headModel;
        public VivaModel headModel { get { return m_headModel; } }
        private float bodyFlagPercent = 1.0f;
        private float bodyFlagSpeedMult = 1.0f;
        private float eyeFlagSpeedMult = 1.0f;
        private VivaModel vivaHead;
        private static readonly int dirtID = Shader.PropertyToID("_Dirt");
        private int lowLODstep = 0;
        private bool previewMode = false;
        private static int loliIDCounter = 0;
        private int loliID = 0;
        private float animationSpeed = 1.0f;
        public float animationDelta { get; protected set; } = 0.0f;
        public float lastPhysicsStepMult { get; protected set; } = 1.0f;
        private Vector3 lastFloorPos;


        protected override Vector3 CalculateFloorPosition()
        {
            return lastFloorPos;
        }

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            spine1RigidBody.transform.position = spine1.position + Vector3.up * (spine1RigidBody.transform.position.y - lastFloorPos.y);
            spine1RigidBody.transform.rotation = spine1.rotation;
            puppetMaster.ClearVelocities();
        }

        public void TeleportToSpawn(Vector3 position, Quaternion rotation)
        {
            spine1RigidBody.transform.position = position;
            spine1RigidBody.transform.rotation = rotation;
            puppetMaster.ClearVelocities();
        }

        public void IncreaseDirt(float percent)
        {
            dirt = Mathf.Clamp01(dirt + percent);
            SetFloatParameter(dirtID, dirt * 0.05f);
        }

        protected override void OnFootstep()
        {
            bool isBathing = active.bathing.GetBathingPhase() != BathingBehavior.BathingPhase.NONE;
            bool touchingDirtyWater = !isBathing && footstepInfo.currentType == FootstepInfo.Type.WATER;
            if (footstepInfo.currentType == FootstepInfo.Type.DIRT || touchingDirtyWater)
            {

                //increase dirt amount
                IncreaseDirt(0.0001f);
            }
        }
        protected override void OnCharacterAwake()
        {

            if (outfit == null)
            {
                outfit = Outfit.Create(
                    new string[]{
                    "skirt 1",
                    },
                    false
                );
            }
            SetOutfit(outfit);

            rightLoliHandState.InitializeIK();
            leftLoliHandState.InitializeIK();
            InitAnimationVariables();
            InitTaskManager();
            InitPuppet();

            name = sessionReferenceName;
            loliID = loliIDCounter++;
            worldMask = Instance.wallsMask | Instance.wallsStaticForCharactersMask | Instance.wallsStaticForLoliOnlyMask;
            lastFloorPos = CalculateCurrentFloorPosition();
        }

        public override void OnEnable()
        {
            if (puppetMaster != null)
            {
                puppetMaster.gameObject.SetActive(true);
            }
        }

        public override void OnDisable()
        {
            if (puppetMaster != null)
            {
                puppetMaster.gameObject.SetActive(false);
            }
        }

        private void InitPuppet()
        {
            puppetMaster.transform.position = transform.position;
            puppetMaster.FixMusclePositionsAndRotations();
            puppetMaster.Initiate();

            foreach (var muscle in puppetMaster.muscles)
            {
                muscle.SetEnableAngularLimits(muscle.props.alwaysOnAngularLimits);
            }
            puppetMaster.transform.SetParent(null, true);

            puppetMaster.SetEnableInternalCollisions(false);

            spine1RigidBody.useGravity = false;
        }

        public override void OnCharacterUpdate()
        {
            UpdateTasks();

            
            if (spine1.transform.position.y < 0.0f || spine1.transform.position.y > 500.0f)
            {
                Vector3 respawnPos = new Vector3(62.34f, 144.57f, 325.11f);
                TeleportToSpawn(respawnPos, spine1.transform.rotation);
            }           
        }

        public override void OnCharacterFixedUpdate()
        {
            FixedUpdateAnimation();

            FixedUpdateViewAwareness();
            FixedUpdateLOD();
        }

        public void SetPreviewMode(bool enable)
        {
            active.idle.enableFaceTargetTimer = !enable;
            puppetMaster.mappingWeight = System.Convert.ToInt32(!enable);
            transform.SetParent(null, true);
            previewMode = enable;
            if (enable)
            {
                SetModelLayer(Instance.offscreenSpecialLayer);
                headSMR.rootBone = head;
            }
            else
            {
                SetModelLayer(0);
                headSMR.rootBone = headRigidBody.transform;
            }
        }

        private ArmIK debugArmIK;
        private ArmIK.RetargetingInfo debugRetargeting;
        private void DebugIK()
        {
            if (debugArmIK == null)
            {
                debugArmIK = new ArmIK(rightLoliHandState.holdArmIK);
                debugRetargeting = new ArmIK.RetargetingInfo();
            }
            debugArmIK.OverrideWorldRetargetingTransform(
                debugRetargeting,
                GameObject.Find("DEBUGSPHERE").transform.position,
                GameObject.Find("DEBUGSPHERE2").transform.position,
                rightLoliHandState.fingerAnimator.hand.rotation
            );

            debugArmIK.Apply(debugRetargeting, 1.0f);
        }

        private void FixedUpdateAnimation()
        {
            int maxPhysicsLOD;
            if (hasBalance)
            {
                maxPhysicsLOD = lodLevel;
            }
            else
            {
                maxPhysicsLOD = 0;
            }
            int lodMult = Mathf.Min(maxPhysicsLOD * maxPhysicsLOD + 1, 4);  //1, 2, 4
            lastPhysicsStepMult = System.Convert.ToInt32((lowLODstep++ + loliID) % lodMult == 0) * lodMult;

            lastFloorPos = CalculateCurrentFloorPosition();

            if (lastPhysicsStepMult > 0.0f)
            {
                animationDelta = Time.fixedDeltaTime * lastPhysicsStepMult * animationSpeed;
                FixedUpdateAnimationGraph();
                animator.Update(animationDelta);
                FixedUpdateAnimationEvents();

                UpdateRootTransform();
                onModifyAnimations?.Invoke();
                FixedUpdateLookAtLogic();
                // DebugIK();
                puppetMaster.VisualizeTargetPose();

                RecalculateGroundHeight();
                puppetMaster.SimulatePhysics(lastPhysicsStepMult);
                FixedUpdateBalanceCheck();
                puppetMaster.ApplyCurrentPose();

                FixedUpdateTasks();

                UpdateEyes();
            }
            else
            {
                UpdateRootTransform();
                FixedUpdateAnimationGraph();
                FixedUpdateAnimationEvents();
                FixedUpdateBalanceCheck();
            }
            LateUpdateTasks();
            if (!anchorActive)
            {
                if (enableStandOnGround)
                {
                    StandOnGround(groundHeight);
                }
            }
            else
            {
                foreach (var muscle in puppetMaster.muscles)
                {
                    muscle.PinPosition(puppetMaster.pinWeight);
                }
            }
        }

        private void ForceAnimateNextFrame()
        {
            lowLODstep = 0;
        }

        public override bool IgnorePersistance()
        {
            if (!gameObject.activeSelf)
            {
                return true;
            }
            return base.IgnorePersistance();
        }

        public override void OnCharacterLateUpdatePostIK()
        {
            LateUpdatePostIKTasks();
        }

        public void ApplyToonAmbience(Vector3 cameraPosition, Color toonAmbience)
        {

            float sqDist = Vector3.SqrMagnitude(spine2.position - cameraPosition);
            float ease = Tools.EaseInOutQuad(Mathf.Clamp01(1.0f - sqDist * 0.001f));
            Color proximityAmbience = Color.LerpUnclamped(Color.black, toonAmbience, ease);
            foreach (Material mat in toonMaterials.objects)
            {
                mat.SetColor(Instance.toonProximityAmbienceID, proximityAmbience);
            }
            foreach (OutfitInstance.AttachmentInstance attachment in outfitInstance.attachmentInstances)
            {
                foreach (Renderer renderer in attachment.activeRenderers)
                {
                    renderer.material.SetColor(Instance.toonProximityAmbienceID, proximityAmbience);
                }
            }
        }

        public override bool IsSittingOnFloor()
        {
            return bodyState == BodyState.FLOOR_SIT;
        }

        public void SetEyeVariables(int flags, float transitionTime)
        {

            eyeFlagSpeedMult = transitionTime;

            if (Mathf.Min(1.0f, flags & (int)EyeLogicFlags.EYELID_PUPIL_CLAMP) + Mathf.Min(1.0f, flags & (int)EyeLogicFlags.EYELID_FOLLOW) == 2.0f)
            {
                Debug.Log("Invalid PUPIL behavior [EYELID_PUPIL_CLAMP+EYELID_FOLLOW] at " + m_currentAnim + "/" + m_targetAnim + " = " + flags);
            }
            eyeFlags = flags;
            float eyePercent = Mathf.Min(1.0f, flags & (int)EyeLogicFlags.PUPIL_FOLLOW);
            rightEye.lookAt.easeBlend.StartBlend(eyePercent, transitionTime);
            leftEye.lookAt.easeBlend.StartBlend(eyePercent, transitionTime);
            eyelidFollowTargetBlend = Mathf.Min(1.0f, flags & (int)EyeLogicFlags.EYELID_FOLLOW);
            randomEyeTimer = 0.0f;
        }

        public void setBodyVariables(int flags, float percent, float transitionTime)
        {
            bodyFlagPercent = percent;
            bodyFlagSpeedMult = transitionTime;
#if UNITY_EDITOR
            if (percent == 0.0f)
            {
                // Debug.LogError("setBodyFlagPercent must not be zero!");
                return;
            }
            if (bodyFlagPercent > 1.0f)
            {
                Debug.LogError("###ERROR### Set body flag percent > 1. at " + m_currentAnim);
            }
#endif
            headLookAt.easeBlend.StartBlend(Mathf.Min(bodyFlagPercent, flags & (int)BodyLogicFlags.HEAD_FOLLOW), transitionTime);
            spine2LookAt.easeBlend.StartBlend(Mathf.Min(bodyFlagPercent, flags & (int)BodyLogicFlags.SPINE2_FOLLOW), transitionTime * 1.4f);
        }

        public void OnDrawGizmosSelected()
        {
            if (rightLoliHandState == null || leftLoliHandState == null)
            {
                return;
            }
            Gizmos.color = new Color(0.0f, 0.0f, 1.0f, 0.5f);
            Gizmos.DrawSphere(rightLoliHandState.holdRetargeting.target, 0.05f);
            Gizmos.DrawSphere(rightLoliHandState.holdRetargeting.pole, 0.05f);
            Gizmos.color = new Color(0.0f, 1.0f, 1.0f, 0.5f);
            Gizmos.DrawSphere(leftLoliHandState.holdRetargeting.target, 0.05f);
            Gizmos.DrawSphere(leftLoliHandState.holdRetargeting.pole, 0.05f);
        }

        public void OnGUI()
        {

            return;
            GUIStyle debugStyle = new GUIStyle();
            debugStyle.normal.textColor = Color.yellow;
            debugStyle.fontSize = 18;
            string extra = " " + rightHandState.holdType + ":" + leftHandState.holdType;
            //extra += " r:"+rightHandHoldState.holdEaseBlend.value+"/"+rightHandHoldState.cachedPoseEaseBlend.value;
            extra += " ACTIVE:" + active.debugMsg;
            GUI.Label(new Rect(35.0f, 40.0f, 100.0f, 30.0f), "REG: " + extra, debugStyle);
            GUI.Label(new Rect(35.0f, 60.0f, 100.0f, 30.0f), "" + m_currentAnim, debugStyle);
            GUI.Label(new Rect(35.0f, 80.0f, 100.0f, 30.0f), "" + m_targetAnim, debugStyle);
            string lookAtStr = "" + changeLookAtSpeed + " (" + viewItems.objects.Count + " VU) ";
            if (m_currentLookAtTransform != null)
            {
                lookAtStr += m_currentLookAtTransform.gameObject.name;
            }
            else
            {
                lookAtStr += "null";
            }
            if (randomViewTimer <= 0.0f)
            {
                GUI.Label(new Rect(25.0f, 100.0f, 100.0f, 30.0f), lookAtStr, debugStyle);
            }
            else
            {
                if (currentLookAtItem != null)
                {
                    GUI.Label(new Rect(25.0f, 100.0f, 100.0f, 30.0f), Mathf.FloorToInt(randomViewTimer * 10.0f) / 10 + "," + currentLookAtItem.name, debugStyle);
                }
            }
            int bodyFlags = Mathf.Min(1, (int)(spine2LookAt.easeBlend.value + 0.99f)) * 2 + Mathf.Min(1, (int)(headLookAt.easeBlend.value + 0.99f));
            GUI.Label(new Rect(25.0f, 120.0f, 100.0f, 30.0f), "BF: " + bodyFlags + " " + (bodyFlagPercent * 100.0f) + "% -->" + headLookAt.easeBlend.getDuration() + "s", debugStyle);
            GUI.Label(new Rect(25.0f, 140.0f, 100.0f, 30.0f), "h: " + (int)(headLookAt.easeBlend.value * 100.0f) + "%", debugStyle);
            GUI.Label(new Rect(25.0f, 160.0f, 100.0f, 30.0f), "s2: " + (int)(spine2LookAt.easeBlend.value * 100.0f) + "%", debugStyle);
            GUI.Label(new Rect(25.0f, 180.0f, 100.0f, 30.0f), "eF: " + eyeFlags + " -->" + Mathf.Floor(leftEye.lookAt.easeBlend.getDuration() * 100.0f) / 100.0f + "s " + awarenessMode, debugStyle);
            GUI.Label(new Rect(25.0f, 200.0f, 100.0f, 30.0f), "e: " + (int)(rightEye.lookAt.easeBlend.value * 100.0f) + "% t-" + randomViewTimer, debugStyle);
            GUI.Label(new Rect(25.0f, 220.0f, 100.0f, 30.0f), "body: " + bodyState + " HAP:" + happiness, debugStyle);
            if (randomViewTimer > 0.0f)
            {
                debugStyle.normal.textColor = Color.red;
            }
            GUI.Label(new Rect(25.0f, 240.0f, 100.0f, 30.0f), "eye: " + randomViewTimer, debugStyle);
        }

        [SerializeField]
        public Vector3 debugVar;
        [SerializeField]
        public Vector3 debugVar2;
    }

}