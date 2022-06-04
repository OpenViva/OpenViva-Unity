using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(viva.PoseCache))]
[CanEditMultipleObjects]
public class PoseCacheEditor : Editor
{

    public viva.Loli loli;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        loli = EditorGUILayout.ObjectField("Loli", loli, typeof(viva.Loli), true) as viva.Loli;

        if (loli)
        {
            if (GUILayout.Button("Copy"))
            {
                SerializedObject sObj = new SerializedObject(target);
                sObj.ApplyModifiedProperties();

                var positionsObj = sObj.FindProperty("positions");
                var quaternionsObj = sObj.FindProperty("quaternions");
                positionsObj.arraySize = loli.bodySMRs[0].bones.Length;
                quaternionsObj.arraySize = loli.bodySMRs[0].bones.Length;
                for (int i = 0; i < positionsObj.arraySize; i++)
                {
                    positionsObj.GetArrayElementAtIndex(i).vector3Value = loli.bodySMRs[0].bones[i].localPosition;
                    quaternionsObj.GetArrayElementAtIndex(i).quaternionValue = loli.bodySMRs[0].bones[i].localRotation;
                    Debug.Log("[Pose Cache] " + i + " = " + loli.bodySMRs[0].bones[i].name);
                }
                sObj.ApplyModifiedProperties();
            }
            if (GUILayout.Button("Apply"))
            {
                SerializedObject sObj = new SerializedObject(target);
                var positionsObj = sObj.FindProperty("positions");
                var quaternionsObj = sObj.FindProperty("quaternions");

                for (int i = 0; i < loli.bodySMRs[0].bones.Length; i++)
                {
                    Transform t = loli.bodySMRs[0].bones[i];
                    t.localPosition = positionsObj.GetArrayElementAtIndex(i).vector3Value;
                    t.localRotation = quaternionsObj.GetArrayElementAtIndex(i).quaternionValue;
                }
            }
        }
    }

}