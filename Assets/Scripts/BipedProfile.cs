using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using System;


namespace viva{

[System.Flags]
public enum BipedMask{
    NONE            =0,
    ALL             =1,
    SPINE_CHAIN     =2,
    ARM_CHAIN_R     =4,
    ARM_CHAIN_L     =8,
    LEG_CHAIN_R     =16,
    LEG_CHAIN_L     =32,
    HIPS            =64,
    LOWER_SPINE     =128,
    UPPER_SPINE     =256,
    NECK            =512,
    HEAD            =1024,
    FINGERS_R       =2048,
    FINGERS_L       =4096,
    SHOULDER_R      =8192,
    SHOULDER_L      =16384
}


public enum BipedBone{
    HIPS,
    LOWER_SPINE,
    UPPER_SPINE, //
    NECK,
    HEAD,
    UPPER_LEG_R,    UPPER_LEG_L,
    LEG_R,          LEG_L,
    FOOT_R,         FOOT_L,
    SHOULDER_R,     SHOULDER_L,
    UPPER_ARM_R,    UPPER_ARM_L,
    ARM_R,          ARM_L,
    HAND_R,         HAND_L, //
    THUMB0_R,       THUMB0_L,
    THUMB1_R,       THUMB1_L,
    THUMB2_R,       THUMB2_L,
    INDEX0_R,       INDEX0_L,
    INDEX1_R,       INDEX1_L,
    INDEX2_R,       INDEX2_L,
    MIDDLE0_R,      MIDDLE0_L,
    MIDDLE1_R,      MIDDLE1_L,
    MIDDLE2_R,      MIDDLE2_L,
    RING0_R,        RING0_L,
    RING1_R,        RING1_L,
    RING2_R,        RING2_L,
    PINKY0_R,       PINKY0_L,
    PINKY1_R,       PINKY1_L,
    PINKY2_R,       PINKY2_L,
    EYEBALL_R,      EYEBALL_L
}

public enum RagdollMuscle{
    HIPS,
    LOWER_SPINE,
    UPPER_SPINE,
    HEAD,
    UPPER_LEG_R,    UPPER_LEG_L,
    LEG_R,          LEG_L,
    FOOT_R,         FOOT_L,
    UPPER_ARM_R,    UPPER_ARM_L,
    ARM_R,          ARM_L,
    HAND_R,         HAND_L,
};

public enum RagdollBlendShape{
    BLINK,
    AH,
    OH,
    EE,
    MAD,
    SAD,
    SMILE
}

public class Bone<T>{
    
    public readonly Transform transform;
    public readonly T name;
    public CollisionDetector colliderDetector { get; private set; } = null;
    public Rigidbody rigidBody = null;
    public ConfigurableJoint joint = null;
    public readonly string originalBoneName;

    
    public Bone( Transform _transform, T _name ){
        transform = _transform;
        name = _name;
        originalBoneName = transform.name;
    }

    public void _InternalCreateColliderDetector(){
        colliderDetector = rigidBody.GetComponent<CollisionDetector>();
        if( colliderDetector == null ){
            colliderDetector = rigidBody.gameObject.AddComponent<CollisionDetector>();
        }
    }
}

public abstract class Profile{

    public string[] blendShapeBindings;

    public static string root { get{ return Viva.contentFolder+"/Profile"; } }

    public abstract int Size();
    public abstract bool ValidateProfile( Model model, out string message );
}

public class BipedProfile: Profile{

    public static int nonOptionalBoneCount { get{ return (int)BipedBone.THUMB0_R; } }
    public override int Size(){
        return System.Enum.GetValues(typeof(BipedBone)).Length;
    }
    
    public static bool HasMirrorVariant( BipedBone bone ){
        return bone > BipedBone.HEAD;
    }
    public static bool? IsOnRightSide( BipedBone bone ){
        if( bone <= BipedBone.HEAD ) return null;
        return ( (int)bone )%2==1;
    }
    public static bool RequiresMuscle( BipedBone bone ){
        return bone < BipedBone.SHOULDER_R;
    }
    public static bool IsOptional( BipedBone bone ){
        return bone > BipedBone.HAND_L;
    }
    public static BipedBone? GetBoneMirrorVariant( BipedBone bone ){
        if( !HasMirrorVariant( bone ) ){
            return null;
        }
        int index = (int)bone;
        if( index%2 == 1 ){
            return bone+1;
        }else{
            return bone-1;
        }
    }
    public static bool BoneIsAtTheEnd( BipedBone bone ){
        switch( bone ){
        case BipedBone.HEAD:
        case BipedBone.FOOT_R:
        case BipedBone.FOOT_L:
        case BipedBone.THUMB2_R:
        case BipedBone.INDEX2_R:
        case BipedBone.MIDDLE2_R:
        case BipedBone.RING2_R:
        case BipedBone.PINKY2_R:
            return true;
        default:
            return false;
        }
    }
    public static BipedBone? GetBoneHierarchyParent( BipedBone bone ){
        if( bone == BipedBone.HIPS ){
            return null;
        }else if( bone <= BipedBone.HEAD ){
            return bone-1;
        }else if( bone <= BipedBone.UPPER_LEG_L ){
            return BipedBone.HIPS;
        }else if( bone <= BipedBone.FOOT_L ){
            return bone-2;
        }else if( bone <= BipedBone.SHOULDER_L ){
            return BipedBone.UPPER_SPINE;
        }else if( bone <= BipedBone.HAND_L ){
            return bone-2;
        }else if( ( bone-BipedBone.THUMB0_R )%3 == 0 ){
            return (int)bone%2==1 ? BipedBone.HAND_R : BipedBone.HAND_L;
        }
        return null;
    }

    private static void AddToFullHierarchy( List<BipedBone> hierarchy, BipedBone bone ){
        var children = GetHierarchyChildren( bone );
        if( children != null ){
            hierarchy.AddRange( children );
            foreach( var child in children ){
                AddToFullHierarchy( hierarchy, child );
            }
        }
    }

    public static List<BipedBone> GetFullHierarchyChildren( BipedBone bone ){
        var bones = new List<BipedBone>(){ bone };
        AddToFullHierarchy( bones, bone );
        return bones;
    }

    public static BipedBone[] GetHierarchyChildren( BipedBone bone ){
        switch( bone ){
        case BipedBone.HIPS:
            return new BipedBone[]{ BipedBone.UPPER_LEG_L, BipedBone.UPPER_LEG_R, BipedBone.LOWER_SPINE };
        case BipedBone.LOWER_SPINE:
            return new BipedBone[]{ BipedBone.UPPER_SPINE };
        case BipedBone.UPPER_SPINE:
            return new BipedBone[]{ BipedBone.SHOULDER_L, BipedBone.SHOULDER_R, BipedBone.NECK };
        case BipedBone.NECK:
            return new BipedBone[]{ BipedBone.HEAD };
        case BipedBone.UPPER_LEG_R:
            return new BipedBone[]{ BipedBone.LEG_R };
        case BipedBone.LEG_R:
            return new BipedBone[]{ BipedBone.FOOT_R };
        case BipedBone.UPPER_ARM_R:
            return new BipedBone[]{ BipedBone.ARM_R };
        case BipedBone.ARM_R:
            return new BipedBone[]{ BipedBone.HAND_R };
        case BipedBone.HAND_R:
            return new BipedBone[]{ BipedBone.THUMB0_R, BipedBone.INDEX0_R, BipedBone.MIDDLE0_R, BipedBone.RING0_R, BipedBone.PINKY0_R, };
        case BipedBone.SHOULDER_R:
            return new BipedBone[]{ BipedBone.UPPER_ARM_R };
        case BipedBone.THUMB0_R:
            return new BipedBone[]{ BipedBone.THUMB1_R };
        case BipedBone.THUMB1_R:
            return new BipedBone[]{ BipedBone.THUMB2_R };
        case BipedBone.INDEX0_R:
            return new BipedBone[]{ BipedBone.INDEX1_R };
        case BipedBone.INDEX1_R:
            return new BipedBone[]{ BipedBone.INDEX2_R };
        case BipedBone.MIDDLE0_R:
            return new BipedBone[]{ BipedBone.MIDDLE1_R };
        case BipedBone.MIDDLE1_R:
            return new BipedBone[]{ BipedBone.MIDDLE2_R };
        case BipedBone.RING0_R:
            return new BipedBone[]{ BipedBone.RING1_R };
        case BipedBone.RING1_R:
            return new BipedBone[]{ BipedBone.RING2_R };
        case BipedBone.PINKY0_R:
            return new BipedBone[]{ BipedBone.PINKY1_R };
        case BipedBone.PINKY1_R:
            return new BipedBone[]{ BipedBone.PINKY2_R };
        case BipedBone.UPPER_LEG_L:
            return new BipedBone[]{ BipedBone.LEG_L };
        case BipedBone.LEG_L:
            return new BipedBone[]{ BipedBone.FOOT_L };
        case BipedBone.UPPER_ARM_L:
            return new BipedBone[]{ BipedBone.ARM_L };
        case BipedBone.ARM_L:
            return new BipedBone[]{ BipedBone.HAND_L };
        case BipedBone.HAND_L:
            return new BipedBone[]{ BipedBone.THUMB0_L, BipedBone.INDEX0_L, BipedBone.MIDDLE0_L, BipedBone.RING0_L, BipedBone.PINKY0_L, };
        case BipedBone.SHOULDER_L:
            return new BipedBone[]{ BipedBone.UPPER_ARM_L };
        case BipedBone.THUMB0_L:
            return new BipedBone[]{ BipedBone.THUMB1_L };
        case BipedBone.THUMB1_L:
            return new BipedBone[]{ BipedBone.THUMB2_L };
        case BipedBone.INDEX0_L:
            return new BipedBone[]{ BipedBone.INDEX1_L };
        case BipedBone.INDEX1_L:
            return new BipedBone[]{ BipedBone.INDEX2_L };
        case BipedBone.MIDDLE0_L:
            return new BipedBone[]{ BipedBone.MIDDLE1_L };
        case BipedBone.MIDDLE1_L:
            return new BipedBone[]{ BipedBone.MIDDLE2_L };
        case BipedBone.RING0_L:
            return new BipedBone[]{ BipedBone.RING1_L };
        case BipedBone.RING1_L:
            return new BipedBone[]{ BipedBone.RING2_L };
        case BipedBone.PINKY0_L:
            return new BipedBone[]{ BipedBone.PINKY1_L };
        case BipedBone.PINKY1_L:
            return new BipedBone[]{ BipedBone.PINKY2_L };
        }
        return null;
    }

    
    public static BipedBone? GetHierarchyPrimaryChild( BipedBone bone ){
        switch( bone ){
        case BipedBone.HIPS:
            return BipedBone.LOWER_SPINE;
        case BipedBone.LOWER_SPINE:
            return BipedBone.UPPER_SPINE;
        case BipedBone.UPPER_SPINE:
            return BipedBone.NECK;
        case BipedBone.NECK:
            return BipedBone.HEAD;
        case BipedBone.UPPER_LEG_R:
            return BipedBone.LEG_R;
        case BipedBone.LEG_R:
            return BipedBone.FOOT_R;
        case BipedBone.UPPER_ARM_R:
            return BipedBone.ARM_R;
        case BipedBone.ARM_R:
            return BipedBone.HAND_R;
        case BipedBone.SHOULDER_R:
            return BipedBone.UPPER_ARM_R;
        case BipedBone.THUMB0_R:
            return BipedBone.THUMB1_R;
        case BipedBone.THUMB1_R:
            return BipedBone.THUMB2_R;
        case BipedBone.INDEX0_R:
            return BipedBone.INDEX1_R;
        case BipedBone.INDEX1_R:
            return BipedBone.INDEX2_R;
        case BipedBone.MIDDLE0_R:
            return BipedBone.MIDDLE1_R;
        case BipedBone.MIDDLE1_R:
            return BipedBone.MIDDLE2_R;
        case BipedBone.RING0_R:
            return BipedBone.RING1_R;
        case BipedBone.RING1_R:
            return BipedBone.RING2_R;
        case BipedBone.PINKY0_R:
            return BipedBone.PINKY1_R;
        case BipedBone.PINKY1_R:
            return BipedBone.PINKY2_R;
        case BipedBone.UPPER_LEG_L:
            return BipedBone.LEG_L;
        case BipedBone.LEG_L:
            return BipedBone.FOOT_L;
        case BipedBone.UPPER_ARM_L:
            return BipedBone.ARM_L;
        case BipedBone.ARM_L:
            return BipedBone.HAND_L;
        case BipedBone.SHOULDER_L:
            return BipedBone.UPPER_ARM_L;
        case BipedBone.THUMB0_L:
            return BipedBone.THUMB1_L;
        case BipedBone.THUMB1_L:
            return BipedBone.THUMB2_L;
        case BipedBone.INDEX0_L:
            return BipedBone.INDEX1_L;
        case BipedBone.INDEX1_L:
            return BipedBone.INDEX2_L;
        case BipedBone.MIDDLE0_L:
            return BipedBone.MIDDLE1_L;
        case BipedBone.MIDDLE1_L:
            return BipedBone.MIDDLE2_L;
        case BipedBone.RING0_L:
            return BipedBone.RING1_L;
        case BipedBone.RING1_L:
            return BipedBone.RING2_L;
        case BipedBone.PINKY0_L:
            return BipedBone.PINKY1_L;
        case BipedBone.PINKY1_L:
            return BipedBone.PINKY2_L;
        }
        return null;
    }

    public static bool Has( BipedMask mask, BipedMask entry ){
        return ( (int)mask & (int)entry ) == (int)entry;
    }

    public static List<BipedBone> RagdollMaskToBones( BipedMask mask ){

        List<BipedBone> bones = new List<BipedBone>();
        if( mask.HasFlag( BipedMask.ALL ) ){
            var values = System.Enum.GetValues(typeof(BipedBone));
            foreach( var val in values ){
                bones.Add( (BipedBone)val );
            }
            return bones;
        }

        if( mask.HasFlag( BipedMask.SPINE_CHAIN ) ) bones.AddRange( new BipedBone[]{
            BipedBone.HIPS,BipedBone.LOWER_SPINE,BipedBone.UPPER_SPINE,BipedBone.NECK,BipedBone.HEAD
        } );
        if( mask.HasFlag( BipedMask.HIPS ) ) bones.Add( BipedBone.HIPS );
        if( mask.HasFlag( BipedMask.LOWER_SPINE ) ) bones.Add( BipedBone.LOWER_SPINE );
        if( mask.HasFlag( BipedMask.UPPER_SPINE ) ) bones.Add( BipedBone.UPPER_SPINE );
        if( mask.HasFlag( BipedMask.NECK ) ) bones.Add( BipedBone.NECK );
        if( mask.HasFlag( BipedMask.HEAD ) ) bones.Add( BipedBone.HEAD );
        if( mask.HasFlag( BipedMask.ARM_CHAIN_R ) ) bones.AddRange( new BipedBone[]{
            BipedBone.SHOULDER_R,BipedBone.UPPER_ARM_R,BipedBone.ARM_R,BipedBone.HAND_R,
        } );
        if( mask.HasFlag( BipedMask.LEG_CHAIN_R ) ) bones.AddRange( new BipedBone[]{
            BipedBone.UPPER_LEG_R,BipedBone.LEG_R,BipedBone.FOOT_R,
        } );
        if( mask.HasFlag( BipedMask.FINGERS_R ) ) bones.AddRange( new BipedBone[]{
             BipedBone.THUMB0_R, BipedBone.INDEX0_R, BipedBone.MIDDLE0_R, BipedBone.RING0_R, BipedBone.PINKY0_R,
             BipedBone.THUMB1_R, BipedBone.INDEX1_R, BipedBone.MIDDLE1_R, BipedBone.RING1_R, BipedBone.PINKY1_R,
             BipedBone.THUMB2_R, BipedBone.INDEX2_R, BipedBone.MIDDLE2_R, BipedBone.RING2_R, BipedBone.PINKY2_R,
        } );
        if( mask.HasFlag( BipedMask.ARM_CHAIN_L ) ) bones.AddRange( new BipedBone[]{
            BipedBone.SHOULDER_L,BipedBone.UPPER_ARM_L,BipedBone.ARM_L,BipedBone.HAND_L,
        } );
        if( mask.HasFlag( BipedMask.LEG_CHAIN_L ) ) bones.AddRange( new BipedBone[]{
            BipedBone.UPPER_LEG_L,BipedBone.LEG_L,BipedBone.FOOT_L,
        } );
        if( mask.HasFlag( BipedMask.SHOULDER_R ) ) bones.Add( BipedBone.SHOULDER_R );
        if( mask.HasFlag( BipedMask.SHOULDER_L ) ) bones.Add( BipedBone.SHOULDER_L );
        if( mask.HasFlag( BipedMask.FINGERS_L ) ) bones.AddRange( new BipedBone[]{
             BipedBone.THUMB0_L, BipedBone.INDEX0_L, BipedBone.MIDDLE0_L, BipedBone.RING0_L, BipedBone.PINKY0_L,
             BipedBone.THUMB1_L, BipedBone.INDEX1_L, BipedBone.MIDDLE1_L, BipedBone.RING1_L, BipedBone.PINKY1_L,
             BipedBone.THUMB2_L, BipedBone.INDEX2_L, BipedBone.MIDDLE2_L, BipedBone.RING2_L, BipedBone.PINKY2_L,
        } );

        return bones;
    }
    
    public static BipedBone[] deltaTposeBones = new BipedBone[]{
        BipedBone.HIPS,
        BipedBone.LOWER_SPINE,
        BipedBone.UPPER_SPINE, //
        BipedBone.NECK,
        BipedBone.HEAD,
        // BipedBone.SHOULDER_R,    BipedBone.SHOULDER_L,
    };
    
    public static BipedBone[] humanMuscles = new BipedBone[]{
        BipedBone.HIPS,
        BipedBone.LOWER_SPINE,
        BipedBone.UPPER_SPINE, //
        BipedBone.HEAD,
        BipedBone.UPPER_LEG_R,    BipedBone.UPPER_LEG_L,
        BipedBone.LEG_R,          BipedBone.LEG_L,
        BipedBone.FOOT_R,         BipedBone.FOOT_L,
        BipedBone.UPPER_ARM_R,    BipedBone.UPPER_ARM_L,
        BipedBone.ARM_R,          BipedBone.ARM_L,
        BipedBone.HAND_R,         BipedBone.HAND_L,
    };

    public static int GetMuscleIndex( BipedBone ragdollBone ){
        if( ragdollBone <= BipedBone.UPPER_SPINE ){
            return (int)ragdollBone;
        }else if( ragdollBone <= BipedBone.FOOT_L ){
            return (int)ragdollBone-1;
        }else if( ragdollBone <= BipedBone.SHOULDER_L ){
            return -1;
        }else if( ragdollBone <= BipedBone.HAND_L ){
            return (int)ragdollBone-3;
        }else{
            return -1;
        }
    }

    public Bone<BipedBone>[] bones = new Bone<BipedBone>[ System.Enum.GetValues(typeof(BipedBone)).Length];
    public readonly Quaternion[] animLocalDeltas = new Quaternion[ System.Enum.GetValues(typeof(BipedBone)).Length ];
    public readonly Quaternion[] animParentDeltas = new Quaternion[ System.Enum.GetValues(typeof(BipedBone)).Length ];
    public readonly Quaternion[] spineTposeDeltas = new Quaternion[ deltaTposeBones.Length ];
    public float hipHeight;
    public float floorToHeadHeight;
    public Vector3 localHeadEyeCenter;
    public float headRadius;
    public float localHeadForeheadY;
    public float footHeight;
    public float footCenterDistance;
    public float shoulderWidth;
    public float upperArmLength;
    public float armThickness;
    public float armLength;
    public Quaternion footForwardOffset;

    public Bone<BipedBone> this[ BipedBone ragdollBone ]{
        get{ return bones[ (int)ragdollBone ]; }
        set{ bones[ (int)ragdollBone ] = value; }
    }

    //build ragdollprofile onto Model given bone list
    public BipedProfile( string[] boneList, string[] _blendShapeBindings, Model alternate ){
        if( boneList == null ) throw new System.Exception("Insufficient bone name list");
        if( boneList.Length > Size() ) throw new System.Exception("Bone name list is too large");
        if( boneList.Length < Size() ){
            //resize to fit Size()
            var newBoneList = new string[ Size() ];
            System.Array.Copy( boneList, newBoneList, boneList.Length );
            boneList = newBoneList;
        }
        if( alternate == null ) throw new System.Exception("Cannot duplicate RagdollProfile with null alternate");
        if( alternate.skinnedMeshRenderer == null ) throw new System.Exception("Alternate Model needs skinned mesh renderer");
        
        for( int i=0; i<bones.Length; i++ ){
            
            var targetName = boneList[i];
            if( targetName == null ) continue;
            if( targetName == "" && i >= BipedProfile.nonOptionalBoneCount ) continue;   //allow skipping of optional bones
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
            bones[i] = new Bone<BipedBone>( alternateBone, (BipedBone)i );
        }

        blendShapeBindings = _blendShapeBindings;
    }

    public BipedProfile( Model autoBindModel ){
        if( autoBindModel == null || autoBindModel.bones == null ){
            throw new System.Exception("Cannot autobind model with null bones");
        }
        blendShapeBindings = new string[ System.Enum.GetValues(typeof(RagdollBlendShape)).Length ];

        var autoBindTransforms = new List<Tuple<string,BipedBone>>();
        autoBindTransforms.Add( new Tuple<string, BipedBone>( "hips", BipedBone.HIPS ) );
        autoBindTransforms.Add( new Tuple<string, BipedBone>( "lower_spine", BipedBone.LOWER_SPINE ) );
        autoBindTransforms.Add( new Tuple<string, BipedBone>( "upper_spine", BipedBone.UPPER_SPINE ) );
        autoBindTransforms.Add( new Tuple<string, BipedBone>( "shoulder_l", BipedBone.SHOULDER_L ) );
        autoBindTransforms.Add( new Tuple<string, BipedBone>( "foot_l", BipedBone.FOOT_L ) );
        autoBindTransforms.Add( new Tuple<string, BipedBone>( "head", BipedBone.HEAD ) );
        autoBindTransforms.Add( new Tuple<string, BipedBone>( "neck", BipedBone.NECK ) );
        autoBindTransforms.Add( new Tuple<string, BipedBone>( "hand_l", BipedBone.HAND_L ) );
        autoBindTransforms.Add( new Tuple<string, BipedBone>( "upper_arm_l", BipedBone.UPPER_ARM_L ) );
        autoBindTransforms.Add( new Tuple<string, BipedBone>( "arm_l", BipedBone.ARM_L ) );
        autoBindTransforms.Add( new Tuple<string, BipedBone>( "upper_leg_l", BipedBone.UPPER_LEG_L ) );
        autoBindTransforms.Add( new Tuple<string, BipedBone>( "leg_l", BipedBone.LEG_L ) );
        
        autoBindTransforms.Add( new Tuple<string, BipedBone>( "spine1", BipedBone.HIPS ) );
        autoBindTransforms.Add( new Tuple<string, BipedBone>( "spine2", BipedBone.LOWER_SPINE ) );
        autoBindTransforms.Add( new Tuple<string, BipedBone>( "spine3", BipedBone.UPPER_SPINE ) );
        autoBindTransforms.Add( new Tuple<string, BipedBone>( "shoulder_l", BipedBone.SHOULDER_L ) );
        autoBindTransforms.Add( new Tuple<string, BipedBone>( "foot_l", BipedBone.FOOT_L ) );
        autoBindTransforms.Add( new Tuple<string, BipedBone>( "head", BipedBone.HEAD ) );
        autoBindTransforms.Add( new Tuple<string, BipedBone>( "neck", BipedBone.NECK ) );
        autoBindTransforms.Add( new Tuple<string, BipedBone>( "hand_l", BipedBone.HAND_L ) );
        autoBindTransforms.Add( new Tuple<string, BipedBone>( "armcontrol_l", BipedBone.UPPER_ARM_L ) );
        autoBindTransforms.Add( new Tuple<string, BipedBone>( "forearmcontrol_l", BipedBone.ARM_L ) );
        autoBindTransforms.Add( new Tuple<string, BipedBone>( "upperlegcontrol_l", BipedBone.UPPER_LEG_L ) );
        autoBindTransforms.Add( new Tuple<string, BipedBone>( "leg_l", BipedBone.LEG_L ) );
        
        autoBindTransforms.Add( new Tuple<string, BipedBone>( "thumb1_l", BipedBone.THUMB0_L ) );
        autoBindTransforms.Add( new Tuple<string, BipedBone>( "thumb2_l", BipedBone.THUMB1_L ) );
        autoBindTransforms.Add( new Tuple<string, BipedBone>( "thumb3_l", BipedBone.THUMB2_L ) );
        // autoBindTransforms.Add( new Tuple<string, BipedBone>( "indexfinger1_l", BipedBone.INDEX0_L ) );
        // autoBindTransforms.Add( new Tuple<string, BipedBone>( "indexfinger2_l", BipedBone.INDEX1_L ) );
        // autoBindTransforms.Add( new Tuple<string, BipedBone>( "indexfinger3_l", BipedBone.INDEX2_L ) );
        // autoBindTransforms.Add( new Tuple<string, BipedBone>( "middlefinger1_l", BipedBone.MIDDLE0_L ) );
        // autoBindTransforms.Add( new Tuple<string, BipedBone>( "middlefinger2_l", BipedBone.MIDDLE1_L ) );
        // autoBindTransforms.Add( new Tuple<string, BipedBone>( "middlefinger3_l", BipedBone.MIDDLE2_L ) );
        // autoBindTransforms.Add( new Tuple<string, BipedBone>( "ringfinger1_l", BipedBone.RING0_L ) );
        // autoBindTransforms.Add( new Tuple<string, BipedBone>( "ringfinger2_l", BipedBone.RING1_L ) );
        // autoBindTransforms.Add( new Tuple<string, BipedBone>( "ringfinger3_l", BipedBone.RING2_L ) );
        // autoBindTransforms.Add( new Tuple<string, BipedBone>( "littlefinger1_l", BipedBone.PINKY0_L ) );
        // autoBindTransforms.Add( new Tuple<string, BipedBone>( "littlefinger2_l", BipedBone.PINKY1_L ) );
        // autoBindTransforms.Add( new Tuple<string, BipedBone>( "littlefinger3_l", BipedBone.PINKY2_L ) );

        autoBindTransforms.Add( new Tuple<string, BipedBone>( "index1_l", BipedBone.INDEX0_L ) );
        autoBindTransforms.Add( new Tuple<string, BipedBone>( "index2_l", BipedBone.INDEX1_L ) );
        autoBindTransforms.Add( new Tuple<string, BipedBone>( "index3_l", BipedBone.INDEX2_L ) );
        autoBindTransforms.Add( new Tuple<string, BipedBone>( "middle1_l", BipedBone.MIDDLE0_L ) );
        autoBindTransforms.Add( new Tuple<string, BipedBone>( "middle2_l", BipedBone.MIDDLE1_L ) );
        autoBindTransforms.Add( new Tuple<string, BipedBone>( "middle3_l", BipedBone.MIDDLE2_L ) );
        autoBindTransforms.Add( new Tuple<string, BipedBone>( "ring1_l", BipedBone.RING0_L ) );
        autoBindTransforms.Add( new Tuple<string, BipedBone>( "ring2_l", BipedBone.RING1_L ) );
        autoBindTransforms.Add( new Tuple<string, BipedBone>( "ring3_l", BipedBone.RING2_L ) );
        autoBindTransforms.Add( new Tuple<string, BipedBone>( "pinky1_l", BipedBone.PINKY0_L ) );
        autoBindTransforms.Add( new Tuple<string, BipedBone>( "pinky2_l", BipedBone.PINKY1_L ) );
        autoBindTransforms.Add( new Tuple<string, BipedBone>( "pinky3_l", BipedBone.PINKY2_L ) );

        foreach( var tuple in autoBindTransforms ){
            foreach( var bone in autoBindModel.bones ){
                if( bone.name.ToLower() == tuple._1 ){
                    AssignToRagdollProfile( bone, tuple._2 );

                    if( BipedProfile.HasMirrorVariant( tuple._2 ) ){
                        AssignToRagdollProfileMirrored( autoBindModel.bones, tuple._2-1, bone.name );
                    }
                }
            }
        }
        
        var autoBindBlendShapes = new List<Tuple<string,RagdollBlendShape>>();
        autoBindBlendShapes.Add( new Tuple<string, RagdollBlendShape>( "ah", RagdollBlendShape.AH ) );
        autoBindBlendShapes.Add( new Tuple<string, RagdollBlendShape>( "blink", RagdollBlendShape.BLINK ) );
        autoBindBlendShapes.Add( new Tuple<string, RagdollBlendShape>( "ee", RagdollBlendShape.EE ) );
        autoBindBlendShapes.Add( new Tuple<string, RagdollBlendShape>( "mad", RagdollBlendShape.MAD ) );
        autoBindBlendShapes.Add( new Tuple<string, RagdollBlendShape>( "oh", RagdollBlendShape.OH ) );
        autoBindBlendShapes.Add( new Tuple<string, RagdollBlendShape>( "sad", RagdollBlendShape.SAD ) );
        autoBindBlendShapes.Add( new Tuple<string, RagdollBlendShape>( "smile", RagdollBlendShape.SMILE ) );

        foreach( var tuple in autoBindBlendShapes ){
            var index = autoBindModel.skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex( tuple._1 );
            if( index != -1 ){
                blendShapeBindings[ (int)tuple._2 ] = tuple._1;
            }
        }
    }

    public void AssignToRagdollProfileMirrored( Transform[] bones, BipedBone mirroredBoneEnum, string sourceName ){
        if( bones == null ){
            throw new System.Exception("Cannot assign to ragdoll profile mirrored with null bones");
        }
        sourceName = Regex.Replace( sourceName, $"{"_L"}$", "_R" );
        sourceName = Regex.Replace( sourceName, $"{"_l"}$", "_r" );
        foreach( var otherBone in bones ){
            if( otherBone.name == sourceName ){
                AssignToRagdollProfile( otherBone, mirroredBoneEnum );
            }
        }
    }

    public void AssignToRagdollProfile( Transform boneTransform, BipedBone boneEnum ){
        if( boneTransform == null ){
            bones[ (int)boneEnum ] = null;
        }else{
            bones[ (int)boneEnum ] = new Bone<BipedBone>( boneTransform, boneEnum );
        }
    }

    public BipedProfile( BipedProfile copy ){
        if( copy == null ) throw new System.Exception("Cannot duplicate null RagdollProfile");

        System.Array.Copy( copy.bones, bones, bones.Length );
        hipHeight = copy.hipHeight;
    }

    public bool BoneInfoHasColliders( Bone<BipedBone> boneInfo ){
        for( int j=0; j<boneInfo.transform.childCount; j++ ){
            if( boneInfo.transform.GetChild(j).gameObject.layer == WorldUtil.itemsLayer ){
                return true;
            }
        }
        return false;
    }

    public override bool ValidateProfile( Model model, out string message ){
        if( model == null ){
            message = "Model is null";
            return false;
        }
        message = null;
        bool valid = true;
        var defaultColliders = new List<BipedBone>();
        for( int i=0; i<bones.Length; i++ ){
            //ignore optional mirrored variants
            var ragdollBone = (BipedBone)i;
            if( BipedProfile.IsOptional( ragdollBone ) ) continue;
            var boneInfo = bones[i];
            if( boneInfo == null || boneInfo.transform == null ){
                message = message == null ? "" : message;
                message += ( ragdollBone ).ToString()+" (Unassigned)\n";
                valid = false;
            }else{
                //check if has colliders
                bool hasColliders = false;
                if( !BoneInfoHasColliders( boneInfo ) ){
                    var mirrorBoneEnum = BipedProfile.GetBoneMirrorVariant( ragdollBone );
                    if( mirrorBoneEnum.HasValue && BoneInfoHasColliders( bones[ (int)mirrorBoneEnum.Value ] ) ){
                        hasColliders = true;
                    }
                }else{
                    hasColliders = true;
                }
                if( hasColliders != System.Array.Exists( BipedProfile.humanMuscles, element => element==ragdollBone ) ){
                    message = message == null ? "" : message;
                    if( !hasColliders ){
                        defaultColliders.Add( ragdollBone );
                    }else{
                        message += ragdollBone.ToString()+" (Collider will be ignored)\n";
                    }
                }
            }
        }
        if( defaultColliders.Count > 0 ){
            message += "Default colliders will be used for:\n";
            foreach( var defaultCollider in defaultColliders ){
                message += defaultCollider.ToString()+", ";
            }
        }
        if( !valid ) return false;
        
        //hips not allowed to be parented to anything
        var hips = this[ BipedBone.HIPS ].transform;
        if( hips.parent != model.armature ){
            message = message == null ? "" : message;
            message += "Hip bone \""+hips.name+"\" must not be parented to any bone. Currently parented to \""+hips.parent.name+"\"";
        }
        
        valid &= ValidateBoneConnection( BipedBone.HIPS, BipedBone.LOWER_SPINE, ref message );
        valid &= ValidateBoneConnection( BipedBone.LOWER_SPINE, BipedBone.UPPER_SPINE, ref message );
        valid &= ValidateBoneConnection( BipedBone.UPPER_SPINE, BipedBone.NECK, ref message );
        valid &= ValidateBoneConnection( BipedBone.NECK, BipedBone.HEAD, ref message );
        valid &= ValidateBoneConnection( BipedBone.UPPER_SPINE, BipedBone.SHOULDER_R, ref message );
        valid &= ValidateBoneConnection( BipedBone.SHOULDER_R, BipedBone.UPPER_ARM_R, ref message );
        valid &= ValidateBoneConnection( BipedBone.UPPER_ARM_R, BipedBone.ARM_R, ref message );
        valid &= ValidateBoneConnection( BipedBone.ARM_R, BipedBone.HAND_R, ref message );
        valid &= ValidateBoneConnection( BipedBone.HIPS, BipedBone.UPPER_LEG_R, ref message );
        valid &= ValidateBoneConnection( BipedBone.UPPER_LEG_R, BipedBone.LEG_R, ref message );
        valid &= ValidateBoneConnection( BipedBone.LEG_R, BipedBone.FOOT_R, ref message );

        return valid;
    }

    private bool ValidateBoneConnection( BipedBone boneEnum0, BipedBone boneEnum1, ref string message ){
        var bone0 = bones[ (int)boneEnum0 ];
        var bone1 = bones[ (int)boneEnum1 ];
        for( int i=0; i<bone0.transform.childCount; i++ ){
            if( bone0.transform.GetChild(i) == bone1.transform ){
                return true;
            }
        }
        message = message == null ? "" : message;
        message += "\""+bone0.transform.name+"\" to \""+bone1.transform.name+"\" (Unconnected)\n";
        return false;
    }
}

}