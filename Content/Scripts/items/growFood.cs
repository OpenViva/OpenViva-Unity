using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using viva;


public class GrowFood: VivaScript{

    public class Growth{
        public Transform transform;
        public Item active;

        public Growth( Transform _transform ){
            transform = _transform;
        }
    }

    private Item tree;
    private List<Growth> growths = new List<Growth>();
    private string itemToGrow;

    public GrowFood( Item _tree ){
        tree = _tree;

        var foodType = tree.FindAttributeWithPrefix("grow:");
        if( foodType == null ){
            Debugger.LogError("Item \""+tree.name+"\" does not have a growth type");
            return;
        }

        itemToGrow = foodType.Substring(5);
        
        foreach( var childModel in tree.model.children ){
            if( childModel.name.StartsWith("growth") ){
                growths.Add( new Growth( childModel.rootTransform ) );
            }
        }
        AmbienceManager.main.onDayTimePassed.AddListener( this, new WaitEntry( GrowFoodOnGrowthPoints, 1f ) );

        Achievement.Add( "Grab a lemon","Lemon trees are scattered around. You will need the hammer to reach lemons.","lemon");
    }

    private void GrowFoodOnGrowthPoints(){
        foreach( var growth in growths ){
            if( growth.active ) continue; 
            var currentGrowth = growth;
            var food = Item.Spawn( itemToGrow, growth.transform.position, growth.transform.rotation );
            if( food ){
                currentGrowth.active = food;
                InitializeFood( food );
                SaveGrowths();
            }
        }

        AmbienceManager.main.onDayTimePassed.AddListener( this, new WaitEntry( GrowFoodOnGrowthPoints, 1f ) );
    }

    private void InitializeFood( Item item ){
        foreach( var grabbable in item.grabbables ){
            grabbable.onGrabbed.AddListener( this, OnFoodPicked );
        }
        item.SetImmovable( true );
        item.AddAttribute( Item.offerAttribute );
    }

    private void OnFoodPicked( GrabContext context ){
        context.grabbable.parentItem.SetImmovable( false );
        context.grabbable.onGrabbed.RemoveListener( this, OnFoodPicked );
        //find growth item belongs to
        foreach( var growth in growths ){
            if( growth.active == context.grabbable.parentItem ){
                growth.active = null;
            }
        }
        if( context.grabbable.parentItem?.name == "lemon" ) AchievementManager.main.CompleteAchievement( "Grab a lemon", true );
    }

    public void LoadFood( object[] foodObjs ){
        for( int i=0; i<foodObjs.Length; i++ ){
            var food = foodObjs[i] as Item;
            if( !food ) continue;
            InitializeFood( food );
            growths[i].active = food;
        }
    }

    public void SaveGrowths(){
        var foodObjs = new Item[ growths.Count ];
        for( int i=0; i<growths.Count; i++ ){
            foodObjs[i] = growths[i].active;
        }
        Save( "LoadFood", foodObjs );
    }
} 