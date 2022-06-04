using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using viva;



public class LoliEditor : EditorWindow
{
    [MenuItem("Tools/Loli Editor")]
    static void Init()
    {
        GetWindow(typeof(LoliEditor));
    }
    public static void ShowWindow()
    {
        GetWindow<LoliEditor>();
    }

    [SerializeField]
    public Voice targetVoice;

    [SerializeField]
    public Loli target;

    private GUIStyle titleStyle = null;
    private Vector2 scrollPos = Vector2.zero;
    private int previewIndex = 0;
    private readonly string VOICE_SOUNDS_ROOT = "Assets/Sound/voices/";
    private readonly string VOICES_ROOT = "Assets/ScriptableObjects/Sound Sets/voices/";

    public void OnGUI()
    {

        if (titleStyle == null)
        {
            titleStyle = new GUIStyle();
            titleStyle.fontSize = 16;
            titleStyle.fontStyle = FontStyle.Bold;

            target = Component.FindObjectOfType(typeof(Loli)) as Loli;

        }

        SerializedObject sObj = new SerializedObject(this);
        EditorGUILayout.PropertyField(sObj.FindProperty("target"));
        EditorGUILayout.PropertyField(sObj.FindProperty("targetVoice"));
        sObj.ApplyModifiedProperties();

        if (targetVoice != null)
        {

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            SerializedObject sTarg = new SerializedObject(targetVoice);
            ShowEditVoice(sTarg);
            sTarg.ApplyModifiedProperties();

            EditorGUILayout.EndScrollView();
        }
        this.minSize = new Vector2(400, 400);
    }

    public void ShowEditVoice(SerializedObject sObj)
    {

        SerializedProperty listProperty = sObj.FindProperty("voiceLines");
        if (listProperty.arraySize != System.Enum.GetValues(typeof(Loli.VoiceLine)).Length)
        {
            listProperty.arraySize = System.Enum.GetValues(typeof(Loli.VoiceLine)).Length;
            return;
        }
        Voice sourceObject = sObj.targetObject as Voice;
        string typeName = sourceObject.type.ToString().ToLower();

        EditorGUI.indentLevel = 0;
        if (GUILayout.Button("match order"))
        {
            for (int i = 0; i < listProperty.arraySize; i++)
            {
                SerializedProperty elementProperty = listProperty.GetArrayElementAtIndex(i);
                string enumName = ((Loli.VoiceLine)i).ToString().ToLower();
                string assetName = VOICES_ROOT + typeName + "/" + typeName + "_" + enumName + ".asset";
                if (System.IO.File.Exists(assetName))
                {
                    elementProperty.objectReferenceValue = AssetDatabase.LoadAssetAtPath(assetName, typeof(SoundSet));
                }
            }
        }


        EditorGUILayout.LabelField("voiceLines", titleStyle);
        EditorGUI.indentLevel = 1;
        for (int i = 0; i < listProperty.arraySize; i++)
        {

            SoundSet voiceSet = sourceObject.voiceLines[i];
            SerializedProperty elementProperty = listProperty.GetArrayElementAtIndex(i);
            Rect drawZone = GUILayoutUtility.GetRect(300f, 18f, GUILayout.MaxWidth(300));

            string enumName = ((Loli.VoiceLine)i).ToString().ToLower();

            //get state color
            bool incomplete = false;
            Color color;
            float modulate = 0.8f + (float)(i % 2) * 0.2f;
            if (voiceSet == null)
            {
                color = new Color(0.8f * modulate, 0.3f, 0.3f);
                incomplete = true;
            }
            else
            {
                if (voiceSet.sounds == null)
                {
                    voiceSet.sounds = new AudioClip[0];
                }
                if (voiceSet.sounds.Length == 0)
                {
                    incomplete = true;
                }
                for (int j = 0; j < voiceSet.sounds.Length; j++)
                {
                    if (voiceSet.sounds[j] == null)
                    {
                        incomplete = true;
                        break;
                    }
                }
                if (incomplete)
                {
                    color = new Color(0.8f * modulate, 0.8f * modulate, 0.3f);
                }
                else
                {
                    if (voiceSet.name == typeName + "_" + enumName)
                    {
                        color = new Color(0.3f, 0.8f * modulate, 0.3f);
                    }
                    else
                    {
                        color = new Color(0.7f, 0.7f, 0.7f) * modulate;
                    }
                }
            }

            EditorGUI.DrawRect(drawZone, color);

            EditorGUI.PropertyField(drawZone, elementProperty, new GUIContent(enumName));
            drawZone.x += drawZone.width;
            drawZone.width = EditorGUIUtility.currentViewWidth - drawZone.x;
            if (voiceSet != null)
            {

                if (incomplete)
                {
                    if (GUI.Button(drawZone, "incomplete"))
                    {
                        Selection.activeObject = voiceSet;
                        var dir = new System.IO.DirectoryInfo(VOICE_SOUNDS_ROOT + typeName);
                        var files = dir.GetFiles();
                        if (enumName.Length >= 4)
                        {
                            for (int j = 0; j < files.Length; j++)
                            {
                                if (files[j].DirectoryName.Length >= 4)
                                {
                                    string[] words = files[j].FullName.Split('/');
                                    string last = words[words.Length - 1];
                                    if (last.Substring(0, 4) == enumName.Substring(0, 4))
                                    {
                                        AudioClip match = AssetDatabase.LoadAssetAtPath(VOICE_SOUNDS_ROOT + typeName + "/" + last, typeof(AudioClip)) as AudioClip;
                                        if (match)
                                        {
                                            EditorGUIUtility.PingObject(match);
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else if (GUI.Button(drawZone, "preview"))
                {

                    previewIndex++;
                    if (previewIndex >= voiceSet.sounds.Length)
                    {
                        previewIndex = 0;
                    }
                    PlayClip(voiceSet.sounds[previewIndex]);
                    Selection.activeObject = voiceSet;
                }
            }
            else
            {
                if (GUI.Button(drawZone, "create"))
                {

                    string assetName = VOICES_ROOT + typeName + "/" + typeName + "_" + enumName + ".asset";
                    Debug.Log("[VOICE EDITOR] " + assetName);
                    if (!System.IO.File.Exists(assetName))
                    {

                        SoundSet newVoiceSet = ScriptableObject.CreateInstance("SoundSet") as SoundSet;
                        newVoiceSet.name = enumName;

                        AssetDatabase.CreateAsset(newVoiceSet, assetName);
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();

                        Selection.activeObject = newVoiceSet;
                    }
                    elementProperty.objectReferenceValue = AssetDatabase.LoadAssetAtPath(assetName, typeof(SoundSet));

                }
            }
        }
        EditorGUI.indentLevel--;
    }

    public static void PlayClip(AudioClip clip)
    {
        Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
        Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
        MethodInfo method = audioUtilClass.GetMethod(
            "PlayClip",
            BindingFlags.Static | BindingFlags.Public,
            null,
            new System.Type[] {
                typeof(AudioClip),
                typeof(Int32),
                typeof(Boolean)
            },
        null
        );
        method.Invoke(
            null,
            new object[] {
                clip,
                0,
                false
            }
        );
    }
}