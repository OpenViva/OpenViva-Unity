using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using System.IO;



namespace viva{

public abstract class Settings: VivaEditable{

    public static T Load<T>( string filepath, ImportRequest _internalSourceRequest, T overwriteTarget ) where T:InstanceSettings{
        return VivaEditable.LoadFromFile<T>( filepath, _internalSourceRequest, overwriteTarget );
    }

    public string name;
    public int thumbnailResolution;
    public string thumbnailTexData;
    public bool hide;
    public string[] attributes = new string[0];
    public GrabbableSettings[] grabbableSettings;
    public string[] scripts = new string[0];


    public Settings( Texture2D thumbnailTexture, string _name, ImportRequest __internalSourceRequest ):base(__internalSourceRequest){
        name = _name;
        
        thumbnail.onThumbnailChange._InternalAddListener( HandleThumbnailChange );
        thumbnail.texture = thumbnailTexture;
        SerializeThumbnailTexture();
    }

    private void HandleThumbnailChange( VivaObject obj ){
        SerializeThumbnailTexture();
    }
    
    public override void _InternalOnGenerateThumbnail(){
        thumbnail.texture = Tools.CreateThumbnailTexture(
            thumbnailTexData,
            thumbnailResolution,
            TextureFormat.ARGB32,
            false
        );
        if( thumbnail.texture == null ){
            thumbnail.texture = Tools.CreateThumbnailTexture(
                thumbnailTexData,
                thumbnailResolution,
                TextureFormat.RGB24,
                false
            );
        }
        if( thumbnail.texture == null ){
            thumbnail.texture = BuiltInAssetManager.main.defaultFBXThumbnail;
        }
    }

    public void SerializeThumbnailTexture(){
        var thumbnailTex = thumbnail.texture ? thumbnail.texture : BuiltInAssetManager.main.defaultFBXThumbnail;
        var texBytes = thumbnailTex.GetRawTextureData();
        thumbnailResolution = thumbnailTex.width;
        thumbnailTexData = Tools.Base64ByteArrayToString( texBytes, 0, texBytes.Length );
    }
}

}