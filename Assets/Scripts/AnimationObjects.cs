using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;


namespace viva{


[System.Serializable]
public class AnimationFrameSet{
    
    [SerializeField]
    private float[] m_frames;
    public float[] frames { get{ return m_frames; } }

    public AnimationFrameSet( float[] _frames ){
        m_frames = _frames;
    }

    public AnimationFrameSet( AnimationFrameSet copy ){
        m_frames = new float[ copy.m_frames.Length ];
        for( int i=0; i<m_frames.Length; i++ ){
            m_frames[i] = copy.m_frames[i];
        }
    }

    public float Sample( int frameA, int frameB, float ratio ){
        float a = frames[ Mathf.Min( frameA, frames.Length-1 ) ];
        float b = frames[ Mathf.Min( frameB, frames.Length-1 ) ];
        return a+(b-a)*ratio;
    }

    public float Sample( int frameA, int frameB, float ratio, float offsetA, float offsetB ){
        
        float a = frames[ Mathf.Min( frameA, frames.Length-1 ) ]+offsetA;
        float b = frames[ Mathf.Min( frameB, frames.Length-1 ) ]+offsetB;
        return a+(b-a)*ratio;
    }

    public float Sample( int frame ){
        return frames[ Mathf.Clamp( frame, 0, frames.Length-1 ) ];
    }
}

[System.Serializable]
public class AnimationChannel{
    
    public enum Channel{
        POSITION,
        ROTATION,
        SCALE,
        BLENDSHAPE
    }

    [SerializeField]
    private Channel m_channel;
    public Channel channel { get{ return m_channel; } }
    [SerializeField]
    public string m_bindTarget;
    public string bindTarget { get{ return m_bindTarget; } }
    [SerializeField]
    private AnimationFrameSet[] m_frameSets;
    public AnimationFrameSet[] frameSets { get{ return m_frameSets; } }
    [SerializeField]
    public int m_bindTargetHash;
    public int bindTargetHash { get{ return m_bindTargetHash; } }


    public AnimationChannel( Channel _channel, string _bindTarget, int _bindTargetHash, AnimationFrameSet[] _frameSets ){
        m_channel = _channel;
        m_bindTarget = _bindTarget;
        m_frameSets = _frameSets;
        m_bindTargetHash = _bindTargetHash;
    }

    public AnimationChannel( AnimationChannel copy ){
        m_channel = copy.m_channel;
        m_bindTarget = copy.m_bindTarget;
        m_frameSets = new AnimationFrameSet[ copy.m_frameSets.Length ];
        for( int i=0; i<m_frameSets.Length; i++ ){
            m_frameSets[i] = new AnimationFrameSet( copy.m_frameSets[i] );
        }
        m_bindTargetHash = copy.m_bindTargetHash;
    }

    public void Mirror(){
        if( channel == AnimationChannel.Channel.ROTATION ){
            for( int i=0; i<frameSets[0].frames.Length; i++ ){
                frameSets[0].frames[i] = -frameSets[0].frames[i];
                frameSets[3].frames[i] = -frameSets[3].frames[i];
            }
            if( bindTarget.EndsWith("_R") ){
                    m_bindTarget = Regex.Replace( m_bindTarget, "_R$", "_L" );
                }else{
                    m_bindTarget = Regex.Replace( m_bindTarget, "_L$", "_R" );
            }
        }else{
            if( channel == AnimationChannel.Channel.POSITION ){
                for( int i=0; i<frameSets[0].frames.Length; i++ ){
                    frameSets[0].frames[i] = -frameSets[0].frames[i];
                }
                if( bindTarget.EndsWith("_R") ){
                    m_bindTarget = Regex.Replace( m_bindTarget, "_R$", "_L" );
                }else{
                    m_bindTarget = Regex.Replace( m_bindTarget, "_L$", "_R" );
                }
            }
        }
        m_bindTargetHash = m_bindTarget.GetHashCode();
    }

    public void RenameBindTarget( string _bindTarget ){
        m_bindTarget = _bindTarget;
        m_bindTargetHash = _bindTarget.GetHashCode();
    }

    public int GetRequiredDataCount(){
        switch( channel ){
        case Channel.POSITION:
        case Channel.SCALE:
            return 3;
        case Channel.ROTATION:
            return 4;
        case Channel.BLENDSHAPE:
            return 1;
        default:
            return 0;
        }
    }

    public void Sample( float[] data, int frameA, int frameB, float ratio ){
        for( int i=0; i<frameSets.Length; i++ ){
            data[i] = frameSets[i].Sample( frameA, frameB, ratio );
        }
    }
    public void Sample( float[] data, int frameA, int frameB, float ratio, Vector3 offsetA, Vector3 offsetB ){
        for( int i=0; i<frameSets.Length; i++ ){
            data[i] = frameSets[i].Sample( frameA, frameB, ratio, offsetA[i], offsetB[i] );
        }
    }
}


public class BindSample{
    public readonly int bindHash;
    public readonly AnimationChannel.Channel channel;
    public readonly float[] data;
    public readonly object source;
    public int bindID = 0;

    public static int bindCounter = 0;

    public BindSample( int _bindHash, AnimationChannel.Channel _channel, object _source, int dataLength ){
        bindHash = _bindHash;
        channel = _channel;
        source = _source;
        data = new float[ dataLength ];
    }
}

public abstract class AnimationNode{

    public readonly string name;
    public List<BindSample> samples = new List<BindSample>();
    public Vector3 deltaPosition = Vector3.zero;
    public AnimationNode nextState = null;
    public float defaultTransitionTime = 0.38f;
    public readonly Dictionary<int,Curve> curves = new Dictionary<int, Curve>();
    private List<Event> events;
    public AnimationLayer _internalLayerAssigned { get; protected set; }

    public void AddEvent( Event newEvent ){
        if( newEvent == null ) throw new System.Exception("Cannot add a null event");
        if( events == null ) events = new List<Event>();
        foreach( var oldEvent in events ){
            if( oldEvent.Equals( newEvent ) ){
                return;
            }
        }
        events.Add( newEvent );
    }
    public virtual void Reset(){
        if( events == null ) return;
        foreach( var eventMethod in events ){
            eventMethod.position = eventMethod.position%1.0f;
        }
        events.Sort( (a,b)=>a.position.CompareTo(b.position) );
    }
    protected virtual void StepEvents( float position, Character character ){
        if( events == null ) return;
        for( int i=0; i<events.Count; i++ ){
            var eventMethod = events[i];
            if( eventMethod.position <= position ){
                float orig = eventMethod.position%1.0f;
                eventMethod.position = Mathf.Floor( position )+orig;
                if( eventMethod.position <= position ) eventMethod.position++;
                eventMethod.Fire( character );
            }
        }
    }
    
    public abstract Vector3 startDeltaLocalPos { get; }
    public abstract int maxFrames { get; }

    public AnimationNode( string _name ){
        name = _name;
    }

    public abstract void Read( AnimationContext context, bool playEvents=true );
    public abstract void AddToContextStack( AnimationContext context );
    public abstract void RemoveFromContextStack( AnimationContext context );
}

public class AnimationContext{

    private List<PlaybackState> stack = new List<PlaybackState>();
    public AnimationNode activeState;
    public PlaybackState mainPlaybackState;
    public PlaybackState lastMainPlaybackState;
    private int index = 0;
    public readonly LimitGroup speed = new LimitGroup( null, -16, 16, true );
    public readonly AnimationPlayer animationPlayer;
    public readonly Character character;
    public VivaScript source;

    public AnimationContext( AnimationPlayer _animationPlayer, Character _character ){
        animationPlayer = _animationPlayer;
        character = _character;
    }

    public void Restart(){
        index = 0;
    }
    public void Reset(){
        stack.Clear();
        activeState = null;
        mainPlaybackState = null;
        speed._InternalReset();
        lastMainPlaybackState = null;
    }
    public void Insert( AnimationNode state ){
        var playbackState = new PlaybackState( state.maxFrames, state.startDeltaLocalPos, state.name );
        stack.Insert( 0, playbackState );
        if( animationPlayer.currentState == state ){
            mainPlaybackState = playbackState;
        }
    }
    public void Add( AnimationNode state, float defaultSpeed=1.0f ){
        var playbackState = new PlaybackState( state.maxFrames, state.startDeltaLocalPos, state.name );
        if( defaultSpeed != 1.0f ) playbackState.speed.Add( state.name, defaultSpeed );
        stack.Add( playbackState );
        if( animationPlayer.currentState == state ){
            lastMainPlaybackState = mainPlaybackState;
            mainPlaybackState = playbackState;
        }
    }
    public void Pop(){
        stack.RemoveAt(0);
    }
    public PlaybackState Next(){
        return stack[ index++ ];
    }
    public PlaybackState Peek(){
        return stack[ index ];
    }
}

public class PlaybackState{
    
    public readonly int maxFrames;
    public float frame { get; private set; }
    public int frameA { get; private set; }
    public int frameB { get; private set; }
    public float ratio { get; private set; }
    public int loopsA { get; private set; }
    public int loopsB { get; private set; }
    public float normalizedTime { get{ return Mathf.Max( frame/maxFrames, (float)loopsB ); } }
    public Vector3 lastDeltaLocalPos;
    public string source;
    public readonly LimitGroup speed = new LimitGroup( null, -16, 16, true );

    public PlaybackState( int _maxFrames, Vector3 _startDeltaLocalPos, string _source ){
        maxFrames = _maxFrames;
        frame = 0;
        lastDeltaLocalPos = _startDeltaLocalPos;
        Constrain(); 
        source = _source;
    }

    public void SetNormalizedTime( float normTime ){
        frame = normTime*maxFrames;
        Constrain();
    }

    public void Constrain(){
        
        float readFrame = frame >= 0 ? frame : frame+( Mathf.FloorToInt( -(float)frame/maxFrames )+1 )*maxFrames;

        float normFrame = readFrame%maxFrames;
        frameA = (int)normFrame;
        frameB = ( frameA+1 )%maxFrames;
        ratio = normFrame-frameA;

        loopsA = Mathf.FloorToInt( frame/maxFrames );
        loopsB = Mathf.FloorToInt( (frame+1)/maxFrames );
    }

    public void Advance( float frameStep, bool allowLooping ){
        if( allowLooping ){
            frame += frameStep;
            Constrain();
        }else{
            int maxLoop = Mathf.Max( 1, Mathf.CeilToInt( frame/maxFrames ) );
            int maxFrame = maxLoop*maxFrames;
            frame += frameStep;
            if( frameStep >= 0 && frame+1 >= maxFrame ){
                frame = maxFrame-0.001f;
                frameA = maxFrames-1;
                frameB = 0;
                ratio = 0.0f;
                loopsA = maxLoop-1;
                loopsB = maxLoop;
            }else if( frameStep < 0 ){ 
                frame = 0;
                Constrain();
            }else{
                Constrain();
            }
        }
    }
}

public class AnimationLayer{

    public readonly AnimationPlayer player;
    public readonly Character character;
    public readonly Dictionary<int,Transform> transformBindings = new Dictionary<int, Transform>();
    public readonly Dictionary<int,int> blendShapeBindings = new Dictionary<int,int>();
    public int[] bipedMuscleIndices { get; private set; } = null;
    public BodySet currentBodySet { get; private set; } = null;
    private BodySet m_nextBodySet;
    public BodySet nextBodySet { get{ return FindNextBodySet(); } }
    private List<string> currentGroups = new List<string>();

    public AnimationLayer( AnimationPlayer _player, Character _character ){
        player = _player;
        character = _character;
    }

    private BodySet FindNextBodySet(){
        if( m_nextBodySet == null ){
            if( player.currentState != null ){
                if( player.currentState.nextState == player.currentState ){
                    m_nextBodySet = currentBodySet;
                }else{
                    m_nextBodySet = character.animationSet.FindBodySet( player.currentState.nextState );
                }
            }else{
                m_nextBodySet = currentBodySet;
            }
        }
        return m_nextBodySet;
    }

    public void _InternalInitialize(){
        player.onAnimationChange += delegate{
            if( character == null ) return;
            var newAnimBodySet = character.animationSet.FindBodySet( player.currentState );
            m_nextBodySet = null; //force recalculate whenever its next requested

            currentGroups.Clear();
            if( newAnimBodySet != null ){
                currentBodySet = newAnimBodySet;
                currentBodySet._InternalGetNodeNames( player.currentState, currentGroups );
            }else{
                Debugger.Log(player.currentState.name+" has no parent bodySet");
            }
        };
    }

    public bool IsPlaying( string animationNode ){
        return currentGroups.Contains( animationNode );
    }

    
    public void BindForAnimal( SkinnedMeshRenderer skinnedMeshRenderer ){
        transformBindings.Clear();
        foreach( var bone in skinnedMeshRenderer.bones ){
            transformBindings[ bone.transform.name.GetHashCode() ] = bone;
        }
        blendShapeBindings.Clear();
        if( skinnedMeshRenderer.sharedMesh ){
            //stores (original blendShape name hash) => blendShape index
            for( int i=0; i<skinnedMeshRenderer.sharedMesh.blendShapeCount; i++ ){
                blendShapeBindings[ skinnedMeshRenderer.sharedMesh.GetBlendShapeName(i).GetHashCode() ] = i;
            }
        }
        Debug.Log("Built "+transformBindings.Count+" bone bindings (SMR)");
    }

    public void BindForBiped( Model model, BipedBone[] muscleMask=null, BipedBone[] animBones=null ){
        SetBipedMuscles( muscleMask );
        SetBipedBones( model.bipedProfile, animBones );
        SetipedBlendshapes( model );
    }

    private void SetipedBlendshapes( Model model ){
        blendShapeBindings.Clear();
        if( model.skinnedMeshRenderer ){
            for( int i=0; i<model.profile.blendShapeBindings.Length; i++ ){
                var blendShapeBinding = model.profile.blendShapeBindings[i];
                var index = -1;

                if( blendShapeBinding != null ) index = model.skinnedMeshRenderer.sharedMesh.GetBlendShapeIndex( blendShapeBinding );
                if( index != -1 ){
                    //stores (RagdollBlendShape name hash) => blendShape index
                    blendShapeBindings[ ((RagdollBlendShape)i).ToString().GetHashCode() ] = index;
                }
            }
        }
    }
    public void SetBipedMuscles( BipedBone[] muscleMask ){
        if( muscleMask != null ){
            var muscleList = new List<int>( muscleMask.Length );
            foreach( var ragdollBone in muscleMask ){
                int muscleIndex = BipedProfile.GetMuscleIndex( ragdollBone );
                if( muscleIndex >= 0 ) muscleList.Add( muscleIndex );
            }
            bipedMuscleIndices = muscleList.ToArray();
        }else{
            bipedMuscleIndices = null;
        }
    }

    public void SetBipedBones( BipedProfile profile, BipedBone[] animBones=null ){
        transformBindings.Clear();
        foreach( var boneInfo in profile.bones ){
            //only include those specified in the mask
            if( boneInfo != null ){
                if( animBones != null && !System.Array.Exists( animBones, element => element == boneInfo.name ) ) continue;
                if( boneInfo.transform != null ){
                    //Bind using enum string names
                    transformBindings[ boneInfo.name.ToString().GetHashCode() ] = boneInfo.transform;
                }
            }
        }
    }
}

public class AnimationSingle: AnimationNode{

    public Animation animation;
    private BindSample deltaPosSample;
    private Vector3 deltaOffset = Vector3.zero;
    public readonly float defaultSpeed=1.0f;

    private int clipMaxFrames;
    private Vector3 clipStartDeltaLocalPos;

    public override Vector3 startDeltaLocalPos { get{ return clipStartDeltaLocalPos; } }
    public override int maxFrames { get{ return clipMaxFrames; } }

    public AnimationSingle( Animation _animation, Character character, bool loop, float _defaultSpeed=1.0f, int? animationLayerIndex=null ):base(_animation.name){
        if( character == null ) throw new System.Exception("Character is null for AnimationSingle \""+_animation.name+"\"");
        if( !animationLayerIndex.HasValue ) animationLayerIndex = character.altAnimationLayerIndex;
        if( animationLayerIndex < 0 || animationLayerIndex >= character.animationLayers.Count )
            throw new System.Exception("Animation layer index out of bounds for AnimationSingle \""+_animation.name+"\"");
        Setup( _animation, character.animationLayers[ animationLayerIndex.Value ], character.model.deltaTransformBindHash );
        defaultSpeed = _defaultSpeed;
        if( loop ) nextState = this;

    }
    
    public AnimationSingle( Animation _animation, AnimationLayer animationLayer, int deltaTransformBindHash, bool loop, float _defaultSpeed=1.0f ):base(_animation.name){
        Setup( _animation, animationLayer, deltaTransformBindHash );
        defaultSpeed = _defaultSpeed;
        if( loop ) nextState = this;
    }

    private void Setup( Animation _animation, AnimationLayer animationLayer, int deltaTransformBindHash ){
        animation = _animation;
        _internalLayerAssigned = animationLayer;
        if( animation == null ) throw new System.Exception("Animation is null");
        if( animationLayer == null ) throw new System.Exception("AnimationLayer is null");
        clipMaxFrames = Mathf.FloorToInt( animation.duration*animation.baseFramerate );
        for( int i=0; i<animation.channels.Length; i++ ){
            var channel = animation.channels[i];
            if( channel.channel == AnimationChannel.Channel.BLENDSHAPE ){
                if( animationLayer.blendShapeBindings.ContainsKey( channel.bindTargetHash ) ){  
                    var bindSample = new BindSample( channel.bindTargetHash, channel.channel, channel, channel.GetRequiredDataCount() );
                    samples.Add( bindSample );
                }
            }else{
                if( animationLayer.transformBindings.ContainsKey( channel.bindTargetHash ) ){
                    var bindSample = new BindSample( channel.bindTargetHash, channel.channel, channel, channel.GetRequiredDataCount() );
                    samples.Add( bindSample );
                    if( animation.enableRootMotion && channel.bindTargetHash == deltaTransformBindHash && channel.channel == AnimationChannel.Channel.POSITION ){
                        deltaPosSample = bindSample;

                        //pre sample to initialize last delta local pos
                        channel.Sample( deltaPosSample.data, maxFrames-1, 0, 0.0f );
                        deltaOffset = new Vector3( deltaPosSample.data[0], deltaPosSample.data[1], deltaPosSample.data[2] );
                        deltaOffset.y = 0.0f;
                        //add extrapolation of last 2 frames
                        channel.Sample( deltaPosSample.data, maxFrames-2, maxFrames-1, 0.0f );
                        var extrapOffset = new Vector3( deltaPosSample.data[0], deltaPosSample.data[1], deltaPosSample.data[2] );
                        extrapOffset.y = 0.0f;

                        deltaOffset += deltaOffset-extrapOffset;

                        channel.Sample( deltaPosSample.data, 0, 1, 0.0f );
                        clipStartDeltaLocalPos = new Vector3( deltaPosSample.data[0], deltaPosSample.data[1], deltaPosSample.data[2] );
                    }
                }
            }
        }
    }

    public override void AddToContextStack( AnimationContext context ){
        context.Add( this, defaultSpeed );
    }
    public override void RemoveFromContextStack( AnimationContext context ){
        context.Pop();
    }
    
    public override void Read( AnimationContext context, bool playEvents=true ){
        var playback = context.Next();
        playback.Advance( Time.fixedDeltaTime*context.speed.value*playback.speed.value*animation.baseFramerate, nextState == this );
        
        foreach( var sample in samples ){
            var channel = (AnimationChannel)sample.source;
            if( sample != deltaPosSample ){
                channel.Sample( sample.data, playback.frameA, playback.frameB, playback.ratio );
            }else{
                var deltaOffsetA = playback.loopsA*deltaOffset;
                var deltaOffsetB = playback.loopsB*deltaOffset;
                channel.Sample( sample.data, playback.frameA, playback.frameB, playback.ratio, deltaOffsetA, deltaOffsetB );
                var newDeltaLocalPos = new Vector3( sample.data[0], sample.data[1], sample.data[2] );

                deltaPosition = newDeltaLocalPos-playback.lastDeltaLocalPos;
                playback.lastDeltaLocalPos = newDeltaLocalPos;
            }
        }
        if( playEvents ) StepEvents( playback.normalizedTime, context.character );
    }
}

public class AnimationMixer: AnimationNode{

    public class SampleGroup{
        public List<BindSample> samples;
        public List<Weight> weights;
    }

    public readonly AnimationNode[] states;
    private readonly Dictionary<int,SampleGroup> sampleGroups;
    protected readonly Weight[] weights;
    
    private Vector3 longestStateStartDeltaLocalPos;
    private int longestStateMaxFrames;
    private bool matchSpeeds;

    public override Vector3 startDeltaLocalPos { get{ return longestStateStartDeltaLocalPos; } }
    public override int maxFrames { get{ return longestStateMaxFrames; } }

    public bool HasWeight( Weight weight ){
        return System.Array.Exists( weights, elem=>elem==weight);
    }

    public override void Reset(){
        base.Reset();
        foreach( var state in states ) state.Reset();
    }
    // play events for self and for layer with biggest weight

    public AnimationMixer( string _name, AnimationNode[] _states, Weight[] _weights, bool _matchSpeeds=false ):base(_name){
        states = _states;
        weights = _weights;
        matchSpeeds = _matchSpeeds;

        if( states == null ) throw new System.Exception("samplers is null");
        if( weights == null ) throw new System.Exception("weights are null");
        if( states.Length < 2 ) throw new System.Exception("samplers must have at least 2 samplers");
        foreach( var sampler in states ){
            if( sampler == null ) throw new System.Exception("AnimationSampler[] contains a null entry");
            if( sampler.maxFrames > longestStateMaxFrames ){
                longestStateMaxFrames = sampler.maxFrames;
                longestStateStartDeltaLocalPos = sampler.startDeltaLocalPos;
            }
        }
        if( weights.Length != states.Length ) throw new System.Exception("Weight properties array must match length of samplers");

        _internalLayerAssigned = states[0]._internalLayerAssigned;
        
        int bindID = BindSample.bindCounter++;
        int capacity = states.Length;
        sampleGroups = new Dictionary<int,SampleGroup>( capacity );

        for( int i=0; i<capacity; i++ ){
            var sampler = states[i];
            foreach( var sample in sampler.samples ){
                SampleGroup sampleGroup;
                var channelHash = Tools.CombineHashes( sample.bindHash, (int)sample.channel );

                if( !sampleGroups.TryGetValue( channelHash, out sampleGroup ) ){
                    sampleGroup = new SampleGroup();
                    sampleGroup.samples = new List<BindSample>( capacity );
                    sampleGroup.weights = new List<Weight>( capacity );
                    sampleGroups[ channelHash ] = sampleGroup;
                }
                sampleGroup.samples.Add( sample );
                sampleGroup.weights.Add( weights[i] );
            }
        }
        foreach( var sampleGroup in sampleGroups.Values ){
            var first = sampleGroup.samples[0];
            samples.Add( new BindSample( first.bindHash, first.channel, sampleGroup.samples.ToArray(), first.data.Length ) );
        }
    }
    
    public override void AddToContextStack( AnimationContext context ){
        context.Add( this );
        foreach( var state in states ){
            state.AddToContextStack( context );
        }
    }
    public override void RemoveFromContextStack( AnimationContext context ){
        context.Pop();
        foreach( var state in states ){
            state.RemoveFromContextStack( context );
        }
    }

    public override void Read( AnimationContext context, bool playEvents=true ){
        var blendPlayback = context.Next();
        
        float sum = 0.0f;
        for( int i=0; i<weights.Length; i++ ){
            sum += Mathf.Abs( weights[i].value );
        }
        if( sum == 0.0f ) return;

        //calculate target average maxFrames
        float avgMaxFrames = 0.0f;
        for( int i=0; i<weights.Length; i++ ){
            var weight = weights[i].value/sum;
            weights[i].value = weight;

            avgMaxFrames += weight*states[i].maxFrames;
        }

        //calculate average deltaPosition
        float targetNormTime = context.Peek().normalizedTime+Mathf.Epsilon; //prevent divison by zero
        blendPlayback.SetNormalizedTime( targetNormTime );
        targetNormTime = Mathf.Abs( targetNormTime );

        float biggestWeight = 0;
        int biggestStateIndex = 0;
        for( int i=0; i<weights.Length; i++ ){
            var weight = weights[i].value;
            if( weight > biggestWeight ){
                biggestWeight = weight;
                biggestStateIndex = i;
            }
        }
        
        deltaPosition = Vector3.zero;
        for( int i=0; i<states.Length; i++ ){
            var state = states[i];
            var weight = weights[i].value;

            var nextPlaybackState = context.Peek();
            if( matchSpeeds ){
                float absNormTime = Mathf.Abs( nextPlaybackState.normalizedTime );
                float speedMult = (float)state.maxFrames/avgMaxFrames;
                // if( absNormTime >= 0.01f && targetNormTime >= 0.01f ){
                //     speedMult *= targetNormTime/absNormTime;
                // }
                nextPlaybackState.speed.Add( name, speedMult );
            }
            state.Read( context, i==biggestStateIndex );
            deltaPosition += state.deltaPosition*weight;
        }
        
        int sampleIndex = 0;
        foreach( var sampleGroup in sampleGroups.Values ){
            var sample = samples[ sampleIndex++ ];

            var firstSample = sampleGroup.samples[0];
            if( sample.channel == AnimationChannel.Channel.ROTATION ){
                //average quaternions using weights
                Quaternion firstRotation = new Quaternion( firstSample.data[0], firstSample.data[1], firstSample.data[2], firstSample.data[3] );
                Vector4 cumulative = Vector4.zero;

                for( int k=0; k<sampleGroup.samples.Count; k++ ){
                    float weight = sampleGroup.weights[k].value;
                    if( weight <= Mathf.Epsilon ) continue;
                    
                    var sourceSample = sampleGroup.samples[k];
                    Quaternion newRotation = new Quaternion( sourceSample.data[0], sourceSample.data[1], sourceSample.data[2], sourceSample.data[3] );
                    Tools.AverageQuaternion( ref cumulative, weight, newRotation, firstRotation );
                }
                //output final unnormalized result
                sample.data[0] = cumulative.x;
                sample.data[1] = cumulative.y;
                sample.data[2] = cumulative.z;
                sample.data[3] = cumulative.w;
            }else{
                //reset with the first samples
                float firstWeight = sampleGroup.weights[0].value;
                for( int j=0; j<sample.data.Length; j++ ){
                    sample.data[j] = firstSample.data[j]*firstWeight;
                }

                //add the rest of the samples
                for( int k=1; k<sampleGroup.samples.Count; k++ ){
                    var sourceSample = sampleGroup.samples[k];
                    float weight = sampleGroup.weights[k].value;
                    for( int j=0; j<sample.data.Length; j++ ){
                        sample.data[j] += sourceSample.data[j]*weight;
                    }
                }
            }
        }
    }
}

public class AnimationTransition: AnimationMixer{

    public readonly float transitionDuration;
    private readonly BindStateCallback onFinishTransitionBind;
    private readonly AnimationNode startState;
    private readonly AnimationNode endState;
    private readonly int depth = 1;
    public float transitionTime = 0.0f;

    public AnimationTransition( AnimationNode _startState,
                                AnimationNode _endState,
                                float _transitionDuration,
                                BindStateCallback _onFinishTransitionBind ):base(
                                    "transition",
                                    new AnimationNode[]{ _startState, _endState },
                                    new Weight[]{ new Weight(), new Weight() },
                                    false
                                ){
        transitionDuration = _transitionDuration;
        onFinishTransitionBind = _onFinishTransitionBind;
        startState = _startState;
        endState = _endState;

        var transitionA = startState as AnimationTransition;   
        var transitionB = endState as AnimationTransition;
        int? highestDepth = null;
        if( transitionA != null ){
            highestDepth = transitionA.depth;
        }
        if( transitionB != null ){
            if( highestDepth.HasValue ){
                highestDepth = Mathf.Max( highestDepth.Value, transitionB.depth );
            }else{
                highestDepth = transitionB.depth;
            }
        }
        if( highestDepth.HasValue ){
            depth = highestDepth.Value+1;
        }
    }
        
    public override void Read( AnimationContext context, bool playEvents=true ){
        transitionTime += Time.fixedDeltaTime;
        float ratio;
        if( transitionDuration == 0 ){
            ratio = 1.0f;
        }else{
            ratio = transitionTime/transitionDuration;
        }

        //when finished transition switch to cheaper animationState (1 less AnimationBlender)
        //set a limit to depth of AnimationBlender for performance
        if( ( ratio >= 1.0f || depth > 3 ) && context.activeState == this ){
            context.Pop(); //remove self
            startState.RemoveFromContextStack( context );
            onFinishTransitionBind( endState );
            endState.Read( context );
        }else{
            weights[1].value = Tools.EaseInOutQuad( ratio );
            weights[0].value = 1.0f-weights[1].value;
            
            base.Read( context );
        }
    }
}


}