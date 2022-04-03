using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using System.IO;



namespace viva{

public abstract class InstanceSettings: Settings{

    // public static T Load<T>( string filepath, ImportRequest _internalSourceRequest, T overwriteTarget ) where T:InstanceSettings{
    //     return VivaEditable.LoadFromFile<T>( filepath, _internalSourceRequest, overwriteTarget );
    // }

    public string fbx;
    public int serializedFBXLength;
    public string modelName;
    public Vector3 holdPosOffset = new Vector3( 0.0125f, -0.005f, 0.01f );
    public Vector3 holdEulerOffset = new Vector3( -90.0f, -160.0f, 0.0f );
    public bool useKeyboardHoldOffsets = false;
    public Vector3 keyboardHoldPosOffset = new Vector3( 0.0125f, -0.005f, 0.02f );
    public Vector3 keyboardHoldEulerOffset = new Vector3( -90f, -190f, 0f );


    public InstanceSettings( Texture2D thumbnailTexture, string _name, ImportRequest _internalSourceRequest ):base(thumbnailTexture,_name,_internalSourceRequest){
    }
}

}