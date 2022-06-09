// using UnityEditor;
// using UnityEngine;
// using System.Collections;
// using System.Collections.Generic;
// using System.Reflection;
// using System;
// using System.IO;


// [CustomEditor(typeof(viva.HoldForm))]
// [CanEditMultipleObjects]
// public class HoldFormEditor : Editor
// {
//     private viva.HoldForm copyHoldFormTarget;
//     private static GUIStyle boldStyle = new GUIStyle();

//     private void OnEnable(){
//         boldStyle.fontStyle = FontStyle.Bold;
//     }
//     public override void OnInspectorGUI(){
//         DrawDefaultInspector();

//         EditorGUILayout.Space();
//         EditorGUILayout.LabelField("Utility", boldStyle );
//         copyHoldFormTarget = EditorGUILayout.ObjectField( "Copy target:", copyHoldFormTarget, typeof(viva.HoldForm), false) as viva.HoldForm;

//         if( copyHoldFormTarget != null && GUILayout.Button("Copy") ){

//             SerializedObject sObj = new SerializedObject( target );
//             sObj.FindProperty("localHandTarget").vector3Value = copyHoldFormTarget.localHandTarget;
//             sObj.FindProperty("localHandPole").vector3Value = copyHoldFormTarget.localHandPole;
//             sObj.FindProperty("handPitch").floatValue = copyHoldFormTarget.handPitch;

//             if( target.name.EndsWith("_r") && copyHoldFormTarget.name.EndsWith("_r") && target.name.EndsWith("_l") && copyHoldFormTarget.name.EndsWith("_l")  ){
//                 sObj.FindProperty("handYaw").floatValue = copyHoldFormTarget.handYaw;
//                 sObj.FindProperty("handRoll").floatValue = copyHoldFormTarget.handRoll;
//             }else{
//                 sObj.FindProperty("handYaw").floatValue = -copyHoldFormTarget.handYaw;
//                 sObj.FindProperty("handRoll").floatValue = -copyHoldFormTarget.handRoll;
//             }

//             sObj.ApplyModifiedProperties();
//         }
//     }

// }