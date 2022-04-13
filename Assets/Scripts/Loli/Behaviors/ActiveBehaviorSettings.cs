using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


namespace viva{


[System.Serializable]
[CreateAssetMenu(fileName = "activeBehaviorSettings", menuName = "Logic/Active Behavior Settings", order = 1)]
public class ActiveBehaviorSettings : ScriptableObject {

    [Header("Bathing")]
    [SerializeField]
    public GameObject bubbleMeterPrefab;
    [SerializeField]
    public Material bubbleMeterLevel1;
    [SerializeField]
    public Material bubbleMeterLevel2;
    [SerializeField]
    public Material bubbleMeterLevel3;
    [SerializeField]
    public GameObject dynBoneShaftBubblesShort;
    [SerializeField]
    public GameObject dynBoneShaftBubblesLong;
    [SerializeField]
    public GameObject dynBoneShaftBubblesTip;
    [SerializeField]
    public GameObject headScrubGeneratedBubbles;
}

}