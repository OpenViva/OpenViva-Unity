using System.Collections;
using System.Collections.Generic;
using UnityEngine;



namespace viva{

public delegate Vector3? NullableVec3ReturnFunc();
public delegate void NullableVec3ParamFunc( Vector3? pos );

[RequireComponent(typeof(BoxCollider))]
public class Zone : MonoBehaviour{

    public BoxCollider boxCollider { get; private set; }
    private Coroutine surveyCoroutine = null;
    public Item _internalParentItem;
    public Item parentItem { get{ return _internalParentItem; } }
    public ListenerItem onTriggerEnterItem { get; private set; }
    public ListenerItem onTriggerExitItem { get; private set; }
    public List<Item> items { get; private set; } = new List<Item>();

    
    public void Awake(){
        boxCollider = GetComponent<BoxCollider>();
        onTriggerEnterItem = new ListenerItem( items, "onTriggerEnterItem" );
        onTriggerExitItem = new ListenerItem( (Item[])null, "onTriggerExitItem" );
    }

    public void OnTriggerEnter( Collider collider ){
        var item = collider.GetComponentInParent<Item>();
        if( item && !items.Contains( item ) ){
            items.Add( item );
            onTriggerEnterItem.Invoke( item );
        }
    }

    public void OnTriggerExit( Collider collider ){
        var item = collider.GetComponentInParent<Item>();
        if( item ){
            var index = items.IndexOf( item );
            if( index>-1 ){
                items.Remove( item );
                onTriggerExitItem.Invoke( item );
            }
        }
    }

    public Vector3 GetRandomBoxLocation(){
#if UNITY_EDITOR
        var boxCollider = GetComponent<BoxCollider>();
#endif
        var randomSize = new Vector3( boxCollider.size.x*( -0.5f+Random.value ), 0, boxCollider.size.z*( -0.5f+Random.value ) );
        return boxCollider.transform.TransformPoint( boxCollider.center+randomSize );
    }

    public Vector3 GetNearestPosition( Vector3 from ){
        Vector3 nearest = transform.InverseTransformPoint( from );
        var worldSize = boxCollider.size;
        worldSize.x *= transform.lossyScale.x;
        worldSize.y *= transform.lossyScale.y;
        worldSize.z *= transform.lossyScale.z;
        nearest.x = Mathf.Clamp( nearest.x, -worldSize.x, worldSize.x );
        nearest.y = Mathf.Clamp( nearest.y, -worldSize.y, worldSize.y );
        nearest.z = Mathf.Clamp( nearest.z, -worldSize.z, worldSize.z );
        
        return transform.TransformPoint( nearest );
    }

    public bool _InternalFindEmptySpot( float radius, NullableVec3ParamFunc onFound ){
        if( onFound == null ){
            Debugger.LogError("Cannot find an empty spot with a null onFound callback");
            return false;
        }
        if( surveyCoroutine != null ){
            return false;
        }

        //allow only 1 listen at a time
        surveyCoroutine = StartCoroutine( ExecuteSurvey( radius, onFound ) );
        return true;
    }

    private IEnumerator ExecuteSurvey( float radius, NullableVec3ParamFunc onFound ){

        var halfSize = boxCollider.size*0.5f;
        halfSize.x *= transform.lossyScale.x;
        halfSize.y *= transform.lossyScale.y;
        halfSize.z *= transform.lossyScale.z;
        
        Vector3? emptySpot = null;
        while( true ){
            bool occupied = Physics.OverlapBoxNonAlloc(
                transform.TransformPoint( boxCollider.center ),
                halfSize,
                new Collider[1],
                transform.rotation,
                WorldUtil.itemsMask|WorldUtil.itemsStaticMask,
                QueryTriggerInteraction.Ignore
            )>0;

            if( occupied ){
                Tools.DrawDiagCross( transform.TransformPoint( boxCollider.center ), Color.red, radius );
            }else{
                Tools.DrawDiagCross( transform.TransformPoint( boxCollider.center ), Color.green, radius );
                emptySpot = transform.TransformPoint( boxCollider.center );
                break;
            }
            yield return new WaitForFixedUpdate();
        }
        surveyCoroutine = null;
        onFound.Invoke( emptySpot );
    }
}


}