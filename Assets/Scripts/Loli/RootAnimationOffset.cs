using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace viva{

	[System.Serializable]
	[CreateAssetMenu(fileName = "RootAnimationOffset", menuName = "Logic/Root Animation Offset", order = 1)]
	public class RootAnimationOffset : ScriptableObject {

		public Vector3 position;
		public Vector3 eulerRotation;
	}
}