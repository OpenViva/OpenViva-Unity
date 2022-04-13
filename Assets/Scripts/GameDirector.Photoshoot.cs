using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

namespace viva{


public partial class GameDirector : MonoBehaviour {

    [SerializeField]
    private Camera photoshootCamera;
    [SerializeField]
    private GameObject photoshootStage;
    [SerializeField]
    private Transform photoshootSun;


    public class PhotoshootRequest{
        
        public Texture2D texture;
        public Vector2Int resolution;
        public CameraPose cameraPose;
        public Texture2D background;
        public Loli.Animation pose;

        public PhotoshootRequest( Vector2Int _resolution, CameraPose _cameraPose, Texture2D _background, Loli.Animation _pose ){
            resolution = _resolution;
            cameraPose = _cameraPose;
            background = _background;
            pose = _pose;
        }
    }

    public IEnumerator RenderPhotoshoot( Loli loli, PhotoshootRequest request ){

        photoshootCamera.transform.localPosition = request.cameraPose.position;
        photoshootCamera.transform.localEulerAngles = request.cameraPose.rotation;
        photoshootCamera.fieldOfView = request.cameraPose.fov;

        Vector3 oldPosition = loli.transform.position;
        Quaternion oldRotation = loli.transform.rotation;
        Quaternion oldSunRotation = skyDirector.sun.transform.rotation;

        //must make sure Shinobu is in a proper behavior to override animations!
        //frezee Shinobu in place without logic momentarily to simulate clothing and hair
        loli.Teleport( photoshootStage.transform.position, photoshootStage.transform.rotation );
        characters.Remove( loli );
        loli.puppetMaster.SetEnableGravity( false );
        
        photoshootStage.SetActive( true );
        
        loli.ResetEyeUniforms();
        loli.ForceImmediatePose( request.pose );
        yield return new WaitForSeconds( 0.5f );
        GameDirector.skyDirector.OverrideDayNightCycleLighting( GameDirector.skyDirector.defaultDayNightPhase, photoshootSun.rotation );
        GameDirector.skyDirector.sun.color = Color.black;
        photoshootCamera.GetComponent<CameraRenderMaterial>().getEffectMat().SetTexture("_Background",request.background);
        request.texture = RenderPhotoshootTexture( photoshootCamera, request.resolution );
        yield return new WaitForSeconds( 0.5f );
        GameDirector.skyDirector.RestoreDayNightCycleLighting();

        //restore unfrozen settings
        characters.Add( loli );
        loli.puppetMaster.SetEnableGravity( true );
        loli.Teleport( oldPosition, oldRotation );
        loli.ForceImmediatePose( loli.GetLastReturnableIdleAnimation() );

        photoshootStage.SetActive( false );
    }

    private Texture2D RenderPhotoshootTexture( Camera camera, Vector2Int resolution ){
		RenderTexture renderTexture = new RenderTexture( resolution.x, resolution.y, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB );
		camera.targetTexture = renderTexture;

		RenderTexture currentRT = RenderTexture.active;
		RenderTexture.active = camera.targetTexture;
        camera.Render();

        //render native size 1x
		Texture2D newTexture = new Texture2D( Steganography.PACK_SIZE, Steganography.CARD_HEIGHT, TextureFormat.RGB24, false, true );
        newTexture.ReadPixels(new Rect(0, 0, Steganography.PACK_SIZE, Steganography.CARD_HEIGHT), 0, 0, false );
        newTexture.Apply( false, false );

		RenderTexture.active = currentRT;
        renderTexture.Release();    //destroy RT memory

		return newTexture;
	}

}

}