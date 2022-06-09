using UnityEngine;

namespace viva
{



    public class ClothingPresetBinding : MonoBehaviour
    {

        [SerializeField]
        private SkinnedMeshRenderer m_modelSMR;
        public SkinnedMeshRenderer modelSMR { get { return m_modelSMR; } }

        [SerializeField]
        private int[] m_boneBindingIndices = new int[0];
        public int[] boneBindingIndices { get { return m_boneBindingIndices; } }
    }

}