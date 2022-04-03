using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Unity.Jobs;
using Unity.Collections;
using Fbx;



namespace viva{


public class ByteBufferedReader{
    
    private readonly byte[] buffer;
    public int index;
    public bool finished { get{ return index >= buffer.Length; } }
    public int bufferLength { get{ return buffer.Length; } }
    public FBXRequest.Read nextType { get; private set; }

    public ByteBufferedReader( byte[] _buffer ){
        buffer = _buffer;
        index = 0;
    }

    public void ReadNextType(){
        nextType = (FBXRequest.Read)buffer[ index++ ];
    }
    public byte ReadNextByte(){
        return buffer[ index++ ];
    }
    public short ReadNextShort(){
        short result = System.BitConverter.ToInt16( buffer, index );
        index += 2;
        return result;
    }
    public int ReadNextInt(){
        int result = System.BitConverter.ToInt32( buffer, index );
        index += 4;
        return result;
    }
    public int[] ReadNextIntArray(){
        var ints = new int[ ReadNextInt() ];
        for( int i=0; i<ints.Length; i++ ){
            ints[i] = ReadNextInt();
        }
        return ints;
    }
    public float[] ReadNextFloatArray(){
        var ints = new float[ ReadNextInt() ];
        for( int i=0; i<ints.Length; i++ ){
            ints[i] = ReadNextFloat();
        }
        return ints;
    }
    public byte[] ReadNextByteArray(){
        var bytes = new byte[ ReadNextInt() ];
        for( int i=0; i<bytes.Length; i++ ){
            bytes[i] = ReadNextByte();
        }
        return bytes;
    }
    public Matrix4x4 ReadNextFloatArrayAsMatrix(){
        ReadNextInt();  //16
        var matrix = new Matrix4x4(
            new Vector4( ReadNextFloat(), ReadNextFloat(), ReadNextFloat(), ReadNextFloat() ),
            new Vector4( ReadNextFloat(), ReadNextFloat(), ReadNextFloat(), ReadNextFloat() ),
            new Vector4( ReadNextFloat(), ReadNextFloat(), ReadNextFloat(), ReadNextFloat() ),
            new Vector4( ReadNextFloat(), ReadNextFloat(), ReadNextFloat(), ReadNextFloat() )
        );
        return matrix;
    }
    public void ReadNextFloatArrayAsTransform( out Vector3 pos, out Vector3 euler, out Vector3 scale ){
        pos = new Vector3( ReadNextFloat(), ReadNextFloat(), ReadNextFloat() );
        euler = new Vector3( ReadNextFloat(), ReadNextFloat(), ReadNextFloat() );
        scale = new Vector3( ReadNextFloat(), ReadNextFloat(), ReadNextFloat() );
    }
    public Vector3[] ReadNextFloatArrayAsVector3Array(){
        var vecs = new Vector3[ ReadNextInt()/3 ];
        for( int i=0; i<vecs.Length; i++ ){
            var vec = new Vector3(
                ReadNextFloat(),
                ReadNextFloat(),
                ReadNextFloat()
            );
            vecs[i] = vec;
        }
        return vecs;
    }
    public Vector2[] ReadNextFloatArrayAsVector2Array(){
        var vecs = new Vector2[ ReadNextInt()/2 ];
        for( int i=0; i<vecs.Length; i++ ){
            var vec = new Vector2(
                ReadNextFloat(),
                ReadNextFloat()
            );
            vecs[i] = vec;
        }
        return vecs;
    }
    public float ReadNextFloat(){
        float result = System.BitConverter.ToSingle( buffer, index );
        index += 4;
        return result;
    }
    public Vector3 ReadNextFloatAsVector3(){
        return new Vector3( ReadNextFloat(), ReadNextFloat(), ReadNextFloat() );
    }
    public long ReadNextLong(){
        long result = System.BitConverter.ToInt64( buffer, index );
        index += 8;
        return result;
    }
    public string ReadNextString(){
        int length = buffer[ index++ ]; //strings must be less than 128 characters
        if( length == 0 ) return "";
        var result = System.Text.Encoding.ASCII.GetString( buffer, index, length );
        index += length;
        return result;
    }
    public GameObject ReadNewGameObject( FBXRequest.Manifest manifest, FBXRequest.Read _type ){
        return ReadNewGameObject( ReadNextShort(), manifest, _type );
    }
    
    public GameObject ReadNewGameObject( short id, FBXRequest.Manifest manifest, FBXRequest.Read _type ){
        var container = new GameObject( ReadNextString() );
        container.transform.position = ReadNextFloatAsVector3();
        container.transform.eulerAngles = ReadNextFloatAsVector3();
        container.transform.localScale = ReadNextFloatAsVector3();

        manifest.transformEntry[ id ] = new FBXRequest.TransformEntry( container.transform, _type );

        container.transform.rotation *= Quaternion.Euler( 90, 0, 0 );   //import fix
        return container;
    }
}

}