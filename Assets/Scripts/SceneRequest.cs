using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Unity.Jobs;
using Unity.Collections;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


namespace viva{

public class SceneRequest: SpawnableImportRequest{

    private delegate void InstanceSpawnCallback( SpawnableImportRequest request, SceneSettings.InstanceData instanceData );

    public SceneSettings sceneSettings { get; private set; }
    public SceneSwitcher sceneSwitcher;
    
    private int progressStep = 0;
    private int progressMaxSteps = 0;


    public SceneRequest( string _filepath ):base( _filepath, ImportRequestType.SESSION ){
    }

    protected override string OnImport(){
        sceneSettings = InstanceSettings.Load<SceneSettings>( filepath, this, sceneSettings );
        if( sceneSettings == null ) return "Could not import scene \""+filepath+"\"";
        HintMessage.hintsDisplayed = (HintMessage.Hints)sceneSettings.hintsDisplayed;
        return null;
    }

    protected override IEnumerator OnPreload(){
        yield return null;
    }

    protected override void OnPreloadInstant(){
    }

    protected override void OnSpawnInstant( SpawnProgress progress ){
    }

    protected override IEnumerator OnSpawn( SpawnProgress progress ){
        
        Debugger._InternalReset();
        //validate session settings
        if( sceneSettings.characterDatas == null ){
            Debugger.LogError("Session character datas is null");
            yield break;
        }
        if( sceneSettings.itemDatas == null ){
            Debugger.LogError("Session item datas is null");
            yield break;
        }
        if( sceneSettings.sceneData == null ){
            Debugger.LogError("Scene data is null");
            yield break;
        }
        bool enablePlayer = true;
        if( sceneSettings.playerDataIndex < 0 || sceneSettings.playerDataIndex >= sceneSettings.characterDatas.Length ){
            enablePlayer = false;
        }
        var sceneData = sceneSettings.sceneData;
        if( SceneUtility.GetBuildIndexByScenePath( sceneData.scene ) < 0 ){
            FinishSpawning( progress, null, "Scene \""+sceneData.scene+"\" does not exist" );
            yield break;
        }
        MessageManager.main._InternalReset( sceneData.pastMessages );
        
        progressMaxSteps = sceneSettings.characterDatas.Length*2;
        progressMaxSteps += sceneSettings.itemDatas.Length*2;
        progressMaxSteps += 1;  //load map
        progressMaxSteps += 1;  //nav tiles
        progressMaxSteps += 1;  //final step
        progressStep = 0;

        //preload instances
        bool useKeyboard;
        if( VivaPlayer.user ){
            VivaPlayer.user.character.model.renderer.enabled = false;
            VivaPlayer.user.Possess( null, VivaPlayer.user.isUsingKeyboard );
            GameObject.DontDestroyOnLoad( VivaPlayer.user );
            GameObject.DontDestroyOnLoad( VivaPlayer.user.controls );
            useKeyboard = VivaPlayer.user.isUsingKeyboard;
            sceneSwitcher?.FadeTo( 0, 1 );
        }else{
            // useKeyboard = !VivaSettings.main.launchInVR;
            useKeyboard = false;
        }
        //wait for fade to end
        if( sceneSwitcher != null ) while( !sceneSwitcher.finished ) yield return null;

        //destroy all objects to clear the scene
        var oldObjects = Resources.FindObjectsOfTypeAll( typeof(VivaInstance) );
        foreach( var oldObj in oldObjects ) Viva.Destroy( (VivaInstance)oldObj );
        yield return null;  //wait 1 frame for OnDestroy to call
        
        // Debug.LogError("~~~~~~~~~~~~~~DESTROYED ALL UNUSED");
        TextureHandle.DestroyUnused();


        //load scene
        StepSceneSwitcherProgress();
        DisplaySceneSwitcherMessage( "Loading map \""+sceneData.scene+"\"");
        AmbienceManager.main._InternalReset();    //allow pre-made map props to hook into AmbienceManager listeners

        var asyncLoad = SceneManager.LoadSceneAsync( sceneData.scene, LoadSceneMode.Single );
        while( !asyncLoad.isDone ) yield return null;
        
        //switch scenes then preload assets (minimizes memory)
        yield return PreloadInstances<CharacterRequest>( sceneSettings.characterDatas, Character.instances, "character" );
        yield return PreloadInstances<ItemRequest>( sceneSettings.itemDatas, Item.instances, "item" );

        AmbienceManager.main.enabled = false;
        AmbienceManager.main.onDayEvent.currentEvent = AmbienceManager.main.GetDayEvent( sceneData.timeOfDay );
        AmbienceManager.main.onDayTimePassed.timeOfDay = sceneData.timeOfDay;
        AmbienceManager.main.SetMusic( AmbienceManager.main.GetDefaultMusic() );
        if( sceneSettings.type == "tutorial" ){
            AmbienceManager.main.cycleSeconds = 0;
        }else{
            AmbienceManager.main.cycleSeconds = 2400;
        }
        Time.timeScale = 0f;

        if( sceneSettings.type != "main" ){
            GameObject.Instantiate( BuiltInAssetManager.main.gameUIPrefab );
        }

        if( sceneSettings.sceneData.completedAchievements == null ) sceneSettings.sceneData.completedAchievements = new List<string>();
        foreach( var completedAchievement in sceneSettings.sceneData.completedAchievements ){
            AchievementManager.main.CompleteAchievement( completedAchievement, false );
        }

        VivaInstance._InternalSetIDCounterStart( sceneSettings.idCounterStart );

        //scene must be instantiated first always
        var scene = Scene.CreateSceneBase( sceneSettings );

        //spawn instances
        Scene.disableBakeQueues = true;
        var characters = new List<VivaInstance>();
        var items = new List<VivaInstance>();
        yield return SpawnInstances<CharacterRequest>( sceneSettings.characterDatas, characters, Character.instances, "character",
            delegate( SpawnableImportRequest request, SceneSettings.InstanceData instanceData ){
                var charReq = request as CharacterRequest;
                charReq.nextIDOverride = instanceData.id;
            }
        );
        yield return SpawnInstances<ItemRequest>( sceneSettings.itemDatas, items, Item.instances, "items",
            delegate( SpawnableImportRequest request, SceneSettings.InstanceData instanceData ){
                var itemReq = request as ItemRequest;
                var itemData = instanceData as SceneSettings.ItemData;
                itemReq.nextID = instanceData.id;
                itemReq.nextAttributes = itemData.attributes;
                itemReq.nextImmovable = itemData.immovable;
            }
        );
        Scene.disableBakeQueues = false;

        //bake navigation tiles
        DisplaySceneSwitcherMessage( "Baking navigation...");
        bool finished = false;
        scene.navTile.BakeNavMesh( delegate{
            finished = true;
        } );
        while( !finished ) yield return null;
        
        StepSceneSwitcherProgress();
        
        Character userCharacter = null;
        if( enablePlayer ){
            userCharacter = characters[ sceneSettings.playerDataIndex ] as Character;
            if( userCharacter == null ){
                Debug.LogError("Could not load user as character in Session");
                yield break;
            }
        }

        //load serialized tasks
        for( int i=0; i<sceneSettings.characterDatas.Length; i++ ){
            if( characters[i] == null ) continue;
            DeserializeScripts( ( characters[i] as Character ).scriptManager, sceneSettings.characterDatas[i].serializedScripts );
        }
        for( int i=0; i<sceneSettings.itemDatas.Length; i++ ){
            if( items[i] == null ) continue;
            DeserializeScripts( ( items[i] as Item ).scriptManager, sceneSettings.itemDatas[i].serializedScripts );
        }

        //load world script (if any)
        DeserializeScripts( scene.scriptManager, sceneSettings.sceneData.serializedScripts );

        //finalize
        AmbienceManager.main.enabled = true;
        Time.timeScale = 1f;
        StepSceneSwitcherProgress();
        DisplaySceneSwitcherMessage( "Finalizing...");

        //spawn or possess for user
        if( !VivaPlayer.user ){
            GameObject.Instantiate( BuiltInAssetManager.main.playerPrefab );
        }
        if( userCharacter ){
            VivaPlayer.user.Possess( userCharacter, useKeyboard );
            if( sceneSettings.type == "main" ) VivaPlayer.user.movement.mouseVelSum.y = 25f;
        }
        AmbienceManager.main.enabled = true;

        if( sceneSwitcher != null ){
            sceneSwitcher.FadeTo( 1, 0 );
            while( !sceneSwitcher.finished ) yield return null;
            GameObject.DestroyImmediate( sceneSwitcher.gameObject );
        }
        scene.scriptManager.Recompile();
        FinishSpawning( progress, scene, null );
        Viva.main.ImportTest();

        InputManager.Reset();
    }

    public override string GetInfoHeaderText(){
        return sceneSettings == null ? "" : sceneSettings.name;
    }
    public override string GetInfoBodyContentText(){
        return "";
    }
    public override void _InternalOnGenerateThumbnail(){
        if( sceneSettings != null ){
            sceneSettings._InternalOnGenerateThumbnail();
            thumbnail.Copy( sceneSettings.thumbnail );
        }else{
            thumbnail.texture = BuiltInAssetManager.main.defaultFBXThumbnail;
        }
        thumbnail.onThumbnailChange.Invoke( this );
    }
    public override void OnCreateMenuSelected(){
        GameUI.main.createMenu.DisplayVivaObjectInfo<SoundSettings>( sceneSettings, this );
        GameUI.main.createMenu.DisplayPlaySoundButton();
    }
    
    private void StepSceneSwitcherProgress(){
        progressStep++;
        if( sceneSwitcher ) sceneSwitcher.SetProgress( (float)progressStep/Mathf.Max(0,progressMaxSteps) );
    }

    private void DisplaySceneSwitcherMessage( string msg ){
        if( sceneSwitcher ) sceneSwitcher.status.text = msg;
        Debug.Log(msg);
    }

    private IEnumerator PreloadInstances<T>( SceneSettings.InstanceData[] instanceDatas, InstanceManager instanceManager, string displayType ) where T:SpawnableImportRequest{
        for( int i=0; i<instanceDatas.Length; i++ ){
            var instanceData = instanceDatas[i];
            var request = instanceManager._InternalGetRequest( instanceData.name ) as T;
            if( request == null || !request.imported ){
                continue;
            }
            StepSceneSwitcherProgress();
            DisplaySceneSwitcherMessage( "Loading "+i+"/"+instanceDatas.Length+" "+displayType+" - \""+instanceData.name+"\"");
            yield return request.Preload();
        }
    }

    private IEnumerator SpawnInstances<T>( SceneSettings.InstanceData[] itemDatas, List<VivaInstance> instances, InstanceManager instanceManager, string displayType, InstanceSpawnCallback onModify ) where T:SpawnableImportRequest{
        for( int i=0; i<itemDatas.Length; i++ ){
            var instanceData = itemDatas[i];
            var request = instanceManager._InternalGetRequest( instanceData.name ) as T;
            if( request == null ){
                Debugger.LogError("Could not load "+displayType+" \""+instanceData.name+"\"");
                instances.Add( null );
                continue;
            }
            StepSceneSwitcherProgress();
            DisplaySceneSwitcherMessage( "Spawning "+i+"/"+itemDatas.Length+" "+displayType+" - \""+instanceData.name+"\"");
            onModify( request, instanceData );

            var progress = new SpawnProgress();
            request._InternalSpawnUnlinked( false, progress );
            while( !progress.finished ){
                yield return null;
            }
            var instance = progress.result as VivaInstance;
            instances.Add( instance );
            if( progress.result == null ){
                Debugger.LogError( progress.error );
            }else{
                instance.transform.position = instanceData.transform.position;
                instance.transform.rotation = instanceData.transform.rotation;
                instance._InternalInitialize();
                instanceManager._InternalLink( instance );
            }
        }
    }

    private void DeserializeScripts( ScriptManager scriptManager, SceneSettings.SerializedScript[] serializedScripts ){
        if( scriptManager == null ) return;
        if( serializedScripts == null ) return;
        foreach( var serializedScript in serializedScripts ){
            if( string.IsNullOrEmpty( serializedScript.script ) ) continue;
            if( serializedScript.functions == null ) continue;

            var scriptInstance = scriptManager.FindScriptInstance( serializedScript.script );
            if( !scriptInstance.HasValue ){
                Debug.LogError("Could not find script \""+serializedScript.script);
            }
            foreach( var serializedFunction in serializedScript.functions ){
                if( serializedFunction == null ) continue;
                DeserializeScriptFunction( scriptInstance.Value, serializedFunction );
            }
        }
    }

    private void DeserializeScriptFunction( ScriptInstance scriptInstance, SceneSettings.SerializedFunction serializedFunction ){
        object[] parameters = new object[ serializedFunction.parameters.Length ];
        //parse and convert to actual objects
        for( int i=0; i<serializedFunction.parameters.Length; i++ ){
            var parameter = serializedFunction.parameters[i] as SceneSettings.SerializedParameter;
            object obj = Util.DeserializeGameObject( parameter.type, parameter.value );
            
            if( obj == null ){
                Debugger.LogError("Error deserializing parameter in \""+scriptInstance.script.name+"\"");
            }
            parameters[i] = obj;
        }
        var vivaScript = scriptInstance.proxy.Instance as VivaScript;
        if( vivaScript == null ){
            Debugger.LogError("Could nto deserialize script \""+scriptInstance.script.name+"\"");
        }else{
            vivaScript.Load( serializedFunction.funcName, parameters );
        }
    }
}

}