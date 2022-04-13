using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.IO;


[CustomEditor(typeof(viva.OnsenPool),true)]
[CanEditMultipleObjects]
public class OnsenPoolEditor : Editor
{
    private GUIStyle boldStyle = new GUIStyle();
    private SerializedProperty randomFloorSamplePointsProp;
    private SerializedProperty waterFloorSampleMeshProp;
    private SerializedObject sObj;
   
    private void OnEnable(){
        boldStyle.fontStyle = FontStyle.Bold;
        sObj = new SerializedObject( target );
        randomFloorSamplePointsProp = sObj.FindProperty("randomFloorSamplePoints");
        waterFloorSampleMeshProp = sObj.FindProperty("waterFloorSampleMesh");
    }
    public override void OnInspectorGUI(){

        EditorGUILayout.Space();
        DrawDefaultInspector();

        Mesh mesh = waterFloorSampleMeshProp.objectReferenceValue as Mesh;
        if( mesh && GUILayout.Button("Update sample point list") ){
            List<Vector3> points = new List<Vector3>();

            var indices = mesh.GetIndices(0);
            foreach( int index in indices ){
                points.Add( mesh.vertices[index] );
            }
            randomFloorSamplePointsProp.arraySize = points.Count;
            for( int i=0; i<points.Count; i++ ){
                var element = randomFloorSamplePointsProp.GetArrayElementAtIndex(i);
                element.vector3Value = points[i];
            }

            Debug.Log("[OnsenPoolEditor] Set "+points.Count/3+" triangles");

            sObj.ApplyModifiedPropertiesWithoutUndo();
        }
    }

}