using UnityEngine;


namespace viva
{

    [System.Serializable]
    [CreateAssetMenu(fileName = "LoliSettings", menuName = "Logic/Loli Settings", order = 1)]
    public class LoliSettings : ScriptableObject
    {

        [SerializeField]
        private SoundSet m_bodyImpactSoftSound;
        public SoundSet bodyImpactSoftSound { get { return m_bodyImpactSoftSound; } }
        [SerializeField]
        private SoundSet m_bodyImpactHardSound;
        public SoundSet bodyImpactHardSound { get { return m_bodyImpactHardSound; } }
        [SerializeField]
        private SoundSet m_getUpSound;
        public SoundSet getUpSound { get { return m_getUpSound; } }
    }

}