namespace viva
{


    public partial class AutonomyGive : Autonomy.Task
    {

        private Character targetCharacter;
        private Item targetItem;
        private AutonomyMoveTo chase;
        private AutonomyFaceDirection faceTarget;
        private AutonomyPlayAnimation playGive;
        private bool itemIsOnRight;


        public AutonomyGive(Autonomy _autonomy, string _name, Character _targetCharacter, Item _targetItem) : base(_autonomy, _name)
        {
            targetCharacter = _targetCharacter;
            targetItem = _targetItem;

            if (targetItem == null || targetItem.mainOccupyState == null || targetItem.mainOccupyState.owner != self)
            {
                return;
            }
            itemIsOnRight = targetItem.mainOccupyState.rightSide;

            chase = new AutonomyMoveTo(_autonomy, _name + " move to", ReadTargetCharacterPos, 1.0f, BodyState.STAND);
            AddRequirement(chase);

            faceTarget = new AutonomyFaceDirection(_autonomy, _name + " give", ReadTargetCharacterPos, 1.0f, 25.0f);
            AddRequirement(faceTarget);

            playGive = new AutonomyPlayAnimation(autonomy, _name + " play anim", GetGiveAnimation());
            playGive.onAnimationExit += delegate { EndGive(false, false); };    //time out for accepting item
            playGive.onRegistered += delegate
            {
                new BlendController(targetItem.mainOccupyState as LoliHandState, playGive.entryAnimation, ModifyArmGiveTo);
            };
            playGive.onUnregistered += PlayReachEndAnimation;
            AddRequirement(playGive);

            //item owner change callbacks
            targetItem.onMainOccupyStateChanged += OnItemOccupyStateChanged;
            onRemovedFromQueue += delegate { targetItem.onMainOccupyStateChanged -= OnItemOccupyStateChanged; };

            onFlagForSuccess += delegate
            {
                //permanent success
                chase.FlagForSuccess();
                faceTarget.FlagForSuccess();
                playGive.FlagForSuccess();
            };
        }

        private void ReadTargetCharacterPos(TaskTarget target)
        {
            if (targetCharacter == null)
            {
                return;
            }
            target.SetTargetItem(targetCharacter.headItem);
        }

        private void EndGive(bool accepted, bool playExitAnim)
        {
            if (!finished)
            {
                if (playExitAnim)
                {
                    PlayReachEndAnimation();
                }

                if (accepted)
                {
                    FlagForSuccess();
                }
                else
                {
                    FlagForFailure();
                }
            }
        }

        private void OnItemOccupyStateChanged(OccupyState oldOccupyState, OccupyState newOccupyState)
        {
            if (targetCharacter == null)
            {
                return;
            }
            //succeed if new owner is the intended target owner
            EndGive(newOccupyState != null && newOccupyState.owner == targetCharacter, true);
        }

        private void PlayReachEndAnimation()
        {
            if (!finished)
            {
                var exitAnim = new AutonomyPlayAnimation(autonomy, name + " play exit anim", GetGiveEndAnimation());
                PrependRequirement(exitAnim);
            }
        }

        public override bool? Progress()
        {
            if (targetItem == null || targetCharacter == null)
            {
                return false;
            }
            //if was handed to targetCharacter
            if (targetItem.mainOwner == targetCharacter)
            {
                return true;
            }
            //if not holding item anymore
            if (targetItem.mainOwner != self)
            {
                return false;
            }
            return null;
        }

        private float ModifyArmGiveTo(BlendController blendController)
        {
            return -1.0f;
        }

        private Loli.Animation GetGiveEndAnimation()
        {
            switch (self.bodyState)
            {
                case BodyState.STAND:
                    if (itemIsOnRight)
                    {
                        return Loli.Animation.STAND_REACH_OUT_END_RIGHT;
                    }
                    else
                    {
                        return Loli.Animation.STAND_REACH_OUT_END_LEFT;
                    }
            }
            return Loli.Animation.NONE;
        }

        private Loli.Animation GetGiveAnimation()
        {
            switch (self.bodyState)
            {
                case BodyState.STAND:
                    if (itemIsOnRight)
                    {
                        return Loli.Animation.STAND_REACH_OUT_RIGHT;
                    }
                    else
                    {
                        return Loli.Animation.STAND_REACH_OUT_LEFT;
                    }
            }
            return Loli.Animation.NONE;
        }
    }

}