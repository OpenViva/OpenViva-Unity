using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using UnityEngine.UI;


namespace viva{


public partial class ModelCustomizer : UITabMenu{

    private static readonly float PRECISION_SCALE = 20.0f;

    public enum VivaModelProperty{
        CENTER_X,
        CENTER_Y,
        RADIUS,
        BONE_NAME,
        CAN_RELAX,
        USE_GRAVITY,
        DAMPING,
        ELASTICITY,
        STIFFNESS,
        COLLISION_HEAD,
        COLLISION_BACK,
        VOICE
    }

    [Header("Tweak Tab")]
    [SerializeField]
    private GameObject tweakTab = null;
    [SerializeField]
    private Button tweakTabButton = null;
    [SerializeField]
    private Image tweakTabSubImage1 = null;
    [SerializeField]
    private Image tweakTabSubImage2 = null;
    [SerializeField]
    private RectTransform lastSubTweakTab = null;
    [SerializeField]
    private RectTransform colorsContainer = null;

    [Header("Eyes")]
    [SerializeField]
    private Text centerXProperty = null;
    [SerializeField]
    private Text centerYProperty = null;
    [SerializeField]
    private Text radiusProperty = null;

    [Header("Bones")]
    [SerializeField]
    private Text boneNameProperty = null;
    [SerializeField]
    private Text boneIndexText = null;
    [SerializeField]
    private Button canRelaxProperty = null;
    [SerializeField]
    private Button useGravityProperty = null;
    [SerializeField]
    private Text dampingProperty = null;
    [SerializeField]
    private Text elasticityProperty = null;
    [SerializeField]
    private Text stiffnessProperty = null;
    [SerializeField]
    private Button headCollisionProperty = null;
    [SerializeField]
    private Button backCollisionProperty = null;

    [SerializeField]
    private Color onColor;
    [SerializeField]
    private Color offColor;

    [SerializeField]
    private Text voiceText;
 
    private int currentDynamicBoneIndex = 0;
    private RectTransform lastSubTab = null;
    private Coroutine testHairMotionCoroutine = null;
    private Color activeSkinColor = Color.grey;

    public void clickSetVoice( int index ){
        modelPreviewer.modelDefault.headModel.voiceIndex = (byte)index;
        modelPreviewer.modelDefault.RebuildVoice();
        UpdateProperty( VivaModelProperty.VOICE );
        //play a sample
        GameDirector.instance.PlayGlobalSound( modelPreviewer.modelDefault.GetNextVoiceLine( Loli.VoiceLine.STARTLE_SHORT ) );
    }

    private void SetImageHeight( Image target, float height ){
        Vector2 sizeDelta = target.rectTransform.sizeDelta;
        sizeDelta.y = height;
        target.rectTransform.sizeDelta = sizeDelta;
    }

    public void clickSetTweakTabFocus( RectTransform subTab ){
        if( lastSubTab != null ){
            lastSubTab.gameObject.SetActive( false );
            StopHairMotionTest();
        }
        subTab.gameObject.SetActive( true );
        lastSubTab = subTab;

        SetImageHeight( tweakTabSubImage1, subTab.sizeDelta.y+8 );
        SetImageHeight( tweakTabSubImage2, tweakTabSubImage1.rectTransform.sizeDelta.y+88+128 );
    }

    private float ToGrayscale( Color32 c ){
        float r = c.r/255.0f;
        float g = c.g/255.0f;
        float b = c.b/255.0f;
        return Vector2.Dot( new Vector3( r, g, b ), new Vector3( 0.278f, 0.598f, 0.114f ) );
    }

    public void clickInitializeVoiceSubTab(){
        modelPreviewer.SetPreviewMode( ModelPreviewViewport.PreviewMode.NONE );
        UpdateProperty( VivaModelProperty.VOICE );
    }

    public void clickInitializeSkinSubTab(){
        modelPreviewer.SetPreviewMode( ModelPreviewViewport.PreviewMode.NONE );
        activeSkinColor = modelPreviewer.modelDefault.headModel.skinColor;

        Texture2D headTexture = modelPreviewer.modelDefault.headModel.texture;
        Color32[] textureData = headTexture.GetPixels32();
        //build random table of colors
        int sampleWidth = 12;
        Color32[] colorTable = new Color32[sampleWidth*sampleWidth];
        float skipX = headTexture.width/(sampleWidth+1);
        float skipY = ( headTexture.height/(sampleWidth+1) )*headTexture.width;
        float index = skipX+skipY;
        int accumulateIndex = 0;
        for( int i=0; i<sampleWidth; i++ ){
            for( int j=0; j<sampleWidth; j++ ){
                Color32 sample = textureData[ (int)index ];
                if( sample.a >= 255 ){
                    colorTable[ accumulateIndex++ ] = sample;
                }
                index += skipX;
            }
            index += skipY;
        }
        //filter unique colors
        List<Color32> uniqueColors = new List<Color32>();
        int uniqueCount = 0;
        for( int i=0; i<accumulateIndex; i++ ){
            Color32 sample = colorTable[i];
            bool newUnique = true;
            for( int j=0; j<uniqueCount; j++ ){
                if( Tools.ColorDistance( uniqueColors[j], sample ) < 8.0f ){
                    newUnique = false;
                    break;
                }
            }
            if( newUnique ){
                uniqueColors.Add( sample );
                if( ++uniqueCount >= colorsContainer.childCount ){
                    break;
                }
            }
        }
        //sort based on brightness
        uniqueColors.Sort( (a,b)=>ToGrayscale(b).CompareTo( ToGrayscale(a) ) );
        int colorSlotIndex = 0;
        for( ; colorSlotIndex<uniqueCount; colorSlotIndex++ ){
            Transform child = colorsContainer.GetChild( colorSlotIndex );
            child.GetComponent<Image>().color = uniqueColors[ colorSlotIndex ];
            child.gameObject.SetActive( true );

            Button button = child.GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            int currSlotIndex = colorSlotIndex;
            button.onClick.AddListener( delegate{ SetActiveSkinColor( uniqueColors[ currSlotIndex ] ); } );
        }
        for( ; colorSlotIndex < colorsContainer.childCount; colorSlotIndex++ ){
            colorsContainer.GetChild( colorSlotIndex ).gameObject.SetActive( false );
        }
    }

    public void clickInitializeEyesSubTab(){
        modelPreviewer.SetPreviewMode( ModelPreviewViewport.PreviewMode.EYES );
    }

    public void clickInitializeBoneSubTab(){
        modelPreviewer.SetPreviewMode( ModelPreviewViewport.PreviewMode.BONES );
        UpdateBoneSelectionText();
    }  

    public void clickInitializePoseSubTab(){
        modelPreviewer.SetPreviewMode( ModelPreviewViewport.PreviewMode.POSE );
    }
    private void SetActiveSkinColor( Color color ){

        activeSkinColor = color;
        modelPreviewer.modelDefault.headModel.skinColor = activeSkinColor; 
        modelPreviewer.modelDefault.SetSkinColor( activeSkinColor );
    }

    public void clickIncreaseBrightness( float amount ){
        activeSkinColor.r += amount;
        activeSkinColor.g += amount;
        activeSkinColor.b += amount;
        SetActiveSkinColor( activeSkinColor );
    }

    public void clickToggleCanRelax(){
        if( modelPreviewer.modelDefault == null ){
            return;
        }
        modelPreviewer.modelDefault.headModel.dynamicBoneInfos[ currentDynamicBoneIndex ].canRelax = !modelPreviewer.modelDefault.headModel.dynamicBoneInfos[ currentDynamicBoneIndex ].canRelax;
        UpdateProperty( VivaModelProperty.CAN_RELAX );
    }

    public void ReloadCurrentDynamicBone(){
        VivaModel.DynamicBoneInfo info = modelPreviewer.modelDefault.headModel.dynamicBoneInfos[ currentDynamicBoneIndex ];
        string rootName = modelPreviewer.modelDefault.headModel.dynamicBoneInfos[ currentDynamicBoneIndex ].rootBone;
        Transform bone = Tools.SearchTransformFamily( modelPreviewer.modelDefault.bodyArmature, rootName );
        if( bone == null ){
            Debug.LogError("ERROR Could not find Dynamic Bone parent!");
            return;
        }
        modelPreviewer.modelDefault.ApplyDynamicBoneInfo( info, bone.GetComponent<DynamicBone>() );
    }

    public void clickToggleUseGravity(){
        if( modelPreviewer.modelDefault == null ){
            return;
        }
        modelPreviewer.modelDefault.headModel.dynamicBoneInfos[ currentDynamicBoneIndex ].useGravity = !modelPreviewer.modelDefault.headModel.dynamicBoneInfos[ currentDynamicBoneIndex ].useGravity;
        UpdateProperty( VivaModelProperty.USE_GRAVITY );
        ReloadCurrentDynamicBone();
        
    }

    public void clickIncreaseDamping( float amount ){
        ChangeProperty( ref modelPreviewer.modelDefault.headModel.dynamicBoneInfos[ currentDynamicBoneIndex ].damping, 0.0f, 1.0f, amount );
        UpdateProperty( VivaModelProperty.DAMPING );
        ReloadCurrentDynamicBone();
    }

    public void clickIncreaseElasticity( float amount ){
        ChangeProperty( ref modelPreviewer.modelDefault.headModel.dynamicBoneInfos[ currentDynamicBoneIndex ].elasticity, 0.0f, 1.0f, amount );
        UpdateProperty( VivaModelProperty.ELASTICITY );
        ReloadCurrentDynamicBone();
    }

    public void clickIncreaseStiffness( float amount ){
        ChangeProperty( ref modelPreviewer.modelDefault.headModel.dynamicBoneInfos[ currentDynamicBoneIndex ].stiffness, 0.0f, 1.0f, amount );
        UpdateProperty( VivaModelProperty.STIFFNESS );
        ReloadCurrentDynamicBone();
    }

    private void ChangeProperty( ref float property, float min, float max, float change ){
        property = Mathf.Clamp( property+change, min, max );
    }

    public void clickToggleCollisionHead(){
        if( modelPreviewer.modelDefault == null ){
            return;
        }
        if( modelPreviewer.modelDefault.headModel.headCollisionWorldSphere.w == 0.0f ){
            DisplayErrorWindow("Your model doesn't have a \""+modelPreviewer.modelDefault.headModel.name+"head collision\". Export your model with one!");
            return;
        }
        modelPreviewer.modelDefault.headModel.dynamicBoneInfos[ currentDynamicBoneIndex ].headCollision = !modelPreviewer.modelDefault.headModel.dynamicBoneInfos[ currentDynamicBoneIndex ].headCollision;
        UpdateProperty( VivaModelProperty.COLLISION_HEAD );
        ReloadCurrentDynamicBone();
    }

    public void clickToggleCollisionBack(){
        if( modelPreviewer.modelDefault == null ){
            return;
        }
        modelPreviewer.modelDefault.headModel.dynamicBoneInfos[ currentDynamicBoneIndex ].collisionBack = !modelPreviewer.modelDefault.headModel.dynamicBoneInfos[ currentDynamicBoneIndex ].collisionBack;
        UpdateProperty( VivaModelProperty.COLLISION_BACK );
        ReloadCurrentDynamicBone();
    }

    public void clickNextBoneChain(){
        currentDynamicBoneIndex = Mathf.Min( currentDynamicBoneIndex+1, modelPreviewer.modelDefault.headModel.dynamicBoneInfos.Count-1 );
        UpdateAllProperties();
        UpdateBoneSelectionText();
    }

    public void clickPrevBoneChain(){
        currentDynamicBoneIndex = Mathf.Max( currentDynamicBoneIndex-1, 0 );
        UpdateAllProperties();
        UpdateBoneSelectionText();
    }

    private void UpdateBoneSelectionText(){
        boneIndexText.text = ""+(currentDynamicBoneIndex+1)+"/"+modelPreviewer.modelDefault.headModel.dynamicBoneInfos.Count;

        //create an indicator for every transform along the bone chain
        if( currentDynamicBoneIndex > modelPreviewer.modelDefault.headModel.dynamicBoneInfos.Count ){
            return;
        }
        VivaModel.DynamicBoneInfo targetInfo = modelPreviewer.modelDefault.headModel.dynamicBoneInfos[currentDynamicBoneIndex];
        modelPreviewer.StartHighlightingBoneChain( viva.Tools.SearchTransformFamily( modelPreviewer.modelDefault.transform, targetInfo.rootBone ) );
    }

    private void UpdateProperty( VivaModelProperty property ){
                
        switch( property ){
        case VivaModelProperty.BONE_NAME:
        case VivaModelProperty.CAN_RELAX:
        case VivaModelProperty.USE_GRAVITY:
        case VivaModelProperty.DAMPING:
        case VivaModelProperty.ELASTICITY:
        case VivaModelProperty.STIFFNESS:
        case VivaModelProperty.COLLISION_HEAD:
        case VivaModelProperty.COLLISION_BACK:
            if( currentDynamicBoneIndex >= modelPreviewer.modelDefault.headModel.dynamicBoneInfos.Count ){
                return; //do not update
            }
            break;
        }

        switch( property ){
        case VivaModelProperty.CENTER_X:
            centerXProperty.text = "Distance "+Mathf.RoundToInt( modelPreviewer.modelDefault.headModel.pupilOffset.x*PRECISION_SCALE );
            break;
        case VivaModelProperty.CENTER_Y:
            centerYProperty.text = "Height "+Mathf.RoundToInt( modelPreviewer.modelDefault.headModel.pupilOffset.y*PRECISION_SCALE );
            break;
        case VivaModelProperty.RADIUS:
            radiusProperty.text = "Radius "+Mathf.RoundToInt( modelPreviewer.modelDefault.headModel.pupilSpanRadius*PRECISION_SCALE );
            break;
        case VivaModelProperty.BONE_NAME:
            boneNameProperty.text = modelPreviewer.modelDefault.headModel.dynamicBoneInfos[ currentDynamicBoneIndex ].rootBone;
            break;
        case VivaModelProperty.CAN_RELAX:
            SetPropertyToggleButtonColor( canRelaxProperty, modelPreviewer.modelDefault.headModel.dynamicBoneInfos[ currentDynamicBoneIndex ].canRelax );
            break;
        case VivaModelProperty.USE_GRAVITY:
            SetPropertyToggleButtonColor( useGravityProperty, modelPreviewer.modelDefault.headModel.dynamicBoneInfos[ currentDynamicBoneIndex ].useGravity );
            break;
        case VivaModelProperty.DAMPING:
            dampingProperty.text = "Damping "+Mathf.RoundToInt( modelPreviewer.modelDefault.headModel.dynamicBoneInfos[ currentDynamicBoneIndex ].damping*20.0f );
            break;
        case VivaModelProperty.ELASTICITY:
            elasticityProperty.text = "Elasticity "+Mathf.RoundToInt( modelPreviewer.modelDefault.headModel.dynamicBoneInfos[ currentDynamicBoneIndex ].elasticity*20.0f );
            break;
        case VivaModelProperty.STIFFNESS:
            stiffnessProperty.text = "Stiffness "+Mathf.RoundToInt( modelPreviewer.modelDefault.headModel.dynamicBoneInfos[ currentDynamicBoneIndex ].stiffness*20.0f );
            break;
        case VivaModelProperty.COLLISION_HEAD:
            SetPropertyToggleButtonColor( headCollisionProperty,  modelPreviewer.modelDefault.headModel.dynamicBoneInfos[ currentDynamicBoneIndex ].headCollision );
            break;
        case VivaModelProperty.COLLISION_BACK:
            SetPropertyToggleButtonColor( backCollisionProperty,  modelPreviewer.modelDefault.headModel.dynamicBoneInfos[ currentDynamicBoneIndex ].collisionBack );
            break;
        case VivaModelProperty.VOICE:
            switch( modelPreviewer.modelDefault.headModel.voiceIndex ){
            case 0:
            default:
                voiceText.text = "Voice: Original";
                break;
            case 1:
                voiceText.text = "Voice: Older Sister";
                break;
            case 2:
                voiceText.text = "Voice: Shy";
                break;
            }
            break;
        }
    }

    private void StopHairMotionTest(){
        if( testHairMotionCoroutine != null ){
            GameDirector.instance.StopCoroutine( testHairMotionCoroutine );
            testHairMotionCoroutine = null;
            modelPreviewer.modelDefault.transform.localPosition = Vector3.zero;
            modelPreviewer.modelDefault.ForceImmediatePose( Loli.Animation.STAND_HAPPY_IDLE2 );
            modelPreviewer.modelDefault.SetLookAtTarget( modelPreviewer.renderCamera.transform );
        }
    }

    private void StartHairMotionTest(){
        StopHairMotionTest();   //ensure coroutine is null
        testHairMotionCoroutine = GameDirector.instance.StartCoroutine( TestHairMotion() );
        modelPreviewer.modelDefault.ForceImmediatePose( Loli.Animation.STAND_HAPPY_IDLE1 );
        modelPreviewer.modelDefault.SetLookAtTarget( null );
    }

    private IEnumerator TestHairMotion(){

        while( true ){
            
            float time = Time.time*4.2f;
            modelPreviewer.modelDefault.transform.localPosition = new Vector3(
                Mathf.Sin( time ),
                Mathf.Max( 0.0f, Mathf.Cos( time*1.23f ) ),
                Mathf.Cos( time*0.8f )
            )*0.1f;

            yield return null;
        }
    }

    public void clickToggleTestHairMotion(){
        
        if( testHairMotionCoroutine != null ){
            StopHairMotionTest();
        }else{
            StartHairMotionTest();
        }
    }

    private void SetPropertyToggleButtonColor( Button button, bool on ){
        ColorBlock cb = button.colors;
        if( on ){
            cb.normalColor = onColor;
            cb.selectedColor = onColor;
            cb.pressedColor = onColor*2.0f;
        }else{
            cb.normalColor = offColor;
            cb.selectedColor = offColor;
            cb.pressedColor = offColor*2.0f;
        }
        button.colors = cb;
    }

    public void ClickIncreasePupilOffsetX( float amount ){
        ChangeProperty( ref modelPreviewer.modelDefault.headModel.pupilOffset.x, -1.0f, 1.0f, amount );
        UpdateProperty( VivaModelProperty.CENTER_X );
    }

    public void ClickIncreasePupilOffsetY( float amount ){
        ChangeProperty( ref modelPreviewer.modelDefault.headModel.pupilOffset.y, -1.0f, 1.0f, amount );
        UpdateProperty( VivaModelProperty.CENTER_Y );
    }

    public void ClickIncreasePupilRadius( float amount ){
        ChangeProperty( ref modelPreviewer.modelDefault.headModel.pupilSpanRadius, 0.0f, 0.5f, amount );
        UpdateProperty( VivaModelProperty.RADIUS );
    }

    private void InitializeTweakTab(){
        UpdateAllProperties();
        clickSetTweakTabFocus( lastSubTweakTab );
    }

    private void UpdateAllProperties(){
        for( int i=0; i<System.Enum.GetValues(typeof(VivaModelProperty)).Length; i++ ){
            UpdateProperty( (VivaModelProperty)i );
        }
    }

    private void ValidateAllowTweakTab(){

        bool allow;
        if( modelPreviewer.modelDefault == null ){
            allow = false;
        }else{
            allow = ValidateForTweaking( modelPreviewer.modelDefault.headModel );
        }
        if( tweakTabButton.interactable != allow ){
            tweakTabButton.interactable = allow;
        }
        Text text = tweakTabButton.transform.GetChild(0).GetComponent<Text>();
        if( allow ){
            text.color = Color.white;
            text.text = "Tweak";
        }else{
            text.color = Color.gray;
            text.text = "Incomplete Model";
        }
    }

    private bool ValidateForTweaking( VivaModel head ){
        if( head == null ){
            return false;
        }
        if( modelPreviewer.modelDefault.headModel.texture == null ){
            return false;
        }
        if( modelPreviewer.modelDefault.headModel.headpatWorldSphere.w == 0.0f ){
            return false;
        }
        if( head.mesh == null ){
            Debug.Log("[VIVA MODEL TWEAK TAB] mesh is null");
            return false;
        }
        return true;
    }
    
    
}


}