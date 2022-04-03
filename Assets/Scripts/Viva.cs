using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fbx;
using System.IO;
using MLAPI;
using System;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;


namespace viva{


public enum ServerMode{
    SERVER,
    HOST,
    CLIENT
}

[System.Serializable]
public class VivaSettings{
    public static VivaSettings main { get; private set; } = new VivaSettings();

    public int qualityLevel = 1;
    public int shadowLevel = 3;
    public int aliasingLevel = 1;
    // public bool launchInVR = true;
    public int bakeNavJobs = 2;
    public float musicVolume = 1f;
    public string[] commonCharacterScripts;

    public void Copy( VivaSettings copy ){
        if( copy == null ) return;
        qualityLevel = copy.qualityLevel;
        shadowLevel = copy.shadowLevel;
        aliasingLevel = copy.aliasingLevel;
        // launchInVR = copy.launchInVR;
        bakeNavJobs = copy.bakeNavJobs;
        commonCharacterScripts = copy.commonCharacterScripts;
    }

    public void Apply(){
        qualityLevel = Mathf.Clamp( qualityLevel, 0, 2 );
        QualitySettings.SetQualityLevel( qualityLevel );

        shadowLevel = Mathf.Clamp( shadowLevel, 0, 5 );
        var sunHD = AmbienceManager.main.sunLight.GetComponent<HDAdditionalLightData>();
        sunHD.SetShadowResolutionLevel( shadowLevel>3 ? shadowLevel-2 : shadowLevel-1 );

        Viva.main.globalVolume.sharedProfile.TryGet( out HDShadowSettings hdShadowSettings );
        switch( shadowLevel ){
        case 1:
        case 2:
            hdShadowSettings.maxShadowDistance.value = 50;
            break;
        case 3:
            hdShadowSettings.maxShadowDistance.value = 75;
            break;
        case 4:
        case 5:
            hdShadowSettings.maxShadowDistance.value = 100;
            break;
        }
        hdShadowSettings.cascadeShadowSplitCount.value = shadowLevel>3 ? 2 : 1;

        aliasingLevel = Mathf.Clamp( aliasingLevel, 0, 3 );
        var camHD = Camera.main.GetComponent<HDAdditionalCameraData>();
        if( aliasingLevel == 0 ){
            camHD.antialiasing = HDAdditionalCameraData.AntialiasingMode.None;
        }else{
            camHD.antialiasing = HDAdditionalCameraData.AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            camHD.SMAAQuality = (HDAdditionalCameraData.SMAAQualityLevel)(aliasingLevel-1);
        }

        var natureRenderer = MonoBehaviour.FindObjectOfType<VisualDesignCafe.Rendering.Nature.NatureRenderer>();
        if( natureRenderer ) natureRenderer.Draw = qualityLevel>0;
    }
}

public class Viva: MonoBehaviour{

    [SerializeField]
    public float DEBUG;
    [SerializeField]
    private Volume m_globalVolume;
    public Volume globalVolume { get{ return m_globalVolume; } }
    [SerializeField]
    private ServerMode serverMode;

    public static string contentFolder { get; private set; }
    public static Viva main { get; private set; }
    public static InputManager input { get; private set; }


    public static void Destroy( VivaInstance instance, float delay=0f ){
        if( !instance || instance._internalDestroyed ){
            Debug.LogWarning("Could not Destroy item");
            return;
        }

        instance._internalDestroyed = true;
        GameObject.Destroy( instance.gameObject, delay );
    }

    public static void Destroy( GrabContext grabContext ){
        if( !grabContext || !grabContext.IsValid() ) return;
        grabContext._InternalCleanup();
        GameObject.Destroy( grabContext );
    }

    public static void Destroy( Joint joint ){
        if( !joint ) return;
        GameObject.Destroy( joint );
    }

    public void OnApplicationFocus( bool focus ){
        var instances = Resources.FindObjectsOfTypeAll<VivaInstance>();
        var uniqueRequests = new List<ImportRequest>();
        bool reimportedFiles = false;
        foreach( var instance in instances ){
            if( instance._internalSettings == null ) continue;
            var request = instance._internalSettings._internalSourceRequest;
            if( request == null || request.type == ImportRequestType.SESSION ) continue;
            if( uniqueRequests.Contains( request ) ) continue;
            uniqueRequests.Add( request );

            if( request.CheckOutdatedImport() ){
                Debugger.LogWarning("Reimporting "+request.filepath);
                request.Import( false );
                reimportedFiles = true;
            }
        }
        if( reimportedFiles ){
            Sound.main.PlayGlobalUISound( UISound.RELOADED );
        }
    }

    public void Awake(){
        if( main ){
            Debug.LogError("FATAL ERROR MAIN ALREADY EXISTS");
            return;
        }

        main = this;
        GameObject.DontDestroyOnLoad( Viva.main );

        input = new InputManager();
        contentFolder = System.IO.Path.GetFullPath( System.IO.Directory.GetParent( Application.dataPath )+"/Content" );
        Tools.EnsureFolder( contentFolder );
        Tools.EnsureFolder( Model.root );
        Tools.EnsureFolder( TextureBinding.root );
        Tools.EnsureFolder( Animation.root );
        Tools.EnsureFolder( Script.root );
        Tools.EnsureFolder( Script.root+"/character" );
        Tools.EnsureFolder( Script.root+"/items" );
        Tools.EnsureFolder( Character.root );
        Tools.EnsureFolder( Profile.root );
        Tools.EnsureFolder( Item.root );
        Tools.EnsureFolder( SoundSettings.root );
        Tools.EnsureFolder( SceneSettings.root );
        Tools.EnsureFolder( Viva.contentFolder+"/Share" );

        UnityEngine.Rendering.TextureXR.maxViews = 2;

        //delete all old *.dll*
        DirectoryInfo directory = new DirectoryInfo( System.IO.Directory.GetParent( Application.dataPath ).ToString() );
        foreach (var file in directory.GetFiles("*dll*")){
            switch( file.Name ){
            case "UnityPlayer.dll":
            case "WinPixEventRuntime.dll":
                continue;
            }
            System.IO.File.Delete( file.FullName );
        }
        //load graphics
        var savedSettings = Tools.LoadJson<VivaSettings>( Profile.root+"/config.init" );
        if( savedSettings == null ){
            Debug.LogError("Could not load Content/config.init");
        }
        if( savedSettings.commonCharacterScripts == null ){
            savedSettings.commonCharacterScripts = new string[]{};
        }
        VivaSettings.main.Copy( savedSettings );
    }

    public void OnDestroy(){
        Tools.SaveJson( VivaSettings.main, true, Profile.root+"/config.init" );
    }

    public void Start(){
        InitializeNetworking();
        
        //initialize always on script refreshing for active characters
        var fw = FileWatcherManager.main.GetFileWatcher( Script.root+"/character" );
        fw.alwaysOn = true;
        fw.onFileChange += Script.OnScriptAssetChange;
        
        fw = FileWatcherManager.main.GetFileWatcher( Script.root+"/items" );
        fw.alwaysOn = true;
        fw.onFileChange += Script.OnScriptAssetChange;

        fw = FileWatcherManager.main.GetFileWatcher( Script.root+"/scenes" );
        fw.alwaysOn = true;
        fw.onFileChange += Script.OnScriptAssetChange;
    }

    public void InitializeNetworking(){
        // switch( serverMode ){
        // case ServerMode.SERVER:
        //     NetworkingManager.Singleton.ConnectionApprovalCallback += OnApproveConnection;
        //     NetworkingManager.Singleton.StartServer();
        //     break;
        // case ServerMode.HOST:
        //     NetworkingManager.Singleton.StartHost( Vector3.zero, Quaternion.identity, false );
        //     break;
        // case ServerMode.CLIENT:
        //     NetworkingManager.Singleton.StartClient();
        //     break;
        // }
    }

    public void ImportTest(){
        // Debug.LogError("-------------------");
        // var result = new List<Interaction>();
        // VivaPlayer.user.character.itemInteractions.FindInteractionPath( result, "strawberry flavor", VivaPlayer.user.character );
        // foreach( var interaction in result ){
        //     Debug.LogError(interaction.ToString() );
        // }

        // var hammer = Item.Spawn("hammer", new Vector3(1,1,1), Quaternion.identity);

        if( !GameUI.main ) return;
        
        var assetProcessor = GameUI.main.createMenu.CreateAssetProcessor( ImportRequest.CreateRequests( new List<string>(){
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/stand_distant_wave1_right.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/stand_distant_wave2.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/ball_BaseColorMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/ball_NormalMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/ball_MaskMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/Saiko (Yandere) v3.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/swim_onsen.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/stand_kill_chicken_right.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/chicken feather.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/knife.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/knife_BaseColorMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/knife_NormalMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/knife_MaskMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/chicken_bone.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/chicken_bone_BaseColorMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/chicken_bone_NormalMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/chicken_bone_MaskMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/saiko.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/saiko_BaseColorMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/chickenLeg_cooked_BaseColorMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/chickenLeg_cooked_NormalMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/chickenLeg_cooked_MaskMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/onsenSign.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/onsenSign_BaseColorMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/onsenSign_NormalMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/onsenSign_MaskMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/squat_idle_loop.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/stand_to_squat.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/squat_forward_loop.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/relax_idle_loop.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/squat_to_stand.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/squat_to_relax.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/relax_to_squat.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/Rat_BaseColorMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/Rat.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/stand_hug_happy_loop.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/stand_hug_to_stand.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/senko.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/senko_BaseColorMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/senko thumb.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/horsey_BaseColorMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/stand_hug_happy_loop.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/stand_to_stand_hug.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/stand_hug_happy_right.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/plank1.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/plank1_MaskMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/plank1_BaseColorMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/plank1_NormalMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/eggSplat.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/eggSplat_BaseColorMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/eggSplat_NormalMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/eggSplat_MaskMap.png",
            // // "C:/Users/Master-Donut/Documents/v/Content/TEMP/chicken_MaskMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/stand_egg_into_mixing_bowl_right.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/stand_mixing_bowl_mix_right.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/stand_reach_out_end_right.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/towel.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/towel_BaseColorMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/towel_NormalMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/towel_MaskMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/cantaloupe.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/cantaloupe_BaseColorMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/cantaloupe_NormalMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/cantaloupe_MaskMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/body_stand_tired_run.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/stand_agree.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/swim_forward.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/mona.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/mona_BaseColorMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/shoji1.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/shoji1_BaseColorMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/shoji1_NormalMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/shoji1_MaskMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/sfusuma_MaskMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/strawberry.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/strawberry_BaseColorMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/strawberry_NormalMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/strawberry_MaskMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/wheatPlant.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/wheatPlant_BaseColorMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/pallet_NormalMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/pallet_MaskMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/peachTree.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/bark_BaseColorMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/bark_NormalMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/leaves_BaseColorMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/stand_bag_put_in_right.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/stand_wear_bag_right.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/stand_remove_bag_right.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/bag.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/bag_BaseColorMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/bag_NormalMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/bag_MaskMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/Kyaru.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/Kyaru_BaseColorMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/klee.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/klee_BaseColorMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/klee.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/klee_BaseColorMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/stand_food_last_bite_right.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/stand_food_bite_right.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/stand_food_smell_right.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/stand_headpat_happy_proper_loop.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/stand_headpat_happy_start.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/stand_bow.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/Items/mixing_bowl.item",
            // "C:/Users/Master-Donut/Documents/v/Content/Items/wheat.item",
            // "C:/Users/Master-Donut/Documents/v/Content/Items/mixing_spoon.item",
            // "C:/Users/Master-Donut/Documents/v/Content/Items/mortar.item",
            // "C:/Users/Master-Donut/Documents/v/Content/Items/pestle.item",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/locomotion.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/stand_wheat_into_mortar_in_left.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/stand_wheat_into_mortar_out_left.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/stand_giddy_surprise.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/kitchen.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/stand_headpat_happy_proper_loop.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/stand_tired_poke_right.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/oven_BaseColorMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/oven_MaskMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/oven_NormalMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/counter_top_BaseColorMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/counter_top_MaskMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/counter_top_NormalMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/counter_bottom_BaseColorMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/counter_bottom_MaskMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/counter_bottom_NormalMap.png",
            
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/stand_face_prox_angry_surprise.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/stand_face_prox_happy_loop.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/stand_face_prox_happy_surprise.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/stand_pour_mortar_into_mixing_bowl_left.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/hug.cs",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/lantern.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/lanternGlass_BaseColorMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/lanternGlass_MaskMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/lantern_BaseColorMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/lantern_MaskMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/mutsuki_BaseColorMap.png",

            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/futon.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/futon_BaseColorMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/futon_NormalMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/desk_BaseColorMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/desk_MaskMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/desk_NormalMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/props_BaseColorMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/props_MaskMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/onsen_receptionDesk.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/wheat_NormalMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/wheat_BaseColorMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/wheat.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/filling_BaseColorMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/filling_MaskMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/pastry_raw.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/pastry_raw_BaseColorMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/pastry_raw_NormalMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/pastry_baked.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/pastry_baked_BaseColorMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/pastry_baked_NormalMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/mixingBowl.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/mixingBowl_BaseColorMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/mixingBowl_NormalMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/batter_BaseColorMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/batter_NormalMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/suit_man.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/suit_man_BaseColorMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/suit_man_NormalMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/suit_man_MaskMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/pestle.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/mortar.fbx",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/mortar_BaseColorMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/mortar_NormalMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/mortar_MaskMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/wheatMound_NormalMap.png",
            // "C:/Users/Master-Donut/Documents/v/Content/TEMP/wheatMound_BaseColorMap.png",
        } ) );

        assetProcessor.ProcessAll();
    }

    private void OnApproveConnection( byte[] data, ulong id, NetworkingManager.ConnectionApprovedDelegate callback ){
        Debug.LogError("APPROVING...");
        callback( true, 0, true, Vector3.zero, Quaternion.identity );
    }

    private void OnClientConnected( ulong clientID ){
        Debug.Log("Connected clientID:"+clientID);
    }
}

}