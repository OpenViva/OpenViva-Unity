using UnityEngine;
using UnityEditor;
using System.Collections;
using UnityEngine.UI;
using Valve.VR;

namespace viva{

public class VRController : InputController {

    public VRController():base(null){
    }

    public override void OnEnter( Player player ){

        player.head.localPosition = Vector3.zero;
        player.head.localRotation = Quaternion.identity;
    }

    public override void OnFixedUpdateControl( Player player ){
        
        player.rightPlayerHandState.UpdateSteamVRInput();
        player.leftPlayerHandState.UpdateSteamVRInput();

        if( GameDirector.instance.controlsAllowed == GameDirector.ControlsAllowed.HAND_INPUT_ONLY || GameDirector.settings.vrControls != Player.VRControlType.TRACKPAD ){
            return;
        }
        player.UpdateTrackpadBodyRotation();
        player.UpdateVRTrackpadMovement();
        player.LateUpdateVRInputTeleportationMovement();
 
        player.FixedUpdatePlayerCapsule( player.head.localPosition.y );
        
        player.ApplyVRHandsToAnimation();
    }
}

}