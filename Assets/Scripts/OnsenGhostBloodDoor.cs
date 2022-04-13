using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace viva{

public class OnsenGhostBloodDoor : MonoBehaviour{

    [SerializeField]
    private Material regularMaterial;
    [SerializeField]
    private SlidingDoor[] doorSet;
    [SerializeField]
    private int referenceIndex;
    [SerializeField]
    private SoundSet growthSoundSet;
    [SerializeField]
    private AudioClip solvedSound;
    [SerializeField]
    private OnsenGhostMiniGame parentGame;

    public int doors { get{ return doorSet.Length; } }
    public bool hasAGap { get; private set; }
    private MeshRenderer[] doorMRs;
    private Material[] bloodMaterials;
    private Rigidbody[] doorRigidBodies;
    private bool[] gapStates;
    private static readonly int bloodAmountID = Shader.PropertyToID("_Blood");
    private static readonly int solveID = Shader.PropertyToID("_Solve");
    private bool bloodEnabled = false;
    private float huddleMinimum;
    private float referenceUnit;
    private float sleepTimeout = 0.0f;


    public SlidingDoor GetDoorSet( int index ){
        return doorSet[ index ];
    }

    public void Awake(){
        doorMRs = new MeshRenderer[ doorSet.Length ];
        bloodMaterials = new Material[ doorSet.Length ];
        doorRigidBodies = new Rigidbody[ doorSet.Length ];
        gapStates = new bool[ doorSet.Length ];
        for( int i=0; i<bloodMaterials.Length; i++ ){
            doorMRs[i] = doorSet[i].GetComponent<MeshRenderer>();
            bloodMaterials[i] = doorMRs[i].material;
            doorRigidBodies[i] = doorMRs[i].GetComponent<Rigidbody>();
        }

        SlidingDoor referenceDoor = doorSet[ referenceIndex ];
        //calculate huddle minimum
        for( int i=0; i<doorSet.Length; i++ ){
            float doorUnitPos = referenceDoor.GetUnitDistanceFromStart( doorSet[i] );
            huddleMinimum = Mathf.Max( Mathf.Abs( referenceUnit-doorUnitPos ), huddleMinimum );
        }
        if( doorSet.Length > 1 ){
            huddleMinimum /= doorSet.Length-1;
        }
        SetEnableBlood( true );

        foreach( var rb in doorRigidBodies ){
            rb.AddForce( rb.transform.right*( Random.value-0.5f )*10.0f, ForceMode.VelocityChange );
        }
    }

    public void SetEnableBlood( bool enable ){
        for( int i=0; i<bloodMaterials.Length; i++ ){
            var mat = bloodMaterials[i];
            Material material;
            if( enable ){
                material = bloodMaterials[i];
            }else{
                material = regularMaterial;
            }
            doorMRs[i].material = material;
        }
    }

    public void CheckClosedState(){
        if( Time.time > 2.0f && gameObject.activeInHierarchy ){
            enabled = true;
            UpdateGapStates();
        }
    }

    private void UpdateGapStates(){
        SlidingDoor referenceDoor = doorSet[ referenceIndex ];
        bool[] hadGap = new bool[ gapStates.Length ];
        for( int i=0; i<gapStates.Length; i++ ){
            hadGap[i] = gapStates[i];
            gapStates[i] = false;
        }
        bool hadAGap = hasAGap;
        hasAGap = false;
        for( int i=0; i<doorSet.Length; i++ ){
            float doorUnitPos_i = referenceDoor.GetUnitDistanceFromStart( doorSet[i] );
            for( int j=i+1; j<doorSet.Length; j++ ){
                float doorUnitPos_j = referenceDoor.GetUnitDistanceFromStart( doorSet[j] );
                bool leftAGap = Mathf.Abs( doorUnitPos_j-doorUnitPos_i ) < huddleMinimum*0.85f;
                gapStates[i] |= leftAGap;
                gapStates[j] |= leftAGap;
                if( leftAGap ){
                    hasAGap = true;
                }
            }
        }
        
        for( int i=0; i<gapStates.Length; i++ ){
            bool had = hadGap[i];
            bool current = gapStates[i];
            if( current && !had ){
                PlayGameSound( doorSet[i].transform, growthSoundSet.GetRandomAudioClip() );
            }
            if( !current && had ){
                PlayGameSound( doorSet[i].transform, solvedSound );
                GameDirector.instance.StartCoroutine( SolveEffect( bloodMaterials[i] ) );
            }
        }
        if( hadAGap && !hasAGap ){
            parentGame.PlayDelayedDoorBangScare( doorSet[ Random.Range( 0, doorSet.Length ) ].transform );
        }
    }

    private IEnumerator SolveEffect( Material mat ){
        float starTime = Time.time;
        float duration = 0.3f;
        while( Time.time-starTime < duration ){
            float ratio = ( Time.time-starTime )/duration;
            mat.SetFloat( solveID, ratio*0.5f );
            yield return null;
        }
        starTime = Time.time;
        duration = 2.0f;
        while( Time.time-starTime < duration ){
            float ratio = ( Time.time-starTime )/duration;
            mat.SetFloat( solveID, (1.0f-ratio)*0.5f );
            yield return null;
        }
    }
    
    private void PlayGameSound( Transform target, AudioClip clip ){
        var handle = SoundManager.main.RequestHandle( Vector3.up, target );
        handle.Play( clip );
        handle.pitch = ( 0.6f+Random.value*0.5f );
        handle.maxDistance = 6.0f;
        handle.volume = 0.8f;
    }

    private void FixedUpdate(){
        UpdateGapStates();
        
        bool sleeping = true;
        foreach( var rb in doorRigidBodies ){
            if( !rb.IsSleeping() ){
                sleeping = false;
                break;
            }
        }
        if( sleeping ){
            if( Time.time-sleepTimeout > 2.0f ){
                enabled = false;
            }
        }else{
            sleepTimeout = Time.time;
        }
    }

    public void Update(){
        for( int i=0; i<doorSet.Length; i++ ){
            var mat = bloodMaterials[i];
            float current = mat.GetFloat( bloodAmountID );
            if( gapStates[i] ){
                mat.SetFloat( bloodAmountID, Mathf.Clamp01( current+Time.deltaTime*(1.0f-current) ) );
            }else{
                mat.SetFloat( bloodAmountID, Mathf.Clamp01( current-Time.deltaTime*2.0f*current ) );
            }
        }
    }
}

}