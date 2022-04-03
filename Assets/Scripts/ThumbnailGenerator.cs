using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;



namespace viva{

public delegate void Texture2DCallback( Texture2D tex );

public class ThumbnailGenerator : MonoBehaviour{

    [SerializeField]
    private new Camera camera;
    private RenderTexture staticRenderTexture;
    private RenderTexture animatedRenderTexture;
    [SerializeField]
    public AnimationPlayer animationPlayer;
    [SerializeField]
    private Light highlight;

    public static ThumbnailGenerator main;
    public Model lastSelectedModel = null;

    
    public void Awake(){
        main = this;
    }

    public void GenerateAnimationThumbnailTexture( Animation animation, Thumbnail targetThumbnail ){
        if( lastSelectedModel == null || lastSelectedModel.skinnedMeshRenderer == null ){
            return;
        }
        if( !PrepareModelForRender( lastSelectedModel, out Bounds bounds1, out int oldLayer, out GameObject container ) ){
            return;
        }
        if( targetThumbnail.texture == null ) targetThumbnail.texture = new Texture2D( 1024, 1024, TextureFormat.ARGB32, false, true );
        targetThumbnail.animatedFrameWidth = 8;
        targetThumbnail.animatedDuration = animation.duration;

        StartCoroutine( ThumbnailAnimatedCoroutine( lastSelectedModel, animation, targetThumbnail, delegate{
            container.layer = oldLayer;
        } ) );
    }

    public void GenerateModelThumbnailTexture( Model model, Thumbnail targetThumbnail ){
        if( !PrepareModelForRender( model, out Bounds bounds, out int oldLayer, out GameObject container ) ){
            return;
        }
        if( targetThumbnail.texture == null ) targetThumbnail.texture = new Texture2D( 256, 256, TextureFormat.ARGB32, false, true );
        targetThumbnail.texture.name = model.name;
        StartCoroutine( ThumbnailStaticCoroutine( bounds, model, targetThumbnail.texture, delegate{
            if( container ) container.layer = oldLayer;
            targetThumbnail.onThumbnailChange.Invoke( model );
        } ) );
    }

    public void GenerateCameraTexture( Texture2DCallback onFinished ){
        if( onFinished == null ) return;
        StartCoroutine( ThumbnailCameraCoroutine( onFinished ) );
    }

    private bool PrepareModelForRender( Model model, out Bounds bounds, out int oldLayer, out GameObject container ){
        
        var renderer = model.renderer;
        if( !renderer ){
            bounds = new Bounds();
            oldLayer = 0;
            container = null;
            return false;
        }

        bounds = renderer.bounds;
        container = renderer.gameObject;
        
        oldLayer = container.layer;
        container.layer = LayerMask.NameToLayer("cameras");
        model.rootTransform.gameObject.SetActive( true );

        return true;
    }

    private IEnumerator ThumbnailStaticCoroutine( Bounds bounds, Model model, Texture2D targetTexture, GenericCallback onRestore ){
        
        if( model.rootTransform == null ){
            Debug.LogError("Could not generate thumbnail");
            onRestore?.Invoke();
            yield break;
        }
        if( staticRenderTexture == null ){
		    staticRenderTexture = new RenderTexture( 256, 256, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear );
        }
        PrepareCameraAndTargeRoot( bounds );
        yield return new WaitForEndOfFrame();
        var oldSunRotation = AmbienceManager.main.sunLight.transform.rotation;
        AmbienceManager.main.sunLight.transform.rotation = Quaternion.Euler( 140, 45, 0 );
        RenderCameraTexture( staticRenderTexture, targetTexture, 0, 0, true );
        AmbienceManager.main.sunLight.transform.rotation = oldSunRotation;
        targetTexture.Apply();

        onRestore?.Invoke();
    }

    private IEnumerator ThumbnailCameraCoroutine( Texture2DCallback onFinished ){
        var targetTexture = new Texture2D( 256, 256, TextureFormat.ARGB32, false, true );

        if( staticRenderTexture == null ){
		    staticRenderTexture = new RenderTexture( 256, 256, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear );
        }

        camera.transform.position = Camera.main.transform.position;
        camera.transform.rotation = Camera.main.transform.rotation;

        yield return new WaitForEndOfFrame();
        RenderCameraTexture( staticRenderTexture, targetTexture, 0, 0, false );
        targetTexture.Apply();

        onFinished.Invoke( targetTexture );
    }

    public void AnimateModel( Model model, Animation targetAnim ){
        
        if( model == null || targetAnim == null ) return;
        
        var animationLayer = new AnimationLayer( animationPlayer, null );
        if( model.bipedProfile != null ){
            animationLayer.BindForBiped( model );
        }else{
            animationLayer.BindForAnimal( model.skinnedMeshRenderer );
        }
        animationPlayer.BindAnimationLayer( animationLayer, model.skinnedMeshRenderer );

        var state = new AnimationSingle( targetAnim, animationLayer, model.deltaTransformBindHash, true );
        animationPlayer.Stop();
        animationPlayer._InternalPlay( state );
        
        animationPlayer.onModifyAnimation = delegate{
            if( model.bipedProfile != null ){
                model.ZeroOutDeltaTransform();
                model.ApplySpineTPoseDeltas();
            }
        };
    }
    public void StopAnimation(){
        if( animationPlayer != null ){
            animationPlayer.Stop();
        }
    }
    private IEnumerator ThumbnailAnimatedCoroutine( Model model, Animation targetAnim, Thumbnail thumbnail, GenericCallback onRestore ){
        
        if( animatedRenderTexture == null ){
            int size = thumbnail.texture.width/thumbnail.animatedFrameWidth;
		    animatedRenderTexture = new RenderTexture( size, size, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear );
        }
        PrepareCameraAndTargeRoot( lastSelectedModel.skinnedMeshRenderer.bounds );

        AnimateModel( model, targetAnim );
        int frameCount = thumbnail.animatedFrameWidth*thumbnail.animatedFrameWidth;
        float normSlice = 1.0f/frameCount;

        int frames = 0;
        int x = 0;
        int y = 0;
        animationPlayer.onAnimate = delegate{
            if( frames < frameCount ){
                frames++;
                RenderCameraTexture( animatedRenderTexture, thumbnail.texture, x, y, true );
                thumbnail.texture.Apply();

                x += animatedRenderTexture.width;
                if( x >= thumbnail.texture.width ){
                    x = 0;
                    y += animatedRenderTexture.height;
                }
            }
        };
        while( frames < frameCount ){
            yield return null;
        }
		thumbnail.texture.Compress(false);

        onRestore?.Invoke();
    }

    private void PrepareCameraAndTargeRoot( Bounds bounds ){
        Vector3 viewDir = new Vector3( -1.0f, 1.0f, 2.0f ).normalized;
        camera.transform.position = bounds.center+viewDir*bounds.extents.magnitude*1.5f;
        camera.transform.rotation = Quaternion.LookRotation( bounds.center-camera.transform.position, Vector3.up );
    }
    
	private void RenderCameraTexture( RenderTexture renderTexture, Texture2D targetTexture, int x, int y, bool isolate ){
		camera.targetTexture = renderTexture;

        var hdCamData = camera.GetComponent<HDAdditionalCameraData>();
		RenderTexture currentRT = RenderTexture.active;
		RenderTexture.active = camera.targetTexture;
        camera.gameObject.SetActive( true );
        highlight.gameObject.SetActive( true );

        camera.cullingMask = isolate ? 1<<11 : -1;
        camera.Render();

        highlight.gameObject.SetActive( false );
        camera.gameObject.SetActive( false );
        targetTexture.ReadPixels(new Rect( 0, 0, renderTexture.width, renderTexture.height), x, y);

		RenderTexture.active = currentRT;
	}
}

}