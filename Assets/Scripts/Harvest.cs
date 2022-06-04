using UnityEngine;

namespace viva
{


    public partial class Harvest : Item
    {


        [SerializeField]
        private bool m_isHarvested = false;
        [VivaFileAttribute]
        public bool isHarvested { get { return m_isHarvested; } protected set { m_isHarvested = value; } }


        protected override void OnItemAwake()
        {
            base.OnItemAwake();
            UpdateHarvestStatus();
        }

        private void UpdateHarvestStatus()
        {
            //onec a Harvest item has been picked up, it is no longer highly desirable (harvested)
            if (isHarvested && HasPickupReason(PickupReasons.HIGHLY_DESIRABLE))
            {
                ClearPickupReason(PickupReasons.HIGHLY_DESIRABLE);
            }
        }

        public override void OnPostPickup()
        {
            base.OnPostPickup();
            m_isHarvested = true;
            UpdateHarvestStatus();
            if (settings.itemType == Item.Type.WHEAT_SPIKE)
            {
                TutorialManager.main.DisplayObjectHint(this, "hint_wheatIntoMortar");
            }
        }
    }

}