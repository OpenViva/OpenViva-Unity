using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class BuildInstancesEditor : EditorWindow
{

    public GameObject target = null;
    public List<GameObject> prefabs = new List<GameObject>();
    private Vector2 scrollPos;

    [MenuItem("Window/Instance Builder")]
    static void Init()
    {
        GetWindow(typeof(BuildInstancesEditor));
    }
    public static void ShowWindow()
    {

        GetWindow<BuildInstancesEditor>();
    }

    public void OnGUI()
    {
        SerializedObject sObj = new SerializedObject(this);

        EditorGUILayout.PropertyField(sObj.FindProperty("target"), false);
        if (target != null)
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos,
                    GUILayout.Width(EditorGUIUtility.currentViewWidth),
                    GUILayout.Height(400));
            EditorGUILayout.PropertyField(sObj.FindProperty("prefabs"), true);

            if (GUILayout.Button("Build", GUILayout.Width(70)))
            {
                buildInstances();
            }
            EditorGUILayout.EndScrollView();
        }

        sObj.ApplyModifiedProperties();
    }

    public void buildInstances()
    {

        for (int i = 0; i < target.transform.childCount; i++)
        {
            for (int j = 0; j < target.transform.GetChild(i).childCount; j++)
            {
                DestroyImmediate(target.transform.GetChild(i).GetChild(j).gameObject);
            }
        }
        int errors = 0;
        string log = "";
        for (int i = 0; i < target.transform.childCount; i++)
        {
            GameObject obj = target.transform.GetChild(i).gameObject;

            string prefabName = obj.name.Split('_')[0];
            GameObject prefab = null;
            for (int j = 0; j < prefabs.Count; j++)
            {
                if (prefabs[j].name == prefabName)
                {
                    prefab = prefabs[j];
                    break;
                }
            }
            if (prefab != null)
            {

                GameObject instance = Instantiate(prefab);
                instance.transform.SetParent(obj.transform, false);
                instance.transform.localScale = instance.transform.localScale / 100.0f;
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
            }
            else
            {
                errors++;
                DestroyImmediate(obj);
                log += "\n" + prefabName;
            }
        }
#if UNITY_EDITOR
        if (errors != 0)
        {
            EditorUtility.DisplayDialog("Build Instances", "Missing:" + log, "Ok");
        }
#endif
    }
}
