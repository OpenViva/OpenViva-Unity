using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fbx;
using System.IO;
using Unity.Jobs;
using Unity.Collections;


namespace viva{


public class CharacterRequest: SpawnableImportRequest{

    public CharacterSettings characterSettings { get; private set; }
    public static string root { get{ return Viva.contentFolder+"/Characters"; } }
    public bool stripScripts = false;
    public string rootOverride = null;
    public Character lastSpawnedCharacter { get; private set; }
    private FBXRequest fbxRequest;
    public int? nextIDOverride;


    public CharacterRequest( string _filepath ):base( _filepath, ImportRequestType.CHARACTER ){
    }
    
    public CharacterRequest( Model _model, string _name, bool _isAnimal ):base( Character.root+"/"+_name+".character", ImportRequestType.CHARACTER ){
        imported = true;
        characterSettings = new CharacterSettings( _model, _name, _isAnimal, this );
        lastSpawnedCharacter = Character.CreateCharacterBase( _model, characterSettings, null );
        FinishSpawning( new SpawnProgress(), lastSpawnedCharacter, null );
    }

    protected override string OnImport(){
        characterSettings = InstanceSettings.Load<CharacterSettings>( filepath, this, characterSettings );
        if( characterSettings == null ) return "Could not import \""+filepath+"\". File read error.";

        if( characterSettings.attributes == null ) characterSettings.attributes = new string[0];

        var blendShapeCount = System.Enum.GetValues(typeof(RagdollBlendShape)).Length;
        if( characterSettings.blendShapeBindings.Length < blendShapeCount ){
            var newArray = new string[ blendShapeCount ];
            System.Array.Copy( characterSettings.blendShapeBindings, newArray, characterSettings.blendShapeBindings.Length );
            characterSettings.blendShapeBindings = newArray;
        }

        if( fbxRequest == null ){
            fbxRequest = new FBXRequest( rootOverride == null ? Model.root+"/"+characterSettings.fbx : rootOverride+"/"+characterSettings.fbx, FBXContent.MODEL, characterSettings.serializedFBXLength );
            fbxRequest.Import();
            
            usage.Increase();
            usage.onDiscarded += fbxRequest.usage.Decrease;
            fbxRequest.usage.Increase();
            fbxRequest.usage.onDiscarded += usage.Decrease;
        }
        
        foreach( var texture in characterSettings.textures ){
            TextureHandle.Load( texture );
        }
        return null;
    }

    protected override IEnumerator OnPreload(){
        yield return fbxRequest.Preload();
        if( !fbxRequest.preloaded ){
            preloadError = "Could not load character \""+filepath+"\" because fbx preload failed:"+fbxRequest.preloadError;
            yield break;
        }
    }

    protected override void OnPreloadInstant(){
        fbxRequest.PreloadInstant();
        if( !fbxRequest.preloaded ){
            preloadError = "Could not load character \""+filepath+"\" because fbx preload failed:"+fbxRequest.preloadError;
        }
    }

    protected override void OnSpawnInstant( SpawnProgress progress ){
        var fbxProgress = new SpawnProgress();
        fbxRequest._InternalSpawnUnlinked( true, fbxProgress );

        CreateCharacter( fbxProgress.result as FBX, progress );
    }

    protected override IEnumerator OnSpawn( SpawnProgress progress ){
        var fbxProgress = new SpawnProgress();
        fbxRequest._InternalSpawnUnlinked( false, fbxProgress );
        while( !fbxProgress.finished ){
            yield return null;
        }
        CreateCharacter( fbxProgress.result as FBX, progress );
    }

    private void CreateCharacter( FBX fbx, SpawnProgress progress ){
        if( fbx == null ){
            FinishSpawning( progress, null, "Missing model \""+characterSettings.modelName+"\" in "+characterSettings.fbx );
            return;
        }
        Model model = fbx.FindModel( characterSettings.modelName );
        if( model == null ){
            FinishSpawning( progress, null, "Missing model inside fbx \""+characterSettings.modelName+"\"" );
            return;
        }
        
        Profile profile;
        try{
            if( characterSettings.isAnimal ){
                profile = new AnimalProfile( characterSettings.boneBindings, characterSettings.blendShapeBindings, model );
            }else{
                profile = new BipedProfile( characterSettings.boneBindings, characterSettings.blendShapeBindings, model );
            }
        }catch( System.Exception e ){
            FinishSpawning( progress, null, e.ToString() );
            return;
        }
        model.Resize( characterSettings.height );
        if( !model.AttemptSetProfile( profile, out string profileError ) ){
            FinishSpawning( progress, null, profileError );
            return;
        }

        characterSettings.liveEditModel = model;

        Character character;
        try{
            var idOverride = nextIDOverride;
            nextIDOverride = null;
            character = Character.CreateCharacterBase( model, characterSettings, idOverride );
        }catch( System.Exception e ){
            FinishSpawning( progress, null, e.ToString() );
            return;
        }

        if( !characterSettings.doNotUseCommonScripts && !characterSettings.isAnimal ) characterSettings.scripts = VivaSettings.main.commonCharacterScripts;
        character.scriptManager.LoadAllScripts( "character", characterSettings.scripts );

        if( characterSettings.materialSettings == null ){
            var materialNames = model._InternalGetAllMaterialNames();
            characterSettings.materialSettings = new MaterialSettings[ materialNames.Count ];
            for( int i=0; i<materialNames.Count; i++ ){
                var materialSetting = new MaterialSettings();
                materialSetting.material = materialNames[i];
                materialSetting.shader = BuiltInAssetManager.main.toonModelMaterials[0].name;    //default opaque
                characterSettings.materialSettings[i] = materialSetting;
            }
        }
        foreach( var materialSetting in characterSettings.materialSettings ){
            foreach( var root in fbx.rootModels ){
                var matTemplate = BuiltInAssetManager.main.FindToonMaterialByShader( materialSetting.shader );
                if( matTemplate == null ){
                    Debugger.LogError("Could not find shader name \""+materialSetting.shader+"\"");
                    matTemplate = BuiltInAssetManager.main.defaultModelMaterials[0];    //default to opaque
                }
                root.SetMaterial( materialSetting.material, new Material( matTemplate ) );
            }
        }

        foreach( var texture in characterSettings.textures ){
            foreach( var childModel in fbx.rootModels ){
                var textureHandle = TextureHandle.Load( texture );
                var binding = childModel.textureBindingGroup.GenerateBinding( textureHandle );
                childModel.textureBindingGroup.Add( binding );
            }
        }
        foreach( var childModel in fbx.rootModels ){
            childModel.textureBindingGroup.SaveForReset();
            childModel.textureBindingGroup.Reset( true );
        }
        character._internalOnDestroy += delegate{
            if( fbx ) GameObject.Destroy( fbx.gameObject );
        };

        Debug.Log("+CHARACTER "+filepath);
        lastSpawnedCharacter = character;
        FinishSpawning( progress, character, null );
    }

    public override string GetInfoHeaderText(){
        return "Character";
    }
    public override string GetInfoBodyContentText(){
        return "";
    }
    public override void _InternalOnGenerateThumbnail(){
        if( characterSettings != null ){
            characterSettings._InternalOnGenerateThumbnail();
            thumbnail.Copy( characterSettings.thumbnail );
        }else{
            thumbnail.texture = BuiltInAssetManager.main.defaultFBXThumbnail;
        }
        thumbnail.onThumbnailChange.Invoke( this );
    }

    public override void OnCreateMenuSelected(){
        characterSettings.OnCreateMenuSelected();
        GameUI.main.createMenu.HideCreateButton();
    }

    public override void OnCreateMenuDeselected(){
        characterSettings.OnCreateMenuDeselected();
    }

    public override void OnShare(){
        if( lastSpawnedCharacter == null ) return;

        var destZipFile = Viva.contentFolder+"/Share/"+lastSpawnedCharacter.name+".zip";
        var sourceFolder = Viva.contentFolder+"/Share/Content";
        Tools.EnsureFolder( sourceFolder );
        bool success = true;

        for( int i=0; i<lastSpawnedCharacter.model.textureBindingGroup.Count; i++ ){
            var textureBinding = lastSpawnedCharacter.model.textureBindingGroup[i];
            success &= GameUI.main.createMenu.ArchiveToShareFolder( Path.GetFullPath( TextureBinding.root+"/"+textureBinding.path ), sourceFolder );
        }
        success &= GameUI.main.createMenu.ArchiveToShareFolder( lastSpawnedCharacter.model.modelRequest.filepath, sourceFolder );
        success &= GameUI.main.createMenu.ArchiveToShareFolder( filepath, sourceFolder );

        for( int i=0; i<lastSpawnedCharacter.scriptManager.Count; i++ ){
            var script = lastSpawnedCharacter.scriptManager.GetScript(i);
            success &= GameUI.main.createMenu.ArchiveToShareFolder( script.scriptRequest.filepath, sourceFolder );
        }
        
        if( !success ){
            UI.main.messageDialog.DisplayError( MessageDialog.Type.ERROR, "Failed to share", "Could not save character file!");
        }

        //zip and delete old temp folder
        System.IO.Compression.ZipFile.CreateFromDirectory( sourceFolder, destZipFile );
        System.IO.Directory.Delete( sourceFolder, true );

        Tools.ExploreFile( destZipFile );
    }
}

}