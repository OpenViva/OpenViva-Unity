using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace viva{


public partial class Loli : Character {

	public class DynamicBoneModification{
		public readonly DynamicBone bone;
		public readonly VivaModel.DynamicBoneInfo info;

		public DynamicBoneModification( DynamicBone _bone, VivaModel.DynamicBoneInfo _info ){
			bone = _bone;
			info = _info;
		}
	}
	[SerializeField]
	private SkinnedMeshRenderer m_headSMR;
	public SkinnedMeshRenderer headSMR { get{ return m_headSMR; } protected set{ m_headSMR = value; } }
	[SerializeField]
	private SkinnedMeshRenderer[] m_bodySMRs;
	public SkinnedMeshRenderer[] bodySMRs { get{ return m_bodySMRs; } protected set{ m_bodySMRs = value; } }
	[SerializeField]
	public ItemSettings dynBoneItemSettings;
    
	public delegate void OnPostRigHierarchyChangeCallback();
	public OnPostRigHierarchyChangeCallback onRigHierarchyChange;

	private DynamicBoneCollider[] dynamicBoneColliders = new DynamicBoneCollider[ System.Enum.GetValues(typeof(VivaModel.DynamicBoneInfo.Collision)).Length ];
	private Set<Material> toonMaterials = new Set<Material>();
	private static readonly int fingerNailID = Shader.PropertyToID("_FingerNailColor");
	private static readonly int toeNailID = Shader.PropertyToID("_ToeNailColor");
	

	public void SetModelLayer( int layer ){
		foreach( var bodySMR in bodySMRs ){
			bodySMR.gameObject.layer = layer;
		}
		headSMR.gameObject.layer = layer;
		foreach( var instance in outfitInstance.attachmentInstances ){
			foreach( GameObject container in instance.containers ){
				container.layer = layer;
			}
		}
	}

	public void SetHeadModel( VivaModel newHeadModel, ModelBuildSettings mbs ){
		if( newHeadModel == null || mbs == null ){
			return;
		}
		m_headModel = newHeadModel;

        newHeadModel.Build( this, mbs );

		ForceImmediatePose( Loli.Animation.NONE );
		
		RebuildToonMaterialList();
		RebindLookAtLogic();
		RebuildVoice();
		ApplyHeadModelCollisions();
		RebuildDynamicBoneColliders();
		RebuildDynamicBones();
		SetSkinColor( headModel.skinColor );

		ForceImmediatePose( m_currentAnim );
	}

	private void RebuildToonMaterialList(){
		
		toonMaterials.objects.Clear();
		toonMaterials.Add( headSMR.materials, headSMR.materials.Length );
		foreach( var bodySMR in bodySMRs ){
			toonMaterials.Add( bodySMR.materials, bodySMR.materials.Length );
		}
	}

	public void SetOutfit( Outfit _outfit ){

		if( _outfit == null ){
			Debug.LogError("[Loli] Outfit cannot be null!");
		}
		outfit = _outfit;

		outfitInstance.DestroyAllAttachmentInstances();

		ForceImmediatePose( Loli.Animation.NONE );
		
		ApplyFingerNailColor( outfit.fingerNailColor );
		ApplyToeNailColor( outfit.toeNailColor );
		ApplyOutfit( outfit );

		outfitInstance.ApplyMaterialOverrides( outfit.clothingOverrides );

		if( onRigHierarchyChange != null ){
			onRigHierarchyChange();
		}
		animator.Rebind();
		ForceImmediatePose( m_currentAnim );
		IncreaseDirt(0.0f);
	}
	
	public void ApplyFingerNailColor( Color color ){
		foreach( var bodySMR in bodySMRs ){
			bodySMR.material.SetColor( fingerNailID, color );
		}
	}
	public void ApplyToeNailColor( Color color ){
		foreach( var bodySMR in bodySMRs ){
			bodySMR.material.SetColor( toeNailID, color );
		}
	}
	
	public void SetFloatParameter( int parameterID, float value ){

		foreach( var bodySMR in bodySMRs ){
			foreach( Material mat in bodySMR.materials ){
				mat.SetFloat( parameterID, value );
			}
		}
		foreach( var instance in outfitInstance.attachmentInstances ){
			foreach( Renderer renderer in instance.activeRenderers ){
				foreach( Material mat in renderer.materials ){
					mat.SetFloat( parameterID, value );
				}
			}
		}
	}

	public void SetSkinColor( Color color ){
		foreach( Material mat in toonMaterials.objects ){
			mat.SetColor( Instance.skinColorID, color );
		}
	}

	public void RebuildDynamicBoneColliders(){

		DynamicBoneCollider collider = viva.Tools.EnsureComponent<DynamicBoneCollider>( head.gameObject );
		if( headModel.headCollisionWorldSphere.w == 0.0f ){
			collider.m_Radius = 0.128f;
			collider.m_Center = new Vector3( 0.0f, 0.08f, 0.0f );
			collider.m_Height = 0.0f;
		}else{
			Vector3 headCollisionLocalSphere = head.InverseTransformPoint( new Vector3( 
				headModel.headCollisionWorldSphere.x,
				headModel.headCollisionWorldSphere.y,
				headModel.headCollisionWorldSphere.z
			) );
			collider.m_Radius = headModel.headCollisionWorldSphere.w;
			collider.m_Center = headCollisionLocalSphere;
			collider.m_Height = 0.0f;
		}
		dynamicBoneColliders[ (int)VivaModel.DynamicBoneInfo.Collision.HEAD ] = collider;

		collider = viva.Tools.EnsureComponent<DynamicBoneCollider>( spine3.gameObject );
		collider.m_Radius = 0.31f;
		collider.m_Center = new Vector3( 0.0f, 0.09f, 0.24f );
		collider.m_Height = 0.83f;
		dynamicBoneColliders[ (int)VivaModel.DynamicBoneInfo.Collision.BACK ] = collider;
	}

	private void RebuildDynamicBones(){
		
		if( headModel == null ){
			return;
		}
		List<DynamicBone> activeDynamicBones = new List<DynamicBone>();
		for( int j=0; j<headModel.dynamicBoneInfos.Count; j++ ){
			VivaModel.DynamicBoneInfo info = headModel.dynamicBoneInfos[j];
			Transform rootBone = Tools.SearchTransformFamily( bodyArmature, info.rootBone );
			if( rootBone == null ){
				Debug.LogError("[Loli] Could not find bone "+info.rootBone+" for DynamicBone");
				continue;
			}
			DynamicBone dynamicBone = viva.Tools.EnsureComponent<DynamicBone>( rootBone.gameObject );

			ApplyDynamicBoneInfo( info, dynamicBone );
			if( info.canRelax ){
				outfitInstance.RegisterDynamicBoneWithModifications( new DynamicBoneModification( dynamicBone, info ) );
			}
			activeDynamicBones.Add( dynamicBone );
			AttachDynamicBoneItem( rootBone, dynamicBone );
		}
		outfitInstance.SetActiveDynamicBones( activeDynamicBones.ToArray() );

		if( lodLevel == 0 ){
			outfitInstance.EnableActiveDynamicBones();
		}else{
			outfitInstance.DisableActiveDynamicBones();
		}
	}

	private void AttachDynamicBoneItem( Transform bone, DynamicBone dynamicBone ){
		//attach to all bones along chain except first one
		if( bone.childCount == 0 ){
			Debug.LogError("ERROR DynamicBoneItem parents must have at least 1 child transform");
			return;
		}
		do{
			bone = bone.GetChild(0);
			
			DynamicBoneItem dynBoneItem = Item.AddAndAwakeItemComponent<DynamicBoneItem>( bone.gameObject, dynBoneItemSettings, this );
			dynBoneItem.InitializeDynamicBoneItem( dynamicBone );

			bone.gameObject.layer = Instance.bodyPartItemsLayer;
			SphereCollider itemCollider = viva.Tools.EnsureComponent<SphereCollider>( bone.gameObject );
			itemCollider.radius = 0.03f;
			itemCollider.isTrigger = true;
		}while( bone.childCount > 0 );
	}

	public void Hotswap( VivaModel targetModel ){
		
		if( !headModel.AttemptHotswap( targetModel ) ){
			return;
		}
		for( int i=0; i<headModel.dynamicBoneInfos.Count; i++ ){
			ApplyDynamicBoneInfo( headModel.dynamicBoneInfos[i], outfitInstance.activeDynamicBones[i] );
		}
		SetSkinColor( headModel.skinColor );
	}

	public void ApplyDynamicBoneInfo( VivaModel.DynamicBoneInfo info, DynamicBone dynamicBone ){
		if( dynamicBone == null ){
            Debug.LogError("ERROR Could not apply null Dynamic Bone Info");
            return;
        }
        if( dynamicBone == null ){
            Debug.LogError("ERROR Could not apply Dynamic Bone Info to null");
            return;
        }
		dynamicBone.m_Root = dynamicBone.transform;
		dynamicBone.m_Damping = info.damping;
		dynamicBone.m_Elasticity = info.elasticity;
		dynamicBone.m_Stiffness = info.stiffness;
		dynamicBone.m_Radius = 0.015f;
		if( info.useGravity ){
			dynamicBone.m_Force = new Vector3( 0.0f, -0.005f, 0.0f );
		}else{
			dynamicBone.m_Force = Vector3.zero;
		}
		dynamicBone.m_EndLength = 0.5f;

		//Rebuild completely new List
		dynamicBone.m_Colliders = new List<DynamicBoneColliderBase>();
		if( info.headCollision ){
			dynamicBone.m_Colliders.Add( dynamicBoneColliders[ (int)VivaModel.DynamicBoneInfo.Collision.HEAD ] );
		}
		if( info.collisionBack ){
			dynamicBone.m_Colliders.Add( dynamicBoneColliders[ (int)VivaModel.DynamicBoneInfo.Collision.BACK ] );
		}
		dynamicBone.UpdateParameters();
	}

	public void AttachOnPostRigHierarchyChangeListener( OnPostRigHierarchyChangeCallback listener ){
		onRigHierarchyChange += listener;
	}
}

}