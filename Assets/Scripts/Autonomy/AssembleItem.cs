using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace viva{

public class AssembleItem: Task{

	private float? searchTimeStart;
	private int timesSearched = 0;
	private Pickup pickupBaseItem;
	private Task otherItemTask;
	private List<Interaction> path;
	private Interaction currentInteraction;
	private Interaction nextInteraction;
	private InteractionInfo currentInteractionInfo;
	private PlayAnimation returnToIdleAnim;
	private Task mainAssemble;
	private AttributeRequest targetAttributeRequest;
	private Task finalAssembleTask;


	public AssembleItem( Autonomy _autonomy, AttributeRequest _targetAttributeRequest ):base(_autonomy){
        name = "assemble "+_targetAttributeRequest;
		targetAttributeRequest = _targetAttributeRequest;

        path = self.itemInteractions.FindInteractionPath( _targetAttributeRequest, self );
		if( path == null ){	//use item name as item attribute
			Fail("Could not find an interaction path to assemble \""+_targetAttributeRequest+"\"");
			return;
		}
;
		ContinueInteractionPath();
		SetupForInternalLastAssembleItem();
	}

	public AssembleItem( Autonomy _autonomy, List<Interaction> _path, AttributeRequest _targetAttributeRequest ):base(_autonomy){
		path = _path;
        if( path == null || path.Count == 0 ){
			Fail("Interaction path for AssembleItem is null or empty");
			return;
		}
		name = "assemble given path length: "+path.Count;
		targetAttributeRequest = _targetAttributeRequest;
		ContinueInteractionPath();
		SetupForInternalLastAssembleItem();
	}

	private void SetupForInternalLastAssembleItem(){
		onAutonomyEnter += delegate{
			self.onGesture._InternalAddListener( ListenForStopGesture );
		};
		onAutonomyExit += delegate{
			self.onGesture._InternalRemoveListener( ListenForStopGesture );
		};
	}

	private void ListenForStopGesture( string gesture, Character caller ){
		if( gesture == "stop" ){
			Fail("Told to stop");
			RemoveAllPassivesAndRequirements();
		}
	}

	private int? FindHighestAvailablePathInteractionIndex(){
		for( int i=path.Count; i-->0; ){
			var interaction = path[i];
			var candidateBaseItems = self.biped.vision.FindItemsByAttributes( interaction.baseRequest );
			if( candidateBaseItems.Count == 0 ) continue;
			if( interaction.otherRequest.HasValue ){
				var candidateOtherItems = self.biped.vision.FindItemsByAttributes( interaction.otherRequest.Value );
				bool matchedBoth = false;
				foreach( var otherItem in candidateOtherItems ){
					var unique = true;
					foreach( var baseItem in candidateBaseItems ){
						if( otherItem == baseItem ){
							unique = false;
							break;
						}
					}
					if( unique ){
						matchedBoth = true;
					}
				}
				if( !matchedBoth ) continue;
			}
			return i;
		}
		return null;
	}

	private void ContinueInteractionPath( int? interactionIndex=null){
		if( !interactionIndex.HasValue ) interactionIndex = FindHighestAvailablePathInteractionIndex();
		if( !interactionIndex.HasValue ){
			interactionIndex = 0;	//start all over
		}
		currentInteraction = path[ interactionIndex.Value ];
		nextInteraction = path.Count>interactionIndex.Value+1 ? path[ interactionIndex.Value+1 ] : null;
		
		Debug.LogError("Now on..."+currentInteraction.ToString());
		currentInteractionInfo = new InteractionInfo();
		currentInteractionInfo.user = self;
		searchTimeStart = Time.time+0.5f;

		if( returnToIdleAnim == null ){
			returnToIdleAnim = new PlayAnimation( self.autonomy, null, "idle", true, 0 );
			returnToIdleAnim.onSuccess += CheckFinishInteraction;
		}

		//First step is to get items in the interaction, then calculate how many times to do it to move to the next (TODO: Alternatively how to clear the items if too much of an attribute)
		GeneratePickupTasks( Random.value>0.5f, currentInteractionInfo );
		AddRequirement( pickupBaseItem );

		pickupBaseItem.onSuccess += BuildMainAssembleTask;
	}

	private void BuildMainAssembleTask(){
		mainAssemble = currentInteraction.onTask( currentInteractionInfo );
		if( mainAssemble == null ){
			Fail("Could not create AssembleItem Task from interaction");
			return;
		}
		mainAssemble.onRegistered += delegate{
			//must be holding both items
			if( currentInteractionInfo.baseItem && currentInteractionInfo.baseItem.IsBeingGrabbedByCharacter( self ) &&
				currentInteractionInfo.otherItem && currentInteractionInfo.otherItem.IsBeingGrabbedByCharacter( self ) ){
				var interactionIndex = FindHighestAvailablePathInteractionIndex();
				if( !interactionIndex.HasValue ){
					mainAssemble.Fail("Could not find available path interaction index");
					return;
				}
				//must be in the same interaction
				if( currentInteraction != path[ interactionIndex.Value ] ){
					mainAssemble.Fail("At a different interaction index");
					return;
				}

				if( !currentInteractionInfo.baseItem.HasAttributes( currentInteraction.baseRequest ) ){
					mainAssemble.Fail("Base item is missing attributes");
					return;
				}
				if( !currentInteraction.otherRequest.HasValue && currentInteractionInfo.otherItem.HasAttributes( currentInteraction.otherRequest.Value ) ){
					mainAssemble.Fail("Other item is missing attributes");
					return;
				}
				self.biped.lookTarget.SetTargetRigidBody( currentInteractionInfo.baseItem.rigidBody );
			}else{
				mainAssemble.Fail("Not grabbing assemble items");
			}
		};
		if( currentInteraction.otherRequest.HasValue ) mainAssemble.name = "assemble action "+currentInteraction.baseRequest;
		else mainAssemble.name = "assemble combine "+currentInteraction.baseRequest+" + "+currentInteraction.otherRequest;

		RemoveRequirement( pickupBaseItem );
		AddRequirement( mainAssemble );
		AddRequirement( returnToIdleAnim );
	}

	private int GetHighestCount( Interaction interaction, string attributeName ){
		int count = 0;
		var attribA = interaction.baseRequest.FindAttribute( attributeName );
		if( attribA != null ){
			count = attribA.count;
		}
		if( interaction.otherRequest.HasValue ){
			var attribB = interaction.otherRequest.Value.FindAttribute( attributeName );
			if( attribB != null ){
				count = Mathf.Max( attribB.count, count );
			}
		}
		return count;
	}

	private void GeneratePickupTasks( bool preferRightForItemA, InteractionInfo info ){
		if( currentInteraction.otherRequest.HasValue && currentInteraction.pickupOtherRequest ){
			bool preferRightForItemB = !preferRightForItemA;
			if( self.biped.rightHandGrabber.IsGrabbing( currentInteraction.baseRequest ) ){
				preferRightForItemA = true;
				preferRightForItemB = false;
			}else if( self.biped.leftHandGrabber.IsGrabbing( currentInteraction.baseRequest ) ){
				preferRightForItemA = false;
				preferRightForItemB = true;
			}else if( self.biped.rightHandGrabber.IsGrabbing( currentInteraction.otherRequest.Value ) ){
				preferRightForItemB = true;
				preferRightForItemA = false;
			}else if( self.biped.leftHandGrabber.IsGrabbing( currentInteraction.otherRequest.Value ) ){
				preferRightForItemB = false;
				preferRightForItemA = true;
			}

			pickupBaseItem = new Pickup( self.autonomy, currentInteraction.baseRequest, HandleNeedsToDropGrabbable, preferRightForItemA );
			otherItemTask = new Pickup( self.autonomy, currentInteraction.otherRequest.Value, HandleNeedsToDropGrabbable, preferRightForItemB );
			
			PreparePickupForSpeechBubble( otherItemTask );
			otherItemTask.onSuccess += delegate{
				info.otherItem = ((Pickup)otherItemTask).grabbable?.parentItem;
			};

			pickupBaseItem.AddRequirement( otherItemTask );
		}else{
			if( self.biped.rightHandGrabber.IsGrabbing( currentInteraction.baseRequest ) ){
				preferRightForItemA = true;
			}else if( self.biped.leftHandGrabber.IsGrabbing( currentInteraction.baseRequest ) ){
				preferRightForItemA = false;
			}
			pickupBaseItem = new Pickup( self.autonomy, currentInteraction.baseRequest, HandleNeedsToDropGrabbable, preferRightForItemA );

			if( currentInteraction.otherRequest.HasValue ){
				otherItemTask = new Task( self.autonomy );
				otherItemTask.onRegistered += delegate{
					self.biped.vision.onItemSeen.AddListener( _internalSource, OnOtherItemSeen );
				};
				otherItemTask.onUnregistered += delegate{
					self.biped.vision.onItemSeen.RemoveListener( _internalSource, OnOtherItemSeen );
				};
				PreparePickupForSpeechBubble( otherItemTask );
				pickupBaseItem.AddRequirement( otherItemTask );
			}
		}
		PreparePickupForSpeechBubble( pickupBaseItem );
		pickupBaseItem.onSuccess += delegate{
			info.rightHandHasBaseItem = pickupBaseItem.grabber==self.biped.rightHandGrabber;
			info.baseItem = pickupBaseItem.grabbable?.parentItem;
		};
	}

	private void OnOtherItemSeen( Item item ){
		if( currentInteractionInfo.otherItem ) return;
		if( item.HasAttributes( currentInteraction.otherRequest.Value ) ){
			currentInteractionInfo.otherItem = item;
			otherItemTask.Succeed();
		}
	}

	private void ClearCurrentInteractionVariables(){
		if( pickupBaseItem.inAutonomy ) RemoveRequirement( pickupBaseItem );
		RemoveRequirement( returnToIdleAnim );
		RemoveRequirement( mainAssemble );
		pickupBaseItem = null;
		otherItemTask = null;
		timesSearched = 0;
	}

	private void CheckFinishInteraction(){
		if( currentInteraction == null ){
			Debugger.LogError("No current interaction in AssembleItem");
			return;
		}
		ClearCurrentInteractionVariables();

		finalAssembleTask = currentInteraction.AttemptComplete( currentInteractionInfo );
        Debug.LogError( "CheckFinishInteraction() AttemptComplete: "+finalAssembleTask );
		if( finalAssembleTask != null ){
			AddRequirement( finalAssembleTask );
			finalAssembleTask.onFail += delegate{
				Fail("Final assemble task failed");
			};
			finalAssembleTask.onSuccess += CheckNextStep;
		}else{
			CheckNextStep();
		}
	}

	private void CheckNextStep(){
		if( finalAssembleTask != null ){
			RemoveRequirement( finalAssembleTask );
			finalAssembleTask = null;
		}
		//check remove
		if( nextInteraction != null ){
			var missingAttributes = self.biped.vision.FindClosestMissingAttributes( nextInteraction.baseRequest );
			foreach( var missingAttribute in missingAttributes ) Debug.LogError("Missing ~"+missingAttribute.ToString());
			if( missingAttributes != null ){
				//if none missing then move to next interaction
				if( missingAttributes.Count == 0 ){
					ContinueInteractionPath( path.IndexOf( nextInteraction ) );
				}else{
					//else make sure it's not any besides the current interaction resultBaseAttribute
					foreach( var missingAttribute in missingAttributes ){
						var found = false;
						foreach(var targetAttrib in currentInteraction.targetRequest.attributes ){
							if( targetAttrib.name == missingAttribute.name ){
								found = true;
								break;
							}
						}
						if( !found ){
							CreateSubAssembleTask( missingAttributes, path.IndexOf( nextInteraction ) );
							return;
						}
					}
					ContinueInteractionPath();
				}
			}else{	//otherwise it's missing everything
				CreateSubAssembleTask( missingAttributes, path.IndexOf( nextInteraction ) );
				missingAttributes = new List<Attribute>( nextInteraction.baseRequest.attributes );
			}
		}else{
			var finalItems = self.biped.vision.FindItemsByAttributes( targetAttributeRequest );
			bool finishedAssembly = false;
			if( finalItems.Count > 0 ){
				foreach( var item in finalItems ){
					if( item.HasAttributes( targetAttributeRequest ) ){
						finishedAssembly = true;
					}
				}
			}
			Debug.LogError("Reached end! "+finishedAssembly+" ? "+finalItems.Count);
			if( finishedAssembly ){
				Succeed();
			}else{
				ContinueInteractionPath();
			}
		}
	}

	private void PreparePickupForSpeechBubble( Task task ){
		task.onFixedUpdate += CheckPickupSearching;
	}

	//play confused animation if cant find items required
	private void CheckPickupSearching(){
		if( !searchTimeStart.HasValue ) return;

		int attributesReq = 0;
		int attributesFound = 0;
		if( pickupBaseItem != null ){
			attributesFound += System.Convert.ToInt32( pickupBaseItem.grabbable );
			attributesReq++;
		}
		if( otherItemTask != null ){
			attributesFound += System.Convert.ToInt32( (otherItemTask as Pickup != null) ? ( otherItemTask as Pickup).grabbable : otherItemTask.succeeded );
			attributesReq++;
		}
		if( attributesFound == attributesReq ){
			searchTimeStart = null;
			return;
		}
		if( Time.time > searchTimeStart.Value ){
			searchTimeStart = null;
			timesSearched++;
			searchTimeStart = Time.time+16f;
			//find how to add missing attributes
			var missingAttributes = self.biped.vision.FindClosestMissingAttributes( currentInteraction.baseRequest );

			if( missingAttributes == null ){
				if( currentInteraction.otherRequest.HasValue ) missingAttributes = self.biped.vision.FindClosestMissingAttributes( currentInteraction.otherRequest.Value );
			}else{
				if( currentInteraction.otherRequest.HasValue ){
					var otherMissingAttributes = self.biped.vision.FindClosestMissingAttributes( currentInteraction.otherRequest.Value );
					if( otherMissingAttributes != null ) missingAttributes.AddRange( otherMissingAttributes );
				}
			}

			CreateSubAssembleTask( missingAttributes );
		}
	}

	private void CreateSubAssembleTask( List<Attribute> missingAttributes, int? nextInteractionIndex=null ){
		if( missingAttributes == null ){
			Debug.LogError("TODO: ANIMATE SEARCH FOR ITEM");
			Debug.LogError(pickupBaseItem.attributeRequest.ToString());
			//TODO: ANIMATE SEARCH FOR ITEM
			return;
		}
		foreach( var missing in missingAttributes ) Debug.LogError("Missing: "+missing);
		Task subAssembleTask = null;
		foreach( var missingAttribute in missingAttributes ){
			Debug.LogError("Testing on: "+missingAttribute.name);
			var attributeRequest = new AttributeRequest( new Attribute[]{ missingAttribute }, false, CountMatch.EQUAL_OR_GREATER );
			var subTaskPath = self.itemInteractions.FindInteractionPath( attributeRequest, self );
			if( subTaskPath == null ) continue;
			
			subAssembleTask = new AssembleItem( self.autonomy, subTaskPath, attributeRequest );
			subAssembleTask.Start( _internalSource, name+" sub: "+subAssembleTask.name );

			subAssembleTask.onSuccess += delegate{
				Debug.LogError("###SUCCEEDED SUBASSEMBLY TASK### resuming...");
				ContinueInteractionPath( nextInteractionIndex );
			};
			break;
		}
		if( subAssembleTask == null ){
			Debug.LogError("Could not create sub assemble task. Defaulting to confused");
			if( self.biped ) SpeechBubble.Create( self.biped.head.rigidBody ).Display( GetSpriteIfAvailable( missingAttributes.ToArray() ), BuiltInAssetManager.main.FindSprite( "question" ) );

			if( self.mainAnimationLayer.currentBodySet["confused"] != null ){
				var playConfusedAnim = new PlayAnimation( self.autonomy, null, "confused", true, 1 );
				playConfusedAnim.Start( VivaScript._internalDefault, "confused" );

				playConfusedAnim.onEnterAnimation += delegate{
					self.PlayVoiceGroup( "confused" );
				};
			}
		}
	}

	private Sprite GetSpriteIfAvailable( Attribute[] attributes ){
		var itemTexture = Item.FindItemTextureWithAttribute( attributes );
		return itemTexture ? Sprite.Create( itemTexture, new Rect( 0, 0, itemTexture.width, itemTexture.height ), Vector2.zero, 1, 0, SpriteMeshType.FullRect ) : null;
	}

	private Task HandleNeedsToDropGrabbable( Grabber grabber ){
		// var dropZone = self.biped.vision.GetRandomVisibleZone();
		// if( dropZone == null ){
			var justDrop = new Task( self.autonomy );
			justDrop.onRegistered += delegate{
				grabber.ReleaseAll();
			};
			justDrop.onFixedUpdate += delegate{
				if( !grabber.grabbing ) justDrop.Succeed();
			};
			return justDrop;
		// }
		// return new Drop( self.autonomy, grabber.GetRandomGrabbable(), dropZone );
	}
}  

}