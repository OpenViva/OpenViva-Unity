using UnityEngine;


namespace viva
{

    [CreateAssetMenu(fileName = "Physics Sound Settings", menuName = "Logic/Physics Sound Settings", order = 1)]
    public partial class PhysicsSoundSettings : ScriptableObject
    {

        [SerializeField]
        public float softMinVel = 1.0f;
        [SerializeField]
        public float hardMinVel = 8.0f;
        [SerializeField]
        public float softMinPitch = 0.75f;
        [SerializeField]
        public float softMaxPitch = 1.0f;
        [SerializeField]
        public float dragMinVel = 0.07f;
        [SerializeField]
        public float dragMaxVel = 0.3f;
        [SerializeField]
        public float dragMinPitch = 0.5f;
        [SerializeField]
        public float dragMaxPitch = 1.2f;
        [SerializeField]
        public float dragMaxVolumeVel = 2.0f;
    }

}