using System.Collections;
using UnityEngine;
using viva;


public class ChickenLegItem: VivaScript{

    private Item item;

    public ChickenLegItem( Item _item ){
        item = _item;

        item.onAttributeChanged.AddListener( this, OnAttributeChanged );

        item.AddAttribute("raw");
        item.AddAttribute( Item.offerAttribute );

        Achievement.Add( "Cook a chicken leg", "Put a chicken leg inside the oven" );
    }

    private void Baked(){
        if( item.HasAttribute( "raw" ) ){
            item.RemoveAttribute( "raw" );
            item.AddAttribute( "food" );
        }
    }

    private void OnEaten(){
        
        Grabber grabber = null;
        var grabbable = item.GetRandomGrabbable();
        if( grabbable ){
            var grabbers = grabbable.GetGrabbers();
            if( grabbers.Count > 0 ) grabber = grabbers[0];
        }
        var chickenBone = Item.Spawn( "chicken bone", item.transform.position, item.transform.rotation );
        grabber.Grab( chickenBone.GetRandomGrabbable(), false );
    }

    private void OnAttributeChanged( Item item, Attribute attribute ){
        if( attribute.count == 0 && attribute.name == "raw" ){

		    AchievementManager.main.CompleteAchievement( "Cook a chicken leg", true );

            item.model.SetTexture( "chickenLeg", "_BaseColorMap", "chickenLeg_cooked_BaseColorMap" );
            item.model.SetTexture( "chickenLeg", "_MaskMap", "chickenLeg_cooked_MaskMap" );
            item.model.SetTexture( "chickenLeg", "_NormalMap", "chickenLeg_cooked_NormalMap" );
        }
    }
} 