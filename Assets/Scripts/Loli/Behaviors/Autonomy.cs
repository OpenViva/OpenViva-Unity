using System.Collections.Generic;
using UnityEngine;


namespace viva
{

    public class Autonomy : Job
    {

        public delegate void AutonomyChangeCalback();

        public new abstract class Task
        {

            public readonly string name;
            public readonly Autonomy autonomy;
            public Loli self { get { return autonomy.self; } }
            public OnGenericCallback onFail;
            public OnGenericCallback onSuccess;
            public OnGenericCallback onRemovedFromQueue;
            private readonly List<Task> requirements = new List<Task>();
            private readonly List<Task> passives = new List<Task>();
            private float? failTime = null;
            private float? successTime = null;
            public bool failed { get { return failTime.HasValue; } }
            public bool succeeded { get { return successTime.HasValue; } }
            private bool forceFailed = false;
            private bool forceSucceeded = false;
            public bool finished { get { return failed || succeeded || forceFailed || forceSucceeded; } }
            public bool registered { get; private set; } = false;
            public bool isAPassive { get; private set; } = false;
            public bool isARequirement { get; private set; } = false;
            public int passiveCount { get { return passives.Count; } }
            public int requirementCount { get { return requirements.Count; } }


            public void AddPassive(Task passive)
            {
                if (passive.registered || passive.isAPassive || passive.isARequirement || passive == this)
                {
                    Debug.LogError("[Task] Task already registered");
                    return;
                }
                passives.Add(passive);
                passive.isAPassive = true;
                if (registered)
                {
                    autonomy.RegisterTask(passive);
                }
            }

            public void RemovePassive(Task passive)
            {
                if (!passive.isAPassive)
                {
                    Debug.LogError("[Task] Task not a passive");
                    return;
                }
                int index = passives.IndexOf(passive);
                if (index > -1)
                {
                    passive.isAPassive = false;
                    passives.RemoveAt(index);

                    autonomy.UnregisterTask(passive);
                    passive.RemovedFromQueue();
                }
                else
                {
                    Debug.LogError("[Task] Passive not found");
                }
            }

            public void AddRequirement(Task requirement)
            {
                if (requirement == null || requirement.isAPassive || requirement.isARequirement || requirement == this)
                {
                    Debug.LogError("[Task] Task not eligible for requirement");
                    return;
                }
                if (isAPassive)
                {
                    Debug.LogError("[Task] SHOULD NOT ADD " + requirement.name + " INTO PASSIVE AS REQUIREMENT");
                }
                requirement.isARequirement = true;
                requirements.Add(requirement);
            }

            public void PrependRequirement(Task requirement)
            {
                if (requirement == null || requirement.isAPassive || requirement.isARequirement || requirement == this)
                {
                    Debug.LogError("[Task] Task not eligible for requirement");
                    return;
                }
                requirement.isARequirement = true;
                requirements.Insert(0, requirement);
            }

            public void RemoveRequirement(Task requirement)
            {
                if (requirement == null || !requirement.isARequirement)
                {
                    Debug.LogError("[Task] Task not a requirement");
                    return;
                }
                int index = requirements.IndexOf(requirement);
                if (index > -1)
                {
                    requirements.RemoveAt(index);
                    requirement.isARequirement = false;
                    requirement.RemovedFromQueue();
                }
                else
                {
                    Debug.LogError("[Task] Requirement not found");
                }
            }

            private void RegisterAllPassives()
            {
                foreach (Task passive in passives)
                {
                    autonomy.RegisterTask(passive);
                }
            }

            private void UnregisterAllPassives()
            {
                foreach (Task passive in passives)
                {
                    autonomy.UnregisterTask(passive);
                }
            }

            public Task GetPassive(int index)
            {
                return passives[index];
            }

            public Task GetRequirement(int index)
            {
                return requirements[index];
            }

            public void Reset()
            {
                onReset?.Invoke();
                failTime = null;
                successTime = null;
                forceFailed = false;
                forceSucceeded = false;
            }

            public Task(Autonomy _autonomy, string _name) : base()
            {
                name = _name;
                if (_autonomy == null)
                {
                    throw new System.Exception("[AUTONOMY] Null autonomy parent!");
                }
                autonomy = _autonomy;
            }
            public virtual bool OnGesture(Item source, ObjectFingerPointer.Gesture gesture)
            {
                return false;
            }

            public OnGenericCallback onFixedUpdate;
            public OnGenericCallback onUpdate;
            public OnGenericCallback onLateUpdate;
            public OnGenericCallback onLateUpdatePreLookAt;
            public OnGenericCallback onLateUpdatePostIK;
            public OnGenericCallback onModifyAnimation;
            public Loli.OnAnimationChangeCallback onAnimationChange;
            public Loli.OnCharacterCollisionCallback onCharacterCollisionEnter;
            public Loli.OnCharacterTriggerCallback onCharacterTriggerEnter;
            public OnGenericCallback onRegistered;
            public OnGenericCallback onUnregistered;
            public OnGenericCallback onFlagForSuccess;
            public OnGenericCallback onReset;

            public void FixedUpdate() { onFixedUpdate?.Invoke(); }
            public void Update() { onUpdate?.Invoke(); }
            public void LateUpdate() { onLateUpdate?.Invoke(); }
            public void LateUpdatePreLookAt() { onLateUpdatePreLookAt?.Invoke(); }
            public void LateUpdatePostIK() { onLateUpdatePostIK?.Invoke(); }
            public void ModifyAnimation() { onModifyAnimation?.Invoke(); }
            public void AnimationChange(Loli.Animation oldAnim, Loli.Animation newAnim) { onAnimationChange?.Invoke(oldAnim, newAnim); }
            public void CharacterCollisionEnter(CharacterCollisionCallback ccc, Collision collision) { onCharacterCollisionEnter?.Invoke(ccc, collision); }
            public void CharacterTriggerEnter(CharacterTriggerCallback ccc, Collider collider) { onCharacterTriggerEnter?.Invoke(ccc, collider); }
            public void RemovedFromQueue()
            {
                foreach (var requirement in requirements)
                {
                    requirement.RemovedFromQueue();
                }
                foreach (var passive in passives)
                {
                    passive.RemovedFromQueue();
                }
                onRemovedFromQueue?.Invoke();
            }


            private void RunAsPassive()
            {
                var completeState = Progress();
                int i = 0;
                while (i < passiveCount)
                {
                    GetPassive(i++).RunAsPassive();
                }

                if (completeState.HasValue)
                {
                    if (!completeState.Value)
                    {
                        InternalFail();
                    }
                    else
                    {
                        InternalSuccess();
                    }
                }
            }

            public bool? RunAsRequirement()
            {
                bool? completeState;
                if (forceFailed)
                {
                    completeState = false;
                    InternalFail();
                }
                else if (forceSucceeded)
                {
                    completeState = true;
                    InternalSuccess();
                }
                else
                {
                    completeState = Progress();
                    if (completeState.HasValue)
                    {
                        if (!completeState.Value)
                        {
                            InternalFail();
                        }
                        else
                        {
                            InternalSuccess();
                        }
                    }
                }
                //run passives
                int i = 0;
                while (i < passiveCount)
                {
                    GetPassive(i++).RunAsPassive();
                }
                return completeState;
            }

            public void FireOnRegistered()
            {
                registered = true;
                RegisterAllPassives();

                successTime = null;
                failTime = null;
                onRegistered?.Invoke();
                Debug.Log("Registered " + name + "  " + self.gameObject.name);
            }
            public void FireOnUnregistered()
            {
                registered = false;
                UnregisterAllPassives();

                onUnregistered?.Invoke();
                Debug.Log("Unregistered " + name);
            }
            public abstract bool? Progress();

            public void FlagForFailure()
            {
                forceFailed = true;
            }

            public void FlagForSuccess()
            {
                if (!forceSucceeded)
                {
                    forceSucceeded = true;
                    onFlagForSuccess?.Invoke();
                }
            }

            private void InternalFail()
            {
                if (failTime.HasValue)
                {
                    return;
                }
                failTime = Time.time;
                onFail?.Invoke();
                if (!isAPassive)
                {
                    // Debug.LogError("[Autonomy] FAILED "+name);
                }
            }
            private void InternalSuccess()
            {
                if (successTime.HasValue)
                {
                    return;
                }
                successTime = Time.time;
                onSuccess?.Invoke();
                if (!isAPassive)
                {
                    // Debug.Log("[Autonomy] SUCCESS "+name);
                }
            }
        }

        private Task inProgress = null;
        private readonly List<Task> queue = new List<Task>();
        private bool hintBreakValidation = false;


        public Autonomy(Loli _self) : base(_self, Job.JobType.AUTONOMY)
        {
        }

        public void SetAutonomy(Task task)
        {
            if (task == null)
            {
                Debug.LogError("[Autonomy] Task is null!");
                return;
            }

            foreach (var oldQueueTask in queue)
            {
                oldQueueTask.RemovedFromQueue();
            }

            queue.Clear();
            queue.Add(task);
            // Debug.LogError("[Autonomy] "+self.name+" set to "+task.name);
        }

        public void RestartValidationHierarchy()
        {
            hintBreakValidation = true;
        }

        public void RemoveFromQueue(string name)
        {
            for (int i = 0; i < queue.Count; i++)
            {
                var task = queue[i];
                if (task.name == name)
                {
                    task.RemovedFromQueue();
                    queue.RemoveAt(i);
                    return;
                }
            }
        }

        public void Interrupt(Task task)
        {
            if (task == null)
            {
                Debug.LogError("[Autonomy] Task is null!");
                return;
            }
            foreach (var queuedTask in queue)
            {
                if (queuedTask.name == task.name && !queuedTask.finished)
                {
                    return;
                }
            }
            // Debug.LogError("[Autonomy] Interrupted with "+task.name);
            queue.Insert(0, task);

            if (inProgress != null)
            {
                UnregisterTask(inProgress);
                inProgress = null;
            }
        }

        private bool? ValidateHierarchy(Task task)
        {

            int i = 0;
            while (i < task.requirementCount)
            {
                var subCondition = task.GetRequirement(i++);
                bool? subCompleteState = ValidateHierarchy(subCondition);
                if (hintBreakValidation)
                {
                    return null;
                }
                if (subCompleteState.HasValue)
                {
                    if (!subCompleteState.Value)
                    {
                        return false;   //fail entire hierarchy
                    }
                }
                else
                {
                    return null;
                }
            }

            //register first incomplete task as main task
            var completeState = task.RunAsRequirement();
            if (hintBreakValidation)
            {
                return null;
            }
            if (!completeState.HasValue)
            {
                RegisterInProgress(task);
            }

            return completeState;
        }

        public void Progress()
        {
            hintBreakValidation = false;
            if (queue.Count > 0)
            {
                var task = queue[0];
                var completeState = ValidateHierarchy(task);
                if (completeState.HasValue)
                {
                    //remove from queue
                    queue.Remove(task);
                    task.RemovedFromQueue();
                    UnregisterInProgress();
                }
                else if (hintBreakValidation)
                {
                    UnregisterInProgress();
                }
            }
        }

        private void UnregisterTask(Task task)
        {
            if (task == null || !task.registered)
            {
                return;
            }
            self.onAnimationChange -= task.AnimationChange;
            self.onFixedUpdate -= task.FixedUpdate;
            self.onUpdate -= task.Update;
            self.onLateUpdate -= task.LateUpdate;
            self.onCharacterCollisionEnter -= task.CharacterCollisionEnter;
            self.onCharacterTriggerEnter -= task.CharacterTriggerEnter;
            self.RemoveModifyAnimationCallback(task.ModifyAnimation);
            task.FireOnUnregistered();
        }

        private void RegisterTask(Task task)
        {
            if (task == null || task.registered)
            {
                return;
            }
            self.onAnimationChange += task.AnimationChange;
            self.onFixedUpdate += task.FixedUpdate;
            self.onUpdate += task.Update;
            self.onLateUpdate += task.LateUpdate;
            self.onCharacterCollisionEnter += task.CharacterCollisionEnter;
            self.onCharacterTriggerEnter += task.CharacterTriggerEnter;
            self.AddModifyAnimationCallback(task.ModifyAnimation);
            task.FireOnRegistered();
        }

        private void UnregisterInProgress()
        {
            if (inProgress != null)
            {
                UnregisterTask(inProgress);
                inProgress = null;
            }
        }

        private void RegisterInProgress(Task task)
        {
            if (task != null && task != inProgress)
            {
                if (inProgress != null)
                {
                    UnregisterInProgress();
                }
                RegisterTask(task);
                inProgress = task;
            }
        }
    }

}