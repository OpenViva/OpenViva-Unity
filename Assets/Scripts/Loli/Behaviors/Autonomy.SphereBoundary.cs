namespace viva
{


    public partial class AutonomySphereBoundary : Autonomy.Task
    {

        private TaskTarget.ReadTargetCallback readSourceLocation;
        private TaskTarget.ReadTargetCallback readTargetLocation;
        private TaskTarget source;
        private TaskTarget target;
        private float radius;
        private float timeStarted;
        private bool inside = false;

        public AutonomySphereBoundary(Autonomy _autonomy, TaskTarget.ReadTargetCallback _readSourceLocation, TaskTarget.ReadTargetCallback _readTargetLocation, float _radius) : base(_autonomy, "sphere bound")
        {
            readSourceLocation = _readSourceLocation;
            readTargetLocation = _readTargetLocation;
            radius = _radius;

            target = new TaskTarget(self);
            source = new TaskTarget(self);
        }

        public override bool? Progress()
        {

            source.lastReadPos = null;
            readSourceLocation?.Invoke(source);
            target.lastReadPos = null;
            readTargetLocation?.Invoke(target);
            if (!target.lastReadPos.HasValue || !source.lastReadPos.HasValue)
            {
                return false;
            }
            float sqRadius = radius + (inside ? LocomotionBehaviors.minCornerDist : 0.0f);
            inside = AutonomyMoveTo.GetFlatSqDist(target.lastReadPos.Value, source.lastReadPos.Value) < sqRadius * sqRadius;
            if (inside)
            {
                return true;
            }
            return null;
        }

    }

}