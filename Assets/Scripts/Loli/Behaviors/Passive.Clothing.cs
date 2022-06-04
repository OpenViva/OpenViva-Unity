using UnityEngine;


namespace viva
{


    public class ClothingBehavior : PassiveBehaviors.PassiveTask
    {

        public ClothingBehavior(Loli _self) : base(_self, Mathf.Infinity)
        {
        }

        public void AttemptReactToOutfitChange()
        {
            self.SetTargetAnimation(Loli.Animation.STAND_OUTFIT_LIKE);
        }
    }

}