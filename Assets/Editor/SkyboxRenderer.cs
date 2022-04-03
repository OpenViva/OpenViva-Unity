using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.IO;


[CustomEditor(typeof(CollisionSetupEditor))]
public class SkyboxRenderer : Editor
{
    [MenuItem("Tools/Render Skybox")]
    static void Init(){

        // create temporary camera for rendering
        GameObject go = new GameObject("CubemapCamera");
        var camera = go.AddComponent<Camera>();

        var cubemap = new Cubemap( 1024, TextureFormat.RGB24, 0 );
        camera.RenderToCubemap( cubemap );

        AssetDatabase.CreateAsset( cubemap, "Assets/skybox.asset" );
        AssetDatabase.SaveAssets();

        DestroyImmediate(go);
    }

}