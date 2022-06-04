using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using viva;

public class BathtubEditor : EditorWindow
{
    public static void ShowWindow()
    {
        GetWindow<BathtubEditor>();
    }

    [SerializeField]
    private Mesh wireMesh = null;

    [SerializeField]
    private Bathtub bathtub = null;


    [MenuItem("Tools/Bathtub Editor")]
    static void Init()
    {
        GetWindow(typeof(BathtubEditor));
    }

    private float getRadian(Vector3 v)
    {
        return Mathf.Atan2(v.y, v.x);
    }

    public void OnGUI()
    {

        SerializedObject sEditor = new SerializedObject(this);
        EditorGUILayout.PropertyField(sEditor.FindProperty("wireMesh"));
        EditorGUILayout.PropertyField(sEditor.FindProperty("bathtub"));
        sEditor.ApplyModifiedProperties();

        if (wireMesh == null || bathtub == null)
        {
            return;
        }

        if (GUILayout.Button("Apply"))
        {

            List<Vector3> vertices = new List<Vector3>();
            wireMesh.GetVertices(vertices);

            if (vertices.Count % Bathtub.WATER_LAYERS != 0)
            {
                EditorUtility.DisplayDialog("Error", "Mesh doesn't have vertex count divisible by " + Bathtub.WATER_LAYERS, "ok");
                return;
            }

            SerializedObject sBathtub = new SerializedObject(bathtub);
            SerializedProperty waterMeshPointsProp = sBathtub.FindProperty("waterMeshPoints");
            int verticesPerLayer = vertices.Count / Bathtub.WATER_LAYERS;

            //order vertices increasing z in height layers
            float height = Mathf.NegativeInfinity;
            const float error = 0.0001f;
            List<Vector3> waterMeshPoints = new List<Vector3>();
            for (int i = 0; i < Bathtub.WATER_LAYERS; i++)
            {

                //find next lowest height
                float nextHeight = Mathf.Infinity;
                for (int j = 0; j < vertices.Count; j++)
                {
                    if (vertices[j].z > height + error)
                    {
                        nextHeight = Mathf.Min(nextHeight, vertices[j].z);
                    }
                }
                height = nextHeight;
                Debug.Log("Calculating height level " + height);

                for (int j = 0; j < vertices.Count; j++)
                {
                    Vector3 candidate = vertices[j];
                    if (Mathf.Abs(candidate.z - height) > error)
                    {
                        continue;
                    }

                    float radian = getRadian(candidate);

                    //percolate into waterMeshPoints in appropriate layer
                    int insertIndex = verticesPerLayer * i;
                    int maxIndex = Mathf.Min(insertIndex + verticesPerLayer, waterMeshPoints.Count);
                    for (int k = insertIndex; k < maxIndex; k++)
                    {
                        float oldRadian = getRadian(waterMeshPoints[k]);
                        if (radian > oldRadian)
                        {
                            insertIndex = k + 1;
                        }
                        else
                        {
                            break;
                        }
                    }
                    waterMeshPoints.Insert(insertIndex, candidate);
                }
            }
            if (waterMeshPoints.Count != vertices.Count)
            {
                Debug.LogError("VERTICES PER LAYER DOES NOT CONSISTENT FOR ALL LAYERS! " + waterMeshPoints.Count + "/" + vertices.Count);
                return;
            }
            //add uvs from top layer
            SerializedProperty meshUVsProp = sBathtub.FindProperty("meshUVs");
            meshUVsProp.arraySize = verticesPerLayer;

            List<Vector2> uvs = new List<Vector2>();
            wireMesh.GetUVs(0, uvs);
            int uvIndex = 0;
            for (int i = (Bathtub.WATER_LAYERS - 1) * verticesPerLayer; i < Bathtub.WATER_LAYERS * verticesPerLayer; i++)
            {
                SerializedProperty uvProp = meshUVsProp.GetArrayElementAtIndex(uvIndex++);
                for (int j = 0; j < vertices.Count; j++)
                {
                    if (vertices[j] == waterMeshPoints[i])
                    {
                        uvProp.vector2Value = uvs[j];
                    }
                }
            }
            //copy to property
            waterMeshPointsProp.arraySize = waterMeshPoints.Count;
            for (int i = 0; i < waterMeshPoints.Count; i++)
            {
                SerializedProperty indexProp = waterMeshPointsProp.GetArrayElementAtIndex(i);
                indexProp.vector3Value = waterMeshPoints[i];
            }

            sBathtub.ApplyModifiedProperties();
        }
    }
}