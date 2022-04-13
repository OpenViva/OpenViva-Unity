using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.IO;
using viva;


[CustomEditor(typeof(NatureDirector))]
public class NatureDirectorInspector : Editor{

    public override void OnInspectorGUI(){

        SerializedObject ndSerialized = new SerializedObject( target );

        if( GUILayout.Button("Update Plant List") ){
            BuildPlantTable( ndSerialized );
        }
        
        DrawDefaultInspector();
    }

    private void BuildPlantTable( SerializedObject ndSerialized ){
        
        Plant[] plants = GameObject.FindObjectsOfType(typeof(Plant)) as Plant[];

        //set array
        SerializedProperty plantsProp = ndSerialized.FindProperty("plants");
        plantsProp.arraySize = plants.Length;
        for( int i=0; i<plantsProp.arraySize; i++ ){
            plantsProp.GetArrayElementAtIndex(i).objectReferenceValue = plants[i];
        }
        ndSerialized.ApplyModifiedProperties();
    }
}