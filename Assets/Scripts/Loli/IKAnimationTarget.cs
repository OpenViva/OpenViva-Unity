using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace viva{

	[System.Serializable]
	[CreateAssetMenu(fileName = "IKAnimationTarget", menuName = "Logic/IK Animation Target", order = 1)]
	public class IKAnimationTarget : ScriptableObject {

		[SerializeField]
		public Vector3 target;
		[SerializeField]
		public Vector3 pole;
		[SerializeField]
		public Vector3 eulerRotation;
	}
}