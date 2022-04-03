using System.Collections;
using UnityEngine;
using viva;


public class Food: VivaScript{

    private Item item;
    private ItemUserListener itemUserListener;

    public Food( Item _item ){
        item = _item;

        itemUserListener = new ItemUserListener( item, this, OnPickedUp, OnDropped );
    }

    private void OnPickedUp( Character newUser, GrabContext context ){
        if( newUser && !newUser.isPossessed ){
            
            SetupAnimations( newUser );
            var inspectFood = new Timer( newUser.autonomy, 1.5f );
            bool smelled = false;
            inspectFood.onSuccess += delegate{
                inspectFood.Reset();
                if( !item.HasAttribute("food") ) return;
                if( !smelled ){
                    smelled = true;
                    if( Random.value < 0.5f ) SmellFood( newUser, context.grabber );
                }else{
                    EatFood( newUser, context.grabber );
                }
            };

            inspectFood.StartConstant( this, "inspect food");
        }
    }

    private void OnDropped( Character oldUser, GrabContext context ){
        oldUser.autonomy.RemoveTask( "inspect food" );
    }

    private void SetupAnimations( Character character ){
        var stand = character.animationSet.GetBodySet("stand");
        var smellFoodRight = stand.Single( "smell food right", "stand_food_smell_right", false );
		smellFoodRight.AddEvent( Event.Voice(0.15f,"inhale") );
		smellFoodRight.AddEvent( Event.Voice(0.6f,"exhale") );
        smellFoodRight.nextState = stand["idle"];
        smellFoodRight.curves[BipedRagdoll.headID] = new Curve(0,0.1f);
        smellFoodRight.curves[BipedRagdoll.rightArmID] = new Curve(0,0.1f);

        var smellFoodLeft = stand.Single( "smell food left", "stand_food_smell_left", false );
		smellFoodLeft.AddEvent( Event.Voice(0.15f,"inhale") );
		smellFoodLeft.AddEvent( Event.Voice(0.6f,"exhale") );
        smellFoodLeft.nextState = stand["idle"];
        smellFoodLeft.curves[BipedRagdoll.headID] = new Curve(0,0.1f);
        smellFoodLeft.curves[BipedRagdoll.leftArmID] = new Curve(0,0.1f);

        var wantedMore = stand.Single( "wanted more", "stand_headpat_happy_wanted_more", false );
        wantedMore.nextState = stand["idle"];
        wantedMore.curves[BipedRagdoll.headID] = new Curve(0,0.2f);
        wantedMore.AddEvent( Event.Voice(0.05f,"startle soft") );
        wantedMore.AddEvent( Event.Voice(0.4f,"disappointed") );

        var eatFoodRight = stand.Single( "eat right", "stand_food_bite_right", false );
		eatFoodRight.AddEvent( Event.Voice(0.1f,"eat") );
		eatFoodRight.AddEvent( Event.Function(0.2f, this, Consume) );
        eatFoodRight.nextState = stand["idle"];
        eatFoodRight.curves[BipedRagdoll.headID] = new Curve(0,0.1f);
        eatFoodRight.curves[BipedRagdoll.rightArmID] = new Curve(0,0.1f);

        var eatFoodLeft = stand.Single( "eat left", "stand_food_bite_left", false );
        eatFoodLeft.AddEvent( Event.Voice(0.1f,"eat") );
        eatFoodLeft.AddEvent( Event.Function(0.2f, this, Consume) );
        eatFoodLeft.nextState = stand["idle"];
        eatFoodLeft.curves[BipedRagdoll.headID] = new Curve(0,0.1f);
        eatFoodLeft.curves[BipedRagdoll.leftArmID] = new Curve(0,0.1f);

        var yummy = stand.Single( "yummy", "stand_yummy", false );
        yummy.AddEvent( Event.Voice(0.1f,"yummy") );
        yummy.nextState = stand["idle"];
        yummy.curves[BipedRagdoll.headID] = new Curve(0,0.3f);
    }

    private void Consume(){
        if( itemUserListener.character ){
            var yummy = new PlayAnimation( itemUserListener.character.autonomy, null, "yummy" );
            yummy.Start( this, "yummy", -1 );
        }
        Debug.LogError("DETROY ITEM "+item);

        item.scriptManager.CallOnAllScripts( "OnEaten", null, true );
        Viva.Destroy( item );
    }

    private void SmellFood( Character character, Grabber grabber ){
        var smellAnim = new PlayAnimation( character.autonomy, null, "smell food "+grabber.signName, true, 0.5f );

        smellAnim.onAutonomyExit += delegate{
            itemUserListener.character?.autonomy.FindTask("inspect food").Reset();
        };
        smellAnim.onFail += delegate{
            if( item && !item.destroyed ){
                var wantedMoreAnimation = new PlayAnimation( character.autonomy, null, "wanted more" );
                wantedMoreAnimation.onRegistered += delegate{
                    if( item && character.isBiped ) character.biped.lookTarget.SetTargetRigidBody( item.rigidBody );
                };
                wantedMoreAnimation.Start( this, "wanted more" );
            }
        };
        
        //must be grabbing item while playing animation
        var pickup = new Pickup( character.autonomy, item );
        smellAnim.AddRequirement( pickup );

        smellAnim.Start( this, smellAnim.name );
    }

    private void EatFood( Character character, Grabber grabber ){
        var eatAnim = new PlayAnimation( character.autonomy, null, "eat "+grabber.signName, true );

        eatAnim.onAutonomyExit += delegate{
            itemUserListener.character?.autonomy.FindTask("inspect food").Reset();
        };
        eatAnim.onFail += delegate{
            if( item && !item.destroyed ){
                var wantedMoreAnimation = new PlayAnimation( character.autonomy, null, "wanted more", true, 0, false );
                wantedMoreAnimation.onRegistered += delegate{
                    if( item && character.isBiped ) character.biped.lookTarget.SetTargetRigidBody( item.rigidBody );
                };
                wantedMoreAnimation.Start( this, "wanted more" );
            }
        };
        
        //must be grabbing item while playing animation
        var pickup = new Pickup( character.autonomy, item );
        eatAnim.AddRequirement( pickup );
        eatAnim.Start( this, eatAnim.animationGroup );
    }
} 