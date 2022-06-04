using UnityEditor;
using UnityEngine;


public class ParallaxCorrectionEditor : EditorWindow
{
    public static void ShowWindow()
    {
        GetWindow<ParallaxCorrectionEditor>();
    }

    [SerializeField]
    public Material targetMaterial;

    [SerializeField]
    public ReflectionProbe targetProbe;


    [MenuItem("Tools/Parallax Correction Editor")]
    static void Init()
    {
        GetWindow(typeof(ParallaxCorrectionEditor));
    }

    public void OnGUI()
    {

        ScriptableObject target = this;
        SerializedObject sObj = new SerializedObject(target);

        EditorGUILayout.PropertyField(sObj.FindProperty("targetMaterial"));
        EditorGUILayout.PropertyField(sObj.FindProperty("targetProbe"));
        sObj.ApplyModifiedProperties();

        if (targetMaterial != null && targetProbe != null)
        {

            targetMaterial.SetVector("_CubeCenter", targetProbe.transform.position);
            targetMaterial.SetVector("_CubeMin", targetProbe.bounds.min);
            targetMaterial.SetVector("_CubeMax", targetProbe.bounds.max);
        }
    }
}