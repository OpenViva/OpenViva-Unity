
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;


namespace viva{


public static class AsyncUtil{

    // public class ReadFileRequest{
    //     public readonly string relativePath;
    //     public FileStream result;
    //     public bool finished;

    //     public ReadFileRequest( string _filepath ){
    //         relativePath = _filepath;
    //     }
    // }

    // public static async Task<ReadFileRequest> ReadFile( ReadFileRequest request ){
        
    //     string path = request.relativePath;
    //     if( !System.IO.File.Exists( path ) ){
    //         return null;
    //     }
    //     return await System.Threading.Tasks.Task.Run(
    //         () =>
    //         {
    //             try{
    //                 request.result = File.OpenRead( path );
    //             }catch( System.Exception e ){
    //             }
    //             request.finished = true;
    //             return request;
    //         }
    //     );
    // }
}

}