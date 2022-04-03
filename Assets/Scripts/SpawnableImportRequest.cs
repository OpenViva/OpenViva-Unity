using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Unity.Jobs;
using Unity.Collections;


namespace viva{

public class SpawnProgress{
    public readonly ObjectCallback callback;
    public bool finished;
    public string error;
    public object result;

    public SpawnProgress( ObjectCallback _callback=null ){
        callback = _callback;
        finished = false;
        error = null;
        result = null;
    }
}

public delegate void SpawnProgressCallback( SpawnProgress progress );

public delegate void ObjectCallback( object obj );

public abstract class SpawnableImportRequest : ImportRequest{

    public ObjectCallback onAnyFinishedSpawning;
    // public bool isPreloading { get; private set; } = false;
    public string preloadError { get; protected set; }
    public bool preloaded { get; private set; } = false;


    public SpawnableImportRequest( string _filepath, ImportRequestType _type ):base(_filepath,_type){
    }
    
    //Does NOT link to instances array, use for auxillary objects (import visuals, non-editable items, etc.)
    public virtual void EnableThumbnailGenerationOnEdit(){}
    protected abstract IEnumerator OnPreload();
    protected abstract void OnPreloadInstant();
    protected abstract IEnumerator OnSpawn( SpawnProgress progress );
    protected abstract void OnSpawnInstant( SpawnProgress progress );




    public void _InternalSpawnUnlinked( bool instant, SpawnProgress progress ){
        if( instant ){
            SpawnInstant( progress );
        }else{
            Viva.main.StartCoroutine( Spawn( progress ) );
        }
    }

    public IEnumerator Preload(){
        if( !imported ){    //skip if already preloaded
            Import( false );
            if( !imported ){
                preloadError = "Could not preload \""+filepath+"\" because "+importError;
                yield break;
            }
        }

        if( preloaded ) yield break;    //skip if already preloaded
        preloadError = null;
        preloaded = false;
        
        yield return OnPreload();

        if( preloadError == null ) preloaded = true;
    }

    public void PreloadInstant(){
        if( !imported ){    //skip if already preloaded
            Import( false );
            if( !imported ){
                preloadError = "Could not preload \""+filepath+"\" because "+importError;
                return;
            }
        }

        if( preloaded ) return;    //skip if already preloaded
        preloadError = null;
        preloaded = false;
        
        OnPreloadInstant();

        if( preloadError == null ) preloaded = true;
    }

    private void SpawnInstant( SpawnProgress progress ){
        if( !preloaded ){
            PreloadInstant();
        }
        if( !preloaded ){
            FinishSpawning( progress, null, preloadError );
            return;
        }
        OnSpawnInstant( progress );
    }

    private IEnumerator Spawn( SpawnProgress progress ){
        if( !preloaded ){
            yield return Preload();
        }
        if( !preloaded ){
            FinishSpawning( progress, null, preloadError );
            yield break;
        }
        yield return OnSpawn( progress);
    }

    protected void FinishSpawning( SpawnProgress progress, object spawnedInstance, string error ){
        progress.finished = true;
        progress.error = error;
        if( error != null ){
            Debugger.LogError( error );
        }else{
            progress.result = spawnedInstance;
        }
        onAnyFinishedSpawning?.Invoke( spawnedInstance );
        progress.callback?.Invoke( spawnedInstance );
    }
}

}