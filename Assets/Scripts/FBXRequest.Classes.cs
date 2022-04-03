using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Unity.Jobs;
using Unity.Collections;
using Fbx;


namespace viva{


public partial class FBXRequest: SpawnableImportRequest{

    public enum Read : byte{
        BONE_INFO,
        ARMATURE_INFO,
        BONE_MODEL,
        ARMATURE_MODEL,
        SHAPE,
        SHAPE_CHANNEL,
        SHAPEKEY,
        GEOMETRY,
        MODEL,
        COLLIDER,
        MATERIAL,
        ANIMATION_CURVE,
        ANIMATION_CHANNEL_INFO,
        ANIMATION_INFO,
        ANIMATION_STACK,
        WORLD,
    }

    public enum MeshType:byte{
        SKINNED,
        STATIC,
        CLOTH
    }

    public enum ColliderType:byte{
        NONE,
        CAPSULE,
        CONCAVE,
        CONVEX,
        BOX,
        SPHERE
    }

    public enum PhysicsType:byte{
        RIGID,
        GRAB,
        CLOTH,
        ZONE,
        TRIGGER,
        JIGGLE,
    }
    public struct DynamicBoneInfo{
        public float stiffness;
        public float damping;
        public float elasticity;
        public float radius;
        public float gravity;
        public string collider;
    }

    private partial struct ExecuteReadFbx : IJob {

        private class Connection{
            public readonly int importance;
            public readonly long id0;
            public readonly long id1;

            public Connection( int _importance, long _id0, long _id1 ){
                importance = _importance;
                id0 = _id0;
                id1 = _id1;
            }
        }

        public class Shape{
            public string name;
            public List<int> indices;
            public List<float> vertexFloats;
        }

        private class ShapeKeyChannel{
            public string name;
            public Shape shape;
        }

        private class ShapeKey{
            public string name;
            public List<ShapeKeyChannel> shapeKeyChannels = new List<ShapeKeyChannel>(); 
        }

        public class AnimationCurveInfo{
            public string name;
            public long[] times;
            public float[] values;
            public float[] bezierInfo;
            public int bind;
        }

        private class AnimationCurveResample{
            public readonly float[] values;
            public readonly bool[] linear;
            public bool hasDelta = false;

            public AnimationCurveResample( int length ){
                values = new float[ length ];
                linear = new bool[ length ];
            }
        }

        public class AnimationChannelInfo{
            public string name;
            public List<AnimationCurveInfo> curves = new List<AnimationCurveInfo>();
            public AnimationChannel.Channel channel;
            public string targetName;
            public bool applyRootOffset;
        }

        public class AnimationInfo{
            public string name;
            public int parentArmatureindex = -1;
            public List<AnimationChannelInfo> channelInfos = new List<AnimationChannelInfo>();
        }

        private class AnimationStack{
            public string name;
            public List<AnimationInfo> animations = new List<AnimationInfo>();
            public long animLength = -1;
        }

        public class WeightInfo{
            public int boneIndex;
            public int vertexIndex;
            public float weight;

            public WeightInfo( int _boneIndex, int _vertexIndex, float _weights ){
                boneIndex = _boneIndex;
                vertexIndex = _vertexIndex;
                weight = _weights;
            }
        }

        public class ModelInfo{
            public string name;
            public Vector3 position = Vector3.zero;
            public Vector3 euler = Vector3.zero;
            public Vector3 scale = Vector3.one*blenderToUnityScale;
            public ModelInfo parent = null;
            public List<ColliderInfo> colliderInfos = new List<ColliderInfo>();
            public bool colliderSource = false;
            public short id;
            public DynamicBoneInfo dynamicBoneInfo;
            public MuscleTemplate muscleTemplate;

            public static short idCounter = 0;

            public ModelInfo(){
                id = idCounter++;
            }
        }

        public class BoneInfo{
            public string name;
            public Vector3 bindPosition;
            public Quaternion bindRotation;
            public Vector3 bindScale;
            public long boneID;
            public List<int> boneIndices = null;
            public List<float> boneWeights = null;
            public double[] transformLink = null;
            public List<BoneInfo> children = new List<BoneInfo>();
            public ModelInfo boneModel;
            public List<ColliderInfo> colliderInfos = new List<ColliderInfo>();
        }

        public class ArmatureInfo{
            public string name;
            public List<BoneInfo> bones = new List<BoneInfo>();
            public ModelInfo root = null;
            public List<AnimationInfo> animations = new List<AnimationInfo>();
        }

        public class ClothInfo{
            public int[] clothPinIndices = null;
            public double[] clothPinColors = null;
            public float[] clothPin = null;
            public float stretching = 1f;
            public float bending = 0.4f;
            public float damping = 0.3f;
        }

        public class Geometry{
            public string name;
            public List<float> vertexFloats = null;
            public int[] polyIndices = null;
            public int[] matPolyIndices = null;
            public double[] uvDoubles = null;
            public int[] uvIndices = null;
            public double[] normalDoubles = null;
            public ClothInfo clothInfo = null;
            public string normalType = null;
            public List<float> normalFloats = null;
            public List<float> uvFloats = null;
            public string materialType = null;
            public ArmatureInfo armature = null;
            public ModelInfo model = null;
            public List<MaterialInfo> materials = new List<MaterialInfo>();
            public List<Shape> shapeKeys = new List<Shape>();
            public ColliderInfo collider = null;
            public List<WeightInfo> meshWeights = new List<WeightInfo>();
        }

        public abstract class ColliderInfo{
            public readonly ColliderType type;
            public readonly PhysicsType physicsType;
            public ModelInfo model = null;

            public ColliderInfo( ColliderType _type, PhysicsType _physicsType ){
                type = _type;
                physicsType = _physicsType;
            }
            
            public abstract void WriteColliderInfo( NativeArray<byte> result, ref int index );
            public abstract void Build( Geometry geometry );
        }

        public class CapsuleColliderInfo:CylinderColliderInfo{

            public CapsuleColliderInfo( PhysicsType _physicsType ):base(_physicsType){
            }
        }

        public class CylinderColliderInfo:ColliderInfo{
            public int dir;
            public float radius = 0.1337f;
            public float height = 0.1337f;

            public CylinderColliderInfo( PhysicsType _physicsType ):base(ColliderType.CAPSULE,_physicsType){
            }

            public override void WriteColliderInfo( NativeArray<byte> result, ref int index ){
                BufferUtil.WriteByte( result, ref index, (byte)dir );
                BufferUtil.WriteFloat( result, ref index, radius );
                BufferUtil.WriteFloat( result, ref index, height );
            }
            public override void Build( Geometry geometry ){
                
                Vector3 axisCounter = Vector3.zero;
                int indexCounter = 0;
                int first = geometry.polyIndices[ indexCounter++ ];
                int second = geometry.polyIndices[ indexCounter++ ];
                bool polyEnd = false;
                while( indexCounter < geometry.polyIndices.Length ){
                    
                    int next = geometry.polyIndices[ indexCounter++ ];
                    if( next < 0 ){
                        next = -next-1;
                        polyEnd = true;
                    }
                    Vector3 a = new Vector3(
                        geometry.vertexFloats[ first*3 ],
                        geometry.vertexFloats[ first*3+1 ],
                        geometry.vertexFloats[ first*3+2 ]
                    );
                    Vector3 b = new Vector3(
                        geometry.vertexFloats[ next*3 ],
                        geometry.vertexFloats[ next*3+1 ],
                        geometry.vertexFloats[ next*3+2 ]
                    );
                    Vector3 c = new Vector3(
                        geometry.vertexFloats[ second*3 ],
                        geometry.vertexFloats[ second*3+1 ],
                        geometry.vertexFloats[ second*3+2 ]
                    );
                    second = next;
                    var norm = Vector3.Cross( a-b, b-c );
                    norm.x = Mathf.Abs( norm.x );
                    norm.y = Mathf.Abs( norm.y );
                    norm.z = Mathf.Abs( norm.z );
                    int zeroes = 0;
                    zeroes += System.Convert.ToInt32( norm.x <= 10e-8 );
                    zeroes += System.Convert.ToInt32( norm.y <= 10e-8 );
                    zeroes += System.Convert.ToInt32( norm.z <= 10e-8 );
                    if( zeroes == 2 ){
                        axisCounter += norm;
                    }
                    
                    if( polyEnd ){
                        if( indexCounter >= geometry.polyIndices.Length ){
                            break;
                        }
                        first = geometry.polyIndices[ indexCounter++ ];
                        second = geometry.polyIndices[ indexCounter++ ];
                        polyEnd = false;
                    }
                }
                float largest = Mathf.Max( axisCounter.x, Mathf.Max( axisCounter.y, axisCounter.z ) );
                if( largest == 0 ){
                    Debug.LogError("Could not find largest axis for "+geometry.name+" ["+axisCounter.x+","+axisCounter.y+","+axisCounter.z+"]");
                    return;
                }
                
                Vector3 vert = new Vector3(
                    geometry.vertexFloats[0],
                    geometry.vertexFloats[1],
                    geometry.vertexFloats[2]
                );
                if( largest == axisCounter.x ){
                    dir = 0;
                }else if( largest == axisCounter.y ){
                    dir = 1;
                }else{
                    dir = 2;
                }
                var flipScale = geometry.model.scale;
                flipScale.y = geometry.model.scale.z;
                flipScale.z = geometry.model.scale.y;

                height = Mathf.Abs( vert[dir] )*2*flipScale[dir];
                vert[dir] = 0;
                flipScale[dir] = 0;
                radius = vert.magnitude*Mathf.Max( flipScale.x, Mathf.Max( flipScale.y, flipScale.z ) );
                geometry.model.scale = Vector3.one;
            }
        }
        

        public class PolygonColliderInfo:ColliderInfo{
            public List<float> vertexFloats;
            public List<int> submesh = new List<int>();

            public PolygonColliderInfo( ColliderType _type, PhysicsType _physicsType ):base(_type,_physicsType){
            }

            public override void WriteColliderInfo( NativeArray<byte> result, ref int index ){
                BufferUtil.WriteFloatList( result, ref index,  vertexFloats );
                BufferUtil.WriteIntList( result, ref index,  submesh );
            }
            public override void Build( Geometry geometry ){
                vertexFloats = geometry.vertexFloats;

                int indexCounter = 0;
                UnwrapSubmesh( geometry.polyIndices, submesh, ref indexCounter, false );
            }
        }

        public class SphereColliderInfo:ColliderInfo{
            public Vector3 center;
            public float radius;

            public SphereColliderInfo( PhysicsType _physicsType ):base(ColliderType.SPHERE,_physicsType){
            }

            public override void WriteColliderInfo( NativeArray<byte> result, ref int index ){
                BufferUtil.WriteFloat( result, ref index, center.x );
                BufferUtil.WriteFloat( result, ref index, center.y );
                BufferUtil.WriteFloat( result, ref index, center.z );
                BufferUtil.WriteFloat( result, ref index, radius );
            }
            public override void Build( Geometry geometry ){
                float minX = System.Single.MaxValue;
                float maxX = System.Single.MinValue;
                float minY = System.Single.MaxValue;
                float maxY = System.Single.MinValue;
                float minZ = System.Single.MaxValue;
                float maxZ = System.Single.MinValue;
                for( int vertIndex=0; vertIndex<geometry.vertexFloats.Count/3; vertIndex++ ){
                    Vector3 vert = new Vector3(
                        geometry.vertexFloats[ vertIndex*3 ],
                        geometry.vertexFloats[ vertIndex*3+1 ],
                        geometry.vertexFloats[ vertIndex*3+2 ]
                    );
                    minX = Mathf.Min( minX, vert.x );
                    maxX = Mathf.Max( maxX, vert.x );
                    minY = Mathf.Min( minY, vert.y );
                    maxY = Mathf.Max( maxY, vert.y );
                    minZ = Mathf.Min( minZ, vert.z );
                    maxZ = Mathf.Max( maxZ, vert.z );
                }
                center.x = (minX+maxX)/2.0f;
                center.y = (minY+maxY)/2.0f;
                center.z = (minZ+maxZ)/2.0f;

                var size = Vector3.zero;
                size.x = maxX-center.x;
                size.y = maxY-center.y;
                size.z = maxZ-center.z;

                radius = Mathf.Max( size.x, Mathf.Max( size.y, size.z ) );
            }
        }

        public class BoxColliderInfo:ColliderInfo{
            public Vector3 center;
            public Vector3 size;

            public BoxColliderInfo( PhysicsType _physicsType ):base(ColliderType.BOX,_physicsType){
            }

            public override void WriteColliderInfo( NativeArray<byte> result, ref int index ){
                BufferUtil.WriteFloat( result, ref index, center.x );
                BufferUtil.WriteFloat( result, ref index, center.y );
                BufferUtil.WriteFloat( result, ref index, center.z );
                BufferUtil.WriteFloat( result, ref index, size.x );
                BufferUtil.WriteFloat( result, ref index, size.y );
                BufferUtil.WriteFloat( result, ref index, size.z );
            }
            public override void Build( Geometry geometry ){
                float minX = System.Single.MaxValue;
                float maxX = System.Single.MinValue;
                float minY = System.Single.MaxValue;
                float maxY = System.Single.MinValue;
                float minZ = System.Single.MaxValue;
                float maxZ = System.Single.MinValue;
                for( int vertIndex=0; vertIndex<geometry.vertexFloats.Count/3; vertIndex++ ){
                    Vector3 vert = new Vector3(
                        geometry.vertexFloats[ vertIndex*3 ],
                        geometry.vertexFloats[ vertIndex*3+1 ],
                        geometry.vertexFloats[ vertIndex*3+2 ]
                    );
                    minX = Mathf.Min( minX, vert.x );
                    maxX = Mathf.Max( maxX, vert.x );
                    minY = Mathf.Min( minY, vert.y );
                    maxY = Mathf.Max( maxY, vert.y );
                    minZ = Mathf.Min( minZ, vert.z );
                    maxZ = Mathf.Max( maxZ, vert.z );
                }
                center.x = (minX+maxX)/2.0f;
                center.y = (minY+maxY)/2.0f;
                center.z = (minZ+maxZ)/2.0f;

                size.x = maxX-center.x;
                size.y = maxY-center.y;
                size.z = maxZ-center.z;
                size *= 2;
            }
        }

        public class MaterialInfo{
            public string name;
        }

        public class Element{
            public readonly long id;
            public Read type;
            public object obj;

            public Element( long _id, Read _type, object _obj ){
                id = _id;
                type = _type;
                obj = _obj;
            }
        }

        private class Manifest{
            public List<Element> elements = new List<Element>();
            public List<Connection> connections = new List<Connection>();
            public AnimationStack animationStack = null;
            
            public Element FindElement( long id ){
                foreach( var element in elements ){
                    if( element.id == id ){
                        return element;
                    }
                }
                return null;
            }
            
            public List<T> FindAllElements<T>( Read type ){
                var list = new List<T>();
                foreach( var element in elements ){
                    if( element.type == type ){
                        var tObj = (T)element.obj;
                        if( tObj == null ) throw new System.Exception("Improper Element Type during find casting");
                        list.Add( tObj );
                    }
                }
                return list;
            }
        }
    }
}

}