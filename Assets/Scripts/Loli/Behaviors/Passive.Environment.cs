using UnityEngine;


namespace viva
{


    public class EnvironmentBehavior : PassiveBehaviors.PassiveTask
    {

        private Loli.Animation initiateListenEntryAnim = Loli.Animation.NONE;

        public EnvironmentBehavior(Loli _self) : base(_self, Mathf.Infinity)
        {
        }

        public bool AttemptReactToSubstanceSpill(SubstanceSpill.Substance substanceType, Vector3 particlePosition)
        {

            //random but still bearing dependent animation variety
            bool sourceSide = Tools.Bearing(self.transform, particlePosition) - 10.0f + UnityEngine.Random.value * 20.0f > 0.0f;
            Loli.Animation reactAnim = GetAvailableSplashedReactAnim(sourceSide);
            if (reactAnim == Loli.Animation.NONE)
            {   //no available animation to react with
                return false;
            }
            if (self.currentAnim != reactAnim && self.targetAnim != reactAnim)
            {
                initiateListenEntryAnim = reactAnim;
            }
            if (substanceType == SubstanceSpill.Substance.FLOUR)
            {
                GameDirector.player.CompleteAchievement(Player.ObjectiveType.POUR_FLOUR_ON_HEAD);
            }

            Vector3 forceDir = self.floorPos - particlePosition;
            forceDir.y = 0.0f;
            self.locomotion.PlayForce(forceDir.normalized, 0.8f);
            
            var playReactAnim = new AutonomyPlayAnimation(self.autonomy, "spill react animation", reactAnim);
            var faceSubstance = new AutonomyFaceDirection(self.autonomy, "face substance", delegate (TaskTarget target)
            { target.SetTargetPosition(particlePosition); }, 2.0f);
            playReactAnim.onRegistered += delegate
            {
                self.leftHandState.AttemptDrop();
                self.rightHandState.AttemptDrop();
            };
            playReactAnim.AddPassive(faceSubstance);

            self.autonomy.Interrupt(playReactAnim);
            return true;
        }

        private Loli.Animation GetAvailableSplashedReactAnim(bool sourceSide)
        {
            switch (self.bodyState)
            {
                case BodyState.STAND:
                    switch (self.currentAnim)
                    {
                        case Loli.Animation.STAND_SPLASHED_START_RIGHT:
                        case Loli.Animation.STAND_SPLASHED_START_LEFT:
                            return Loli.Animation.STAND_SPLASHED_LOOP;
                        default:
                            if (sourceSide)
                            {
                                return Loli.Animation.STAND_SPLASHED_START_RIGHT;
                            }
                            else
                            {
                                return Loli.Animation.STAND_SPLASHED_START_LEFT;
                            }
                    }
                case BodyState.BATHING_IDLE:
                    if (sourceSide)
                    {
                        return Loli.Animation.BATHTUB_SPLASH_REACT_RIGHT;
                    }
                    else
                    {
                        return Loli.Animation.BATHTUB_SPLASH_REACT_LEFT;
                    }
            }
            return Loli.Animation.NONE;
        }

        public override void OnAnimationChange(Loli.Animation oldAnim, Loli.Animation newAnim)
        {
            if (newAnim == initiateListenEntryAnim)
            {
                if (UnityEngine.Random.value > 0.6f)
                {

                    self.ShiftHappiness(-1);
                }
                //initiate logic hook
            }
        }
    }

}