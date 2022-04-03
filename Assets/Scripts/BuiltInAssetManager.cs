using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using MLAPI;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace viva{

public class BuiltInAssetManager: MonoBehaviour{
    
    [SerializeField]
    private GameObject m_playerPrefab;
    public GameObject playerPrefab { get{ return m_playerPrefab; } }
    [SerializeField]
    private BuiltInVivaObjectList builtInAnimations;
    [SerializeField]
    public Shader modelDefaultShader;
    [SerializeField]
    public Shader modelUnlitShader;
    [SerializeField]
    public Material modeOutlineMaterial;
    [SerializeField]
    private BipedRagdoll m_bipedPrefab;
    public BipedRagdoll bipedPrefab { get{ return m_bipedPrefab; } }
    [SerializeField]
    private AnimalRagdoll m_animalPrefab;
    public AnimalRagdoll animalPrefab { get{ return m_animalPrefab; } }
    [SerializeField]
    public PhysicMaterial ragdollActivePhysicMaterial;
    [SerializeField]
    public PhysicMaterial ragdollHandPhysicMaterial;
    [SerializeField]
    public PhysicMaterial ragdollStickyHandPhysicMaterial;
    [SerializeField]
    public PhysicMaterial ragdollFootPhysicMaterial;
    [SerializeField]
    public Mesh grabbableCylinderMesh;
    [SerializeField]
    public Material grabbableMaterial;
    [SerializeField]
    public Vector3[] grabbableCylinderDirectionOffset = new Vector3[3];
    [SerializeField]
    public SpeechBubble speechBubblePrefab;
    [SerializeField]
    public Vector3[] tPoseEuler = new Vector3[51];
    [SerializeField]
    public float[] tPoseUp = new float[51];
    [SerializeField]
    public Texture2D defaultFBXThumbnail;
    [SerializeField]
    private GameObject m_gameUIPrefab;
    public GameObject gameUIPrefab { get{ return m_gameUIPrefab; } }
    [SerializeField]
    public GameObject commandLibraryEntryPrefab;
    [SerializeField]
    public Material[] defaultModelMaterials;
    [SerializeField]
    public Material[] toonModelMaterials;
    [SerializeField]
    private _InternalSerializedSoundGroup[] physicsSoundGroups;
    [SerializeField]
    private AudioClip[] physicsDragSounds;
    [SerializeField]
    private PhysicsSoundSourceSettings[] physicsSoundSoundSettings;
    [SerializeField]
    private WeatherSound[] weatherSounds;
    [SerializeField]
    private Sprite[] sprites;
    [SerializeField]
    public PhysicMaterial capsuleGround;
    [SerializeField]
    public PhysicMaterial capsuleAir;
    [SerializeField]
    private PhysicMaterial[] physicMaterials;
    
    public static BuiltInAssetManager main;

    public Sprite FindSprite( string name ){
        foreach( var sprite in sprites ){
            if( sprite.name == name ) return sprite;
        }
        return null;
    }

    public PhysicMaterial FindPhysicMaterial( string name ){
        foreach( var physicMaterial in physicMaterials ){
            if( physicMaterial.name == name ) return physicMaterial;
        }
        return null;
    }

    public WeatherSound FindWeatherSound( string name ){
        foreach( var weatherSound in weatherSounds ){
            if( weatherSound.name == name ) return weatherSound;
        }
        return null;
    }

    public _InternalSerializedSoundGroup FindPhysicsSoundGroup( string name ){
        if( string.IsNullOrEmpty(name) ) return null;
        foreach( var group in physicsSoundGroups ){
            if( group.name == name ) return group;
        }
        Debugger.LogError("Could not find physics sound group with the name \""+name+"\"");
        return null;
    }

    public PhysicsSoundSourceSettings FindPhysicsSoundSourceSetting( string name ){
        if( string.IsNullOrEmpty(name) ) return null;
        foreach( var setting in physicsSoundSoundSettings ){
            if( setting.name == name ) return setting;
        }
        Debugger.LogError("Could not find physics sound source setting with the name \""+name+"\"");
        return null;
    }
    
    public AudioClip FindPhysicsDragSound( string name ){
        if( string.IsNullOrEmpty(name) ) return null;
        foreach( var audioClip in physicsDragSounds ){
            if( audioClip.name == name ) return audioClip;
        }
        Debugger.LogError("Could not find physics drag sound with the name \""+name+"\"");
        return null;
    }

    public string[] SettingsMaterialNames(){
        var names = new string[ defaultModelMaterials.Length ];
        for( int i=0; i<names.Length; i++ ){
            names[i] = defaultModelMaterials[i].name;
        }
        return names;
    }

    public Material FindToonMaterialByShader( string shader ){
        foreach( var mat in toonModelMaterials ){
            if( mat.name == shader ) return mat;
        }
        return null;
    }

    public Material FindMaterialByShader( string shader ){
        foreach( var mat in defaultModelMaterials ){
            if( mat.name == shader ) return mat;
        }
        return null;
    }

#if UNITY_EDITOR

    public void ArchiveAnimation( string name, string value ){
        var obj = new BuiltInVivaObject();
        obj.fileData = value;
        AssetDatabase.CreateAsset( obj, "Assets/Built-In/"+name+".asset" );
        AssetDatabase.SaveAssets();
        
        int index = -1;
        for( int i=0; i<builtInAnimations.objects.Count; i++ ){
            if( builtInAnimations.objects[i].name == name ){
                index = i;
                break;
            }
        }
        if( index == -1 ){
            builtInAnimations.objects.Add( obj );
        }else{
            builtInAnimations.objects[index] = obj;
        }
    }
#endif

    public void Awake(){
        main = this;
        foreach( var obj in builtInAnimations.objects ){
            Animation.RegisterBuiltInAnimation( obj.fileData );
        }
    }
    
    public void ApplyTPose( Model model ){
        for( int i=0; i<model.bipedProfile.bones.Length; i++ ){
            var boneEnum = (BipedBone)i;
            if( BipedProfile.IsOptional( boneEnum ) ) continue;
            var boneInfo = model.bipedProfile.bones[i];
            if( boneInfo == null || boneInfo.transform == null ) continue;
            
            boneInfo.transform.transform.eulerAngles = tPoseEuler[i];
        }
    }

    public void SetupBuiltInAnimationSets( AnimationSet animationSet, Character character ){
        var animLayerIndex = character.isPossessed ? character.altAnimationLayerIndex : character.mainAnimationLayerIndex;
        var stand = animationSet.GetBodySet( character.isPossessed ? "stand legs" : "stand", animLayerIndex );

        stand.Single( "debug", "tpose", false, 0 );
        
        var walk = new AnimationSingle( Animation.Load("walk"), character, true, 1f, animLayerIndex );
        walk.AddEvent( Event.Footstep(.44f,false) );
        walk.AddEvent( Event.Footstep(.96f,true) );
        var run = new AnimationSingle( Animation.Load("run"), character, true, 1f, animLayerIndex );
        run.AddEvent( Event.Footstep(.2f,true) );
        run.AddEvent( Event.Footstep(.65f,false) );

        var standLocomotion = stand.Mixer( "idle",
            new AnimationNode[]{
                new AnimationSingle( Animation.Load("body_stand_happy_idle1"), character, true, character.isPossessed ? 0.01f : 1f, animLayerIndex ),
                walk,
                run
            },
            new Weight[]{
                character.GetWeight("idle"),
                character.GetWeight("walking"),
                character.GetWeight("running")
            },
            true
        );
        standLocomotion.curves[BipedRagdoll.emotionID] = new Curve(1f);

        character.GetWeight("idle").value = 1.0f;
        character.GetWeight("walking").value = 0.0f;
        character.GetWeight("running").value = 0.0f;

        //pickup animations
        var pickupFloorRight = new AnimationSingle( Animation.Load("stand_happy_pickup_floor_alt_right"), character, false, 0.9f );
        var pickupChestRight = new AnimationSingle( Animation.Load("stand_pickup_chest_right"), character, false, 0.9f );
        var pickupFloorLeft = new AnimationSingle( Animation.Load("stand_happy_pickup_floor_alt_left"), character, false, 0.9f );
        var pickupChestLeft = new AnimationSingle( Animation.Load("stand_pickup_chest_left"), character, false, 0.9f );

        var standPickupRight = stand.Mixer(
            "pickup right",
            new AnimationNode[]{
                pickupChestRight,
                pickupFloorRight,
            },
            new Weight[]{
                character.GetWeight("pickup_height_chest"),
                character.GetWeight("pickup_height_floor")
            }
        );
        standPickupRight.nextState = standLocomotion;
        standPickupRight.curves[BipedRagdoll.emotionID] = new Curve(1f);
        
        var standPickupLeft = stand.Mixer(
            "pickup left",
            new AnimationNode[]{
                pickupChestLeft,
                pickupFloorLeft,
            },
            new Weight[]{
                character.GetWeight("pickup_height_chest"),
                character.GetWeight("pickup_height_floor")
            }
        );
        standPickupLeft.nextState = standLocomotion;
        standPickupLeft.curves[BipedRagdoll.emotionID] = new Curve(1f);

        var jump = stand.Single( "jump", "stand_jump", false, 1f );
        jump.nextState = stand["idle"];

        var refuse = stand.Single( "refuse", "stand_refuse", false );
        refuse.AddEvent( Event.Voice(0.1f,"refuse"));
        refuse.curves[BipedRagdoll.headID] = new Curve(0f,0.1f);
        refuse.curves[BipedRagdoll.eyesID] = new Curve(0f,0.2f);
        refuse.nextState = stand["idle"];
        
        var agree = stand.Single( "agree", "stand_agree", false );
        agree.AddEvent( Event.Voice(0.1f,"misc"));
        agree.curves[BipedRagdoll.headID] = new Curve(0.3f,0.2f);
        agree.curves[BipedRagdoll.eyesID] = new Curve(0f,0.2f);
        agree.nextState = stand["idle"];
    }


    public void Install( object obj, string root, string filename, string ext ){
        var path = root+"/"+Path.GetFileNameWithoutExtension( filename )+ext;
        if( Tools.SaveJson( obj, true, path ) ){
            
            Debug.Log("Installed "+path);
            Sound.main.PlayGlobalUISound( UISound.SAVED );
        }else{
            UI.main.messageDialog.DisplayError( MessageDialog.Type.ERROR, "Could not install "+ext, "Failed to write to \""+path+"\"" );
            Sound.main.PlayGlobalUISound( UISound.FATAL_ERROR );
        }
    }
}

}