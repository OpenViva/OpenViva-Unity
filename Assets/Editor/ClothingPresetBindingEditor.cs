using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using viva;


[CustomEditor(typeof(ClothingPresetBinding))]
public class ClothingPresetBindingEditor : Editor
{

    public override void OnInspectorGUI()
    {

        if (GUILayout.Button("Generate Skeleton profile"))
        {
            GenerateBoneProfile();
        }

        DrawDefaultInspector();
    }

    private int FindDeformableTransformIndex(string name, Loli loli)
    {
        for (int i = 0; i < loli.bodySMRs[0].bones.Length; i++)
        {
            Transform t = loli.bodySMRs[0].bones[i];
            if (t.name == name)
            {
                return i;
            }
        }
        return -1;
    }

    private void GenerateBoneProfile()
    {

        string loliPrefabPath = "Assets/Items/prefabs/loli.prefab";
        Loli loli = AssetDatabase.LoadAssetAtPath(loliPrefabPath, typeof(Loli)) as Loli;
        if (loli == null)
        {
            Debug.LogError("Could not find loli asset");
            return;
        }

        GameObject targetObj = (target as ClothingPresetBinding).gameObject;
        Transform armatureBase = targetObj.transform.Find("ShinobuArmature");
        Transform model = targetObj.transform.GetChild(1 - armatureBase.GetSiblingIndex());
        if (model == null || armatureBase == null)
        {
            Debug.LogError("Missing model or ShinobuArmature");
            return;
        }

        SkinnedMeshRenderer smr = model.GetComponent<SkinnedMeshRenderer>();
        var boneWeights = smr.sharedMesh.boneWeights;
        int[] boneUses = new int[smr.bones.Length];
        foreach (var boneWeight in boneWeights)
        {
            if (boneWeight.weight0 > 0)
            {
                boneUses[boneWeight.boneIndex0]++;
            }
            if (boneWeight.weight1 > 0)
            {
                boneUses[boneWeight.boneIndex1]++;
            }
            if (boneWeight.weight2 > 0)
            {
                boneUses[boneWeight.boneIndex2]++;
            }
            if (boneWeight.weight3 > 0)
            {
                boneUses[boneWeight.boneIndex3]++;
            }
        }

        foreach (Transform bone in smr.bones)
        {
            bone.SetParent(armatureBase, true);
        }

        //trim away unused bones from list
        int trimmed = 0;
        List<Matrix4x4> remappedBindPoses = new List<Matrix4x4>();
        List<Transform> remappedBoneList = new List<Transform>();
        for (int i = 0; i < boneUses.Length; i++)
        {
            var bone = smr.bones[i];
            if (boneUses[i] == 0)
            {
                GameObject.DestroyImmediate(bone.gameObject);
                trimmed++;
            }
            else
            {
                remappedBindPoses.Add(smr.sharedMesh.bindposes[i]);
                remappedBoneList.Add(bone);
            }
        }
        Debug.Log("[ClothingPresetBinding] Trimmed " + trimmed + " bones");
        //rebind boneWeights
        int[] remappedBoneIndices = new int[boneUses.Length];
        int usedIndex = 0;
        for (int i = 0; i < boneUses.Length; i++)
        {
            if (boneUses[i] > 0)
            {
                remappedBoneIndices[i] = usedIndex++;
            }
        }

        var remappedBoneWeights = new BoneWeight[boneWeights.Length];
        for (int i = 0; i < remappedBoneWeights.Length; i++)
        {
            var remappedBoneWeight = new BoneWeight();
            var boneWeight = boneWeights[i];

            if (boneWeight.weight0 > 0)
            {
                remappedBoneWeight.boneIndex0 = remappedBoneIndices[boneWeight.boneIndex0];
                remappedBoneWeight.weight0 = boneWeight.weight0;
            }

            if (boneWeight.weight1 > 0)
            {
                remappedBoneWeight.boneIndex1 = remappedBoneIndices[boneWeight.boneIndex1];
                remappedBoneWeight.weight1 = boneWeight.weight1;
            }

            if (boneWeight.weight2 > 0)
            {
                remappedBoneWeight.boneIndex2 = remappedBoneIndices[boneWeight.boneIndex2];
                remappedBoneWeight.weight2 = boneWeight.weight2;
            }

            if (boneWeight.weight3 > 0)
            {
                remappedBoneWeight.boneIndex3 = remappedBoneIndices[boneWeight.boneIndex3];
                remappedBoneWeight.weight3 = boneWeight.weight3;
            }
            remappedBoneWeights[i] = remappedBoneWeight;
        }

        smr.sharedMesh.boneWeights = remappedBoneWeights;
        smr.sharedMesh.bindposes = remappedBindPoses.ToArray();
        smr.bones = remappedBoneList.ToArray();

        //serialize bone index order
        SerializedObject sObj = new SerializedObject(target);
        var boneBindingIndicesProp = sObj.FindProperty("m_boneBindingIndices");
        boneBindingIndicesProp.arraySize = smr.bones.Length;
        for (int i = 0; i < smr.bones.Length; i++)
        {
            var element = boneBindingIndicesProp.GetArrayElementAtIndex(i);
            element.intValue = FindDeformableTransformIndex(smr.bones[i].name, loli);
        }

        sObj.ApplyModifiedProperties();
    }
}