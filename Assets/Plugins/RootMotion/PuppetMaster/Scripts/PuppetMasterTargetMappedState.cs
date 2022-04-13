using UnityEngine;
using System.Collections;
using System;

namespace RootMotion.Dynamics {
	
	// Code for storing and sampling mapped poses of the animated target and blending back to the last sampled mapped pose.
	public partial class PuppetMaster: MonoBehaviour {

		/// <summary>
		/// The pose that the target will be fixed to if calling FixTargetToSampledState(). This should normally be used only by the Puppet Behaviours.
		/// </summary>
		// public void SampleTargetMappedState() {
			
		// 	muscles[0].targetSampledPosition = muscles[0].targetMappedPosition;
        //     for (int i = 0; i < muscles.Length; i++){
        //         muscles[i].targetSampledRotation = muscles[i].targetMappedRotation;
        //     }
        // }

		/// <summary>
		/// Blend the target to the pose that was sampled by the last SampleTargetMappedState call. This should normally be used only by the Puppet Behaviours.
		/// </summary>
		// public void FixTargetToSampledState(float weight) {

        //     if (weight <= 0f) return;

        //     for (int i = 0; i < muscles.Length; i++)
        //     {
        //         if (i == 0) muscles[i].target.position = Vector3.Lerp(muscles[i].target.position, muscles[i].targetSampledPosition, weight);
        //         muscles[i].target.rotation = Quaternion.Lerp(muscles[i].target.rotation, muscles[i].targetSampledRotation, weight);
        //     }
            
        //     foreach (Muscle m in muscles) m.positionOffset = m.target.position - m.rigidbody.position;
        // }

		/// <summary>
		/// Stores the current pose of the target for sampling. This should normally be used only by the Puppet Behaviours.
		/// </summary>
		public void StoreTargetMappedState() {
			
			muscles[0].StoreTargetMappedPosition();
            for( int i=0; i<muscles.Length; i++ ){
                muscles[i].StoreTargetMappedRotation();
            }
        }

		// Should be called each time the puppet structure is changed
		private void UpdateHierarchies() {

			AssignParentAndChildIndexes();
			AssignKinshipDegrees();
		}


		// Find parent and child muscle indexes for each muscle
		private void AssignParentAndChildIndexes() {
			for (int i = 0; i < muscles.Length; i++) {
				// Parents
				muscles[i].parentIndexes = new int[0];
				
				if (muscles[i].joint.connectedBody != null) {
					AddToParentsRecursive(muscles[i].joint.connectedBody.GetComponent<ConfigurableJoint>(), ref muscles[i].parentIndexes);
				}
				
				// Children
				muscles[i].childIndexes = new int[0];
				muscles[i].childFlags = new bool[muscles.Length];
				
				for (int n = 0; n < muscles.Length; n++) {
					if (i != n && muscles[n].joint.connectedBody == muscles[i].rigidbody) {
						AddToChildrenRecursive(muscles[n].joint, ref muscles[i].childIndexes, ref muscles[i].childFlags);
					}
				}
			}
		}

		// Add all parent indexes to the indexes array recursively
		private void AddToParentsRecursive(ConfigurableJoint joint, ref int[] indexes) {
			if (joint == null) return;
			
			int muscleIndex = GetMuscleIndexLowLevel(joint);
			if (muscleIndex == -1) return;
			
			Array.Resize(ref indexes, indexes.Length + 1);
			indexes[indexes.Length - 1] = muscleIndex;
			
			if (joint.connectedBody == null) return;
			AddToParentsRecursive(joint.connectedBody.GetComponent<ConfigurableJoint>(), ref indexes);
		}
		
		// Add all child indexes to the indexes array recursively
		private void AddToChildrenRecursive(ConfigurableJoint joint, ref int[] indexes, ref bool[] childFlags) {
			if (joint == null) return;
			
			int muscleIndex = GetMuscleIndexLowLevel(joint);
			if (muscleIndex == -1) return;
			
			Array.Resize(ref indexes, indexes.Length + 1);
			indexes[indexes.Length - 1] = muscleIndex;

			childFlags[muscleIndex] = true;
			
			for (int i = 0; i < muscles.Length; i++) {
				if (i != muscleIndex && muscles[i].joint.connectedBody == joint.GetComponent<Rigidbody>()) {
					AddToChildrenRecursive(muscles[i].joint, ref indexes, ref childFlags);
				}
			}
		}

		private void AssignKinshipDegrees() {
			for (int i = 0; i < muscles.Length; i++) {
				// Parents
				muscles[i].kinshipDegrees = new int[muscles.Length];

				AssignKinshipsDownRecursive(ref muscles[i].kinshipDegrees, 1, i);
				AssignKinshipsUpRecursive(ref muscles[i].kinshipDegrees, 1, i);
			}
		}

		private void AssignKinshipsDownRecursive(ref int[] kinshipDegrees, int degree, int index) {
			for (int i = 0; i < muscles.Length; i++) {
				if (i != index) {
					if (muscles[i].joint.connectedBody == muscles[index].rigidbody) {
						kinshipDegrees[i] = degree;

						AssignKinshipsDownRecursive(ref kinshipDegrees, degree + 1, i);
					}
				}
			}
		}

		private void AssignKinshipsUpRecursive(ref int[] kinshipDegrees, int degree, int index) {
			for (int i = 0; i < muscles.Length; i++) {
				if (i != index) {
					if (muscles[i].rigidbody == muscles[index].joint.connectedBody) {
						kinshipDegrees[i] = degree;
						
						AssignKinshipsUpRecursive(ref kinshipDegrees, degree + 1, i);

						for (int c = 0; c < muscles.Length; c++) {
							if (c != i && c != index) {
								if (muscles[c].joint.connectedBody == muscles[i].rigidbody) {
									kinshipDegrees[c] = degree + 1;
									
									AssignKinshipsDownRecursive(ref kinshipDegrees, degree + 2, c);
								}
							}
						}
					}
				}
			}
		}

		// Returns the index of the muscle that has the specified Joint. Returns -1 if not found.
		private int GetMuscleIndexLowLevel(ConfigurableJoint joint) {
			for (int i = 0; i < muscles.Length; i++) {
				if (muscles[i].joint == joint) return i;
			}
			return -1;
		}
	}
}
