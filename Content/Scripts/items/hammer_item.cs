using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using viva;


public class Hammer: VivaScript{

    private Item item;
    private ItemUserListener userListener;
    private Item activeGhost;
    private List<Item> items = new List<Item>();
    private bool freeze = true;
    private Item lastHitItem;
    private int lastHitCount;
    private bool align = false;
    private float lastHitTime;



    public Hammer( Item _item ){
        item = _item;

        userListener = new ItemUserListener( item, this, OnBind, OnUnbind );

        Sound.PreloadSet("tools");

        Achievement.Add( "Find a hammer","They can be used to build small structures.","hammer");
    }

    private void OnBind( Character newUser, GrabContext context ){
        if( newUser.isPossessedByKeyboard ){

            AchievementManager.main.CompleteAchievement( "Find a hammer", true );

            SpawnNextGhostItem();
            newUser.autonomy.onFixedUpdate.AddListener( this, UpdateGhostItemTransform );
            newUser.GetInput( Input.LeftAction ).onDown.AddListener( this, ApplyGhost );
            newUser.GetInput( Input.Z ).onDown.AddListener( this, Undo );
            newUser.GetInput( Input.MiddleAction ).onDown.AddListener( this, ToggleFreeze );
            newUser.GetInput( Input.MiddleAction ).onUp.AddListener( this, ToggleFreeze );
            newUser.GetInput( Input.LeftShift ).onDown.AddListener( this, ToggleAlign );
            newUser.GetInput( Input.LeftShift ).onUp.AddListener( this, ToggleAlign );
            
            var rotateListener = new Task( newUser.autonomy );
            rotateListener.onFixedUpdate += RotateProp;
            rotateListener.StartConstant( this, "rotate prop" );
            MessageManager.main.DisplayMessage( Vector3.zero, "Z to UNDO. Hold Middle Mouse to ROTATE, Shift to align", delegate{ return !userListener.character; }, false );

        }else if( newUser.isPossessedByVR ){
            SpawnNextGhostItem();
            newUser.autonomy.onFixedUpdate.AddListener( this, UpdateGhostItemTransform );
            item.onCollision.AddListener( this, OnCollision );
        }
    }

    private void OnCollision( Collision collision ){
        if( Time.time-lastHitTime < 0.15f ) return;
        lastHitTime = Time.time;

        bool hitWithHead = false;
        for( int i=0; i<collision.contactCount; i++ ){
            var contact = collision.GetContact(i);
            if( contact.thisCollider.name == "rigid_convex" ){
                hitWithHead = true;
                break;
            }   //must hit with head of hammer
        }
        if( !hitWithHead ) return;

        var itemHit = Util.GetItem( collision.rigidbody );
        if( itemHit && collision.relativeVelocity.sqrMagnitude > 0.5f ){
            if( itemHit.HasAttribute( "construction" ) ){
                Nail( itemHit );

                var handle = Sound.Create( itemHit.rigidBody.worldCenterOfMass );
                handle.volume = Mathf.Clamp01( collision.relativeVelocity.magnitude/0.5f );
                handle.Play( "tools", "nail hit" );

            }
        }
    }

    private void Nail( Item itemHit ){
        if( lastHitItem != itemHit ){
            lastHitCount = 0;
            lastHitItem = itemHit;
        }
        if( !lastHitItem ) return;
        lastHitCount++;
        if( lastHitCount%3 == 0 ){
            if( lastHitItem.immovable ){
                Sound.Create( lastHitItem.rigidBody.worldCenterOfMass ).Play( "tools", "hammer", "undo.wav" );
            }else{
                Sound.Create( lastHitItem.rigidBody.worldCenterOfMass ).Play( "tools", "hammer", "spawn.wav" );
            }
            lastHitItem.SetImmovable( !lastHitItem.immovable ); //toggle
        }
    }

    private void ToggleFreeze(){
        freeze = !freeze;
        ApplyFreezeState();
    }

    private void RotateProp(){
        if( !activeGhost ) return;
        if( freeze ) return;
        if( align ){
            var body = activeGhost.rigidBody;
            body.MoveRotation( Quaternion.LookRotation( Tools.RoundToNearestAxis( body.transform.forward ), Tools.RoundToNearestAxis( body.transform.up ) ) );
        }else{
            var mouseVel = Viva.input.mouseVelocity;
            activeGhost.rigidBody.AddTorque( Camera.main.transform.TransformDirection( new Vector3( mouseVel.y, mouseVel.x, 0 )*0.1f ), ForceMode.VelocityChange );
        }
    }

    private void ToggleAlign(){
        align = !align;
    }

    private void ApplyFreezeState(){
        if( activeGhost ){
            activeGhost.rigidBody.freezeRotation = freeze;
            foreach( var mat in activeGhost.model.renderer.materials ){
                var emission = freeze ? new Vector4( 100, 3000, 8000 ) : new Vector4( 2000, 2000, 2000 );
                mat.SetVector( "_EmissionMult", emission );
            }
        }
        ApplyKeyboardEnableLook( userListener.character );
    }

    private void ApplyKeyboardEnableLook( Character character ){
        var kbUser = character.possessor;
        if( kbUser ){
            var kbControls = kbUser.controls as KeyboardCharacterControls;
            if( kbControls ){
                if( !freeze ){
                    kbControls.enableLook.Add( "hammer controls", 0 );
                }else{
                    kbControls.enableLook.Remove( "hammer controls" );
                }
            }
        }
    }

    private void Undo(){
        if( items.Count > 0 ){
            if( activeGhost ){
                Viva.Destroy( activeGhost );
            }
            var old = items[ items.Count-1 ];
            items.RemoveAt( items.Count-1 );
            Sound.Create( old.rigidBody.worldCenterOfMass ).Play( "tools", "hammer", "undo.wav" );

            old.enabled = false;
            activeGhost = old;
            ApplyFreezeState();
        }
    }

    private void SpawnNextGhostItem( Vector3? spawnPosOverride = null, Quaternion? spawnRotOverride=null ){
        var item = Item.Spawn( "plank1", spawnPosOverride.HasValue ? spawnPosOverride.Value : Camera.main.transform.position, spawnRotOverride.HasValue ? spawnRotOverride.Value : Quaternion.identity );
        if( item ){
            if( !userListener.character || !userListener.character.isPossessedByKeyboard ){
                Viva.Destroy( item );
                return;
            }else{
                activeGhost = item;
                activeGhost.enabled = false;
                ApplyFreezeState();
            }
        }
    }

    private void ApplyGhost(){
        if( InputManager.cursorActive ) return;
        if( VivaPlayer.user.movement.leftShiftDown ) return;

        if( activeGhost ){
            activeGhost.enabled = true;
            activeGhost.SetImmovable( true );
            Scene.main.BakeNavigation();

            Sound.Create( activeGhost.rigidBody.worldCenterOfMass ).Play( "tools", "hammer", "spawn.wav" );
            items.Add( activeGhost );

            var nextSpawnPos = activeGhost.rigidBody.worldCenterOfMass-Camera.main.transform.forward;
            var nextSpawnRot =activeGhost.rigidBody.rotation;
            activeGhost = null;
            SpawnNextGhostItem( nextSpawnPos, nextSpawnRot );
        }
    }

    private void UpdateGhostItemTransform(){
        if( !activeGhost ) return;

        var bounds = activeGhost.model.bounds;
        if( !bounds.HasValue ) return;
        var radius = bounds.Value.extents.magnitude*1.1f+0.25f;
        var targetPos = Camera.main.transform.position+Camera.main.transform.forward*radius;
        var targetVel = targetPos-activeGhost.rigidBody.worldCenterOfMass;
        activeGhost.rigidBody.AddForce( targetVel, ForceMode.VelocityChange );
    }

    private void OnUnbind( Character oldUser, GrabContext context ){
        if( oldUser.isPossessed ){
            oldUser.autonomy.onFixedUpdate.RemoveListener( this, UpdateGhostItemTransform );
            if( activeGhost ){
                Viva.Destroy( activeGhost );
                activeGhost = null;
            }
            freeze = true;
            align = false;
            ApplyKeyboardEnableLook( oldUser );

            if( oldUser.isPossessedByKeyboard ){
                oldUser.GetInput( Input.LeftAction ).onDown.RemoveListener( this, ApplyGhost );
                oldUser.GetInput( Input.Z ).onDown.RemoveListener( this, Undo );
                oldUser.GetInput( Input.MiddleAction ).onDown.RemoveListener( this, ToggleFreeze );
                oldUser.GetInput( Input.MiddleAction ).onUp.RemoveListener( this, ToggleFreeze );
                oldUser.GetInput( Input.LeftShift ).onDown.RemoveListener( this, ToggleAlign );
                oldUser.GetInput( Input.LeftShift ).onUp.RemoveListener( this, ToggleAlign );
            }else{
                
            }
        }
    }
} 