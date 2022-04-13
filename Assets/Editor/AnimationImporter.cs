using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System;

public class AnimationImporter: AssetPostprocessor{
	
	void OnPreprocessAnimation(){

		ModelImporter importer = assetImporter as ModelImporter;

		Avatar shinobuAvatar = null;
		AvatarMask shinobuMask = null;

		Avatar playerAvatar = null;

        ModelImporterClipAnimation[] animations = importer.defaultClipAnimations;
		bool valid = false;
        foreach (ModelImporterClipAnimation animation in animations){
		
			importer.materialImportMode = ModelImporterMaterialImportMode.None;
			if( assetPath.Contains("Animations/shinobu") ){
				Debug.Log("Processing "+assetPath);
				valid = true;

				if( shinobuMask == null ){
					shinobuMask = Resources.Load<AvatarMask>("Animations/shinobu/misc/loli_importMask");
					if( shinobuMask == null ){
						Debug.LogError("AvatarMask shinobu is null!");
						return;
					}
				}
				if( shinobuAvatar == null ){
					shinobuAvatar = Resources.Load<Animator>("Models/shinobu").avatar;
					if( shinobuAvatar == null ){
						Debug.LogError("Avatar shinobu is null!");
						return;
					}
				}
				
				importer.globalScale = 3.75f;
		
				importer.animationType = ModelImporterAnimationType.Human;
				importer.sourceAvatar = shinobuAvatar;

				animation.keepOriginalOrientation = true;
				animation.keepOriginalPositionXZ = true;
				animation.keepOriginalPositionY = true;
				animation.lockRootHeightY = true;
				animation.lockRootPositionXZ = true;
				animation.lockRootRotation = true;

				//setup AvatarMask so extra non-Humanoid ones get imported
				animation.maskType = ClipAnimationMaskType.CopyFromOther;
				animation.maskSource = shinobuMask;
			}else if( assetPath.Contains("Animations/player") ){

				if( playerAvatar == null ){
					playerAvatar = Resources.Load<Animator>("Models/player/player").avatar;
					if( playerAvatar == null ){
						Debug.LogError("Avatar player is null!");
						return;
					}
				}
				importer.globalScale = 3.75f;

				importer.animationType = ModelImporterAnimationType.Generic;
				importer.sourceAvatar = playerAvatar;
			}

			//  _nm stands for no mirror
			if( animation.name.Contains("_left") && !animation.name.Contains("_nm") ){
				animation.mirror = true;
			}
			if( animation.name.Contains("_loop") ){
				animation.loopTime = true;
			}
        }
		if( valid ){
    		importer.clipAnimations = animations;
		}
     }
}