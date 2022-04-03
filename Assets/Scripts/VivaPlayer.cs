using UnityEngine;
using System.Collections;
using System.Collections.Generic;


namespace viva{


public sealed partial class VivaPlayer : MonoBehaviour{

    public static readonly float bodyAngleLag = 35.0f;
    public static readonly float jumpVelY = 4.5f;
    public static VivaPlayer user { get; private set; }

    public delegate void CharacterChangeCallback( Character oldCharacter, Character newCharacter );

    public enum Hand{
        RIGHT,
        LEFT
    }

    [SerializeField]
    private PlayerMovement m_movement;
    public PlayerMovement movement { get{ return m_movement; } }
    [SerializeField]
    private CameraControls keyboardCharacterRigPrefab;
    [SerializeField]
    private CameraControls keyboardGhostRigPrefab;
    [SerializeField]
    private CameraControls vrCameraCharacterPrefab;
    [SerializeField]
    private CameraControls vrGhostRigPrefab;
    [SerializeField]
    private AudioSource flyingSoundSource;
    [SerializeField]
    private float flySoundSqVelMin;
    [SerializeField]
    private float flySoundSqVelMax;
    [SerializeField]
    private Gestures m_gestures;
    public Gestures gestures { get{ return m_gestures; } }


    public CameraControls controls { get; private set; } = null;
    public Character character { get; private set; } = null;
    public new Camera camera { get{ return controls == null ? null : controls.camera; } }
    public Vector3 debug1;
    public Vector3 debug2;
    public CharacterChangeCallback onPreCharacterChange;
    public CharacterChangeCallback onPostCharacterChange;
    public bool isUsingKeyboard { get; private set; } = true;
    public bool isUsingVR { get{ return !isUsingKeyboard; } }
    public GenericCallback onControlsChanged;


    public void Awake(){
        enabled = false;
        onControlsChanged += delegate{
            if( UI.main ) UI.main.SetCanvasMode( isUsingKeyboard );
        };
        
        onPostCharacterChange += delegate( Character old, Character newChar ){
            VivaSettings.main.Apply();
        };
        user = this;
    }

    private bool ReloadControls( bool useKeyboard ){

        CameraControls controlsPrefab;
        if( useKeyboard ){
            controlsPrefab = character ? keyboardCharacterRigPrefab : keyboardGhostRigPrefab;
        }else{
            controlsPrefab = character ? vrCameraCharacterPrefab : vrCameraCharacterPrefab;
        }
        if( !controlsPrefab.Allowed() ){
            return false;
        }
        // VivaSettings.main.launchInVR = !useKeyboard;
        isUsingKeyboard = useKeyboard;

        Vector3 controlsSpawnPos = Vector3.zero;
        Quaternion controlsSpawnRot = Quaternion.identity;
        if( controls ){
            if( Camera.main && useKeyboard ){
                controlsSpawnPos = Camera.main.transform.position;
                controlsSpawnRot = Camera.main.transform.rotation;
            }
            if( controls.GetType() != controlsPrefab.GetType() ){
                GameObject.DestroyImmediate( controls.gameObject );
                controls = null;
            }
        }
        if( controls == null ){
            controls = GameObject.Instantiate( controlsPrefab, controlsSpawnPos, controlsSpawnRot );
            controls.Initialize( this );
        }
        if( UI.main ) UI.main.SetCanvasCamera( controls.camera );

        onControlsChanged?.Invoke();
        return true;
    }

    public void ToggleVR(){
        if( ReloadControls( !isUsingKeyboard ) ) character?._InternalReset();
    }

    public void Possess( Character newCharacter, bool useKeyboard ){
        
        gestures._InternalReset();

        var oldCharacter = character;
        if( oldCharacter ){
            oldCharacter.biped.head.target.localScale = Vector3.one;
            oldCharacter.onRagdollChange -= HandleRagdollChange;
            transform.SetParent( null, false );
            oldCharacter._InternalSetPossessor( null );
            enabled = false;
        
            foreach( var muscle in oldCharacter.ragdoll.muscles ){
                foreach( var collider in muscle.colliders ){
                    collider.gameObject.layer = WorldUtil.characterCollisionsLayer;
                }
            }
        }

        character = newCharacter;
        if( newCharacter ){
            newCharacter.onRagdollChange += HandleRagdollChange;
            HandleRagdollChange( oldCharacter?.ragdoll, newCharacter.ragdoll );
            transform.SetParent( newCharacter.model.armature, false );
            newCharacter._InternalSetPossessor( this );
            enabled = true;
        
            foreach( var muscle in newCharacter.ragdoll.muscles ){
                foreach( var collider in muscle.colliders ){
                    collider.gameObject.layer = WorldUtil.selfCharacterCollisionsLayer;
                }
            }
        }

        onPreCharacterChange?.Invoke( oldCharacter, newCharacter );
        if( (bool)oldCharacter != (bool)character ){
            if( !ReloadControls( useKeyboard ) ){
                ReloadControls( !useKeyboard );
            }
        }
        onPostCharacterChange?.Invoke( oldCharacter, character );

        oldCharacter?.scriptManager.Recompile();
        character?.scriptManager.Recompile(); //todo prevent calling reset twice?
    }

    private void HandleRagdollChange( Ragdoll oldRagdoll, Ragdoll newRagdoll ){
        if( newRagdoll == null ) return;
        var bipedRagdoll = newRagdoll as BipedRagdoll;
        if( bipedRagdoll ) bipedRagdoll.head.target.localScale = Vector3.zero;
    }

    private void FixedUpdate(){
        var vel = character.ragdoll.root.rigidBody.velocity.sqrMagnitude;
        flyingSoundSource.volume = Tools.GetClampedRatio( flySoundSqVelMin, flySoundSqVelMax, vel );
    }
}

}