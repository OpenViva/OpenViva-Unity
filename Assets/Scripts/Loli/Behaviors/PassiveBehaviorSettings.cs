using UnityEngine;


namespace viva
{


    [System.Serializable]
    [CreateAssetMenu(fileName = "passiveBehaviorSettings", menuName = "Logic/Passive Behavior Settings", order = 1)]
    public class PassiveBehaviorSettings : ScriptableObject
    {

        [SerializeField]
        public PhysicMaterial hugPlayerLoliPhysicsMaterial;
        [Range(0.01f, 0.8f)]
        [SerializeField]
        public float hugPlayerHeadMaxProximityDistance = 0.3f;
        [Range(0.01f, 0.5f)]
        [SerializeField]
        public float hugPlayerHeadMinProximityDistance = 0.3f;
        [SerializeField]
        public float hugPlayerPitchProximityOffset = 0.0f;
        [SerializeField]
        public float hugPlayerRollProximityOffset = 0.0f;
        [SerializeField]
        public PhysicMaterial lolibasePhysicsMaterial;
        [SerializeField]
        public float hugPlayerLoliDrag = 50.0f;
        [SerializeField]
        public float hugPlayerLoliAngularDrag = 30.0f;
        [SerializeField]
        public float hugPlayerDistance = 1.0f;
        [SerializeField]
        public float hugPlayerMinDistance = 0.2f;
        [SerializeField]
        public float hugPlayerMaxDistance = 0.4f;
        [SerializeField]
        public float hugPlayerSmoothLerpSpeed = 2.0f;
        [SerializeField]
        public float hugPlayerRotationForce = 32.0f;
        [SerializeField]
        public float hugPlayerAnimSideMinDistance = 0.3f;
        [SerializeField]
        public float hugPlayerAnimSideMaxDistance = 0.3f;
        [SerializeField]
        public Texture2D globalDirtTexture = null;
    }

}