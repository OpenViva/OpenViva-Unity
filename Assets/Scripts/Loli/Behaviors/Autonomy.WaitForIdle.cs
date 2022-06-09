namespace viva
{


    public partial class AutonomyWaitForIdle : Autonomy.Task
    {

        private bool waiting = true;

        public AutonomyWaitForIdle(Autonomy _autonomy, string _name) : base(_autonomy, _name)
        {

            onRegistered += delegate { waiting = true; Reset(); };
        }

        public override bool? Progress()
        {
            if (self.IsCurrentAnimationIdle() || !waiting)
            {
                waiting = false;
                return true;
            }
            else
            {
                self.OverrideClearAnimationPriority();
                self.SetTargetAnimation(self.GetLastReturnableIdleAnimation());
            }
            return null;
        }

    }

}