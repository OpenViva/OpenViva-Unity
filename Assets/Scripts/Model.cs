using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;



namespace viva{

using KeepEntry = Tuple<Transform,Quaternion>;

public class Model: VivaEditable{

    public static string root { get{ return Viva.contentFolder+"/FBX"; } }
    
    public class PoseInfo{
        public Vector3 localPos;
        public Quaternion localRot;
        public Vector3 localScale;
        public Transform target;
    }

    public readonly string name;
    public readonly Transform rootTransform;
    public readonly Transform[] bones;
    public readonly SkinnedMeshRenderer skinnedMeshRenderer;
    public readonly MeshRenderer meshRenderer;
    public readonly MeshFilter meshFilter;
    public readonly Transform armature;
    public Profile profile { get; private set; }
    public BipedProfile bipedProfile { get{ return profile as BipedProfile; } }
    public AnimalProfile animalProfile { get{ return profile as AnimalProfile; } }
    public FBXRequest modelRequest { get{ return _internalSourceRequest as FBXRequest; } }
    public TextureBindingGroup textureBindingGroup { get; private set; }
    public Transform deltaTransform { get; private set; }   //used for root container animations
    public int deltaTransformBindHash { get; private set; }
    public readonly float startingHeight;
    public readonly float boundsRadius;
    public PoseInfo[] spawnPose { get; private set; }
    public Cloth cloth { get; private set; }
    public Bounds? bounds { get{
        if( skinnedMeshRenderer == null && meshRenderer == null ) return null;
        return skinnedMeshRenderer ? skinnedMeshRenderer.bounds : meshRenderer.bounds;
    } }
    public Renderer renderer { get{ //renderer might be null for NULL empty models
        return skinnedMeshRenderer ? (Renderer)skinnedMeshRenderer : meshRenderer;
    } }
    public readonly Animation[] animations;
    public Model[] children { get; private set; } = new Model[0];
    public bool isStatic { get{ return renderer ? renderer.gameObject.isStatic : rootTransform.gameObject.isStatic; } }
    public readonly List<Collider> colliders = new List<Collider>();
    public bool _internalIsRoot = true;
    public float scale { get{ return rootTransform ? rootTransform.lossyScale.y : 1f; } }
    public MuscleTemplate[] muscleTemplates;


    public Ragdoll RebuildRagdoll( Character character ){
        
        var isAnimal = muscleTemplates != null && muscleTemplates.Length > 0;
        Ragdoll ragdollPrefab = isAnimal ? (Ragdoll)BuiltInAssetManager.main.animalPrefab : (Ragdoll)BuiltInAssetManager.main.bipedPrefab;
        var ragdoll = GameObject.Instantiate( ragdollPrefab, rootTransform );
        ragdoll.Setup( this, character );

        return ragdoll;
    }

    public void _InternalAddChild( Model model ){
        if( model == null ){
            Debug.LogError("Cannot add null model");
            return;
        }
        var childrenList = new List<Model>( children );
        childrenList.Add( model );
        children = childrenList.ToArray();
    }

    public void _InternalAddCollider( Collider collider ){
        if( collider == null ){
            Debugger.LogError("Cannot add a null collider to Model");
            return;
        }
        colliders.Add( collider );
    }

    public void _InternalSetStatic( bool _static ){
        if( renderer ){
            renderer.gameObject.isStatic = _static;
        }else{
            rootTransform.gameObject.isStatic = _static;
        }
        foreach( var collider in colliders ){
            collider.gameObject.isStatic = _static;
        }
        foreach( var child in children ){
            child._InternalSetStatic( _static );
        }
    }

    public List<string> _InternalGetAllMaterialNames(){
        var materialNames = new List<string>();
        AddMaterialNames( materialNames );
        return materialNames;
    }

    private void AddMaterialNames( List<string> materialNames ){
        if( renderer != null ){
            foreach( var mat in renderer.materials ){
                materialNames.Add( mat.name );
            }
        }
        foreach( var childModel in children ){
            childModel.AddMaterialNames( materialNames );
        }
    }

    public bool HasTransform( Transform transform ){
        if( skinnedMeshRenderer ){
            foreach( var bone in skinnedMeshRenderer.bones ){
                if( bone == transform ) return true;
            }
            if( skinnedMeshRenderer.transform == transform ) return true;
        }
        if( rootTransform && rootTransform == transform ) return true;
        if( meshRenderer && meshRenderer == transform ) return true;
        return false;
    }

    private void AddModelHierarchy( List<Model> models ){
        models.Add( this );
        foreach( var child in children ){
            child.AddModelHierarchy( models );
        }
    }
    
    public void SetMaterialColor( string materialName, Color color ){
        if( renderer == null ) return;
        foreach( var mat in renderer.materials ){
            if( mat.name == materialName ) mat.color = color;
        }
        foreach( var childModel in children ) childModel.SetMaterialColor( materialName, color );
    }

    public List<Model> GetModelHierarchy(){
        var models = new List<Model>();
        AddModelHierarchy( models );
        return models;
    }

    public void SetTexture( string materialName, string _binding, string path ){
        var textureHandle = TextureHandle.Load( path );
        if( textureHandle == null ) return;
        _InternalSetTexture( materialName, _binding, textureHandle, true );
    }

    public void _InternalSetTexture( string materialName, string _binding, TextureHandle textureHandle, bool applyToHierarchy=true ){
        if( textureHandle == null ) return;

        int materialIndex = -1;
        if( renderer != null ){
            for( int i=0; i<renderer.materials.Length; i++ ){
                var mat = renderer.materials[i];
                if( mat.name == materialName ){
                    materialIndex = i;
                    break;
                }
            }
        }
        if( materialIndex == -1 ){
            Debugger.LogError("Could not find material name \""+materialName+"\"");
            return;
        }
        var newBinding = new TextureBinding( textureHandle, renderer, name+"/"+textureHandle._internalTexture.name+".tex", _binding, materialIndex );
        textureBindingGroup.Add( newBinding );
        newBinding.Apply();
        
        if( applyToHierarchy ){
            foreach( var childModel in children ) childModel._InternalSetTexture( materialName, _binding, textureHandle );
        }
    }

    public void SetMaterial( string targetMaterialName, Material newMaterial ){
        if( renderer != null ){
            for( int i=0; i<renderer.materials.Length; i++ ){
                var mat = renderer.materials[i];
                if( mat.name == targetMaterialName ){
                    newMaterial.name = mat.name;

                    var materialsCopy = new List<Material>( renderer.materials );
                    materialsCopy[i] = newMaterial;
                    renderer.materials = materialsCopy.ToArray();

                    foreach( var other in renderer.materials ){
                        other.name = other.name.Replace(" (Instance)","");
                    }
                    break;
                }
            }
        }
        foreach( var childModel in children ) childModel.SetMaterial( targetMaterialName, newMaterial );
    }

    //build skinned mesh model
    public Model( string _name, MuscleTemplate[] _muscleTemplates, Transform _rootTransform, SkinnedMeshRenderer _smr, Transform _armature, Animation[] _animations, FBXRequest __internalSourceRequest, Cloth _cloth ):base(__internalSourceRequest){
        name = _name;
        rootTransform = _rootTransform;
        skinnedMeshRenderer = _smr;
        armature = _armature;
        animations = _animations;
        bones = skinnedMeshRenderer.bones;
        textureBindingGroup = new TextureBindingGroup( this );
        spawnPose = SaveCurrentPose();
        cloth = _cloth;
        muscleTemplates = _muscleTemplates;

        Bounds bounds = skinnedMeshRenderer.bounds;
        startingHeight = bounds.size.y;
        boundsRadius = bounds.extents.magnitude;

        usage.onDiscarded += ClearModelData;
    }

    //build static mesh model
    public Model( string _name, Transform _rootTransform, MeshRenderer _mr, MeshFilter _mf, Animation[] _animations, FBXRequest _internalSourceRequest ):base(_internalSourceRequest){
        name = _name;
        rootTransform = _rootTransform;
        bones = null;
        meshRenderer = _mr;
        meshFilter = _mf;
        animations = _animations;
        textureBindingGroup = new TextureBindingGroup( this );
        
        var bounds = this.bounds;
        if( bounds.HasValue ){
            startingHeight = bounds.Value.size.y;
            boundsRadius = bounds.Value.extents.magnitude;
        }else{
            startingHeight = 0;
            boundsRadius = 0;
        }

        usage.onDiscarded += ClearModelData;
    }

    public List<PoseInfo> ExtractFromSpawnPose( BipedMask mask ){
        if( skinnedMeshRenderer == null || bipedProfile == null ) return null;
        
        var pose = new List<PoseInfo>();
        var toAnimateBones = BipedProfile.RagdollMaskToBones( mask );
        foreach( var toAnimateBone in toAnimateBones ){
            var boneInfo = bipedProfile[ toAnimateBone ];
            if( boneInfo == null ) continue;
            var bone = bipedProfile[ toAnimateBone ].transform;
            for( int i=0; i<skinnedMeshRenderer.bones.Length; i++ ){
                if( skinnedMeshRenderer.bones[i] == bone ){
                    pose.Add( spawnPose[i] );
                    break;
                }
            }
        }
        return pose;
    }

    private PoseInfo[] SaveCurrentPose(){
        
        //save spawn positions
        PoseInfo[] newSpawnPose = new PoseInfo[ bones.Length ];
        for( int boneIndex=0; boneIndex<bones.Length; boneIndex++ ){
            var bone = bones[ boneIndex ];
            var poseInfo = new PoseInfo();
            poseInfo.localPos = bone.localPosition;
            poseInfo.localRot = bone.localRotation;
            poseInfo.localScale = bone.localScale;
            poseInfo.target = bone;
            newSpawnPose[ boneIndex ] = poseInfo;
        }
        return newSpawnPose;
    }
    
    public override void OnCreateMenuSelected(){
        GameUI.main.createMenu.DisplayVivaObjectInfo<Model>( this, _internalSourceRequest );
        DisplayCreateMenuSettings( delegate{
                GameUI.main.createMenu.FindAndDisplay( CreateMenu.InfoColor.MODEL, this );
        } );
    }
    public void DisplayCreateMenuSettings( GenericCallback onRagdollEditApply ){
        if( rootTransform != null ){
            rootTransform.gameObject.SetActive( true );
            ThumbnailGenerator.main.StopAnimation();
        }
        GameUI.main.createMenu.DisplayCreateButton();
        if( bones != null && animalProfile == null ) GameUI.main.createMenu.DisplayEditRagdollButton();
        GameUI.main.createMenu.editRagdollButton.SetCallback( delegate{
            GameUI.main.ragdollEditor.EditBipedProfile( this, onRagdollEditApply, null );
        } );
    }
    public override void OnCreateMenuDeselected(){
        if( rootTransform != null ){
            rootTransform.gameObject.SetActive( false );
        }
    }

    private void ClearModelData(){
        Debug.Log("~Model "+name);
        if( skinnedMeshRenderer ){
            GameObject.Destroy( skinnedMeshRenderer.sharedMesh );
        }else if( meshFilter ){
            GameObject.Destroy( meshFilter.mesh );
        }
        GameObject.Destroy( rootTransform.gameObject );
        profile = null;
        textureBindingGroup.DiscardAll( true );
    }
    
    public void ApplyDeltaPosition( Vector3 deltaOffset ){
        deltaOffset.y = 0.0f;
        var scaleTransform = armature ?? rootTransform;
        scaleTransform.position += scaleTransform.rotation*deltaOffset*bipedProfile.hipHeight*scaleTransform.localScale.x;
    }

    public void ZeroOutDeltaTransform(){
        var pos = deltaTransform.localPosition;
        if( bipedProfile != null ) pos.y *= bipedProfile.hipHeight;
        pos.x = 0.0f;
        pos.z = 0.0f;
        deltaTransform.localPosition = pos;
    }

    public void SetDeltaTransform( Transform transform ){
        deltaTransform = transform;
        deltaTransformBindHash = deltaTransform.name.GetHashCode();
    }

    public void ApplySpineTPoseDeltas(){
        if( bipedProfile == null ) return;
        ApplySpineTPoseDeltaCacheTriple( 0, BipedBone.UPPER_LEG_L, BipedBone.UPPER_LEG_R, BipedBone.LOWER_SPINE );
        ApplySpineTPoseDeltaCacheSingle( 1, BipedBone.UPPER_SPINE );
        ApplySpineTPoseDeltaCacheTriple( 2, BipedBone.NECK, BipedBone.SHOULDER_R, BipedBone.SHOULDER_L );
        ApplySpineTPoseDeltaCacheSingle( 3, BipedBone.HEAD );
        ApplySpineTPoseDelta( 4 );
    }
    
    private void ApplySpineTPoseDeltaCacheTriple( int deltaTposeBoneIndex, BipedBone r0, BipedBone r1, BipedBone r2 ){
        var c0 = bipedProfile[ r0 ].transform;
        var c1 = bipedProfile[ r1 ].transform;
        var c2 = bipedProfile[ r2 ].transform;
        Quaternion q0 = c0.rotation;
        Quaternion q1 = c1.rotation;
        Quaternion q2 = c2.rotation;
        ApplySpineTPoseDelta( deltaTposeBoneIndex );
        c0.rotation = q0;
        c1.rotation = q1;
        c2.rotation = q2;
    }

    private void ApplySpineTPoseDeltaCacheDouble( int deltaTposeBoneIndex, BipedBone r0, BipedBone r1 ){
        var c0 = bipedProfile[ r0 ].transform;
        var c1 = bipedProfile[ r1 ].transform;
        Quaternion q0 = c0.rotation;
        Quaternion q1 = c1.rotation;
        ApplySpineTPoseDelta( deltaTposeBoneIndex );
        c0.rotation = q0;
        c1.rotation = q1;
    }
    
    private void ApplySpineTPoseDeltaCacheSingle( int deltaTposeBoneIndex, BipedBone childRagdollBone ){
        var childBone = bipedProfile[ childRagdollBone ].transform;
        Quaternion oldRot = childBone.rotation;
        ApplySpineTPoseDelta( deltaTposeBoneIndex );

        childBone.rotation = oldRot;
    }
    
    private void ApplySpineTPoseDelta( int deltaTposeBoneIndex ){
        var targetBone = bipedProfile[ BipedProfile.deltaTposeBones[ deltaTposeBoneIndex ] ].transform;
        targetBone.localRotation *= bipedProfile.spineTposeDeltas[ deltaTposeBoneIndex ];
    }

    public void Resize( float? newHeight ){
        if( rootTransform == null ){
            Debugger.LogWarning("Cannot resize with null root");
            return;
        }
        if( newHeight.HasValue ){
            rootTransform.localScale = Vector3.one*( newHeight.Value/startingHeight );
        }else{
            rootTransform.localScale = Vector3.one;
        }
    }

    public bool AttemptSetProfile( Profile newProfile, out string message ){
        if( newProfile == null ){
            message = "Profile is null";
            return false;
        }
        if( newProfile.ValidateProfile( this, out message ) ){
            profile = newProfile;
            
            if( newProfile as BipedProfile != null ){
                SetDeltaTransform( ((BipedProfile)profile)[ BipedBone.HIPS ].transform );
                SetupForBiped();
            }else{
                SetupForAnimal();
            }
            return true;
        }
        message = "Could not set profile "+message;
        return false;
    }

    public void SetupForBiped(){
        RestructureForBipedAnimation();
        
        //fix animations associated with this model
        if( modelRequest != null && modelRequest.lastSpawnedFBX ){
            foreach( var animation in animations ){
                animation.ApplyRagdollProfile( this );
            }
        }
        //rename all bones to match BipedBone enums
        foreach( var boneInfo in bipedProfile.bones ){
            if( boneInfo != null ) boneInfo.transform.name = boneInfo.name.ToString();
        }
    }

    private void SetupForAnimal(){
        foreach( var boneInfo in animalProfile.bones ){
            if( boneInfo != null ) boneInfo.transform.name = boneInfo.name.ToString();
        }
    }

    public void ApplySpawnPose( bool applyPositions ){
        if( skinnedMeshRenderer == null || skinnedMeshRenderer.bones == null ) return;

        foreach( var poseInfo in spawnPose ){
            if( applyPositions ) poseInfo.target.localPosition = poseInfo.localPos;
            poseInfo.target.localRotation = poseInfo.localRot;
            poseInfo.target.localScale = poseInfo.localScale;
        }
    }

    public Model FindChildModel( string name ){
        foreach( var childModel in children ){
            if( childModel.name == name ) return childModel;
        }
        return null;
    }

    public override void OnInstall( string subFolder=null ){
        Tools.ArchiveFile( _internalSourceRequest.filepath, Model.root+"/"+Path.GetFileName( _internalSourceRequest.filepath ) );
        textureBindingGroup.OnInstall( subFolder );
    }

    public void SetShader( Shader shader ){
        var renderer = meshRenderer ? (Renderer)meshRenderer : (Renderer)skinnedMeshRenderer;
        for( int i=0; i<renderer.materials.Length; i++ ){
            renderer.materials[i].shader = shader;
        }
    }
    
    private void RestructureForBipedAnimation(){
        if( bones == null ){
            Debug.LogError("Cannot restructure for ragdoll animation with null bones");
            return;
        }
        
        ApplySpawnPose( true );

        //cache old rotations
        for( int i=0; i<bipedProfile.Size(); i++ ){
            var boneInfo = bipedProfile.bones[i];
            if( i>=BipedProfile.nonOptionalBoneCount && boneInfo == null ) continue;

            bipedProfile.animLocalDeltas[i] = boneInfo.transform.rotation;
            var parent = boneInfo.transform.parent;
            bipedProfile.animParentDeltas[i] = parent ? parent.rotation : Quaternion.identity;
        }

        //skin new rest pose
        float oldScale = bounds.Value.size.y;
        Resize( null );

        var bindPoses = new Matrix4x4[ bones.Length ];
        for( int i=0; i<bindPoses.Length; i++ ){
            var bone = bones[i];
            var boneEnum = GetRagdollBoneFromTransform( bone );
            if( boneEnum.HasValue ){
                bindPoses[i] = FixRagdollBoneTransformRoll( boneEnum.Value );
            }else{  //reuse old binding
                bindPoses[i] = skinnedMeshRenderer.sharedMesh.bindposes[i];
            }
        }
        skinnedMeshRenderer.sharedMesh.bindposes = bindPoses;

        var head = bipedProfile[ BipedBone.HEAD ].transform;
        var upperArmR = bipedProfile[ BipedBone.UPPER_ARM_R ].transform;
        var armR = bipedProfile[ BipedBone.ARM_R ].transform;
        var handR = bipedProfile[ BipedBone.HAND_R ].transform;
        
        bipedProfile.hipHeight = bipedProfile[ BipedBone.HIPS ].transform.position.y;
        bipedProfile.floorToHeadHeight = head.position.y;
        var foot = bipedProfile.bones[ (int)BipedBone.FOOT_R ].transform;
        bipedProfile.footForwardOffset = Quaternion.Inverse( foot.rotation);
        bipedProfile.footHeight = foot.position.y;
        bipedProfile.footCenterDistance = foot.position.x;
        bipedProfile.shoulderWidth = Mathf.Abs( head.position.x-upperArmR.position.x );
        bipedProfile.upperArmLength = Vector3.Distance( upperArmR.position, armR.position );
        bipedProfile.armLength = Vector3.Distance( armR.position, handR.position );

        Resize( oldScale );

        //build animation delta
        for( int i=0; i<bipedProfile.Size(); i++ ){
            var boneInfo = bipedProfile.bones[i];
            if( i>=BipedProfile.nonOptionalBoneCount && boneInfo == null ) continue;

            var oldRot = bipedProfile.animLocalDeltas[i];
            Quaternion newRot = boneInfo.transform.rotation;
            Quaternion localChange = Quaternion.Inverse( newRot )*oldRot;

            var parent = boneInfo.transform.parent;
            var oldParentRot = bipedProfile.animParentDeltas[i];
            Quaternion newParentRot = parent ? parent.rotation : Quaternion.identity;
            Quaternion parentChange = Quaternion.Inverse( newParentRot )*oldParentRot;

            bipedProfile.animParentDeltas[i] = Quaternion.Euler( parentChange.eulerAngles );
            bipedProfile.animLocalDeltas[i] = Quaternion.Euler( -localChange.eulerAngles );
        }

        Quaternion[] parentCache = new Quaternion[ bipedProfile.spineTposeDeltas.Length ];
        for( int i=0; i<bipedProfile.spineTposeDeltas.Length; i++ ){
            parentCache[i] = bipedProfile[ BipedProfile.deltaTposeBones[i] ].transform.rotation;
        }
        spawnPose = SaveCurrentPose();

        BuiltInAssetManager.main.ApplyTPose( this );

        for( int i=0; i<bipedProfile.spineTposeDeltas.Length; i++ ){
            var bone = bipedProfile[ BipedProfile.deltaTposeBones[i] ].transform;
            bipedProfile.spineTposeDeltas[i] = Quaternion.Inverse( bone.rotation )*parentCache[i];
        }

        ApplySpineTPoseDeltas();
    }

    private BipedBone? GetRagdollBoneFromTransform( Transform bone ){
        for( int j=0; j<bipedProfile.bones.Length; j++ ){
            var boneInfo = bipedProfile.bones[j];
            if( boneInfo != null && boneInfo.transform == bone ){
                return (BipedBone)j;
            }
        }
        return null;
    }
    
    //force bone to point forward while keeping axis up direction
    private Matrix4x4 FixRagdollBoneTransformRoll( BipedBone boneEnum ){
        var bone = bipedProfile[ boneEnum ].transform;
        var children = new Transform[ bone.childCount ];
        for( int j=0; j<bone.childCount; j++ ){
            children[j] = bone.GetChild(j);
        }
        foreach( var child in children ){
            child.SetParent( null, true );
        }
        //enforce an up direction when twisting bone
        float reqUpSign = BuiltInAssetManager.main.tPoseUp[ (int)boneEnum ];

        if( reqUpSign < 2 ){
            Vector3 up = bone.up;
            if( reqUpSign != 0 ){
                if( Mathf.Sign( bone.up.y ) != reqUpSign ){
                    up *= -1;
                }
            }
            bone.rotation = Quaternion.LookRotation( Vector3.ProjectOnPlane( Vector3.forward, up ) , up );
        }else{
            bone.rotation = Quaternion.identity;
        }

        foreach( var child in children ){
            child.SetParent( bone, true );
        }
        return bone.worldToLocalMatrix;
    }

    public override string GetInfoHeaderTitleText(){
        return name;
    }

    public override void _InternalOnGenerateThumbnail(){
        ThumbnailGenerator.main.GenerateModelThumbnailTexture( this, thumbnail );
    }

    public override string GetInfoHeaderText(){
        return "Model";
    }

    public override string GetInfoBodyContentText(){
        string s = "Skinned: ";
        if( skinnedMeshRenderer ){
            s += "<color=#00ff00>YES</color>\n";
            s += "BipedRagdoll profile: "+( bipedProfile!=null ? "<color=#00ff00>YES</color>" : "<color=#ffff00>NO</color>" )+"\n";
            s += "Vertices: "+skinnedMeshRenderer.sharedMesh.vertexCount+"\n";
            s += "Materials: "+skinnedMeshRenderer.materials.Length+"\n";
            s += "Bones: "+bones.Length+"\n";
            s += "Blend shapes: "+skinnedMeshRenderer.sharedMesh.blendShapeCount+"\n";
        }else if( meshFilter ){
            s += "<color=#ffff00>NO</color>\n";
            s += "vertices: "+meshFilter.mesh.vertexCount+"\n";
            s += "materials: "+meshRenderer.materials.Length+"\n";
        }
        return s;
    }
}

}