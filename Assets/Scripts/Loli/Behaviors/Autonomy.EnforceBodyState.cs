using System.Collections.Generic;


namespace viva
{


    public class AutonomyEnforceBodyState : Autonomy.Task
    {

        public BodyState targetBodyState { get; private set; }
        private List<Loli.Animation> transferAnims = new List<Loli.Animation>();


        public AutonomyEnforceBodyState(Autonomy _autonomy, string _name, BodyState _targetBodyState) : base(_autonomy, _name)
        {
            targetBodyState = _targetBodyState;

            onFixedUpdate += OnFixedUpdate;
            onRegistered += RecalculateTransferAnims;
            onRegistered += delegate { self.OnBodyStateChanged += OnBodyStateChanged; };
            onUnregistered += delegate { self.OnBodyStateChanged -= OnBodyStateChanged; };
        }

        public void SetTargetBodyState(BodyState _targetBodyState)
        {
            if (targetBodyState == _targetBodyState)
            {
                return;
            }
            targetBodyState = _targetBodyState;
            RecalculateTransferAnims();
        }

        private void OnBodyStateChanged(BodyState oldBodyState, BodyState newBodyState)
        {
            RecalculateTransferAnims();
        }

        private void RecalculateTransferAnims()
        {
            Loli.FindBodyStatePath(self, transferAnims, self.bodyState, targetBodyState);
        }

        public override bool? Progress()
        {
            if (self.bodyState == targetBodyState)
            {
                return true;
            }
            return null;
        }

        public void OnFixedUpdate()
        {
            if (transferAnims.Count == 0)
            {
                return;
            }
            if (self.IsCurrentAnimationIdle())
            {

                var exitAnim = transferAnims[0];
                if (exitAnim != Loli.Animation.NONE)
                {
                    self.SetTargetAnimation(exitAnim);
                }
            }
        }
    }

}