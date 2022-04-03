using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.UI;



namespace viva{

public sealed partial class VRSettings : MonoBehaviour{

    public static int firstVR = 0;

    [SerializeField]
    private GameObject calibrateHandInstructions;
    [SerializeField]
    private GameObject calibrateBodyInstructions;
    [SerializeField]
    private Transform dummy;
    [SerializeField]
    private Text switchControlsText;

    private int calibrateTriggerCounts = 0;
    private Transform calibrateTarget;
    private bool calibratingHands = false;
    private Quaternion lastCalibrateTargetRot;


    public void onEnable(){
        if( VivaPlayer.user == null ) return;
        var vrCameraControls = VivaPlayer.user.controls as VRCharacterControls;
        if( vrCameraControls == null ) return;
        SetSwitchControlsText( vrCameraControls.rightHandedness );
    }

    private void OnDisable(){
        EndCalibrateVRBody();
    }

    public void SwitchControls(){
        var vrCameraControls = VivaPlayer.user.controls as VRCharacterControls;
        vrCameraControls.SetControlHandedness( !vrCameraControls.rightHandedness );
        SetSwitchControlsText( vrCameraControls.rightHandedness );
    }

    private void SetSwitchControlsText( bool rightHanded ){
        switchControlsText.text = "Switch Controls\n ("+(rightHanded?"Right":"Left")+" is move)";
    }

    public void BeginCalibrateVRBody(){
        if( VivaPlayer.user == null ) return;

        calibrateBodyInstructions.SetActive( true );
        
        calibrateTriggerCounts = 0;
        Viva.input.actions.vrControls.rightTriggerClick.performed += HandleCalibrateVRBodyTrigger;
        Viva.input.actions.vrControls.leftTriggerClick.performed += HandleCalibrateVRBodyTrigger;
    }

    public void EndCalibrateVRBody(){
        if( Viva.input == null ) return;
        calibrateBodyInstructions.SetActive( false );
        Viva.input.actions.vrControls.rightTriggerClick.performed -= HandleCalibrateVRBodyTrigger;
        Viva.input.actions.vrControls.leftTriggerClick.performed -= HandleCalibrateVRBodyTrigger;
    }

    private void HandleCalibrateVRBodyTrigger( InputAction.CallbackContext ctx ){
        calibrateTriggerCounts += System.Convert.ToInt32( ctx.ReadValueAsButton() )*2-1;
        if( calibrateTriggerCounts == 1 ){
            CalibrateVRBody();
        }
    }

    public void CalibrateVRBody(){
        
        var vrCameraControls = VivaPlayer.user.controls as VRCharacterControls;
        if( vrCameraControls == null ) return;

        var bipedRagdoll = VivaPlayer.user.character.biped;
        var profile = VivaPlayer.user.character.model.bipedProfile;
        var rightController = vrCameraControls.rightHandTPD.transform;
        var leftController = vrCameraControls.leftHandTPD.transform;
        var head = vrCameraControls.headTPD.transform;

        var newHeight = head.localPosition.y;
        VivaPlayer.user.character.model.Resize( newHeight );

        var trueRightArmSpan = Vector3.Distance(
            new Vector3( rightController.position.x, 0, rightController.position.z ),
            new Vector3( head.position.x, 0, head.position.z )
        )-profile.shoulderWidth*VivaPlayer.user.character.scale;

        var trueLeftArmSpan = Vector3.Distance(
            new Vector3( leftController.position.x, 0, leftController.position.z ),
            new Vector3( head.position.x, 0, head.position.z )
        )-profile.shoulderWidth*VivaPlayer.user.character.scale;

        var avgTrueArmSpan = (trueRightArmSpan+trueLeftArmSpan)/2;
        avgTrueArmSpan /= VivaPlayer.user.character.scale;

        bipedRagdoll.rightArm.target.localPosition = bipedRagdoll.rightArm.target.localPosition.normalized*( avgTrueArmSpan/2 );
        bipedRagdoll.rightHand.target.localPosition = bipedRagdoll.rightHand.target.localPosition.normalized*( avgTrueArmSpan/2 );

        bipedRagdoll.leftArm.target.localPosition = bipedRagdoll.leftArm.target.localPosition.normalized*( avgTrueArmSpan/2 );
        bipedRagdoll.leftHand.target.localPosition = bipedRagdoll.leftHand.target.localPosition.normalized*( avgTrueArmSpan/2 );

        //calibrate hand offset (assuming TPOSE)
        var flatHeadForward = Tools.FlatForward( vrCameraControls.headTPD.transform.forward );
        var flatHeadRot = Quaternion.LookRotation( flatHeadForward, Vector3.up );
        
        dummy.rotation = flatHeadRot;

        var localHandForward = RoundToNearestAxis( dummy.InverseTransformDirection( -vrCameraControls.rightHandTPD.transform.up ) );
        var localHandUp = RoundToNearestAxis( dummy.InverseTransformDirection( vrCameraControls.rightHandTPD.transform.forward ) );

        if( localHandForward == localHandUp ){
            Debug.LogError("Error rounding hands to nearest axis");
            VRCharacterControls.controllerHandOffset = Quaternion.identity;
        }else{
            VRCharacterControls.controllerHandOffset = Quaternion.Inverse( Quaternion.Euler( 0,90,-90 ) )*Quaternion.LookRotation( localHandForward, localHandUp );
        }

        // VivaPlayer.user.gestures.UnbindFromTrackedHands();
        // VivaPlayer.user.character.RebuildRagdoll();
        // VivaPlayer.user.gestures.BindToTrackedHands( ragdoll.rightHand.target, ragdoll.leftHand.target, true );
        // vrCameraControls.SetupRagdollForVRIKRig( ragdoll );

        var vrRigScale = Vector3.one/(VivaPlayer.user.character.model.rootTransform.localScale.y*VivaPlayer.user.character.model.rootTransform.localScale.y);
        vrCameraControls.transform.localScale = vrRigScale;

        vrRigScale /= VivaPlayer.user.character.model.armature.localScale.y;
        bipedRagdoll.rightHand.rigidBody.transform.localScale = vrRigScale;
        bipedRagdoll.rightHand.target.localScale = vrRigScale;
        bipedRagdoll.leftHand.rigidBody.transform.localScale = vrRigScale;
        bipedRagdoll.leftHand.target.localScale = vrRigScale;
        
        EndCalibrateVRBody();
    }

    public Vector3 debug = Vector3.zero;

    private Vector3 RoundToNearestAxis( Vector3 vec ){
        return new Vector3(
            Mathf.Round( vec.x ),
            Mathf.Round( vec.y ),
            Mathf.Round( vec.z )
        );
    }
}

}
