namespace viva
{


    public abstract class AutonomyItemMove : Autonomy.Task
    {

        public AutonomyWaitForIdle waitForIdle { get; private set; }
        public AutonomyMoveTo moveTo { get; private set; }
        public AutonomyFaceDirection faceTarget { get; private set; }
        public AutonomyPlayAnimation playTargetAnim { get; private set; }
        private bool checkPlayAgain = true;

        //item must remain in the same initial occupyState to be a valid task
        public AutonomyItemMove(Autonomy _autonomy, string _name) : base(_autonomy, _name)
        {

            waitForIdle = new AutonomyWaitForIdle(_autonomy, _name + " wait for idle");
            AddRequirement(waitForIdle);

            moveTo = new AutonomyMoveTo(_autonomy, _name + " move to", ReadTargetLocation, self.GetBodyStatePropertyValue(PropertyValue.PICKUP_DISTANCE), BodyState.STAND);
            moveTo.onRegistered += delegate
            {
                moveTo.distance = self.bodyStateAnimationSets[(int)moveTo.preferredBodyState].propertyValues[PropertyValue.PICKUP_DISTANCE];
            };
            AddRequirement(moveTo);

            faceTarget = new AutonomyFaceDirection(_autonomy, _name + " item face dir", ReadTargetLocation);
            AddRequirement(faceTarget);

            playTargetAnim = new AutonomyPlayAnimation(self.autonomy, _name + " play anim", Loli.Animation.NONE);
            playTargetAnim.onRegistered += CheckIfShouldPlayAgain;
            AddRequirement(playTargetAnim);

            onFlagForSuccess += delegate
            {
                viva.DevTools.LogExtended("running onFlagForSuccess", true, true);
                waitForIdle.FlagForSuccess();
                moveTo.FlagForSuccess();
                faceTarget.FlagForSuccess();
                playTargetAnim.FlagForSuccess();
                checkPlayAgain = false;
            };
        }

        protected abstract void ReadTargetLocation(TaskTarget target);
        protected abstract Loli.Animation GetTargetAnimation();

        protected void CheckIfShouldPlayAgain()
        {
            if (!checkPlayAgain)
            {
                return;
            }
            playTargetAnim.OverrideAnimations(GetTargetAnimation());
            playTargetAnim.Reset();
        }
    }

}