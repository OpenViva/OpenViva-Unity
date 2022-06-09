using UnityEngine;

namespace viva
{


    public class ChangingRoom : Mechanism
    {

        [SerializeField]
        private ChangingRoomBasket[] baskets;


        public ChangingRoomBasket GetNextEmptyBasket()
        {
            foreach (var basket in baskets)
            {
                if (basket.outfit == null)
                {
                    return basket;
                }
            }
            return null;
        }

        public override bool AttemptCommandUse(Loli targetLoli, Character commandSource)
        {
            if (targetLoli == null)
            {
                return false;
            }
            return targetLoli.active.onsenSwimming.AttemptChangeOutOfSwimmingClothes();
        }

        public override void EndUse(Character targetCharacter)
        {

        }
    }

}