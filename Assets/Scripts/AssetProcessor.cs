using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Unity.Jobs;
using Unity.Collections;



namespace viva{

public class AssetProcessor{

    public readonly List<ImportRequest> requests = new List<ImportRequest>();
    private int toImport = 0;
    private int toSpawn = 0;
    private GenericCallback onFinished;


    public AssetProcessor( List<ImportRequest> _requests ){
        requests = _requests;

        foreach( var request in requests ){
            var spawnableRequest = request as SpawnableImportRequest;
            
            request._internalOnImported += OnRequestImported;
            toImport++;
            if( spawnableRequest != null ){
                spawnableRequest.onAnyFinishedSpawning += OnRequestSpawned;
                toSpawn++;
            }
        }
    }
    
    public void ProcessAll( GenericCallback _onFinished=null ){
        onFinished = _onFinished;
        foreach( var request in requests ){
            GameUI.main.createMenu.AddImportRequest( request );
            request.Import();
        }
    }
    
    private void OnRequestImported(){
        toImport--;
        if( toImport == 0 ){
            foreach( var request in requests ){
                var spawnableRequest = request as SpawnableImportRequest;
                if( spawnableRequest != null ){
                    spawnableRequest._InternalSpawnUnlinked( false, new SpawnProgress() );
                }
            }
        }
    }

    private void OnRequestSpawned( object obj ){
        toSpawn--;
        if( toSpawn == 0 ){
            CombineAssets();
            onFinished?.Invoke();
        }
    }

    private List<T> GetAllRequestsOfType<T>( ImportRequestType type ) where T:ImportRequest{
        List<T> list = new List<T>();
        foreach( var req in requests ){
            if( req.type == type ){
                list.Add( (T)req );
            }
        }
        return list;
    }

    private void CombineAssets(){
        var textureRequests = GetAllRequestsOfType<TextureRequest>( ImportRequestType.TEXTURE );
        var modelRequests = GetAllRequestsOfType<FBXRequest>( ImportRequestType.FBX );
        foreach( var modelRequest in modelRequests ){
            if( !modelRequest.lastSpawnedFBX ) continue;
            foreach( var model in modelRequest.lastSpawnedFBX.rootModels ){
                CombineModelAssets( model, textureRequests );
            }
        }
        
    }

    private void CombineModelAssets( Model model, List<TextureRequest> textureRequests ){
        var bindings = new List<TextureBinding>();
        foreach( var texRequest in textureRequests ){
            var binding = model.textureBindingGroup.GenerateBinding( texRequest.handle );
            if( binding != null ) bindings.Add( binding );
        }
        foreach( var binding in bindings ){
            model.textureBindingGroup.Add( binding );
        }
        model.textureBindingGroup.Apply( false );
        
        foreach( var childModel in model.children ){
            CombineModelAssets( childModel, textureRequests );
        }
    }
}

}
