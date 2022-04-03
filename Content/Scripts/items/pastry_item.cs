using System.Collections;
using UnityEngine;
using viva;


public class PastryItem: VivaScript{

    private Item item;

    public PastryItem( Item _item ){
        item = _item;    
        item.model.SetMaterialColor( "filling", new Color(0.89f,0.79f,0.63f) );

        item.AddAttribute("raw");

        item.onAttributeChanged.AddListener( this, OnAttributeChanged );

        item.onCollision.AddListener( this, OnCollision );

    }

    private bool ValidForAction( Item otherItem ){
        if( Vector3.Dot( item.rigidBody.transform.up, Vector3.up ) < 0.3f ) return false;
        if( item.rigidBody.transform.InverseTransformPoint( otherItem.rigidBody.worldCenterOfMass ).y < 0f ) return false;
        return true;

    }

    private void OnCollision( Collision collision ){
        var collisionItem = Util.GetItem( collision.rigidbody );
        if( !collisionItem || collisionItem.destroyed || collisionItem.isBeingGrabbed ) return;

        if( !ValidForAction( collisionItem ) ) return;

        if( collisionItem.HasAttribute("strawberry") ){
            Viva.Destroy( collisionItem );
            SetFlavor("strawberry");
            Sound.Create( item.transform.position ).Play( "mortar", "jellyScoop" );
        }else if( collisionItem.HasAttribute("cantaloupe") ){
            Viva.Destroy( collisionItem );
            SetFlavor("cantaloupe");
            Sound.Create( item.transform.position ).Play( "mortar", "jellyScoop" );
        }else if( collisionItem.HasAttribute("peach") ){
            Viva.Destroy( collisionItem );
            SetFlavor("peach");
            Sound.Create( item.transform.position ).Play( "mortar", "jellyScoop" );
        }
    }

    private void SetFlavor( string flavor ){
        item.RemoveAttributeWithPrefix("flavor:");
        item.AddAttribute( "flavor:"+flavor );
    }

    private void OnAttributeChanged( Item item, Attribute attribute ){
        if( attribute.name == "flavor:strawberry" ){
            item.model.SetMaterialColor( "filling", new Color(1f,0.2f,0.2f) );
        }
        if( attribute.name == "flavor:cantaloupe" ){
            item.model.SetMaterialColor( "filling", new Color(0.4f,0.8f,0.2f) );
        }
        if( attribute.name == "flavor:peach" ){
            item.model.SetMaterialColor( "filling", new Color(1f,0.7f,0.5f) );
        }
    }

    public void Baked(){

        if( item.HasAttribute("raw") ){
            item.RemoveAttribute("raw");
            var flavor = item.FindAttributeWithPrefix("flavor:");
            if( flavor == null ){
                ParticleSystemManager.CreateParticleSystem( "smoke poof", item.rigidBody.worldCenterOfMass );
                Viva.Destroy( item );
                Sound.Create( item.rigidBody.worldCenterOfMass ).Play( "generic", "oven", "burnUp.wav" );

                MessageManager.main.DisplayMessage( item.rigidBody.worldCenterOfMass, "Pastries without flavor will burn up!", null );
            }else{
                Item.Spawn( "baked pastry", item.transform.position, item.transform.rotation );
                if( flavor != null ){
                    item?.RemoveAttribute( flavor );
                    item.AddAttribute( flavor );
                }
                Sound.Create( item.rigidBody.worldCenterOfMass ).Play( "generic", "oven", "startCooking.wav" );
                Viva.Destroy( item );
            }
        }
    }
} 