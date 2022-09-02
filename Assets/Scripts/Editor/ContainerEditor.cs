using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


[CustomEditor(typeof(viva.Container), true)]
[CanEditMultipleObjects]
public class ContainerEditor : Editor
{
    private static GUIStyle boldStyle = new GUIStyle();
    private SerializedObject sObj;
    private bool enableKeyboardModeMixing = false;
    private SerializedProperty keyboardMixAnimProp;

    private void OnEnable()
    {
        boldStyle.fontStyle = FontStyle.Bold;
        sObj = new SerializedObject(target);
        keyboardMixAnimProp = sObj.FindProperty("playerKeyboardMixAnimations");
        enableKeyboardModeMixing = keyboardMixAnimProp.arraySize != 0;
    }
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        enableKeyboardModeMixing = GUILayout.Toggle(enableKeyboardModeMixing, "Enable keyboard mode mixing");
        if (enableKeyboardModeMixing)
        {

            if (keyboardMixAnimProp.arraySize == 0)
            {
                keyboardMixAnimProp.arraySize = System.Enum.GetValues(typeof(viva.Container.PlayerKeyboardMixAnimType)).Length;
                sObj.ApplyModifiedProperties();
            }

            for (int i = 0; i < keyboardMixAnimProp.arraySize; i++)
            {
                viva.Container.PlayerKeyboardMixAnimType enumVal = (viva.Container.PlayerKeyboardMixAnimType)i;

                SerializedProperty subProp = keyboardMixAnimProp.GetArrayElementAtIndex(i);
                var oldSel = (viva.Player.Animation)subProp.enumValueIndex;
                var newSel = (viva.Player.Animation)EditorGUILayout.EnumPopup(enumVal.ToString(), oldSel);
                if (newSel != oldSel)
                {
                    subProp.enumValueIndex = (int)newSel;
                    sObj.ApplyModifiedProperties();
                }
            }
        }
        else
        {
            if (keyboardMixAnimProp.arraySize != 0)
            {
                keyboardMixAnimProp.arraySize = 0;
                sObj.ApplyModifiedProperties();
            }
        }

        if (GUILayout.Button("Build Collider list"))
        {
            var newColliders = ((viva.Container)target).gameObject.GetComponentsInChildren<Collider>();
            var validColliders = new List<Collider>();
            foreach (var c in newColliders)
            {
                if (c.gameObject.layer == viva.WorldUtil.itemsLayer && !c.isTrigger)
                {
                    validColliders.Add(c);
                }
            }
            SerializedProperty colliders = sObj.FindProperty("m_colliders");
            colliders.arraySize = validColliders.Count;
            for (int i = 0; i < colliders.arraySize; i++)
            {
                colliders.GetArrayElementAtIndex(i).objectReferenceValue = validColliders[i];
            }
            sObj.ApplyModifiedProperties();
        }
    }
}