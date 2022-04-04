using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace viva{

public delegate void CharacterCallback( Character character );

public partial class Character: VivaInstance{



    public class CharacterCallbackWrapper: InstanceCallbackWrapper{
        private readonly CharacterCallback func;
        public CharacterCallbackWrapper(CharacterCallback _func ){
            func = _func;
        }
        public override void Invoke( VivaInstance instance ){ func?.Invoke( instance as Character ); }
    }

    public static readonly InstanceManager instances = new InstanceManager( Character.root, ".character", ImportRequestType.CHARACTER );

    public static Character Spawn( string name, Vector3 position, Quaternion rotation, CharacterCallback onSpawn=null ){
        Character instance = null;
        instances._InternalSpawnAndLink( name, true, position, rotation, new CharacterCallbackWrapper( delegate( Character character ){
            instance = character;
            onSpawn?.Invoke(character);
        } ), null );
        return instance;
    }

    public static void _InternalSpawn( string name, bool instant, Vector3 position, Quaternion rotation, CharacterCallback onSpawn, CharacterCallback onPreInitialize ){
        instances._InternalSpawnAndLink( name, instant, position, rotation, new CharacterCallbackWrapper( onSpawn ), onPreInitialize==null ? null : new CharacterCallbackWrapper( onPreInitialize ) );
    }

    public static Character CreateCharacterBase( Model _model, CharacterSettings _characterSettings, int? id ){
        if( _model == null ){
            throw new System.Exception("Cannot create Character with a null model!");
        }
        if( _model.profile == null ){
            throw new System.Exception("Cannot create Character without a ragdoll profile");
        }
        if( _model.rootTransform.TryGetComponent<Character>( out Character character ) ){
            throw new System.Exception("Cannot create Character on a model that already has a Character!");
        }
        character = VivaInstance.CreateVivaInstance<Character>( _model.rootTransform.gameObject, _characterSettings, id );
        _model.usage.Increase();

        character.model = _model;
        character.scriptManager.onScriptFailure += character.OnScriptFailure;
        character.scriptManager.onScriptSuccess += character.OnScriptSuccess;

        character.autonomy = _model.rootTransform.gameObject.AddComponent<Autonomy>();
        character.nav = _model.rootTransform.gameObject.AddComponent<Nav>();
        character.autonomy.self = character;
        character.name = _characterSettings.name;
        return character;
    }

    public delegate void RagdollChangeCallback( Ragdoll oldRagdoll, Ragdoll newRagdoll );

    public static string root { get{ return Viva.contentFolder+"/Characters"; } }
    
    public ListenerCharacterString onAttributeChanged { get; private set; }
	/// <summary> The Model used by the character
    public Model model { get; private set; }
	/// <summary> The autonomy of the character for logic
    public Autonomy autonomy { get; private set; }
	/// <summary> The navigation of the character for logic
    public Nav nav { get; private set; }
	/// <summary> The physics ragdoll of the Character.
    public Ragdoll ragdoll { get; private set; } = null;
    public BipedRagdoll biped { get{ return ragdoll as BipedRagdoll; } }
    public AnimalRagdoll animal { get{ return ragdoll as AnimalRagdoll; } }
	/// <summary> The AnimationSet of the character. This is recreated every time scripts are reloaded
    public AnimationSet animationSet { get; private set; }
    public int mainAnimationLayerIndex { get; private set; } = 0;
    public int altAnimationLayerIndex { get; private set; } = 0;
    private GameObject animationLayersContainer = null;
	/// <summary> The List of all AnimationLayers. Always has at least 1 layer.
    public readonly List<AnimationLayer> animationLayers = new List<AnimationLayer>();
    public CharacterRequest characterRequest { get{ return characterRequest as CharacterRequest; } }
	/// <summary> The main AnimationLayer. Always includes the torso.
    public AnimationLayer mainAnimationLayer { get{ return animationLayers[ mainAnimationLayerIndex ]; } }
	/// <summary> The AnimationLayer for legs
    public AnimationLayer altAnimationLayer { get{ return animationLayers[ altAnimationLayerIndex ]; } }
    private List<CharacterIK> customIK = new List<CharacterIK>();
    private Dictionary<string,Weight> weights = new Dictionary<string, Weight>();
    private List<Grabber> _internalAllGrabbers = new List<Grabber>();
    public System.Collections.ObjectModel.ReadOnlyCollection<Grabber> allGrabbers { get{ return _internalAllGrabbers.AsReadOnly(); } }
	/// <summary> The armature lossyScale's x of the model used by this character
    public float scale { get{ return model.scale; } }
	/// <summary> The locomotion animation blend to go forward
    public WeightManager1D locomotionForward { get; private set; }
	/// <summary> The callback for receiving gestures
    public readonly ListenerGesture onGesture = new ListenerGesture( "onGesture" );
	/// <summary> The callback for sending gestures (not receiving)
    public readonly ListenerString onSendGesture = new ListenerString( null, "onSendGesture" );
	/// <summary> Returns if the character is currently being controlled by an actual player
    public VivaPlayer possessor { get; private set; }
    public bool isPossessed { get{ return possessor; } }
    public bool isPossessedByKeyboard { get{ return possessor && possessor.isUsingKeyboard; } }
    public bool isPossessedByVR { get{ return possessor && possessor.isUsingVR; } }
	public InteractionSolver itemInteractions { get; private set; } = new InteractionSolver();
	/// <summary> The callback for whenever this character is selected
    public readonly ListenerGeneric onSelected = new ListenerGeneric( "onSelected" );
    public readonly ListenerWater onWater = new ListenerWater( "onWater" );
    public string emotion { get; private set; } = "happy";
    public CharacterDetector characterDetector { get; private set; }
    public bool isAnimal { get{ return animal!=null; } }
    public bool isBiped { get{ return animal==null; } }
    public CharacterSettings characterSettings { get{ return _internalSettings as CharacterSettings; } }
    public bool isBeingGrabbed { get{ return ragdoll.isBeingGrabbed; } }

    private bool initialized = false;
    public GenericCallback onReset;
    public RagdollChangeCallback onRagdollChange;
    private GenericCallback locomotionFunc;
    private GenericCallback armatureRetargetFunc;
    public SoundHandle lastVoiceHandle = null;
    private BipedMask[] animationLayerMasks = null;
    private BipedMask[] muscleLayerMasks = null;
    private string lastGroundType;


    
	/// <summary> Sets the animationLayers for multi layered animations.
    /// <param name="_muscleLayerMasks"> The array of RagdollMask to specify which muscles each layer will animate.</param>
    /// <param name="_animationLayerMasks"> The optional corresponding array of RagdollMask to specify which bones each layer will animate.</param>
    public void SetBipedAnimationLayers( BipedMask[] _muscleLayerMasks, BipedMask[] _animationLayerMasks, int _mainLayerIndex, int _legLayerIndex ){
        if( _muscleLayerMasks == null ){
            _muscleLayerMasks = new BipedMask[]{ BipedMask.ALL };
        }
        if( _muscleLayerMasks.Length < 1 ){
            Debugger.LogError( "SetAnimationLayers must be fed a RagdollBoneMask[] of at least size 1" );
            return;
        }
        if( _animationLayerMasks != null && _muscleLayerMasks.Length != _animationLayerMasks.Length ){
            Debugger.LogError( "muscleMasks must be the same length as animMasks" );
            return;
        }
        muscleLayerMasks = _muscleLayerMasks;
        animationLayerMasks = _animationLayerMasks;
        mainAnimationLayerIndex = _mainLayerIndex;
        altAnimationLayerIndex = _legLayerIndex;
    }

    public override float? CalculateApproximateRadius(){
        var bounds = model.bounds;
        if( bounds.HasValue ) return null;
        return Mathf.Max( bounds.Value.extents.x, bounds.Value.extents.z );
    }

    public override float? CalculateApproximateFloorY(){
        var bounds = model.bounds;
        if( bounds.HasValue ) return null;
        return bounds.Value.min.y;
    }

    public Grabber IsGrabbing( VivaInstance instance ){
        foreach( var grabber in allGrabbers ){
            if( grabber.IsGrabbing( instance ) ) return grabber;
        }
        return null;
    }
    public Grabber IsGrabbing( string attribute ){
        foreach( var grabber in allGrabbers ){
            if( grabber.IsGrabbing( attribute ) ) return grabber;
        }
        return null;
    }
    
    public List<GrabContext> GetGrabContexts( Character targetCharacter ){
        var contexts = new List<GrabContext>();
        foreach( var muscle in ragdoll.muscles ){
            foreach( var grabbable in muscle.grabbables ){
                if( !grabbable ) continue;
                contexts.AddRange( grabbable.GetGrabContextsByCharacter( targetCharacter ) );
            }
        }
        return contexts;
    }
    
    public List<GrabContext> GetGrabContexts( Grabber grabber ){
        var contexts = new List<GrabContext>();
        foreach( var muscle in ragdoll.muscles ){
            foreach( var grabbable in muscle.grabbables ){
                if( !grabbable ) continue;
                contexts.AddRange( grabbable.GetGrabContexts( grabber ) );
            }
        }
        return contexts;
    }
    
    public bool IsGrabbedByAnyCharacter(){
        var contexts = new List<GrabContext>();
        foreach( var muscle in ragdoll.muscles ){
            foreach( var grabbable in muscle.grabbables ){
                if( !grabbable || !grabbable.parentCharacter ) continue;
                return true;
            }
        }
        return false;
    }

    public void _InternalSetPossessor( VivaPlayer _possessor ){ 
        possessor = _possessor;
        model.skinnedMeshRenderer.updateWhenOffscreen = _possessor!=null;
        if( possessor ){
            BindInput();
            scriptManager.LoadAllScripts( "character", new string[]{"recipes","balance"} );
        }else{
            UnbindInput();
            
        }
        if( biped ) biped.SetMusclePinMode( possessor );
    }

    public void _InternalRegisterGrabber( Grabber grabber ){
        if( _internalAllGrabbers.Contains( grabber ) ) return;
        _internalAllGrabbers.Add( grabber );
        grabber.onGrabbed._InternalAddListener( OnCustomGrabbedGrabbable );
        grabber.onReleased._InternalAddListener( OnCustomReleasedGrabbable );
    }
    private void OnCustomGrabbedGrabbable( GrabContext grabContext ){
        SetIgnoreGrabbableWithBody( grabContext.grabbable, true );
    }
    private void OnCustomReleasedGrabbable( GrabContext grabContext ){
        SetIgnoreGrabbableWithBody( grabContext.grabbable, false );
    }

    public void SetIgnoreGrabbableWithBody( Grabbable grabbable,  bool ignore ){
        if( grabbable == null ) return;

        ragdoll.OnSetIgnoreGrabbableWithBody( grabbable, ignore );
        
        if( grabbable.parentItem ){
            if( !isPossessed ){
                foreach( var grabber in allGrabbers ){
                    for( int i=0; i<grabber.contextCount; i++ ){
                        var context = grabber.GetGrabContext(i);
                        context?.grabbable?.parentItem?.SetIgnorePhysics( grabbable.parentItem, ignore );
                    }
                }
            }
        }
    }

    //return to factory settings from a fresh spawn
    public override void _InternalReset(){
        animationSet._InternalReset();
        SetupAnimationLayers();
        autonomy._InternalReset();
        
        var allGrabbersCopy = _internalAllGrabbers.ToArray();
        for( int i=allGrabbersCopy.Length; i-->0; ){
            var grabber = allGrabbersCopy[i];
            grabber._InternalReset();
            if( !grabber._internalBuiltIn ){
                GameObject.Destroy( grabber );
                _internalAllGrabbers.RemoveAt(i);
            }
        }
        input._InternalReset();
        onAttributeChanged._InternalReset();
        onScroll._InternalReset();
        itemInteractions._InternalReset();
        foreach( var ik in customIK ) ik.Kill( false );
        characterDetector._InternalReset();
        ragdoll._InternalReset( this );
        SetLocomotionFunction( null );
        SetArmatureRetargetFunction( null );
        onGesture._InternalReset();
        onSendGesture._InternalReset();
        onSelected._InternalReset();
        onWater._InternalReset();
        
        m_attributes.Clear();
        foreach( var attrib in characterSettings.attributes ) AddAttribute( attrib );

        onReset?.Invoke();
        if( biped ) BuiltInAssetManager.main.SetupBuiltInAnimationSets( animationSet, this );
        
        if( !isAnimal ){
            altAnimationLayer.player._InternalPlay( animationSet.GetBodySet( isPossessed ? "stand legs" : "stand")["idle"] );
        }
    }

    private void Awake(){
        enabled = false;

        onAttributeChanged = new ListenerCharacterString( this, m_attributes, "onAttributeChanged" );

        _internalOnAttributeChanged += onAttributeChanged.Invoke;
    }

    private void SetupAnimationLayers(){

        if( animationLayersContainer ){
            GameObject.Destroy( animationLayersContainer );
        }
        animationLayersContainer = new GameObject( "Animation Layers" );
        animationLayersContainer.transform.SetParent( model.rootTransform, false );
        
        animationLayers.Clear();
        if( biped ){
            for( int i=0; i<muscleLayerMasks.Length; i++ ){
                BipedMask muscleMask = muscleLayerMasks[i];
                BipedMask animMask = animationLayerMasks!=null ? animationLayerMasks[i] : muscleMask;
                
                var animationPlayer = AnimationPlayer.Create( animationLayersContainer, i );
                var animationLayer = new AnimationLayer( animationPlayer, this );
                var humanMuscles = BipedProfile.RagdollMaskToBones( muscleMask );
                var animBones = BipedProfile.RagdollMaskToBones( animMask );

                animationLayer.BindForBiped( model, humanMuscles.ToArray(), animBones.ToArray() );
                animationPlayer.BindAnimationLayer( animationLayer, model.skinnedMeshRenderer );

                animationLayers.Add( animationLayer );
            }
        }else{
            var animationPlayer = AnimationPlayer.Create( animationLayersContainer, 0 );
            var animationLayer = new AnimationLayer( animationPlayer, this );

            animationLayer.BindForAnimal( model.skinnedMeshRenderer );
            animationPlayer.BindAnimationLayer( animationLayer, model.skinnedMeshRenderer );

            animationLayers.Add( animationLayer );
        }

        altAnimationLayer.player.onModifyAnimation += delegate{
            model.ZeroOutDeltaTransform();
            model.ApplySpineTPoseDeltas();  ///TODO: ONLY APPLY TO THOSE IN LAYERS MASK
        };

        mainAnimationLayer.player.onAnimate += ApplyLateAnimationModifiers;

        //pin to ragdoll last
        foreach( var animationLayer in animationLayers ){
            animationLayer.player.onAnimate += delegate{
                ragdoll.PinToAnimation( animationLayer );
            };
            animationLayer._InternalInitialize();
        }
        altAnimationLayer.player.onAnimate += ragdoll.SnapArmatureToPhysics;
    }
    
	/// <summary> Searches all character weights with the specified name. If not present it will create a new one.
    /// <returns> Weight: The weight with the specified name. 
    public Weight GetWeight( string name ){
        Weight weight;
        if( !weights.TryGetValue( name, out weight ) ){
            weight = new Weight();
            weights[ name ] = weight;
        }
        return weight;
    }

	/// <summary> Adds a new custom CharacterIK. This should only be done for bones that do not already have IK.
    public void AddIK( CharacterIK ik ){
        if( ik == null ){
            Debugger.LogError("Cannot add a null IK");
            return;
        }
        foreach( var candidate in customIK ){
            if( candidate.name == ik.name ){
                Debugger.LogWarning("Already have an IK with the name \""+ik.name+"\"");
                return;
            }
        }
        if( !customIK.Contains( ik ) ) customIK.Add( ik );
    }
    
    public override void TeleportTo( Vector3 position, Quaternion rotation ){
        if( ragdoll ){
            ragdoll.transform.position = position;
            ragdoll.transform.rotation = rotation;

            ragdoll.movementBody.velocity = Vector3.zero;
            ragdoll.movementBody.angularVelocity = Vector3.zero;
            foreach( var muscle in ragdoll.muscles ){
                muscle.rigidBody.velocity = Vector3.zero;
                muscle.Read();
            }
        }
    }

    public override void _InternalInitialize(){
        if( initialized ) return;
        initialized = true;
        animationSet = new AnimationSet( this );

        for( int i=0; i<System.Enum.GetValues(typeof(Input)).Length; i++ ){
            Input val = (Input)i;
            string inputId = val.ToString();
            input.AddInputButton( inputId );
        }

        model.rootTransform.gameObject.SetActive( true );
        enabled = true;
        SetBipedAnimationLayers( null, null, 0, 0 );

        RebuildRagdoll();

        _internalOnDestroy += delegate{
            if( ragdoll ) GameObject.DestroyImmediate( ragdoll.gameObject );
            scriptManager.RemoveAllScripts();
            UnbindInput();
            model.usage.Decrease();
        };

        if( string.IsNullOrWhiteSpace( characterSettings.voice ) ) Debugger.LogWarning("Character does not have a voice set");
        else Sound.PreloadSet( characterSettings.voice );

        locomotionForward = new WeightManager1D( new Weight[]{
            GetWeight("idle"),
            GetWeight("walking"),
            GetWeight("running"),
        }, new float[]{ 0, 0.5f, 1.0f });
    }

    public void SetLocomotionFunction( GenericCallback newFunc ){
        if( newFunc == null ){
            newFunc = DefaultLocomotion;  //default locomotion func
        }
        if( locomotionFunc != null ) animationLayers[0].player.onAnimate -= locomotionFunc;
        locomotionFunc = newFunc;
        animationLayers[0].player.onAnimate += locomotionFunc;
    }

    public void SetArmatureRetargetFunction( GenericCallback newFunc ){
        if( newFunc == null ){
            newFunc = ragdoll.RetargetArmature;  //default armature retarget func
        }
        if( armatureRetargetFunc != null ) ragdoll.onPreMap -= armatureRetargetFunc;
        armatureRetargetFunc = newFunc;
        ragdoll.onPreMap += armatureRetargetFunc;
    }

    //checks for physics ground
    public void DefaultLocomotion(){
        if( !ragdoll.surface.HasValue && !ragdoll.isOnWater ){
            return;
        }

        var targetVel = altAnimationLayer.player.currentState == null ? Vector3.zero : altAnimationLayer.player.currentState.deltaPosition/Time.fixedDeltaTime;
        if( model.bipedProfile != null ) targetVel *= model.bipedProfile.hipHeight*scale;
        targetVel = model.armature.rotation*targetVel;
        targetVel.y = 0;
        
        ApplyLocomotion( targetVel );
    }

    public void ApplyLocomotion( Vector3 targetVel ){
        if( targetVel.sqrMagnitude > Mathf.Epsilon ){

            var oldVelY = ragdoll.movementBody.velocity.y;
            var newVel = Vector3.ClampMagnitude( ragdoll.movementBody.velocity+targetVel, targetVel.magnitude );
            newVel.y = oldVelY;
            ragdoll.movementBody.velocity = newVel*ragdoll.pinLimit.value;
            ragdoll.capsuleCollider.material = BuiltInAssetManager.main.capsuleAir;
        }else{
            ragdoll.capsuleCollider.material = BuiltInAssetManager.main.capsuleGround;
        }
    }

    public void ApplyLateAnimationModifiers(){
        
        ragdoll.OnApplyLateAnimationModifiers( this );

        for( int i=customIK.Count; i-->0; ){
            var ik = customIK[i];
            if( !ik.alive && !ik.hasHandles ){
                customIK.RemoveAt(i);
            }else{
                ik.Apply();
            }
        }
    }

    public void OnScriptFailure(){
        foreach( var animationLayer in animationLayers ) animationLayer.player.enabled = false;
    }
    public void OnScriptSuccess(){
        foreach( var animationLayer in animationLayers ) animationLayer.player.enabled = true;
    }

    public void RebuildRagdoll(){
        Vector3? oldRebuidFramePos = null;
        Quaternion oldHipsRotation = Quaternion.identity;
        bool reset = false;
        if( ragdoll ){
            oldRebuidFramePos = ragdoll.root.rigidBody.transform.position;
            oldHipsRotation = ragdoll.root.rigidBody.transform.rotation;
            model.armature.SetParent( null, true );
            if( possessor ) possessor.controls.transform.SetParent( null, true );
            GameObject.Destroy( ragdoll.gameObject );
            reset = true;
        }
        var oldRotation = transform.rotation;
        transform.rotation = Quaternion.identity;

        //execute recreate ragdoll
        var oldRagdoll = ragdoll;
        ragdoll = model.RebuildRagdoll( this );

        model.skinnedMeshRenderer.transform.SetParent( ragdoll.root.target, true );
        model.ApplySpawnPose( false );  //tpose

        model.armature.SetParent( ragdoll.transform, true );
        model.armature.localPosition = Vector3.zero;
        model.armature.localRotation = Quaternion.identity;
        if( possessor ){
            possessor.controls.transform.SetParent( ragdoll.transform, true );
            possessor.controls.transform.localPosition = Vector3.zero;
            possessor.controls.transform.localRotation = Quaternion.identity;
        }

        //destroy old detector
        if( characterDetector ) GameObject.DestroyImmediate( characterDetector );
        characterDetector = CharacterDetector.Create( ragdoll.root.rigidBody.transform, this );

        ragdoll.capsuleCollider.radius = 0.1f/ragdoll.transform.lossyScale.x;
        
        scriptManager.Recompile();

        onRagdollChange?.Invoke( oldRagdoll, ragdoll );

        //make ragdoll face direction of Character
        transform.rotation = oldRotation;
        if( oldRebuidFramePos.HasValue ){
            ragdoll.root.rigidBody.transform.position = oldRebuidFramePos.Value;
            ragdoll.root.rigidBody.transform.rotation = oldHipsRotation;
        }
        if( reset ) _InternalReset();
    }

    public Vector3 debug;

    public void PlayVoiceGroup( string group ){
        if( string.IsNullOrWhiteSpace( group ) ) return;
        if( string.IsNullOrWhiteSpace( characterSettings.voice ) ) return;
        if( lastVoiceHandle != null ) lastVoiceHandle.Stop();
        var sourceBody = biped ? biped.head.rigidBody : animal.root.rigidBody;
        lastVoiceHandle = Sound.Create( sourceBody.centerOfMass, sourceBody.transform );
        lastVoiceHandle.Play( Sound.Load( characterSettings.voice, group ) );
    }

    public Grabber GetGrabber( RagdollMuscle muscleIndex ){
        return Grabber._InternalCreateGrabber( this, ragdoll, muscleIndex );
    }

    public void Footstep( bool rightFoot ){
        if( !ragdoll.surface.HasValue || !ragdoll.surfaceCollider || ragdoll.isOnWater ) return;
        var groundMatName = ragdoll.surfaceCollider.material.name;
        groundMatName = groundMatName.Replace(" (Instance)","");    //Unity does some weird runtime renaming of physicMaterials

        var pos = rightFoot ? biped.rightFoot.target.position : biped.leftFoot.target.position;
        var handle = Sound.Create( pos );
        handle.volume = 0.6f;
        handle.pitch = 0.6f+Random.value*0.6f;
        var sound = Sound.Load( "footsteps", groundMatName );
        if( sound ){
            lastGroundType = groundMatName;
        }else{
            sound = Sound.Load( "footsteps", lastGroundType );
        }
        handle.Play( sound );    
    }
}

}