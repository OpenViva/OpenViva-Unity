using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using System;


namespace viva{

public class AnimalProfile: Profile{

    public override int Size(){
        return bones.Count;
    }

    public List<Bone<string>> bones = new List<Bone<string>>();
    public Quaternion armatureDelta;

    public Bone<string> this[ string boneName ]{
        get{
            foreach( var bone in bones ){
                if( bone.name == boneName ) return bone;
            }
            return null;
        }
    }

    //build ragdollprofile onto Model given bone list
    public AnimalProfile( string[] boneList, string[] _blendShapeBindings, Model alternate ){
        if( boneList == null ) throw new System.Exception("Insufficient bone name list");
        if( alternate == null ) throw new System.Exception("Cannot duplicate RagdollProfile with null alternate");
        if( alternate.skinnedMeshRenderer == null ) throw new System.Exception("Alternate Model needs skinned mesh renderer");
        
        for( int i=0; i<boneList.Length; i++ ){
            
            var targetName = boneList[i];
            if( targetName == null ) continue;
            Transform alternateBone = null;
            foreach( var bone in alternate.bones ){
                if( bone.name == targetName ){
                    alternateBone = bone;
                    break;
                }
            }
            if( !alternateBone ){
                throw new System.Exception("FBX missing bone \""+targetName+"\"");
            }
            bones.Add( new Bone<string>( alternateBone, targetName ) );
        }
        armatureDelta = Quaternion.Inverse( alternate.armature.rotation );
        blendShapeBindings = _blendShapeBindings;
    }

    public AnimalProfile( Model model ){
        blendShapeBindings = new string[ System.Enum.GetValues(typeof(RagdollBlendShape)).Length ];

        if( model == null || model.bones == null ){
            throw new System.Exception("Cannot autobind model with null bones");
        }
        if( model.muscleTemplates == null || model.muscleTemplates.Length == 0 ){
            throw new System.Exception("Model does not have muscles defined");
        }

        foreach( var bone in model.skinnedMeshRenderer.bones ){
            bones.Add( new Bone<string>( bone, bone.name ) );
        }
        //TODO: ADD BLENSHAPES HERE
    }

    public AnimalProfile( AnimalProfile copy ){
        blendShapeBindings = new string[ System.Enum.GetValues(typeof(RagdollBlendShape)).Length ];
        if( copy == null ) throw new System.Exception("Cannot duplicate null RagdollProfile");
        bones.AddRange( copy.bones );
    }

    public bool BoneInfoHasColliders( Bone<string> boneInfo ){
        for( int j=0; j<boneInfo.transform.childCount; j++ ){
            if( boneInfo.transform.GetChild(j).gameObject.layer == WorldUtil.itemsLayer ){
                return true;
            }
        }
        return false;
    }

    public override bool ValidateProfile( Model model, out string message ){
        message = null;
        bool valid = true;
        for( int i=0; i<bones.Count; i++ ){
            var boneInfo = bones[i];
            if( boneInfo == null || boneInfo.transform == null ){
                message = message == null ? "" : message;
                message += "Entry in profile is null\n";
                valid = false;
            }
        }
        return valid;
    }
}

}