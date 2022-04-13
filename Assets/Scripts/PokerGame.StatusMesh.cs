using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;




namespace viva{

public partial class PokerGame: Mechanism{
    
    public class Player{
        public readonly Character character;
        public GameObject inGameIndicator;
        public int requiredDrawCards = 0;
        public int requiredPlayCards = 0;

        public Player( Character _character ){
            character = _character;
        }

        private int CalculateCardCount( HandState handState ){
            PokerCard card = handState.GetItemIfHeld<PokerCard>();
            if( card == null ){
                return 0;
            }
            return card.deckContent.Length+card.fanGroupSize;
        }

        public int CalculateHeldCardCount(){
            int total = CalculateCardCount( character.rightHandState );
            total += CalculateCardCount( character.leftHandState );
            return total;
        }

        public PokerCard.Suit GetRandomSuitInHand( PokerGame game ){
            Set<PokerCard.Suit> suits = new Set<PokerCard.Suit>();
            var inHandContent = PokerCard.FindAllCardsLastOwnedByCharacter( game.masterDeck.cardPoolIndex, character );
            foreach( PokerCard ownedCard in inHandContent ){
                PokerCard.Suit suit = PokerCard.GetCardSuit( ownedCard.topValue );
                int cardValue = PokerCard.GetCardType( ownedCard.topValue );
                if( suit != PokerCard.Suit.JOKER ){
                    suits.Add( suit );
                }
            }
            if( suits.Count == 0 ){
                suits.Add( PokerCard.Suit.HEART );
                suits.Add( PokerCard.Suit.SPADE );
                suits.Add( PokerCard.Suit.DIAMOND );
                suits.Add( PokerCard.Suit.CLOVER );
            }
            return suits.objects[ UnityEngine.Random.Range( 0, suits.Count ) ];
        }
    }

    [SerializeField]
    private MeshFilter statusMeshFilter;

    private float statusAnim = 0.0f;

    public enum Status{
        NONE,
        DRAW,
        SKIP,
        WILD,
        RANDOMIZE,
        WINNER,
        TIE,
        HEART,
        SPADE,
        DIAMOND,
        CLOVER
    }

    public void UpdateStatusMeshAnimation(){

        statusAnim = Mathf.Clamp01( statusAnim+Time.deltaTime );
        
        statusMeshFilter.transform.rotation = Quaternion.LookRotation( Tools.FlatForward( statusMeshFilter.transform.position-GameDirector.instance.mainCamera.transform.position ), Vector3.up );
        float ease = 1.0f-Mathf.Pow( 1.0f-statusAnim, 4.0f );
        statusMeshFilter.transform.rotation *= Quaternion.Euler( 0.0f, 540.0f*(1.0f-ease), 0.0f );
        statusMeshFilter.transform.localScale = Vector3.one*ease;
    }

    public void SetStatusMesh( Status status, int parameter=1 ){

        if( status == Status.NONE ){
            statusMeshFilter.gameObject.SetActive( false );
            return;
        }
        statusMeshFilter.gameObject.SetActive( true );
        statusAnim = 0.0f;
        UpdateStatusMeshAnimation();
        
        Mesh mesh = new Mesh();
        int quads = 1;
        if( status == Status.DRAW ){
            parameter = Mathf.Clamp( parameter, 1, 10 );
            quads = 2;
            if( parameter == 10 ){
                quads++;
            }
        }
        Vector3[] vertices = new Vector3[ quads*2*3 ];
        Vector2[] uvs = new Vector2[ vertices.Length ];

        float size = 0.075f;
        int index = 0;
        float height = size*0.5f;

        switch( status ){
        case Status.DRAW:
            MeshBufferUtil.BufferXYQuad( Vector2.zero, new Vector2( 0.125f, -0.1875f ), 8, 11, new Vector3( -1.0f*size, height, 0 ), new Vector2( size, size*1.5f ), vertices, uvs, ref index );
            if( quads == 3 ){
                MeshBufferUtil.BufferXYQuad( Vector2.zero, new Vector2( 0.125f, -0.1875f ), 8, 9, new Vector3( 0.0f*size, height, 0 ), new Vector2( size, size*1.5f ), vertices, uvs, ref index );
                MeshBufferUtil.BufferXYQuad( Vector2.zero, new Vector2( 0.125f, -0.1875f ), 8, 10, new Vector3( 1.0f*size, height, 0 ), new Vector2( size, size*1.5f ), vertices, uvs, ref index );
            }else{
                MeshBufferUtil.BufferXYQuad( Vector2.zero, new Vector2( 0.125f, -0.1875f ), 8, parameter-1, new Vector3( 0.0f*size, height, 0 ), new Vector2( size, size*1.5f ), vertices, uvs, ref index );
            }
            break;
        case Status.SKIP:
            MeshBufferUtil.BufferXYQuad( new Vector2( 0.625f, 0.1875f ), new Vector2( 0.375f, -0.1875f ), 1, 0, new Vector3( -1.5f*size, height, 0 ), new Vector2( 3.0f*size, size*1.5f ), vertices, uvs, ref index );
            break;
        case Status.WILD:
            MeshBufferUtil.BufferXYQuad( new Vector2( 0.0f, 0.375f ), new Vector2( 0.5f, -0.1875f ), 1, 0, new Vector3( -2.0f*size, height, 0 ), new Vector2( 4.0f*size, size*1.5f ), vertices, uvs, ref index );
            break;
        case Status.RANDOMIZE:
            MeshBufferUtil.BufferXYQuad( new Vector2( 0.5f, 0.375f ), new Vector2( 0.5f, -0.1875f ), 1, 0, new Vector3( -2.0f*size, height, 0 ), new Vector2( 4.0f*size, size*1.5f ), vertices, uvs, ref index );
            break;
        case Status.WINNER:
            MeshBufferUtil.BufferXYQuad( new Vector2( 0.0f, 0.5625f ), new Vector2( 0.5f, -0.1875f ), 1, 0, new Vector3( -2.0f*size, height, 0 ), new Vector2( 4.0f*size, size*1.5f ), vertices, uvs, ref index );
            break;
        case Status.TIE:
            MeshBufferUtil.BufferXYQuad( new Vector2( 0.0f, 0.75f ), new Vector2( 0.5f, -0.1875f ), 1, 0, new Vector3( -1.5f*size, height, 0 ), new Vector2( 3.0f*size, size*1.5f ), vertices, uvs, ref index );
            break;
        case Status.HEART:
        case Status.SPADE:
        case Status.DIAMOND:
        case Status.CLOVER:
            MeshBufferUtil.BufferXYQuad( new Vector2( 0.5f, 0.5625f ), new Vector2( 0.125f, -0.1875f ), 4, (int)status-(int)Status.HEART, new Vector3( -0.5f*size, height, 0 ), new Vector2( size, size*1.5f ), vertices, uvs, ref index );
            break;
        }
        mesh.vertices = vertices;
        mesh.uv = uvs;

        int[] indices = new int[ quads*2*3 ];
		MeshBufferUtil.BuildTrianglesFromQuadPoints( indices, quads, 0, 0 );

        mesh.SetTriangles( indices, 0 );

        statusMeshFilter.mesh = mesh;
    }
}

}