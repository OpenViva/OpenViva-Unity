using UnityEngine;
using System.Collections;
using System;

namespace RootMotion.Dynamics {
	
	// Contains high level API calls for the PuppetMaster.
	public partial class PuppetMaster: MonoBehaviour {

		/// <summary>
		/// Returns the muscle that has the specified target.
		/// </summary>
		public Muscle GetMuscle(Transform target) {
			int index = GetMuscleIndex(target);
			if (index == -1) return null;
			return muscles[index];
		}

		/// <summary>
		/// Returns the muscle of the specified Rigidbody.
		/// </summary>
		public Muscle GetMuscle(Rigidbody rigidbody) {
			int index = GetMuscleIndex(rigidbody);
			if (index == -1) return null;
			return muscles[index];
		}

		/// <summary>
		/// Returns the muscle of the specified Joint.
		/// </summary>
		public Muscle GetMuscle(ConfigurableJoint joint) {
			int index = GetMuscleIndex(joint);
			if (index == -1) return null;
			return muscles[index];
		}

		/// <summary>
		/// Does the PuppetMaster have a muscle for the specified joint.
		/// </summary>
		public bool ContainsJoint(ConfigurableJoint joint) {
	
			foreach (Muscle m in muscles) {
				if (m.joint == joint) return true;
			}
			return false;
		}

		/// <summary>
		/// Returns the index of the muscle that has the humanBodyBone target (works only with a Humanoid avatar).
		/// </summary>
		public int GetMuscleIndex(HumanBodyBones humanBodyBone) {

			if (targetAnimator == null) {
				Debug.LogWarning("PuppetMaster 'Target Root' has no Animator component on it nor on it's children.", transform);
				return -1;
			}
			if (!targetAnimator.isHuman) {
				Debug.LogWarning("PuppetMaster target's Animator does not belong to a Humanoid, can hot get human muscle index.", transform);
				return -1;
			}
			
			var bone = targetAnimator.GetBoneTransform(humanBodyBone);
			if (bone == null) {
				Debug.LogWarning("PuppetMaster target's Avatar does not contain a bone Transform for " + humanBodyBone, transform);
				return -1;
			}
			
			return GetMuscleIndex(bone);
		}

		/// <summary>
		/// Returns the index of the muscle that has the specified target. Returns -1 if not found.
		/// </summary>
		public int GetMuscleIndex(Transform target) {

			if (target == null) {
				Debug.LogWarning("Target is null, can not get muscle index.", transform);
				return -1;
			}

			for (int i = 0; i < muscles.Length; i++) {
				if (muscles[i].target == target) return i;
			}

			Debug.LogWarning("No muscle with target " + target.name + "found on the PuppetMaster.", transform);
			return -1;
		}
		
		/// <summary>
		/// Returns the index of the muscle that has the specified Rigidbody. Returns -1 if not found.
		/// </summary>
		public int GetMuscleIndex(Rigidbody rigidbody) {

			if (rigidbody == null) {
				Debug.LogWarning("Rigidbody is null, can not get muscle index.", transform);
				return -1;
			}

			for (int i = 0; i < muscles.Length; i++) {
				if (muscles[i].rigidbody == rigidbody) return i;
			}

			Debug.LogWarning("No muscle with Rigidbody " + rigidbody.name + "found on the PuppetMaster.", transform);
			return -1;
		}
		
		/// <summary>
		/// Returns the index of the muscle that has the specified Joint. Returns -1 if not found.
		/// </summary>
		public int GetMuscleIndex(ConfigurableJoint joint) {
			if (joint == null) {
				Debug.LogWarning("Joint is null, can not get muscle index.", transform);
				return -1;
			}

			for (int i = 0; i < muscles.Length; i++) {
				if (muscles[i].joint == joint) return i;
			}

			Debug.LogWarning("No muscle with Joint " + joint.name + "found on the PuppetMaster.", transform);
			return -1;
		}
	}
}

