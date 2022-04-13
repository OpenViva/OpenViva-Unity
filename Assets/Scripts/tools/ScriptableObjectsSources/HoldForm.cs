using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace viva{


[System.Serializable]
public class HoldForm: ScriptableObject{

	public bool rightHandOnly;
	public float handSize = 1.0f;
	public Vector3 normal;
	public Quaternion[] fingers = new Quaternion[15];
	public Vector3 targetBoneLocalPos;
	public Quaternion targetBoneLocalRotation;
	public float handPitch = 0.0f;
	public float handYaw = 0.0f;
	public float handRoll = 0.0f;
	public Vector3 localHandTarget = Vector3.zero;
	public Vector3 localHandPole = Vector3.zero;
	public bool keepYawOffset;
}

}