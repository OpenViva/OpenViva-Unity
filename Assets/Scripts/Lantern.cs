using UnityEngine;


namespace viva
{


    public class Lantern : Item
    {
        [SerializeField]
        private MeshRenderer lanternBaseMR;
        [SerializeField]
        private Material lanternOff;
        [SerializeField]
        private Material lanternOn;
        [SerializeField]
        private GameObject lightContainer;
        [SerializeField]
        private GameObject lanternBase;
        [SerializeField]
        private bool m_on = false;
        [VivaFileAttribute]
        public bool on { get { return m_on; } protected set { m_on = value; } }
        [SerializeField]
        private HingeJoint lanternBaseHingeJoint;

        protected override void OnItemAwake()
        {
            UpdateLightState();
        }

        public override void OnPostPickup()
        {
            lanternBase.transform.SetParent(null, true);
        }

        public override void OnItemLateUpdatePostIK()
        {

            PlayerHandState heldState = mainOccupyState as PlayerHandState;
            if (heldState != null && heldState.actionState.isDown)
            {
                on = !on;
                UpdateLightState();
            }
        }

        private void UpdateLightState()
        {
            lightContainer.SetActive(m_on);
            var mats = lanternBaseMR.materials;
            if (m_on)
            {
                mats[1] = lanternOn;
            }
            else
            {
                mats[1] = lanternOff;
            }
            lanternBaseMR.materials = mats;
        }
        protected override void OnItemEnable()
        {
            if (lanternBase)
            {
                lanternBase.SetActive(true);
            }
        }
        protected override void OnItemDisable()
        {
            if (lanternBase)
            {
                lanternBase.SetActive(false);
            }
        }
    }

}