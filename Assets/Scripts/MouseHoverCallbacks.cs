using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


namespace viva{


public class MouseHoverCallbacks : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler{

    public GenericCallback onEnter;
    public GenericCallback onExit;
    public GenericCallback whileHovering;
    private Coroutine hoverCoroutine;


    private void StopHoverCoroutine(){
        if( hoverCoroutine != null ){
            StopCoroutine( hoverCoroutine );
            hoverCoroutine = null;
        }
    }

    private void OnDisable(){
        StopHoverCoroutine();
    }

    public void OnPointerEnter(PointerEventData pointerEventData){
        onEnter?.Invoke();

        if( whileHovering != null ){
            StopHoverCoroutine();
            hoverCoroutine = StartCoroutine( HoverHandle() );
        }
    }
    
    public void OnPointerExit(PointerEventData pointerEventData){
        onExit?.Invoke();
        StopHoverCoroutine();
    }

    private IEnumerator HoverHandle(){
        while( true ){
            whileHovering?.Invoke();
            yield return null;
        }
    }
}

}