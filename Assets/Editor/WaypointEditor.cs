using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using viva;


[CustomEditor(typeof(Waypoints))]
public class WaypointsEditor : Editor{

    public void OnEnable(){
        EditorApplication.update -= CheckPoint;
        EditorApplication.update += CheckPoint;
    }

    public void OnDisable(){
        EditorApplication.update -= CheckPoint;
    }

    private int selectedIndex = -1;
    private string linkString = "0";

    public override void OnInspectorGUI(){

        DrawDefaultInspector();

        Waypoints w = target as Waypoints;
        Transform container = w.transform.Find("EDIT");

        EditorGUILayout.BeginHorizontal();
        if( GUILayout.Button("Edit") ){
            container = GetContainer(w);

            for( int i=container.childCount; i-->0; i++ ){
                GameObject.DestroyImmediate( container.GetChild(i) );
            }

            if( w.nodes.Length == 0 ){
                GameObject point = new GameObject("POINT");
                point.transform.SetParent( container );
                point.transform.position = SceneView.lastActiveSceneView.camera.WorldToScreenPoint(Vector2.zero);
            }
            
            for( int i=0; i<w.nodes.Length; i++ ){
                GameObject point = new GameObject("POINT");
                point.transform.SetParent( container );
                point.transform.position = w.nodes[i].position;
            }
        }

        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.LabelField("Selected: "+selectedIndex );
        linkString = EditorGUILayout.TextField(linkString);
        int targIndex=-1;

        if( int.TryParse( linkString, out targIndex ) && targIndex != selectedIndex && GUILayout.Button("Link") ){
            SerializedObject ndSerialized = new SerializedObject( w );
            SerializedProperty nodes = ndSerialized.FindProperty("m_nodes");

            var selectedLinks = nodes.GetArrayElementAtIndex(selectedIndex).FindPropertyRelative("links");
            var targLinks = nodes.GetArrayElementAtIndex(targIndex).FindPropertyRelative("links");

            AddLink( selectedLinks, targIndex );
            AddLink( targLinks, selectedIndex );
            ndSerialized.ApplyModifiedProperties();
        }

        if( container ){
            for( int i=0; i<container.childCount; i++ ){
                if( container.GetChild(i).name != "POINT_"+i ){
                    container.GetChild(i).name = "POINT_"+i;
                }
            }
        }
    }

    private void AddLink( SerializedProperty property, int value ){
        bool exists = false;
        for( int i=0; i<property.arraySize; i++ ){
            if( property.GetArrayElementAtIndex(i).intValue == value ){
                exists = true;
                break;
            }
        }
        if( !exists ){
            property.arraySize = property.arraySize+1;
            property.GetArrayElementAtIndex( property.arraySize-1 ).intValue = value;
        }
    }

    private Transform GetContainer( Waypoints w ){
        Transform container = w.transform.Find("EDIT");
        if( container == null ){
            container = new GameObject("EDIT").transform;
            container.SetParent( w.transform );
        }
        return container;
    }

    public void CheckPoint(){
        
        if( Selection.activeGameObject == null ){
            return;
        }
        Transform t = Selection.activeGameObject.transform;
        if( !t.name.StartsWith("POINT") ){
            return;
        }
        Waypoints w = t.parent.parent.GetComponent<Waypoints>();
        if( w == null ){
            return;
        }
        selectedIndex = t.GetSiblingIndex();
        if( t.hasChanged ){
            t.hasChanged = false;

            SerializedObject ndSerialized = new SerializedObject( w );
            SerializedProperty nodes = ndSerialized.FindProperty("m_nodes");
            if( nodes.arraySize < t.GetSiblingIndex()+1 ){
                nodes.arraySize = t.GetSiblingIndex()+1;
                nodes.GetArrayElementAtIndex( nodes.arraySize-1 ).FindPropertyRelative("links").arraySize = 0;
                linkString = ""+(nodes.arraySize-2);
            }
            var element = nodes.GetArrayElementAtIndex(t.GetSiblingIndex());
            element.FindPropertyRelative("position").vector3Value = w.transform.InverseTransformPoint( t.position );

            ndSerialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}