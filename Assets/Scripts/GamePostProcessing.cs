using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Rendering;

namespace viva{

//TODO: Do this a better way?
public class GamePostProcessing : MonoBehaviour {

    private delegate IEnumerator ScreenAnimCoroutine();

    public enum Effect{
        HURT,
        SPLASH
    }

    [SerializeField]
    private Material[] screenMaterials;
	[SerializeField]
	private Material playerHurtMat;
    [SerializeField]
    private Material playerSplashMat;
    [SerializeField]
     private Material cloudCameraMaterial;
    private int alphaID = Shader.PropertyToID("_Alpha");
    private CommandBuffer renderTextureCommands;
    private RenderTexture m_screenTexture = null;
    public RenderTexture screenTexture { get{ return m_screenTexture; } }
	private Coroutine activeEffectCoroutine;
	private Dictionary<MeshRenderer[],CommandBuffer> postProcessRenderers = new Dictionary<MeshRenderer[],CommandBuffer>();
    private Material screenMaterial = null;
    private CameraEvent initialEvent = CameraEvent.BeforeFinalPass;
    private bool m_usingUnderwater = false;
    public bool usingUnderwater { get{ return m_usingUnderwater; } }
    private bool screenBlitActive = false;
    private Set<Material> postProcessingQueue = new Set<Material>();
    private float lastScreenEffect = 0.0f;



    private void Start(){
        InitRenderTextureCommands( Screen.width/2, Screen.height/2 );
        cloudCameraMaterial.SetTexture( "_CloudsRT", GameDirector.instance.GetCloudRT() );
        screenMaterial = cloudCameraMaterial;
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination){
        cloudCameraMaterial.SetTexture( "_CloudsRT", GameDirector.instance.GetCloudRT() );
        if( screenMaterial != null ){
            Graphics.Blit( source, destination, screenMaterial );
        }else{
            Graphics.Blit( source, destination );
        }
	}

    private void InitRenderTextureCommands( int width, int height ){
        m_screenTexture = new RenderTexture( width, height, 16, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear );
        renderTextureCommands = new CommandBuffer();
        renderTextureCommands.name = "POST_PROCESSING_TEXTURE";
        renderTextureCommands.Blit( BuiltinRenderTextureType.CameraTarget, m_screenTexture );
    }
    
    public void DisplayScreenEffect( Effect effect ){
        if( usingUnderwater ){
            return;
        }
        
        if( Time.time-lastScreenEffect < 4.0f ){
            return;
        }
        lastScreenEffect = Time.time;

        StopActiveEffectCoroutine();
        switch( effect ){
        case Effect.HURT:
            AddToQueue( playerHurtMat );
            activeEffectCoroutine = GameDirector.instance.StartCoroutine( AnimateHurtMaterial() );
            break;
        case Effect.SPLASH:
            AddToQueue( playerSplashMat );
            activeEffectCoroutine = GameDirector.instance.StartCoroutine( AnimateSplashMaterial() );
            break;
        }
    }

    public void AddToQueue( Material material ){
        postProcessingQueue.Add( material );
        UpdatePostProcessUsage();
        material.mainTexture = screenTexture;
        UpdatePostProcessingQueue();
    }

    public void RemoveFromQueue( Material material ){
        postProcessingQueue.Remove( material );
        UpdatePostProcessUsage();
        UpdatePostProcessingQueue();
    }

    private void UpdatePostProcessingQueue(){
        if( postProcessingQueue.Count > 0 ){
            screenMaterial = postProcessingQueue.objects[ postProcessingQueue.objects.Count-1 ];
        }else{
            screenMaterial = null;
        }
    }

    private void StopActiveEffectCoroutine(){
        if( activeEffectCoroutine != null ){
            GameDirector.instance.StopCoroutine( activeEffectCoroutine );
        }
        activeEffectCoroutine = null;
    }

    public void EnableUnderwaterEffect(){
        AddToQueue( screenMaterials[0] );
        m_usingUnderwater = true;
    }

    public void DisableUnderwaterEffect(){
        screenMaterial = cloudCameraMaterial;
        RemoveFromQueue( screenMaterials[0] );
        m_usingUnderwater = false;
        DisplayScreenEffect( Effect.SPLASH );
    }

    public void DisableGhostEffect(){
        screenMaterial = cloudCameraMaterial;
    }

    public bool IncreaseScreenTextureUse( MeshRenderer[] targetMRs, Material targetMaterial ){
        if( targetMaterial == null || targetMRs == null ){
            Debug.LogError("ERROR null screen texture use args");
            return false;
        }
        if( postProcessRenderers.ContainsKey( targetMRs ) ){
            return false;
        }
        var cmdBuffer = new CommandBuffer();
        postProcessRenderers.Add( targetMRs, cmdBuffer );
        UpdatePostProcessUsage();

        foreach( MeshRenderer mr in targetMRs ){
            cmdBuffer.DrawRenderer( mr, targetMaterial );
        }
        targetMaterial.mainTexture = screenTexture;
        GameDirector.instance.mainCamera.AddCommandBuffer( CameraEvent.BeforeForwardAlpha, cmdBuffer );
        return true;
    }

    public bool DecreaseScreenTextureUse( MeshRenderer[] targetMRs ){
        
        CommandBuffer cmdBuffer;
        if( !postProcessRenderers.TryGetValue( targetMRs, out cmdBuffer ) ){
            return false;
        }
        GameDirector.instance.mainCamera.RemoveCommandBuffer( CameraEvent.BeforeForwardAlpha, cmdBuffer );
        cmdBuffer.Dispose();
        postProcessRenderers.Remove( targetMRs );
        UpdatePostProcessUsage();
        return true;
    }

    private void UpdatePostProcessUsage(){
        bool shouldBeActive = postProcessingQueue.Count > 0 || activeEffectCoroutine != null;
        if( shouldBeActive == screenBlitActive ){
            return;
        }
        screenBlitActive = shouldBeActive;
        if( screenBlitActive ){
            GameDirector.instance.mainCamera.AddCommandBuffer( CameraEvent.BeforeForwardAlpha, renderTextureCommands );
        }else{
            GameDirector.instance.mainCamera.RemoveCommandBuffer( CameraEvent.BeforeForwardAlpha, renderTextureCommands );
            screenMaterial = cloudCameraMaterial;
        }
    }
    
	private IEnumerator AnimateSplashMaterial(){

        screenMaterial = playerSplashMat;
        float timerLength = 2.0f;
        float timer = 0.0f;
        while( timer < timerLength ){
            float ratio = 1.0f-timer/timerLength;
            screenMaterial.SetFloat( alphaID, ratio*ratio*ratio );
            timer += Time.deltaTime;
            yield return null;
        }	
        activeEffectCoroutine = null;       
        UpdatePostProcessUsage();    
        screenMaterial = cloudCameraMaterial;     
    }

	private IEnumerator AnimateHurtMaterial(){

        screenMaterial = playerHurtMat;
        float timerLength = 0.1f;
        float timer = 0.0f;
        while( timer < timerLength ){
            float ratio = 1.0f-timer/timerLength;
            playerHurtMat.SetFloat( alphaID, 1.0f-ratio*ratio );
            timer += Time.deltaTime;
            yield return null;
        }
        timerLength = 0.3f;
        timer = timerLength;
        while( timer > 0.0f ){
            playerHurtMat.SetFloat( alphaID, Tools.EaseInOutQuad( timer/timerLength ) );
            timer -= Time.deltaTime;
            yield return null;
        }
		activeEffectCoroutine = null;
        UpdatePostProcessUsage();
        screenMaterial = cloudCameraMaterial;
    }
}


}