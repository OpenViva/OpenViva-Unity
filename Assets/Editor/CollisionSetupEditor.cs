using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.IO;


[CustomEditor(typeof(CollisionSetupEditor))]
[CanEditMultipleObjects]
public class CollisionSetupEditor : Editor
{
    [MenuItem("Tools/Build Collision Setup")]
    static void Init(){
        if( Selection.transforms.Length == 0 ){
            EditorUtility.DisplayDialog( "Build Collision Setup", "No scene target objects selected", "ok" );
            return;
        }
        List<string> errors = new List<string>();
        foreach( Transform selectedTransform in Selection.transforms ){
            string setupPath;
            
            MeshFilter mf = selectedTransform.GetComponent<MeshFilter>();
            GameObject obj = null;
            if( mf != null ){
                if( mf.sharedMesh == null ){
                    EditorUtility.DisplayDialog( "Build Collision Setup", "Target object MeshFilter has no mesh", "ok" );
                    return;
                }
                setupPath = AssetDatabase.GetAssetPath( mf.sharedMesh ).Replace(".fbx","CollisionSetup.fbx");
                //try alternative path name
                obj = (GameObject)AssetDatabase.LoadAssetAtPath( setupPath, typeof(GameObject) );
                if( obj == null ){
                    setupPath = AssetDatabase.GetAssetPath( mf.sharedMesh ).Replace(".fbx","_collisionSetup.fbx");
                }
            }else{
                setupPath = selectedTransform.name;
            }
            obj = (GameObject)AssetDatabase.LoadAssetAtPath( setupPath, typeof(GameObject) );
            if( obj == null ){
                EditorUtility.DisplayDialog( "Build Collision Setup", "Could not find \""+setupPath+"\"", "ok" );
                return;
            }
            //set temporary transform for less calculations
            Vector3 cachedPos = selectedTransform.position;
            Quaternion cachedRot = selectedTransform.rotation;
            Vector3 cachedScale = selectedTransform.localScale;

            selectedTransform.position = Vector3.zero;
            selectedTransform.rotation = Quaternion.identity;
            selectedTransform.localScale = Vector3.one;

            //Clear old collisions
            DestroyAllComponentsOfType<BoxCollider>( Selection.activeGameObject );
            DestroyAllComponentsOfType<SphereCollider>( Selection.activeGameObject );
            for( int i=selectedTransform.childCount; i-->0; ){
                Transform child = selectedTransform.GetChild(i);
                if( child.name == "COLLISION" ){
                    DestroyImmediate( selectedTransform.GetChild(i).gameObject );
                }
            }
            Debug.Log("Building "+obj.name+" with "+obj.transform.childCount+" objects");
            for( int i=0; i<obj.transform.childCount; i++ ){
                Transform child = obj.transform.GetChild(i);
                MeshFilter childMF = child.GetComponent<MeshFilter>();
                if( childMF == null ){
                    errors.Add( obj.name+": no MeshFilter found" );
                    continue;
                }
                if( childMF.sharedMesh == null ){
                    errors.Add( obj.name+": no mesh" );
                }
                string error = "Unknown collision type";
                if( child.name.StartsWith("collisionCube") ){
                    error = BuildCollisionCube( childMF, selectedTransform );
                }else if( child.name.StartsWith("collisionRotatedCube") ){
                    error = BuildCollisionRotatedCube( childMF, selectedTransform );
                }else if( child.name.StartsWith("collisionSphere") ){
                    error = BuildCollisionSphere( childMF, selectedTransform );
                }else if( child.name.StartsWith("collisionConcave") ){
                    MeshCollider mesh = BuildCollisionMesh( childMF, selectedTransform );
                    mesh.convex = false;
                    error = null;
                }else if( child.name.StartsWith("collisionConvex") ){
                    MeshCollider mesh = BuildCollisionMesh( childMF, selectedTransform );
                    mesh.convex = true;
                    error = null;
                }
                if( error != null ){
                    errors.Add( child.name+": "+error );
                }
            }
            selectedTransform.position = cachedPos;
            selectedTransform.rotation = cachedRot;
            selectedTransform.localScale = cachedScale;
        }

        //display accumulated errors if any
        if( errors.Count > 0 ){
            string message = "";
            foreach( string error in errors ){
                message += "\n"+error;
            }
            EditorUtility.DisplayDialog( "Build Collision Setup "+errors.Count+" Error", message, "ok", "cancel" );
        }
    }

    private static void DestroyAllComponentsOfType<T>( GameObject targetObj ){
        T[] components = targetObj.GetComponents<T>();
        for( int i=0; i<components.Length; i++ ){
            DestroyImmediate( components[i] as Component );
        }
    }

    private static Vector3 MultVec3( Vector3 a, Vector3 b ){
        a.x *= b.x;
        a.y *= b.y;
        a.z *= b.z;
        return a;
    }
    
    private static string BuildCollisionCube( MeshFilter collisionMF, Transform targetObj ){

        Bounds bounds = collisionMF.sharedMesh.bounds;
        Vector3 worldCenter = collisionMF.transform.TransformPoint(bounds.center);
        worldCenter += targetObj.position;

        BoxCollider box = targetObj.gameObject.AddComponent<BoxCollider>();
        box.center = targetObj.InverseTransformPoint( worldCenter )-collisionMF.transform.position;
        box.size = MultVec3( bounds.size, collisionMF.transform.lossyScale );
        return null;
    }
    
    private static string BuildCollisionSphere( MeshFilter collisionMF, Transform targetObj ){

        Bounds bounds = collisionMF.sharedMesh.bounds;
        Vector3 worldCenter = collisionMF.transform.TransformPoint(bounds.center);
        worldCenter += targetObj.position;

        SphereCollider sphere = targetObj.gameObject.AddComponent<SphereCollider>();
        sphere.center = targetObj.InverseTransformPoint( worldCenter )-collisionMF.transform.position;
        sphere.radius = bounds.size.x*collisionMF.transform.lossyScale.x*0.5f;
        return null;
    }

    private class RotatedCubeFace{
        public Vector3 n;
        public Vector3 a;
        public Vector3 b;
        public Vector3 c;
        public Vector3 m;
    }

    private static string BuildCollisionRotatedCube( MeshFilter collisionMF, Transform targetObj ){

        Vector3[] vertices = collisionMF.sharedMesh.vertices;
        int[] indices = collisionMF.sharedMesh.GetIndices(0);

        if( vertices.Length != 24 ){
            return "Not a cube (bad vertex count) "+vertices.Length;
        }
        if( indices.Length != 36 ){
            return "Not a cube (bad indices count) "+indices.Length;
        }

        Vector3 worldCenter = Vector3.zero;
        RotatedCubeFace[] faces = new RotatedCubeFace[6];

        for( int i=0; i<indices.Length-2; i +=3 ){
            Vector3 a = vertices[ indices[i] ];
            Vector3 b = vertices[ indices[i+1] ];
            Vector3 c = vertices[ indices[i+2] ];
            Vector3 n = Vector3.Cross( a-b, b-c ).normalized;

            //sort into unique entry
            int normalIndex = -1;
            int lastEmpty = -1;
            for( int j=0; j<faces.Length; j++ ){
                if( faces[j] == null ){
                    lastEmpty = j;
                    continue;
                }
                if( Vector3.SqrMagnitude( faces[j].n-n ) < 0.02f ){
                    normalIndex = j;
                    break;
                }
            }
            if( normalIndex == -1 ){
                //find hypotenuse midpoint (center of face)
                Vector3 m = Vector3.zero;
                Vector3[] points = new Vector3[]{a,b,c};
                float longest = 0.0f;
                for( int j=0,k=points.Length-1; j<points.Length; k=j++ ){
                    float segLength = Vector3.SqrMagnitude( points[j]-points[k] );
                    if( segLength > longest ){
                        longest = segLength;
                        m = ( points[j]+points[k] )/2.0f;
                    }
                }
                faces[ lastEmpty ] = new RotatedCubeFace(){ n=n, a=a, b=b, c=c, m=m };
                worldCenter += m;
            }
        }
        worldCenter /= 6;
        //find complimentary face to find dimension distance
        Vector3 size = Vector3.zero;
        Vector3 up = Vector3.zero;
        Vector3 forward = Vector3.zero;
        int dimensionIndex = 0;
        for( int i=0; i<faces.Length; i++ ){

            if( faces[i] == null ){
                continue;
            }
            for( int j=i+1; j<faces.Length; j++ ){
                if( faces[j] == null ){
                    continue;
                }
                if( Vector3.SqrMagnitude( faces[i].n+faces[j].n ) < 0.01f ){
                    float distance = Vector3.Distance( faces[i].m, faces[j].m );
                    switch( dimensionIndex++ ){
                    case 0:
                        size.x = distance;
                        break;
                    case 1:
                        size.y = distance;
                        up = faces[i].n;
                        break;
                    case 2:
                        size.z = distance;
                        forward = faces[i].n;
                        break;
                    }
                    //mark as used
                    faces[i] = null;
                    faces[j] = null;
                    break;
                }
            }
        }

        if( forward == Vector3.zero ){
            return "not rectangular";
        }

        GameObject collisionContainer = new GameObject("COLLISION");
        collisionContainer.isStatic = targetObj.gameObject.isStatic;
        collisionContainer.layer = targetObj.gameObject.layer;
        collisionContainer.transform.SetParent( targetObj );
        collisionContainer.transform.localPosition = worldCenter;
        collisionContainer.transform.rotation = Quaternion.LookRotation( forward, up );

        BoxCollider box = collisionContainer.AddComponent<BoxCollider>();
        box.center = Vector3.zero;
        box.size = MultVec3( size, collisionMF.transform.lossyScale );

        return null;
    }

    private static MeshCollider BuildCollisionMesh( MeshFilter collisionMF, Transform targetObj ){

        GameObject collisionContainer = new GameObject("COLLISION");
        collisionContainer.isStatic = targetObj.gameObject.isStatic;
        collisionContainer.layer = targetObj.gameObject.layer;
        collisionContainer.transform.SetParent( targetObj );
        collisionContainer.transform.localPosition = Vector3.zero;
        collisionContainer.transform.rotation = Quaternion.identity;

        MeshCollider mesh = collisionContainer.AddComponent<MeshCollider>();
        mesh.sharedMesh = collisionMF.sharedMesh;

        return mesh;
    }
}