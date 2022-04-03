using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fbx;
using System.IO;
using Unity.Jobs;
using Unity.Collections;



namespace viva{

//creates new TextureHandles from raw images (png, jpg, pixel data, etc.)
public partial class TextureRequest: ImportRequest{

    public TextureHandle handle { get; private set; }
    

    public TextureRequest( string _filepath ):base( _filepath, ImportRequestType.TEXTURE ){
    }

    public override void _InternalOnGenerateThumbnail(){
        if( handle != null ) thumbnail.texture = handle._internalTexture;
    }
    public override string GetInfoHeaderText(){
        if( handle == null ) return "loading...";
        string s = "";
        s += CreateMenu.InfoTypeToColor( CreateMenu.InfoColor.TEXTURE_REQUEST )+handle._internalTexture.name+"\n";
        s += handle._internalTexture.width+"x"+handle._internalTexture.height+"\n";
        s += handle._internalTexture.format+"</color>\n";
        return s;
    }

    public override void OnInstall( string subfolder=null ){
        handle?.Save( subfolder );
    }
    
    public override string GetInfoBodyContentText(){
        return importError;
    }

    protected override string OnImport(){

        byte[] fileData = null;
        try{
            fileData = File.ReadAllBytes( filepath );
        }catch( System.Exception e ){
            return "Could not import texture \""+filepath+"\" "+e.ToString();
        }

        var name = Path.GetFileNameWithoutExtension( filepath );
        //reuse texture handle
        if( handle == null ){
            handle = new TextureHandle( new Texture2D( 1, 1, TextureFormat.ARGB32, true, name.EndsWith("_NormalMap") ), filepath );    //size does not matter as per Unity Docs
        }
        if( !handle._internalTexture.LoadImage( fileData, false ) ){
            return "Could not read file as a texture";
        }else{
            handle._internalTexture.name = name;
        }
        if( handle._internalTexture.width != handle._internalTexture.height ){
            Texture2D.Destroy( handle._internalTexture );
            return "Texture must be square. Please resize before importing. Currently: "+handle._internalTexture.width+"x"+handle._internalTexture.height;
        }
        return null;
    }

    public override void OnCreateMenuSelected(){
        GameUI.main.createMenu.DisplayVivaObjectInfo<TextureRequest>( this, this );
    }
}

}