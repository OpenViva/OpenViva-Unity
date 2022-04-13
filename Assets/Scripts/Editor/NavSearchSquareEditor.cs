using System.ComponentModel;
using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.IO;


[CustomEditor(typeof(viva.NavSearchSquare), true)]
[CanEditMultipleObjects]
public class NavSearchSquareEditor : Editor
{
    private static GUIStyle boldStyle = new GUIStyle();
    private SerializedObject sObj;
    private bool enableKeyboardModeMixing = false;
    private SerializedProperty sideMaskProp;
   
    private void OnEnable(){
        boldStyle.fontStyle = FontStyle.Bold;
        sObj = new SerializedObject( target );
        sideMaskProp = sObj.FindProperty("sideMask");
    }
    public override void OnInspectorGUI(){
        DrawDefaultInspector();

        DisplaySideButton( "Side X+", 1 );
        DisplaySideButton( "Side X-", 2 );
        DisplaySideButton( "Side Z+", 4 );
        DisplaySideButton( "Side Z-", 8 );
    }

    private void DisplaySideButton( string label, int value ){
        bool oldValue = ( sideMaskProp.intValue&value ) != 0;
        bool newValue = GUILayout.Toggle( oldValue, label );
        if( oldValue != newValue ){
            if( newValue ){
                sideMaskProp.intValue |= value;
            }else{
                sideMaskProp.intValue &= ~value;
            }
            sObj.ApplyModifiedProperties();
        }
    }
}