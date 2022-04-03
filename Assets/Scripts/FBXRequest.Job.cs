using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Unity.Jobs;
using Unity.Collections;
using Fbx;



namespace viva{


[System.Flags]
public enum FBXContent{
    MODEL       =1,
    ANIMATON    =2,
}
public partial class FBXRequest: SpawnableImportRequest{

    private partial struct ExecuteReadFbx : IJob {

        public static readonly float blenderToUnityScale = 0.01f;
        public static readonly long kTimeUnit = 46186158000;   //1 second
        public static readonly int framerateSampleDensity = 24;

        public NativeArray<byte> result;
        public NativeArray<byte> filepathBuffer;
        public int index;
        public FBXContent content;


        public ExecuteReadFbx( NativeArray<byte> _buffer, NativeArray<byte> _filepathBuffer, FBXContent _content ){
            result = _buffer;
            filepathBuffer = _filepathBuffer;
            index = 0;
            content = _content;
            
            BufferUtil.InitializeHeader( result, ref index, BufferUtil.manifestHeaderCount );
        }

        public void Execute(){
            
            var filepathBytes = new byte[ filepathBuffer.Length ];
            filepathBuffer.CopyTo( filepathBytes );
            string filepath = System.Text.Encoding.UTF8.GetString( filepathBytes );
            var manifest = new Manifest();
            string error = null;
            try{
                var output = FbxIO.ReadBinary( filepath );
                // if( filepath.Contains("test") ){
                //     var stream = new MemoryStream();
                //     var writer = new FbxAsciiWriter( stream );
                //     writer.Write(output);
                //     Debug.LogError( System.Text.Encoding.UTF8.GetString(stream.ToArray()) );
                // }

                manifest.elements.Add( new Element( 0, Read.WORLD, null ) );    //dummy for world root
                int progressStep = (int)( 128*( 0.9f/output.Nodes.Count ) );
                foreach( var node in output.Nodes ){
                    if( node.Name == "Objects" ){
                        foreach( var objChild in node.Nodes ){
                            if( objChild == null ) continue;
                            
                            if( objChild.Name == "Geometry" ) PrepareGeometryObject( objChild, manifest );
                            else
                            if( objChild.Name == "Model" ) PrepareModelObject( objChild, manifest );
                            else
                            if( objChild.Name == "Deformer" ) PrepareDeformerObject( objChild, manifest );
                            else
                            if( objChild.Name == "Material" ) PrepareMaterialObject( objChild, manifest );
                            else
                            if( content.HasFlag( FBXContent.ANIMATON ) && objChild.Name == "AnimationCurve" ) PrepareAnimationCurveObject( objChild, manifest );
                            else
                            if( content.HasFlag( FBXContent.ANIMATON ) && objChild.Name == "AnimationCurveNode" ) PrepareAnimationChannelInfoObject( objChild, manifest );
                            else
                            if( content.HasFlag( FBXContent.ANIMATON ) && objChild.Name == "AnimationLayer" ) PrepareAnimationInfoObject( objChild, manifest );
                            else
                            if( content.HasFlag( FBXContent.ANIMATON ) && objChild.Name == "AnimationStack" ) PrepareAnimationStackObject( objChild, manifest );
                        }
                    }else if( node.Name == "Connections" ){
                        PrepareManifestConnections( node, manifest );
                        ApplyManifestConnections( manifest );
                    }
                }
            }catch( System.Exception e ){
                error = e.ToString();
            }
            if( error == null ){
                try{
                    WriteAllGeometries( manifest );
                }catch( System.Exception e ){
                    error = e.ToString();
                    index = 0;
                }
            }
            if( error != null ){
                var errorBytes = System.Text.Encoding.ASCII.GetBytes( error );
                BufferUtil.IncreaseHeaderEntry( result, ref index, (int)BufferUtil.Header.ERROR_LENGTH, errorBytes.Length );

                index = ((int)BufferUtil.Header.ERROR_LENGTH+3)*4;   //override everything past ERROR_LENGTH for the error string
                BufferUtil.WriteBytes( result, ref index, errorBytes );
            }
            BufferUtil.IncreaseHeaderEntry( result, ref index, (int)BufferUtil.Header.READ_LENGTH, index );
        }

        private void PrepareAnimationCurveObject( FbxNode objChild, Manifest manifest ){
            var animationCurve = new AnimationCurveInfo();
            PrepareObjectHeader( Read.ANIMATION_CURVE, objChild, manifest, animationCurve, ref animationCurve.name );
            foreach( var animCurveNode in objChild.Nodes ){
                if( animCurveNode == null ) continue;

                if( animCurveNode.Name == "KeyTime" ) animationCurve.times = (long[])animCurveNode.Value;
                else
                if( animCurveNode.Name == "KeyValueFloat" ) animationCurve.values = (float[])animCurveNode.Value;
                else
                if( animCurveNode.Name == "KeyAttrDataFloat" ) animationCurve.bezierInfo = (float[])animCurveNode.Value;

            }
        }
        private void PrepareAnimationChannelInfoObject( FbxNode objChild, Manifest manifest ){
            var animationTargetInfo = new AnimationChannelInfo();
            PrepareObjectHeader( Read.ANIMATION_CHANNEL_INFO, objChild, manifest, animationTargetInfo, ref animationTargetInfo.name );
        }
        private void PrepareAnimationInfoObject( FbxNode objChild, Manifest manifest ){
            var animationInfo = new AnimationInfo();
            PrepareObjectHeader( Read.ANIMATION_INFO, objChild, manifest, animationInfo, ref animationInfo.name );
            Debug.Log("Found animation \""+animationInfo.name+"\"");
        }
        private void PrepareAnimationStackObject( FbxNode objChild, Manifest manifest ){
            
            AnimationStack animStack;
            if( manifest.animationStack == null ){
                animStack = new AnimationStack();
                manifest.animationStack = animStack;
            }else{
                animStack = manifest.animationStack;
            }
            PrepareObjectHeader( Read.ANIMATION_STACK, objChild, manifest, animStack, ref animStack.name );
            foreach( var animStackNode in objChild.Nodes ){
                if( animStackNode == null ) continue;

                if( animStackNode.Name == "ReferenceStop" ) animStack.animLength = (long)animStackNode.Properties[4];
            }
        }

        private void ApplyManifestConnections( Manifest manifest ){
            
            foreach( var connection in manifest.connections ){
                var element0 = manifest.FindElement( connection.id0 );
                var element1 = manifest.FindElement( connection.id1 );
                // Debug.LogError(element1.type+" -> "+element0.type);
                switch( element0.type ){
                case Read.ARMATURE_INFO:
                    switch( element1.type ){
                    case Read.GEOMETRY:
                        ((Geometry)element1.obj).armature = (ArmatureInfo)element0.obj;
                        break;
                    }
                    break;
                case Read.MODEL:
                    switch( element1.type ){
                    case Read.WORLD:
                        break;
                    case Read.MODEL:
                        ((ModelInfo)element0.obj).parent = (ModelInfo)element1.obj;
                        break;
                    case Read.ARMATURE_MODEL:
                        ((ModelInfo)element0.obj).parent = (ModelInfo)element1.obj;
                        break;
                    case Read.BONE_MODEL:
                        ((ModelInfo)element0.obj).parent = (ModelInfo)element1.obj;
                        break;
                    }
                    break;
                case Read.MATERIAL:
                    switch( element1.type ){
                    case Read.MODEL:
                        var model = (ModelInfo)element1.obj;
                        foreach( var element in manifest.elements ){
                            if( element.type == Read.GEOMETRY ){
                                var geometry = (Geometry)element.obj;
                                if( geometry.model == model ){
                                    geometry.materials.Add( (MaterialInfo)element0.obj );
                                }
                            }
                        }
                        break;
                    }
                    break;
                case Read.ANIMATION_CURVE:
                    if( element1.type == Read.ANIMATION_CHANNEL_INFO ){
                        ( (AnimationChannelInfo)element1.obj ).curves.Add( (AnimationCurveInfo)element0.obj );
                    }
                    break;
                case Read.ANIMATION_CHANNEL_INFO:
                    if( element1.type == Read.ANIMATION_INFO ){
                        ( (AnimationInfo)element1.obj ).channelInfos.Add( (AnimationChannelInfo)element0.obj );
                    }else if( element1.type == Read.BONE_MODEL ){
                        ( (AnimationChannelInfo)element0.obj ).targetName = ( (ModelInfo)element1.obj ).name;
                    }else if( element1.type == Read.ARMATURE_MODEL ){
                        //armature animation manipulation not allowed for the game
                        ( (AnimationChannelInfo)element0.obj ).targetName = null;   //nullify and throw away animation channel
                    }else if( element1.type == Read.SHAPE_CHANNEL ){
                        
                    }
                    break;
                case Read.ANIMATION_INFO:
                    if( element1.type == Read.ANIMATION_STACK ){
                        ( (AnimationStack)element1.obj ).animations.Add( (AnimationInfo)element0.obj );
                    }
                    break;
                case Read.BONE_MODEL:
                    switch( element1.type ){
                    case Read.BONE_INFO:
                        ((BoneInfo)element1.obj).boneModel = (ModelInfo)element0.obj;
                        break;
                    case Read.BONE_MODEL:
                        ((ModelInfo)element0.obj).parent = (ModelInfo)element1.obj;
                        break;
                    case Read.ARMATURE_MODEL:
                        var model0 = (ModelInfo)element0.obj;
                        var model1 = (ModelInfo)element1.obj;
                        ((ModelInfo)element0.obj).parent = (ModelInfo)element1.obj;
                        foreach( var element in manifest.elements ){
                            if( element.type == Read.ARMATURE_INFO ){
                                var armatureInfo = (ArmatureInfo)element.obj;
                                foreach( var childBoneInfo in armatureInfo.bones ){
                                    if( childBoneInfo.boneModel == model0 ){
                                        armatureInfo.root = model1;
                                        break;
                                    }
                                }
                            }
                        }
                        break;
                    }
                    break;
                case Read.GEOMETRY:
                    switch( element1.type ){
                    case Read.MODEL:
                        var geometry = (Geometry)element0.obj;
                        var model = (ModelInfo)element1.obj;
                        geometry.model = model;
                        geometry.collider = CheckForCollider( model );
                        break;
                    }
                    break;
                case Read.BONE_INFO:
                    switch( element1.type ){
                    case Read.ARMATURE_INFO:
                        ((ArmatureInfo)element1.obj).bones.Add( (BoneInfo)element0.obj );
                        break;
                    }
                    break;
                case Read.ARMATURE_MODEL:
                    switch( element1.type ){
                    case Read.WORLD:
                        break;
                    }
                    break;
                case Read.SHAPE:
                    switch( element1.type ){
                    case Read.SHAPE_CHANNEL:
                        ((ShapeKeyChannel)element1.obj).shape = (Shape)element0.obj;
                        break;
                    }
                    break;
                case Read.SHAPE_CHANNEL:
                    switch( element1.type ){
                    case Read.SHAPEKEY:
                        ((ShapeKey)element1.obj).shapeKeyChannels.Add( (ShapeKeyChannel)element0.obj );
                        break;
                    }
                    break;
                case Read.SHAPEKEY:
                    switch( element1.type ){
                    case Read.GEOMETRY:
                        var geometry = (Geometry)element1.obj;
                        foreach( var shapeKeyChannel in ( (ShapeKey)element0.obj ).shapeKeyChannels ){
                            bool valid = true;
                            //enforce unique shapekey names
                            foreach( var existingShape in geometry.shapeKeys ){
                                if( existingShape.name == shapeKeyChannel.shape.name ){
                                    valid = false;
                                    break;
                                }
                            }
                            if( valid ){
                                geometry.shapeKeys.Add( shapeKeyChannel.shape );
                            }
                        }
                        break;
                    }
                    break;
                }
            }
            ProcessElements( manifest );
        }

        private void BuildArmatureChildHierarchy( Manifest manifest ){
            var armatures = manifest.FindAllElements<ArmatureInfo>( Read.ARMATURE_INFO );
            foreach( var armature in armatures ){
                for( int i=0; i<armature.bones.Count; i++ ){
                    var boneInfo_i = armature.bones[i];
                    for( int j=i+1; j<armature.bones.Count; j++ ){
                        var boneInfo_j = armature.bones[j];
                        if( boneInfo_i.boneModel.parent == boneInfo_j.boneModel ){
                            boneInfo_j.children.Add( boneInfo_i );
                        }else if( boneInfo_j.boneModel.parent == boneInfo_i.boneModel ){
                            boneInfo_i.children.Add( boneInfo_j );
                        }
                    }
                }
                //trim all armature bones that don't deform
                for( int i=armature.bones.Count; i-->0; ){
                    var boneInfo = armature.bones[i];
                    if( !IsBoneDeforming( boneInfo ) ){
                        armature.bones.RemoveAt(i);
                    }else{
                        if( boneInfo.boneWeights == null ){
                            boneInfo.boneIndices = new List<int>();
                            boneInfo.boneWeights = new List<float>();
                        }
                    }
                }
            }
        }

        private ColliderInfo CheckForCollider( ModelInfo model ){
            var name = model.name;
            //find collider type
            PhysicsType physicsType;
            int suffixStart = name.IndexOf( "rigid" );
            if( suffixStart == -1 ){
                suffixStart = name.IndexOf( "grab" );
                if( suffixStart == -1 ){
                    suffixStart = name.IndexOf( "cloth" );
                    if( suffixStart == -1 ){
                        suffixStart = name.IndexOf( "zone" );
                        if( suffixStart == -1 ){
                            suffixStart = name.IndexOf( "trigger" );
                            if( suffixStart == -1 ){
                                suffixStart = name.IndexOf( "jiggle" );
                                if( suffixStart == -1 ){
                                    return null;
                                }else{
                                    physicsType = PhysicsType.JIGGLE;
                                }
                            }else{
                                physicsType = PhysicsType.TRIGGER;
                            }
                        }else{
                            physicsType = PhysicsType.ZONE;
                        }
                    }else{
                        physicsType = PhysicsType.CLOTH;
                    }
                }else{
                    physicsType = PhysicsType.GRAB;
                }
            }else{
                physicsType = PhysicsType.RIGID;
            }

            model.scale = new Vector3(
                Mathf.Abs( model.scale.x ),
                Mathf.Abs( model.scale.y ),
                Mathf.Abs( model.scale.z )
            );

            int periodStart = name.LastIndexOf('.');
            periodStart = periodStart == -1 ? name.Length : periodStart;
            suffixStart += physicsType.ToString().Length;
            string suffix = name.Substring( suffixStart, periodStart-suffixStart );
            switch( physicsType ){
            case PhysicsType.RIGID:
                switch( suffix ){
                case "_capsule":
                    return new CapsuleColliderInfo( physicsType );
                case "_sphere":
                    return new SphereColliderInfo( physicsType );
                case "_convex":
                    return new PolygonColliderInfo( ColliderType.CONVEX, physicsType );
                case "_concave":
                    return new PolygonColliderInfo( ColliderType.CONCAVE, physicsType );
                case "_cube":
                case "_box":
                    return new BoxColliderInfo( physicsType );
                default:
                    throw new System.Exception(suffix+" is not a supported physics type. object:  \""+name+"\"");
                }
            case PhysicsType.GRAB:
                switch( suffix ){
                case "_cube":
                case "_box":
                    return new BoxColliderInfo( physicsType );
                case "_cylinder":
                    return new CylinderColliderInfo( physicsType );
                default:
                    throw new System.Exception(suffix+" is not supported grab type. object:  \""+name+"\"");
                }
            case PhysicsType.CLOTH:
                switch( suffix ){
                case "_capsule":
                    return new CapsuleColliderInfo( physicsType );
                default:
                    throw new System.Exception(suffix+" is not supported cloth type. object:  \""+name+"\"");
                }
            case PhysicsType.ZONE:
                switch( suffix ){
                case "_cube":
                case "_box":
                    return new BoxColliderInfo( physicsType );
                default:
                    throw new System.Exception(suffix+" is not supported zone type. object:  \""+name+"\"");
                }
            case PhysicsType.TRIGGER:
                switch( suffix ){
                case "_capsule":
                    return new CapsuleColliderInfo( physicsType );
                case "_sphere":
                    return new SphereColliderInfo( physicsType );
                case "_convex":
                    return new PolygonColliderInfo( ColliderType.CONVEX, physicsType );
                case "_cube":
                case "_box":
                    return new BoxColliderInfo( physicsType );
                default:
                    throw new System.Exception(suffix+" is not supported zone type. object:  \""+name+"\"");
                }
            case PhysicsType.JIGGLE:
                switch( suffix ){
                case "_capsule":
                    return new CapsuleColliderInfo( physicsType );
                case "_sphere":
                    return new SphereColliderInfo( physicsType );
                default:
                    throw new System.Exception(suffix+" is not supported cloth type. object:  \""+name+"\"");
                }
            }
            return null;
        }

        private void ConvertColliderMeshesToColliderInfos( Manifest manifest ){
            foreach( var element in manifest.elements ){
                //filter out Geometries only
                if( element.type != Read.GEOMETRY ) continue;
                var geometry = (Geometry)element.obj;
                if( geometry.collider == null ) continue;
                if( geometry.vertexFloats == null ) throw new System.Exception("Collider "+geometry.name+" missing vertices");
                if( geometry.polyIndices == null ) throw new System.Exception("Collider "+geometry.name+" missing indices");
                
                geometry.collider.model = geometry.model;
                geometry.collider.model.colliderSource = true;
                geometry.collider.Build( geometry );
            }
        }

        private void RemoveUnbindedAnimationChannels( Manifest manifest ){
            if( manifest.animationStack != null ){  //(Armature animations not allowed)
                foreach( var animation in manifest.animationStack.animations ){

                    //reorder curve channels to x y z binding order
                    foreach( var channelInfo in animation.channelInfos ){
                        channelInfo.curves.Sort( (a,b)=>a.bind.CompareTo(b.bind) );
                    }

                    for( int i=animation.channelInfos.Count; i-->0; ){
                        if( animation.channelInfos[i].targetName == null ){
                            animation.channelInfos.RemoveAt(i);
                        }
                    }

                    //reorder channelInfos by alphabetical targetBoneName
                    animation.channelInfos.Sort( (a,b)=>a.targetName.CompareTo(b.targetName) );
                }
            }
        }

        private void FixRootBoneAnimationCurves( Manifest manifest ){
            var armatureInfos = manifest.FindAllElements<ArmatureInfo>( Read.ARMATURE_INFO );
            var animationChannels = manifest.FindAllElements<AnimationChannelInfo>( Read.ANIMATION_CHANNEL_INFO );
            foreach( var armatureInfo in armatureInfos ){
                foreach( var bone in armatureInfo.bones ){
                    if( bone.boneModel.parent == armatureInfo.root ){
                        foreach( var animationChannel in animationChannels ){
                            if( animationChannel.targetName == bone.name ){
                                animationChannel.applyRootOffset = true;
                            }
                        }
                    }
                }
            }
        }

        private void AssignParentGeometryToAnimations( Manifest manifest ){
            
            var armatures = manifest.FindAllElements<ArmatureInfo>( Read.ARMATURE_INFO );
            if( manifest.animationStack != null ){
                foreach( var animation in manifest.animationStack.animations ){
                    var colonIndex = animation.name.LastIndexOf('|');
                    
                    bool overrideAddToFirst = false;;
                    if( armatures.Count == 1 ){
                        overrideAddToFirst = true;
                    }else if( armatures.Count == 0 ){
                        throw new System.Exception("Error there are no armatures parsed in this fbx");
                    }
                    var parentArmatureName = colonIndex >= 0 ? animation.name.Substring( 0, colonIndex ) : animation.name;
                    animation.name = animation.name.Substring( ++colonIndex, animation.name.Length-colonIndex );
                    bool found = false;
                    foreach( var armature in armatures ){
                        if( armature.name == parentArmatureName || overrideAddToFirst ){
                            armature.animations.Add( animation );
                            found = true;
                            break;
                        }
                    }
                    if( !found ){
                        throw new System.Exception("Could not find parent armature to assign animation for \""+parentArmatureName+"\"");
                    }
                }
            }
        }

        private void ProcessElements( Manifest manifest ){
            BuildArmatureChildHierarchy( manifest );
            ConvertColliderMeshesToColliderInfos( manifest );
            RemoveUnbindedAnimationChannels( manifest );
            FixRootBoneAnimationCurves( manifest );
            AssignParentGeometryToAnimations( manifest );
        }

        private void PrepareManifestConnections( FbxNode node, Manifest manifest ){
            for( int i=0; i<node.Nodes.Count; i++ ){
                var objChild = node.Nodes[i];
                if( objChild == null ) continue;
                string connectionType = (string)objChild.Properties[0];
                long id0 = (long)objChild.Properties[1];
                long id1 = (long)objChild.Properties[2];

                var element0 = manifest.FindElement( id0 );
                var element1 = manifest.FindElement( id1 );
                if( element0 == null || element1 == null ) continue;

                int importance = (int)element0.type+i;
                importance += (int)element1.type*System.Enum.GetValues(typeof(Read)).Length*node.Nodes.Count;
                
                manifest.connections.Add( new Connection( importance, id0, id1 ) );
                
                if( connectionType == "OP" ){
                    var obj1 = manifest.FindElement( id1 );
                    if( obj1.type == Read.BONE_MODEL ){
                        var animationTargetInfo = manifest.FindElement( id0 ).obj as AnimationChannelInfo;
                        string bindString = (string)objChild.Properties[3];
                        if( bindString == "Lcl Translation"){
                            animationTargetInfo.channel = AnimationChannel.Channel.POSITION;
                        }else if( bindString == "Lcl Rotation"){
                            animationTargetInfo.channel = AnimationChannel.Channel.ROTATION;
                        }else if( bindString == "Lcl Scaling"){
                            animationTargetInfo.channel = AnimationChannel.Channel.SCALE;
                        }else{
                            throw new System.Exception("Unknown AnimationChannelInfo bind type \""+bindString+"\"");
                        }
                    }else if( obj1.type == Read.GEOMETRY ){
                        var animationTargetInfo = manifest.FindElement( id0 ).obj as AnimationChannelInfo;
                        string bindString = (string)objChild.Properties[3];
                        animationTargetInfo.channel = AnimationChannel.Channel.BLENDSHAPE;
                        animationTargetInfo.targetName = bindString;

                    }else if( obj1.type == Read.ANIMATION_CHANNEL_INFO ){
                        var animationCurve = manifest.FindElement( id0 ).obj as AnimationCurveInfo;
                        string bindString = (string)objChild.Properties[3];
                        if( bindString == "d|X"){
                            animationCurve.bind = 0;
                        }else if( bindString == "d|Y"){
                            animationCurve.bind = 1;
                        }else if( bindString == "d|Z"){
                            animationCurve.bind = 2;
                        }else{
                            // throw new System.Exception("Unknown animation curve bind channel: "+(string)objChild.Properties[3]);
                        }
                    }
                }
            }
            manifest.connections.Sort( (emp1,emp2)=>emp1.importance.CompareTo(emp2.importance) );
        }

        private bool IsBoneDeforming( BoneInfo boneInfo ){
            if( boneInfo.boneWeights != null ){
                return true;
            }
            foreach( var childBone in boneInfo.children ){
                if( IsBoneDeforming( childBone ) ){
                    return true;
                }
            }
            return false;
        }

        private void FixResampledCurveRotations( AnimationChannelInfo channelInfo, List<AnimationCurveResample> curveResamples ){
            float pitchOffset = channelInfo.applyRootOffset ? -90.0f : 0.0f;
            var curveSampleW = new AnimationCurveResample( curveResamples[0].values.Length );
            curveResamples.Add( curveSampleW );
            for( int i=0; i<curveResamples[0].values.Length; i++ ){
                float x = curveResamples[0].values[i];
                float y = curveResamples[1].values[i];
                float z = curveResamples[2].values[i];

                var rot = FixImportEuler( new Vector3( x, y, z ), pitchOffset );

                curveResamples[0].values[i] = rot.x;
                curveResamples[1].values[i] = rot.y;
                curveResamples[2].values[i] = rot.z;
                curveSampleW.values[i] = rot.w;
            }
        }

        private void FixResampledCurvePositions( AnimationChannelInfo channelInfo, List<AnimationCurveResample> curveResamples ){
            for( int i=0; i<curveResamples[0].values.Length; i++ ){
                float x = curveResamples[0].values[i];
                float y = curveResamples[1].values[i];
                float z = curveResamples[2].values[i];
                if( channelInfo.applyRootOffset ){
                    curveResamples[0].values[i] = -x;
                    curveResamples[1].values[i] = z;
                    curveResamples[2].values[i] = -y;
                }else{
                    curveResamples[0].values[i] = -x;
                    curveResamples[1].values[i] = y;
                    curveResamples[2].values[i] = z;
                }
            }
        }
        
        private void FixResampledCurveBlendshapes( AnimationChannelInfo channelInfo, List<AnimationCurveResample> curveResamples ){
            for( int i=0; i<curveResamples[0].values.Length; i++ ){
                float x = curveResamples[0].values[i]*0.01f;
                curveResamples[0].values[i] = x;
            }
        }

        private void WriteAnimations( ArmatureInfo armature ){

            if( armature.animations.Count > 0 ){
                BufferUtil.WriteInt( result, ref index, armature.animations.Count );
                BufferUtil.WriteInt( result, ref index, framerateSampleDensity );
                foreach( var animation in armature.animations ){
                    BufferUtil.WriteString( result, ref index, animation.name );
                    BufferUtil.WriteInt( result, ref index, animation.channelInfos.Count );
                    BufferUtil.WriteFloat( result, ref index, (float)animation.channelInfos[0].curves[0].times[ animation.channelInfos[0].curves[0].times.Length-1 ]/kTimeUnit );
                    foreach( var channelInfo in animation.channelInfos ){
                        BufferUtil.WriteString( result, ref index, channelInfo.targetName );
                        BufferUtil.WriteInt( result, ref index, (int)channelInfo.channel );

                        var curveResamples = new List<AnimationCurveResample>();
                        foreach( var curve in channelInfo.curves ){
                            curveResamples.Add( ResampleCurve( curve.times, curve.values, 0.001f ) );
                        }
                        //fix blender to Unity rotation axis
                        if( channelInfo.channel == AnimationChannel.Channel.ROTATION ){
                            FixResampledCurveRotations( channelInfo, curveResamples );
                        }else if( channelInfo.channel == AnimationChannel.Channel.POSITION ){
                            FixResampledCurvePositions( channelInfo, curveResamples );
                        }else if( channelInfo.channel == AnimationChannel.Channel.BLENDSHAPE ){
                            FixResampledCurveBlendshapes( channelInfo, curveResamples );
                        }
                        bool noneHaveDeltas = true;
                        foreach( var curveResample in curveResamples ){
                            noneHaveDeltas &= !curveResample.hasDelta;
                        }
                        BufferUtil.WriteInt( result, ref index, curveResamples.Count );
                        if( noneHaveDeltas ){
                            foreach( var curveResample in curveResamples ){
                                BufferUtil.WriteInt( result, ref index, 1 );
                                BufferUtil.WriteFloat( result, ref index, curveResample.values[0] );
                                BufferUtil.WriteInt( result, ref index, 1 );
                                BufferUtil.WriteByte( result, ref index, (byte)System.Convert.ToInt32( curveResample.linear[0] ) );
                            }
                        }else{
                            foreach( var curveResample in curveResamples ){
                                BufferUtil.WriteFloatArray( result, ref index, curveResample.values );
                                BufferUtil.WriteBoolArrayAsByteArray( result, ref index, curveResample.linear );
                            }
                        }
                    }
                }
            }else{
                BufferUtil.WriteInt( result, ref index, 0 );
            }
        }

        private AnimationCurveResample ResampleCurve( long[] times, float[] values, float minDelta ){
            
            int frameCount = Mathf.CeilToInt( (float)times[ times.Length-1 ]/kTimeUnit )*framerateSampleDensity;
            //add 1 for the end frame
            frameCount += 1;
            var sample = new AnimationCurveResample( frameCount );

            long kTimeUnitStep = kTimeUnit/framerateSampleDensity;

            int frame = 0;
            long targetKTime = 0;
            int kTimeIndex = 0;
            //first frame
            sample.values[ frame ] = values[0];
            float lastC = values[0];

            //in between frames
            for( frame=1; frame<frameCount-1; frame++ ){
                targetKTime += kTimeUnitStep;
                long nextTime = times[ kTimeIndex ];
                while( nextTime <= targetKTime ){
                    if( kTimeIndex >= times.Length-1 ){
                        break;
                    }
                    kTimeIndex++;
                    nextTime = times[ kTimeIndex ];
                }

                //interpolate both KTime values
                float a = values[ kTimeIndex-1 ];
                float b = values[ kTimeIndex ];
                long prevTime = times[ kTimeIndex-1 ];
                float timeDiff = nextTime-prevTime;
                float lerp = (float)( nextTime-targetKTime )/timeDiff;
                float c = b+(a-b)*lerp;
                sample.values[ frame ] = c;

                if( Mathf.Abs( lastC-c ) > minDelta ){
                    sample.hasDelta = true;
                }
            }

            //last frame
            sample.values[ frame ] = values[ values.Length-1 ];
            return sample;
        }

        private void WriteAllGeometries( Manifest manifest ){
            
            var armatureIDs = new List<short>();
            var geometries = manifest.FindAllElements<Geometry>( Read.GEOMETRY );
            BufferUtil.WriteInt( result, ref index, geometries.Count );
            foreach( var geometry in geometries ){
                if( geometry.collider == null ){
                    WriteGeometry( manifest, geometry, armatureIDs );
                }else{
                    WriteColliderGeometry( manifest, geometry.collider );
                }
            }
            WriteParentHierarchies( manifest );
        }
        
        private void WriteColliderGeometry( Manifest manifest, ColliderInfo colliderInfo ){
            BufferUtil.WriteByte( result, ref index, 1 );
            WriteModel( colliderInfo.model );
            BufferUtil.WriteByte( result, ref index, (byte)colliderInfo.physicsType );
            BufferUtil.WriteByte( result, ref index, (byte)colliderInfo.type );
            colliderInfo.WriteColliderInfo( result, ref index);
        }

        private void WriteGeometry( Manifest manifest, Geometry geometry, List<short> armatureIDs ){
            BufferUtil.WriteByte( result, ref index, 0 );
            if( geometry.materialType == "AllSame" ){
                geometry.matPolyIndices = null;
            }
            if( geometry.normalType == null ){
                throw new System.Exception("No normal type specified");
            }else if( geometry.normalType == "ByPolygonVertex" ){
                ReduceSimilarVertices(
                    geometry
                );
            }else if( geometry.normalType == "ByVertex" ){
                geometry.normalFloats = DoubleArrayToFloatList( geometry.normalDoubles );
            }else{
                throw new System.Exception("Unsupported normal encoding ["+geometry.normalType+"]");
            }

            WriteModel( geometry.model );

            //write mesh data
            BufferUtil.WriteFloatList( result, ref index,  geometry.vertexFloats );
            if( geometry.vertexFloats.Count > 0 ){
                BufferUtil.WriteFloatList( result, ref index,  geometry.normalFloats );
                if( geometry.uvFloats != null ){
                    BufferUtil.WriteInt( result, ref index, 1 );
                    WriteUVs( geometry.uvFloats );
                }else{
                    BufferUtil.WriteInt( result, ref index, 0 );
                }
                WriteSubmeshIndices( geometry );
                WriteShapeKeys( geometry );

                if( geometry.armature == null ){
                    BufferUtil.WriteByte( result, ref index, (byte)MeshType.STATIC);
                }else{
                    if( geometry.clothInfo != null ){
                        BufferUtil.WriteByte( result, ref index, (byte)MeshType.CLOTH);
                        WriteArmature( geometry, armatureIDs, manifest );
                        
                        BufferUtil.WriteFloatArray( result, ref index, geometry.clothInfo.clothPin );
                        BufferUtil.WriteFloat( result, ref index, geometry.clothInfo.bending );
                        BufferUtil.WriteFloat( result, ref index, geometry.clothInfo.stretching );
                        BufferUtil.WriteFloat( result, ref index, geometry.clothInfo.damping );
                    }else{
                        BufferUtil.WriteByte( result, ref index, (byte)MeshType.SKINNED);
                        WriteArmature( geometry, armatureIDs, manifest );
                        BufferUtil.WriteInt( result, ref index, geometry.meshWeights.Count );
                        foreach( var meshWeight in geometry.meshWeights ){
                            BufferUtil.WriteInt( result, ref index, meshWeight.vertexIndex );
                            BufferUtil.WriteInt( result, ref index, meshWeight.boneIndex );
                            BufferUtil.WriteFloat( result, ref index, meshWeight.weight );
                        }

                        WriteAnimations( geometry.armature );
                    }
                }
            }
        }

        private void WriteArmature( Geometry geometry, List<short> armatureIDs, Manifest manifest ){
            if( armatureIDs.Contains( geometry.armature.root.id ) ){
                BufferUtil.WriteShort( result, ref index, geometry.armature.root.id );
            }else{
                armatureIDs.Add( geometry.armature.root.id );
                WriteModel( geometry.armature.root );
                var dynamicBoneRoots = new List<ModelInfo>();
                var muscles = new List<ModelInfo>();
                BufferUtil.WriteInt( result, ref index, geometry.armature.bones.Count );
                foreach( var boneInfo in geometry.armature.bones ){
                    BufferUtil.WriteShort( result, ref index, boneInfo.boneModel.id );
                    BufferUtil.WriteString( result, ref index, boneInfo.boneModel.name );
                    BufferUtil.WriteDoubleArrayAsFloatArray( result, ref index,  boneInfo.transformLink );
                    if( boneInfo.boneModel.dynamicBoneInfo.stiffness > 0 ||
                        boneInfo.boneModel.dynamicBoneInfo.damping > 0 ||
                        boneInfo.boneModel.dynamicBoneInfo.elasticity > 0 ||
                        boneInfo.boneModel.dynamicBoneInfo.gravity != 0
                     ){
                        dynamicBoneRoots.Add( boneInfo.boneModel );
                    }
                    if( boneInfo.boneModel.muscleTemplate.mass > 0 ){
                        muscles.Add( boneInfo.boneModel );
                    }
                }
                //write muscles
                BufferUtil.WriteByte( result, ref index, (byte)muscles.Count );
                foreach( var muscle in muscles ){
                    BufferUtil.WriteShort( result, ref index, muscle.id );
                    BufferUtil.WriteFloat( result, ref index, muscle.muscleTemplate.mass );
                    BufferUtil.WriteFloat( result, ref index, muscle.muscleTemplate.pitch );
                    BufferUtil.WriteFloat( result, ref index, muscle.muscleTemplate.yaw );
                    BufferUtil.WriteFloat( result, ref index, muscle.muscleTemplate.roll );
                }
                //trim max dynamicBoneRoots
                if( dynamicBoneRoots.Count >= System.Byte.MaxValue ){
                    dynamicBoneRoots.RemoveRange( System.Byte.MaxValue, dynamicBoneRoots.Count-System.Byte.MaxValue );
                }
                BufferUtil.WriteByte( result, ref index, (byte)dynamicBoneRoots.Count );
                var colliders = manifest.FindAllElements<Geometry>( Read.GEOMETRY );
                foreach( var dynamicBoneRoot in dynamicBoneRoots ){
                    BufferUtil.WriteShort( result, ref index, dynamicBoneRoot.id );
                    BufferUtil.WriteFloat( result, ref index, dynamicBoneRoot.dynamicBoneInfo.stiffness );
                    BufferUtil.WriteFloat( result, ref index, dynamicBoneRoot.dynamicBoneInfo.damping );
                    BufferUtil.WriteFloat( result, ref index, dynamicBoneRoot.dynamicBoneInfo.elasticity );
                    BufferUtil.WriteFloat( result, ref index, dynamicBoneRoot.dynamicBoneInfo.radius );
                    BufferUtil.WriteFloat( result, ref index, dynamicBoneRoot.dynamicBoneInfo.gravity );
                    short colliderId = -1;
                    foreach( var collider in colliders ){
                        if( collider.collider == null ) continue;
                        if( collider.model.name == dynamicBoneRoot.dynamicBoneInfo.collider ){
                            colliderId = collider.model.id;
                            break;
                        }
                    }
                    BufferUtil.WriteShort( result, ref index, colliderId );
                }
                foreach( var boneInfo in geometry.armature.bones ){
                    short parentIndex = -1;
                    for( short i=0; i<geometry.armature.bones.Count; i++ ){
                        if( geometry.armature.bones[i].boneModel == boneInfo.boneModel.parent ){
                            parentIndex = i;
                            break;
                        }
                    }
                    BufferUtil.WriteShort( result, ref index, parentIndex );
                }
            }
        }

        private void WriteShapeKeys( Geometry geometry ){
            BufferUtil.WriteByte( result, ref index, (byte)geometry.shapeKeys.Count );
            foreach( var shapekey in geometry.shapeKeys ){
                BufferUtil.WriteString( result, ref index, shapekey.name );
                BufferUtil.WriteIntList( result, ref index, shapekey.indices );
                BufferUtil.WriteFloatList( result, ref index, shapekey.vertexFloats );
            }
        }

        private void WriteParentHierarchies( Manifest manifest ){
            var models = manifest.FindAllElements<ModelInfo>( Read.MODEL );
            int modelParents = 0;
            foreach( var model in models ){
                if( model.parent != null ) modelParents++;
            }
            BufferUtil.WriteInt( result, ref index, modelParents );
            foreach( var model in models ){
                if( model.parent != null ){
                    BufferUtil.WriteShort( result, ref index, model.id );
                    BufferUtil.WriteShort( result, ref index, model.parent.id );
                }
            }
        }

        private void WriteModel( ModelInfo model ){
            
            var pos = model.position;
            float temp = pos.z;
            pos.z = pos.y;
            pos.y = temp;
            BufferUtil.WriteShort( result, ref index, model.id );
            BufferUtil.WriteString( result, ref index,  model.name );
            BufferUtil.WriteFloat( result, ref index, pos.x );
            BufferUtil.WriteFloat( result, ref index, pos.y );
            BufferUtil.WriteFloat( result, ref index, pos.z );

            var euler = ( Quaternion.Euler(-90,0,0)*Quaternion.Euler( model.euler )*Quaternion.Euler(90,0,0) ).eulerAngles;

            BufferUtil.WriteFloat( result, ref index, model.euler.x );
            BufferUtil.WriteFloat( result, ref index, model.euler.y );
            BufferUtil.WriteFloat( result, ref index, model.euler.z );
            BufferUtil.WriteFloat( result, ref index, model.scale.x );
            BufferUtil.WriteFloat( result, ref index, model.scale.y );
            BufferUtil.WriteFloat( result, ref index, model.scale.z );
        }

        private void PrepareGeometryObject( FbxNode objChild, Manifest manifest ){
            var geometryType = (string)objChild.Properties[2];
            if( (string)objChild.Properties[2] == "Mesh" ) PrepareGeometryMesh( objChild, manifest );
            else
            if( (string)objChild.Properties[2] == "Shape" ) PrepareGeometryShapeKey( objChild, manifest );
        }

        private void PrepareGeometryShapeKey( FbxNode objChild, Manifest manifest ){
            var shapekey = new Shape();
            PrepareObjectHeader( Read.SHAPE, objChild, manifest, shapekey, ref shapekey.name );
            foreach( var geomChild in objChild.Nodes ){
                if( geomChild == null )continue;

                if( geomChild.Name == "Indexes" ) shapekey.indices = IntArrayToIntList( (int[])geomChild.Properties[0] );
                else
                if( geomChild.Name == "Vertices" ) GetGeometryVertices( geomChild, ref shapekey.vertexFloats );
            }
        }


        private void PrepareGeometryMesh( FbxNode objChild, Manifest manifest ){
            var geometry = new Geometry();
            PrepareObjectHeader( Read.GEOMETRY, objChild, manifest, geometry, ref geometry.name );

            var clothInfo = new ClothInfo();
            //rigidBody objects only need vertex data to decompile
            foreach( var geomChild in objChild.Nodes ){
                if( geomChild == null ) continue;

                if( geomChild.Name == "Vertices" ) GetGeometryVertices( geomChild, ref geometry.vertexFloats );
                else
                if( geomChild.Name == "PolygonVertexIndex" ) { geometry.polyIndices = (int[])geomChild.Properties[0]; }
                else
                if( geomChild.Name == "LayerElementNormal" ) GetNormals( geomChild, ref geometry.normalDoubles, ref geometry.normalType );
                else
                if( geomChild.Name == "LayerElementColor" ) GetClothPin( geomChild, ref clothInfo );
                else
                if( geomChild.Name == "LayerElementUV" ) GetUVs( geomChild, ref geometry.uvDoubles, ref geometry.uvIndices );
                else
                if( geomChild.Name == "LayerElementMaterial" ) GetGeometryMatPolyIndices( geomChild, ref geometry.matPolyIndices, ref geometry.materialType );
                else
                if( geomChild.Name == "Properties70" ){
                    foreach( var p in geomChild.Nodes ){
                        if( p == null ) continue;
                        switch( (string)p.Properties[0] ){
                        case "bending":
                            clothInfo.bending = ParseNumber( p.Properties[4] );
                            break;
                        case "stretching":
                            clothInfo.bending = ParseNumber( p.Properties[4] );
                            break;
                        case "damping":
                            clothInfo.bending = ParseNumber( p.Properties[4] );
                            break;
                        }
                    }
                }
            }
            if( clothInfo.clothPinColors != null ){
                geometry.clothInfo = clothInfo;
            }
        }

        private void WriteUVs( List<float> uvFloats ){
            BufferUtil.WriteInt( result, ref index, uvFloats.Count );
            for( int i=0; i<uvFloats.Count; i++ ){
                BufferUtil.WriteFloat( result, ref index, uvFloats[i] );
            }
        }

        private bool NormalsSimilar( Vector3 a, Vector3 b ){ 
            return a==b;
        }

        private bool UvsSimilar( Vector2 a, Vector2 b ){    
            return a==b;
        }

        private void ReduceSimilarVertices( Geometry geometry ){
            
            var vertexFloats = geometry.vertexFloats;
            var polyIndices = geometry.polyIndices;
            var normalDoubles = geometry.normalDoubles;
            var uvDoubles = geometry.uvDoubles;
            var uvIndices = geometry.uvIndices; 
            int vertexCount = vertexFloats.Count/3;
            int originalVertexCount = vertexCount;

            List<Vector3>[] uniqueNormalsList = new List<Vector3>[ vertexCount ];
            for( int i=0; i<uniqueNormalsList.Length; i++ ){ uniqueNormalsList[i] = new List<Vector3>(); }
            List<Vector2>[] uniqueUVsList = new List<Vector2>[ vertexCount ];
            for( int i=0; i<uniqueUVsList.Length; i++ ){ uniqueUVsList[i] = new List<Vector2>(); }

            geometry.normalFloats = new List<float>( vertexCount*3 );
            for( int i=0; i<geometry.normalFloats.Capacity; i++ ){ geometry.normalFloats.Add(0); }
            
            if( uvDoubles != null ){
                geometry.uvFloats = new List<float>( vertexCount*2 );
                for( int i=0; i<geometry.uvFloats.Capacity; i++ ){ geometry.uvFloats.Add(0); }
            }else{
                geometry.uvFloats = null;
            }

            List<int> duplicates = new List<int>();

            Vector2 polyUV = Vector2.zero;
            List<Vector2> uniqueUVs = null;

            for( int i=0; i<polyIndices.Length; i++ ){
                int vertIndex = polyIndices[i];
                bool polyEnd = vertIndex < 0;
                if( polyEnd ) vertIndex = -vertIndex-1;
                bool split = false;

                Vector3 polyNormal = new Vector3(
                    (float)normalDoubles[ i*3 ],
                    (float)normalDoubles[ i*3+1 ],
                    (float)normalDoubles[ i*3+2 ]
                );
                
                var uniqueNormals = uniqueNormalsList[ vertIndex ];
                if( uvDoubles != null ){ 
                    uniqueUVs = uniqueUVsList[ vertIndex ];
                    int uvIndex = uvIndices[i];
                    polyUV = new Vector2(
                        (float)uvDoubles[ uvIndex*2 ],
                        (float)uvDoubles[ uvIndex*2+1 ]
                    );
                    for( int j=0; j<uniqueNormals.Count; j++ ){
                        //check unique pair (uv & normal)
                        if( NormalsSimilar( polyNormal, uniqueNormals[j] ) && UvsSimilar( polyUV, uniqueUVs[j] ) ){
                        }else{
                            split = true;
                            break;
                        }
                    }
                }else{
                    for( int j=0; j<uniqueNormals.Count; j++ ){
                        //check unique normal
                        if( NormalsSimilar( polyNormal, uniqueNormals[j] ) ){
                        }else{
                            split = true;
                            break;
                        }
                    }
                }

                if( split ){
                    //duplicate vertex and push to end of list
                    duplicates.Add( vertIndex );
                    int newVertIndex = vertexCount++;
                    Vector3 polyPos = new Vector3(
                        vertexFloats[ vertIndex*3 ],
                        vertexFloats[ vertIndex*3+1 ],
                        vertexFloats[ vertIndex*3+2 ]
                    );
                    vertexFloats.Add( polyPos.x );
                    vertexFloats.Add( polyPos.y );
                    vertexFloats.Add( polyPos.z );

                    uniqueNormals.Add( polyNormal );
                    if( uniqueUVs != null ){
                        uniqueUVs.Add( polyUV );
                        geometry.uvFloats.Add( polyUV.x );
                        geometry.uvFloats.Add( polyUV.y );
                    }

                    //replace with new vertex index
                    if( polyEnd ){
                        newVertIndex = -(newVertIndex+1);
                    }
                    polyIndices[i] = newVertIndex;
                    
                    geometry.normalFloats.Add( polyNormal.x );
                    geometry.normalFloats.Add( polyNormal.y );
                    geometry.normalFloats.Add( polyNormal.z );
                }else{
                    uniqueNormals.Add( polyNormal );
                    if( uniqueUVs != null ){
                        uniqueUVs.Add( polyUV );
                        geometry.uvFloats[ vertIndex*2 ] = polyUV.x;
                        geometry.uvFloats[ vertIndex*2+1 ] = polyUV.y;
                    }
                    
                    geometry.normalFloats[ vertIndex*3 ] = polyNormal.x;
                    geometry.normalFloats[ vertIndex*3+1 ] = polyNormal.y;
                    geometry.normalFloats[ vertIndex*3+2 ] = polyNormal.z;
                }
            }

            int armatureVertexCount = originalVertexCount;
            if( geometry.armature != null ){
                foreach( var push in duplicates ){
                    foreach( var boneInfo in geometry.armature.bones ){
                        for( int i=0; i<boneInfo.boneIndices.Count; i++ ){
                            if( boneInfo.boneIndices[i] == push ){
                                boneInfo.boneIndices.Add( armatureVertexCount );
                                boneInfo.boneWeights.Add( boneInfo.boneWeights[i] );
                                break;
                            }
                        }
                    }
                    armatureVertexCount++;
                }
                //reoder bone weights in descending order
                geometry.meshWeights.Capacity = vertexCount*2;

                for( int boneIndex=0; boneIndex<geometry.armature.bones.Count; boneIndex++ ){
                    var boneInfo = geometry.armature.bones[ boneIndex ];
                    for( int i=0; i<boneInfo.boneIndices.Count; i++ ){
                        geometry.meshWeights.Add( new WeightInfo( boneIndex, boneInfo.boneIndices[i], boneInfo.boneWeights[i] ) );
                    }
                }
                // geometry.meshWeights.Sort( (emp1,emp2)=>emp2.weight.CompareTo(emp1.weight) );
            }

            int shapeKeyVertexCount = originalVertexCount;
            if( geometry.shapeKeys.Count > 0 ){
                foreach( var push in duplicates ){
                    foreach( var shapekey in geometry.shapeKeys ){
                        for( int i=0; i<shapekey.indices.Count; i++ ){
                            if( shapekey.indices[i] == push ){
                                shapekey.indices.Add( shapeKeyVertexCount );
                                shapekey.vertexFloats.Add( shapekey.vertexFloats[ i*3 ] );
                                shapekey.vertexFloats.Add( shapekey.vertexFloats[ i*3+1 ] );
                                shapekey.vertexFloats.Add( shapekey.vertexFloats[ i*3+2 ] );
                            }
                        }
                    }
                    shapeKeyVertexCount++;
                }
            }
        }

        private void WriteSubmeshIndices( Geometry geometry ){

            List<int>[] submeshes;
            int indexCounter = 0;

            if( geometry.matPolyIndices == null ){
                submeshes = new List<int>[1];
                var submesh = new List<int>();
                submeshes[0] = submesh;
                UnwrapSubmesh( geometry.polyIndices, submesh, ref indexCounter, false );
            }else{
                int submeshCount = 0;
                for( int i=0; i<geometry.matPolyIndices.Length; i++ ){
                    submeshCount = Mathf.Max( submeshCount, geometry.matPolyIndices[i] );
                }
                submeshCount++;

                submeshes = new List<int>[ submeshCount ];
                for( int i=0; i<submeshes.Length; i++ ){
                    submeshes[i] = new List<int>();
                }
                for( int poly=0; poly<geometry.matPolyIndices.Length; poly++ ){

                    int targetSubmesh = geometry.matPolyIndices[ poly ];
                    var submesh = submeshes[ targetSubmesh ];
                    UnwrapSubmesh( geometry.polyIndices, submesh, ref indexCounter, true );
                }
            }
            while( geometry.materials.Count < submeshes.Length ){
                var matInfo = new MaterialInfo();
                matInfo.name = "Material_"+geometry.materials.Count;
                geometry.materials.Add( matInfo );
            }
            BufferUtil.WriteInt( result, ref index, submeshes.Length );
            for( int i=0; i<submeshes.Length; i++ ){
                var submesh = submeshes[i];
                BufferUtil.WriteString( result, ref index, geometry.materials[i].name );
                BufferUtil.WriteIntList( result, ref index,  submesh );
            }

            if( geometry.clothInfo != null ){
                
                geometry.clothInfo.clothPin = new float[ geometry.vertexFloats.Count/3 ];
                for( int i=0; i<geometry.clothInfo.clothPinIndices.Length; i++ ){
                    var clothPinColorIndex = geometry.clothInfo.clothPinIndices[i];
                    var next = geometry.polyIndices[i];
                    if( next < 0 ) next = -next-1;

                    var clothPinValue = (float)geometry.clothInfo.clothPinColors[ clothPinColorIndex*4 ]; //get RED color
                    geometry.clothInfo.clothPin[ next ] = 1f-clothPinValue;
                }
            }
        }

        public static void UnwrapSubmesh( int[] polyIndices, List<int> submesh, ref int indexCounter, bool stopAtPolyEnd ){
            int first = polyIndices[ indexCounter++ ];
            int second = polyIndices[ indexCounter++ ];
            bool polyEnd = false;
            while( indexCounter < polyIndices.Length ){
                
                int next = polyIndices[ indexCounter++ ];
                if( next < 0 ){
                    next = -next-1;
                    polyEnd = true;
                }
                submesh.Add( first );
                submesh.Add( next );
                submesh.Add( second );
                second = next;
                
                if( polyEnd ){
                    if( stopAtPolyEnd || indexCounter >= polyIndices.Length ){
                        break;
                    }
                    first = polyIndices[ indexCounter++ ];
                    second = polyIndices[ indexCounter++ ];
                    polyEnd = false;
                }
            }
        }

        private void GetGeometryMatPolyIndices( FbxNode geomChild, ref int[] matPolyIndices, ref string materialType ){
            foreach( var matNode in geomChild.Nodes ){
                if( matNode == null ) continue;
                if( matNode.Name == "Materials" ){
                    matPolyIndices = (int[])matNode.Properties[0];
                }else if( matNode.Name == "MappingInformationType" ){
                    materialType = (string)matNode.Value;
                }
            }
        }

        private List<int> IntArrayToIntList( int[] array ){
            var intList = new List<int>();
            for( int i=0; i<array.Length; i++ ){
                intList.Add( array[i] );
            }
            return intList;
        }

        private List<float> DoubleArrayToFloatList( double[] array ){
            var floatList = new List<float>();
            for( int i=0; i<array.Length; i++ ){
                floatList.Add( (float)array[i] );
            }
            return floatList;
        }

        private void GetGeometryVertices( FbxNode geomChild, ref List<float> vertexFloats ){
            vertexFloats = DoubleArrayToFloatList( (double[])geomChild.Properties[0] );
            FixVec3ImportAxis( vertexFloats );
        }

        private void GetClothPin( FbxNode node, ref ClothInfo clothInfo ){
            bool isClothPin = false;
            int[] indices = null;
            double[] colors = null;
            foreach( var normChild in node.Nodes ){
                if( normChild == null ) continue;
                if( normChild.Name == "Name"){
                    if( (string)normChild.Value == "cloth_pin" ) isClothPin = true;
                }else if( normChild.Name == "Colors" ){
                    colors = (double[])normChild.Properties[0];
                }else if( normChild.Name == "ColorIndex" ){
                    indices = (int[])normChild.Properties[0];
                }
            }
            if( isClothPin && indices != null && colors != null ){
                clothInfo.clothPinIndices = indices;
                clothInfo.clothPinColors = colors;
            }
        }
        
        private void GetNormals( FbxNode node, ref double[] normalDoubles, ref string normalType ){
            foreach( var normChild in node.Nodes ){
                if( normChild == null ) continue;
                if( normChild.Name == "Normals"){
                    normalDoubles = (double[])normChild.Properties[0];
                    FixVec3ImportAxis( normalDoubles );
                }else if( normChild.Name == "MappingInformationType" ){
                    normalType = (string)normChild.Value;
                }
            }
        }

        private void GetUVs( FbxNode node, ref double[] uvDoubles, ref int[] uvIndices ){
            foreach( var uvChild in node.Nodes ){
                if( uvChild == null ) continue;
                if( uvChild.Name == "UV"){
                    uvDoubles = (double[])uvChild.Properties[0];
                }else if( uvChild.Name == "UVIndex" ){
                    uvIndices = (int[])uvChild.Properties[0];
                }
            }
        }

        private void PrepareDeformerObject( FbxNode objChild, Manifest manifest ){
            var deformerType = (string)objChild.Properties[2];

            if( deformerType == "Cluster" ) PrepareDeformerCluster( objChild, manifest );
            else
            if( deformerType == "Skin" ) PrepareDeformerArmature( objChild, manifest );
            else
            if( deformerType == "BlendShapeChannel" ) PrepareDeformerChannel( objChild, manifest );
            else
            if( deformerType == "BlendShape" ) PrepareDeformerBlendShape( objChild, manifest );
        }

        private void PrepareDeformerBlendShape( FbxNode objChild, Manifest manifest ){
            var shapeKey = new ShapeKey();
            PrepareObjectHeader( Read.SHAPEKEY, objChild, manifest, shapeKey, ref shapeKey.name );
        }

        private void PrepareDeformerChannel( FbxNode objChild, Manifest manifest ){
            var shapeChannel = new ShapeKeyChannel();
            PrepareObjectHeader( Read.SHAPE_CHANNEL, objChild, manifest, shapeChannel, ref shapeChannel.name );
        }

        private void PrepareMaterialObject( FbxNode objChild, Manifest manifest ){
            var materialInfo = new MaterialInfo();
            PrepareObjectHeader( Read.MATERIAL, objChild, manifest, materialInfo, ref materialInfo.name );
        }

        private void PrepareDeformerArmature( FbxNode objChild, Manifest manifest ){
            var armature = new ArmatureInfo();
            PrepareObjectHeader( Read.ARMATURE_INFO, objChild, manifest, armature, ref armature.name );
        }

        private void PrepareDeformerCluster( FbxNode objChild, Manifest manifest ){
            var boneInfo = new BoneInfo();
            PrepareObjectHeader( Read.BONE_INFO, objChild, manifest, boneInfo, ref boneInfo.name );

            foreach( var modelChild in objChild.Nodes ){
                if( modelChild == null ) continue;
                if( modelChild.Name == "Indexes" ){
                    boneInfo.boneIndices = IntArrayToIntList( (int[])modelChild.Properties[0] );
                }else if( modelChild.Name == "Weights" ){
                    boneInfo.boneWeights = DoubleArrayToFloatList( (double[])modelChild.Properties[0] );
                }else if( modelChild.Name == "TransformLink" ){
                    boneInfo.transformLink = (double[])modelChild.Properties[0];
                    int m_i = 0;
                    var matrix = new Matrix4x4(
                        new Vector4( (float)boneInfo.transformLink[ m_i++ ], (float)boneInfo.transformLink[ m_i++ ], (float)boneInfo.transformLink[ m_i++ ], (float)boneInfo.transformLink[ m_i++ ] ),
                        new Vector4( (float)boneInfo.transformLink[ m_i++ ], (float)boneInfo.transformLink[ m_i++ ], (float)boneInfo.transformLink[ m_i++ ], (float)boneInfo.transformLink[ m_i++ ] ),
                        new Vector4( (float)boneInfo.transformLink[ m_i++ ], (float)boneInfo.transformLink[ m_i++ ], (float)boneInfo.transformLink[ m_i++ ], (float)boneInfo.transformLink[ m_i++ ] ),
                        new Vector4( (float)boneInfo.transformLink[ m_i++ ], (float)boneInfo.transformLink[ m_i++ ], (float)boneInfo.transformLink[ m_i++ ], (float)boneInfo.transformLink[ m_i++ ] )
                    );
                    
                    Vector3 pos = FixVec3ImportAxis( matrix.GetPosition() );
                    //apply -90 pitch rotation to position
                    float temp = pos.y;
                    pos.y = pos.z;
                    pos.z = temp;
                    
                    Vector3 forward = FixVec3ImportAxis( matrix.GetColumn(2) );
                    Vector3 up = FixVec3ImportAxis( matrix.GetColumn(1) );
                    forward.y = -forward.y;
                    up.y = -up.y;
                    Quaternion rot = Quaternion.Euler( -90.0f, 0.0f, 0.0f )*Quaternion.LookRotation( forward, up );

                    Vector3 scale = matrix.GetScale();
                    scale *= blenderToUnityScale;
                    matrix.SetTRS( pos, rot, scale );

                    m_i = 0;
                    for( int i=0; i<4; i++ ){
                        var c = matrix.GetColumn(i);
                        boneInfo.transformLink[m_i++] = c.x;
                        boneInfo.transformLink[m_i++] = c.y;
                        boneInfo.transformLink[m_i++] = c.z;
                        boneInfo.transformLink[m_i++] = c.w;
                    }
                }
            }
        }
        
        private void PrepareModelObject( FbxNode objChild, Manifest manifest ){
            var modeltype = (string)objChild.Properties[2];
            if( modeltype == "Mesh" ){
                PrepareModelTransform( Read.MODEL, objChild, manifest );
            }else if( modeltype == "LimbNode" ){
                PrepareModelTransform( Read.BONE_MODEL, objChild, manifest );
            }else if( modeltype == "Null" ){
                PrepareModelTransform( Read.ARMATURE_MODEL, objChild, manifest );
            }
        }

        private void PrepareObjectHeader( Read type, FbxNode objChild, Manifest manifest, object obj, ref string name ){
            var id = (long)objChild.Properties[0];
            name = (string)objChild.Properties[1];
            name = name.Substring( name.LastIndexOf(':')+1 );
            manifest.elements.Add( new Element( id, type, obj ) );
        }

        private void PrepareModelTransform( Read transformType, FbxNode objChild, Manifest manifest ){
            var model = new ModelInfo();
            PrepareObjectHeader( transformType, objChild, manifest, model, ref model.name );

            //defaults
            model.muscleTemplate.pitch = 75f;
            model.muscleTemplate.yaw = 75f;
            model.muscleTemplate.roll = 75f;
            PrepareProperties70Transform( objChild, model );
        }

        private void PrepareProperties70Transform( FbxNode objChild, ModelInfo model ){
            
            foreach( var modelChild in objChild.Nodes ){
                if( modelChild == null ) continue;
                if( modelChild.Name == "Properties70" ){
                    foreach( var p in modelChild.Nodes ){
                        if( p == null ) continue;
                        switch( (string)p.Properties[0] ){
                        case "Lcl Translation":
                            model.position = FixVec3ImportAxis( new Vector3(
                                (float)(double)p.Properties[4],
                                (float)(double)p.Properties[5],
                                (float)(double)p.Properties[6]
                            ) );
                            break;
                        case "Lcl Rotation":
                            model.euler = FixImportEuler( new Vector3(
                                (float)(double)p.Properties[4],
                                (float)(double)p.Properties[5],
                                (float)(double)p.Properties[6]
                            ) ).eulerAngles;
                            break;
                        case "Lcl Scaling":
                            model.scale = new Vector3(
                                (float)(double)p.Properties[4],
                                (float)(double)p.Properties[5],
                                (float)(double)p.Properties[6]
                            )*blenderToUnityScale;
                            break;
                        case "stiffness":
                            var number = p.Properties[4];
                            model.dynamicBoneInfo.stiffness = ParseNumber( p.Properties[4] );
                            break;
                        case "damping":
                            model.dynamicBoneInfo.damping = ParseNumber( p.Properties[4] );
                            break;
                        case "elasticity":
                            model.dynamicBoneInfo.elasticity = ParseNumber( p.Properties[4] );
                            break;
                        case "radius":
                            model.dynamicBoneInfo.radius = ParseNumber( p.Properties[4] );
                            break;
                        case "gravity":
                            model.dynamicBoneInfo.gravity = ParseNumber( p.Properties[4] );
                            break;
                        case "collider":
                            model.dynamicBoneInfo.collider = (string)p.Properties[4];
                            break;
                        case "mass":
                            model.muscleTemplate.mass = ParseNumber( p.Properties[4] );
                            break;
                        }
                    }
                }
            }
        }

        private float ParseNumber( object number ){
            if( number.GetType() == typeof(int) ){
                return (float)(int)number;
            }else{
                return (float)(double)number;
            }
        }
    }
}

}