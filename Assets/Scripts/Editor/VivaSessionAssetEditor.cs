using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.IO;


[CustomEditor(typeof(viva.VivaSessionAsset),true)]
[CanEditMultipleObjects]
public class VivaSessionAssetEditor : Editor
{
    private GUIStyle boldStyle = new GUIStyle();
    private SerializedProperty disablePersistanceProp;
    private SerializedProperty assetNameProp;
    private SerializedProperty targetsSceneAssetProp;
    private SerializedProperty loadAtTheEndProp;
    private SerializedObject sObj;
   
    private void OnEnable(){
        boldStyle.fontStyle = FontStyle.Bold;
        sObj = new SerializedObject( target );
        disablePersistanceProp = sObj.FindProperty("disablePersistance");
        assetNameProp = sObj.FindProperty("assetName");
        targetsSceneAssetProp = sObj.FindProperty("targetsSceneAsset");
        loadAtTheEndProp = sObj.FindProperty("m_loadAtTheEnd");
    }
    public override void OnInspectorGUI(){

        disablePersistanceProp.boolValue = GUILayout.Toggle( disablePersistanceProp.boolValue, "Disable persistance" );
        if( !disablePersistanceProp.boolValue ){

            targetsSceneAssetProp.boolValue = GUILayout.Toggle( targetsSceneAssetProp.boolValue, "Targets Scene Asset" );
            if( !targetsSceneAssetProp.boolValue ){
                assetNameProp.stringValue = EditorGUILayout.TextField( "Asset name:", assetNameProp.stringValue );
            }
        }
        sObj.ApplyModifiedPropertiesWithoutUndo();

        EditorGUILayout.Space();

        DrawDefaultInspector();
    }

}