using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace viva{


	[System.Serializable]
	[CreateAssetMenu(fileName = "Camera Pose", menuName = "Camera Pose", order = 1)]
	public class CameraPose: ScriptableObject{

		public Vector3 position;
		public Vector3 rotation;
		public float fov;
	}

}