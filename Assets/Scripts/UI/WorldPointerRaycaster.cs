//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.UI;
//using UnityEngine.EventSystems;
//using UnityEngine.InputSystem;




//namespace viva{

        //ill have to implement this later or write one from scratch instead of using viva 0.9's worldpointer this is just here for reference

//public class WorldPointerRaycaster : MonoBehaviour{

//    private class RaycastHitData{
//        public readonly Graphic graphic;
//        public readonly Vector3 worldHitPosition;
//        public readonly Vector2 screenPosition;
//        public readonly float distance;

//        public RaycastHitData( Graphic graphic, Vector3 worldHitPosition, Vector2 screenPosition, float distance ){
//            this.graphic = graphic;
//            this.worldHitPosition = worldHitPosition;
//            this.screenPosition = screenPosition;
//            this.distance = distance;
//        }
//    }
//    private static readonly List<RaycastHitData> sortedGraphics = new List<RaycastHitData>();
//    private static GameObject lastObjHover = null;
//    private static GameObject lastObjDown = null;
    
//    [SerializeField]
//    private EventSystem eventSystem;
//    [SerializeField]
//    private Canvas canvas;
//    [SerializeField]
//    private Image canvasHighlight;
//    [SerializeField]
//    private LineRenderer lineRenderer;

//    private Coroutine highlightCoroutine;

//    private InputAction clickAction;
//    private Transform pointer;


//    private void Awake(){
//        if( canvasHighlight ) canvasHighlight.color = Color.black;
//        lineRenderer.positionCount = 2;
//    }

//    private void OnEnable(){
//        PlayerHandState hand;
//        Transform pointer;
//        InputAction clickAction;
//        if(GameDirector.instance.useRightHandForVRPointer){
//            hand = GameDirector.player.rightPlayerHandState;
//            clickAction = GameDirector.player.vivaControls.VRRightHand.Interact;
//        }
//        else
//        {
//            hand = GameDirector.player.leftPlayerHandState;
//            clickAction = GameDirector.player.vivaControls.VRLeftHand.Interact;
//        }
//        pointer = hand.behaviourPose.transform;
//        SetPointer( pointer, clickAction );
//    }

//    private void OnDisable(){
//        SetPointer( null, null );
//        lineRenderer.enabled = false;
//        if( highlightCoroutine != null ){
//            StopCoroutine( highlightCoroutine );
//            highlightCoroutine = null;
//        }
//        if( canvasHighlight ) canvasHighlight.color = Color.black;
//        lastObjHover = null;
//    }

//    public void SetPointer( Transform _pointer, InputAction _clickAction ){
//        pointer = _pointer;
//        if( clickAction != null ) clickAction.performed -= HandleClick;
//        clickAction = _clickAction;
//        if( pointer && clickAction != null ) clickAction.performed += HandleClick;
//    }

//    private void Update(){
//        Vector3 pointerPos;
//        Vector3 pointerDir;
//        if( pointer == null ){
//            Ray camRay;
//            if( Camera.main != null ){
//                camRay = Camera.main.ScreenPointToRay( GameDirector.input.mousePosition );
//            }else{
//                camRay = new Ray();
//            }
//            pointerPos = camRay.origin;
//            pointerDir = camRay.direction;
//            lineRenderer.enabled = false;
//        }else{
//            pointerPos = pointer.position;
//            pointerDir = -pointer.up;
//            lineRenderer.enabled = true;
//        }

//        sortedGraphics.Clear();
//        var ray = new Ray( pointerPos, pointerDir );
//        SortedRaycastGraphics( ray, sortedGraphics );

//        if( sortedGraphics.Count > 0 ){
//            var top = sortedGraphics[0];
//            SetHover( top );
//            lineRenderer.SetPositions( new Vector3[]{
//                pointerPos,
//                top.worldHitPosition
//            });
//        }else{
//            SetHover( null );
//            lineRenderer.SetPositions( new Vector3[]{
//                pointerPos,
//                pointerPos+pointerDir*1000.0f
//            });
//        }
//        SetHover( sortedGraphics.Count > 0 ? sortedGraphics[0] : null );
//    }

//    private void HandleClick( InputAction.CallbackContext ctx ){
//        if( !lastObjHover ) return;
        
//        var eventData = new PointerEventData( eventSystem );
//        if( ctx.ReadValueAsButton() ){
//            ExecuteEvents.Execute( lastObjHover, eventData, ExecuteEvents.pointerDownHandler );
//        }else{
//            ExecuteEvents.Execute( lastObjHover, eventData, ExecuteEvents.pointerUpHandler );
//            ExecuteEvents.Execute( lastObjHover, eventData, ExecuteEvents.pointerClickHandler );
//        }
//    }

//    private void SetHover( RaycastHitData hitData ){
//        GameObject newObj = hitData?.graphic.gameObject;
//        if( newObj != lastObjHover ){
//            var eventData = new PointerEventData( eventSystem );
//            if( lastObjHover ) ExecuteEvents.Execute( lastObjHover, eventData, ExecuteEvents.pointerExitHandler );
//            lastObjHover = newObj;
//            if( lastObjHover ){
//                ExecuteEvents.Execute( lastObjHover, eventData, ExecuteEvents.pointerEnterHandler );
//                StartHighlightCoroutine( 1.0f );
//            }else{
//                StartHighlightCoroutine( 0.0f );
//            }
//        }
//    }

//    private void StartHighlightCoroutine( float targetAlpha ){
//        if( highlightCoroutine != null ){
//            StopCoroutine( highlightCoroutine );
//        }
//        highlightCoroutine = StartCoroutine( FadeCanvasHighlight( targetAlpha ) );
//    }

//    private IEnumerator FadeCanvasHighlight( float targetAlpha ){
//        if( !canvasHighlight ) yield break;
//        float startAlpha = canvasHighlight.color.r;
//        if( targetAlpha > 0 ) canvasHighlight.enabled = true;

//        while( canvasHighlight.color.r != targetAlpha ){
//            var color = canvasHighlight.color;
//            color = Color.LerpUnclamped( color, Color.white*targetAlpha, Time.deltaTime*8.0f );
//            canvasHighlight.color = color;
//            yield return null;
//        }
//        if( targetAlpha == 0 ) canvasHighlight.enabled = false;
//    }

//    private void SortedRaycastGraphics( Ray ray, List<RaycastHitData> sortedGraphics ){
        
//        if( !canvas.worldCamera ) return;
//        var graphics = GraphicRegistry.GetGraphicsForCanvas(canvas);
//        for (var i = 0; i < graphics.Count; ++i){
//            var graphic = graphics[i];
//            if( graphic.depth == -1 ) continue;
//            if( !graphic.raycastTarget ) continue;
//            if( Tools.RayIntersectsRectTransform( graphic.rectTransform, ray, out Vector3 worldPos, out float distance ) ){

//                Vector2 screenPos = canvas.worldCamera.WorldToScreenPoint(worldPos);
//                if (graphic.Raycast(screenPos, canvas.worldCamera)){
//                    sortedGraphics.Add(new RaycastHitData(graphic, worldPos, screenPos, distance));
//                }
//            }
//        }
//        sortedGraphics.Sort((g1, g2) => g2.graphic.depth.CompareTo(g1.graphic.depth));
//    }

//}

//}