using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.IO;


public class FenceGenerator : MonoBehaviour{

    [SerializeField]
    public Mesh anchorMesh;

    [System.Serializable]
    public class FenceObject{

        public GameObject prefab;
        [Range(-0.5f, 0.5f)]
        public float padding;
    }

    [SerializeField]
    public float height;
    
    [Range(0.0f,0.3f)]
    [SerializeField]
    public float heightVariety;

    [SerializeField]
    public string fenceObjectPrefix;

    [SerializeField]
    public FenceObject[] fenceObjects;

    [SerializeField]
    private List<Vector3> anchorPoints;

    [SerializeField]
    public bool[] skips = new bool[0];

// #if UNITY_EDITOR
//     public void OnDrawGizmos(){
        
//         Gizmos.color = Color.green;
//         GUIStyle style = new GUIStyle();
//         style.fontStyle = FontStyle.Bold;
//         style.alignment = TextAnchor.UpperCenter;

//         Vector3? lastAnchorPos = null;
//         for( int i=anchorPoints.Count; i-->0; ){
//             Vector3 anchor = anchorPoints[i];
//             if( i < skips.Length ){
//                 if( skips[i] ){
//                     style.normal.textColor = Color.red;
//                     if( Vector3.SqrMagnitude( SceneView.lastActiveSceneView.camera.transform.position-anchor )< 400.0f ){
//                         Handles.Label( anchor, "Anchor\n[HIDDEN]", style );
//                     }
//                 }else{
//                     style.normal.textColor = Color.green;
//                      if( Vector3.SqrMagnitude( SceneView.lastActiveSceneView.camera.transform.position-anchor )< 400.0f ){
//                         Handles.Label( anchor, "Anchor", style );
//                     }
//                 }
//             }
//             if( lastAnchorPos.HasValue ){
//                 Gizmos.DrawLine( anchor, lastAnchorPos.Value );
//             }
//             lastAnchorPos = anchor;
//         }
//     }
// #endif

    public void SetAnchorPoint( int index, Vector3 p ){
        anchorPoints[index] = p;
    }
}
