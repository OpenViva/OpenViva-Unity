using System.Collections;
using UnityEngine;
using viva;


public class SlidingDoor: VivaScript{

    private Item item;

    public SlidingDoor( Item _item ){
        item = _item;
        
        OnMoveEditorSelected( false );

        item.rigidBody.useGravity = false;
        item.rigidBody.drag = 3f;
        item.rigidBody.constraints = RigidbodyConstraints.FreezePositionY;
    }

    // when exiting and saving, Unity physics does not guarantee the rigid body transform will be
    // accurate within the slide joint, so manually save/load it
    private void LoadSlideTransform( object[] savedObjs ){
        if( savedObjs == null || savedObjs.Length < 2 ){
            Debugger.LogWarning("Could nto load sliding door from save file");
            return;
        }
        var position = (Vector3)savedObjs[0];
        var yaw = (float)savedObjs[1];

        item.rigidBody.transform.position = position;
        item.rigidBody.transform.localEulerAngles = new Vector3( 0, yaw, 0 );
        ResetSlideJoint();
    }

    public void OnMoveEditorSelected( object selectedObj ){
        var selected = (bool)selectedObj;
        if( !selected ) ResetSlideJoint();
    }

    private void ResetSlideJoint(){
        var cj = item.rigidBody.GetComponent<ConfigurableJoint>();
        if( cj ) Viva.Destroy( cj );

        cj = item.rigidBody.gameObject.AddComponent<ConfigurableJoint>();

        cj.autoConfigureConnectedAnchor = false;
        cj.yMotion = ConfigurableJointMotion.Locked;
        cj.zMotion = ConfigurableJointMotion.Locked;
        
        cj.angularXMotion = ConfigurableJointMotion.Locked;
        cj.angularYMotion = ConfigurableJointMotion.Locked;
        cj.angularZMotion = ConfigurableJointMotion.Locked;
        
        cj.anchor = Vector3.zero;
        cj.axis = Vector3.right;
        
        cj.connectedAnchor = item.rigidBody.transform.position;

        //save slide joint
        Save( "LoadSlideTransform", new object[]{ item.rigidBody.transform.position, item.rigidBody.transform.localEulerAngles.y } );
        SetupSlideExtents();
    }

    private void SetupSlideExtents(){

        item.customVariables.Get( this, "slide+" ).value = null;
        item.customVariables.Get( this, "slide-" ).value = null;

        if( !item.model.meshFilter ) return;

        SetupSlideExtent( "slide+", 1.0f );
        SetupSlideExtent( "slide-", -1.0f );
    }

    private void SetupSlideExtent( string side, float sign ){

        var bounds = item.model.meshFilter.sharedMesh.bounds;
        var maxDistance = item.model.rootTransform.lossyScale.x*bounds.size.x*4;    //4 door size checks
        var halfExtents = bounds.size*0.5f;
        halfExtents.x *= item.model.rootTransform.lossyScale.x*0.95f;
        halfExtents.y *= item.model.rootTransform.lossyScale.y*0.9f;   //shrink a bit to prevent colliding with floor
        halfExtents.z *= item.model.rootTransform.lossyScale.z*0.9f;   //shrink a bit to prevent colliding with other adj doors
        var start = item.model.rootTransform.TransformPoint( bounds.center );
        var dir = item.transform.right*sign;
        var hitResults = Physics.BoxCastAll(
            start,
            halfExtents,
            dir,
            item.transform.rotation,
            maxDistance,
            WorldUtil.defaultMask|WorldUtil.itemsMask|WorldUtil.itemsStaticMask,
            QueryTriggerInteraction.Ignore
        );
        //find shortest wall distance
        float toWallDist = 100;
        foreach( var hitResult in hitResults ){
            var colliderItem = hitResult.collider.gameObject.GetComponentInParent<Item>();
            if( colliderItem == item ) continue;
            if( colliderItem && colliderItem.HasAttribute("sliding_door") ) continue;
            //if no item then that means it hit a Default layer collider
            toWallDist = Mathf.Min( toWallDist, hitResult.distance );
        }
        //adjacent doors are only those within the toWallDist
        var adjDoorCount = 0;
        foreach( var hitResult in hitResults ){
            var colliderItem = hitResult.collider.gameObject.GetComponentInParent<Item>();
            if( colliderItem == item ) continue;
            if( !colliderItem || !colliderItem.HasAttribute("sliding_door") || hitResult.distance > toWallDist ) continue;
            adjDoorCount++;
        }
        Vector3 slideExtent = start+dir*( toWallDist-halfExtents.x*2*adjDoorCount );

        // Tools.DrawDiagCross( slideExtent, Color.green, 0.3f );
        item.customVariables.Get( this, side ).value = slideExtent;
    }
} 