using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.IO;



namespace viva{

[CustomEditor(typeof(NavTile))]
[CanEditMultipleObjects]
public class NavTileEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if( GUILayout.Button( "Bake Nav Mesh" ) ){
            foreach( var target in targets ){
                var navTile = target as NavTile;
                navTile.BakeNavMesh();
                EditorUtility.SetDirty( navTile );
            }

            AssetDatabase.SaveAssets();
        }
    }
}

}