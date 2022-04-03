using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;


namespace viva{

public delegate void ItemCallback( Item item );

public enum CountMatch{
    ANY,
    EQUAL,
    EQUAL_OR_GREATER,
    GREATER
}
public struct AttributeRequest{

    public readonly Attribute[] attributes;
    public readonly bool exclusive; //dont allow other attributes?
    public readonly CountMatch countMatch;

    public AttributeRequest( string[] attributeStrings, bool _exclusive, CountMatch _countMatch ){
        if( attributeStrings == null ) throw new System.Exception("Cannot request a null attribute");
        attributes = Attribute.StringArrayToArray( attributeStrings );
        exclusive = _exclusive;
        countMatch = _countMatch;
    }

    public AttributeRequest( Attribute[] _attributes, bool _exclusive, CountMatch _countMatch ){
        if( _attributes == null ) throw new System.Exception("Cannot request a null attribute");
        attributes = _attributes;
        exclusive = _exclusive;
        countMatch = _countMatch;
    }

    public override string ToString(){
        var str = "[";
        foreach( var attrib in attributes ){
            str += attrib.ToString()+",";
        }
        return str+"]";
    }

    public Attribute FindAttribute( string name ){
        foreach( var attrib in attributes ){
            if( attrib.name == name ) return attrib;
        }
        return null;
    }
}

[System.Serializable]
public class Attribute{
    [SerializeField]
    private string m_name;
    public string name { get{ return m_name; } }
    [SerializeField]
    public int count;

    public Attribute( string _name, int _count ){
        m_name = _name;
        count = _count;
    }

    public static Attribute[] StringArrayToArray( string[] attributeInfo ){
        if( attributeInfo == null ) return null;
        var attributes = new Attribute[ attributeInfo.Length ];
        for( int i=0; i<attributes.Length; i++ ){
            attributes[i] = new Attribute( attributeInfo[i] );
        }
        return attributes;
    }

    public Attribute( string attributeString ){
        if( !ExtractInfo( attributeString, out count, out m_name ) ){
            throw new System.Exception("Could not parse attribute with count \""+attributeString+"\"");
        }
    }
    
    public static bool ExtractInfo( string attribute, out int count, out string name ){
        count = 0;
        name = null;
        if( string.IsNullOrWhiteSpace( attribute ) ) return false;
        var result = Regex.Match( attribute, @"^-?\d+x ", RegexOptions.CultureInvariant );
        if( result.Success ){
            if( !System.Int32.TryParse( result.Value.Substring( 0, result.Value.Length-2 ), out count ) ) return false;

            name = attribute.Substring( result.Value.Length );
            return true;
        }else{
            count = 1;
            name = attribute;
            return true;
        }
    }

    public static bool Matches( Attribute[] attributesA, Attribute[] attributesB, CountMatch countMatch ){
        if( attributesA == null || attributesB == null ) return false;
        if( attributesA.Length < attributesB.Length ) return false;
        int matches = 0;
        foreach( var attribA in attributesA ){
            foreach( var attribB in attributesB ){
                if( attribA.Matches( attribB, countMatch ) ){
                    matches++;
                    break;
                }
            }
        }
        return matches==attributesB.Length;
    }

    public bool Matches( Attribute otherAttrib, CountMatch countMatch ){
        if( otherAttrib.name != name ) return false;
        switch( countMatch ){
        case CountMatch.ANY:
            return true;
        case CountMatch.EQUAL:
            return count==otherAttrib.count;
        case CountMatch.EQUAL_OR_GREATER:
            return count>=otherAttrib.count;
        case CountMatch.GREATER:
            return count>otherAttrib.count;
        }
        return false;
    }

    public override bool Equals( object obj ){
        if (!(obj is Attribute)) return false;
        var candidate = (Attribute)obj;
		return candidate.name==name &&
			   candidate.count==count;
	}

    public override string ToString(){
        return count+"x "+name;
    }
}
public class Item : VivaInstance{

    public class ItemCallbackWrapper: InstanceCallbackWrapper{
        private readonly ItemCallback func;
        public ItemCallbackWrapper( ItemCallback _func ){
            func = _func;
        }
        public override void Invoke( VivaInstance instance ){ func?.Invoke( instance as Item ); }
    }

    public static readonly InstanceManager instances = new InstanceManager( Item.root, ".item", ImportRequestType.ITEM );
    public static readonly string offerAttribute = "pickup";
    public static readonly float defaultAngularDrag = 1f;

    public static Item Spawn( string name, Vector3 position, Quaternion rotation ){
        Item instance = null;
        instances._InternalSpawnAndLink( name, true, position, rotation, new ItemCallbackWrapper( delegate( Item item ){
            instance = item;
        } ), null );
        return instance;
    }

    public static void _InternalSpawn( string name, bool instant, Vector3 position, Quaternion rotation, ItemCallback onSpawn, ItemCallback onPreInitialize ){
        instances._InternalSpawnAndLink( name, instant, position, rotation, new ItemCallbackWrapper( onSpawn ), onPreInitialize==null ? null : new ItemCallbackWrapper( onPreInitialize ) );
    }
    
    public static Texture2D FindItemTextureWithAttribute( Attribute[] attributes ){
        foreach( var importRequest in instances.cachedRequests.Values ){
            var itemRequest = importRequest as ItemRequest;
            var otherAttributes = itemRequest.itemSettings.attributes;
            foreach( var otherAttribute in otherAttributes ){
                foreach( var attribute in attributes ){
                    if( otherAttribute == attribute.name ) return importRequest.thumbnail.texture;
                }
            }
        }
        return null;
    }

    public static Item _InternalCreateItemBase( Model model, ItemSettings itemSettings, int? idOverride=null, string[] attributesOverride=null, bool? immovableOverride=null ){
        if( model == null || model.rootTransform == null ){
            throw new System.Exception("Cannot create item when Model or rootTransform is null");
        }
        var item = model.rootTransform.GetComponent<Item>();
        if( item != null ){
            throw new System.Exception("Model already has an item");
        }

        Rigidbody rigidBody = null;
        if( !model.rootTransform.TryGetComponent<Rigidbody>( out rigidBody ) ){
            rigidBody = model.rootTransform.gameObject.AddComponent<Rigidbody>();
        }

        //disable parent to prevent awake on add
        var container = model.rootTransform.parent.gameObject;
        container.SetActive( false );

        item = VivaInstance.CreateVivaInstance<Item>( model.rootTransform.gameObject, itemSettings, idOverride );
        item.m_rigidBody = rigidBody;

        var candidateColliders = new List<Collider>();
        var zones = new List<Zone>();
        Bounds? bounds = null;
        for( int i=0; i<item.transform.childCount; i++ ){
            var child = item.transform.GetChild(i);

            var collider = child.GetComponent<Collider>();
            if( collider ){
                if( child.gameObject.layer == WorldUtil.itemsLayer ){
                    candidateColliders.Add( collider );
                    if( bounds == null ){
                        bounds = collider.bounds;
                    }else{
                        bounds.Value.Encapsulate( collider.bounds );
                    }
                    //setup collider
                    foreach( var colliderSetting in itemSettings.colliderSettings ){
                        if( colliderSetting.name == collider.name ){
                            collider.material = BuiltInAssetManager.main.FindPhysicMaterial( colliderSetting.material );
                        }
                    }

                }else if( child.gameObject.layer == WorldUtil.objectDetectorLayer ){
                    item.enableTriggerEnter = true;
                }
                var zone = collider.GetComponent<Zone>();
                if( zone ){
                    zones.Add( zone );
                    zone._internalParentItem = item;
                }
            }
        }
        if( candidateColliders.Count == 0 ) Debugger.LogWarning("Item \""+itemSettings.name+"\" is missing colliders!");
        item.m_colliders = candidateColliders.ToArray();
        item.m_zones = zones.ToArray();

        if( rigidBody ){
            item.rigidBody.angularDrag = defaultAngularDrag;
            rigidBody.mass = itemSettings.mass;

            if( !string.IsNullOrEmpty( itemSettings.collisionSoundSoft ) || !string.IsNullOrEmpty( itemSettings.collisionSoundHard ) || !string.IsNullOrEmpty( itemSettings.dragSound ) ){
                var physicsSoundSource = rigidBody.gameObject.AddComponent<PhysicsSoundSource>();
                physicsSoundSource.collisionSoundsSoft = BuiltInAssetManager.main.FindPhysicsSoundGroup( itemSettings.collisionSoundSoft );
                physicsSoundSource.collisionSoundsHard = BuiltInAssetManager.main.FindPhysicsSoundGroup( itemSettings.collisionSoundHard );
                physicsSoundSource.settings = BuiltInAssetManager.main.FindPhysicsSoundSourceSetting( itemSettings.soundSetting );
                physicsSoundSource.dragSoundLoop = BuiltInAssetManager.main.FindPhysicsDragSound( itemSettings.dragSound );

                if( physicsSoundSource.collisionSoundsSoft || physicsSoundSource.collisionSoundsHard || physicsSoundSource.dragSoundLoop ){
                    if( !physicsSoundSource.settings ) Debugger.LogWarning( "Item \""+item.name+"\" has physics sounds but missing sound settings!" );
                }
            }
            if( bounds.HasValue ){
                float avgSize = ( bounds.Value.extents.x+bounds.Value.extents.y+bounds.Value.extents.z )/3.0f;
                if( avgSize < 0.07f ) rigidBody.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }
        }else{
            model._InternalSetStatic( true );
        }
        item.model = model;

        item.m_grabbables = item.gameObject.GetComponentsInChildren<Grabbable>();
        item.immovable = immovableOverride.HasValue ? immovableOverride.Value : itemSettings.immovable;

        itemSettings.EnsureGrabbableSettingsLength( item.grabbables.Length );
        item.grabbableSettings = itemSettings.grabbableSettings;
        if( item.grabbableSettings==null ){
            item.grabbableSettings = new GrabbableSettings[ item.grabbableCount ];
        }
        for( int i=0; i<item.grabbableCount; i++ ){
            var grabbableSettings = item.grabbableSettings[i];
            if( grabbableSettings == null ){
                grabbableSettings = new GrabbableSettings();
                item.grabbableSettings[i] = grabbableSettings;
            }
            Grabbable._InternalSetup( item, rigidBody, item.grabbables[i], grabbableSettings );
        }
        item.model.textureBindingGroup.MatchAndSetForReset( itemSettings.textureBindingGroups );

        //bind model usage for deletion
        model.usage.Increase();
        item._internalOnDestroy += model.usage.Decrease;
        
        //call Item Awake()
        item.name = itemSettings.name;
        container.SetActive( true );
        if( attributesOverride != null ){
            foreach( var attrib in attributesOverride ) item.AddAttribute( attrib );
        }else{
            foreach( var attrib in itemSettings.attributes ) item.AddAttribute( attrib );
        }

        return item;
    }


    public static string root { get{ return Viva.contentFolder+"/Items"; } }

    [SerializeField]
    public Rigidbody m_rigidBody;
    public Rigidbody rigidBody { get{ return m_rigidBody; } }
    [SerializeField]
    private Grabbable[] m_grabbables = new Grabbable[0];
    public Grabbable[] grabbables { get{ return m_grabbables; } }
    [SerializeField]
    private Collider[] m_colliders = new Collider[0];
    public ListenerItemString onAttributeChanged { get; private set; }
    public Collider[] colliders { get{ return m_colliders; } }
    private Zone[] m_zones = new Zone[0];
    public Zone[] zones { get{ return m_zones; } }
    public ItemSettings itemSettings { get{ return _internalSettings as ItemSettings; } }
    public Model model { get; private set; }
    public bool immovable { get; private set; }
    public ListenerGeneric onSelected { get; private set; }
    public ListenerItem onTriggerEnterItem { get; private set; }
    public ListenerItem onTriggerExitItem { get; private set; }
    public ListenerRigidBody onTriggerEnterRigidBody { get; private set; }
    public ListenerRigidBody onTriggerExitRigidBody { get; private set; }
    public ListenerCollision onCollision { get; private set; }
    public ListenerAttributes onReceiveSpill { get; private set; }
    private SpillListener spillListener;
    public bool canSpill { get{ return spillListener; } }
    private GrabbableSettings[] grabbableSettings;
    public bool enableTriggerEnter = false;
    public int grabbableCount { get{ return grabbables.Length; } }
    private bool initialEnable = false;

    public void EnableSpilling( AttributesReturnCallback _onSpillAttributes ){
        spillListener = gameObject.GetComponent<SpillListener>();
        if( !spillListener ){
            spillListener = gameObject.AddComponent<SpillListener>();
            spillListener._InternalSetup( _onSpillAttributes, this );
        }
        spillListener.onSpillAttributes = _onSpillAttributes;
    }

    private void Awake(){
        onAttributeChanged = new ListenerItemString( this, m_attributes, "onAttributeChanged" );
        onSelected = new ListenerGeneric( "onSelected" );
        onTriggerEnterItem = new ListenerItem( (List<Item>)null, "onTriggerEnterItem" );
        onTriggerExitItem = new ListenerItem( (List<Item>)null, "onTriggerExitItem" );
        onTriggerEnterRigidBody = new ListenerRigidBody( "onTriggerEnterRigidBody" );
        onTriggerExitRigidBody = new ListenerRigidBody( "onTriggerExitRigidBody" );
        onCollision = new ListenerCollision( "onCollision" );
        onReceiveSpill = new ListenerAttributes( "onReceiveSpill" );

        _internalOnAttributeChanged += onAttributeChanged.Invoke;
    }

    public Grabbable GetRandomGrabbable(){
        if( grabbableCount == 0 ) return null;
        return grabbables[ Random.Range( 0, grabbableCount ) ];
    }

    private void OnTriggerEnter( Collider collider ){
        if( !enableTriggerEnter || collider.isTrigger ) return;
        var rb = collider.GetComponentInParent<Rigidbody>();
        if( rb ){
            var item = rb.GetComponent<Item>();
            if( item ){
                onTriggerEnterItem.Invoke( item );
            }
            onTriggerEnterRigidBody.Invoke( rb );
        }
    }

    private void OnTriggerExit( Collider collider ){
        if( !enableTriggerEnter || collider.isTrigger ) return;
        var rb = collider.GetComponentInParent<Rigidbody>();
        if( rb ){
            var item = rb.GetComponent<Item>();
            if( item ){
                onTriggerExitItem.Invoke( item );
            }
            onTriggerExitRigidBody.Invoke( rb );
        }
    }

    private void OnCollisionEnter( Collision collision ){
        onCollision.Invoke( collision );
    }

    public override void TeleportTo( Vector3 position, Quaternion rotation ){
        var offset = position-transform.position;

        var floorY = CalculateApproximateFloorY();
        if( floorY.HasValue ){
            offset.y += transform.position.y-floorY.Value;
        }
        transform.position += offset;
        if( rigidBody ){
            rigidBody.velocity = Vector3.zero;
            rigidBody.angularVelocity = Vector3.zero;
        }
    }

    public override void _InternalReset(){
        onAttributeChanged._InternalReset();
        onSelected._InternalReset();
        onTriggerEnterItem._InternalReset();
        onTriggerExitItem._InternalReset();
        onTriggerEnterRigidBody._InternalReset();
        onTriggerExitRigidBody._InternalReset();
        onCollision._InternalReset();
        onReceiveSpill._InternalReset();
        foreach( var grabbable in grabbables ) grabbable._InternalReset();

        m_attributes.Clear();
        foreach( var attrib in itemSettings.attributes ) AddAttribute( attrib );
    }

    public void SetIgnorePhysics( Item otherItem, bool ignore ){
        if( otherItem == null || otherItem == this ) return;
        foreach( var collider in colliders ){
            foreach( var otherCollider in otherItem.colliders ){
                Physics.IgnoreCollision( collider, otherCollider, ignore );
            }
        }
    }

    public void SetIgnorePhysics( Ragdoll ragdoll, bool ignore ){
        if( ragdoll == null ) return;
        foreach( var collider in colliders ){
            foreach( var muscle in ragdoll.muscles ){
                foreach( var muscleCollider in muscle.colliders ){
                    Physics.IgnoreCollision( collider, muscleCollider, ignore );
                }
            }
        }
    }

    public Zone FindZone( string name ){
        foreach( var zone in zones ){
            if( zone.name == name ) return zone;
        }
        return null;
    }

    public void SetModelColor( string modelName, Color color ){
        var targetModel = model.name==modelName ? model : model.FindChildModel( modelName );
        if( targetModel == null ){
            Debugger.LogError("Item \""+name+"\" does not have a model named \""+modelName+"\"");
            return;
        }
        if( targetModel.skinnedMeshRenderer == null ){
            Debugger.LogError("Item \""+name+"\" model \""+modelName+"\" does not support BlendShapes");
            return;
        }
        foreach( var mat in targetModel.skinnedMeshRenderer.materials ){
            mat.color = color;
        }
    }

    public void AnimateBlendShape( string modelName, string blendShape, float targetPercent, float duration ){
        var targetModel = model.name==modelName ? model : model.FindChildModel( modelName );
        if( targetModel == null ){
            Debugger.LogError("Item \""+name+"\" does not have a model named \""+modelName+"\"");
            return;
        }
        if( targetModel.skinnedMeshRenderer == null ){
            Debugger.LogError("Item \""+name+"\" model \""+modelName+"\" does not support BlendShapes");
            return;
        }

        var blendShapeIndex = targetModel.skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex( blendShape );
        if( blendShapeIndex == -1 ){
            Debugger.LogError("Item \""+name+"\" model \""+modelName+"\" does not have BlendShape \""+blendShape+"\"");
            return;
        }
        StartCoroutine( AnimateBlendShapeCoroutine( targetModel, blendShapeIndex, targetPercent, duration ) );
    }

    private IEnumerator AnimateBlendShapeCoroutine( Model targetModel, int blendShapeIndex, float targetPercent, float duration ){
        float timer = 0;
        float startPercent = targetModel.skinnedMeshRenderer.GetBlendShapeWeight( blendShapeIndex );
        while( timer < duration ){
            timer += Time.deltaTime;
            float alpha = Mathf.Clamp01( timer/duration );
            if( targetModel.skinnedMeshRenderer == null ) yield break;
            
            targetModel.skinnedMeshRenderer.SetBlendShapeWeight( blendShapeIndex, Mathf.LerpUnclamped( startPercent, targetPercent, alpha ) );
            yield return new WaitForFixedUpdate();
        }
    }

    public Grabbable GetGrabbable( int index ){
        return grabbables[ index ];
    }

    public bool isBeingGrabbed {
        get{
            foreach( var grabbable in grabbables ){
                if( grabbable.isBeingGrabbed ) return true;
            }
            return false;
        }
    }

    public bool IsBeingGrabbedByCharacter( Character character ){
        foreach( var grabbable in grabbables ){
            if( grabbable.IsBeingGrabbedByCharacter( character ) ) return true;
        }
        return false;
    }

    public List<Character> GetCharactersGrabbing(){
        var result = new List<Character>();
        foreach( var grabbable in grabbables ){
            var candidates = grabbable.GetCharactersGrabbing();
            foreach( var candidate in candidates ){
                if( !result.Contains( candidate ) ) result.Add( candidate );
            }
        }
        return result;
    }

    public List<Grabbable> GetGrabbablesByCharacter( Character character ){
        var result = new List<Grabbable>();
        if( character == null ) return result;
        foreach( var grabbable in grabbables ){
            var candidates = grabbable.GetGrabContextsByCharacter( character );
            foreach( var candidate in candidates ){
                if( !result.Contains( candidate.grabbable ) ) result.Add( candidate.grabbable );
            }
        }
        return result;
    }

    public void DropByCharacter( Character character ){
        if( character == null ) return;
        var grabContexts = GetGrabContextsByCharacter( character );
        foreach( var grabContext in grabContexts ){
            Viva.Destroy( grabContext );
        }
    }

    public List<Grabber> GetGrabbers(){
        var result = new List<Grabber>();
        foreach( var grabbable in grabbables ){
            var grabbers = grabbable.GetGrabbers();
            foreach( var candidate in grabbers ){
                if( !result.Contains( candidate ) ) result.Add( candidate );
            }
        }
        return result;
    }
    
    public List<GrabContext> GetGrabContextsByGrabber( Grabber grabber ){
        var from = new List<GrabContext>();
        if( grabber == null ) return from;
        foreach( var grabbable in grabbables ){
            if( grabbable ){
                var candidates = grabbable.GetGrabContexts( grabber );
                foreach( var candidate in candidates ){
                    if( !from.Contains( candidate ) ){
                        from.Add( candidate );
                    }
                }
            }
        }
        return from;
    }
    
    public List<GrabContext> GetGrabContextsByCharacter( Character character ){
        var from = new List<GrabContext>();
        if( character == null ) return from;
        foreach( var grabbable in grabbables ){
            if( grabbable ){
                var candidates = grabbable.GetGrabContextsByCharacter( character );
                foreach( var candidate in candidates ){
                    if( !from.Contains( candidate ) ){
                        from.Add( candidate );
                    }
                }
            }
        }
        return from;
    }

    public override float? CalculateApproximateRadius(){
        if( !model.renderer ) return null;
        var bounds = model.renderer.bounds.extents;
        return Mathf.Max( bounds.x, bounds.z );
    }
    public override float? CalculateApproximateFloorY(){
        if( !model.renderer ) return null;
        var bounds = model.renderer.bounds;
        return bounds.min.y;
    }
    
    public float? CalculateApproximateStandingRadius(){
        if( model.meshFilter ){
            var xz = new Vector2( model.meshFilter.mesh.bounds.extents.x, model.meshFilter.mesh.bounds.extents.z );
            return Mathf.Sqrt( Vector2.Dot( xz, xz ) )*Mathf.Max( model.meshFilter.transform.lossyScale.x, model.meshFilter.transform.lossyScale.z );
        }else if( model.skinnedMeshRenderer ){
            var xz = new Vector2( model.skinnedMeshRenderer.sharedMesh.bounds.extents.x, model.skinnedMeshRenderer.sharedMesh.bounds.extents.z );
            return Mathf.Sqrt( Vector2.Dot( xz, xz ) )*Mathf.Max( model.skinnedMeshRenderer.transform.lossyScale.x, model.skinnedMeshRenderer.transform.lossyScale.z );
        }
        return null;
    }

    public override void _InternalInitialize(){
        scriptManager.Recompile();

        _internalOnDestroy += delegate{
            foreach( var grabbable in grabbables ) grabbable.ReleaseAll();
            scriptManager.RemoveAllScripts();
            if( immovable ) Scene.main.BakeNavigation();

            model.textureBindingGroup.DiscardAll( true );
        };
    }

    public void OpenDialogForCommand( string title, bool worldSpace, bool commandSelection, string functionName ){
        var scriptDialogOptions = scriptManager.HandleRequestDialogOptionsAllScripts( functionName );
        if( scriptDialogOptions == null || scriptDialogOptions.Length == 0 ){
            Debugger.LogWarning("Item \""+name+"\" did not have any dialog options to display");
            return;
        }

        var item = this;
        if( worldSpace ){
            GameUI.main.TrackTarget( delegate{
                if( item == null ) return null;
                var targetPos = rigidBody.worldCenterOfMass;
                var diff = Camera.main.transform.position-targetPos;
                var diffL = diff.magnitude;
                if( diffL == 0 ) return Vector3.zero;

                diff /= diffL;
                var approxRadius = item.CalculateApproximateStandingRadius();
                return targetPos+diff*Mathf.Max( diffL-0.6f, 0.01f );
            } );
        }

        GameUI.main.OpenTab( "Items" );
        GameUI.main.SetHideDecorations( true );
        GameUI.main.SetEnableNav( false );
        GameUI.main.libraryExplorer.SetOverridePrepare( delegate{
            GameUI.main.libraryExplorer.DisplaySelection( title,

                //display the expanded script options
                GameUI.main.libraryExplorer.ExpandDialogOptions( ImportRequestType.ITEM, scriptDialogOptions ),
                delegate( DialogOption selectedOption, LibraryEntry source ){
                    CloseCommandDialogOptions();
                    if( commandSelection ){
                        for( int i=0; i<VivaPlayer.user.gestures.characterSelectionCount; i++ ){
                            var selected = VivaPlayer.user.gestures.GetSelectedCharacter(i);
                            if( selected == null ) continue;
                            scriptManager.CallOnAllScripts( "OnDialogOption", new object[]{ selected, selectedOption } );
                        }
                        VivaPlayer.user.gestures.ClearCharacterSelection();
                        Sound.main.PlayGlobalOneShot( VivaPlayer.user.gestures.gestureSounds[ (int)Gesture.MECHANISM ] );
                    }else{
                        scriptManager.CallOnAllScripts( "OnDialogOption", new object[]{ VivaPlayer.user.character, selectedOption } );
                    }
                },
                CloseCommandDialogOptions,
                BuiltInAssetManager.main.commandLibraryEntryPrefab
            );
        } );
    }

    private void CloseCommandDialogOptions(){
        GameUI.main.SetHideDecorations( false );
        GameUI.main.StopTrackingTarget();
        GameUI.main.SetEnableUI( false );
    }

    public void SetImmovable( bool _immovable ){
        immovable = _immovable;
        RefreshImmovable( immovable );
    }

    private void RefreshImmovable( bool asImmovable ){
        int targetLayer;
        if( enabled ){
            targetLayer = asImmovable ? WorldUtil.itemsStaticLayer : WorldUtil.itemsLayer;
            rigidBody.isKinematic = asImmovable;
        }else{
            targetLayer = WorldUtil.inertLayer;
            rigidBody.isKinematic = false;
        }
        model.renderer.gameObject.layer = targetLayer;
        foreach( var collider in colliders ){
            collider.gameObject.layer = targetLayer;
        }
        rigidBody.useGravity = !asImmovable;
        rigidBody.drag = asImmovable ? 10f : 0f;
        rigidBody.angularDrag = asImmovable ? 10f : defaultAngularDrag;
    }
    
    private void OnEnable(){
        if( destroyed ) return;
        
        RefreshImmovable( immovable );
        foreach( var grabbable in grabbables ){
            grabbable.enabled = true;
        }

        ResetModelMaterials();
        model.textureBindingGroup.Reset( true );
    }
    
    private void OnDisable(){
        if( destroyed ) return;

        RefreshImmovable( true );
        foreach( var grabbable in grabbables ){
            grabbable.enabled = false;
        }
        
        ResetModelMaterials("ghost");
        model.textureBindingGroup.Reset( true );
    }

    public void ResetModelMaterials( string shaderOverride=null ){
        if( itemSettings != null ){
            foreach( var materialSetting in itemSettings.materialSettings ){
                var shaderName = shaderOverride!=null ? shaderOverride : materialSetting.shader;
                var matTemplate = BuiltInAssetManager.main.FindMaterialByShader( shaderName );
                if( matTemplate == null ){
                    Debugger.LogError("Could not find shader name \""+shaderName+"\"");
                    matTemplate = BuiltInAssetManager.main.defaultModelMaterials[0];    //default to opaque
                }
                model.SetMaterial( materialSetting.material, new Material( matTemplate ) );
            }
        }
    }

    // public void ResetModelTextures(){
    //     if( itemSettings != null ){
    //         foreach( var textureBindingGroup in itemSettings.textureBindingGroups ){
    //             var targetModel = model.renderer.name==textureBindingGroup.rendererName ? model : model.FindChildModel( textureBindingGroup.rendererName );
    //             if( targetModel != null ){
    //                 targetModel.textureBindingGroup.DiscardAll();

    //                 for( int i=0; i<textureBindingGroup.Count; i++ ){
    //                     var texturebinding = textureBindingGroup[i];
    //                     targetModel.textureBindingGroup.Add( texturebinding );
    //                 }
    //                 targetModel.textureBindingGroup.Apply( false );
    //             }
    //         }
    //     }
    // }
}

}