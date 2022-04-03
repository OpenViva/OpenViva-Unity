using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace viva{

/// <summary> Class used for attaching multiple percent limits. The limit with the lowest value will be used in whatever it limits.
/// Whenever a new limit is added or removed, the overall group value is recalculated.
[System.Serializable]
public class LimitGroup{

    public struct LimitEntry{
        public string tag;
        public float limit;

        public LimitEntry( string _tag, float _limit ){
            tag = _tag;
            limit = _limit;
        }
    }

    private struct CoroutineEntry{
        public Coroutine coroutine;
        public string tag;

        public CoroutineEntry( Coroutine _coroutine, string _tag ){
            coroutine = _coroutine;
            tag = _tag;
        }
    }

    private readonly List<LimitEntry> limits = new List<LimitEntry>();
    private readonly GenericCallback onUpdate;
    private readonly float min;
    private readonly float max;
    private readonly bool multiply;
    private List<CoroutineEntry> coroutineEntries = new List<CoroutineEntry>();

#if UNITY_EDITOR
    [Range(0f,1f)] public float OVERRIDE = 1.0f;
    private float m_value = 1.0f;
    public float value {
    get{
        if( OVERRIDE < 1.0f ) return OVERRIDE;
        return m_value;
    }
    private set{ m_value = value; } }
#else
    /// <summary> The value of the overall group. If no limits are present, the limit is 1.0f, otherwise it is the lowest limit value. </summary>
    public float value { get; private set; } = 1.0f;    //default
#endif


    public LimitGroup( GenericCallback _onUpdate=null, float _min=-1, float _max=1, bool _multiply=false ){
        onUpdate = _onUpdate;
        min = _min;
        max = _max;
        multiply = _multiply;
    }

    /// <summary> Creates or updates a limit entry with the specified tag. </summary>
    /// <param name="tag"> The tag that represents who added this limit. This is so it can be later removed using this string as a tag.</param>
    /// <param name="value"> The value of the limit. Value will be clamped between 0 and 1.</param>
    public void Add( string tag, float value ){
        if( tag == null ) return;
        for( int i=0; i<limits.Count; i++ ){
            var limit = limits[i];
            if( limit.tag == tag ){
                limit.limit = Mathf.Clamp( value, min, max );
                limits[i] = limit;
                StopAnimations( tag );
                Update();
                return;
            }
        }
        limits.Add( new LimitEntry( tag, Mathf.Clamp( value, min, max ) ) );
        Update();
    }
    /// <summary> Updates a limit entry value if the tag is already present in the group. </summary>
    /// <param name="tag"> The tag to search for. </param>
    /// <param name="value"> The new value of the limit. Value will be clamped between 0 and 1.</param>
    public void Set( string tag, float value ){
        if( tag == null ) return;
        for( int i=0; i<limits.Count; i++ ){
            var limit = limits[i];
            if( limit.tag == tag ){
                limit.limit = Mathf.Clamp( value, min, max );
                limits[i] = limit;
                StopAnimations( tag );
                Update();
                return;
            }
        }
    }

    /// <summary> Removes a limit entry with the specified tag. </summary>
    /// <param name="tag"> The tag to search and remove.</param>
    public void Remove( string tag ){
        if( tag == null ) return;
        for( int i=0; i<limits.Count; i++ ){
            var limit = limits[i];
            if( limits[i].tag == tag ){
                limits.RemoveAt(i);
                StopAnimations( tag );
                Update();
                break;
            }
        }
    }

    public float? Get( string tag ){
        if( tag == null ) return null;
        for( int i=0; i<limits.Count; i++ ){
            var limit = limits[i];
            if( limits[i].tag == tag ){
                return limits[i].limit;
            }
        }
        return null;
    }

    public void Animate( string tag, float from, float to, float duration ){
        duration = Mathf.Min( duration, 10 );
        StopAnimations( tag );
        Add( tag, from );
        Viva.main.StartCoroutine( AnimateCoroutine( tag, from, to, duration ) );
    }

    private IEnumerator AnimateCoroutine( string tag, float from, float to, float duration ){
        
        float timer = 0;
        while( timer < duration ){
            yield return null;
            timer += Time.deltaTime;
            float ratio = Mathf.Clamp01( timer/duration );
            Set( tag, Mathf.LerpUnclamped( from, to, ratio ) );
        }
        StopAnimations( tag );
    }

    private void StopAnimations( string tag ){
        for( int i=0; i<coroutineEntries.Count; i++ ){
            var coroutineEntry = coroutineEntries[i];
            if( coroutineEntry.tag == tag ){
                coroutineEntries.RemoveAt(i);
                Viva.main.StopCoroutine( coroutineEntry.coroutine );
                return;
            }
        }
    }

    /// <summary> Removes all limits. </summary>
    public void _InternalReset(){
        foreach( var coroutineEntry in coroutineEntries ){
            Viva.main.StopCoroutine( coroutineEntry.coroutine );
        }
        coroutineEntries.Clear();

        limits.Clear();
        Update();
    }

    /// <summary> Recalculates the overall group value. This is done automatically already. </summary>
    public void Update(){
        value = 1.0f;
        if( multiply ){
            foreach( var limit in limits ){
                value *= limit.limit;
            }
        }else{
            foreach( var limit in limits ){
                value = Mathf.Min( limit.limit, value );
            }
        }
        onUpdate?.Invoke();
    }

    public override string ToString(){
        var s = "";
        foreach( var limit in limits ){
            s += limit.tag+"@"+limit.limit+" ";
        }
        return s;
    }
}

}