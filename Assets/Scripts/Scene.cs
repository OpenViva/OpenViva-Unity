using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using Unity.Jobs.LowLevel.Unsafe;


namespace viva{

public class Scene : VivaInstance
{
    private static bool firstRun = true;

    [SerializeField]
    private Viva vivaPrefab;

    public static Scene main { get; private set; }
    public static bool disableBakeQueues = false;
    

    public SceneSettings sceneSettings { get{ return _internalSettings as SceneSettings; } }

    public static Scene CreateSceneBase( SceneSettings sceneSettings ){
        GameObject container = new GameObject("Scene");
        
        var scene = VivaInstance.CreateVivaInstance<Scene>( container, sceneSettings, System.Int32.MinValue );  //always first object
        scene.name = "Scene - "+sceneSettings.name;
        scene.min = sceneSettings.sceneData.min;
        scene.max = sceneSettings.sceneData.max;
        scene.respawn = GameObject.Find("RESPAWN");
        if( scene.respawn == null ) Debugger.LogError("No RESPAWN transform found");

        scene._InternalInitialize();

        var navMeshBuildSettings = scene.navTile.surface.GetBuildSettings();
        navMeshBuildSettings.agentClimb = 0.25f;
        navMeshBuildSettings.agentHeight = 1.5f;
        navMeshBuildSettings.agentRadius = 0.23f;
        navMeshBuildSettings.agentSlope = 45f;
        navMeshBuildSettings.maxJobWorkers = (uint)Mathf.Clamp( VivaSettings.main.bakeNavJobs, 0, (int)JobsUtility.JobWorkerCount );
        navMeshBuildSettings.minRegionArea = 2f;
        navMeshBuildSettings.overrideTileSize = true;
        navMeshBuildSettings.tileSize = 96;
        navMeshBuildSettings.minRegionArea = 4;
        navMeshBuildSettings.overrideVoxelSize = true;
        navMeshBuildSettings.voxelSize = 0.13f;

        var scripts = new List<string>();
        if( sceneSettings.sceneData.serializedScripts != null ){
            foreach( var serializedScript in sceneSettings.sceneData.serializedScripts ){
                scripts.Add( serializedScript.script );
            }
        }

        scene.scriptManager.LoadAllScripts( "scenes", scripts.ToArray() );

        return scene;
    }

    public Vector3 min { get; private set; }
    public Vector3 max { get; private set; }
    public GameObject respawn { get; private set; }
    public NavTile navTile { get; private set; }
    private float bottomFloor;
    
    
    public override float? CalculateApproximateRadius(){
        return null;
    }
    public override float? CalculateApproximateFloorY(){
        return null;
    }

    public override void TeleportTo( Vector3 position, Quaternion rotation ){}

    public override void _InternalInitialize(){
        BuildBoundaryWalls();

        navTile = NavTile.CreateNavTile( 0, 0, 1, 1, min, max );

        navTile.surface.navMeshData = new NavMeshData();
    }

    public override void _InternalReset(){
    }

    private void Awake(){
        main = this;
        gameObject.layer = 0;

        enabled = false;

        if( firstRun ){
            FirstRun();
        }
    }

    public void FirstRun(){
        firstRun = false;
        GameObject.Instantiate( vivaPrefab );
        
        var mainScenePath = System.IO.Path.GetFullPath( Profile.root +"/main.viva" );
        var request = ImportRequest.CreateRequest( mainScenePath ) as SceneRequest;
        request.Import();
        request._InternalSpawnUnlinked( false, new SpawnProgress() );
    }

    private void BuildBoundaryWalls(){
        //build collision walls
        var center = ( min+max )/2;
        var size = max-min;

        //add padding for reset bottom
        size.y += 4;
        center.y -= 2;

        var halfSize = size/2;
        float thickness = 32;
        float halfThickness = thickness/2;

        var left = gameObject.AddComponent<BoxCollider>();
        left.center = center+new Vector3( -halfSize.x-halfThickness, 0, 0 );
        left.size = new Vector3( thickness, size.y, size.z+thickness*2 );
        
        var right = gameObject.AddComponent<BoxCollider>();
        right.center = center+new Vector3( halfSize.x+halfThickness, 0, 0 );
        right.size = new Vector3( thickness, size.y, size.z+thickness*2 );
        
        var front = gameObject.AddComponent<BoxCollider>();
        front.center = center+new Vector3( 0, 0, -halfSize.x-halfThickness );
        front.size = new Vector3( size.z+thickness*2, size.y, thickness );
        
        var back = gameObject.AddComponent<BoxCollider>();
        back.center = center+new Vector3( 0, 0, halfSize.x+halfThickness );
        back.size = new Vector3( size.z+thickness*2, size.y, thickness );
        
        var bottom = gameObject.AddComponent<BoxCollider>();
        bottom.center = center+new Vector3( 0, -halfSize.y-halfThickness, 0 );
        bottom.size = new Vector3( size.x+thickness*2, thickness, size.z+thickness*2 );
        
        var top = gameObject.AddComponent<BoxCollider>();
        top.center = center+new Vector3( 0, halfSize.y+halfThickness, 0 );
        top.size = new Vector3( size.x+thickness*2, thickness, size.z+thickness*2 );

        var resetTouch = gameObject.AddComponent<BoxCollider>();
        resetTouch.isTrigger = true;
        resetTouch.center = center+new Vector3( 0, -halfSize.y+2, 0 );
        resetTouch.size = new Vector3( size.x, 4, size.z );
        bottomFloor = center.y-halfSize.y+4;

        var navModifier = gameObject.AddComponent<NavMeshModifier>();
        navModifier.ignoreFromBuild = true;
    }
    
    public void BakeNavigation(){
        if( disableBakeQueues ) return;
        if( this ) enabled = true;
    }

    private void OnDrawGizmosSelected(){
        
        Gizmos.DrawIcon( respawn ? respawn.transform.position : Vector3.zero, "AvatarSelector", false, Color.green );

        var center = ( min+max )/2;
        Gizmos.color = new Color( 1.0f, 0, 0, 0.25f );
        Gizmos.DrawCube( center, (max-min)*-1f  );
    }

    private void OnTriggerEnter( Collider collider ){
        var instance = collider.GetComponentInParent<VivaInstance>();
        if( !instance ) return;

        if( collider.bounds.min.y > bottomFloor ) return;

        instance.TeleportTo( respawn ? respawn.transform.position : Vector3.zero, Quaternion.identity );
    }

    private void FixedUpdate(){
        enabled = false;

        navTile?.BakeNavMesh();
    }
}

}