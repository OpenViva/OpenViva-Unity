using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.IO;
using viva;


[CustomEditor(typeof(AmbienceDirector))]
public class AmbienceDirectorInspector : Editor{

    public override void OnInspectorGUI(){

        SerializedObject ndSerialized = new SerializedObject( target );
        DrawDefaultInspector();

        if( GUILayout.Button("Update DaySegment Binded AudioSource List") ){
            SetAllAmbienceSourceLists( ndSerialized );
        }
        GUILayout.Label( "Day sources:"+ndSerialized.FindProperty( "daytimeOnlySources" ).arraySize );
        GUILayout.Label( "Night sources:"+ndSerialized.FindProperty( "nighttimeOnlySources" ).arraySize );
        GUILayout.Label( "Window sources:"+ndSerialized.FindProperty( "windowSourcesA" ).arraySize );
    }

    private void SetAllAmbienceSourceLists( SerializedObject ADSerialized ){

        SetAmbienceSourceList( ADSerialized, "daytimeOnlySources", "daytimeOnlySound" );
        SetAmbienceSourceList( ADSerialized, "nighttimeOnlySources", "nighttimeOnlySound" );
        SetAmbienceSourceList( ADSerialized, "windowSourcesA", "windowAmbience" );
        
        ADSerialized.ApplyModifiedProperties();
    }

    private void SetAmbienceSourceList( SerializedObject ADSerialized, string propName, string tagName ){
        //build parent list
        var bindedSources = new List<AudioSource>();
        var sourceContainers = GameObject.FindGameObjectsWithTag(tagName);
        foreach( var container in sourceContainers ){
            AudioSource source = container.GetComponent<AudioSource>();
            if( source == null ){
                continue;
            }
            bindedSources.Add( source );
        }

        SerializedProperty ambienceSources = ADSerialized.FindProperty( propName );
        ambienceSources.arraySize = bindedSources.Count;
        for( int i=0; i<ambienceSources.arraySize; i++ ){
            ambienceSources.GetArrayElementAtIndex(i).objectReferenceValue = bindedSources[i];
        }
    }
}