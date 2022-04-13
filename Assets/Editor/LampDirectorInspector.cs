using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using viva;


[CustomEditor(typeof(LampDirector))]
public class LampDirectorInspector : Editor{

    public override void OnInspectorGUI(){

        SerializedObject ndSerialized = new SerializedObject( target );
        DrawDefaultInspector();

        if( GUILayout.Button("Update Lamp List") ){
            SetLampSourceList( ndSerialized );
        }
        GUILayout.Label( "Map lamps:"+ndSerialized.FindProperty( "mapLamps" ).arraySize );
    }

    private void SetLampSourceList( SerializedObject ndSerialized ){
        //build parent list
        var lamps = GameObject.FindObjectsOfType(typeof(Lamp)) as Lamp[];
        Debug.Log(lamps.Length);
        SerializedProperty mapLamps = ndSerialized.FindProperty( "mapLamps" );
        mapLamps.arraySize = lamps.Length;
        for( int i=0; i<mapLamps.arraySize; i++ ){
            mapLamps.GetArrayElementAtIndex(i).objectReferenceValue = lamps[i];
        }
        ndSerialized.ApplyModifiedProperties();
    }
}