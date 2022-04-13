using UnityEngine;
using System.Collections;
using System;

namespace RootMotion.Dynamics {

	[System.Serializable]
	public enum MuscleRemoveMode {
		Sever, // Severs the body part disconnecting the first joit
		Explode, // Explodes the body part disconnecting all joints
		Numb, // Removes the muscles, keeps the joints connected, but disables spring and damper forces
	}

    [System.Serializable]
    public enum MuscleDisconnectMode
    {
        Sever,
        Explode
    }
	
	// Contains high level API calls for changing the PuppetMaster's muscle structure.
	public partial class PuppetMaster: MonoBehaviour {

        #region Public API

		/// <summary>
		/// Moves all muscles to the positions of their targets.
		/// </summary>
		[ContextMenu("Fix Muscle Positions")]
		public void FixMusclePositions() {
			foreach (Muscle m in muscles) {
				if (m.joint != null && m.target != null) {
					m.joint.transform.position = m.target.position;
				}
			}
		}

        /// <summary>
		/// Moves all muscles to the positions and rotations of their targets.
		/// </summary>
		[ContextMenu("Fix Muscle Positions and Rotations")]
        public void FixMusclePositionsAndRotations()
        {
            foreach (Muscle m in muscles)
            {
                if (m.joint != null && m.target != null)
                {
                    m.joint.transform.position = m.target.position;
                    m.joint.transform.rotation = m.target.rotation;
                }
            }
        }

        /// <summary>
        /// Are all the muscles parented to the PuppetMaster Transform?
        /// </summary>
        public bool HierarchyIsFlat()
        {
            foreach (Muscle m in muscles)
            {
                if (m.joint.transform.parent != transform) return false;
            }
            return true;
        }

        #endregion Public API

        private int GetHighestDisconnectedParentIndex(int index)
        {
            for (int i = muscles[index].parentIndexes.Length - 1; i > -1; i--)
            {
                int parentIndex = muscles[index].parentIndexes[i];
            }

            return index;
        }

        private void AddIndexesRecursive(int index, ref int[] indexes) {
			int l = indexes.Length;
			Array.Resize(ref indexes, indexes.Length + 1 + muscles[index].childIndexes.Length);
			indexes[l] = index;
			
			if (muscles[index].childIndexes.Length == 0) return;
			
			for (int i = 0; i < muscles[index].childIndexes.Length; i++) {
				AddIndexesRecursive(muscles[index].childIndexes[i], ref indexes);
			}
		}

		// Disables joint target rotation, position spring and damper
		private void KillJoint(ConfigurableJoint joint) {
			joint.targetRotation = Quaternion.identity;
			JointDrive j = new JointDrive();
			j.positionSpring = 0f;
			j.positionDamper = 0f;
			
			#if UNITY_5_2
			j.mode = JointDriveMode.None;
			#endif
			
			joint.slerpDrive = j;
		}
	}
}
