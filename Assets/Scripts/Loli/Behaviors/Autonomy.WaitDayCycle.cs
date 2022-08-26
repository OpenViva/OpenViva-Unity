namespace viva
{


    public partial class AutonomyWaitDayCycle : Autonomy.Task
    {

        private float duration;
        private float dayCycleStarted;
        public bool loop = false;

        public AutonomyWaitDayCycle(Autonomy _autonomy, string _name, float _duration) : base(_autonomy, _name)
        {
            duration = _duration;

            onRegistered += delegate { dayCycleStarted = GameDirector.skyDirector.worldTime; };
        }

        public override bool? Progress()
        {
            if (GameDirector.skyDirector.worldTime - dayCycleStarted > duration)
            {
                if (loop)
                {
                    dayCycleStarted = GameDirector.skyDirector.worldTime;
                    Reset();
                }
                return true;
            }
            return null;
        }

    }

}