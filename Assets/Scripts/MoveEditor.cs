using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


namespace viva{

public class MoveEditor : MonoBehaviour
{

    public abstract class Tool{

        protected readonly MoveEditor moveEditor;
        public Tool( MoveEditor _moveEditor ){
            moveEditor = _moveEditor;
        }

        public abstract void OnEnd();
        public abstract void OnBegin();
        public abstract void OnMouseDown();
        public abstract void OnMouseUp();
        public abstract void OnMouseMove();
    }

    public class PositionTool: Tool{

        public PositionTool( MoveEditor _moveEditor ):base(_moveEditor){
        }

        private string dragStartUI = null;
        private Vector3? startPositionAxis;
        private Vector3? lastHitPos = null;

        public override void OnBegin(){
            moveEditor.positionAxis.gameObject.SetActive( true );
            if( !startPositionAxis.HasValue && moveEditor.selected != null && moveEditor.selected.rootTransform ){
                startPositionAxis = moveEditor.selected.rootTransform.position;
                moveEditor.positionAxis.position = startPositionAxis.Value;
            }
        }

        public override void OnEnd(){
            moveEditor.positionAxis.gameObject.SetActive( false );
        }
        
        public override void OnMouseDown(){
            var ui = moveEditor.SelectAndReturnCollider( Viva.input.mousePosition, true, out Vector3 newSelectionPos );
            lastHitPos = null;
            if( newSelectionPos != Vector3.zero ){
                moveEditor.positionAxis.position = newSelectionPos;
            }else{
                SetColorMult( ui, 0.0f );   //click filter
                dragStartUI = ui;
                startPositionAxis = moveEditor.positionAxis.position;
            }
        }
        
        public override void OnMouseUp(){
            SetColorMult( null, 1 );   //click filter
        }
        
        public override void OnMouseMove(){
            var ui = moveEditor.SelectAndReturnCollider( Viva.input.mousePosition, false, out Vector3 newSelectionPos );
            if( !moveEditor.mouseIsDown ){
                SetColorMult( ui, 0.5f );   //hover highlight
            }else{
                switch( dragStartUI ){
                case "yz":
                    MovePosition( Vector3.right );
                    break;
                case "x":
                    MovePosition( Vector3.forward, 0 );
                    break;
                case "xz":
                    MovePosition( Vector3.up );
                    break;
                case "y":
                    MovePosition( Vector3.right, 1 );
                    break;
                case "xy":
                    MovePosition( Vector3.forward );
                    break;
                case "z":
                    MovePosition( Vector3.up, 2 );
                    break;
                }
            }
        }

        private void MovePosition( Vector3 planeNormal, int limitCoord=-1 ){
            if( moveEditor.selected == null || moveEditor.selected.rootTransform == null ){
                moveEditor.SetSelection( null );
                return;
            }
            var currRay = Camera.main.ScreenPointToRay( Viva.input.mousePosition );

            var plane = new Plane( planeNormal, startPositionAxis.Value );
            if( !plane.Raycast( currRay, out float currEnter ) ) return;

            var currHitPos = currRay.origin+currRay.direction*currEnter;
            if( !lastHitPos.HasValue ){
                lastHitPos = currHitPos;
                return;
            }
            var hitDelta = currHitPos-lastHitPos.Value;
            lastHitPos = currHitPos;

            var oldAxisPos = moveEditor.positionAxis.position;
            if( limitCoord >= 0 ){
                var newPos = moveEditor.positionAxis.position;
                newPos[ limitCoord ] += hitDelta[ limitCoord ];
                moveEditor.positionAxis.position = newPos;
            }else{
                moveEditor.positionAxis.position += hitDelta;
            }
            var delta = moveEditor.positionAxis.position-oldAxisPos;
            if( moveEditor.selected != null ){
                moveEditor.selected.rootTransform.position += delta;
            }
        }

        private void SetColorMult( string ui, float gain ){
            Color mult;
            switch( ui ){
            case "yz":
            case "x":
                mult = new Color( 1, gain, gain, 1 );
                break;
            case "xz":
            case "y":
                mult = new Color( gain, 1, gain, 1 );
                break;
            case "z":
            case "xy":
                mult = new Color( gain, gain, 1, 1 );
                break;
            default:
                mult = Color.white;
                break;
            }
            moveEditor.positionAxis.GetComponent<MeshRenderer>().material.color = mult;
        }
    }
    
    public class RotationTool: Tool{

        public RotationTool( MoveEditor _moveEditor ):base(_moveEditor){
        }
        
        private string dragStartUI = null;
        private float? lastDeg = null;

        public override void OnBegin(){
            moveEditor.rotationAxis.gameObject.SetActive( true );
            if( moveEditor.selected != null && moveEditor.selected.rootTransform ){
                moveEditor.rotationAxis.position = moveEditor.selected.rootTransform.position;
            }
        }

        public override void OnEnd(){
            moveEditor.rotationAxis.gameObject.SetActive( false );
        }
        
        public override void OnMouseDown(){
            var ui = moveEditor.SelectAndReturnCollider( Viva.input.mousePosition, true, out Vector3 newSelectionPos );
            lastDeg = null;
            if( newSelectionPos != Vector3.zero ){
                moveEditor.rotationAxis.position = newSelectionPos;
                moveEditor.rotationAxis.rotation = moveEditor.selected.rootTransform.rotation;
            }else{
                SetColorMult( ui, 0.0f );   //click filter
                dragStartUI = ui;
            }
        }
        
        public override void OnMouseUp(){
            SetColorMult( null, 1 );   //click filter
        }
        
        public override void OnMouseMove(){
            var ui = moveEditor.SelectAndReturnCollider( Viva.input.mousePosition, false, out Vector3 newSelectionPos );
            if( !moveEditor.mouseIsDown ){
                SetColorMult( ui, 0.5f );   //hover highlight
            }else{
                switch( dragStartUI ){
                case "x":
                    Rotate( Vector3.right, 0 );
                    break;
                case "y":
                    Rotate( Vector3.up, 1 );
                    break;
                case "z":
                    Rotate( Vector3.forward, 2 );
                    break;
                }
            }
        }

        private void Rotate( Vector3 planeNormal, int limitCoord ){
            if( moveEditor.selected == null || moveEditor.selected.rootTransform == null ){
                moveEditor.SetSelection( null );
                return;
            }

            moveEditor.rotationAxis.rotation = moveEditor.selected.rootTransform.rotation;
            planeNormal = moveEditor.rotationAxis.TransformDirection( planeNormal );

            var currRay = Camera.main.ScreenPointToRay( Viva.input.mousePosition );

            var plane = new Plane( planeNormal, moveEditor.rotationAxis.position );
            if( !plane.Raycast( currRay, out float currEnter ) ) return;

            var currHitPos = currRay.origin+currRay.direction*currEnter;
            float sign = System.Convert.ToInt32( plane.GetSide( Camera.main.transform.position ) )*2-1;
            float currDeg = GetDegree( currHitPos-moveEditor.rotationAxis.position, limitCoord )*sign;
            
            if( !lastDeg.HasValue ){
                lastDeg = currDeg;
                return;
            }
            int cuts = moveEditor.shiftDown ? 256: 32;
            float degsPerCut = 360f/cuts;
            currDeg = Mathf.Round( ( currDeg-lastDeg.Value )/degsPerCut )*degsPerCut+lastDeg.Value;
            if( currDeg == lastDeg.Value ) return;
            //snap
            if( !moveEditor.shiftDown ){
                var euler = moveEditor.selected.rootTransform.localEulerAngles;
                var eulerCoord = euler[ limitCoord ];
                eulerCoord = Mathf.Round( eulerCoord/degsPerCut )*degsPerCut;
                euler[ limitCoord ] = eulerCoord;
                moveEditor.selected.rootTransform.localEulerAngles = euler;
            }

            moveEditor.selected.rootTransform.RotateAround( moveEditor.rotationAxis.position, planeNormal, currDeg-lastDeg.Value );
            moveEditor.rotationAxis.rotation = moveEditor.selected.rootTransform.rotation;
            lastDeg = currDeg;
        }

        private float GetDegree( Vector3 delta, int limitCoord ){

            switch( limitCoord ){
            case 0:
                return Mathf.Atan2( delta.y, -delta.z )*Mathf.Rad2Deg;
            case 1:
                return Mathf.Atan2( delta.z, -delta.x )*Mathf.Rad2Deg;
            case 2:
                return Mathf.Atan2( delta.x, -delta.y )*Mathf.Rad2Deg;
            }
            return 0;
        }

        private void SetColorMult( string ui, float gain ){
            Color mult;
            switch( ui ){
            case "x":
                mult = new Color( 1, gain, gain, 1 );
                break;
            case "y":
                mult = new Color( gain, 1, gain, 1 );
                break;
            case "z":
                mult = new Color( gain, gain, 1, 1 );
                break;
            default:
                mult = Color.white;
                break;
            }
            moveEditor.rotationAxis.GetComponent<MeshRenderer>().material.color = mult;
        }
    }


    
    [SerializeField]
    public Transform positionAxis;
    [SerializeField]
    public Transform rotationAxis;

    public Model selected { get; private set; }
    private bool startedKinematic = false;
    private Model oldSelection = null;
    private Tool tool = null;
    private bool? inputBindedKeyboard = null;
    private Outline.Entry selectionOutline = null;
    public bool mouseIsDown { get; private set; }
    public bool shiftDown { get; private set; }



    
    private void OnMouseMove( InputAction.CallbackContext ctx ){
        tool?.OnMouseMove();
    }

    private void OnMouseButton( InputAction.CallbackContext ctx ){
        if( ctx.ReadValueAsButton() ){
            mouseIsDown = true;
            tool?.OnMouseDown();
        }else{
            mouseIsDown = false;
            tool?.OnMouseUp();
        }
    }

    public void SetSelection( Model newSelection ){
        if( selected == newSelection ) return;
        if( selected != null && selected.rootTransform && selected.rootTransform.TryGetComponent<Item>( out Item oldItem ) ){
            if( oldItem.rigidBody ) oldItem.rigidBody.isKinematic = startedKinematic;
            oldItem.scriptManager.CallOnAllScripts( "OnMoveEditorSelected", new object[]{ false }, true );
            
            if( oldItem.immovable ){
                var bounds = selected.bounds;
                if( bounds.HasValue ) Scene.main.BakeNavigation();
            }
        }
        
        Outline.StopOutlining( selectionOutline );
        selected = newSelection;
        selectionOutline = Outline.StartOutlining( selected, null, Color.yellow, Outline.Constant );

        if( selected != null && selected.rootTransform && selected.rootTransform.TryGetComponent<Item>( out Item newItem ) ){
            if( newItem.rigidBody ){
                startedKinematic = newItem.rigidBody.isKinematic;
                newItem.rigidBody.isKinematic = true;
            }
            newItem.scriptManager.CallOnAllScripts( "OnMoveEditorSelected", new object[]{ true }, true );
        }
    }

    public void OnEnable(){
        GameUI.main.SetHideDecorations( true );

        UnbindLastInput();
        if( VivaPlayer.user.isUsingKeyboard ){
            inputBindedKeyboard = true;
            var desktop = Viva.input.actions.desktop;
            desktop.LeftMouse.performed += OnMouseButton;
            desktop.LeftMouse.canceled += OnMouseButton;
            desktop.MousePosition.performed += OnMouseMove;
            desktop.W.performed += OnPositionHotkey;
            desktop.E.performed += OnRotationHotkey;
            desktop.LeftShift.performed += OnShiftKey;
        }
        tool?.OnBegin();
        
        SetSelection( oldSelection );
    }

    private void UnbindLastInput(){
        if( !inputBindedKeyboard.HasValue ) return;

        if( inputBindedKeyboard.Value ){
            var desktop = Viva.input.actions.desktop;
            desktop.LeftMouse.performed -= OnMouseButton;
            desktop.LeftMouse.canceled -= OnMouseButton;
            desktop.MousePosition.performed -= OnMouseMove;
            desktop.W.performed -= OnPositionHotkey;
            desktop.E.performed -= OnRotationHotkey;
            desktop.LeftShift.performed -= OnShiftKey;
        }
        inputBindedKeyboard = null;
    }
    
    private void OnShiftKey( InputAction.CallbackContext ctx ){
        shiftDown = ctx.ReadValueAsButton();
    }

    private void OnPositionHotkey( InputAction.CallbackContext ctx ){
        SwitchToPositionTool();
    }
    
    private void OnRotationHotkey( InputAction.CallbackContext ctx ){
        SwitchToRotationTool();
    }

    public void OnDisable(){
        tool?.OnEnd();
        GameUI.main.SetHideDecorations( false );
        UnbindLastInput();
        
        oldSelection = selected;
        SetSelection( null );
    }

    public void SwitchToPositionTool(){
        tool?.OnEnd();
        tool = new PositionTool( this );
        tool.OnBegin();
    }

    public void SwitchToRotationTool(){
        tool?.OnEnd();
        tool = new RotationTool( this );
        tool.OnBegin();
    }

    //prioritizes UI collisions (besides main canvas collider)
    public string SelectAndReturnCollider( Vector2 screenMousePos, bool select, out Vector3 newSelectionPos ){
        var ray = Camera.main.ScreenPointToRay( screenMousePos );
        var raycastHits = Physics.RaycastAll( ray.origin, ray.direction, 100, WorldUtil.uiMask|WorldUtil.itemsMask|WorldUtil.itemsStaticMask, QueryTriggerInteraction.Ignore );

        float shortestModelDist = Mathf.Infinity;
        Model closestModel = null;
        float shortestUIDist = Mathf.Infinity;
        string closestUI = null;
        foreach( var raycastHit in raycastHits ){
            int layer = raycastHit.collider.gameObject.layer;
            if( layer == WorldUtil.itemsLayer || layer == WorldUtil.itemsStaticLayer ){
                var item = raycastHit.collider.GetComponentInParent<Item>();
                if( !item ) continue;
                if( raycastHit.distance < shortestModelDist ){
                    shortestModelDist = raycastHit.distance;
                    closestModel = item.model;
                }
            }else{
                //ignore main canvas collider
                if( raycastHit.collider == GameUI.main.canvasCollider ) continue;
                if( raycastHit.collider.name.EndsWith("_f") ){
                    if( Vector3.Dot( raycastHit.normal, raycastHit.collider.transform.forward ) < 0 ) continue;
                }
                if( raycastHit.distance < shortestUIDist ){
                    shortestUIDist = raycastHit.distance;
                    closestUI = raycastHit.collider.name.Replace("_f","");
                }
            }
        }
        newSelectionPos = Vector3.zero;
        if( closestUI == null ){
            if( closestModel != null ){
                if( select ){
                    SetSelection( closestModel );
                    newSelectionPos = ray.origin+ray.direction*shortestModelDist;
                }
            }
        }
        return closestUI;
    }
}

}