using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Text.RegularExpressions;


namespace viva{


/// <summary>
/// Allows the character to play an animation through the Autonomy tree. Optionally enforces correct BodySet to ensure the character is in the correct BodySet before performing the animation.
/// </summary>
public class PlayAnimation : Task {

	private List<AnimationNode> transferPath = new List<AnimationNode>();
	public string bodySetName { get; private set; }
	public string animationGroup { get; private set; }
	public AnimationLayer animationLayer {get; private set;}
	public int animationLayerIndex {get; private set;}
	private AnimationNode enteredAnimationState;
	public GenericCallback onEnterAnimation;
	public GenericCallback onExitAnimation;
	private readonly bool waitForIdle;
	private bool waitingForIdle;
	public bool hasAnimationControl { get{ return enteredAnimationState!=null; } }
	public float loopsToComplete;
	public float? transitionTime = null;

	/// <summary>
	/// Tasks the character to play a BodySet animation. BodySet and/or Animation name must be specified.
	/// </summary>
	/// <param name="_autonomy">The Autonomy of the parent character. Exception will be thrown if it is null.</param>
	/// <param name="_bodySetName">The target BodySet to reach. If _bodySetName is null, it will play the _bodySetAnimation of the current bodySet.</param>
	/// <param name="_bodySetAnimation">The animation slot (not name!) to play in the BodySet. If _bodySetAnimation is null, it will default to the "idle" slot of the destination _bodySetName.</param>
	/// <param name="_animateTorso">Is this a torso animationLayer animation? Value does not matter for NPCs</param>
	/// <param name="_loopsToComplete">The number or fraction of loops to play before it automatically succeeds. A value of -1 will never succeed unless specified by code.</param>
	/// <param name="_waitForIdle">Should this animation wait for the character to be idle first? False will just play it immediately.</param>
	/// <example>
    /// The following makes the character tpose in place.
    /// <code>
	/// var playTpose = new PlayAnimation( character.autonomy, "stand", "tpose" );
    /// </code>
    /// </example>
	public PlayAnimation( Autonomy _autonomy, string _bodySetName, string _bodySetAnimation=null, bool _animateTorso=true, float _loopsToComplete=1, bool _waitForIdle=true ):base(_autonomy){
		
		if( _bodySetName == null && _bodySetAnimation == null ) throw new System.Exception("_bodySetName or _bodySetAnimation must be specified!");
		
		bodySetName = _bodySetName;
		animationGroup = _bodySetAnimation == null ? "idle" : _bodySetAnimation;
		SetAnimationLayerIndex( _animateTorso ? autonomy.self.mainAnimationLayerIndex : autonomy.self.altAnimationLayerIndex );
		name = "play animation "+_bodySetName+"/"+animationGroup;
		waitForIdle = _waitForIdle;
		loopsToComplete = _loopsToComplete;

		onAnimationChange += OnAnimationChange;
		onRegistered += UpdateAnimationPath;
		onFixedUpdate += OnFixedUpdate;

		onReset += CheckExitedControl;
		onInterrupted += CheckExitedControl;
		onUnregistered += CheckExitedControl;
		onUnregistered += delegate{
			if( passiveParent != null ) Reset();
		};
		onInterrupted += delegate{
			if( requirementParent != null ) Reset();
		};

		waitingForIdle = waitForIdle;
	}

	public void SetTargetBodySet( string _bodySetName ){
		if( bodySetName != _bodySetName ){
			bodySetName = _bodySetName;
			Reset();
		}
	}

	public void SetTargetAnimationGroup( string _animationGroup ){
		if( animationGroup != _animationGroup ){
			animationGroup = _animationGroup;
			Reset();
		}
	}

	public void SetAnimationLayerIndex( int _animationLayerIndex ){	
		if( _animationLayerIndex>self.animationLayers.Count ) throw new System.Exception("Invalid animation layer index is out of bounds. Use character.altAnimationLayerIndex or character.mainAnimationLayerIndex");
		animationLayerIndex = _animationLayerIndex;
		animationLayer = self.animationLayers[ animationLayerIndex ];
	}

	public bool ForceSkipToNextState(){
		var targetAnimation = GetTargetAnimation();
		if( targetAnimation == null || targetAnimation.nextState == null || targetAnimation.nextState == targetAnimation ){
			Debugger.LogWarning("Cannot SkipToNextState because the next state loops or is null");
			return false;
		}
		animationLayer.player._InternalPlay( _internalSource, targetAnimation.nextState );
		return true;
	}

	private void CheckExitedControl(){	
		if( enteredAnimationState != null ){
			enteredAnimationState = null;
			onExitAnimation?.Invoke();
		}
		waitingForIdle = waitForIdle;
		//if reset while registered, reuse current animation
		if( registered ) OnAnimationChange( animationLayerIndex );
	}

	public void MirrorTargetAnimation(){
		if( animationGroup == null ) return;

		if( animationGroup.EndsWith("right") ){
			animationGroup = Regex.Replace( animationGroup, "right$", "left" );
		}else if( animationGroup.EndsWith("left") ){
			animationGroup = Regex.Replace( animationGroup, "left$", "right" );
		}else{
			Debugger.LogError("Could not mirror animation name \""+animationGroup+"\". Must end with right or left");
		}
	}

	private void OnAnimationChange( int animationLayerIndex ){
		if( animationLayerIndex != this.animationLayerIndex ) return;
		
		if( enteredAnimationState != null && animationLayer.player.currentState != enteredAnimationState ){
			enteredAnimationState = null;
			if( loopsToComplete >= 0 && animationLayer.player.context.lastMainPlaybackState.normalizedTime >= loopsToComplete ){
				Succeed();
			}
			
			onExitAnimation?.Invoke();
		}

		if( succeeded ) return;
		UpdateAnimationPath();
	}

	private void UpdateAnimationPath(){
		if( waitingForIdle && self.autonomy.HasTag("idle") ) waitingForIdle = false;
		if( waitingForIdle ){
			var currentState = animationLayer.player.currentState;
			if( currentState.nextState == null || currentState.nextState == currentState ){
				//if animation is looping or a dead-end, quit after it plays once
				waitingForIdle = false;
			}else{
				//wait for currentState to play
				return;
			}
		}

		transferPath.Clear();
		if( animationLayer.player.currentState == GetTargetAnimation() ){
			EnteredAnimation();
		}else{
			BodySet targetBodySet = bodySetName == null ? animationLayer.currentBodySet : self.animationSet.GetBodySet( bodySetName );
			if( animationLayer.currentBodySet == null || targetBodySet == null || targetBodySet.isEmpty ){
				Fail("\""+name+"\": BodySet \""+bodySetName+"\" has no animations. Was this BodySet correctly specified?");
			}else{
				if( !self.animationSet.FindBodyStatePath( transferPath, animationLayer.currentBodySet, targetBodySet ) ){
					Fail("\""+name+"\": Could not find animation path from \""+animationLayer.currentBodySet.name+"\" to \""+targetBodySet.name+"\"");
				}
			}
		}
	}

	private void EnteredAnimation(){
		if( enteredAnimationState != null ) return;
		enteredAnimationState = animationLayer.player.currentState;
		onEnterAnimation?.Invoke();
	}

	private AnimationNode GetTargetAnimation(){
		AnimationNode targetAnimation;
		if( bodySetName == null ){
			targetAnimation = animationLayer.currentBodySet?[ animationGroup ];
			if( animationLayer.currentBodySet != null && targetAnimation == null ){
				Fail( "\""+animationLayer.currentBodySet.name+"\" has no animation named \""+animationGroup+"\"");
			}
		}else{
			var bodySet = self.animationSet.GetBodySet( bodySetName );
			if( bodySet == null ) return null;
			targetAnimation = bodySet[ animationGroup ];
			if( targetAnimation == null ){
				Fail( "\""+bodySet.name+"\" has no animation named \""+animationGroup+"\"");
			}
		}
		return targetAnimation;
	}

	private void OnFixedUpdate(){

		if( finished || waitingForIdle ) return;
		if( transferPath.Count > 0 ){
			animationLayer.player._InternalPlay( _internalSource, transferPath[0] );
		}else{
			var targetAnimation = GetTargetAnimation();
			if( targetAnimation == null ) return;
			if( animationLayer.player.currentState == targetAnimation ){
				if( loopsToComplete >= 0 && animationLayer.player.context.mainPlaybackState.normalizedTime >= loopsToComplete ){
					Succeed();
				}
			}else{
				animationLayer.player._InternalPlay( _internalSource, targetAnimation, transitionTime );
			}
		}
	}

	public float GetPlayedNormalizedTime(){
		if( !animationLayer.player ) return 0;
		if( animationLayer.player.context == null ) return 0;
		if( animationLayer.player.context.mainPlaybackState == null ) return 0;
		return animationLayer.player.context.mainPlaybackState.normalizedTime;
	}

	public bool IsLooping(){
		if( !animationLayer.player ) return false;
		if( animationLayer.player.currentState == null ) return false;
		return animationLayer.player.currentState.nextState == animationLayer.player.currentState;
	}
}

}