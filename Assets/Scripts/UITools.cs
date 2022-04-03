using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;


namespace viva{


public class UITools : MonoBehaviour{

    public static Vector2 GetScreenFitWindowPos( Vector2 screenPos, RectTransform target, out bool farX ){
        var size = target.rect.size;
        var parentRect = target.parent as RectTransform;
        var parentSize = parentRect.rect.size;
        RectTransformUtility.ScreenPointToLocalPointInRectangle( parentRect, screenPos, VivaPlayer.user.camera, out Vector2 localPos );
        if( localPos.x+size.x > parentSize.x/2 ){
            localPos.x -= size.x;
            farX = true;
        }else{
            farX = false;
        }
        if( localPos.y+size.y < 0 ){
            localPos.y += size.y;
        }
        return localPos;
    }
}


}