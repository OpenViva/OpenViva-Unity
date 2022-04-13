using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace viva{


[System.Serializable]
[CreateAssetMenu(fileName = "CloudRenderSettings", menuName = "Cloud Render Settings", order = 1)]
public class CloudRenderSettings: ScriptableObject{

    [SerializeField]
    private Texture3D volumetricTexture;

    public void Apply( MeshRenderer mr ){
        mr.sharedMaterial.SetTexture("_VolumeMap",volumetricTexture);
    }
}

}