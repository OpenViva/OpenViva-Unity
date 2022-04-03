using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System;


namespace viva{


public class SceneSwitcher: MonoBehaviour{

    [SerializeField]
    public Text status;
    [SerializeField]
    private Image progressImage;
    [SerializeField]
    private MeshRenderer meshRenderer;

    public bool finished { get{ return fadeCoroutine==null; } }
    private Coroutine fadeCoroutine;
    private Coroutine progressCoroutine;

    private void Awake(){
        progressImage.material.SetFloat("_Progress",0);
    }

    public void FadeTo( float fadeStart, float fadeEnd ){
        if( fadeCoroutine != null ){
            StopCoroutine( fadeCoroutine );
        }
        fadeCoroutine = StartCoroutine( AnimateFadeCoroutine( fadeStart, fadeEnd ) );
    }

    public void SetProgress( float progEnd ){
        if( progressCoroutine != null ){
            StopCoroutine( progressCoroutine );
        }
        progressCoroutine = StartCoroutine( AnimateProgressCoroutine( progEnd ) );
    }

    private IEnumerator AnimateProgressCoroutine( float progEnd ){
        float progStart = progressImage.material.GetFloat("_Progress");
        float timer = 0;
        while( timer < 0.3f ){
            timer += Time.deltaTime;
            float progRatio = Mathf.LerpUnclamped( progStart, progEnd, Mathf.Clamp01( timer/0.3f ) );
            progressImage.material.SetFloat("_Progress",progRatio);
            yield return null;
        }
        progressCoroutine = null;
    }

    private IEnumerator AnimateFadeCoroutine( float fadeStart, float fadeEnd ){
        float timer = 0;
        while( timer < 1 ){
            timer += Time.deltaTime;
            float fade = Mathf.Clamp01( timer );
            float fadeRatio = Mathf.LerpUnclamped( fadeStart, fadeEnd, fade );
            meshRenderer.material.color = new Color( fadeRatio, 0, 0, fadeRatio );
            yield return null;
        }
        fadeCoroutine = null;
    }

    private void LateUpdate(){
        if( Camera.main == null ) return;
        transform.position = Camera.main.transform.position;
        transform.rotation = Quaternion.LerpUnclamped( transform.rotation, Camera.main.transform.rotation, Time.deltaTime*16.0f );
    }
}

}