using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(viva.FingerAnimator))]
[CanEditMultipleObjects]
public class FingerAnimatorEditor : Editor
{
    private static GUIStyle boldStyle = new GUIStyle();

    private void OnEnable()
    {
        boldStyle.fontStyle = FontStyle.Bold;
    }
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Generate Finger Profile"))
        {
            SerializedObject sObj = new SerializedObject(target);

            SerializedProperty fingersProp = sObj.FindProperty("fingers");
            fingersProp.arraySize = 15;

            Transform transform = (target as viva.FingerAnimator).transform;
            string suffix = transform.name.Split('_')[1];
            int buildIndex = 0;
            addTreeTransform(transform, fingersProp, ref buildIndex, "thumb1_" + suffix + "/thumb2_" + suffix + "/thumb3_" + suffix);
            addTreeTransform(transform, fingersProp, ref buildIndex, "index1_" + suffix + "/index2_" + suffix + "/index3_" + suffix);
            addTreeTransform(transform, fingersProp, ref buildIndex, "middle1_" + suffix + "/middle2_" + suffix + "/middle3_" + suffix);
            addTreeTransform(transform, fingersProp, ref buildIndex, "ring1_" + suffix + "/ring2_" + suffix + "/ring3_" + suffix);
            addTreeTransform(transform, fingersProp, ref buildIndex, "pinky1_" + suffix + "/pinky2_" + suffix + "/pinky3_" + suffix);

            sObj.ApplyModifiedProperties();
        }
    }

    private void addTreeTransform(Transform hand, SerializedProperty fingersProp, ref int buildIndex, string search)
    {
        Transform fingerEnd = hand.Find(search);
        fingersProp.GetArrayElementAtIndex(buildIndex++).objectReferenceValue = fingerEnd;
        fingersProp.GetArrayElementAtIndex(buildIndex++).objectReferenceValue = fingerEnd.parent;
        fingersProp.GetArrayElementAtIndex(buildIndex++).objectReferenceValue = fingerEnd.parent.parent;
    }


}