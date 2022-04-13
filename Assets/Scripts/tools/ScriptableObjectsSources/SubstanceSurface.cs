using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace viva{


[System.Serializable]
[CreateAssetMenu(fileName = "Substance Surface Sim", menuName = "Logic/Substance Surface Sim", order = 1)]
public class SubstanceSurfaceSim: ScriptableObject{

    public float yawRotateSpeed = 8.0f;
    public float acceleration = 2.0f;
    public float friction = 0.98f;
    public float parentVelocityInfluence = 1.0f;
}

}