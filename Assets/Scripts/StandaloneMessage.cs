using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace viva{

public class StandaloneMessage : MonoBehaviour{

    public Canvas canvas;
    public Text text;
    public BoolReturnFunc onEnd;
    public bool faceCamera;
    private float animTimer;
    private float lifespan;


    private void OnEnable(){
        FaceCamera();
        text.transform.parent.localScale = Vector3.zero;
        animTimer = 0;

        VivaPlayer.user.onControlsChanged += OnControlsChanged;
        if( UI.main ){
            UI.main.onUIToggled += OnUIToggled;
            OnUIToggled( UI.main.isUIActive );
        }
        OnControlsChanged();
    }

    private void OnDisable(){
        VivaPlayer.user.onControlsChanged -= OnControlsChanged;
        if( UI.main ) UI.main.onUIToggled -= OnUIToggled;
    }

    private void OnUIToggled( bool on ){
        canvas.enabled = !on;
    }

    private void OnControlsChanged(){
        var frame = text.transform.parent as RectTransform;
        if( VivaPlayer.user.isUsingKeyboard ){
            frame.anchorMin = new Vector2(0.5f,1);
            frame.anchorMax = new Vector2(0.5f,1);
            frame.pivot = new Vector2(0.5f,1);
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
        }else{
            frame.anchorMin = Vector2.one*0.5f;
            frame.anchorMax = Vector2.one*0.5f;
            frame.pivot = Vector2.one*0.5f;
            canvas.renderMode = RenderMode.WorldSpace;
        }
    }

    private void FaceCamera(){
        if( VivaPlayer.user && VivaPlayer.user.camera ) transform.rotation = Quaternion.LookRotation( transform.position-VivaPlayer.user.camera.transform.position, Vector3.up );
    }

    private void FixedUpdate(){
        
        animTimer = Mathf.Clamp( animTimer+Time.deltaTime, 0f, 0.4f );
        text.transform.parent.localScale = Vector3.one*Tools.EaseOutQuad( animTimer );

        if( faceCamera ) FaceCamera();
        
        if( onEnd != null ){
            try{
                if( onEnd() ){
                    canvas.gameObject.SetActive( false );
                }
            }catch( System.Exception e ){
                Debug.LogError("Error when checking to close Message "+e.ToString());
                onEnd = null;
            }
        }else{
            lifespan += Time.deltaTime;
            if( !string.IsNullOrEmpty( text.text ) && lifespan > 0.15f*text.text.Length+3f ){
                canvas.gameObject.SetActive( false );
            }
        }
    }
    
}

}