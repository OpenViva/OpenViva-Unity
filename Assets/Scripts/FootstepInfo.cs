using UnityEngine;



namespace viva{


public class FootstepInfo: MonoBehaviour {

    public enum Type{
        DIRT,   //default
        WOOD,
        TILE,
        WATER,
        STONE,
        STONE_WET
    }

    [SerializeField]
    private SoundSet[] m_sounds = new SoundSet[ System.Enum.GetValues(typeof(FootstepInfo.Type)).Length ];
    public SoundSet[] sounds { get{ return m_sounds; } }

    [HideInInspector]
    public Vector3 lastFloorPos = Vector2.zero;
    private Type m_currentType = Type.DIRT;
    public Type currentType { get{ return m_currentType; } }
    private int[] footstepRegionCount = new int[ System.Enum.GetValues(typeof(Type)).Length ];


    public void AddtoFootstepRegion( Type footstep ){
        m_currentType = footstep;
        footstepRegionCount[ (int)footstep ]++;
    }

    public void RemoveFromFootstepRegion( Type footstep ){
        footstepRegionCount[ (int)footstep ]--;
        if( footstepRegionCount[ (int)footstep] == 0 ){
            m_currentType = Type.DIRT;
            for( int i=0; i<footstepRegionCount.Length; i++ ){
                if( footstepRegionCount[i] > 0 ){
                    m_currentType = (Type)i;
                    break;
                }
            }
        }
    }
}

}