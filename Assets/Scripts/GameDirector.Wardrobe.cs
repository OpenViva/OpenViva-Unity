using System.Collections.Generic;
using UnityEngine;


namespace viva
{


    public partial class GameDirector : MonoBehaviour
    {

        [Header("Clothing")]
        [SerializeField]
        private List<ClothingPreset> m_wardrobe = new List<ClothingPreset>();
        public List<ClothingPreset> wardrobe { get { return m_wardrobe; } }


        public ClothingPreset FindClothing(string clothingName)
        {
            foreach (ClothingPreset item in wardrobe)
            {
                if (item.name == clothingName)
                {
                    return item;
                }
            }
            Debug.LogError("#Wardrobe piece not found# " + clothingName);
            return null;
        }
    }

}