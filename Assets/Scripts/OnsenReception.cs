using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace viva{

public class OnsenReception : Service{

    public delegate void OnAttendStartCallback( Loli loli, bool valid );

    [SerializeField]
    private TowelClip[] m_storageTowelClips = new TowelClip[0];
    public TowelClip[] storageTowelClips { get{ return m_storageTowelClips; } }
    [SerializeField]
    private Transform m_followMeStart;
    public Transform followMeStart { get{ return m_followMeStart; } }
    [SerializeField]
    private Transform m_followMeBathroom;
    public Transform followMeBathroom { get{ return m_followMeBathroom; } }
    [SerializeField]
    private ReceptionBell m_receptionBell;
    public ReceptionBell receptionBell { get{ return m_receptionBell; } }
    [SerializeField]
    private Vector3 m_localClientWaitZonePos;
    public Vector3 localClientWaitZonePos { get{ return m_localClientWaitZonePos; } }
    [SerializeField]
    private Vector3 m_localClientShowChangingRoomPos;
    public Vector3 localClientShowChangingRoomPos { get{ return m_localClientShowChangingRoomPos; } }
    [SerializeField]
    private Transform m_localQueueStart;
    public Transform localQueueStart { get{ return m_localQueueStart; } }
    [SerializeField]
    private Transform m_localClientPostRingWaitZone;
    public Transform localClientPostRingWaitZone { get{ return m_localClientPostRingWaitZone; } }



    public override void OnDrawGizmosSelected(){
        base.OnDrawGizmosSelected();

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere( localClientWaitZonePos, 0.2f );
        Gizmos.DrawWireSphere( localClientShowChangingRoomPos, 0.2f );
    }
    
    protected override void OnInitializeEmployment( Loli targetLoli ){
        targetLoli.active.onsenClerk.onsenClerkSession.onsenReceptionAsset = this;
    }

    //returns next employee loli
    public bool CreateClerkSession( Character caller, OnsenClerkBehavior.ClerkSessionCallback onStart ){
        if( caller == null ){
            return false;
        }
        //alert nearest employee
        var closest = GetActiveServiceUser(0);
        if( closest != null && closest.loli != null ){
            return closest.loli.active.onsenClerk.AttemptAttendClient( caller, onStart );
        }
        return false;
    }

    public Towel SpawnNextStorageTowel(){

        foreach( var towelClip in storageTowelClips ){
            if( towelClip.activeTowel ){
                return towelClip.activeTowel;
            }else{
                towelClip.SpawnNewTowel();
            }
        }
        return storageTowelClips[0].activeTowel;
    }

    public TowelClip GetNextEmptyStorageTowel(){

        foreach( var towelClip in storageTowelClips ){
            if( towelClip.activeTowel == null ){
                return towelClip;
            }
        }
        return null;
    }

    public override void EndUse(Character targetCharacter){
    }
}

}