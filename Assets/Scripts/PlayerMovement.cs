using UnityEngine;
using System.Collections;
using System.Collections.Generic;


namespace viva{

public partial class PlayerMovement : MonoBehaviour{

    [SerializeField]
    private VivaPlayer player;
    [SerializeField]
    private float accel = 5.0f;
    [SerializeField]
    private float movementFriction = 0.9f;
    [Range(45,180)]
    [SerializeField]
    private float turnDegrees = 45.0f;

    public Quaternion keyboardMouseLook = Quaternion.identity;
    public Vector2 mouseVelSum;
    public Vector3 movementDir = Vector3.zero;
    public Vector3 movementNormDir = Vector3.zero;
    public bool leftShiftDown = false;
    private bool turnReset = true;

    
    public void FlyMovement(){

        // head.rotation *= Quaternion.Euler( keyboardMouseVel.y, -keyboardMouseVel.x, 0.0f );
        // keyboardMouseVel = Vector3.zero; //reset to zero
        // head.rotation = Quaternion.LookRotation( head.forward, Vector3.up );

        Vector3 movementVel = player.character.biped.root.rigidBody.velocity;
        movementVel.x *= movementFriction;
        movementVel.y *= movementFriction;
        movementVel.z *= movementFriction;
        
        // Vector3 cameraForward = player.character.biped.headLookAt.head.TransformDirection( movementNormDir );
        // movementVel += cameraForward*accel;
        
        // body.velocity = movementVel;
    }

    private void OnDisable(){
        movementDir = Vector3.zero;
        movementNormDir = Vector3.zero;
    }

    private void OnEnable(){
        movementDir = Vector3.zero;
        movementNormDir = Vector3.zero;
        leftShiftDown = false;
    }
    
    public void KeyboardForwardDown(){
        movementDir += Vector3.forward;
        movementDir.z = Mathf.Min( movementDir.z, 1 );
        movementNormDir = movementDir.normalized;
    }
    public void KeyboardForwardUp(){
        movementDir -= Vector3.forward;
        movementNormDir = movementDir.normalized;;
    }
    
    public void KeyboardBackwardDown(){
        movementDir += Vector3.back;
        movementDir.z = Mathf.Max( movementDir.z, -1 );
        movementNormDir = movementDir.normalized;
    }
    public void KeyboardBackwardUp(){
        movementDir -= Vector3.back;
        movementNormDir = movementDir.normalized;;
    }
    
    public void KeyboardRightDown(){
        movementDir += Vector3.right;
        movementDir.x = Mathf.Min( movementDir.x, 1 );
        movementNormDir = movementDir.normalized;
    }
    public void KeyboardRightUp(){
        movementDir -= Vector3.right;
        movementNormDir = movementDir.normalized;
    }
    
    public void KeyboardLeftDown(){
        movementDir += Vector3.left;
        movementDir.x = Mathf.Max( movementDir.x, -1 );
        movementNormDir = movementDir.normalized;
    }
    public void KeyboardLeftUp(){
        movementDir -= Vector3.left;
        movementNormDir = movementDir.normalized;
    }
    
    public void KeyboardLeftShiftDown(){
        leftShiftDown = true;
    }
    public void KeyboardLeftShiftUp(){
        leftShiftDown = false;
    }
    
    public void HandleAnalogMovement( Vector2 axis ){
        player.movement.movementDir.x = axis.x;
        player.movement.movementDir.z = axis.y;
    }

    public void HandleAnalogTurning( Vector2 axis ){
        var absX = Mathf.Abs( axis.x );
        if( absX > 0.85f ){
            if( turnReset ){
                turnReset = false;
                Turn( Mathf.Sign( axis.x )*turnDegrees );
            }
        }else if( absX < 0.7f ){
            turnReset = true;
        }
    }
    public void Turn( float degrees ){
        VivaPlayer.user.character.ragdoll.transform.RotateAround( player.camera.transform.position, Vector3.up, degrees );
        foreach( var muscle in VivaPlayer.user.character.ragdoll.muscles ){
            muscle.Read();
        }
    }
}

}