using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using RootMotion;

namespace RootMotion.Dynamics
{

    /// <summary>
    /// The master of puppets. Enables character animation to be played physically in muscle space.
    /// </summary>
    [HelpURL("https://www.youtube.com/watch?v=LYusqeqHAUc")]
    [AddComponentMenu("Scripts/RootMotion.Dynamics/PuppetMaster/Puppet Master")]
    public partial class PuppetMaster : MonoBehaviour
    {

        // Open the User Manual URL
        [ContextMenu("User Manual (Setup)")]
        void OpenUserManualSetup()
        {
            Application.OpenURL("http://root-motion.com/puppetmasterdox/html/page4.html");
        }

        // Open the User Manual URL
        [ContextMenu("User Manual (Component)")]
        void OpenUserManualComponent()
        {
            Application.OpenURL("http://root-motion.com/puppetmasterdox/html/page5.html");
        }

        [ContextMenu("User Manual (Performance)")]
        void OpenUserManualPerformance()
        {
            Application.OpenURL("http://root-motion.com/puppetmasterdox/html/page8.html");
        }

        // Open the Script Reference URL
        [ContextMenu("Scrpt Reference")]
        void OpenScriptReference()
        {
            Application.OpenURL("http://root-motion.com/puppetmasterdox/html/class_root_motion_1_1_dynamics_1_1_puppet_master.html");
        }

        // Open a video tutorial about setting up the component
        [ContextMenu("TUTORIAL VIDEO (SETUP)")]
        void OpenSetupTutorial()
        {
            Application.OpenURL("https://www.youtube.com/watch?v=mIN9bxJgfOU&index=2&list=PLVxSIA1OaTOuE2SB9NUbckQ9r2hTg4mvL");
        }

        // Open a video tutorial about setting up the component
        [ContextMenu("TUTORIAL VIDEO (COMPONENT)")]
        void OpenComponentTutorial()
        {
            Application.OpenURL("https://www.youtube.com/watch?v=LYusqeqHAUc");
        }

        /// <summary>
        /// Active mode means all muscles are active and the character is physically simulated. Kinematic mode sets rigidbody.isKinematic to true for all the muscles and simply updates their position/rotation to match the target's. Disabled mode disables the ragdoll. Switching modes is done by simply changing this value, blending in/out will be handled automatically by the PuppetMaster.
        /// </summary>
        [System.Serializable]
        public enum Mode
        {
            Active,
            Kinematic,
            Disabled
        }

        /// <summary>
        /// The root Transform of the animated target character.
        /// </summary>
        public Transform targetRoot;// { get; private set; }

        [Tooltip("Rigidbody.solverIterationCount for the muscles of this Puppet.")]
        /// <summary>
        /// Rigidbody.solverIterationCount for the muscles of this Puppet.
        /// </summary>
        public int solverIterationCount = 6;

        [Tooltip("If true, will draw the target's pose as green lines in the Scene view. This runs in the Editor only. If you wish to profile PuppetMaster, switch this off.")]
        /// <summary>
        /// If true, will draw the target's pose as green lines in the Scene view. This runs in the Editor only. If you wish to profile PuppetMaster, switch this off.
        /// </summary>
        public bool visualizeTargetPose = true;

        [LargeHeader("Master Weights")]

        [Tooltip("The weight of mapping the animated character to the ragdoll pose.")]
        /// <summary>
        /// The weight of mapping the animated character to the ragdoll pose.
        /// </summary>
        [Range(0f, 1f)] public float mappingWeight = 1f;

        [Tooltip("The weight of pinning the muscles to the position of their animated targets using simple AddForce.")]
        /// <summary>
        /// The weight of pinning the muscles to the position of their animated targets using simple AddForce.
        /// </summary>
        [Range(0f, 1f)] public float pinWeight = 1f;

        [Tooltip("The normalized strength of the muscles.")]
        /// <summary>
        /// The normalized strength of the muscles. Useful for blending muscle strength in/out when you have multiple puppets with various Muscle Spring values.
        /// </summary>
        [Range(0f, 1f)] public float muscleWeight = 1f;

        [LargeHeader("Joint and Muscle Settings")]

        [Tooltip("The positionSpring of the ConfigurableJoints' Slerp Drive.")]
        /// <summary>
        /// The general strength of the muscles. PositionSpring of the ConfigurableJoints' Slerp Drive.
        /// </summary>
        public float muscleSpring = 100f;

        [Tooltip("The positionDamper of the ConfigurableJoints' Slerp Drive.")]
        /// <summary>
        /// The positionDamper of the ConfigurableJoints' Slerp Drive.
        /// </summary>
        public float muscleDamper = 0f;

        // [Tooltip("If disabled, only world space AddForce will be used to pin the ragdoll to the animation while 'Pin Weight' > 0. If enabled, AddTorque will also be used for rotational pinning. Keep it disabled if you don't see any noticeable improvement from it to avoid wasting CPU resources.")]
        // /// <summary>
        // /// If disabled, only world space AddForce will be used to pin the ragdoll to the animation while 'Pin Weight' > 0. If enabled, AddTorque will also be used for rotational pinning. Keep it disabled if you don't see any noticeable improvement from it to avoid wasting CPU resources.
        // /// </summary>
        // public bool angularPinning;

        [Tooltip("When the target has animated bones between the muscle bones, the joint anchors need to be updated in every update cycle because the muscles' targets move relative to each other in position space. This gives much more accurate results, but is computationally expensive so consider leaving it off.")]
        /// <summary>
        /// When the target has animated bones between the muscle bones, the joint anchors need to be updated in every update cycle because the muscles' targets move relative to each other in position space. This gives much more accurate results, but is computationally expensive so consider leaving it off.
        /// </summary>
        public bool updateJointAnchors = true;

        [Tooltip("Enable this if any of the target's bones has translation animation.")]
        /// <summary>
        /// Enable this if any of the target's bones has translation animation.
        /// </summary>
        public bool supportTranslationAnimation;

        [LargeHeader("Individual Muscle Settings")]

        [Tooltip("The Muscles managed by this PuppetMaster.")]
        /// <summary>
        /// The Muscles managed by this PuppetMaster.
        /// </summary>
        public Muscle[] muscles = new Muscle[0];

        public delegate void UpdateDelegate();
        public delegate void MuscleDelegate(Muscle muscle);


        /// <summary>
        /// Called before (and only if) reading.
        /// </summary>
        public UpdateDelegate OnRead;

        /// <summary>
        /// Called after (and only if) writing
        /// </summary>
        public UpdateDelegate OnWrite;

        /// <summary>
        /// Called when the puppet hierarchy has changed by adding/removing muscles
        /// </summary>
        public UpdateDelegate OnHierarchyChanged;

        /// <summary>
        /// Called when a muscle has been removed.
        /// </summary>
        public MuscleDelegate OnMuscleRemoved;

        /// <summary>
        /// Called when a muscle has been disconnected.
        /// </summary>
        public MuscleDelegate OnMuscleDisconnected;

        /// <summary>
        /// Called when muscles have been reconnected.
        /// </summary>
        public MuscleDelegate OnMuscleReconnected;

        /// <summary>
        /// Gets the Animator on the target.
        /// </summary>
        [SerializeField]
        public Animator targetAnimator { get; protected set; }

        /// <summary>
        /// Gets the Animation component on the target.
        /// </summary>
        public Animation targetAnimation { get; private set; }

        /// <summary>
        /// The list of solvers that will be updated by this PuppetMaster. When you add a Final-IK component in runtime after PuppetMaster has initiated, add it to this list using solver.Add(SolverManager solverManager).
        /// </summary>
        [HideInInspector] public List<SolverManager> solvers = new List<SolverManager>();

        /// <summary>
        /// If true, PuppetMaster will not handle angular limits and you can have full control over handling it (call SetAngularLimitsManual();).
        /// </summary>
        [HideInInspector] [NonSerialized] public bool manualAngularLimitControl;

        /// <summary>
        /// If disabled, disconnected bones will not be mapped to disconnected ragdoll parts.
        /// </summary>
        [SerializeField] [HideInInspector] public bool mapDisconnectedMuscles = true;

        #region Update Sequence

        private bool awakeFailed;
        // private bool interpolated;
        private bool freezeFlag;
        private bool hasBeenDisabled;
        private Vector3 teleportPosition;
        private Quaternion teleportRotation = Quaternion.identity;
        private bool teleportMoveToTarget;

        // If PuppetMaster has been deactivated externally
        void OnDisable()
        {
            if (!gameObject.activeInHierarchy && Application.isPlaying)
            {
                foreach (Muscle m in muscles) m.Reset();
            }
            hasBeenDisabled = true;
        }

        // If reactivating a PuppetMaster that has been forcefully deactivated and state/mode switching interrupted
        // void OnEnable()
        // {
        //     if (gameObject.activeInHierarchy && hasBeenDisabled && Application.isPlaying)
        //     {
        //         // Reset mode
        //         isSwitchingMode = false;
        //         activeMode = mode;
        //         lastMode = mode;
        //         mappingBlend = mode == Mode.Active ? 1f : 0f;

        //         // Reset state
        //         activeState = state;
        //         lastState = state;
        //         isKilling = false;

        //         // Animation
        //         SetAnimationEnabled(state == State.Alive);
        //         if (state == State.Alive && targetAnimator != null && targetAnimator.gameObject.activeInHierarchy)
        //         {
        //             targetAnimator.Update(0.001f);
        //         }

        //         // Muscle weights
        //         foreach (Muscle m in muscles)
        //         {
        //             m.state.pinWeightMlp = state == State.Alive ? 1f : 0f;
        //             m.state.muscleWeightMlp = state == State.Alive ? 1f : stateSettings.deadMuscleWeight;
        //             m.state.muscleDamperAdd = 0f;
        //             //m.state.immunity = 0f;
        //         }
        //     }
        // }

        public Transform FindTargetRootRecursive(Transform t)
        {
            if (t.parent == null) return null;

            foreach (Transform child in t.parent)
            {
                if (child == transform) return t;
            }

            return FindTargetRootRecursive(t.parent);
        }

        public void Initiate()
        {
            // Validation
            if (!IsValid(true)) return;

            for (int i = 0; i < muscles.Length; i++)
            {
                // Initiating the muscles
                muscles[i].Initiate(muscles);
            }

            UpdateHierarchies();

            // Switching states
            // SwitchStates();

            // Switching modes
            // SwitchModes();

            foreach (Muscle m in muscles) m.Read();

            // Mapping
            StoreTargetMappedState();

            if (PuppetMasterSettings.instance != null)
            {
                PuppetMasterSettings.instance.Register(this);
            }

            var solversArray = (SolverManager[])targetRoot.GetComponentsInChildren<SolverManager>();
            solvers.AddRange(solversArray);
        }

        void OnDestroy()
        {
            if (PuppetMasterSettings.instance != null)
            {
                PuppetMasterSettings.instance.Unregister(this);
            }
        }

        // private bool IsInterpolated()
        // {
        //     if (!initiated) return false;

        //     foreach (Muscle m in muscles)
        //     {
        //         if (m.rigidbody.interpolation != RigidbodyInterpolation.None) return true;
        //     }

        //     return false;
        // }

        private Vector3 rebuildPelvisPos;
        private Quaternion rebuildPelvisRot = Quaternion.identity;

        // Moves the muscles to where their targets are.
        private void MoveToTarget()
        {
            if (PuppetMasterSettings.instance == null || (PuppetMasterSettings.instance != null && PuppetMasterSettings.instance.UpdateMoveToTarget(this)))
            {
                foreach (Muscle m in muscles)
                {
                    m.MoveToTarget();
                }
            }
        }

        // Read the current animated target pose
        private void Read()
        {
#if UNITY_EDITOR
            VisualizeTargetPose();
#endif

            foreach (Muscle m in muscles) m.Read();

            if (updateJointAnchors)
            {
                for (int i = 0; i < muscles.Length; i++) muscles[i].UpdateAnchor(supportTranslationAnimation);
            }
        }

        // Which update mode is the target's Animator/Animation using?
        private AnimatorUpdateMode targetUpdateMode
        {
            get
            {
                if (targetAnimator != null) return targetAnimator.updateMode;
                if (targetAnimation != null) return targetAnimation.animatePhysics ? AnimatorUpdateMode.AnimatePhysics : AnimatorUpdateMode.Normal;
                return AnimatorUpdateMode.Normal;
            }
        }

        #endregion Update Sequence

        // Visualizes the target pose exactly as it is read by the PuppetMaster
        public void VisualizeTargetPose()
        {
            if (!visualizeTargetPose) return;
            if (!Application.isEditor) return;

            foreach (Muscle m in muscles)
            {
                if (m.joint.connectedBody != null && m.connectedBodyTarget != null)
                {
                    Debug.DrawLine(m.target.position, m.connectedBodyTarget.position, Color.black);

                    bool isEndMuscle = true;
                    foreach (Muscle m2 in muscles)
                    {
                        if (m != m2 && m2.joint.connectedBody == m.rigidbody)
                        {
                            isEndMuscle = false;
                            break;
                        }
                    }

                    if (isEndMuscle) VisualizeHierarchy(m.target, Color.black);
                }
            }
        }

        // Recursively visualizes a bone hierarchy
        private void VisualizeHierarchy(Transform t, Color color)
        {
            for (int i = 0; i < t.childCount; i++)
            {
                Debug.DrawLine(t.position, t.GetChild(i).position, color);
                VisualizeHierarchy(t.GetChild(i), color);
            }
        }

        private void IgnoreInternalCollisions(){
            for (int i = 0; i < muscles.Length; i++){
                for (int i2 = i; i2 < muscles.Length; i2++){
                    if (i != i2){
                        muscles[i].IgnoreInternalCollisions(muscles[i2]);
                    }
                }
            }
        }

        public void IgnoreInternalCollisions(Muscle m){
            foreach (Muscle otherMuscle in muscles){
                if (otherMuscle != m){
                    m.IgnoreInternalCollisions(otherMuscle);
                }
            }
        }

        private void ResetInternalCollisions(){
            for (int i = 0; i < muscles.Length; i++){
                for (int i2 = i; i2 < muscles.Length; i2++){
                    if (i != i2){
                        muscles[i].ResetInternalCollisions(muscles[i2]);
                    }
                }
            }
        }
    }
}
