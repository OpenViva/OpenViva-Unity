using UnityEngine;
using UnityEditor;
using System.Collections;
using UnityEngine.InputSystem;


namespace viva{

 
public class KeyboardController : InputController {


    public KeyboardController():base( null ){
    }

    public override void OnEnter( Player player ){
        player.transform.rotation = Quaternion.identity;   //keyboard requires no rotation
    }

    public override void OnFixedUpdateControl( Player player ){
        if( GameDirector.instance.controlsAllowed != GameDirector.ControlsAllowed.NONE ){       
            Vector3 accel = new Vector3( player.movement.x, 0.0f, player.movement.y );
            if( accel != Vector3.zero ){
                Vector3 headPlaneXZ = player.head.TransformDirection( accel );
                headPlaneXZ.y = 0.0f;
                float speed;
                if(GameDirector.player.keyboardTargetHeight == GameDirector.player.keyboardFloorHeight){
                    speed = player.walkSpeed;
                }else{
                    speed = player.walkSpeed*( 1.0f+System.Convert.ToInt32( player.keyboardAlt )*2.0f );
                }            
                player.moveVel += headPlaneXZ.normalized*speed;
            }
        }

        float angleSlow = 1.0f-Mathf.Abs( player.head.forward.y )*0.5f;
        player.head.rotation *= Quaternion.Euler( player.mouseVelocitySum.x, player.mouseVelocitySum.y*angleSlow, 0.0f );
        player.mouseVelocitySum *= 0.6f;

	   	player.moveVel *= 0.85f;
        player.moveVel.y = player.rigidBody.velocity.y;
        player.rigidBody.velocity = player.moveVel;        

        player.FixedUpdatePlayerCapsule( player.keyboardCurrentHeight );
        player.ApplyHeadTransformToArmature();

        if( GameDirector.instance.controlsAllowed <= GameDirector.ControlsAllowed.HAND_INPUT_ONLY ){
            
            if( GameDirector.instance.controlsAllowed == GameDirector.ControlsAllowed.ALL ){
                player.UpdateInputKeyboardRotateHead();
                player.UpdateInputKeyboardCrouching();
            }
        }else{
            player.UpdateGUIKeyboardShortcuts();
        }
    }
}

}