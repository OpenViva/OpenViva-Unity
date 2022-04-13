using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace viva{


public partial class Loli : Character{

	public delegate void BodyStateCallback( BodyState oldBodyState, BodyState newBodyState );

	public BodyState bodyState { get; private set; } = BodyState.STAND;
	private BodyState onAnimationEndBodyState = BodyState.NONE;

	public BodyStateCallback OnBodyStateChanged;

	
	private class BodyStateNode{
		public readonly BodyState state;
		public readonly int range;
		public BodyStateNode parent;
		public Animation transferAnim;

		public BodyStateNode( BodyState _state, BodyStateNode _parent, int _range, Animation _transferAnim = Loli.Animation.NONE ){
			state = _state;
			parent = _parent;
			transferAnim = _transferAnim;
			range = _range+1;
		}
	}

	public static bool FindBodyStatePath( Loli loli, List<Loli.Animation> path, BodyState startBodyState, BodyState endBodyState ){
		// Debug.Log("**** "+startBodyState+" to "+endBodyState);
		path.Clear();
		if( startBodyState == endBodyState || startBodyState <= BodyState.OFFBALANCE ){
			return true;
		}
		List<BodyStateNode> stack = new List<BodyStateNode>();
		stack.Add( new BodyStateNode( startBodyState, null, 0 ) );
		
		bool[] visitedBodyStateNodes = new bool[ System.Enum.GetValues(typeof(BodyState)).Length ];
		visitedBodyStateNodes[ (int)startBodyState ] = true;

		int range = 1;
		BodyStateNode finalNode = null;
		while( stack.Count > 0 ){
			var node = stack[0];

			if( node.state == endBodyState ){
				finalNode = node;
				break;
			}
			stack.RemoveAt(0);
			foreach( KeyValuePair<BodyState,Loli.Animation> pair in loli.bodyStateAnimationSets[ (int)node.state ].bodyStateConnections ){
				var newState = pair.Key;
				if( visitedBodyStateNodes[ (int)newState] ){
					continue;
				}
				visitedBodyStateNodes[ (int)newState ] = true;
				var newNode = new BodyStateNode( newState, node, node.range, pair.Value );
				if( newNode.range > range ){
					range = newNode.range;
					stack.Add( newNode );
				}else{
					stack.Insert( 0, newNode );
				}
			}
		}
		if( finalNode == null ){
			Debug.Log("[BodyStates] No path found from "+startBodyState+" to "+endBodyState);
			return false;
		}
		while( finalNode.parent != null ){
			path.Insert( 0, finalNode.transferAnim );
			finalNode = finalNode.parent;
		}
		// foreach( var a in path ){
		// 	Debug.Log("then "+a);
		// }
		return true;
	}

	private void InitializeBodyStateFunctions(){
		OnBodyStateChanged += delegate( BodyState oldBodyState, BodyState newBodyState ){
			if( newBodyState == BodyState.CRAWL_TIRED ){
				foreach( var collider in rightHandMuscle.colliders ){
					collider.material = GameDirector.instance.slipperyPhysicsMaterial;
				}
			}else if( oldBodyState == BodyState.CRAWL_TIRED ){
				foreach( var collider in rightHandMuscle.colliders ){
					collider.material = GameDirector.instance.stickyPhysicsMaterial;
				}
			}
		};
	}

	private bool AnimationAllowsBalanceCheck( Animation animation ){
		AnimationInfo info = animationInfos[ animation ];
		if( (info.flags&(int)AnimationInfo.Flag.DISABLE_RAGDOLL_CHECK) != 0  ){
			return false;
		}
		bool conditionAllows = bodyStateAnimationSets[ (int)info.conditionBodyState ].checkBalance;
		bool newAllows = bodyStateAnimationSets[ (int)info.newBodyState ].checkBalance;
		return conditionAllows && newAllows;
	}

	public void OverrideBodyState( BodyState newBodyState ){
		if( bodyState != newBodyState ){
			var oldBodyState = bodyState;
			bodyState = newBodyState;
			OnBodyStateChanged?.Invoke( oldBodyState, newBodyState );
			onAnimationEndBodyState = BodyState.NONE;
		}
	}

	public bool IsAnimationChangingBodyState(){
		AnimationInfo currAnimInfo = animationInfos[ m_currentAnim ];
		return currAnimInfo.conditionBodyState!=currAnimInfo.newBodyState;
	}

	public Animation GetAnimationFromSet( AnimationSet animationSet ){
		return bodyStateAnimationSets[ (int)bodyState ].GetRandomAnimationSet( animationSet );
	}
	
	public float GetBodyStatePropertyValue( PropertyValue value ){
		var animationSet = bodyStateAnimationSets[ (int)bodyState ];
		if( !animationSet.propertyValues.TryGetValue( value, out float propertyValue ) ){
			propertyValue = 0.0f;
		}
		return propertyValue;
	}

	public Animation GetLastReturnableIdleAnimation(){
		if( IsHappy() ){
			return bodyStateAnimationSets[ (int)bodyState ].GetRandomAnimationSet( AnimationSet.IDLE_HAPPY );
		}else{
			return bodyStateAnimationSets[ (int)bodyState ].GetRandomAnimationSet( AnimationSet.IDLE_ANGRY );
		}
	}
}

}