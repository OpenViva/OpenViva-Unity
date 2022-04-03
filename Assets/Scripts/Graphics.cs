using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace viva{

public class Graphics : MonoBehaviour
{
    [SerializeField]
    private Text qualityText;
    [SerializeField]
    private Text shadowsText;
    [SerializeField]
    private Text aliasingText;


    public void ShiftQuality( int d ){
        VivaSettings.main.qualityLevel += d;
        VivaSettings.main.Apply();
        qualityText.text = GetQualityText();
    }

    public void ShiftShadows( int d ){
        VivaSettings.main.shadowLevel += d;
        VivaSettings.main.Apply();
        shadowsText.text = GetShadowsText();
    }

    public void ShiftAliasing( int d ){
        VivaSettings.main.aliasingLevel += d;
        VivaSettings.main.Apply();
        aliasingText.text = GetAliasingText();
    }

    private void OnEnable(){
        qualityText.text = GetQualityText();
        shadowsText.text = GetShadowsText();
        aliasingText.text = GetAliasingText();
    }

    public string GetQualityText(){
        switch( VivaSettings.main.qualityLevel ){
        default:
            return "Quality\n(LOW)";
        case 1:
            return "Quality\n(MEDIUM)";
        case 2:
            return "Quality\n(HIGH)";
        }
    }

    public string GetShadowsText(){
        switch( VivaSettings.main.shadowLevel ){
        default:
            return "Shadows\noff";
        case 1:
            return "Shadows\n(LOW)";
        case 2:
            return "Shadows\n(MEDIUM)";
        case 3:
            return "Shadows\n(HIGH)";
        case 4:
            return "Shadows\n(VERY HIGH)";
        case 5:
            return "Shadows\n(ULTRA)";
        }
    }

    public string GetAliasingText(){
        switch( VivaSettings.main.aliasingLevel ){
        default:
            return "Anti-Aliasing\noff";
        case 1:
            return "Anti-Aliasing\n(MSAA 2x)";
        case 2:
            return "Anti-Aliasing\n(MSAA 4x)";
        case 3:
            return "Anti-Aliasing\n(MSAA 8x)";
        }
    }
}

}