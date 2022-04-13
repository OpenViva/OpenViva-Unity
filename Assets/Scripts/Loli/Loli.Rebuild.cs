using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace viva{



public partial class Loli : Character {

    [SerializeField]
    private CapsuleCollider[] legCC;
    [SerializeField]
    private ClothSphereColliderPair[] handSC;


    public void ApplyOutfit( Outfit outfit ){
        for( int i=0; i<outfit.GetClothingPieceCount(); i++ ){
            ClothingPreset clothingPiece = outfit.GetClothingPiece(i);
            switch( clothingPiece.attribute ){
            case ClothingPreset.Attribute.CLOTH:
                ApplyOutfitCloth( clothingPiece );
                break;
            case ClothingPreset.Attribute.DEFORM:
                ApplyOutfitDeform( clothingPiece );
                break;
            case ClothingPreset.Attribute.OBJECT:
                ApplyOutfitObject( clothingPiece );
                break;
            }
        }
    }

    private void ApplyOutfitCloth( ClothingPreset clothingPiece ){

        Transform clothParent = null;
        switch( clothingPiece.wearType ){
        case ClothingPreset.WearType.SKIRT:
            clothParent = spine1RigidBody.transform;
            break;
        }

        GameObject container = Instantiate( clothingPiece.prefab, anchor.position, anchor.rotation, clothParent );
        container.transform.SetParent( clothParent, false );

        SkinnedMeshRenderer clothSMR = container.GetComponent(typeof(SkinnedMeshRenderer)) as SkinnedMeshRenderer;
        clothSMR.bones = bodySMRs[0].bones;
        Cloth cloth = container.GetComponent(typeof(Cloth)) as Cloth;
        if( legCC != null ){
            cloth.capsuleColliders = legCC;
        }
        if( handSC != null ){
            cloth.sphereColliders = handSC;
        }
        cloth.ClearTransformMotion();
        outfitInstance.attachmentInstances.Add( new OutfitInstance.AttachmentInstance(
            new GameObject[]{ container },
            clothingPiece,
            new Renderer[]{ clothSMR }
        ));
    }

    private void ApplyOutfitDeform( ClothingPreset clothingPiece ){
        GameObject container = Instantiate( clothingPiece.prefab, Vector3.zero, Quaternion.identity );
        var bindingInfo = container.GetComponent<ClothingPresetBinding>();
        bindingInfo.modelSMR.transform.SetParent( anchor, false );
        bindingInfo.modelSMR.rootBone = spine1;

        //remap bone profile to own skeleton
        Transform[] rebindedBones = new Transform[ bindingInfo.boneBindingIndices.Length ];
        for( int i=0; i<bindingInfo.boneBindingIndices.Length; i++ ){
            Transform bone = bodySMRs[0].bones[ bindingInfo.boneBindingIndices[i] ];
            if( bone == null ){
                Debug.LogError("[Loli] Could not deform clothing bone to "+bindingInfo.modelSMR.bones[i].name);
            }
            rebindedBones[i] = bone;
        }
        bindingInfo.modelSMR.bones = rebindedBones;

        outfitInstance.attachmentInstances.Add( new OutfitInstance.AttachmentInstance(
            new GameObject[]{ bindingInfo.modelSMR.gameObject },
            clothingPiece,
            new Renderer[]{
                bindingInfo.modelSMR
            }
        ));
        toonMaterials.Add( bindingInfo.modelSMR.material );
        Destroy( container );
    }

    private void ApplyOutfitObject( ClothingPreset clothingPiece ){
        
        GameObject container = Instantiate( clothingPiece.prefab, Vector3.zero, Quaternion.identity );
        
        if( clothingPiece.wearType == ClothingPreset.WearType.FOOTWEAR ){
            if( container.transform.childCount != 2 ){
                Debug.LogError("ERROR Footwear prefabs must have 2 children *_r and *_l");
                Destroy( container );
                return;
            }
            if( !container.transform.GetChild(0).name.Contains("_r") ){
                Debug.LogError("ERROR First shoe must be *_r as name!");
                Destroy( container );
                return;
            }
            Transform rightFootwear = container.transform.GetChild(0);
            rightFootwear.SetParent( foot_r, false );
            Transform leftFootwear = container.transform.GetChild(0);
            leftFootwear.SetParent( foot_l, false );

            outfitInstance.attachmentInstances.Add( new OutfitInstance.AttachmentInstance(
                new GameObject[]{ rightFootwear.gameObject, leftFootwear.gameObject },
                clothingPiece,
                new Renderer[]
                { 	rightFootwear.gameObject.GetComponent<MeshRenderer>(),
                    leftFootwear.gameObject.GetComponent<MeshRenderer>()
                }
            ));

            Destroy( container );	//remove the now empty object
        }else{
            
            Transform parent;
            switch( clothingPiece.wearType ){
            case ClothingPreset.WearType.GROIN:
                parent = spine1;
                break;
            case ClothingPreset.WearType.TORSO:
                parent = spine3;
                break;
            case ClothingPreset.WearType.SKIRT:
                parent = spine2;
                break;
            case ClothingPreset.WearType.FACEWEAR:
                parent = head;	//head
                break;
            default:
                Debug.LogError("ERROR OBJECT Clothing wearType not yet linked to parent!");
                return;
            }
            container.transform.SetParent( parent, false );
            outfitInstance.attachmentInstances.Add( new OutfitInstance.AttachmentInstance(
                new GameObject[]{ container },
                clothingPiece,
                new Renderer[]{
                    container.GetComponent<MeshRenderer>()
                }
            ));
        }
    }
}

}