using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace viva{


public partial class PolaroidFrame : Item{

    [System.Serializable]
    public class PolaroidFrameAsset{
        public PhotoSummary photoSummary;
        public int size;
        public string rawDataString;
        public GameDirector.TransformSave transformSave;
    }

    public override void Save( GameDirector.VivaFile vivaFile ){
        
        PolaroidFrameAsset frameSave = new PolaroidFrameAsset();
        frameSave.photoSummary = photoSummary;

        MeshRenderer mr = GetComponent( typeof(MeshRenderer) ) as MeshRenderer;
        Texture2D frameTex = mr.materials[1].mainTexture as Texture2D;
        if( frameTex.format != TextureFormat.DXT1 ){
            Debug.LogError("ERROR Invalid PolaroidFrame texture format! "+frameTex.format);
            return;
        }
        frameSave.size = frameTex.width;

        //convert from ARGB to RGB
        byte[] RGB = frameTex.GetRawTextureData();
        frameSave.rawDataString = Tools.Base64ByteArrayToString( RGB, 0, RGB.Length );
        frameSave.transformSave = new GameDirector.TransformSave( transform );

        // vivaFile.serializedAssets.Add( frameSave );
    }

    private static void LoadAsset(){

        // PolaroidFrameAsset frameAsset = assetFile as PolaroidFrameAsset;
        // if( frameAsset == null ){
        //     return;
        // }
        // GameObject container = GameObject.Instantiate( gameObjects[0], frameAsset.transformSave.position, frameAsset.transformSave.rotation );

        // byte[] rawData = Tools.StringToBase64ByteArray( frameAsset.rawDataString );
        // Texture2D tex = new Texture2D( frameAsset.size, frameAsset.size, TextureFormat.DXT1, false, true );
        // tex.LoadRawTextureData( rawData );
        // tex.Apply();
        // MeshRenderer mr = container.GetComponent( typeof(MeshRenderer) ) as MeshRenderer;
        // mr.materials[1].mainTexture = tex;

        // PolaroidFrame frame = container.GetComponent<PolaroidFrame>();
        // frame.photoSummary = frameAsset.photoSummary;

        // Rigidbody rigidBody = container.GetComponent(typeof(Rigidbody)) as Rigidbody;
        // rigidBody.useGravity = false;
        // rigidBody.freezeRotation = true;
        // rigidBody.constraints = RigidbodyConstraints.FreezeAll;
    }
}

}