using UnityEngine;


namespace viva
{


    public class ActiveBehaviors : Job
    {

        public enum Permission
        {
            BEGIN_HEADPAT,
            BEGIN_RIGHT_HANDHOLD,
            BEGIN_LEFT_HANDHOLD,
            CONSTRAINT_PHYSICS_HANDHOLD,
            ALLOW_ROOT_FACING_TARGET_CHANGE,
            ALLOW_IMPULSE_ANIMATION,
            ALLOW_GO_AND_PICKUP
        }

        public class ActiveTask : Task
        {

            public Behavior type { get; protected set; }
            public readonly SerializedTaskData session;

            public virtual bool RequestPermission(Permission permission)
            {
                return true;    //default allows all
            }

            public ActiveTask(Loli _self, Behavior _type, SerializedTaskData _session) : base(_self, JobType.ACTIVE)
            {
                type = _type;
                session = _session;

            }

            public virtual void OnRefresh() { }
            public virtual void OnActivate() { }
            public virtual void OnDeactivate() { }
            public virtual void OnCharacterCollisionEnter(CharacterCollisionCallback ccc, Collision collision) { }
            public virtual bool OnReturnPollTaskResult(ActiveTask returnSource, bool succeeded)
            {
                return false;
            }
        }

        public enum Behavior
        {
            IDLE,
            FOLLOW,
            BATHING,
            CATTAIL,
            CAMERA_POSE,
            CAMERA_USE,
            BEG,
            CHOPSTICKS,
            SLEEPING,
            HORSEBACK,
            CHICKEN_CHASE,
            COOKING,
            POKER,
            SOCIAL,
            MERCHANT,
            WAYPOINT_FOLLOW,
            ONSEN_CLERK,
            ONSEN_SWIMMING
        }

        public ActiveTask currentTask { get; private set; } = null;

        private ActiveTask[] activeBehaviors = new ActiveTask[System.Enum.GetValues(typeof(Behavior)).Length];
        public readonly ActiveBehaviorSettings settings;
        private ActiveTask listenerTask = null;
        public bool isPolling { get { return listenerTask != null; } }

        public IdleBehavior idle { get { return activeBehaviors[(int)Behavior.IDLE] as IdleBehavior; } }
        public FollowBehavior follow { get { return activeBehaviors[(int)Behavior.FOLLOW] as FollowBehavior; } }
        public BathingBehavior bathing { get { return activeBehaviors[(int)Behavior.BATHING] as BathingBehavior; } }
        public CattailBehavior cattail { get { return activeBehaviors[(int)Behavior.CATTAIL] as CattailBehavior; } }
        public CameraPoseBehavior cameraPose { get { return activeBehaviors[(int)Behavior.CAMERA_POSE] as CameraPoseBehavior; } }
        public CameraUseBehavior cameraUse { get { return activeBehaviors[(int)Behavior.CAMERA_USE] as CameraUseBehavior; } }
        public BegBehavior beg { get { return activeBehaviors[(int)Behavior.BEG] as BegBehavior; } }
        public ChopsticksBehavior chopsticks { get { return activeBehaviors[(int)Behavior.CHOPSTICKS] as ChopsticksBehavior; } }
        public SleepingBehavior sleeping { get { return activeBehaviors[(int)Behavior.SLEEPING] as SleepingBehavior; } }
        public HorsebackBehavior horseback { get { return activeBehaviors[(int)Behavior.HORSEBACK] as HorsebackBehavior; } }
        public ChickenChaseBehavior chickenChase { get { return activeBehaviors[(int)Behavior.CHICKEN_CHASE] as ChickenChaseBehavior; } }
        public CookingBehavior cooking { get { return activeBehaviors[(int)Behavior.COOKING] as CookingBehavior; } }
        public PokerBehavior poker { get { return activeBehaviors[(int)Behavior.POKER] as PokerBehavior; } }
        public SocialBehavior social { get { return activeBehaviors[(int)Behavior.SOCIAL] as SocialBehavior; } }
        public MerchantBehavior merchant { get { return activeBehaviors[(int)Behavior.MERCHANT] as MerchantBehavior; } }
        public WaypointFollowBehavior waypointFollow { get { return activeBehaviors[(int)Behavior.WAYPOINT_FOLLOW] as WaypointFollowBehavior; } }
        public OnsenClerkBehavior onsenClerk { get { return activeBehaviors[(int)Behavior.ONSEN_CLERK] as OnsenClerkBehavior; } }
        public OnsenSwimming onsenSwimming { get { return activeBehaviors[(int)Behavior.ONSEN_SWIMMING] as OnsenSwimming; } }

        public string debugMsg = "";
        private bool recursionLock = false;


        public ActiveBehaviors(Loli _self, ActiveBehaviorSettings _settings) : base(_self, Job.JobType.ACTIVE)
        {

            settings = _settings;

            RegisterBehavior(new IdleBehavior(self));
            RegisterBehavior(new FollowBehavior(self));
            RegisterBehavior(new BathingBehavior(self));
            RegisterBehavior(new CattailBehavior(self));
            RegisterBehavior(new CameraPoseBehavior(self));
            RegisterBehavior(new CameraUseBehavior(self));
            RegisterBehavior(new BegBehavior(self));
            RegisterBehavior(new ChopsticksBehavior(self));
            RegisterBehavior(new SleepingBehavior(self));
            RegisterBehavior(new HorsebackBehavior(self));
            RegisterBehavior(new ChickenChaseBehavior(self));
            RegisterBehavior(new CookingBehavior(self));
            RegisterBehavior(new PokerBehavior(self));
            RegisterBehavior(new SocialBehavior(self));
            RegisterBehavior(new MerchantBehavior(self));
            RegisterBehavior(new WaypointFollowBehavior(self));
            RegisterBehavior(new OnsenClerkBehavior(self));
            RegisterBehavior(new OnsenSwimming(self));
        }

        private void RegisterBehavior(ActiveTask task)
        {
            activeBehaviors[(int)task.type] = task;
        }

        public void OnGesture(Item source, ObjectFingerPointer.Gesture gesture)
        {
            //Find accepted gesture in reverse order
            //prioritize the active behavior EXCEPT for idle (always lowest priority)
            if (currentTask != idle)
            {
                if (currentTask.OnGesture(source, gesture))
                {
                    return;
                }
            }

            for (int i = activeBehaviors.Length; i-- > 0;)
            {
                if (currentTask != idle)
                {
                    if (activeBehaviors[i] == currentTask)
                    {
                        continue;   //already tested
                    }
                }
                if (activeBehaviors[i].OnGesture(source, gesture))
                {
                    break;
                }
            }
        }

        public void PollNextTaskResult(ActiveTask _listenerTask)
        {
            listenerTask = _listenerTask;
        }

        public ActiveTask GetTask(Behavior behavior)
        {
            return activeBehaviors[(int)behavior];
        }

        public ActiveTask GetTaskType(Behavior behavior)
        {
            return activeBehaviors[(int)behavior];
        }

        //succeededTask: null means not applicable (used for initializing tasks)
        public void SetTask(ActiveTask task, bool? succeededTask = null)
        {

            if (task == currentTask)
            {
                return;
            }
            for (int i = 0; i < activeBehaviors.Length; i++)
            {
                if (activeBehaviors[i] == task)
                {
                    if (isPolling)
                    {
                        Debug.Log("returning poll result");
                    }
                    Debug.Log("#new Task [" + (Behavior)i + "]");
                    break;
                }
            }
            if (task.jobType != jobType)
            {
                Debug.LogError("ERROR Task not compatible with Job!");
                return;
            }
            if (recursionLock)
            {
                Debug.LogError("setTask RECURSION DETECTED! Move SetTask code outside of OnEnable/OnDisable/OnPollTaskResult!");
                Debug.Break();
            }
            recursionLock = true;

            if (listenerTask != null)
            {
                //only return to listener if a valid success value is present (skips null tasks that were active momentarily)
                if (succeededTask.HasValue)
                {
                    if (listenerTask.OnReturnPollTaskResult(currentTask, succeededTask.Value))
                    {
                        task = listenerTask;    //override with returnToTask
                    }
                    listenerTask = null;
                }
            }

            ActiveTask lastTask = currentTask;
            currentTask = task;

            self.onCharacterCollisionEnter += task.OnCharacterCollisionEnter;
            if (lastTask != null)
            {
                lastTask.OnDeactivate();
                self.onCharacterCollisionEnter -= lastTask.OnCharacterCollisionEnter;
            }
            self.onTaskChange?.Invoke(self, task.type);
            recursionLock = false;

            currentTask.OnActivate();
        }
        public bool RequestPermission(Permission permission)
        {
            if (currentTask == null)
            {
                return false;
            }
            return currentTask.RequestPermission(permission);
        }
        public bool IsTaskActive(Task task)
        {
            return currentTask == task;
        }

        public override void OnFixedUpdate()
        {
            currentTask.OnFixedUpdate();
        }
        public override void OnUpdate()
        {
            currentTask.OnUpdate();
        }
        public override void OnLateUpdate()
        {
            currentTask.OnLateUpdate();
        }
        public override void OnLateUpdatePostIK()
        {
            currentTask.OnLateUpdatePostIK();
        }
        public override void OnAnimationChange(Loli.Animation oldAnim, Loli.Animation newAnim)
        {
            currentTask.OnAnimationChange(oldAnim, newAnim);
        }
    }

}