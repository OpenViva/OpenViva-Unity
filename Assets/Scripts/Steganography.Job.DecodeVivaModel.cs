using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;
using Unity.Jobs;



namespace viva{

public partial class Steganography : MonoBehaviour {

    public static NativeArray<byte> ConvertToNativeArray( byte[] byteArray, Allocator allocator=Allocator.Persistent ){
        NativeArray<byte> nativeByteArray = new NativeArray<byte>(byteArray.Length, allocator, NativeArrayOptions.UninitializedMemory);
        nativeByteArray.CopyFrom( byteArray );
        return nativeByteArray;
    }

    public struct DecodeVivaModelJob : IJob {
        
        public static readonly string[] skeletonBones = new string[]{
            "head",
            "neck",
            "shoulder_r",
            "spine3",
            "spine2",
            "spine1",
            "hip_r",
            "glute_r",
            "upperThigh_r",
            "thigh_r",
            "leg_r",
            "kneecap_r",
            "foot_r",
            "foot_end_r",
            "upperArm_r",
            "bicep_r",
            "arm_r",
            "wrist_r",
            "hand_r",
            "toePinky_r",
            "toeRing1_r",
            "toeRing2_r",
            "toeMiddle1_r",
            "toeMiddle2_r",
            "toeIndex1_r",
            "toeIndex2_r",
            "toeBig1_r",
            "toeBig2_r",
            "thumb1_r",
            "thumb2_r",
            "thumb3_r",
            "index1_r",
            "index2_r",
            "index3_r",
            "middle1_r",
            "middle2_r",
            "middle3_r",
            "ring1_r",
            "ring2_r",
            "ring3_r",
            "pinky1_r",
            "pinky2_r",
            "pinky3_r",
            "handTarget_r",
            "shoulder_l",
            "hip_l",
            "glute_l",
            "upperThigh_l",
            "thigh_l",
            "leg_l",
            "kneecap_l",
            "foot_l",
            "foot_end_l",
            "upperArm_l",
            "bicep_l",
            "arm_l",
            "wrist_l",
            "hand_l",
            "toePinky_l",
            "toeRing1_l",
            "toeRing2_l",
            "toeMiddle1_l",
            "toeMiddle2_l",
            "toeIndex1_l",
            "toeIndex2_l",
            "toeBig1_l",
            "toeBig2_l",
            "thumb1_l",
            "thumb2_l",
            "thumb3_l",
            "index1_l",
            "index2_l",
            "index3_l",
            "middle1_l",
            "middle2_l",
            "middle3_l",
            "ring1_l",
            "ring2_l",
            "ring3_l",
            "pinky1_l",
            "pinky2_l",
            "pinky3_l",
            "handTarget_l",

            "eyeball_r",
            "eyeball_l",
            //used for live animation and calibration
            // "pupil_r",
            // "pupil_l",
        };
        
        private static float BLENDER_TO_UNITY_SCALE = 3.75f/100.0f;
        
        public readonly NativeArray<byte> nativeTargetByteArray;
        private int readOffset;
        private int boneCount;
        public int shapeKeyCount;
        public NativeArray<Vector4> sphereInfos;
        public NativeArray<byte> boneNameStringTable;
        public NativeArray<Vector3> boneHeadTable;
        public NativeArray<Vector3> boneTailTable;
        public NativeArray<float> boneRollTable;
        public NativeArray<bool> IsPartOfBaseSkeletonTable;
        public NativeArray<int> boneHierarchyChildCountTable;
        public NativeArray<int> childBoneIndicesTable;
        public NativeArray<byte> materialNamesTable;
        public NativeArray<Vector3> vertexTable;
        public NativeArray<Vector3> normalTable;
        public NativeArray<Vector2> uvTable;
        public NativeArray<int> triVertexIndicesTable;
        public NativeArray<BoneWeight> boneWeightsTable;
        public NativeArray<int> submeshTriCountTable;
        public NativeArray<byte> shapeKeyNamesTable;
        public NativeArray<int> shapeKeyLengthsTable;
        public NativeArray<int> vertexDeltaIndicesTable;
        public NativeArray<Vector3> vertexDeltaOffsetTable;

        
        public DecodeVivaModelJob(
                int readOffset,
                NativeArray<byte> _nativeTargetByteArray,
                int boneNamesBytesLength,
                int boneCount,
                int childBoneIndicesTableLength,
                int materialNamesBytesLength,
                int vertexCount,
                int materialCount,
                int triIndicesCount,
                int shapeKeyCount,
                int shapeKeyNamesByteLength,
                int shapeKeyVertexElements ){
            nativeTargetByteArray = _nativeTargetByteArray;
            this.readOffset = readOffset;
            this.boneCount = boneCount;
            this.shapeKeyCount = shapeKeyCount;
            sphereInfos = new NativeArray<Vector4>( 3, Allocator.TempJob, NativeArrayOptions.UninitializedMemory );

            boneNameStringTable = new NativeArray<byte>( boneNamesBytesLength, Allocator.TempJob, NativeArrayOptions.UninitializedMemory );
            boneHeadTable = new NativeArray<Vector3>( boneCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory );
            boneTailTable = new NativeArray<Vector3>( boneCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory );
            boneRollTable = new NativeArray<float>( boneCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory );

            boneHierarchyChildCountTable = new NativeArray<int>( boneCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory );
            IsPartOfBaseSkeletonTable = new NativeArray<bool>( boneCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory );
            childBoneIndicesTable = new NativeArray<int>( childBoneIndicesTableLength, Allocator.TempJob, NativeArrayOptions.UninitializedMemory );

            materialNamesTable = new NativeArray<byte>( materialNamesBytesLength, Allocator.TempJob, NativeArrayOptions.UninitializedMemory );
            vertexTable = new NativeArray<Vector3>( vertexCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory );
            normalTable = new NativeArray<Vector3>( vertexCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory );
            uvTable = new NativeArray<Vector2>( vertexCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory );
            triVertexIndicesTable = new NativeArray<int>( triIndicesCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory );

            boneWeightsTable = new NativeArray<BoneWeight>( vertexCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory );
            submeshTriCountTable = new NativeArray<int>( materialCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory );

            shapeKeyNamesTable = new NativeArray<byte>( shapeKeyNamesByteLength, Allocator.TempJob, NativeArrayOptions.UninitializedMemory );
            shapeKeyLengthsTable = new NativeArray<int>( shapeKeyCount, Allocator.TempJob, NativeArrayOptions.UninitializedMemory );
            vertexDeltaIndicesTable = new NativeArray<int>( shapeKeyVertexElements, Allocator.TempJob, NativeArrayOptions.UninitializedMemory );
            vertexDeltaOffsetTable = new NativeArray<Vector3>( shapeKeyVertexElements, Allocator.TempJob, NativeArrayOptions.UninitializedMemory );
        }

        private static bool IsPartOfBaseSkeleton( string boneName ){
            foreach( string baseBoneName in skeletonBones ){
                if( baseBoneName == boneName ){
                    return true;
                }
            }
            return false;
        }

        private void ReadBoneTable( ByteStreamReader bsr ){
            int boneNameStringTableIndex = 0;
            for( byte i=0; i<boneCount; i++ ){
                byte nameLength = bsr.ReadUnsigned1ByteInt();
                boneNameStringTable[ boneNameStringTableIndex++ ] = nameLength;
                var nameBytes = bsr.ReadBytes( nameLength );
                for( int j=0; j<nameBytes.Length; j++ ){
                    boneNameStringTable[ boneNameStringTableIndex++ ] = nameBytes[j];
                }

                boneHeadTable[i] = new Vector3( -bsr.ReadFloat(), bsr.ReadFloat(), bsr.ReadFloat() )*BLENDER_TO_UNITY_SCALE;
                boneTailTable[i] = new Vector3( -bsr.ReadFloat(), bsr.ReadFloat(), bsr.ReadFloat() )*BLENDER_TO_UNITY_SCALE;
                boneRollTable[i] = bsr.ReadFloat();
                IsPartOfBaseSkeletonTable[i] = IsPartOfBaseSkeleton( Tools.UTF8ByteArrayToString( nameBytes, 0, nameBytes.Length ) );
            }
        }

        private void ReadBoneHierarchy( ByteStreamReader bsr ){
            int childBoneIndicesTableIndex = 0;
            for( int i=0; i<boneCount; i++ ){
                byte childBoneIndicesCount = bsr.ReadUnsigned1ByteInt();
                boneHierarchyChildCountTable[i] = childBoneIndicesCount;
                for( byte j=0; j<childBoneIndicesCount; j++ ){
                    childBoneIndicesTable[ childBoneIndicesTableIndex++ ] = bsr.ReadUnsigned1ByteInt();
                }
            }
        }
        
        private void ReadMaterialTable( ByteStreamReader bsr ){
            int materialCount = submeshTriCountTable.Length;
            int materialNamesTableIndex = 0;
            for( byte i=0; i<materialCount; i++ ){
                byte nameLength = bsr.ReadUnsigned1ByteInt();
                var name = bsr.ReadBytes( nameLength );
                
                materialNamesTable[ materialNamesTableIndex++ ] = nameLength;
                for( int j=0; j<nameLength; j++ ){
                    materialNamesTable[ materialNamesTableIndex++ ] = name[j];
                }
            }
        }

        private void ReadMesh( ByteStreamReader bsr ){
            int vertexCount = vertexTable.Length;
            for( int i=0; i<vertexCount; i++ ){
                vertexTable[i] = new Vector3( -bsr.ReadFloat(), bsr.ReadFloat(), bsr.ReadFloat() )*BLENDER_TO_UNITY_SCALE;
                normalTable[i] = new Vector3( -bsr.Read1ByteNormalFloat(), bsr.Read1ByteNormalFloat(), bsr.Read1ByteNormalFloat() );
                
                int weightCount = bsr.ReadUnsigned1ByteInt();
                BoneWeight boneWeight = new BoneWeight();
                if( weightCount > 0 ){
                    boneWeight.boneIndex0 = bsr.ReadUnsigned1ByteInt();
                    boneWeight.weight0 = bsr.ReadUnsigned1ByteNormalFloat();
                }
                if( weightCount > 1 ){
                    boneWeight.boneIndex1 = bsr.ReadUnsigned1ByteInt();
                    boneWeight.weight1 = bsr.ReadUnsigned1ByteNormalFloat();
                }
                if( weightCount > 2 ){
                    boneWeight.boneIndex2 = bsr.ReadUnsigned1ByteInt();
                    boneWeight.weight2 = bsr.ReadUnsigned1ByteNormalFloat();
                }
                if( weightCount > 3 ){
                    boneWeight.boneIndex3 = bsr.ReadUnsigned1ByteInt();
                    boneWeight.weight3 = bsr.ReadUnsigned1ByteNormalFloat();
                }
                //normalize
                float sum = boneWeight.weight0+boneWeight.weight1+boneWeight.weight2+boneWeight.weight3;
                if( sum > 0.0f ){   //prevent divison by zero
                    boneWeight.weight0 = boneWeight.weight0/sum;
                    boneWeight.weight1 = boneWeight.weight1/sum;
                    boneWeight.weight2 = boneWeight.weight2/sum;
                    boneWeight.weight3 = boneWeight.weight3/sum;
                }
                boneWeightsTable[i] = boneWeight;
            }
            
            //build submeshes
            int triVertexIndicesTableIndex = 0;
            for( int i=0; i<submeshTriCountTable.Length; i++ ){
                int triangleCount = bsr.ReadUnsigned2ByteInt();
                submeshTriCountTable[i] = triangleCount*3;

                //build triangle faces in reverse
                int counter = triVertexIndicesTableIndex+triangleCount*3;
                for( int tri=0; tri<triangleCount; tri++ ){
                    for( int j=0; j<3; j++ ){
                        int vertexIndex = bsr.ReadUnsigned2ByteInt();
                        triVertexIndicesTable[ --counter ] = vertexIndex;
                        uvTable[ vertexIndex ] = new Vector2( bsr.ReadFloat(), bsr.ReadFloat() );
                    }
                }
                triVertexIndicesTableIndex += triangleCount*3;
            }
        }
        
        private void ReadShapeKeys( ByteStreamReader bsr ){
            
            int vertexCount = vertexTable.Length;
            int shapeKeyNamesIndex = 0;
            int vertexDeltaIndicesIndex = 0;
            int vertexDeltaOffsetIndex = 0;
            for( int i=0; i<shapeKeyCount; i++ ){
                
                var shapeKeyName = bsr.ReadBytes( bsr.ReadUnsigned1ByteInt() );
                shapeKeyNamesTable[ shapeKeyNamesIndex++ ] = (byte)shapeKeyName.Length;
                for( int j=0; j<shapeKeyName.Length; j++ ){
                    shapeKeyNamesTable[ shapeKeyNamesIndex++ ] = shapeKeyName[j];
                }

                Vector3[] vertexDeltas = new Vector3[ vertexCount ];
                int deltas = bsr.ReadUnsigned2ByteInt();
                shapeKeyLengthsTable[i] = deltas;
                for( int j=0; j<deltas; j++ ){
                    vertexDeltaIndicesTable[ vertexDeltaIndicesIndex++ ] = bsr.ReadUnsigned2ByteInt();

                    Vector3 delta = new Vector3( -bsr.ReadFloat(), bsr.ReadFloat(), bsr.ReadFloat() )*BLENDER_TO_UNITY_SCALE;
                    vertexDeltaOffsetTable[ vertexDeltaOffsetIndex++ ] = delta;
                }
            }
        }
        
        private void ReadInfo( ByteStreamReader bsr ){
            int infoSphereCount = bsr.ReadUnsigned1ByteInt();
            for( int i=0; i<infoSphereCount; i++ ){
                string infoName = bsr.ReadUTF8String( bsr.ReadUnsigned1ByteInt() );
                infoName = infoName.Replace("_","");
                Vector4 info = new Vector4( bsr.ReadFloat(), bsr.ReadFloat(), bsr.ReadFloat(), bsr.ReadFloat() );
                if( infoName == "headpat" ){
                    sphereInfos[0] = info*BLENDER_TO_UNITY_SCALE;
                }else if( infoName == "head collision" ){
                    sphereInfos[2] = info*BLENDER_TO_UNITY_SCALE;
                }else if( infoName == "hat" ){
                    Vector4 hatPos = new Vector4();
                    hatPos.x = -info.x*BLENDER_TO_UNITY_SCALE;
                    hatPos.y = info.y*BLENDER_TO_UNITY_SCALE;
                    hatPos.z = info.z*BLENDER_TO_UNITY_SCALE;
                    hatPos.w = info.w;
                    sphereInfos[1] = hatPos;
                }else{
                    // Debug.Log("[VIVA MODEL] Unrecognized info sphere: "+infoName);
                }
            }
        }


        public void Execute() {
            
            byte[] rawData = new byte[ nativeTargetByteArray.Length ];
            for( int i=0; i<rawData.Length; i++ ){
                rawData[i] = nativeTargetByteArray[i];
            }
            ByteStreamReader bsr = new ByteStreamReader( rawData );
            bsr.index = readOffset;
            ReadBoneTable( bsr );
            ReadBoneHierarchy( bsr );
            ReadMaterialTable( bsr );
            ReadMesh( bsr );
            ReadShapeKeys( bsr );
            ReadInfo( bsr );
        }
    }
    
}


}