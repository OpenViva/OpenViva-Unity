using System.Collections;
using UnityEngine;
using viva;


public class Recipes: VivaScript{


    public Recipes( Character character ){
        SetupInteractions( character );
    }

    private void SetupInteractions( Character character ){
		character.itemInteractions.Add( new Interaction(
			new AttributeRequest( new string[]{ "mortar" }, true, CountMatch.EQUAL ),
			new AttributeRequest( new string[]{ "wheat" }, true, CountMatch.EQUAL ),
			true,
			delegate( InteractionInfo info ){
				return CreateInteractionAnimTask(character,info,"stand","wheat into mortar right");
			},
			new AttributeRequest( new string[]{ "wheat seeds" }, true, CountMatch.EQUAL ),
			delegate( InteractionInfo info ){
				if( info.user && info.user.isBiped ){
					if( info.rightHandHasBaseItem ){
						info.user.biped.leftHandGrabber.ReleaseAll();
					}else{
						info.user.biped.rightHandGrabber.ReleaseAll();
					}
				}
				return null;
			}
		) );
		character.itemInteractions.Add( new Interaction(
			new AttributeRequest( new string[]{ "mortar", "3x wheat seeds" }, false, CountMatch.EQUAL_OR_GREATER ),
			new AttributeRequest( new string[]{ "pestle" }, true, CountMatch.EQUAL ),
			true,
			delegate( InteractionInfo info ){
				return CreateInteractionAnimTask(character,info,"stand","mortar and pestle grind left",6);
			},
			new AttributeRequest( new string[]{ "flour" }, true, CountMatch.EQUAL ),
			delegate( InteractionInfo info ){
				return null;
			}
		) );
		character.itemInteractions.Add( new Interaction(
			new AttributeRequest( new string[]{ "mortar", "3x wheat seeds" }, false, CountMatch.EQUAL_OR_GREATER ),
			new AttributeRequest( new string[]{ "pestle" }, true, CountMatch.EQUAL ),
			true,
			delegate( InteractionInfo info ){
				return CreateInteractionAnimTask(character,info,"stand","mortar and pestle grind left",5);
			},
			new AttributeRequest( new string[]{ "flour" }, true, CountMatch.EQUAL ),
			delegate( InteractionInfo info ){
				return null;
			}
		) );

		character.itemInteractions.Add( new Interaction(
			new AttributeRequest( new string[]{ "mixingBowl" }, true, CountMatch.EQUAL ),
			new AttributeRequest( new string[]{ "egg" }, true, CountMatch.EQUAL ),
			true,
			delegate( InteractionInfo info ){
				return CreateInteractionAnimTask(character,info,"stand","egg into mixing bowl right");
			},
			new AttributeRequest( new string[]{ "mixed egg" }, true, CountMatch.EQUAL ),
			delegate( InteractionInfo info ){
				return null;
			}
		) );

		character.itemInteractions.Add( new Interaction(
			new AttributeRequest( new string[]{ "mixingBowl" }, true, CountMatch.EQUAL ),
			new AttributeRequest( new string[]{ "flour" }, true, CountMatch.EQUAL ),
			true,
			delegate( InteractionInfo info ){
				return CreateInteractionAnimTask(character,info,"stand","egg into mixing bowl right");
			},
			new AttributeRequest( new string[]{ "mixing flour" }, true, CountMatch.EQUAL ),
			delegate( InteractionInfo info ){
				return null;
			}
		) );

		character.itemInteractions.Add( new Interaction(
			new AttributeRequest( new string[]{ "mixingBowl", "3x mixed egg", "mixing flour" }, true, CountMatch.EQUAL ),
			new AttributeRequest( new string[]{ "mixingSpoon" }, true, CountMatch.EQUAL ),
			true,
			delegate( InteractionInfo info ){
				return CreateInteractionAnimTask(character,info,"stand","mixing bowl mix left",5);
			},
			new AttributeRequest( new string[]{ "raw pastry" }, true, CountMatch.EQUAL ),
			delegate( InteractionInfo info ){
				return null;
			}
		) );

		CreateFlavorPastryEndRecipe( character, "strawberry" );
		CreateFlavorPastryEndRecipe( character, "cantaloupe" );
		CreateFlavorPastryEndRecipe( character, "peach" );
	}

	private void CreateFlavorPastryEndRecipe( Character character, string fruitFlavor ){
		
		character.itemInteractions.Add( new Interaction(
			new AttributeRequest( new string[]{ "mortar", "flour" }, true, CountMatch.EQUAL_OR_GREATER ),
			new AttributeRequest( new string[]{ fruitFlavor }, true, CountMatch.EQUAL ),
			true,
			delegate( InteractionInfo info ){
				return CreateInteractionAnimTask(character,info,"stand","wheat into mortar right");
			},
			new AttributeRequest( new string[]{ "flavor:"+fruitFlavor }, true, CountMatch.EQUAL ),
			delegate( InteractionInfo info ){
				return null;
			}
		) );
		character.itemInteractions.Add( new Interaction(
			new AttributeRequest( new string[]{ "flavor:"+fruitFlavor, "raw pastry" }, false, CountMatch.EQUAL ),
			new AttributeRequest( new string[]{ "oven" }, true, CountMatch.EQUAL ),
			false,
			delegate( InteractionInfo info ){
				var oven = info.otherItem.FindZone( "oven_zone_box" );
				if( !oven ) return null;
				var grabbables = info.baseItem.GetGrabbablesByCharacter( info.user );
				var putInOven = new Drop( info.user.autonomy, grabbables.Count > 0 ? grabbables[0] : null, oven );
				putInOven.onSuccess += delegate{
					Sound.Create( oven.transform.TransformPoint( oven.boxCollider.center ) ).Play( "generic", "oven", "startCooking.wav" );
				};
				return putInOven;
			},
			new AttributeRequest( new string[]{ "flavor:"+fruitFlavor, "baked pastry" }, true, CountMatch.EQUAL ),
			delegate( InteractionInfo info ){
				var waitForItemSpawn = new Task( info.user.autonomy );
				var spawnPos = info.baseItem.transform.position;
				var item = Item.Spawn( "baked pastry", spawnPos, Quaternion.identity );
				if( item ){
					waitForItemSpawn.Succeed();
					character.biped.vision.See( item );
					var flavor = info.baseItem.FindAttributeWithPrefix("flavor:");
					if( flavor != null ){
						info.baseItem?.RemoveAttribute( flavor );
						item.AddAttribute( flavor );
					}
                	Viva.Destroy( info.baseItem );
				}
				return waitForItemSpawn;
			}
		) );
	}

	private Vector3 CalculateSafeSpawnPos( Character character, Vector3 startPos, float itemRadius ){
		var flatForward = Tools.FlatForward( character.biped.head.target.forward );
		return startPos+flatForward*itemRadius;
	}

	private PlayAnimation CreateInteractionAnimTask( Character character, InteractionInfo info, string bodySetName, string bodySetAnimation, float loopsToComplete=1 ){
		var combineItems = new PlayAnimation( character.autonomy, bodySetName, bodySetAnimation, true, loopsToComplete );
		combineItems.onRegistered += delegate{
			if( info.rightHandHasBaseItem ) combineItems.MirrorTargetAnimation(); 
		};
		combineItems.onFixedUpdate += delegate{
			if( combineItems.hasAnimationControl && combineItems.GetPlayedNormalizedTime()>=loopsToComplete ){
				combineItems.Succeed();
			}
		};
		combineItems.onEnterAnimation += delegate{
			character.biped.rightArmIK.strength.Add( "recipe combine", 0 );
			character.biped.leftArmIK.strength.Add( "recipe combine", 0 );
		};
		combineItems.onExitAnimation += delegate{
			character.biped.rightArmIK.strength.Remove( "recipe combine" );
			character.biped.leftArmIK.strength.Remove( "recipe combine" );
		};
		return combineItems;
	}
} 