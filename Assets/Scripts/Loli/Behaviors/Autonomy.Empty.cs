namespace viva
{


    public partial class AutonomyEmpty : Autonomy.Task
    {

        public delegate bool? ProgressCallback();

        private readonly ProgressCallback onProgress;

        public AutonomyEmpty(Autonomy _autonomy, string _name, ProgressCallback _onProgress = null) : base(_autonomy, _name)
        {
            onProgress = _onProgress;
        }

        public override bool? Progress()
        {
            if (onProgress != null)
            {
                return onProgress();
            }
            return null;
        }

    }

}