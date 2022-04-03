using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace viva{

/// <summary> Class used for simple on/off permission based limit
/// If there are any entries added the group's allow value is false, true otherwise
public struct BoolLimitGroup{

    private readonly List<object> limits;

    /// <summary> The value of the overall group. If no limits are present, the limit is 1.0f, otherwise it is the lowest limit value. </summary>
    public bool allow { get{ return limits.Count==0; } }


    public BoolLimitGroup( List<object> _limits ){
        limits = _limits;
    }

    /// <summary> Creates a bool entry with the specified tag. </summary>
    /// <param name="tag"> The tag that represents who added the tag. This is so it can be later removed using this object as a tag.</param>
    public void Add( object tag ){
        if( tag == null ) return;
        if( limits.Contains( tag ) ) return;
        limits.Add( tag );
    }

    /// <summary> Removes a bool entry with the specified tag. </summary>
    /// <param name="tag"> The tag to search and remove.</param>
    public void Remove( object tag ){
        if( tag == null ) return;
        limits.Remove( tag );
    }

    /// <summary> Removes all bool entries. </summary>
    public void Reset(){
        limits.Clear();
    }
}

}