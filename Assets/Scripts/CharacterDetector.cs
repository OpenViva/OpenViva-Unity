using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;



namespace viva{


public partial class CharacterDetector: MonoBehaviour{

    public static CharacterDetector Create( Transform target, Character ignore ){
        var cdContainer = new GameObject("Character Detector");
        var cd = cdContainer.AddComponent<CharacterDetector>();

        cdContainer.transform.SetParent( target, false );
        cdContainer.layer = WorldUtil.characterCollisionsLayer;
        cd.ignore = ignore;

        cd.sphere = cdContainer.AddComponent<SphereCollider>();
        cd.sphere.isTrigger = true;
        cd.sphere.radius = 1f/cdContainer.transform.lossyScale.y;

        return cd;
    }


    public ListenerCharacter onCharacterNearby { get; private set; }
    public ListenerCharacter onCharacterFar { get; private set; }
    private List<Character> m_nearbyCharacters = new List<Character>();
    public IList<Character> nearbyCharacters { get{ return m_nearbyCharacters.AsReadOnly(); } }
    private Character ignore;
    private SphereCollider sphere;
    public float radius {
        get{
            return sphere.radius/Mathf.Max( transform.lossyScale.y, Mathf.Epsilon );
        }
        set{
            sphere.radius = value/Mathf.Max( transform.lossyScale.y, Mathf.Epsilon );
        }
    }


    private void Awake(){
        onCharacterNearby = new ListenerCharacter( m_nearbyCharacters, "onCharacterNearby" );
        onCharacterFar = new ListenerCharacter( (Character[])null, "onCharacterFar" );
    }

    public void _InternalReset(){
        onCharacterNearby._InternalReset();
        onCharacterFar._InternalReset();
    }

    private void OnTriggerEnter( Collider collider ){
        var character = Util.GetCharacter( collider );
        if( !character ) return;
        if( character == ignore ) return;
        if( collider != character.ragdoll.root.colliders[0] ) return;
        if( m_nearbyCharacters.Contains( character ) ) return;
    
        m_nearbyCharacters.Add( character );
        onCharacterNearby.Invoke( character );
    }

    private void OnTriggerExit( Collider collider ){
        var character = Util.GetCharacter( collider );
        if( !character ) return;
        if( character == ignore ) return;
        if( collider != character.ragdoll.root.colliders[0] ) return;
        var index = m_nearbyCharacters.IndexOf( character );
        if( index != -1 ){
            m_nearbyCharacters.RemoveAt( index );
            onCharacterFar.Invoke( character );
        }
    }
}

}