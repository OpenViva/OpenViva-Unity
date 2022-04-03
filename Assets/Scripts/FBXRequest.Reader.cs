using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Unity.Jobs;
using Unity.Collections;
using Fbx;


namespace viva{


public partial class FBXRequest: SpawnableImportRequest{

    private static void FixVec3ImportAxis( List<float> floats ){
        for( int vert=0; vert<floats.Count; vert+=3 ){
            floats[ vert+0 ] = -floats[ vert+0 ];
            float y = floats[ vert+1 ];
            floats[ vert+1 ] = floats[ vert+2 ];
            floats[ vert+2 ] = -y;
        }
    }
    
    private static void FixVec3ImportAxis( double[] doubles ){
        for( int vert=0; vert<doubles.Length; vert+=3 ){
            doubles[ vert+0 ] = -doubles[ vert+0 ];
            double y = doubles[ vert+1 ];
            doubles[ vert+1 ] = doubles[ vert+2 ];
            doubles[ vert+2 ] = -y;
        }
    }

    private static Vector3 FixVec3ImportAxis( Vector3 vec ){
        vec *= ExecuteReadFbx.blenderToUnityScale;
        vec.x = -vec.x;
        float temp = vec.y;
        vec.y = vec.z;
        vec.z = temp;
        return vec;
    }
    
    private static Quaternion FixImportEuler( Vector3 blenderEuler, float pitchOffset=0.0f ){
        var rot = Quaternion.Euler( pitchOffset, 0, 0 );
        rot *= Quaternion.AngleAxis( -blenderEuler.z, Vector3.forward );
        rot *= Quaternion.AngleAxis( -blenderEuler.y, Vector3.up );
        rot *= Quaternion.AngleAxis( -blenderEuler.x, -Vector3.right );
        return rot;
    }
}

}