using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.IO;


[CustomEditor(typeof(FenceGenerator))]
[CanEditMultipleObjects]
public class FenceGeneratorEditor : Editor
{
    SerializedProperty anchorMesh;
    SerializedProperty fenceObjectPrefix;
    SerializedProperty fenceObjects;
    SerializedProperty anchorPoints;
    SerializedProperty skips;
    SerializedProperty height;
    SerializedProperty heightVariety;
    private bool change = false;
    private Vector3 lastPos = Vector3.zero;
    private int anchorSelectionIndex = -1;
    private bool mouseIsUp = false;
    private float lastHideTime = 0.0f;

    private class Instance{
        public int prefabIndex;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
    }

    private void OnEnable(){
        anchorMesh = serializedObject.FindProperty("anchorMesh");
        fenceObjectPrefix = serializedObject.FindProperty("fenceObjectPrefix");
        fenceObjects = serializedObject.FindProperty("fenceObjects");
        anchorPoints = serializedObject.FindProperty("anchorPoints");
        skips = serializedObject.FindProperty("skips");
        height = serializedObject.FindProperty("height");
        heightVariety = serializedObject.FindProperty("heightVariety");
    }

    private GameObject Get3DCursor(){
        GameObject cursor = GameObject.Find("_CURSOR_");
        if( cursor == null ){
            cursor = new GameObject("_CURSOR_");
            cursor.isStatic = true;
            var mf = cursor.AddComponent<MeshFilter>();
            mf.mesh = anchorMesh.objectReferenceValue as Mesh;
            cursor.transform.localScale = Vector3.one*0.3f;
        }
        return cursor;
    }

    private void OnSceneGUI(){
        int controlID = GUIUtility.GetControlID(FocusType.Passive);

        switch (Event.current.GetTypeForControl(controlID)){
        case EventType.MouseUp:
            mouseIsUp = true;
            break;
        case EventType.MouseDrag:
            if( Selection.activeGameObject == Get3DCursor() && anchorSelectionIndex != -1 && anchorSelectionIndex < anchorPoints.arraySize ){
            
                SerializedProperty anchorPoint = anchorPoints.GetArrayElementAtIndex( anchorSelectionIndex );
                if( Selection.activeGameObject.transform.position != anchorPoint.vector3Value ){
                    (target as FenceGenerator).SetAnchorPoint(anchorSelectionIndex,Selection.activeGameObject.transform.position);
                    RebuildFenceInstances( target as FenceGenerator );
                }
            }
            break;
        case EventType.MouseDown:
            if( !mouseIsUp ){
                break;
            }
            mouseIsUp = false;
            int newAnchorSelectionIndex = -1;
            GameObject cursor = Get3DCursor(); 
            if( Event.current.alt ){
                //check if selected an anchor point
                float closest = 10000.0f;
                Vector3 mouseScreenPoint = SceneView.lastActiveSceneView.camera.WorldToScreenPoint(HandleUtility.GUIPointToWorldRay( Event.current.mousePosition ).origin);
                for( int i=0; i<anchorPoints.arraySize; i++ ){
                    Vector3 anchorScreenPoint = SceneView.lastActiveSceneView.camera.WorldToScreenPoint( anchorPoints.GetArrayElementAtIndex(i).vector3Value );
                    if( anchorScreenPoint.z < 0.0f ){
                        continue;
                    }
                    float sqMag = Vector2.SqrMagnitude( new Vector2(mouseScreenPoint.x,mouseScreenPoint.y)-new Vector2( anchorScreenPoint.x, anchorScreenPoint.y ) );
                    if( sqMag < closest ){
                        closest = sqMag;
                        newAnchorSelectionIndex = i;
                    }
                }
                
                if( newAnchorSelectionIndex == -1 ){
                    cursor.transform.position = Vector3.zero;
                }
                if( anchorSelectionIndex != newAnchorSelectionIndex ){
                    if( newAnchorSelectionIndex != -1 ){
                        Selection.activeGameObject = cursor.gameObject;
                        cursor.transform.position = anchorPoints.GetArrayElementAtIndex(newAnchorSelectionIndex).vector3Value;
                    }
                    GUIUtility.hotControl = controlID;
                    Event.current.Use();
                }
                anchorSelectionIndex = newAnchorSelectionIndex;
            }else if( Selection.activeGameObject != cursor ){
                cursor.transform.position = Vector3.zero;
                anchorSelectionIndex = -1;
            }
            break;
        }
    }

    public static object GetTargetObjectOfProperty( SerializedProperty property ){
        var targetObject = property.serializedObject.targetObject;
        var targetObjectClassType = targetObject.GetType();
        var field = targetObjectClassType.GetField(property.propertyPath);
        if( field != null ){
            var value = field.GetValue( targetObject );
            return value;
        }
        return null;
    }

    private bool DisplayFloatAndRebuildIfChange( SerializedProperty prop, FenceGenerator fenceGenerator ){
        float oldValue = prop.floatValue;
        EditorGUILayout.PropertyField( prop );
        return oldValue != prop.floatValue;
    }

    public override void OnInspectorGUI(){
        EditorGUILayout.PropertyField(anchorMesh);
        EditorGUILayout.PropertyField(fenceObjectPrefix);
        EditorGUILayout.PropertyField(anchorPoints,true);
        FenceGenerator fenceGenerator = serializedObject.targetObject as FenceGenerator;

        float[] fenceLengths = new float[ fenceObjects.arraySize ];
        for( int i=0; i<fenceGenerator.fenceObjects.Length; i++ ){
            fenceLengths[i] = fenceGenerator.fenceObjects[i].padding;
        }
        bool rebuildFences = false;
        EditorGUILayout.PropertyField( fenceObjects, true );

        rebuildFences |= DisplayFloatAndRebuildIfChange( height, fenceGenerator );
        rebuildFences |= DisplayFloatAndRebuildIfChange( heightVariety, fenceGenerator );

        GUILayout.Label( "Anchors: "+anchorPoints.arraySize );
        if( anchorSelectionIndex != -1 ){
            if( anchorSelectionIndex >= skips.arraySize ){
                skips.arraySize = anchorPoints.arraySize;
            }
            Vector3 anchorPoint = anchorPoints.GetArrayElementAtIndex(anchorSelectionIndex).vector3Value;
            GUILayout.BeginHorizontal();
            if( GUILayout.Button("Split") ){
                Vector3 position;
                if( anchorSelectionIndex >= anchorPoints.arraySize-1 ){
                    position = anchorPoint+Vector3.forward*4.0f;
                }else{
                    position = ( anchorPoint+anchorPoints.GetArrayElementAtIndex( anchorSelectionIndex+1 ).vector3Value )/2.0f;
                }
                CreateAnchor( fenceGenerator.transform, anchorSelectionIndex, position );
                rebuildFences = true;
            }
            SerializedProperty skipProperty = skips.GetArrayElementAtIndex(anchorSelectionIndex);
            string skipName;
            if( !skipProperty.boolValue ){
                skipName = "Hide";
            }else{
                skipName = "Show";
            }
            if( GUILayout.Button(skipName) ){
                skipProperty.boolValue = !skipProperty.boolValue;
                rebuildFences = true;
            }
            if( GUILayout.Button("Remove") ){
                anchorPoints.DeleteArrayElementAtIndex(anchorSelectionIndex);
                rebuildFences = true;
            }
            GUILayout.EndHorizontal();
        }
        

        Transform rootTransform = fenceGenerator.transform;
        if( GUILayout.Button("Add") ){
            Vector3 position;
            if( anchorPoints.arraySize == 0 ){
                position = rootTransform.position;
            }else{
                position = anchorPoints.GetArrayElementAtIndex( anchorPoints.arraySize-1 ).vector3Value;
            }

            CreateAnchor( fenceGenerator.transform, anchorPoints.arraySize, position );
        }
        serializedObject.ApplyModifiedProperties();

        for( int i=0; i<fenceGenerator.fenceObjects.Length; i++ ){
            if( fenceGenerator.fenceObjects[i].padding != fenceLengths[i] ){
                rebuildFences = true;
                break;
            }
        }
        if( rebuildFences ){
            RebuildFenceInstances( fenceGenerator );
        }
    }

    public void CreateAnchor( Transform rootTransform, int index, Vector3 position ){
        anchorPoints.InsertArrayElementAtIndex( index );
        anchorPoints.GetArrayElementAtIndex( index ).vector3Value = position;
        anchorSelectionIndex = index;
    }
    
    public void DestroyFenceInstances( FenceGenerator fenceGenerator ){
        //edit the fence
        for( int i=fenceGenerator.transform.childCount; i-->0; ){
            Transform obj = fenceGenerator.transform.GetChild(i);
            if( obj.name.StartsWith( fenceGenerator.fenceObjectPrefix ) ){
                DestroyImmediate( obj.gameObject );
            }
        }
    }

    private float GetPitch( Vector3 a, Vector3 b ){
        return Mathf.Atan( (b.y-a.y)/Vector3.Distance( b, a ) )*Mathf.Rad2Deg;
    }
    
    private void RebuildFenceInstances( FenceGenerator fenceGenerator ){
        
        DestroyFenceInstances( fenceGenerator );
        UnityEngine.Random.InitState(0);
        //Build prefab list
        List<Instance> instances = GetInstancesAlongPath( fenceGenerator );

        //reuse existing child objects
        for( int i=fenceGenerator.transform.childCount; i-->instances.Count; ){
            DestroyImmediate( fenceGenerator.transform.GetChild(i).gameObject );
        }
        if( skips.arraySize != instances.Count ){
            skips.arraySize = instances.Count;
        }
        for( int i=0; i<instances.Count; i++ ){
            Instance instance = instances[i];
            //attempt reuse
            Transform fence = null;
            if( i<fenceGenerator.transform.childCount ){
                GameObject child = fenceGenerator.transform.GetChild(i).gameObject;
                if( child.name.EndsWith("_"+instance.prefabIndex) ){
                    fence = child.transform;
                }else{
                    DestroyImmediate( child );
                }
            }
            if( fence == null ){
                fence = GameObject.Instantiate( fenceGenerator.fenceObjects[ instance.prefabIndex ].prefab, fenceGenerator.transform ).transform;
                fence.transform.SetSiblingIndex(i-1);
            }
            fence.name = fenceGenerator.fenceObjectPrefix+"_"+i+"_"+instance.prefabIndex;
            fence.position = instance.position;
            fence.rotation = instance.rotation;
            fence.localScale = instance.scale;
        }
    }

    private List<Instance> GetInstancesAlongPath( FenceGenerator fenceGenerator ){
        //cache prefabLengths
        float[] prefabLengths = new float[ fenceGenerator.fenceObjects.Length ];
        for( int i=0; i<prefabLengths.Length; i++ ){
            prefabLengths[i] = fenceGenerator.fenceObjects[i].prefab.transform.GetChild(0).GetComponent<MeshFilter>().sharedMesh.bounds.extents.z*2.0f;
        }
        List<Instance> instances = new List<Instance>();
        Vector3 prev = anchorPoints.GetArrayElementAtIndex(0).vector3Value;
        for( int i=1; i<anchorPoints.arraySize; i++ ){

            Vector3 curr = anchorPoints.GetArrayElementAtIndex(i).vector3Value;
            
            if( i < fenceGenerator.skips.Length && fenceGenerator.skips[i] ){
                prev = curr;
                continue;
            }
            //adjust to be inbetween 2 poles
            float yaw = Mathf.Atan2( prev.x-curr.x, prev.z-curr.z )*Mathf.Rad2Deg;
            float pitch = GetPitch( prev, curr );

            float targetLength = Vector3.Magnitude( curr-prev );
            if( i < anchorPoints.arraySize-1 ){
                Vector3 next = anchorPoints.GetArrayElementAtIndex(i+1).vector3Value;
                float nextPitch = GetPitch( curr, next );
                if( pitch > nextPitch ){
                    //adjust trueLength for next fence
                    targetLength += Mathf.Sin( (pitch-nextPitch)*Mathf.Deg2Rad )*height.floatValue;
                }
            }

            float shortest_diff = Mathf.Infinity;
            int targetPrefabIndex = -1;
            for( int j=0; j<fenceGenerator.fenceObjects.Length; j++ ){
                float length_diff = Mathf.Abs( prefabLengths[j]-targetLength );
                if( length_diff < shortest_diff ){
                    shortest_diff = length_diff;
                    targetPrefabIndex = j;
                }
            }

            Quaternion rotation = Quaternion.AngleAxis( yaw, Vector3.up)*Quaternion.AngleAxis( pitch, Vector3.right );
            
            Vector3 scale = Vector3.one;
            float actualLength = prefabLengths[ targetPrefabIndex ]+fenceGenerator.fenceObjects[ targetPrefabIndex ].padding;
            scale.z *= targetLength/actualLength;
            scale.y = 1.0f+UnityEngine.Random.value*heightVariety.floatValue;

            instances.Add( new Instance{
                prefabIndex = targetPrefabIndex,
                position = prev,
                rotation = rotation,
                scale = scale
            });

            prev = curr;
        }

        return instances;
    }
}