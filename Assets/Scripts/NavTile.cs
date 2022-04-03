using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;
using Unity.AI.Navigation;


namespace viva{

public class NavTile : MonoBehaviour
{
    [System.Serializable]
    private class CachedNavMeshSource{
        public NavMeshBuildSourceShape shape;
        public Matrix4x4 transform;
        public Object sourceObject;
        public Vector3 size;
    }


    public static NavTile CreateNavTile( int x, int z, int terrainNavTilesX, int terrainNavTilesZ, Vector3 min, Vector3 max){
        GameObject target = new GameObject("navTile_" + x + "," + z);
        target.layer = WorldUtil.navLayer;
        target.isStatic = true;

        float unitX = 1f / (float)terrainNavTilesX * (max.x - min.x);
        float unitZ = 1f / (float)terrainNavTilesZ * (max.z - min.z);
        NavTile navTile = target.AddComponent<NavTile>();
        NavMeshSurface surface = target.AddComponent<NavMeshSurface>();
        navTile.surface = surface;

        surface.layerMask = WorldUtil.defaultMask|WorldUtil.itemsStaticMask|WorldUtil.waterMask|WorldUtil.navMask;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.collectObjects = CollectObjects.Volume;
        surface.center = ( min+max )/2f;
        surface.size = max-min;

        // List<CachedNavMeshSource> cachedColliders = new List<CachedNavMeshSource>();
        // Collider[] array = Physics.OverlapBox( surface.center, surface.size / 2f, Quaternion.identity, WorldUtil.defaultMask, QueryTriggerInteraction.Ignore);
        // foreach (Collider collider in array)
        // {
        //     if (!collider.gameObject.isStatic) continue;

        //     CachedNavMeshSource source = new CachedNavMeshSource();

        //     MeshCollider meshCollider = (MeshCollider)(object)((collider is MeshCollider) ? collider : null);
        //     if (meshCollider ){
        //         source.shape = (NavMeshBuildSourceShape)0;
        //         source.transform = collider.transform.localToWorldMatrix;
        //         source.sourceObject = meshCollider.sharedMesh;
        //         cachedColliders.Add(source);
        //         continue;
        //     }
        //     BoxCollider boxCollider = (BoxCollider)(object)((collider is BoxCollider) ? collider : null);
        //     if (boxCollider){
        //         source.shape = (NavMeshBuildSourceShape)2;
        //         source.transform = Matrix4x4.TRS( boxCollider.transform.TransformPoint(boxCollider.center), boxCollider.transform.rotation, boxCollider.transform.lossyScale );
        //         source.size = boxCollider.size;
        //         cachedColliders.Add(source);
        //     }
        //     var terrainCollider = collider as TerrainCollider;
        //     if ( terrainCollider ){
        //         source.shape = (NavMeshBuildSourceShape)1;
        //         source.transform = Matrix4x4.TRS( collider.transform.position, Quaternion.identity, Vector3.one );
        //         source.sourceObject = terrainCollider.terrainData;
        //         cachedColliders.Add(source);
        //     }
        // }
        // navTile.cachedSources = cachedColliders.ToArray();

        var colliderHelper = target.AddComponent<BoxCollider>();
        colliderHelper.center = surface.center;
        colliderHelper.size = surface.size;

        return navTile;
    }


    [SerializeField]
    public NavMeshSurface surface;
    [SerializeField]
    private CachedNavMeshSource[] cachedSources = new CachedNavMeshSource[0];

    private bool isBaking = false;

    
    public void BakeNavMesh( GenericCallback onFinished=null ){
        if( isBaking ) return;
        isBaking = true;

        //only search for items
        // var bounds = new Bounds( surface.transform.TransformPoint( surface.center ), surface.transform.TransformVector( surface.size/2 ) );
        // var newSources = new List<NavMeshBuildSource>();
        // foreach( var cachedSource in cachedSources ){
        //     newSources.Add( new NavMeshBuildSource(){
        //         shape = cachedSource.shape,
        //         transform = cachedSource.transform,
        //         sourceObject = cachedSource.sourceObject,
        //         size = cachedSource.size,
        //         area = 0
        //     });
        // }
        // int oldCount = newSources.Count;

        // var colliders = Physics.OverlapBox( bounds.center, bounds.size, Quaternion.identity, WorldUtil.itemsMask, QueryTriggerInteraction.Ignore );
        // foreach( var collider in colliders ){
        //     if( !collider.gameObject.isStatic ) continue;
            
        //     var item = collider.GetComponentInParent<Item>();
        //     Debug.LogError( collider?.name+" = "+ item?.name);
        //     if( item ) Debug.LogError( collider.name );
        //     if( item && !item.immovable ) continue;

        //     NavMeshBuildSource source = new NavMeshBuildSource();
        //     var meshCollider = collider as MeshCollider;
        //     if( meshCollider ){
        //         source.shape = NavMeshBuildSourceShape.Mesh;
        //         source.area = 0;
        //         source.transform = collider.transform.localToWorldMatrix;
        //         source.sourceObject = meshCollider.sharedMesh;
        //         newSources.Add( source );
        //         continue;
        //     }
        //     var boxCollider = collider as BoxCollider;
        //     if( boxCollider ){
        //         source.shape = NavMeshBuildSourceShape.Box;
        //         source.area = 0;
        //         source.transform = Matrix4x4.TRS( boxCollider.transform.TransformPoint( boxCollider.center ), boxCollider.transform.rotation, boxCollider.transform.lossyScale );
        //         source.size = boxCollider.size;
        //         newSources.Add( source );
        //         continue;
        //     }
        // }
        // Debug.Log("Updating "+name+" with "+newSources.Count+" new sources (+"+(newSources.Count-oldCount)+")");
        // var test = new Bounds( bounds.center, bounds.size*2 );

        surface.RemoveData();
        var task = surface.UpdateNavMesh( surface.navMeshData );
        //UpdateNavMeshDataAsync does not work in runtime builds >:|
        // var task = UnityEngine.AI.NavMeshBuilder.UpdateNavMeshDataAsync( surface.navMeshData, Scene.main ? Scene.main.navMeshBuildSettings : surface.GetBuildSettings(), newSources, test );
        task.completed += delegate{
            isBaking = false;
            onFinished?.Invoke();
            surface.AddData();
        };
    }
}

}