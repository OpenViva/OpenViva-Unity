using System.Collections.Generic;


namespace viva
{


    public class ScaredBehaviour : PassiveBehaviors.PassiveTask
    {

        public bool scared { get; private set; }
        private List<Loli.Animation> oldHappyIdleAnimationSetList;
        private List<Loli.Animation> oldTiredIdleAnimationSetList;
        private List<Loli.Animation> oldAngryIdleAnimationSetList;

        public ScaredBehaviour(Loli _self) : base(_self, 0.0f)
        {
        }

        public void Scare(float duration)
        {

            if (!scared)
            {
                scared = true;

                var scareTimer = new AutonomyWait(self.autonomy, "scare timer", duration);
                scareTimer.onRemovedFromQueue += FinishScared;

                switch (self.bodyState)
                {
                    case BodyState.SQUAT:
                    case BodyState.RELAX:
                    case BodyState.SLEEP_PILLOW_UP:
                    case BodyState.SLEEP_PILLOW_SIDE_RIGHT:
                    case BodyState.SLEEP_PILLOW_SIDE_LEFT:
                    case BodyState.AWAKE_PILLOW_UP:
                    case BodyState.AWAKE_PILLOW_SIDE_RIGHT:
                    case BodyState.AWAKE_PILLOW_SIDE_LEFT:
                        self.BeginRagdollMode(0.5f, Loli.Animation.FLOOR_CURL_LOOP);
                        break;
                    case BodyState.STAND:
                    case BodyState.STANDING_HUG:
                        var playScareAnim = new AutonomyPlayAnimation(self.autonomy, "play scare anim", Loli.Animation.STAND_SCARED_STARTLE);
                        scareTimer.AddRequirement(playScareAnim);
                        break;
                }

                self.autonomy.Interrupt(scareTimer);

                var bodyStateAnimationSet = self.bodyStateAnimationSets[(int)BodyState.STAND];
                oldHappyIdleAnimationSetList = bodyStateAnimationSet.GetAnimationSetList(AnimationSet.IDLE_HAPPY);
                oldTiredIdleAnimationSetList = bodyStateAnimationSet.GetAnimationSetList(AnimationSet.IDLE_TIRED);
                oldAngryIdleAnimationSetList = bodyStateAnimationSet.GetAnimationSetList(AnimationSet.IDLE_ANGRY);

                bodyStateAnimationSet.SetAnimationSetList(AnimationSet.IDLE_HAPPY, new List<Loli.Animation>() { Loli.Animation.STAND_SCARED_LOCOMOTION });
                bodyStateAnimationSet.SetAnimationSetList(AnimationSet.IDLE_TIRED, new List<Loli.Animation>() { Loli.Animation.STAND_SCARED_LOCOMOTION });
                bodyStateAnimationSet.SetAnimationSetList(AnimationSet.IDLE_ANGRY, new List<Loli.Animation>() { Loli.Animation.STAND_SCARED_LOCOMOTION });
            }
        }

        private void FinishScared()
        {
            scared = false;

            var bodyStateAnimationSet = self.bodyStateAnimationSets[(int)BodyState.STAND];
            bodyStateAnimationSet.SetAnimationSetList(AnimationSet.IDLE_HAPPY, oldHappyIdleAnimationSetList);
            bodyStateAnimationSet.SetAnimationSetList(AnimationSet.IDLE_TIRED, oldTiredIdleAnimationSetList);
            bodyStateAnimationSet.SetAnimationSetList(AnimationSet.IDLE_ANGRY, oldAngryIdleAnimationSetList);

            self.SetTargetAnimation(self.GetLastReturnableIdleAnimation());
        }
    }

}