using UnityEngine;
using System.Collections;
using System.Collections.Generic;


namespace viva{

public delegate void BindStateCallback( AnimationNode state );
public delegate void IntReturnFunc( int layer );

public class AnimationPlayer: MonoBehaviour{


    public static AnimationPlayer Create( GameObject target, int layerIndex ){
        var animationPlayer = target.AddComponent<AnimationPlayer>();
        animationPlayer.animationLayerIndex = layerIndex;

        return animationPlayer;
    }

    public SkinnedMeshRenderer smr {get; private set;} = null;
    public AnimationNode lastState {get; private set;} = null;
    public AnimationNode currentState {get; private set;} = null;
    private AnimationNode activeState = null;
    public object[] activeBindings {get; private set;} = null;
    public GenericCallback onAnimate;
    public GenericCallback onModifyAnimation;
    public IntReturnFunc onAnimationChange;
    public AnimationContext context {get; private set;} = null;
    private AnimationLayer animationLayer = null;
    public bool isTransitioning { get; private set; }
    public int animationLayerIndex { get; private set; }
    private int targetLoopsB = -1;
    private bool offscreen;

    
    public void BindAnimationLayer( AnimationLayer _animationLayer, SkinnedMeshRenderer _smr ){
        animationLayer = _animationLayer;
        smr = _smr;

        if( animationLayer == null ) throw new System.Exception("Cannot bind null AnimationLayer");
        if( smr == null ) throw new System.Exception("Cannot bind null SkinnedMeshRenderer");

        context = new AnimationContext( this, animationLayer.character );
        RefreshAnimationLayer();
    }

    public void RefreshAnimationLayer(){
        if( animationLayer == null ) return;

        if( activeState != null ) SetActiveState( activeState );
    }

    private void SetActiveState( AnimationNode state ){
        isTransitioning = false;
        activeState = state;
        context.activeState = activeState;
        
        activeBindings = new object[ state.samples.Count ];
        int binded = 0;
        foreach( var sample in state.samples ){
            if( sample.channel == AnimationChannel.Channel.BLENDSHAPE ){
                if( animationLayer.blendShapeBindings.TryGetValue( sample.bindHash, out int blendShapeIndex ) ){
                    activeBindings[ binded ] = blendShapeIndex;
                }
            }else{
                if( animationLayer.transformBindings.TryGetValue( sample.bindHash, out Transform bone ) ){
                    activeBindings[ binded ] = bone;
                }
            }
            binded++;
        }
    }

    public void Play( VivaScript source, string bodySetName, string animationKey, float? transitionTime=null ){

        var bodySet = animationLayer.character.animationSet.GetBodySet( bodySetName );
        if( bodySet == null ){
            Debugger.LogError("Cannot Play \""+bodySet+"\" - \""+animationKey+"\" BodySet does not exist");
            return;
        }
        var animation = bodySet[ animationKey ];
        if( animation == null ){
            Debugger.LogError("Cannot Play \""+bodySet+"\" - \""+animationKey+"\" Animation does not exist in BodySet");
            return;
        }
        _InternalPlay( animation, transitionTime );
        context.source = source;
    }

    public void _InternalPlay( VivaScript source, AnimationNode newState, float? transitionTime=null ){
        _InternalPlay( newState, transitionTime );
        context.source = source;
    }
    
    public void _InternalPlay( AnimationNode newState, float? transitionTime=null ){
        if( newState == null ){
            Debugger.LogError("Cannot play null AnimationState");
            return;
        }
        if( newState == currentState ){
            return;
        }
        if( newState._internalLayerAssigned == null ){
            Debugger.LogError("Cannot play \""+newState.name+"\" because it is missing assigned animation layer");
            return;
        }
        if( newState._internalLayerAssigned != animationLayer ){
            Debugger.LogError("Cannot play \""+newState.name+"\" because it is from a different animation layer");
            return;
        }
        lastState = currentState;
        currentState = newState;
        currentState.Reset();

        if( activeState == null ){
            context.Reset();
            SetActiveState( newState );
            newState.AddToContextStack( context );
        }else{
            var transitionState = new AnimationTransition( activeState, newState, transitionTime.HasValue ? transitionTime.Value : newState.defaultTransitionTime, SetActiveState );
            SetActiveState( transitionState );
            context.Insert( transitionState );
            newState.AddToContextStack( context );
            isTransitioning = true;
        }
        targetLoopsB = context.mainPlaybackState.loopsB;
        
        enabled = true;
        context.speed.Remove( name );

        onAnimationChange?.Invoke( animationLayerIndex );
    }

    public float GetTransitionNormTime(){
        if( !isTransitioning ) return 1;

        var transitionState = activeState as AnimationTransition;
        return transitionState.transitionTime/transitionState.transitionDuration;
    }

    public void Stop(){
        currentState = null;
        activeState = null;
        lastState = null;
        enabled = false;
    }

    public void Pause(){
        context.speed.Add( name, 0 );
    }

    public void FixedUpdate(){
        Animate();
    }

    public float SampleCurve( int curveNameHashCode, float defaultValue ){
        if( currentState == null ) return defaultValue;

        float lastVal = defaultValue;
        if( isTransitioning && lastState != null ){
            if( lastState.curves.TryGetValue( curveNameHashCode, out Curve lastCurve ) ){
                lastVal = lastCurve.Sample(1);  // sample endpoint
            }
        }
        float currVal = defaultValue;
        if( currentState.curves.TryGetValue( curveNameHashCode, out Curve currCurve ) ){
            currVal = currCurve.Sample( context.mainPlaybackState.normalizedTime%1f );
        }
        return Mathf.LerpUnclamped( lastVal, currVal, GetTransitionNormTime() );
    }

    public void Animate(){
        if( activeBindings == null || activeState == null || smr == null ) return;
        
        if( context.speed.value != 0 ){
            context.Restart();
            activeState.Read( context );
        }
        //apply read results    
        int i=0;
        foreach( var sample in activeState.samples ){
            var binding = activeBindings[i++];
            if( binding == null ) continue;

            switch( sample.channel ){
            case AnimationChannel.Channel.POSITION:
                ( binding as Transform ).localPosition = new Vector3(
                    sample.data[0],
                    sample.data[1],
                    sample.data[2]
                );
                break;
            case AnimationChannel.Channel.ROTATION:
                ( binding as Transform ).localRotation = new Quaternion(
                    sample.data[0],
                    sample.data[1],
                    sample.data[2],
                    sample.data[3]
                );
                break;
            case AnimationChannel.Channel.SCALE:
                // ( boneBinding as Transform ).localScale = new Vector3(
                //     sample.data[0],
                //     sample.data[1],
                //     sample.data[2]
                // );
                break;
            case AnimationChannel.Channel.BLENDSHAPE:
                smr.SetBlendShapeWeight( (int)binding, sample.data[0] );
                break;
            }
        }
        onModifyAnimation?.Invoke();
        onAnimate?.Invoke();
        if( context.mainPlaybackState != null ){
            if( context.mainPlaybackState.loopsB > targetLoopsB ){
                if( currentState.nextState != currentState && currentState.nextState != null ){
                    _InternalPlay( context.source, currentState.nextState );
                }
            }
            targetLoopsB = context.mainPlaybackState.loopsB;
        }else{
            targetLoopsB = -1;
        }
    }
}

}