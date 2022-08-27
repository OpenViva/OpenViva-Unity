using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using viva;



public class RiverRegionEditor : EditorWindow
{
    [MenuItem("Tools/River Region Editor")]
    static void Init()
    {
        GetWindow(typeof(RiverRegionEditor));
    }
    public static void ShowWindow()
    {
        GetWindow<RiverRegionEditor>();
    }

    private class Vert
    {
        public Vector3 pos;
        public Vector2 uv;
    }

    [SerializeField]
    public GameObject root;

    [SerializeField]
    public Mesh edges;
    [SerializeField]
    private float currentStrength = 0.0f;
    [SerializeField]
    private float boxHeight = 1.0f;

    private SerializedObject sObj;
    private GUIStyle titleStyle = null;
    private SerializedProperty rootProperty;
    private SerializedProperty edgesProperty;
    private SerializedProperty currentStrengthProperty;
    private SerializedProperty boxHeightProperty;

    private void OnEnable()
    {
        sObj = new SerializedObject(this);
        rootProperty = sObj.FindProperty("root");
        edgesProperty = sObj.FindProperty("edges");
        currentStrengthProperty = sObj.FindProperty("currentStrength");
        boxHeightProperty = sObj.FindProperty("boxHeight");
    }

    private void ShowAndUpdateProperty(SerializedProperty sProp)
    {
        if (sProp.type == "float")
        {
            float old = sProp.floatValue;
            EditorGUILayout.PropertyField(sProp);
            if (sProp.floatValue != old)
            {
                sObj.ApplyModifiedProperties();
            }
        }
        else
        {
            var old = sProp.objectReferenceInstanceIDValue;
            EditorGUILayout.PropertyField(sProp);
            if (sProp.objectReferenceInstanceIDValue != old)
            {
                sObj.ApplyModifiedProperties();
            }
        }
    }

    public void OnGUI()
    {

        if (titleStyle == null)
        {
            titleStyle = new GUIStyle();
            titleStyle.fontSize = 16;
            titleStyle.fontStyle = FontStyle.Bold;
        }
        ShowAndUpdateProperty(rootProperty);
        if (root == null)
        {
            return;
        }
        ShowAndUpdateProperty(edgesProperty);
        if (edges == null)
        {
            return;
        }
        ShowAndUpdateProperty(currentStrengthProperty);
        ShowAndUpdateProperty(boxHeightProperty);
        if (GUILayout.Button("Build"))
        {
            BuildWaterRegions();
        }
    }

    private float GetOrderValue(Vert vert)
    {
        return vert.uv.y * 1000.0f + vert.uv.x;
    }

    private void BuildWaterRegions()
    {
        //delete all children first
        while (root.transform.childCount > 0)
        {
            DestroyImmediate(root.transform.GetChild(0).gameObject);
        }

        //build along mesh
        List<Vert> verts = new List<Vert>(edges.vertices.Length);
        for (int i = 0; i < edges.vertices.Length; i++)
        {
            var vert = new Vert();
            vert.pos = edges.vertices[i];
            vert.uv = edges.uv[i];
            bool found = false;
            foreach (var c in verts)
            {
                if (c.pos == vert.pos)
                {
                    found = true;
                }
            }
            if (!found)
            {
                verts.Add(vert);
            }
        }
        verts.Sort((emp1, emp2) => GetOrderValue(emp1).CompareTo(GetOrderValue(emp2)));
        for (int i = 2; i < verts.Count - 2; i += 2)
        {
            Vert prevA = verts[i - 2];
            Vert prevB = verts[i - 1];
            Vert currA = verts[i];
            Vert currB = verts[i + 1];

            var container = new GameObject("region");
            container.layer = WorldUtil.waterLayer;
            container.isStatic = true;
            container.name = "footstep_water";
            container.transform.SetParent(root.transform, false);
            BoxCollider bc = container.AddComponent<BoxCollider>();

            float length = (Vector3.Magnitude(prevA.pos - currA.pos) + Vector3.Magnitude(prevB.pos - currB.pos)) / 2.0f;
            float width = (Vector3.Magnitude(prevA.pos - prevB.pos) + Vector3.Magnitude(currA.pos - currB.pos)) / 2.0f;

            bc.transform.position = (currA.pos + currB.pos + prevA.pos + prevB.pos) / 4.0f;
            var rotA = Quaternion.LookRotation(currA.pos - prevA.pos, Vector3.up);
            var rotB = Quaternion.LookRotation(currB.pos - prevB.pos, Vector3.up);
            bc.transform.rotation = Quaternion.LerpUnclamped(rotA, rotB, 0.5f);
            bc.size = new Vector3(width + 0.3f, boxHeight, length * 1.5f);
            bc.center = Vector2.down * boxHeight * 0.5f;
            bc.isTrigger = true;

            //attach current force if applicable
            Vector3 currentDir = ((currA.pos - prevA.pos) + (currB.pos - prevB.pos)) / 2.0f;
            if (currentStrength > 0.0f && currentDir.sqrMagnitude > 0.0f)
            {
                WaterCurrent wc = container.AddComponent<WaterCurrent>();
                var wcSOBJ = new SerializedObject(wc);
                var forceProperty = wcSOBJ.FindProperty("m_force");
                forceProperty.vector3Value = currentDir * currentStrength;
                wcSOBJ.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}