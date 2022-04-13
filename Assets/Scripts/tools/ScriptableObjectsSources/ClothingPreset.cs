using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace viva{


[System.Serializable]
[CreateAssetMenu(fileName = "Clothing Preset", menuName = "Clothing Preset", order = 1)]
public class ClothingPreset: ScriptableObject{
	
	public enum WearType{
		TORSO,
		HANDS,
		SKIRT,
		FOOTWEAR,
		SOCKS,
		GROIN,
		LEGWEAR,
		HEAD_ACCESSORY,
		FULL_BODY,
		FACEWEAR,
	};
	public enum Attribute {
		OBJECT,
		DEFORM,
		CLOTH
	};

	public GameObject prefab;
	public WearType wearType;
	public Attribute attribute;
	public bool enable = true;
	public Sprite preview;
	public string clothePieceName;
	public CameraPose photoshootPose;
}

}