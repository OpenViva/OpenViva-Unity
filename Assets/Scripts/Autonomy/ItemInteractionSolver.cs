using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace viva{

public class InteractionInfo{
	public Character user;
	public bool rightHandHasBaseItem;
	public Item baseItem;	//will always be null at the start!
	public Item otherItem;
}

public delegate Task InteractionTaskReturnFunc( InteractionInfo info );

public class Interaction{

	public readonly AttributeRequest baseRequest;
	public readonly AttributeRequest? otherRequest;
	public readonly bool pickupOtherRequest;
	public readonly string bodySetName;
	public readonly string bodySetAnimation;
	public readonly float loopsToComplete;
	public readonly AttributeRequest targetRequest;
	public readonly InteractionTaskReturnFunc onTask;
	private readonly InteractionTaskReturnFunc onComplete;

	public Interaction( AttributeRequest _baseItemRequest, AttributeRequest? _otherItemRequest, bool _pickupOtherRequest, InteractionTaskReturnFunc _onTask, AttributeRequest _targetRequest, InteractionTaskReturnFunc _onComplete ){
		baseRequest = _baseItemRequest;
		otherRequest = _otherItemRequest;
		pickupOtherRequest = _pickupOtherRequest;
		onTask = _onTask;
		targetRequest = _targetRequest;
		onComplete = _onComplete;
	}

	public override string ToString(){
		return "("+baseRequest.ToString()+") + ("+(otherRequest.HasValue ? otherRequest.ToString() : "<blank>")+") = "+targetRequest;
	}

	public Task AttemptComplete( InteractionInfo info ){
		if( !info.baseItem || !info.baseItem.HasAttributes( baseRequest ) ) return null;
		if( otherRequest.HasValue ){
			if( !info.otherItem || !info.otherItem.HasAttributes( otherRequest.Value ) ) return null;
		}
		return onComplete?.Invoke( info );
	}
}

public class InteractionSolver{

    private class InteractionNode{
		public readonly Interaction interaction;
		public int range;
		public List<InteractionNode> prev = new List<InteractionNode>();
		public List<InteractionNode> next = new List<InteractionNode>();
		public InteractionNode parent;

		public InteractionNode( Interaction _interaction ){
			interaction = _interaction;
		}
	}

	private List<InteractionNode> interactionNodes = new List<InteractionNode>();


	public void _InternalReset(){
		interactionNodes.Clear();
	}

	public void Add( Interaction interaction ){
		if( interaction == null ){
			Debugger.LogError("Cannot add null interaction!");
			return;
		}
		//ensure interaction does not exist already
		foreach( var node in interactionNodes ){
			if( node.interaction.Equals( interaction ) ) return;
		}
		var newNode = new InteractionNode( interaction );
		foreach( var otherNode in interactionNodes ){
			if( Attribute.Matches( interaction.baseRequest.attributes, otherNode.interaction.targetRequest.attributes, CountMatch.EQUAL_OR_GREATER ) ||
			  ( interaction.otherRequest.HasValue && Attribute.Matches( interaction.otherRequest.Value.attributes, otherNode.interaction.targetRequest.attributes, CountMatch.EQUAL_OR_GREATER ) ) ){
				otherNode.next.Add( newNode );
				newNode.prev.Add( otherNode );
			}
		}
		interactionNodes.Add( newNode );
	}

	public Interaction FindInteraction( Item rightHandItem, Item leftHandItem, bool matchCounts ){
		foreach( var node in interactionNodes ){
			var interaction = node.interaction;
			if( rightHandItem.HasAttributes( interaction.baseRequest ) ){
				if( !interaction.otherRequest.HasValue ) return interaction;
				if( leftHandItem.HasAttributes( interaction.otherRequest.Value ) ){
					return interaction;
				}
			}
		}
		return null;
	}

	//if character sees or is holding
	private bool CharacterMatchesInteraction( Character character, Interaction interaction ){
		bool matches = true;
		//required
		matches &= character.biped.vision.SeesItemWithAttributes( interaction.baseRequest ) ||
				character.biped.rightHandGrabber.IsGrabbing( interaction.baseRequest ) ||
				character.biped.leftHandGrabber.IsGrabbing( interaction.baseRequest );
		//optional
		if( interaction.otherRequest.HasValue ){
			matches &= character.biped.vision.SeesItemWithAttributes( interaction.otherRequest.Value ) ||
						character.biped.rightHandGrabber.IsGrabbing( interaction.otherRequest.Value ) ||
						character.biped.leftHandGrabber.IsGrabbing( interaction.otherRequest.Value );
		}
		return matches;
	}

    public List<Interaction> FindInteractionPath( AttributeRequest targetAttributeRequest, Character character ){
        if( character == null ){
            Debugger.LogError("FindInteractionPath needs a character");
            return null;
        }

		foreach( var startingNode in interactionNodes ){
			//start at the bottom of the graph tree (the roots where none came before them)
			if( startingNode.prev.Count > 0 ) continue;
			startingNode.range = 0;
			List<Interaction> candidatePath = new List<Interaction>();

			//find starting interaction node
			List<InteractionNode> stack = new List<InteractionNode>();
			stack.Add( startingNode );
			
			List<InteractionNode> alreadyVisited = new List<InteractionNode>();
			alreadyVisited.Add( startingNode );

			int range = 1;
			InteractionNode finalNode = null;
			while( stack.Count > 0 ){
				var testNode = stack[0];

				if( Attribute.Matches( testNode.interaction.targetRequest.attributes, targetAttributeRequest.attributes, CountMatch.ANY ) ){
					finalNode = testNode;
					break;
				}
				if( finalNode != null ) break;

				stack.RemoveAt(0);

				foreach( var nextNode in testNode.next ){
					if( alreadyVisited.Contains( nextNode ) ) continue;
					alreadyVisited.Add( nextNode );

					nextNode.range = testNode.range+1;
					nextNode.parent = testNode;
					if( nextNode.range > range ){
						range = nextNode.range;
						stack.Add( nextNode );
					}else{
						stack.Insert( 0, nextNode );
					}
				}
			}
			if( finalNode == null ) continue;
			while( finalNode.parent != null ){
				candidatePath.Insert( 0, finalNode.interaction );
				finalNode = finalNode.parent;
			}
			candidatePath.Insert( 0, startingNode.interaction );
			var path = new List<Interaction>();
			path.AddRange( candidatePath );
			return path;
		}
		return null;
	}
}


}