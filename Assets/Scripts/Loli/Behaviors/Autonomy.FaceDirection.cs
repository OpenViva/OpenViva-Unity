using UnityEngine;


namespace viva
{


    public class TaskTarget
    {
        public delegate void ReadTargetCallback(TaskTarget target);

        public enum TargetType
        {
            WORLD_POSITION,
            ITEM_POSITION,
            IN_HIERARCHY,
            TRANSFORM,
            CHARACTER,
            RIGIDBODY
        }

        private readonly Loli self;
        public Vector3? lastReadPos = null;

        public object target { get; private set; }
        public TargetType type { get; private set; } = TargetType.WORLD_POSITION;

        public TaskTarget(Loli _self)
        {
            self = _self;
        }

        public void SetTargetPosition(Vector3? pos)
        {
            type = TargetType.WORLD_POSITION;
            lastReadPos = pos;
        }

        public void SetTargetCharacter(Character character)
        {
            type = TargetType.CHARACTER;
            target = character;
        }

        public void SetTargetRigidBody(Rigidbody rigidBody)
        {
            type = TargetType.RIGIDBODY;
            target = rigidBody;
        }

        /// <summary>Assigns a transform to track its world position.</summary>
        /// <param name="newTargetTransform">The new target transform. You can pass null to disable the entire TaskTarget.</param>
        public void SetTargetTransform(Transform newTargetTransform)
        {
            type = TargetType.TRANSFORM;
            target = newTargetTransform;
        }

        public void SetTargetItem(Item targetItem)
        {
            //check that target is not part of self hierarchy (would caus spinning in place)
            if (targetItem == null)
            {
                lastReadPos = null; //count as complete
                return;
            }
            if (targetItem.mainOwner == self)
            {
                lastReadPos = null; //count as complete
                type = TargetType.IN_HIERARCHY;
            }
            else
            {
                lastReadPos = targetItem.transform.position + Vector3.up * 0.025f;
                type = TargetType.ITEM_POSITION;
            }
        }

        public Vector3? Read()
        {
            switch (type)
            {
                case TargetType.WORLD_POSITION:
                    return lastReadPos;
                case TargetType.TRANSFORM:
                    var targetTransform = target as Transform;
                    if (targetTransform)
                    {
                        lastReadPos = targetTransform.position;
                    }
                    else
                    {
                        lastReadPos = null;
                    }
                    return lastReadPos;
                case TargetType.RIGIDBODY:
                    var targetRigidBody = target as Rigidbody;
                    if (targetRigidBody)
                    {
                        lastReadPos = targetRigidBody.worldCenterOfMass;
                    }
                    else
                    {
                        lastReadPos = null;
                    }
                    return lastReadPos;
                default:
                    return null;
            }
        }
    }


    public class AutonomyFaceDirection : Autonomy.Task
    {


        private readonly float durationRequired = 0.35f;
        private readonly float minSuccessBearing;
        private readonly float faceYawEaseRange = 25.0f;

        public TaskTarget.ReadTargetCallback readTargetCallback;
        private float duration = 0.0f;
        private float faceYawAcc;
        private float faceYawMaxVel;
        private Vector3 rootDirEuler = Vector3.zero;
        private float faceYawVelocity = 0.0f;
        private TaskTarget target;


        public AutonomyFaceDirection(Autonomy _autonomy, string _name, TaskTarget.ReadTargetCallback _readTargetCallback, float speedMultiplier = 1.0f, float _minSuccessBearing = 10.0f) : base(_autonomy, _name)
        {
            readTargetCallback = _readTargetCallback;
            faceYawAcc = 20.0f * speedMultiplier;
            faceYawMaxVel = 200 * speedMultiplier;
            rootDirEuler.y = self.anchor.eulerAngles.y;
            minSuccessBearing = _minSuccessBearing;
            target = new TaskTarget(autonomy.self);

            onModifyAnimation += OnModifyAnimation;
            onRegistered += delegate { duration = 0.0f; };
        }

        public override bool? Progress()
        {
            if (readTargetCallback == null)
            {
                return false;
            }
            target.lastReadPos = null;
            readTargetCallback(target);
            if (target.type == TaskTarget.TargetType.IN_HIERARCHY)
            {
                return true;
            }
            if (!target.lastReadPos.HasValue)
            {
                return false;
            }
            //never finish if passive
            if (isAPassive)
            {
                return null;
            }
            Vector3 readPos = ConstrainFromFloorPos(target.lastReadPos.Value);
            if (Mathf.Abs(Tools.Bearing(self.anchor, readPos)) > minSuccessBearing)
            {
                return null;
            }
            //must face target direction for a specified duration
            if (duration >= durationRequired)
            {
                return true;
            }
            else
            {
                return null;
            }
        }

        private Vector3 ConstrainFromFloorPos(Vector3 p)
        {
            Vector3 diff = p - self.floorPos;
            diff.y = 0.0f;
            return self.floorPos + diff.normalized;
        }

        public void OnModifyAnimation()
        {

            if (!target.lastReadPos.HasValue)
            {
                return;
            }
            Vector3 readPos = ConstrainFromFloorPos(target.lastReadPos.Value);
            Debug.DrawLine(self.floorPos, readPos, Color.magenta, 0.1f);
            float bearing = Tools.Bearing(self.anchor, readPos);
            if (self.faceYawDisableSum > 0)
            {   //disable if sum is greater than 1
                faceYawVelocity *= Mathf.Pow(0.8f, self.animationDelta * 10.0f);
            }
            else
            {
                faceYawVelocity += Mathf.Sign(bearing) * faceYawAcc * self.animationDelta;

                float absBearing = Mathf.Abs(bearing);
                float absMaxVelocity = faceYawMaxVel * self.animationDelta;
                if (absBearing < faceYawEaseRange)
                {
                    float ratio = 1.0f - absBearing / faceYawEaseRange;
                    absMaxVelocity *= 1.0f - ratio * ratio;
                    duration += self.animationDelta;
                }
                else
                {
                    duration = 0.0f;
                }
                faceYawVelocity = Mathf.Clamp(faceYawVelocity, -absMaxVelocity, absMaxVelocity);
            }
            if (bearing > 0.0f)
            {
                if (bearing + faceYawVelocity <= 0.0f)
                {
                    faceYawVelocity = bearing;
                }
            }
            else if (bearing + faceYawVelocity >= 0.0f)
            {
                faceYawVelocity = bearing;
            }
            rootDirEuler.y = self.anchor.eulerAngles.y + faceYawVelocity;
            self.anchor.eulerAngles = rootDirEuler;
        }
    }

}