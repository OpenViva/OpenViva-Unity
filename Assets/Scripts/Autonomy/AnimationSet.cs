using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;



namespace viva{

/// <summary>
/// Represents a collection of AnimationStates that share a similar body pose.
/// Example: "walk forward", "standing idle", "standing waving hello" all belong to a BodySet named "standing" because they
/// are all being performed  while standing.
/// This is used for pathfinding animation transitions to animate going from one BodySet to another in a contextually coherent way.
/// Example: Going from a "sleep face down" animation to a "standing jump" requires the character to roll over and stand up before jumping.
public class BodySet{

    /// <summary> The name of the BodySet
    public readonly string name;
    private readonly Dictionary<string,AnimationNode> animationStates = new Dictionary<string,AnimationNode>();
    /// <summary> The dictionary for maintaining BodySet path connections. Use this if you have a new type of BodySet pose you wish to add.
    public readonly Dictionary<BodySet, AnimationNode> transitions = new Dictionary<BodySet, AnimationNode>();
    /// <returns> Get the number of AnimationStates in this BodySet
    public int Count { get{ return animationStates.Count; } }
    public bool isEmpty { get{ return Count==0; } }
    private Character character;
    public readonly int? animationLayerIndex;

    /// <summary> The name of the BodySet
	/// <returns> AnimationState: The AnimationState if found. Null otherwise.</returns>
    public AnimationNode this[ string key ]{
        get{
            if( animationStates.TryGetValue( key, out AnimationNode state ) ){
                return state;
            }
            return null;
        }
        private set{
            if( value == null || key == null ) return;
            if( animationStates.ContainsKey( key ) ){
                Debugger.LogWarning("AnimationState \""+key+"\" already exists in \""+name+"\" skipping...");
                return;
            }
            animationStates[ key ] = value;
        }
    }

    public AnimationNode Single( string key, string clipName, bool loop=false, float _defaultSpeed=1.0f ){
        if( animationStates.TryGetValue( key, out AnimationNode single ) ){
            Debugger.LogWarning("AnimationState \""+key+"\" already exists in \""+name+"\"");
            return single;
        }
        single = new AnimationSingle( viva.Animation.Load( clipName ), character, loop, _defaultSpeed, animationLayerIndex );
        animationStates[ key ] = single;
        return single;
    }

    public AnimationNode Mixer( string key, AnimationNode[] clips, Weight[] weights, bool matchSpeeds=false ){
        if( animationStates.TryGetValue( key, out AnimationNode mixer ) ){
            Debugger.LogWarning("AnimationState \""+key+"\" already exists in \""+name+"\"");
        }
        mixer = new AnimationMixer( key+" mixer", clips, weights, matchSpeeds );
        animationStates[ key ] = mixer;
        return mixer;
    }

    /// <summary> Checks if this BodySet contains the following AnimationState
	/// <returns> bool: True if it is contained. False otherwise.</returns>
	/// <param name="animationState">The AnimationState to check.</param>
    public bool Contains( AnimationNode animationState ){
        return animationStates.ContainsValue( animationState );
    }

    public void _InternalGetNodeNames( AnimationNode animationState, List<string> nodeNames ){
        foreach( var pair in animationStates ){
            if( pair.Value == animationState ) nodeNames.Add( pair.Key );
        }
    }

    public BodySet( string _name, Character _character, int? _animationLayerIndex ){
        name = _name;
        character = _character;
        animationLayerIndex = _animationLayerIndex;
    }
    public BodySet( BodySet copy ){
        name = copy.name;
        animationLayerIndex = copy.animationLayerIndex;

        foreach( var pair in copy.animationStates ){
            this[ pair.Key ] = pair.Value;
        }
    }
}

/// <summary> The object that holds all BodySet objects.
public class AnimationSet{

    private class BodyStateNode{
		public readonly BodySet state;
		public readonly int range;
		public BodyStateNode parent;
		public AnimationNode transferAnim;

		public BodyStateNode( BodySet _state, BodyStateNode _parent, int _range, AnimationNode _transferAnim ){
			state = _state;
			parent = _parent;
			transferAnim = _transferAnim;
			range = _range+1;
		}
	}

    private Dictionary<int,BodySet> sets = new Dictionary<int, BodySet>();
    private Character character;
    
    public AnimationSet( Character _character ){
        character = _character;
    }

    // public AnimationSet( AnimationSet copy ){

    //     if( copy == null ) throw new System.Exception("Cannot copy from null AnimationSet");

    //     foreach( var pair in copy.sets ){
    //         var copySet = pair.Value;
    //         var set = new BodySet( copySet );
    //         sets[ pair.Key ] = set;
    //     }
    //     //build transitions last to use the AnimationSet's unique BodySet objects
    //     foreach( var pair in sets ){
    //         var copySet = copy.sets[ pair.Key ];
    //         foreach( var copyTransition in copySet.transitions ){
    //             if( sets.TryGetValue( copyTransition.Key.name.GetHashCode(), out BodySet key ) ){
    //                 pair.Value.transitions[ key ] = copyTransition.Value;
    //             }else{
    //                 throw new System.Exception("sourceSets missing aniamtion for transition \""+copyTransition.Key.name+"\"");
    //             }
    //         }
    //     }
    // }

    public void _InternalReset(){
        sets.Clear();
    }

	/// <summary>
	/// Searches all BodySets for the animationState and returns the parent BodySet.
	/// </summary>
	/// <param name="animationState">The AnimationState to search.</param>
	/// <returns>BodySet: the parent BodySet if found. Null otherwise.</param>
    public BodySet FindBodySet( AnimationNode animationState ){
        foreach( var set in sets.Values ){
            if( set.Contains( animationState ) ){
                return set;
            }
        }
        return null;
    }

    // public BodySet this[ int index ]{
    //     get{
    //         if( sets.TryGetValue( index, out BodySet value ) ){
    //             return value;
    //         }else{
    //             return null;
    //         }
    //     }
    // }
    
	/// <summary>
	/// Returns the BodySet associated with the name given. If it does not exist, it will create a BodySet with that name.
	/// </summary>
	/// <param name="name">The name of the BodySet to search for or create.</param>
	/// <returns>BodySet: the BodySet if found or a new one if not found.</param>
    /// <example>
    /// The following creates 2 connected BodySets and allows them to transition to each other.
    /// <code>
    /// var layingRightSide = character.animationSet["laying right"];
    /// var sleepingSideRight = new AnimationStateClip( //...some animation
    /// layingRightSide["sleeping"] = sleepingSideRight;
    /// 
    /// var layingLeftSide = character.animationSet["laying left"];
    /// var sleepingSideLeft = new AnimationStateClip( //...some animation
    /// layingLeftSide["sleeping"] = sleepingSideLeft;
    /// 
    /// //create transitions from sleeping left to sleeping right, and vice versa
    /// layingRightSide.transitions[ layingLeftSide ] = new AnimationStateClip( //some animation transitioning from right to left
    /// layingLeftSide.transitions[ layingRightSide ] = new AnimationStateClip( //some animation transitioning from left to right
    /// //These 2 BodySets are now connected by these transitions and PlayAnimation() can now pathfind from and to each other!
    /// </code>
    /// </example>


    public BodySet GetBodySet( string bodySetName, int? _animationLayerIndex=null ){
        var hash = bodySetName.GetHashCode();
        if( sets.TryGetValue( hash, out BodySet value ) ){
            return value;
        }
        value = new BodySet( bodySetName, character, _animationLayerIndex );
        sets[ hash ] = value;
        return value;
    }

    public bool FindBodyStatePath( List<AnimationNode> path, BodySet startSet, BodySet endSet ){
		path.Clear();
        if( startSet == null || endSet == null ){
            Debugger.LogError("FindBodyStatePath inputs contain null BodySets");
            return false;
        }
		if( startSet == endSet ){
			return true;
		}
		List<BodyStateNode> stack = new List<BodyStateNode>();
		stack.Add( new BodyStateNode( startSet, null, 0, null ) );
		
        List<BodySet> visitedBodyStateNodes = new List<BodySet>();
        visitedBodyStateNodes.Add( startSet );

		int range = 1;
		BodyStateNode finalNode = null;
		while( stack.Count > 0 ){
			var node = stack[0];

			if( node.state == endSet ){
				finalNode = node;
				break;
			}
			stack.RemoveAt(0);
			foreach( KeyValuePair<BodySet,AnimationNode> pair in node.state.transitions ){
				var newState = pair.Key;
				if( visitedBodyStateNodes.Contains( newState ) ){
					continue;
				}
                visitedBodyStateNodes.Add( newState );
				var newNode = new BodyStateNode( newState, node, node.range, pair.Value );
				if( newNode.range > range ){
					range = newNode.range;
					stack.Add( newNode );
				}else{
					stack.Insert( 0, newNode );
				}
			}
		}
		if( finalNode == null ) return false;
		while( finalNode.parent != null ){
			path.Insert( 0, finalNode.transferAnim );
			finalNode = finalNode.parent;
		}
		foreach( var a in path ){
			// Debug.Log("then "+a.name);
		}
		return true;
	}
}

}