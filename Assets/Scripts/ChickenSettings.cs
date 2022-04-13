using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace viva{


[CreateAssetMenu(fileName = "Chicken Settings", menuName = "Logic/Chicken Settings", order = 1)]
public class ChickenSettings : ScriptableObject{

    [Range(0.001f,0.01f)]
    [SerializeField]
    public float standRecalculateThreshold = 0.002f;
    [Range(0.1f,0.5f)]
    [SerializeField]
    public float standCheckDownDistance = 0.32f;
    [Range(0.1f,1.0f)]
    [SerializeField]
    public float standCheckRandomRadius = 0.7f;
    [Range(0.0f,0.5f)]
    [SerializeField]
    public float standingDistance = 0.02f;
    [Range(1.0f,16.0f)]
    [SerializeField]
    public float standPositionLerpStrength = 16.0f;
    [Range(1.0f,10.0f)]
    [SerializeField]
    public float standRotationLerpStrength = 10.0f;
    [Range(0.0f,1.0f)]
    [SerializeField]
    public float maxTurnSpeedDecay = 0.6f;
    [Range(1.0f,90.0f)]
    [SerializeField]
    public float randomChaseDirectionChange = 5.0f;
    [SerializeField]
    public SoundSet buk;
    [SerializeField]
    public SoundSet bukku;
    [SerializeField]
    public Material tamedChickenMaterial;
    [SerializeField]
    public GameObject eggPrefab;
    [SerializeField]
    public AudioClip eggSpawnSound;
    [SerializeField]
    public float proxMin = 0.5f;
    [SerializeField]
    public float proxMax = 3.0f;
    [Range(0.3f,1.0f)]
    [SerializeField]
    public float dropHeightMin = 0.7f;
    [Range(2.0f,4.0f)]
    public float maxSpeed = 3.9f;
    [Range(1.0f,3.0f)]
    [SerializeField]
    public float accel = 1.9f;
    [Range(8.0f,24.0f)]
    [SerializeField]
    public float turnSpeed = 12.0f;
    [Range(1.0f,4.0f)]
    public float maxAccel = 2.5f;
    public float friction = 0.99f;
    [SerializeField]
    public AudioClip tameSound;
}


}