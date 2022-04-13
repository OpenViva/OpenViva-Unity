using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace viva{



public partial class Loli : Character {

	
	public int lodLevel { get; protected set; } = 0;
	public OutfitInstance outfitInstance { get; private set; } = new OutfitInstance();

	private void FixedUpdateLOD(){
		float sqDist = Vector3.SqrMagnitude( GameDirector.instance.mainCamera.transform.position-floorPos );
		sqDist *= System.Convert.ToInt32( !previewMode );	//cancels out lod to zero if on
		int newHighLOD;
		if( sqDist < 200.0f ){
			newHighLOD = 0;
		}else if( sqDist < 500.0f ){
			newHighLOD = 1;
		}else{
			newHighLOD = 2;
		}
		if( newHighLOD != lodLevel ){
			lodLevel = newHighLOD;

			bool enableClothSim = lodLevel==0;
			foreach( var clothingInstance in outfitInstance.attachmentInstances ){
				if( clothingInstance.sourceClothingPiece.attribute == ClothingPreset.Attribute.CLOTH ){
					Cloth cloth = clothingInstance.containers[0].GetComponent<Cloth>();
					cloth.enabled = enableClothSim;
				}
			}
			if( lodLevel == 0 ){
				outfitInstance.EnableActiveDynamicBones();
			}else{
				outfitInstance.DisableActiveDynamicBones();
			}
		}
	}

    public partial class OutfitInstance{

		public class AttachmentInstance{
			
			public readonly GameObject[] containers;
			public readonly ClothingPreset sourceClothingPiece;
			public readonly Renderer[] activeRenderers;
			public string cardFilename = null;

			public AttachmentInstance( GameObject[] _containers, ClothingPreset _sourceClothingPiece, Renderer[] _activeRenderers ){
				containers = _containers;
				sourceClothingPiece = _sourceClothingPiece;
				activeRenderers = _activeRenderers;

				foreach( Renderer renderer in activeRenderers ){
					foreach( Material mat in renderer.materials ){
						mat.name = mat.name.Replace(" (Instance)","");
					}
				}
			}
		}

		public List<AttachmentInstance> attachmentInstances { get; private set; } = new List<AttachmentInstance>();
		private List<DynamicBoneModification> dynamicBoneWithModifications = new List<DynamicBoneModification>();
		public DynamicBone[] activeDynamicBones { get; private set; }
		public List<GameObject> createdBones = new List<GameObject>();

		public OutfitInstance(){
		}

		public void DestroyAllAttachmentInstances(){

			foreach( AttachmentInstance activeClothingInstance in attachmentInstances ){
				foreach( var container in activeClothingInstance.containers ){
					Destroy( container );
				}
			}
			attachmentInstances.Clear();
		}

		public void DestroyAllCreatedBones(){
			foreach( GameObject go in createdBones ){
				DestroyImmediate( go );
			}
			createdBones.Clear();
		}

		public void RegisterDynamicBoneWithModifications( DynamicBoneModification modification ){
			dynamicBoneWithModifications.Add( modification );
		}

		public void SetActiveDynamicBones( DynamicBone[] dynamicBones ){
			activeDynamicBones = dynamicBones;
		}

		public void DisableActiveDynamicBones(){
			foreach( DynamicBone dynBone in activeDynamicBones ){
				GameDirector.dynamicBones.Remove( dynBone );
			}
		}

		public void EnableActiveDynamicBones(){
			foreach( DynamicBone dynBone in activeDynamicBones ){
				dynBone.ResetParticlesPosition();
				GameDirector.dynamicBones.Add( dynBone );
			}
		}
		
		public void SetDynamicBoneModificationsStiffness( float multiplier ){
			for( int i=0; i<dynamicBoneWithModifications.Count; i++ ){
				DynamicBoneModification mod = dynamicBoneWithModifications[i];
				mod.bone.m_Stiffness = mod.info.stiffness*multiplier;
				mod.bone.m_Elasticity = mod.info.elasticity*multiplier;
				mod.bone.UpdateParameters();
			}
		}
		public void SetDynamicBoneModificationsCollider( DynamicBoneColliderBase collider, bool add ){
			if( collider == null ){
				Debug.LogError("ERROR Cannot attach a null plane collider!");
				return;
			}
			if( add ){
				for( int i=0; i<dynamicBoneWithModifications.Count; i++ ){
					DynamicBoneModification mod = dynamicBoneWithModifications[i];
					mod.bone.m_Colliders.Add( collider );
				}
			}else{
				for( int i=0; i<dynamicBoneWithModifications.Count; i++ ){
					DynamicBoneModification mod = dynamicBoneWithModifications[i];
					mod.bone.m_Colliders.Remove( collider );
				}
			}
		}

		public bool ApplyMaterialOverrides( Dictionary<ClothingPreset,Outfit.ClothingOverride> clothingOverrides ){
			if( clothingOverrides == null ){
				return false;
			}
			bool foundAll = true;
			foreach( KeyValuePair<ClothingPreset,Outfit.ClothingOverride> clothingOverride in clothingOverrides ){
				bool found = false;
				foreach( AttachmentInstance attachment in attachmentInstances ){
					if( attachment.sourceClothingPiece != clothingOverride.Key ){
						continue;
					}

					foreach( Renderer renderer in attachment.activeRenderers ){
						renderer.material.mainTexture = clothingOverride.Value.texture;
					}
					found = true;
				}
				foundAll &= found;
				if( !found ){
					Debug.LogError("ERROR Could not find material to override "+clothingOverride.Key.name);
				}
			}
			return foundAll;
		}
	}
}


}