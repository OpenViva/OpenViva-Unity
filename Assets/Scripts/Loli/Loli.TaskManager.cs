using UnityEngine;

namespace viva
{

    public partial class Loli : Character
    {

        [SerializeField]
        private ActiveBehaviorSettings activeBehaviorSettings;
        [SerializeField]
        private PassiveBehaviorSettings passiveBehaviorSettings;

        private Job[] jobs = new Job[System.Enum.GetValues(typeof(Job.JobType)).Length - 1];

        public LocomotionBehaviors locomotion { get { return jobs[(int)Job.JobType.LOCOMOTION] as LocomotionBehaviors; } }
        public ActiveBehaviors active { get { return jobs[(int)Job.JobType.ACTIVE] as ActiveBehaviors; } }
        public PassiveBehaviors passive { get { return jobs[(int)Job.JobType.PASSIVE] as PassiveBehaviors; } }
        public Autonomy autonomy { get; private set; } = null;

        public delegate void OnAnimationChangeCallback(Loli.Animation oldAnim, Loli.Animation newAnim);
        public delegate void OnCharacterCollisionCallback(CharacterCollisionCallback ccc, Collision collision);
        public delegate void OnCharacterTriggerCallback(CharacterTriggerCallback ccc, Collider collider);
        public delegate void TaskChangeCallback(Loli loli, ActiveBehaviors.Behavior newType);

        public OnAnimationChangeCallback onAnimationChange;
        public OnGenericCallback onFixedUpdate;
        public OnGenericCallback onUpdate;
        public OnGenericCallback onLateUpdate;
        public OnGenericCallback onLateUpdatePostIK;
        public OnCharacterCollisionCallback onCharacterCollisionEnter;
        public OnCharacterTriggerCallback onCharacterTriggerEnter;
        public TaskChangeCallback onTaskChange;
        public FirstServeItemCallStack onGiftItemCallstack { get; private set; } = new FirstServeItemCallStack();


        private static readonly int globalDirtTexID = Shader.PropertyToID("_GlobalDirtTex");


        public void InitTaskManager()
        {
            jobs[(int)Job.JobType.LOCOMOTION] = new LocomotionBehaviors(this);
            jobs[(int)Job.JobType.ACTIVE] = new ActiveBehaviors(this, activeBehaviorSettings);
            jobs[(int)Job.JobType.PASSIVE] = new PassiveBehaviors(this, passiveBehaviorSettings);
            autonomy = new Autonomy(this);

            DebugValidateAnimationInfos();
            Shader.SetGlobalTexture(globalDirtTexID, passiveBehaviorSettings.globalDirtTexture);
        }

        public void UpdateTasks()
        {
            for (int i = 0; i < jobs.Length; i++)
            {
                jobs[i].OnUpdate();
            }
            onUpdate?.Invoke();
        }
        public void FixedUpdateTasks()
        {
            autonomy.Progress();

            onFixedUpdate?.Invoke();

            for (int i = 0; i < jobs.Length; i++)
            {
                jobs[i].OnFixedUpdate();
            }
        }
        public void LateUpdateTasks()
        {
            for (int i = 0; i < jobs.Length; i++)
            {
                jobs[i].OnLateUpdate();
            }
            onLateUpdate?.Invoke();
        }

        public void LateUpdatePostIKTasks()
        {
            for (int i = 0; i < jobs.Length; i++)
            {
                jobs[i].OnLateUpdatePostIK();
            }
            onLateUpdatePostIK?.Invoke();
        }

        private void HandleJobAnimationChange(Animation oldAnim, Animation newAnim)
        {
            for (int i = 0; i < jobs.Length; i++)
            {
                jobs[i].OnAnimationChange(oldAnim, newAnim);
            }
            onAnimationChange?.Invoke(oldAnim, newAnim);
        }
    }

}