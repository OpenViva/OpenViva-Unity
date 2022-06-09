using UnityEngine;

namespace viva
{


    [CreateAssetMenu(fileName = "Item Settings", menuName = "Logic/Item Settings", order = 1)]
    public class ItemSettings : ScriptableObject
    {

        [SerializeField]
        private bool m_usePickupAnimation = true;
        public bool usePickupAnimation { get { return m_usePickupAnimation; } }

        [Header("Player Info")]
        [SerializeField]
        private Player.Animation m_playerHeldAnimation = Player.Animation.IDLE;
        public Player.Animation playerHeldAnimation { get { return m_playerHeldAnimation; } }

        [Header("Loli Info")]
        [SerializeField]
        private Loli.HoldFormAnimation m_loliHeldAnimation;
        public Loli.HoldFormAnimation loliHeldAnimation { get { return m_loliHeldAnimation; } }
        [SerializeField]
        private Vector3 m_IKHoldTarget = new Vector3(0.12f, 0.06f, 0.17f);
        public Vector3 IKTarget { get { return m_IKHoldTarget; } }
        [SerializeField]
        private Vector3 m_IKHoldPole = new Vector3(0.18f, 0.06f, -0.45f);
        public Vector3 IKPole { get { return m_IKHoldPole; } }
        [SerializeField]
        private Vector3 m_IKHoldEuler = new Vector3(177.28f, -99.4f, -61.8f);
        public Vector3 IKHandEuler { get { return m_IKHoldEuler; } }
        [SerializeField]
        private float m_ikBlendStrength = 0.65f;
        public float ikBlendStrength { get { return m_ikBlendStrength; } }

        [Header("General")]
        [SerializeField]
        private Vector3 m_inertia = Vector3.one * 0.1f;
        public Vector3 inertia { get { return m_inertia; } }
        [SerializeField]
        private Vector3 m_centerMass = Vector3.zero;
        public Vector3 centerMass { get { return m_centerMass; } }
        [SerializeField]
        private bool m_allowMultipleOwners = false;
        public bool allowMultipleOwners { get { return m_allowMultipleOwners; } }
        [SerializeField]
        private float m_heldMassScale = 10.0f;
        public float heldMassScale { get { return m_heldMassScale; } }
        [SerializeField]
        private Vector3 m_holdRotationOffset = new Vector3(0, 0, 0);
        public Vector3 holdRotationOffset { get { return m_holdRotationOffset; } }
        [SerializeField]
        private bool m_allowChangeOwner = false;
        public bool allowChangeOwner { get { return m_allowChangeOwner; } }
        [SerializeField]
        private bool m_permanentlyEnabled = false;
        public bool permanentlyEnabled { get { return m_permanentlyEnabled; } }
        [SerializeField]
        private Item.Type m_itemType = Item.Type.NONE;
        public Item.Type itemType { get { return m_itemType; } }
        [SerializeField]
        private Texture2D m_pickupTexture = null;
        public Texture2D pickupTexture { get { return m_pickupTexture; } }
        [SerializeField]
        private SoundSet m_pickupSound = null;
        public SoundSet pickupSound { get { return m_pickupSound; } }
        [SerializeField]
        private int m_pickupReasons = 0;
        public int pickupReasons { get { return m_pickupReasons; } }
        [SerializeField]
        private int m_attributes = 0;
        public int attributes { get { return m_attributes; } }
        [SerializeField]
        private bool m_toggleHolding = true;
        public bool toggleHolding { get { return m_toggleHolding; } }
        [SerializeField]
        private Vector3 m_iconEulerAngles = Vector3.zero;
        public Vector3 iconEulerAngles { get { return m_iconEulerAngles; } }
        [SerializeField]
        private bool m_pickupAnimMaintainsYaw = false;
        public bool pickupAnimMaintainsYaw { get { return m_pickupAnimMaintainsYaw; } }
        [Header("Water")]
        [SerializeField]
        private float m_buoyancySphereRadius = 0.0f;
        public float buoyancySphereRadius { get { return m_buoyancySphereRadius; } }
        [SerializeField]
        private Vector3 m_buoyancyEuler = Vector3.zero;
        public Vector3 buoyancyEuler { get { return m_buoyancyEuler; } }
        [Range(1.0f, 16.0f)]
        [SerializeField]
        private float m_buoyancyStrength = 2.0f;
        public float buoyancyStrength { get { return m_buoyancyStrength; } }
        [SerializeField]
        private SoundSet m_splashSounds;
        public SoundSet splashSounds { get { return m_splashSounds; } }

    }

}