using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace viva{

public delegate IEnumerator OutlineCoroutine( Outline.Entry outlineEntry, Color color );


public static class Outline
{
    public class Entry{
        public readonly Model model;
        public Coroutine outlineCoroutine;
        private readonly List<Material> lastTargetMats = new List<Material>();
        public global::Outline outline;
        public readonly VivaInstance source;

        public Entry( Model _model, VivaInstance _source ){
            model = _model;
            source = _source;

            if( model.renderer == null ) return;
                if( model.renderer.gameObject.TryGetComponent<global::Outline>( out outline ) ){
                    CancelDestroy( outline );
                }else{
                    outline = model.renderer.gameObject.AddComponent<global::Outline>();
                }
            if( outline == null ) return;
                outline.OutlineMode = global::Outline.Mode.OutlineVisible;
        }
        
        public void SetOutline( Color color, float width ){
            if( outline == null ) return;
            outline.OutlineColor = color;
            outline.OutlineWidth = width;
        }
    }

    private struct DestroyEntry{
        public Coroutine coroutine;
        public int id;

        public DestroyEntry( Coroutine _coroutine, int _id ){
            coroutine = _coroutine;
            id = _id;
        }
    }

    private static List<Entry> outlineEntries = new List<Entry>();
    private static List<DestroyEntry> destroyQueue = new List<DestroyEntry>();

    private static void QueueForDestroy( global::Outline outline ){
        if( outline == null ) return;

        var instanceId = outline.GetInstanceID();
        var entry = new DestroyEntry( Viva.main.StartCoroutine( DestroyOnUpdate( instanceId, outline ) ), instanceId );
        destroyQueue.Add( entry );
    }

    private static IEnumerator DestroyOnUpdate( int id, global::Outline outline ){
        yield return null;

        for( int i=destroyQueue.Count; i-->0; ){
            var entry = destroyQueue[i];
            if( entry.id == id ){
                destroyQueue.RemoveAt(i);
            }
        }
        if( outline ) GameObject.DestroyImmediate( outline );
    }

    private static void CancelDestroy( global::Outline outline ){
        if( outline == null ) return;
        for( int i=destroyQueue.Count; i-->0; ){
            var entry = destroyQueue[i];
            if( entry.id == outline.GetInstanceID() ){
                destroyQueue.RemoveAt(i);
                Viva.main.StopCoroutine( entry.coroutine );
            }
        }
    }

    public static Outline.Entry StartOutlining( Model model, VivaInstance source, Color color, OutlineCoroutine outlineCoroutine ){
        if( model == null ) return null;

        var entry = new Outline.Entry( model, source );
        if( outlineCoroutine != null ) entry.outlineCoroutine = Viva.main.StartCoroutine( outlineCoroutine( entry, color ) );
        outlineEntries.Add( entry );
        return entry;
    }
    
    public static void StopOutlining( Entry entry ){
        if( entry == null ) return;

        if( entry.outlineCoroutine != null ) Viva.main.StopCoroutine( entry.outlineCoroutine );
        QueueForDestroy( entry.outline );

        outlineEntries.Remove( entry );
    }

    public static IEnumerator Flash( Outline.Entry entry, Color color ){
        float timer = 0;
        float duration = 0.75f;
        while( timer < duration ){
            timer += Time.deltaTime;
            float ratio = 1.0f-Mathf.Clamp01( timer/duration );
            ratio = 1.0f-Mathf.Pow( ratio, 4 );
            entry.SetOutline( color*ratio, ratio*6 );
            yield return null;
        }
        Outline.StopOutlining( entry );
    }

    public static IEnumerator Constant( Entry outlineEntry, Color color ){
        while( true ){
            outlineEntry.SetOutline( color, Mathf.Sin( Time.time*8.0f )*2+4.0f );
            yield return null;
        }
    }
}

}