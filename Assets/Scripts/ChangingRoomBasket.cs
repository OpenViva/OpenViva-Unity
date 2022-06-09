using UnityEngine;


namespace viva
{

    public class ChangingRoomBasket : VivaSessionAsset
    {

        [SerializeField]
        private Outfit m_outfit = null;
        [VivaFileAttribute]
        public Outfit outfit { get { return m_outfit; } protected set { m_outfit = value; } }


        public void SetDisposedOutfit(Outfit _outfit)
        {
            if (_outfit == null)
            {
                if (outfit != null)
                {
                    outfit = null;
                }
            }
            else
            {
                if (outfit == null)
                {
                    outfit = _outfit;
                }
            }
        }
    }

}