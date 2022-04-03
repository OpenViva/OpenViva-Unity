using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Unity.Jobs;



namespace viva{

public class Usage{
    public bool discarded { get; private set; } = false;
    public int usage { get; private set; } = 0;
    public GenericCallback onDiscarded;
    private readonly bool manualMemory;
    // private string tag;

    public void Decrease(){
        if( !discarded ){
            usage--;
            // if( tag != null ) Debug.LogError(tag+"="+usage);
            if( usage <= 0 && !manualMemory ){
                discarded = true;
                onDiscarded?.Invoke();
            }
        }
    }
    public void Increase(){
        if( !discarded ){
            usage++;
            // if( tag != null ) Debug.LogError(tag+"="+usage);
        }
    }

    public Usage( bool _manualMemory=false, string _tag=null ){
        manualMemory = _manualMemory;
        // tag = _tag;
    }
}

public abstract class VivaDisposable{

    public Usage usage { get; protected set; } = null;

    public VivaDisposable( bool _manualMemory=false, string _tag=null ){
        usage = new Usage(_manualMemory,_tag);
    }
}

}