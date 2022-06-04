using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;


namespace viva
{


    public partial class VivaModel
    {

        public class CreateLoliRequest
        {
            public readonly string sourceCardFilename;
            public readonly Loli targetLoli = null;
            public readonly byte[] data = null;
            public readonly ModelBuildSettings modelImportSettings = null;
            public readonly bool dataIncludesModelSettings;
            public readonly string loliName;
            public string error = null;
            public Loli result = null;
            public ModelBuildSettings mbs = null;

            public CreateLoliRequest(string _sourceCardFilename, Loli _targetLoli, byte[] _buildData, ModelBuildSettings _modelImportSettings)
            {
                sourceCardFilename = _sourceCardFilename;
                targetLoli = _targetLoli;
                data = _buildData;
                dataIncludesModelSettings = true;
                modelImportSettings = _modelImportSettings;
            }

            public CreateLoliRequest(Loli _targetLoli, string filePath, ModelBuildSettings _modelImportSettings)
            {
                targetLoli = _targetLoli;
                string[] words = filePath.Split('\\').Last().Split('/').Last().Split('.');
                loliName = "";
                for (int i = 0; i < words.Length - 1; i++)
                {
                    loliName += words[i];
                }

                try
                {
                    data = System.IO.File.ReadAllBytes(filePath);
                }
                catch
                {
                    data = null;
                }
                dataIncludesModelSettings = false;
                modelImportSettings = _modelImportSettings;
            }
        }

        public static IEnumerator DeserializeVivaModel(CreateLoliRequest request)
        {

            request.error = null;
            request.result = null;

            if (request.data == null)
            {
                request.error = "\nDeserialize request rawData is null";
                yield break;
            }

            VivaModel model = new VivaModel(request.sourceCardFilename, request.loliName);
            if (request.dataIncludesModelSettings)
            {
                model.DeserializeFromCardData(request.data);
            }
            else
            {
                model.modelData = request.data;
            }

            ByteStreamReader bsr = new ByteStreamReader(model.modelData);
            float version = bsr.ReadFloat();
            if (version < MODEL_IMPORT_VERSION)
            {
                request.error = "Model version outdated.\nUse the latest exporter version";
                yield break;
            }
            if (version > MODEL_IMPORT_VERSION)
            {
                request.error = "Model version is from the future. Wtf.";
                yield break;
            }
            Dictionary<string, int> properties = new Dictionary<string, int>();
            int propertyCount = bsr.ReadUnsigned1ByteInt();
            for (int i = 0; i < propertyCount; i++)
            {
                string propertyName = bsr.ReadUTF8String(bsr.ReadUnsigned1ByteInt());
                properties[propertyName] = bsr.ReadUnsigned2ByteInt();
            }
            model.texture = request.modelImportSettings.modelTexture;
            model.rightEyeTexture = request.modelImportSettings.rightEyeTexture;
            model.leftEyeTexture = request.modelImportSettings.leftEyeTexture;

            model.boneNamesBytesLength = properties["boneNamesBytesLength"];
            model.boneCount = properties["boneCount"];
            model.childBoneIndicesTableLength = properties["childBoneIndicesTableLength"];
            model.materialNamesBytesLength = properties["materialNamesBytesLength"];
            model.vertexCount = properties["vertexCount"];
            model.materialCount = properties["materialCount"];
            model.triIndicesCount = properties["triIndicesCount"] * 3;
            model.shapeKeyCount = properties["shapeKeyCount"];
            model.shapeKeyNamesByteLength = properties["shapeKeyNamesByteLength"];
            model.shapeKeyVertexElements = properties["shapeKeyVertexElements"];

            //start Job
            NativeArray<byte> nativeTargetArray = Steganography.ConvertToNativeArray(model.modelData, Allocator.TempJob);
            var decodeVivaModel = new Steganography.DecodeVivaModelJob(
                bsr.index,
                nativeTargetArray,
                model.boneNamesBytesLength,
                model.boneCount,
                model.childBoneIndicesTableLength,
                model.materialNamesBytesLength,
                model.vertexCount,
                model.materialCount,
                model.triIndicesCount,
                model.shapeKeyCount,
                model.shapeKeyNamesByteLength,
                model.shapeKeyVertexElements
            );
            JobHandle jobHandle = decodeVivaModel.Schedule();

            while (true)
            {
                if (!jobHandle.IsCompleted)
                {
                    yield return null;
                    continue;
                }
                break;
            }
            jobHandle.Complete();
            nativeTargetArray.Dispose();
            //convert
            var mbs = request.modelImportSettings;
            request.mbs = mbs;

            mbs.boneCount = model.boneCount;
            mbs.vertexCount = model.vertexCount;
            mbs.shapeKeyCount = model.shapeKeyCount;
            mbs.materialCount = model.materialCount;
            mbs.headpatWorldSphere = decodeVivaModel.sphereInfos[0];
            mbs.hatLocalPosAndPitch = decodeVivaModel.sphereInfos[1];
            mbs.headCollisionWorldSphere = decodeVivaModel.sphereInfos[2];

            mbs.boneNameTable = NativeByteArrayToStringArray(decodeVivaModel.boneNameStringTable, mbs.boneCount);
            mbs.boneHeadTable = NativeArrayToVector3Array(decodeVivaModel.boneHeadTable);
            mbs.boneTailTable = NativeArrayToVector3Array(decodeVivaModel.boneTailTable);
            mbs.boneRollTable = NativeArrayToFloatArray(decodeVivaModel.boneRollTable);
            mbs.boneHierarchyChildCountTable = NativeArrayToIntArray(decodeVivaModel.boneHierarchyChildCountTable);
            mbs.IsPartOfBaseSkeletonTable = NativeArrayToBoolArray(decodeVivaModel.IsPartOfBaseSkeletonTable);
            mbs.childBoneIndicesTable = NativeArrayToIntArray(decodeVivaModel.childBoneIndicesTable);
            mbs.materialNameTable = NativeByteArrayToStringArray(decodeVivaModel.materialNamesTable, mbs.materialCount);
            mbs.vertexTable = NativeArrayToVector3Array(decodeVivaModel.vertexTable);
            mbs.normalTable = NativeArrayToVector3Array(decodeVivaModel.normalTable);
            mbs.uvTable = NativeArrayToVector2Array(decodeVivaModel.uvTable);
            mbs.triVertexIndicesTable = NativeArrayToIntArray(decodeVivaModel.triVertexIndicesTable);
            mbs.boneWeightsTable = NativeArrayToBoneWeightsArray(decodeVivaModel.boneWeightsTable);
            mbs.submeshTriCountTable = NativeArrayToIntArray(decodeVivaModel.submeshTriCountTable);
            mbs.shapeKeyNamesTable = NativeByteArrayToStringArray(decodeVivaModel.shapeKeyNamesTable, mbs.shapeKeyCount);
            mbs.shapeKeyLengthsTable = NativeArrayToIntArray(decodeVivaModel.shapeKeyLengthsTable);
            mbs.vertexDeltaIndicesTable = NativeArrayToIntArray(decodeVivaModel.vertexDeltaIndicesTable);
            mbs.vertexDeltaOffsetTable = NativeArrayToVector3Array(decodeVivaModel.vertexDeltaOffsetTable);

            decodeVivaModel.sphereInfos.Dispose();
            decodeVivaModel.boneNameStringTable.Dispose();
            decodeVivaModel.boneHeadTable.Dispose();
            decodeVivaModel.boneTailTable.Dispose();
            decodeVivaModel.boneRollTable.Dispose();
            decodeVivaModel.boneHierarchyChildCountTable.Dispose();
            decodeVivaModel.IsPartOfBaseSkeletonTable.Dispose();
            decodeVivaModel.childBoneIndicesTable.Dispose();
            decodeVivaModel.materialNamesTable.Dispose();
            decodeVivaModel.vertexTable.Dispose();
            decodeVivaModel.normalTable.Dispose();
            decodeVivaModel.uvTable.Dispose();
            decodeVivaModel.triVertexIndicesTable.Dispose();
            decodeVivaModel.boneWeightsTable.Dispose();
            decodeVivaModel.submeshTriCountTable.Dispose();
            decodeVivaModel.shapeKeyNamesTable.Dispose();
            decodeVivaModel.shapeKeyLengthsTable.Dispose();
            decodeVivaModel.vertexDeltaIndicesTable.Dispose();
            decodeVivaModel.vertexDeltaOffsetTable.Dispose();

            request.result = request.targetLoli;
            request.targetLoli.SetHeadModel(model, mbs);
        }

        private static bool[] NativeArrayToBoolArray(NativeArray<bool> array)
        {
            var result = new bool[array.Length];
            array.CopyTo(result);
            return result;
        }
        private static float[] NativeArrayToFloatArray(NativeArray<float> array)
        {
            var result = new float[array.Length];
            array.CopyTo(result);
            return result;
        }
        private static int[] NativeArrayToIntArray(NativeArray<int> array)
        {
            var result = new int[array.Length];
            array.CopyTo(result);
            return result;
        }
        private static Vector2[] NativeArrayToVector2Array(NativeArray<Vector2> array)
        {
            var result = new Vector2[array.Length];
            array.CopyTo(result);
            return result;
        }
        private static Vector3[] NativeArrayToVector3Array(NativeArray<Vector3> array)
        {
            var result = new Vector3[array.Length];
            array.CopyTo(result);
            return result;
        }
        private static Vector4[] NativeArrayToVector4Array(NativeArray<Vector4> array)
        {
            var result = new Vector4[array.Length];
            array.CopyTo(result);
            return result;
        }
        private static BoneWeight[] NativeArrayToBoneWeightsArray(NativeArray<BoneWeight> array)
        {
            var result = new BoneWeight[array.Length];
            array.CopyTo(result);
            return result;
        }

        private static string[] NativeByteArrayToStringArray(NativeArray<byte> bytes, int stringCount)
        {
            var rawBytes = new byte[bytes.Length];
            for (int i = 0; i < bytes.Length; i++)
            {
                rawBytes[i] = bytes[i];
            }
            var bsr = new ByteStreamReader(rawBytes);
            var strings = new string[stringCount];
            for (int i = 0; i < stringCount; i++)
            {
                strings[i] = bsr.ReadUTF8String(bsr.ReadUnsigned1ByteInt());
            }
            return strings;
        }

        private static float MODEL_IMPORT_VERSION = 1.2f;
        private static float BLENDER_TO_UNITY_SCALE = 3.75f / 100.0f;

        public static readonly CardTextureSerializer.CardTextureFormat[] skinFormat = new CardTextureSerializer.CardTextureFormat[]{
        new CardTextureSerializer.CardTextureFormat( 1024, true ),
        new CardTextureSerializer.CardTextureFormat( 512, false ),
        new CardTextureSerializer.CardTextureFormat( 512, false ),
    };

        public class DynamicBoneInfo
        {

            public enum Collision
            {
                HEAD,
                BACK
            };

            public readonly string rootBone;
            public float damping = 0.1f;
            public float elasticity = 0.05f;
            public float stiffness = 0.0f;
            public bool disableEndLength = true;
            public bool useGravity = true;
            public bool canRelax = false;
            public bool headCollision = false;
            public bool collisionBack = false;

            public DynamicBoneInfo(string _rootBone)
            {
                rootBone = _rootBone;
            }

            public void CopySettings(DynamicBoneInfo copy)
            {
                damping = copy.damping;
                elasticity = copy.elasticity;
                stiffness = copy.stiffness;
                disableEndLength = copy.disableEndLength;
                useGravity = copy.useGravity;
                canRelax = copy.canRelax;
                headCollision = copy.headCollision;
                collisionBack = copy.collisionBack;
            }
        }

        public string error { get; private set; }

        public class BoneChain
        {
            public string rootName;
            public bool canRelax;
            public bool useGravity;
            public float damping;
            public float elasticity;
            public float stiffness;
            public bool headCollision;
            public bool backCollision;
        }

        public Mesh mesh { get; private set; }
        public Transform[] bonesShared { get; private set; }
        public string sourceCardFilename { get; private set; }
        public string name { get; private set; }
        protected int boneNamesBytesLength;
        protected int boneCount;
        protected int childBoneIndicesTableLength;
        protected int materialNamesBytesLength;
        protected int vertexCount;
        protected int materialCount;
        protected int triIndicesCount;
        protected int shapeKeyCount;
        protected int shapeKeyNamesByteLength;
        protected int shapeKeyVertexElements;

        public Texture2D texture = null;
        public Texture2D rightEyeTexture = null;
        public Texture2D leftEyeTexture = null;
        public float pupilSpanRadius = 0.5f;
        public Vector2 pupilOffset = Vector2.zero;
        public Color skinColor = Color.grey;
        public byte voiceIndex;
        public bool fullBodyOverride;
        public Vector4 headpatWorldSphere;
        public Vector4 hatLocalPosAndPitch;
        public Vector4 headCollisionWorldSphere;
        public readonly int hashID; //used to match textures
        public byte[] modelData = null;
        // public readonly ModelBuildSettings mis;
        public List<DynamicBoneInfo> dynamicBoneInfos = new List<DynamicBoneInfo>();
        private byte VIVA_MODEL_CARD_VERSION = 2;

        public VivaModel(string _sourceCardFilename, string _name)
        {
            sourceCardFilename = _sourceCardFilename;
            name = _name;
        }

        public bool AttemptHotswap(VivaModel model)
        {
            if (name.ToLower() != model.name.ToLower())
            {
                return false;
            }
            sourceCardFilename = model.sourceCardFilename;
            name = model.name;
            texture = model.texture;
            rightEyeTexture = model.rightEyeTexture;
            leftEyeTexture = model.leftEyeTexture;
            pupilSpanRadius = model.pupilSpanRadius;
            pupilOffset = model.pupilOffset;
            skinColor = model.skinColor;
            voiceIndex = model.voiceIndex;
            fullBodyOverride = model.fullBodyOverride;
            // headpatWorldSphere = model.headpatWorldSphere;
            // hatLocalPosAndPitch = model.hatLocalPosAndPitch;
            // headCollisionWorldSphere = model.headCollisionWorldSphere;

            int matched = 0;
            for (int i = 0; i < dynamicBoneInfos.Count; i++)
            {
                DynamicBoneInfo info = dynamicBoneInfos[i];
                for (int j = 0; j < model.dynamicBoneInfos.Count; j++)
                {
                    DynamicBoneInfo candidate = model.dynamicBoneInfos[j];
                    if (info.rootBone == candidate.rootBone)
                    {
                        info.CopySettings(candidate);
                        matched++;
                        break;
                    }
                }
            }
            Debug.Log("[VIVA MODEL] Hotswapped and matched " + matched + " bones");
            return true;
        }

        public void DestroyAll(SkinnedMeshRenderer targetSMR)
        {
            Debug.Log("[VIVA MODEL] Destroying Model");
            foreach (Material mat in targetSMR.materials)
            {
                GameDirector.Destroy(mat);
            }
            if (mesh != null)
            {
                GameDirector.Destroy(mesh);
                mesh = null;
            }
            //TODO: Handle memory cleanup
            if (texture != null)
            {
                GameDirector.Destroy(texture);
            }
            if (rightEyeTexture != null)
            {
                GameDirector.Destroy(rightEyeTexture);
                rightEyeTexture = null;
            }
            if (leftEyeTexture != null)
            {
                GameDirector.Destroy(leftEyeTexture);
                leftEyeTexture = null;
            }
        }

        private static int FindBoneIndexByName(Transform[] bones, string name)
        {
            for (int i = 0; i < bones.Length; i++)
            {
                var bone = bones[i];
                if (bone.name == name)
                {
                    return i;
                }
            }
            return -1;
        }

        private void GenerateBonesShared(Loli targetLoli, Transform[] baseBones, ModelBuildSettings mbs)
        {

            bonesShared = new Transform[mbs.boneCount];
            for (int i = 0; i < bonesShared.Length; i++)
            {

                string name = mbs.boneNameTable[i];
                Vector3 head = mbs.boneHeadTable[i];
                Vector3 tail = mbs.boneTailTable[i];
                float roll = mbs.boneRollTable[i];

                int boneIndex = FindBoneIndexByName(baseBones, name);
                Transform bone;
                if (name.StartsWith("pupil_"))
                {
                    if (name.EndsWith("r"))
                    {
                        bone = targetLoli.pupil_r;
                    }
                    else
                    {
                        bone = targetLoli.pupil_l;
                    }
                    //Pupil bones must always face the same direction
                    bone.position = head;
                    bone.eulerAngles = new Vector3(0.0f, 0.0f, 180.0f);
                }
                else if (boneIndex == -1)
                {
                    bone = new GameObject(name).transform;
                    targetLoli.outfitInstance.createdBones.Add(bone.gameObject);

                    bone.position = head;
                    bone.rotation = Quaternion.AngleAxis(roll * Mathf.Rad2Deg, (tail - head).normalized);
                }
                else
                {
                    bone = baseBones[boneIndex];
                }
                //add to boneTable
                bonesShared[i] = bone;
            }
        }

        private void GenerateBoneHierarchy(ModelBuildSettings mbs)
        {
            List<Transform> potentialDynamicBoneRoots = new List<Transform>();
            int childBoneIndicesTableIndex = 0;
            for (int i = 0; i < mbs.boneCount; i++)
            {

                Transform bone = bonesShared[i];
                int childCount = mbs.boneHierarchyChildCountTable[i];
                bool boneIsFromBase = mbs.IsPartOfBaseSkeletonTable[i];

                for (int j = 0; j < childCount; j++)
                {
                    int childIndex = mbs.childBoneIndicesTable[childBoneIndicesTableIndex++];
                    Transform child = bonesShared[childIndex];
                    bool childIsFromBase = mbs.IsPartOfBaseSkeletonTable[childIndex];
                    if (childIsFromBase)
                    {
                        //do not change hierarchy of base skeleton rig
                        continue;
                    }
                    if (boneIsFromBase && !childIsFromBase)
                    {
                        potentialDynamicBoneRoots.Add(child);
                    }
                    child.SetParent(bone, true);
                }
            }
            //initialize DynamicBoneInfos only if it hasnt been created from Deserialize()
            if (dynamicBoneInfos.Count == 0)
            {
                foreach (Transform bone in potentialDynamicBoneRoots)
                {
                    if (bone.childCount > 0)
                    {
                        dynamicBoneInfos.Add(new DynamicBoneInfo(bone.name));
                    }
                }
            }
        }

        private static Material CreateModelMaterial(ModelBuildSettings mis, string materialName)
        {
            Shader materialShader;
            if (materialName == "pupil_r" || materialName == "pupil_l")
            {
                materialShader = mis.pupil;
            }
            else
            {
                materialName = "skin";
                materialShader = mis.skin;
            }
            Material newMaterial = new Material(materialShader);
            newMaterial.name = materialName;
            return newMaterial;
        }

        private Material[] GenerateMaterials(ModelBuildSettings mbs)
        {
            Material[] materialTable = new Material[mbs.materialCount];
            for (int i = 0; i < materialTable.Length; i++)
            {
                materialTable[i] = CreateModelMaterial(mbs, mbs.materialNameTable[i]);
            }

            if (materialTable.Length == 0)
            {
                Debug.Log("[VIVA MODEL MATERIAL] No materials found! Creating default material...");
                Material newMaterial = new Material(mbs.skin);
                materialTable = new Material[] { newMaterial };
                newMaterial.name = "skin";
            }
            return materialTable;
        }

        private Mesh GenerateMesh(Transform[] baseBones, SkinnedMeshRenderer targetSMR, ModelBuildSettings mbs)
        {

            //build submeshes
            Mesh mesh = new Mesh();
            mesh.vertices = mbs.vertexTable;
            mesh.normals = mbs.normalTable;
            mesh.uv = mbs.uvTable;
            mesh.boneWeights = mbs.boneWeightsTable;
            mesh.subMeshCount = targetSMR.materials.Length;

            int submeshTriCountTableIndex = 0;
            for (int i = 0; i < targetSMR.materials.Length; i++)
            {

                int[] submeshIndices = new int[mbs.submeshTriCountTable[i]];
                System.Array.Copy(mbs.triVertexIndicesTable, submeshTriCountTableIndex, submeshIndices, 0, submeshIndices.Length);
                mesh.SetIndices(submeshIndices, MeshTopology.Triangles, i, false);
                submeshTriCountTableIndex += submeshIndices.Length;
            }
            mesh.bindposes = CreateBindPoses(bonesShared);
            ///TODO: have a default value for bounds?
            return mesh;
        }

        private Matrix4x4[] CreateBindPoses(Transform[] boneTable)
        {
            Matrix4x4[] poseTable = new Matrix4x4[boneTable.Length];
            for (int i = 0; i < boneTable.Length; i++)
            {
                Transform bone = boneTable[i];
                poseTable[i] = bone.worldToLocalMatrix;
            }
            return poseTable;
        }

        private void GenerateShapeKeys(Mesh mesh, ModelBuildSettings mbs)
        {

            int vertexDeltaIndicesIndex = 0;
            for (int i = 0; i < mbs.shapeKeyCount; i++)
            {
                string shapeKeyName = mbs.shapeKeyNamesTable[i];
                Vector3[] vertexDeltas = new Vector3[mbs.vertexCount];
                int deltas = mbs.shapeKeyLengthsTable[i];
                for (int j = 0; j < deltas; j++)
                {
                    int index = mbs.vertexDeltaIndicesTable[vertexDeltaIndicesIndex];
                    vertexDeltas[index] = mbs.vertexDeltaOffsetTable[vertexDeltaIndicesIndex];
                    vertexDeltaIndicesIndex++;
                }
                mesh.AddBlendShapeFrame(shapeKeyName, 100.0f, vertexDeltas, null, null);
            }
            //ensure pupilShrink exists
            if (!mbs.shapeKeyNamesTable.Contains("pupilShrink"))
            {
                // Debug.Log("[VIVA MODEL] Added pupilShrink blendShape");
                mesh.AddBlendShapeFrame("pupilShrink", 100.0f, new Vector3[mbs.vertexCount], null, null);
            }
        }

        public byte[] SerializeToCardData()
        {
            var bsw = new ByteStreamWriter(50000, 50000);
            bsw.WriteByte(VIVA_MODEL_CARD_VERSION);
            bsw.WriteByte((byte)Steganography.UnpackedCard.CardType.CUSTOM_CHARACTER_MODEL);
            bsw.WriteNormal1ByteFloat(pupilSpanRadius);
            bsw.WriteNormal1ByteFloat(pupilOffset.x);
            bsw.WriteNormal1ByteFloat(pupilOffset.y);
            bsw.WriteUnsignedNormal1ByteFloat(skinColor.r);
            bsw.WriteUnsignedNormal1ByteFloat(skinColor.g);
            bsw.WriteUnsignedNormal1ByteFloat(skinColor.b);
            bsw.WriteByte(voiceIndex);
            bsw.WriteByte(System.Convert.ToByte(fullBodyOverride));
            bsw.Write4ByteFloat(headpatWorldSphere.x);
            bsw.Write4ByteFloat(headpatWorldSphere.y);
            bsw.Write4ByteFloat(headpatWorldSphere.z);
            bsw.Write4ByteFloat(headpatWorldSphere.w);
            bsw.Write4ByteFloat(hatLocalPosAndPitch.x);
            bsw.Write4ByteFloat(hatLocalPosAndPitch.y);
            bsw.Write4ByteFloat(hatLocalPosAndPitch.z);
            bsw.Write4ByteFloat(hatLocalPosAndPitch.w);
            bsw.Write4ByteFloat(headCollisionWorldSphere.x);
            bsw.Write4ByteFloat(headCollisionWorldSphere.y);
            bsw.Write4ByteFloat(headCollisionWorldSphere.z);
            bsw.Write4ByteFloat(headCollisionWorldSphere.w);
            bsw.WriteUTF8String(name);
            bsw.WriteByteArray(modelData);
            bsw.WriteByte((byte)dynamicBoneInfos.Count);
            foreach (DynamicBoneInfo info in dynamicBoneInfos)
            {
                bsw.WriteUTF8String(info.rootBone);
                bsw.WriteUnsignedNormal1ByteFloat(info.damping);
                bsw.WriteUnsignedNormal1ByteFloat(info.elasticity);
                bsw.WriteUnsignedNormal1ByteFloat(info.stiffness);
                bsw.WriteByte(System.Convert.ToByte(info.disableEndLength));
                bsw.WriteByte(System.Convert.ToByte(info.useGravity));
                bsw.WriteByte(System.Convert.ToByte(info.canRelax));
                bsw.WriteByte(System.Convert.ToByte(info.headCollision));
                bsw.WriteByte(System.Convert.ToByte(info.collisionBack));
            }
            return bsw.ToArray();
        }

        private void DeserializeFromCardData(byte[] cardData)
        {
            error = null;

            var bsr = new ByteStreamReader(cardData);
            byte version = bsr.ReadUnsigned1ByteInt();
            if (version < VIVA_MODEL_CARD_VERSION)
            {
                error = "Outdated card version";
                return;
            }
            byte cardType = bsr.ReadUnsigned1ByteInt();
            if (cardType != (byte)Steganography.UnpackedCard.CardType.CUSTOM_CHARACTER_MODEL)
            {
                error = "Data is not a character card";
                return;
            }
            pupilSpanRadius = bsr.Read1ByteNormalFloat();
            pupilOffset.x = bsr.Read1ByteNormalFloat();
            pupilOffset.y = bsr.Read1ByteNormalFloat();
            skinColor.r = bsr.ReadUnsigned1ByteNormalFloat();
            skinColor.g = bsr.ReadUnsigned1ByteNormalFloat();
            skinColor.b = bsr.ReadUnsigned1ByteNormalFloat();
            voiceIndex = bsr.ReadUnsigned1ByteInt();
            fullBodyOverride = System.Convert.ToBoolean(bsr.ReadUnsigned1ByteInt());
            headpatWorldSphere.x = bsr.ReadFloat();
            headpatWorldSphere.y = bsr.ReadFloat();
            headpatWorldSphere.z = bsr.ReadFloat();
            headpatWorldSphere.w = bsr.ReadFloat();
            hatLocalPosAndPitch.x = bsr.ReadFloat();
            hatLocalPosAndPitch.y = bsr.ReadFloat();
            hatLocalPosAndPitch.z = bsr.ReadFloat();
            hatLocalPosAndPitch.w = bsr.ReadFloat();
            headCollisionWorldSphere.x = bsr.ReadFloat();
            headCollisionWorldSphere.y = bsr.ReadFloat();
            headCollisionWorldSphere.z = bsr.ReadFloat();
            headCollisionWorldSphere.w = bsr.ReadFloat();
            name = bsr.ReadUTF8String(bsr.ReadUnsigned1ByteInt());
            modelData = bsr.ReadBytes(bsr.ReadSigned4ByteInt());
            int dynamicBoneInfoCount = bsr.ReadUnsigned1ByteInt();
            dynamicBoneInfos = new List<DynamicBoneInfo>(dynamicBoneInfoCount);
            for (int i = 0; i < dynamicBoneInfoCount; i++)
            {
                var info = new DynamicBoneInfo(bsr.ReadUTF8String(bsr.ReadUnsigned1ByteInt()));
                info.damping = bsr.ReadUnsigned1ByteNormalFloat();
                info.elasticity = bsr.ReadUnsigned1ByteNormalFloat();
                info.stiffness = bsr.ReadUnsigned1ByteNormalFloat();
                info.disableEndLength = System.Convert.ToBoolean(bsr.ReadUnsigned1ByteInt());
                info.useGravity = System.Convert.ToBoolean(bsr.ReadUnsigned1ByteInt());
                info.canRelax = System.Convert.ToBoolean(bsr.ReadUnsigned1ByteInt());
                info.headCollision = System.Convert.ToBoolean(bsr.ReadUnsigned1ByteInt());
                info.collisionBack = System.Convert.ToBoolean(bsr.ReadUnsigned1ByteInt());
                dynamicBoneInfos.Add(info);
            }
        }

        public void Build(Loli targetLoli, ModelBuildSettings mbs)
        {
            error = null;
            if (targetLoli == null)
            {
                Debug.LogError("[VivaModel] Target Loli cannot be null!");
                return;
            }

            if (mbs == null)
            {
                Debug.LogError("[VivaModel] MBS is null!");
                return;
            }

            if (targetLoli.sessionReferenceName != null)
            {
                var words = targetLoli.sessionReferenceName.Split('_');
                targetLoli.gameObject.name = name + "_" + words[words.Length - 1];
            }
            targetLoli.outfitInstance.DestroyAllCreatedBones();

            Transform bodyArmature = targetLoli.bodyArmature;
            Transform[] baseBones = targetLoli.bodySMRs[0].bones;
            SkinnedMeshRenderer destSMR = targetLoli.headSMR;

            //zero out variables
            Vector3 cachedArmaturePos = bodyArmature.position;
            Quaternion cachedArmatureRot = bodyArmature.rotation;
            bodyArmature.position = Vector3.zero;
            bodyArmature.rotation = Quaternion.identity;

            //reset bone to base to sync model export
            for (int i = 0; i < baseBones.Length; i++)
            {
                Transform bone = baseBones[i];
                bone.localPosition = GameDirector.instance.loliBasePose.positions[i];
                bone.localRotation = GameDirector.instance.loliBasePose.quaternions[i];
            }

            //try to load the textures automatically
            //model textures can be manually set later if they fail now
            GenerateBonesShared(targetLoli, baseBones, mbs);
            GenerateBoneHierarchy(mbs);
            Material[] materials = GenerateMaterials(mbs);
            destSMR.materials = materials;
            //rename and remove (Instance) suffixes
            foreach (Material mat in destSMR.materials)
            {
                mat.name = mat.name.Replace(" (Instance)", "");
            }
            mesh = GenerateMesh(baseBones, destSMR, mbs);
            GenerateShapeKeys(mesh, mbs);
            ApplyHeadModelTextures(destSMR);

            hatLocalPosAndPitch = mbs.hatLocalPosAndPitch;
            headpatWorldSphere = mbs.headpatWorldSphere;
            headCollisionWorldSphere = mbs.headCollisionWorldSphere;

            mesh.name = name;
            destSMR.bones = bonesShared;
            destSMR.sharedMesh = mesh;

            bodyArmature.position = cachedArmaturePos;
            bodyArmature.rotation = cachedArmatureRot;
        }

        public void ApplyHeadModelTextures(SkinnedMeshRenderer destSMR)
        {

            //assign textures from modelTextures
            foreach (Material material in destSMR.materials)
            {
                if (material.name.StartsWith("skin"))
                {
                    material.mainTexture = texture;
                }
                else if (material.name.StartsWith("pupil_r"))
                {
                    material.mainTexture = rightEyeTexture;
                }
                else if (material.name.StartsWith("pupil_l"))
                {
                    material.mainTexture = leftEyeTexture;
                }
            }
        }
    }

}