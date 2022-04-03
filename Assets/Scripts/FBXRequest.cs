using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fbx;
using System.IO;
using Unity.Jobs;
using Unity.Collections;
using System.Text.RegularExpressions;



namespace viva{

public partial class FBXRequest: SpawnableImportRequest{

    public SerializedFBX serializedFBX;
    public FBX lastSpawnedFBX { get; private set; }
    public bool doNotResize = false;
    private bool enabledThumbnailGeneration;

    
    public struct TransformEntry{
        public Transform transform;
        public Read type;
        private DynamicBoneInfo dynamicBoneInfo;

        public TransformEntry( Transform _transform, Read _type ){
            transform = _transform;
            type = _type;
            dynamicBoneInfo = new DynamicBoneInfo();
        }
    }

    public class Manifest{
        public Dictionary<short,TransformEntry> transformEntry = new Dictionary<short,TransformEntry>();
        public List<DynamicBone> dynamicBones = new List<DynamicBone>();
        public Dictionary<Transform,Model> modelDictionary = new Dictionary<Transform,Model>();
        public List<CapsuleCollider> clothCapsules = new List<CapsuleCollider>();
    }

    public class SerializedFBX: VivaDisposable{
        public readonly byte[] data;
        public readonly int serializedFBXLength;

        public SerializedFBX( byte[] _data, int _serializedFBXLength ){
            data = _data;
            serializedFBXLength = _serializedFBXLength;
        }
    }

    // private class ReimportData{
    //     public string name;
    //     public string[] ragdollProfileBoneNames;
    //     public List<TextureBinding> textureBindings;

    //     public ReimportData( Model model ){
    //         name = model.name;
    //         if( model.profile != null ){
    //             ragdollProfileBoneNames = new string[ model.profile.boneInfos.Length ];
    //             for( int i=0; i<model.profile.boneInfos.Length; i++ ){
    //                 var refBone = model.profile.boneInfos[i];
    //                 if( refBone != null && refBone.transform != null ){
    //                     ragdollProfileBoneNames[i] = refBone.transform.name;
    //                 }
    //             }
    //         }
    //         textureBindings = model.textureBindingGroup.DuplicateList();
    //         foreach( var texturebinding in textureBindings ){
    //             texturebinding.usage.Increase(); //reuse for next reimport
    //         }
    //     }
    // }

    public static readonly int DEFAULT_MAX_BUFFER_SIZE = 10*1000000; //MB

    public readonly FBXContent content;
    public readonly int maxBufferSize;


    public FBXRequest( string _filepath, FBXContent _content, int _maxBufferSize=-1 ):base( _filepath, ImportRequestType.FBX ){
        content = _content;
        maxBufferSize = _maxBufferSize<=0 ? DEFAULT_MAX_BUFFER_SIZE : _maxBufferSize;
    }

    public override void OnCreateMenuSelected(){
        if( lastSpawnedFBX ){
            // foreach( var model in lastSpawnedFBX.rootModels ){
            //     model.OnCreateMenuSelected();
            // }
        }
        GameUI.main.createMenu.DisplayVivaObjectInfo<FBXRequest>( this, this );
        GameUI.main.createMenu.DisplayCreateButton();
    }
    public override void OnCreateMenuDeselected(){
        // if( lastSpawnedFBX ){
        //     foreach( var model in lastSpawnedFBX.rootModels ){
        //         model.OnCreateMenuDeselected();
        //     }
        // }
    }
    
    protected override string OnImport(){
        return null;
    }

    private void SetupMultiThreading( out NativeArray<byte> buffer, out NativeArray<byte> filepathBuffer, out JobHandle jobHandle ){
        buffer = new NativeArray<byte>( maxBufferSize, Allocator.Persistent, NativeArrayOptions.UninitializedMemory );
        filepathBuffer = Tools.StringToUTF8NativeArray( filepath );
        var readFBX = new ExecuteReadFbx( buffer, filepathBuffer, content );
        jobHandle = readFBX.Schedule();
    }

    protected override IEnumerator OnPreload(){
        SetupMultiThreading( out NativeArray<byte> buffer, out NativeArray<byte> filepathBuffer, out JobHandle jobHandle );
        while( true ){
            if( !jobHandle.IsCompleted ){
                yield return null;
            }
            break;                                      
        }
        CleanupMultiThreading( buffer, filepathBuffer, jobHandle );
    }

    private void CleanupMultiThreading( NativeArray<byte> buffer, NativeArray<byte> filepathBuffer, JobHandle jobHandle ){
        jobHandle.Complete();
        filepathBuffer.Dispose();
        var data = BufferUtil.ExtractHeaderError( buffer, out string error );
        serializedFBX = new SerializedFBX( data, BufferUtil.GetManifestHeader( (int)BufferUtil.Header.READ_LENGTH, buffer ) );
        Debug.Log("+FBX "+filepath+" [buffered "+BufferUtil.GetManifestHeader( (int)BufferUtil.Header.READ_LENGTH, buffer )+"/"+maxBufferSize+" bytes]");
        buffer.Dispose();
        preloadError = error;
    }

    protected override void OnPreloadInstant(){
        SetupMultiThreading( out NativeArray<byte> buffer, out NativeArray<byte> filepathBuffer, out JobHandle jobHandle );
        while( !jobHandle.IsCompleted ){                                    
        }
        CleanupMultiThreading( buffer, filepathBuffer, jobHandle );
    }

    protected override void OnSpawnInstant( SpawnProgress progress){
        var fbx = DeserializeFBXData( new ByteBufferedReader( serializedFBX.data ) );

        lastSpawnedFBX = fbx;
        if( !doNotResize ) FirstLoadResize( fbx );
        FinishSpawning( progress, fbx, null );
        if( enabledThumbnailGeneration ){
            foreach( var rootModel in fbx.rootModels ){
                rootModel._InternalOnGenerateThumbnail();
            }
        }
    }

    protected override IEnumerator OnSpawn( SpawnProgress progress ){
        OnSpawnInstant( progress );
        yield break;
    }

    
    private void FirstLoadResize( FBX fbx ){
        float smallestHeight = Mathf.Infinity;
        float largestHeight = 0;
        foreach( var model in fbx.rootModels ){
            if( model != null && model._internalIsRoot ){
                var bounds = model.bounds;
                if( !bounds.HasValue ) continue;
                smallestHeight = Mathf.Min( smallestHeight, bounds.Value.size.y );
                largestHeight = Mathf.Max( largestHeight, bounds.Value.size.y );
            }
        }
        if( smallestHeight <= 1.6f ) return;
        foreach( var model in fbx.rootModels ){
            if( model != null && model._internalIsRoot ){
                var bounds = model.bounds;
                if( !bounds.HasValue ) continue;
                model.Resize( 1.6f*(bounds.Value.size.y/largestHeight) );
            }
        }
    }
    protected void Reimport(){
        //build carry over data to apply to import once finished
        // List<ReimportData> reimportData = new List<ReimportData>();
        // foreach( var model in fbx.modelGroups ){
        //     if( model != null ){
        //         reimportData.Add( new ReimportData( model ) );
        //     }
        // }
        // ClearFBX();
        // Viva.main.StartCoroutine( ExecuteImportFBX( delegate{
        //     foreach( var model in fbx ){
        //         var model = model;
        //         if( model != null ){
        //             foreach( var data in reimportData ){
        //                 if( data.name == model.name ){
                            
        //                     foreach( var textureBinding in data.textureBindings ){
        //                         if( model.textureBindings.AutoBind( textureBinding ) ) break;
        //                     }
        //                     model.textureBindings.Apply();
        //                     RagdollProfile reimportRagdollProfile = null;
        //                     try{
        //                         reimportRagdollProfile = new RagdollProfile( data.ragdollProfileBoneNames, new string[0], model );
        //                     }catch( System.Exception e ){
        //                         Debugger.LogError( e.ToString() );
        //                     }
        //                     if( reimportRagdollProfile != null ){
        //                         if( model.AttemptSetRagdollProfile( reimportRagdollProfile, out string error ) ){
        //                             Debugger.Log("Successfully rebinded "+model.name);
        //                         }else{
        //                             Debugger.Log("Could not rebind "+model.name+" "+error);
        //                         }
        //                     }
        //                     break;
        //                 }
        //             }
        //         }
        //     }

        // } ) );
    }

    public override void EnableThumbnailGenerationOnEdit(){
        enabledThumbnailGeneration = true;
    }

    public override void _InternalOnGenerateThumbnail(){
        thumbnail.texture = BuiltInAssetManager.main.defaultFBXThumbnail;
    }
    public override string GetInfoHeaderText(){
        
        if( !lastSpawnedFBX ){
            return "none spawned";
        }
        string s = "";
        int modelCount = 0;
        int animationCount = 0;
        foreach( var model in lastSpawnedFBX.rootModels ){
            if( model != null ) modelCount++;
            animationCount += model.animations.Length;
        }
        s += CreateMenu.InfoTypeToColor( CreateMenu.InfoColor.MODEL )+modelCount+" model(s)</color>\n";
        s += CreateMenu.InfoTypeToColor( CreateMenu.InfoColor.ANIMATION )+animationCount+" animation(s)</color>\n";
        return s;
    }
    public override string GetInfoBodyContentText(){
        string s = "";
        return s;
    }

    private Animation[] ReadAnimations( ByteBufferedReader reader, Mesh sourceMesh ){
    
        int animationCount = reader.ReadNextInt();
        Animation[] animations;
        if( animationCount > 0 ){
            animations = new Animation[ animationCount ];
            int baseFramerate = reader.ReadNextInt();
            for( int i=0; i<animationCount; i++ ){
                string animationName = reader.ReadNextString();
                int channelCount = reader.ReadNextInt();
                float duration = reader.ReadNextFloat();

                var channels = new AnimationChannel[ channelCount ];
                for( int j=0; j<channels.Length; j++ ){
                    string targetChannelName = reader.ReadNextString();
                    var channelType = (AnimationChannel.Channel)reader.ReadNextInt();
                    int frameSetCount = reader.ReadNextInt();

                    var frameSets = new AnimationFrameSet[ frameSetCount ];
                    for( int k=0; k<frameSets.Length; k++ ){
                        var frames = reader.ReadNextFloatArray();
                        var tweens = reader.ReadNextByteArray();

                        frameSets[k] = new AnimationFrameSet( frames );
                    }
                    channels[j] = new AnimationChannel( channelType, targetChannelName, targetChannelName.GetHashCode(), frameSets );
                }
                animations[i] = new Animation( animationName, channels, duration, baseFramerate, this );
            }
        }else{
            animations = new Animation[0];
        }
        return animations;
    }

    private FBX DeserializeFBXData( ByteBufferedReader reader ){
        //load geometry models
        int geometries = reader.ReadNextInt();
        var fbxContainer = new GameObject( filepath ).transform;
        var fbx = fbxContainer.gameObject.AddComponent<FBX>();

        var manifest = new Manifest();

        for( int i=0; i<geometries; i++ ){
            byte geometryType = reader.ReadNextByte();
            if( geometryType == 0 ){
                var model = ReadModelGeometry( reader, fbx, manifest );
                model.rootTransform.parent = fbxContainer;
                fbx._InternalAddRoot( model );
                manifest.modelDictionary.Add( model.rootTransform, model );
            }else{
                ReadCollider( reader, manifest );
            }
        }

        //add capsules to cloth
        foreach( var root in fbx.rootModels ){
            if( root.cloth ) root.cloth.capsuleColliders = manifest.clothCapsules.ToArray();
        }

        BuildLODs( fbx );
        ReadParentingHierarchy( reader, manifest, fbx );
        
        foreach( var dynamicBone in manifest.dynamicBones ){
            if( dynamicBone._internalParentBoneId > -1 ){
                var parentBone = manifest.transformEntry[ dynamicBone._internalParentBoneId ].transform;
                DynamicBoneColliderCapsule colliderBase = parentBone.GetComponent<DynamicBoneColliderCapsule>();
                if( colliderBase ) dynamicBone.colliders.Add( colliderBase );
            }
        }

        return fbx;
    }

    private void BuildLODs( FBX fbx ){
        for( int i=fbx.rootModels.Count; i-->0; ){
            var model = fbx.rootModels[i];
            if( !model._internalIsRoot ) continue;

            if( model.name.EndsWith("_lod0") ){
                var rootName = model.name.Substring( 0, model.name.Length-5 );
                var lodModels = new List<Tuple<int,Model>>();
                foreach( var candidate in fbx.rootModels ){
                    if( candidate == model ) continue;
                    if( !candidate._internalIsRoot ) continue;
                    var prefixStart = candidate.name.IndexOf( "_lod" );
                    if( prefixStart == -1 ) continue;

                    prefixStart += 4;
                    if( !System.Int32.TryParse( candidate.name.Substring( prefixStart, candidate.name.Length-prefixStart ), out int lodLevel ) ) continue;
                    if( lodLevel <= 0 ) continue;

                    lodModels.Add( new Tuple<int, Model>(lodLevel,candidate) );
                }
                if( lodModels.Count == 0 ) continue;
                var modelBounds = model.bounds;
                if( !modelBounds.HasValue ) continue;

                foreach( var lodModel in lodModels ){
                    model._InternalAddChild( lodModel._2 );
                    lodModel._2._internalIsRoot = false;
                    lodModel._2.rootTransform.SetParent( model.rootTransform, true );
                }
                lodModels.Add( new Tuple<int, Model>(0,model) );
                lodModels.Sort( (a,b)=>a._1.CompareTo( b._1 ) );

                var lodGroup = model.rootTransform.gameObject.AddComponent<LODGroup>();
                
                var screenSize = Mathf.Max( Mathf.Max( modelBounds.Value.extents.x, modelBounds.Value.extents.y ), modelBounds.Value.extents.z )*2;
                var lods = new LOD[ lodModels.Count ];
                for( int j=0; j<lodModels.Count; j++ ){
                    var lodModel = lodModels[j];
                    float screenRatio = (float)j/lodModels.Count;
                    screenRatio = Mathf.Pow( 0.1f+screenRatio*0.9f, Tools.RemapClamped( 0.1f, 5f, 0.1f, 0.2f, screenSize ) );
                    
                    lods[j] = new LOD( 1.0f-screenRatio, new Renderer[]{ lodModels[j]._2.renderer } );
                }
                lods[ lodModels.Count-1 ].screenRelativeTransitionHeight = Tools.RemapClamped( 0.1f, 5f, 0.005f, 0.035f, screenSize );
                lodGroup.SetLODs( lods );
                lodGroup.size = screenSize;
                lodGroup.localReferencePoint = modelBounds.Value.center/model.renderer.transform.lossyScale.x;
                
            }
        }
    }

    private void ReadParentingHierarchy( ByteBufferedReader reader, Manifest manifest, FBX fbx ){
        var blenderToUnityRot = Quaternion.Euler( -90.0f, 0.0f, 0.0f );
        var unityToBlenderRot = Quaternion.Euler( 90.0f, 0.0f, 0.0f );
        int modelParents = reader.ReadNextInt();
        for( int i=0; i<modelParents; i++ ){
            var child = manifest.transformEntry[ reader.ReadNextShort() ];
            var parent = manifest.transformEntry[ reader.ReadNextShort() ];

            //parent model hierarchy
            if( manifest.modelDictionary.TryGetValue( child.transform, out Model rootChildModel ) ){
                if( manifest.modelDictionary.TryGetValue( parent.transform, out Model rootParentModel ) ){
                    rootChildModel._internalIsRoot = false;
                    rootParentModel._InternalAddChild( rootChildModel );
                }
            }
            if( parent.type == Read.ARMATURE_MODEL ){
                //armatures apply special case offsets
                if( child.type == Read.COLLIDER ){
                    child.transform.SetParent( parent.transform, false );
                    // child.transform.localRotation = blenderToUnityRot*child.transform.localRotation;
                    child.transform.localPosition /= ExecuteReadFbx.blenderToUnityScale;
                    child.transform.localScale /= ExecuteReadFbx.blenderToUnityScale;

                    // child.transform.localRotation *= unityToBlenderRot;
                    var localScale = unityToBlenderRot*child.transform.localScale;
                    localScale.x = Mathf.Abs( localScale.x );
                    localScale.y = Mathf.Abs( localScale.y );
                    localScale.z = Mathf.Abs( localScale.z );
                    child.transform.localScale = localScale;

                }else if( child.type == Read.MODEL ){
                    var origRootChildParent = fbx.FindParentModel( child.transform );
                    child.transform.SetParent( parent.transform, true );
                    //special case, if object is a cloth, then remove from root entirely and add
                    if( rootChildModel == null && origRootChildParent != null && origRootChildParent.cloth ){
                        rootChildModel = origRootChildParent;
                    }
                    if( rootChildModel != null ){
                        rootChildModel._internalIsRoot = false;
                        Model parentArmatureModel = fbx.FindParentModel( parent.transform );
                        if( parentArmatureModel != null ){
                            parentArmatureModel._InternalAddChild( rootChildModel );
                        }
                    }
                }
            }else if( parent.type == Read.MODEL ){
                //normal model parenting does not apply special case offsets
                child.transform.SetParent( parent.transform, false );
                child.transform.localRotation = blenderToUnityRot*child.transform.localRotation;
                child.transform.localPosition = blenderToUnityRot*child.transform.localPosition/ExecuteReadFbx.blenderToUnityScale;
                child.transform.localScale /= ExecuteReadFbx.blenderToUnityScale;
                //the scale is not rotating when imported???

                if( child.type == Read.COLLIDER ){
                    if( manifest.modelDictionary.TryGetValue( parent.transform, out Model parentModel ) ){
                        parentModel._InternalAddCollider( child.transform.GetComponent<Collider>() );
                    }
                }
            }
        }
    }
    
    private void ReadCollider( ByteBufferedReader reader, Manifest manifest ){
        var colliderTransform = reader.ReadNewGameObject( manifest, Read.COLLIDER ).transform;
        PhysicsType physicsType = (PhysicsType)reader.ReadNextByte();
        ColliderType colliderType = (ColliderType)reader.ReadNextByte();

        if( physicsType == PhysicsType.JIGGLE ){
            switch( colliderType ){
            case ColliderType.SPHERE:
                var sc = colliderTransform.gameObject.AddComponent<DynamicBoneColliderCapsule>();
                sc.center = reader.ReadNextFloatAsVector3();
                sc.radius = reader.ReadNextFloat();
                sc.gameObject.layer = WorldUtil.transparentFX;
                break;
            case ColliderType.CAPSULE:
                var cc = colliderTransform.gameObject.AddComponent<DynamicBoneColliderCapsule>();
                cc.direction = (DynamicBoneColliderCapsule.Direction)reader.ReadNextByte();
                cc.radius = reader.ReadNextFloat();
                cc.height = reader.ReadNextFloat();
                cc.gameObject.layer = WorldUtil.transparentFX;
                cc.height += cc.radius*2;
                break;
            }
        }else{
            switch( colliderType ){
            case ColliderType.CAPSULE:
                var cc = colliderTransform.gameObject.AddComponent<CapsuleCollider>();
                cc.isTrigger = physicsType == PhysicsType.GRAB;
                cc.direction = (int)reader.ReadNextByte(); 
                cc.radius = reader.ReadNextFloat();
                cc.height = reader.ReadNextFloat();
                cc.height += cc.radius*2;

                if( physicsType == PhysicsType.CLOTH ) manifest.clothCapsules.Add( cc );
                break;
            case ColliderType.CONCAVE:
                BuildPolygonCollider( reader, colliderTransform, true );
                break;
            case ColliderType.CONVEX:
                BuildPolygonCollider( reader, colliderTransform, false );
                break;
            case ColliderType.BOX:
                var bc = colliderTransform.gameObject.AddComponent<BoxCollider>();
                bc.center = reader.ReadNextFloatAsVector3();
                bc.size = reader.ReadNextFloatAsVector3();
                break;
            case ColliderType.SPHERE:
                var sc = colliderTransform.gameObject.AddComponent<SphereCollider>();
                sc.center = reader.ReadNextFloatAsVector3();
                sc.radius = reader.ReadNextFloat();
                break;
            }

            switch( physicsType ){
            case PhysicsType.RIGID:
                colliderTransform.gameObject.layer = WorldUtil.itemsLayer;
                break;
            case PhysicsType.GRAB:
                colliderTransform.gameObject.layer = WorldUtil.grabbablesLayer;
                colliderTransform.gameObject.AddComponent<Grabbable>();
                colliderTransform.GetComponent<Collider>().isTrigger = true;
                break;
            case PhysicsType.CLOTH:
                colliderTransform.gameObject.layer = WorldUtil.cameraLayer;
                break;
            case PhysicsType.ZONE:
                colliderTransform.gameObject.layer = WorldUtil.zonesLayer;
                colliderTransform.gameObject.AddComponent<Zone>();
                colliderTransform.GetComponent<Collider>().isTrigger = true;
                break;
            case PhysicsType.TRIGGER:
                colliderTransform.gameObject.layer = WorldUtil.objectDetectorLayer;
                colliderTransform.GetComponent<Collider>().isTrigger = true;
                break;
            }
        }
    }

    private void BuildPolygonCollider( ByteBufferedReader reader, Transform colliderTransform, bool isConcave ){
        var mc = colliderTransform.gameObject.AddComponent<MeshCollider>();
        mc.cookingOptions = MeshColliderCookingOptions.CookForFasterSimulation|MeshColliderCookingOptions.WeldColocatedVertices;
        if( isConcave ){
            mc.convex = false;
        }else{
            mc.convex = true;
        }
        var colliderMesh = new Mesh();
        var vertices = reader.ReadNextFloatArrayAsVector3Array();
        colliderMesh.vertices = vertices;
        colliderMesh.SetIndices( reader.ReadNextIntArray(), MeshTopology.Triangles, 0 );
        mc.sharedMesh = colliderMesh;
    }

    private Model ReadModelGeometry( ByteBufferedReader reader, FBX fbx, Manifest manifest ){
        GameObject geo = reader.ReadNewGameObject( manifest, Read.MODEL );
        
        var vertices = reader.ReadNextFloatArrayAsVector3Array();
        //if empty gameobject
        if( vertices.Length == 0 ){
            return new Model( geo.name, geo.transform, (MeshRenderer)null, null, new Animation[0], this );
        }
        
        var mesh = new Mesh();
        mesh.name = geo.name;
        //mesh data        
        mesh.vertices = vertices;
        mesh.normals = reader.ReadNextFloatArrayAsVector3Array();
        if( reader.ReadNextInt() == 1 ){
            mesh.uv = reader.ReadNextFloatArrayAsVector2Array();
        }else{
            //no uv specified
        }

        mesh.subMeshCount = reader.ReadNextInt();
        var materials = new Material[ mesh.subMeshCount ];
        for( int submesh=0; submesh<mesh.subMeshCount; submesh++ ){
            var material = new Material( BuiltInAssetManager.main.defaultModelMaterials[0] );   //default is opaque
            material.name = reader.ReadNextString();
            materials[ submesh ] = material;
            var intArray = reader.ReadNextIntArray();
            mesh.SetIndices( intArray, MeshTopology.Triangles, submesh );
        }
        ReadShapeKeys( reader, mesh );

        Model model;
        //armature
        var meshType = (MeshType)reader.ReadNextByte();
        switch( meshType ){
        case MeshType.SKINNED:
            {            
                model = ReadSkinnedMesh( reader, mesh, materials, geo, manifest );
            }
            break;
        case MeshType.CLOTH:
            {
                GameObject container = new GameObject( mesh.name+"_container" );
                container.SetActive( false );

                geo.transform.SetParent( container.transform, false );
                
                ReadArmature( reader, out MuscleTemplate[] muscleTemplates, container, manifest, out GameObject armature, out Matrix4x4[] bindPoses, out Transform[] bones );

                var smr = geo.AddComponent<SkinnedMeshRenderer>();
                smr.renderingLayerMask = 1+2+4+8+16+32+64+128;
                // smr.bones = bones;
                smr.sharedMesh = mesh;
                smr.rootBone = geo.transform;

                smr.materials = materials;
                //rename and remove (Instance) suffix
                foreach( var mat in smr.materials ){
                    mat.name = mat.name.Replace( " (Instance)", "" );
                }

                var clothPin = reader.ReadNextFloatArray();
                var clothLogic = geo.AddComponent<ClothLogic>();
                var cloth = geo.AddComponent<Cloth>();
                clothLogic.cloth = cloth;
                clothLogic.smr = smr;

                cloth.worldAccelerationScale = 0.5f;
                cloth.worldVelocityScale = 0.5f;
                cloth.useTethers = false;
                cloth.bendingStiffness = reader.ReadNextFloat();
                cloth.stretchingStiffness = reader.ReadNextFloat();
                cloth.damping = reader.ReadNextFloat();
                var coefficients = new ClothSkinningCoefficient[ clothPin.Length ];
                var meshSize = mesh.bounds.extents.magnitude;
                for( int i=0; i<clothPin.Length; i++ ){
                    coefficients[i].collisionSphereDistance = clothPin[i]*meshSize*10;
                    coefficients[i].maxDistance = clothPin[i]*meshSize;
                }
                cloth.coefficients = coefficients;

                model = new Model( geo.name, muscleTemplates, container.transform, smr, armature.transform, new Animation[0], this, cloth );
            }
            break;
        case MeshType.STATIC:
            {
                //unskinned mesh with blendshapes
                if( mesh.blendShapeCount > 0 ){
                    var smr = geo.AddComponent<SkinnedMeshRenderer>();
                    smr.sharedMesh = mesh;
                    smr.renderingLayerMask = 1+2+4+8+16+32+64+128;
                    mesh.RecalculateTangents(); //Unity bug fix, blendshapes wont work without bones and tangents
                    smr.rootBone = geo.transform;

                    model = new Model( geo.name, null, geo.transform, smr, null, new Animation[0], this, null );
                    
                    smr.materials = materials;
                    //rename and remove (Instance) suffix
                    foreach( var mat in smr.materials ){
                        mat.name = mat.name.Replace( " (Instance)", "" );
                    }
                }else{
                    //unskinned mesh without blendshapes
                    var mf = geo.AddComponent<MeshFilter>();
                    mf.mesh = mesh;
                    var mr = geo.AddComponent<MeshRenderer>();
                    mr.renderingLayerMask = 1+2+4+8+16+32+64+128;

                    model = new Model( geo.name, geo.transform, mr, mf, new Animation[0], this );
                    
                    mr.materials = materials;
                    //rename and remove (Instance) suffix
                    foreach( var mat in mr.materials ){
                        mat.name = mat.name.Replace( " (Instance)", "" );
                    }
                }
            }
            break;
        default:
            throw new System.Exception("Unidenfified MeshType in fbx "+meshType);
        }
        return model;
    }

    private Model ReadSkinnedMesh( ByteBufferedReader reader, Mesh mesh, Material[] materials, GameObject geo, Manifest manifest ){
        GameObject container = new GameObject( mesh.name );
        container.SetActive( false );
        container.transform.position = geo.transform.position;
        container.transform.rotation = geo.transform.rotation;

        geo.transform.SetParent( container.transform, true );
        
        ReadArmature( reader, out MuscleTemplate[] muscleTemplates, container, manifest, out GameObject armature, out Matrix4x4[] bindPoses, out Transform[] bones );

        //bone weights
        var boneWeightCounter = new int[ mesh.vertices.Length ];
        var boneWeights = new BoneWeight[ mesh.vertices.Length ];
        for( int j=0; j<boneWeights.Length; j++ ) boneWeights[j] = new BoneWeight();

        int meshWeights = reader.ReadNextInt();
        for( int j=0; j<meshWeights; j++ ){
            int vertexIndex = reader.ReadNextInt();
            int boneIndex = reader.ReadNextInt();
            float weight = reader.ReadNextFloat();
            int count = boneWeightCounter[ vertexIndex ];
            switch( count ){
            case 0:
                boneWeights[ vertexIndex ].boneIndex0 = boneIndex;
                boneWeights[ vertexIndex ].weight0 = weight;
                break;
            case 1:
                boneWeights[ vertexIndex ].boneIndex1 = boneIndex;
                boneWeights[ vertexIndex ].weight1 = weight;
                break;
            case 2:
                boneWeights[ vertexIndex ].boneIndex2 = boneIndex;
                boneWeights[ vertexIndex ].weight2 = weight;
                break;
            default:
                boneWeights[ vertexIndex ].boneIndex3 = boneIndex;
                boneWeights[ vertexIndex ].weight3 = weight;
                break;
            }
            boneWeightCounter[ vertexIndex ]++;
        }
        for( int j=0; j<boneWeights.Length; j++ ){
            var bw = boneWeights[j];
            float sum = Mathf.Max( Mathf.Epsilon, bw.weight0+bw.weight1+bw.weight2+bw.weight3 );
            bw.weight0 = bw.weight0/sum;
            bw.weight1 = bw.weight1/sum;
            bw.weight2 = bw.weight2/sum;
            bw.weight3 = bw.weight3/sum;
            boneWeights[j] = bw;
        }
        mesh.boneWeights = boneWeights;
        mesh.bindposes = bindPoses;
        
        var smr = geo.AddComponent<SkinnedMeshRenderer>();
        smr.renderingLayerMask = 1+2+4+8+16+32+64+128;
        smr.bones = bones;
        smr.sharedMesh = mesh;
        smr.rootBone = armature.transform;

        var model = new Model( geo.name, muscleTemplates, container.transform, smr, armature.transform, ReadAnimations( reader, mesh ), this, null );
        smr.materials = materials;
        //rename and remove (Instance) suffix
        foreach( var mat in smr.materials ){
            mat.name = mat.name.Replace( " (Instance)", "" );
        }
        return model;
    }

    private void ReadArmature( ByteBufferedReader reader, out MuscleTemplate[] muscleTemplates, GameObject container, Manifest manifest, out GameObject armature, out Matrix4x4[] bindPoses, out Transform[] bones ){
        short armatureID = reader.ReadNextShort();
        muscleTemplates = null;

        if( manifest.transformEntry.TryGetValue( armatureID, out TransformEntry armatureContainer ) ){
            armature = armatureContainer.transform.gameObject;

            //extract from parent
            var oldSMR = armature.transform.parent.gameObject.GetComponentInChildren<SkinnedMeshRenderer>();
            bindPoses = oldSMR.sharedMesh.bindposes;
            bones = oldSMR.bones;
            return;
        }
        
        armature = reader.ReadNewGameObject( armatureID, manifest, Read.ARMATURE_MODEL );
                    
        armature.transform.SetParent( container.transform, true );
        container.transform.position = Vector3.zero;

        //build bones
        bones = new Transform[ reader.ReadNextInt() ];
        for( int boneIndex=0; boneIndex<bones.Length; boneIndex++ ){
            short id = reader.ReadNextShort();
            var bone = new GameObject( reader.ReadNextString() ).transform;
            var spawnMatrix = reader.ReadNextFloatArrayAsMatrix();
            bone.localPosition = spawnMatrix.GetPosition();
            bone.localRotation = spawnMatrix.GetRotation();
            bone.localScale = spawnMatrix.GetScale();
            bones[ boneIndex ] = bone;
            manifest.transformEntry[ id ] = new TransformEntry( bone, Read.ARMATURE_MODEL );
        }

        muscleTemplates = new MuscleTemplate[ reader.ReadNextByte() ];
        for( int i=0; i<muscleTemplates.Length; i++ ){
            short id = reader.ReadNextShort();
            var bone = manifest.transformEntry[ id ];
            var muscleTemplate = new MuscleTemplate();
            muscleTemplate.boneName = bone.transform.name;
            muscleTemplate.mass = reader.ReadNextFloat();
            muscleTemplate.pitch = reader.ReadNextFloat();
            muscleTemplate.yaw = reader.ReadNextFloat();
            muscleTemplate.roll = reader.ReadNextFloat();
            muscleTemplates[i] = muscleTemplate;
        }

        var dynamicBoneRoots = reader.ReadNextByte();
        for( int i=0; i<dynamicBoneRoots; i++ ){
            short id = reader.ReadNextShort();
            var model = manifest.transformEntry[ id ].transform;
            var dynamicBone = model.gameObject.AddComponent<DynamicBone>();
            manifest.dynamicBones.Add( dynamicBone );
            dynamicBone.m_Stiffness = reader.ReadNextFloat();
            dynamicBone.m_Damping = reader.ReadNextFloat();
            dynamicBone.m_Elasticity = reader.ReadNextFloat();
            dynamicBone.m_Radius = reader.ReadNextFloat();
            dynamicBone.m_Radius = dynamicBone.m_Radius<=0 ? 0.3f : dynamicBone.m_Radius;
            dynamicBone.m_Force = new Vector3( 0, reader.ReadNextFloat(), 0 );
            dynamicBone._internalParentBoneId = reader.ReadNextShort();
            dynamicBone.m_Root = model;
        }
        //build bone hierarchy
        for( int boneIndex=0; boneIndex<bones.Length; boneIndex++ ){
            var boneTransform = bones[ boneIndex ];
            int parentIndex = reader.ReadNextShort();
            Transform parent = parentIndex >= 0 ? bones[ parentIndex ] : armature.transform;
            boneTransform.SetParent( parent, true );
        }
        bindPoses = new Matrix4x4[ bones.Length ];
        for( int boneIndex=0; boneIndex<bones.Length; boneIndex++ ){
            bindPoses[ boneIndex ] = bones[ boneIndex ].worldToLocalMatrix;
        }
    }

    private void ReadShapeKeys( ByteBufferedReader reader, Mesh mesh ){
        var shapeKeyCount = (int)reader.ReadNextByte();
        for( int j=0; j<shapeKeyCount; j++ ){
            var name = reader.ReadNextString();
            Vector3[] offsets = new Vector3[ mesh.vertexCount ];
            var indices = reader.ReadNextIntArray();
            var rawOffsets = reader.ReadNextFloatArrayAsVector3Array();
            for( int offsetIndex=0; offsetIndex<indices.Length; offsetIndex++ ){
                offsets[ indices[offsetIndex] ] = rawOffsets[ offsetIndex ];
            }
            mesh.AddBlendShapeFrame( name, 1.0f, offsets, null, null );
        }
    }
}

}