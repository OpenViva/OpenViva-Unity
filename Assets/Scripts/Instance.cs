using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


namespace viva{


public class Instance{

	public static readonly int pupilShrinkID = Shader.PropertyToID("_PupilShrink");
	public static readonly int sideMultiplierID = Shader.PropertyToID("_SideMultiplier");
	public static readonly int pupilRightID = Shader.PropertyToID("_PupilRight");
	public static readonly int pupilUpID = Shader.PropertyToID("_PupilUp");
	public static readonly int skinColorID = Shader.PropertyToID("_SkinColor");
    public static readonly int toonProximityAmbienceID = Shader.PropertyToID("_ToonProximityAmbience");
	
	public static readonly int forwardSpeedID = Animator.StringToHash("forwardSpeed");
	public static readonly int speedID = Animator.StringToHash("speed");
	public static readonly int sidewaysSpeedID = Animator.StringToHash("sidewaysSpeed");
	public static readonly int headpatRoughnessID = Animator.StringToHash("HeadpatRoughness");
	public static readonly int headpatProperID = Animator.StringToHash("HeadpatProper");
	public static readonly int chopsticksReachID = Animator.StringToHash("ChopsticksReach");
	public static readonly int pickupHeightID = Animator.StringToHash("pickupHeight");
	public static readonly int pickupReachID = Animator.StringToHash("pickupReach");
	public static readonly int pickupReverseID = Animator.StringToHash("pickupReverse");
	public static readonly int pokeTummyXID = Animator.StringToHash("pokeTummyX");
	public static readonly int begDirection = Animator.StringToHash("begDirection");
	public static readonly int cameraPitchDir = Animator.StringToHash("cameraPitchDir");
	public static readonly int cameraPrepared = Animator.StringToHash("cameraPrepared");
	public static readonly int splashDirID = Animator.StringToHash("splashDirection");
	public static readonly int hugSideID = Animator.StringToHash("hugSide");

	public static readonly int vrSoapRubID = Animator.StringToHash("vrSoapRub");

	public static readonly int bubbleSizeID = Shader.PropertyToID("_BubbleSize");
	public static readonly int alphaID = Shader.PropertyToID("_Alpha");
	
	public static readonly float maxIK = 0.65f;
	public static readonly int visionMask = LayerMask.GetMask(new string[]{"bodyPartItems","items"});
	public static readonly int regionMask = LayerMask.GetMask("region");
	public static readonly int itemsMask = LayerMask.GetMask(new string[]{"items","items2","bodyPartItems"});
	public static readonly int itemsOnlyMask = LayerMask.GetMask(new string[]{"items","items2"});
	public static readonly int characterMovementMask = LayerMask.GetMask("characterMovement");
	public static readonly int wallsMask = LayerMask.GetMask(new string[]{"wallsStatic","wallsDynamic","wallsStaticForLoliOnly"});
	public static readonly int wallsStaticForCharactersMask = LayerMask.GetMask("wallsStaticForCharacters");
	public static readonly int wallsStaticForLoliOnlyMask = LayerMask.GetMask("wallsStaticForLoliOnly");
	public static readonly float speedToAnim = 1.4f;
	public static readonly int uiMask = LayerMask.GetMask("UI");
	public static readonly int offscreenSpecialMask = LayerMask.GetMask("offscreenSpecial");
	public static readonly int itemDetectorMask = LayerMask.GetMask("itemDetector");

	public static readonly int waterLayer = 4;
	public static readonly int uiLayer = 5;
	public static readonly int bodyPartItemsLayer = 8;
	public static readonly int playerMovementLayer = 9;
	public static readonly int noneLayer = 10;				//no collisions
	public static readonly int itemsLayer = 12;
	public static readonly int wallsStatic = 11;
	public static readonly int heldItemsLayer = 13;
	public static readonly int itemsLayer2 = 14;
	public static readonly int regionLayer = 15;
	public static readonly int offscreenSpecialLayer = 19;
	public static readonly int itemDetectorLayer = 21;
	public static readonly int wallsStaticForCharactersLayer = 20;
}

}