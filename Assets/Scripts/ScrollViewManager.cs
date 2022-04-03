using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace viva{


public class ScrollViewManager : MonoBehaviour{
    
    public delegate void TransformReturnFunc( Transform prefab );

    [SerializeField]
    private GameObject scrollButtonEntry;
    [SerializeField]
    public RectTransform scrollContent;
    private GameObject lastEntryPrefab = null;


    public void SetContentCount( int count, TransformReturnFunc onScrollEntryStart, GameObject overrideScrollButtonEntry=null ){
        if( scrollContent == null ) return;

        var entryPrefab = overrideScrollButtonEntry ? overrideScrollButtonEntry : scrollButtonEntry;
        var reset = false;
        if( lastEntryPrefab != entryPrefab ){
            lastEntryPrefab = entryPrefab;
            reset = true;
        }
        //destroy excess objects
        int low = reset?0:count;
        for( int i=scrollContent.childCount; i--> low; ){
            GameObject.DestroyImmediate( scrollContent.GetChild(i).gameObject );
        }
        AdditionalContentCount( count, count-scrollContent.childCount, onScrollEntryStart, overrideScrollButtonEntry );
    }

    public void AdditionalContentCount( int end, int count, TransformReturnFunc onScrollEntryStart, GameObject overrideScrollButtonEntry=null ){
        if( scrollContent == null ) return;

        var entryPrefab = overrideScrollButtonEntry ? overrideScrollButtonEntry : scrollButtonEntry;

        for( int i=0; i<count; i++ ){
            GameObject entry = GameObject.Instantiate( entryPrefab, scrollContent );
            entry.transform.localRotation = Quaternion.identity;
        }
        if( onScrollEntryStart != null ){
            for( int i=0; i<end; i++ ){
                onScrollEntryStart( scrollContent.GetChild( i ) );
            }
        }
    }

    public Button GetButtonAt( int index ){
        if( index < 0 || index > scrollContent.childCount ){
            return null;
        }
        return scrollContent.GetChild( index ).GetComponent<Button>();
    }
}

}