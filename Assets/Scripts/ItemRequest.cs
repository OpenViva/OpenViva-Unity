using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Unity.Jobs;
using Unity.Collections;


namespace viva{


public class ItemRequest: SpawnableImportRequest{

    public ItemSettings itemSettings { get; private set; }
    public Item lastSpawnedItem { get; private set; }
    private FBXRequest fbxRequest;
    public int? nextID;
    public string[] nextAttributes;
    public bool? nextImmovable;


    public ItemRequest( string _filepath ):base( _filepath, ImportRequestType.ITEM ){
    }
    
    public ItemRequest( Model _model, string _name ):base( Item.root+"/"+_name+".item", ImportRequestType.ITEM ){
        if( _model == null ) throw new System.Exception("Cannot create ItemRequest from a null Model");

        imported = true;
        itemSettings = new ItemSettings( _model, _name, this );
        lastSpawnedItem = Item._InternalCreateItemBase( _model, itemSettings );
        lastSpawnedItem.model.textureBindingGroup.SaveForReset();
        lastSpawnedItem.rigidBody.isKinematic = true;
        FinishSpawning( new SpawnProgress(), lastSpawnedItem, null );
    }

    protected override string OnImport(){
        itemSettings = InstanceSettings.Load<ItemSettings>( filepath, this, itemSettings );
        if( itemSettings == null ) return "Could not import \""+filepath+"\" because the .item file is not available";
        
        if( itemSettings.attributes == null ) itemSettings.attributes = new string[0];

        if( fbxRequest != null ){
            fbxRequest.usage.Decrease();
            usage.Decrease();
        }
        fbxRequest = new FBXRequest( Model.root+"/"+itemSettings.fbx, FBXContent.MODEL, itemSettings.serializedFBXLength );
        
        usage.Increase();
        usage.onDiscarded += fbxRequest.usage.Decrease;
        fbxRequest.usage.Increase();
        fbxRequest.usage.onDiscarded += usage.Decrease;
        return null;
    }

    protected override IEnumerator OnPreload(){
        yield return fbxRequest.Preload();
        if( !fbxRequest.preloaded ){
            preloadError = "Could not load item \""+filepath+"\" because fbx preload failed:"+fbxRequest.preloadError;
            yield break;
        }
        
        foreach( var textureBindingGroup in itemSettings.textureBindingGroups ){
            for( int i=0; i<textureBindingGroup.Count; i++ ){
                textureBindingGroup[i].Preload();
            }
        }
    }
    
    protected override void OnPreloadInstant(){
        
        fbxRequest.PreloadInstant();
        if( !fbxRequest.preloaded ){
            preloadError = "Could not load item \""+filepath+"\" because fbx preload failed:"+fbxRequest.preloadError;
            return;
        }
        
        foreach( var textureBindingGroup in itemSettings.textureBindingGroups ){
            for( int i=0; i<textureBindingGroup.Count; i++ ){
                textureBindingGroup[i].Preload();
            }
        }
    }

    protected override void OnSpawnInstant( SpawnProgress progress ){
        var fbxProgress = new SpawnProgress();
        fbxRequest.doNotResize = true;
        fbxRequest._InternalSpawnUnlinked( true, fbxProgress );

        CreateItem( fbxProgress.result as FBX, progress );
    }

    protected override IEnumerator OnSpawn( SpawnProgress progress ){
        var fbxProgress = new SpawnProgress();
        fbxRequest.doNotResize = true;
        fbxRequest._InternalSpawnUnlinked( false, fbxProgress );
        while( !fbxProgress.finished ){
            yield return null;
        }
        CreateItem( fbxProgress.result as FBX, progress );
    }

    private void CreateItem( FBX fbx, SpawnProgress progress ){
        if( fbx == null ){
            FinishSpawning( progress, null, "Missing model \""+itemSettings.modelName+"\" in "+itemSettings.fbx );
            return;
        }

        var model = fbx.FindModel( itemSettings.modelName );
        if( model == null ){
            FinishSpawning( progress, null, "Missing model \""+itemSettings.modelName+"\" in "+itemSettings.fbx );
            return;
        }

        if( itemSettings.materialSettings == null ) itemSettings.materialSettings = new MaterialSettings[0];
        if( itemSettings.colliderSettings == null ) itemSettings.colliderSettings = new ColliderSetting[0];
        if( itemSettings.textureBindingGroups == null ) itemSettings.textureBindingGroups = new List<TextureBindingGroup>();
        
        Item item;
        try{
            item = Item._InternalCreateItemBase( model, itemSettings, nextID, nextAttributes, nextImmovable );
        }catch( System.Exception e ){
            FinishSpawning( progress, null, e.ToString() );
            return;
        }
        item._internalOnDestroy += delegate{
            if( fbx ) GameObject.Destroy( fbx.gameObject );
        };

        item.scriptManager.LoadAllScripts( "items", itemSettings.scripts );
        
        lastSpawnedItem = item;
        FinishSpawning( progress, item, null );
    }

    public override string GetInfoHeaderText(){
        return itemSettings == null ? "" : itemSettings.name;
    }
    public override string GetInfoBodyContentText(){
        return "";
    }
    public override void _InternalOnGenerateThumbnail(){
        if( itemSettings != null ){
            itemSettings._InternalOnGenerateThumbnail();
            thumbnail.Copy( itemSettings.thumbnail );
        }else{
            thumbnail.texture = BuiltInAssetManager.main.defaultFBXThumbnail;
        }
        thumbnail.onThumbnailChange.Invoke( this );
    }
    public override void OnCreateMenuSelected(){
        GameUI.main.createMenu.DisplayVivaObjectInfo<ItemSettings>( itemSettings, this );
        GameUI.main.createMenu.DisplayEditLogicButton();
        GameUI.main.createMenu.editLogicButton.SetCallback( delegate{
            
            var itemScripts = itemSettings.scripts;

            var available = GameUI.main.createMenu.GenerateAllScriptNamesList();
            for( int i=available.Count; i-->0; ){
                if( System.Array.Exists( itemScripts, element => element==available[i] ) ){
                    available.RemoveAt(i);
                }
            }
            UI.main.messageDialog.RequestBlackWhiteList( "Edit Logic", "Available", "Active", new List<string>( itemScripts ), available, delegate( List<string> newScriptNames ){
                itemSettings.scripts = newScriptNames.ToArray();
                lastSpawnedItem.scriptManager.SetScripts( GameUI.main.createMenu.FindScripts( newScriptNames ) );
            } );
        } );
    }
    public override void OnShare(){
        if( lastSpawnedItem == null ) return;

        var destZipFile = Viva.contentFolder+"/Share/"+lastSpawnedItem.name+".zip";
        var sourceFolder = Viva.contentFolder+"/Share/Content";
        Tools.EnsureFolder( sourceFolder );
        bool success = true;

        for( int i=0; i<lastSpawnedItem.model.textureBindingGroup.Count; i++ ){
            var textureBinding = lastSpawnedItem.model.textureBindingGroup[i];
            success &= GameUI.main.createMenu.ArchiveToShareFolder( Path.GetFullPath( TextureBinding.root+"/"+textureBinding.path ), sourceFolder );
        }
        success &= GameUI.main.createMenu.ArchiveToShareFolder( lastSpawnedItem.model.modelRequest.filepath, sourceFolder );
        success &= GameUI.main.createMenu.ArchiveToShareFolder( filepath, sourceFolder );

        for( int i=0; i<lastSpawnedItem.scriptManager.Count; i++ ){
            var script = lastSpawnedItem.scriptManager.GetScript(i);
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