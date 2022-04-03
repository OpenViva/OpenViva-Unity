using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace viva{

public class SpeechBubble : MonoBehaviour
{
    public static SpeechBubble Create( Vector3 position, float scale=1f ){
        var speechBubble = GameObject.Instantiate( BuiltInAssetManager.main.speechBubblePrefab, position, Quaternion.identity );
        speechBubble.transform.localScale = Vector3.one*0.003f*scale;
        return speechBubble;
    }

    public static SpeechBubble Create( Rigidbody trackTarget, float scale=1f ){
        
        SpeechBubble speechBubble = null;
        if( trackTarget ){
            speechBubble = trackTarget.transform.GetComponentInChildren<SpeechBubble>( true );
        }
        if( !speechBubble ){
            speechBubble = GameObject.Instantiate( BuiltInAssetManager.main.speechBubblePrefab );
        }
        speechBubble.trackTarget = trackTarget;
        speechBubble.transform.localScale = Vector3.one*0.003f*scale;
        speechBubble.transform.SetParent( trackTarget ? trackTarget.transform : null, true );
        return speechBubble;
    }
    
    [SerializeField]
    private float animationDuration = 0.25f;
    [SerializeField]
    private float heightOffset;
    [SerializeField]
    private Image speechImage;
    [SerializeField]
    private Image image1;
    [SerializeField]
    private Image image2;
    [SerializeField]
    private Image image3;

    private float animateTime = 0;
    public Rigidbody trackTarget;
    private float scale = 0.003f;


    public void OnEnable(){
        animateTime = Time.time;
    }

    private void Start(){
        scale = transform.localScale.x;
    }

    public void Display( Sprite sprite1, Sprite sprite2=null, Sprite sprite3=null ){
        image1.sprite = sprite1;
        image1.gameObject.SetActive( sprite1 );
        image2.sprite = sprite2;
        image2.gameObject.SetActive( sprite2 );
        image3.sprite = sprite3;
        image3.gameObject.SetActive( sprite3 );

        int imagesActive = System.Convert.ToInt32( sprite1 )+System.Convert.ToInt32( sprite2 )+System.Convert.ToInt32( sprite3 );

        var sizeDelta = speechImage.rectTransform.sizeDelta;
        if( imagesActive == 3 ){
            sizeDelta.x = 132;
        }else{
            sizeDelta.x = 100;
        }
        speechImage.rectTransform.sizeDelta = sizeDelta;
        speechImage.rectTransform.sizeDelta = new Vector3( Mathf.LerpUnclamped( 65, 100, (-1f+imagesActive)/2f ), 100 );

        gameObject.SetActive( false );
        gameObject.SetActive( true );
    }

    private void LateUpdate(){
        var timeDelta = Time.time-animateTime;
        var alpha = new Color( 1, 1, 1, 1.0f-Tools.GetClampedRatio( 3.0f, 3.5f, timeDelta ) );
        speechImage.color = alpha;
        image1.color = alpha;
        image2.color = alpha;
        image3.color = alpha;

        float animateAlpha = Mathf.Clamp01( ( timeDelta )/animationDuration );
        
        var animateScale = Vector3.one;
        animateScale.y = 0.5f+0.5f*Mathf.Sin( animateAlpha*Mathf.PI*0.8f )*animateAlpha;
        animateScale.x = ( animateScale.y+1.0f )/2.0f;
        transform.localScale = animateScale*scale;

        Vector3 flatForward;
        if( trackTarget ){
            transform.position = trackTarget.worldCenterOfMass+Vector3.up*0.2f;

            flatForward = Camera.main.transform.position-transform.position;
            transform.position += Vector3.ClampMagnitude( flatForward, 0.2f );
        }else{
            flatForward = Camera.main.transform.position-transform.position;
        }
        flatForward.y = 0.0001f;

        transform.rotation = Quaternion.LookRotation( -flatForward, Vector3.up );
    }
}

}