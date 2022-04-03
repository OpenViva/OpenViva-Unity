using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Unity.Jobs;
using Unity.Collections;


namespace viva{

public class BufferUtil{

    public enum Header :byte{
        READ_LENGTH,
        ERROR_LENGTH,
    }

    public static int manifestHeaderCount { get{ return System.Enum.GetValues( typeof(Header) ).Length; } }


    public static byte[] ExtractHeaderError( NativeArray<byte> buffer, out string error ){
        error = null;
        int readLength = BufferUtil.GetManifestHeader( (int)Header.READ_LENGTH, buffer );
        int headerLength = BufferUtil.manifestHeaderCount*4;
        var byteSlice = new NativeSlice<byte>( buffer, headerLength, readLength-headerLength );
        
		byte[] result = new byte[ readLength-headerLength ];
        byteSlice.CopyTo( result ); //TODO: Optimize away byte copy

        int errorLength = BufferUtil.GetManifestHeader( (int)Header.ERROR_LENGTH, buffer );
        if( errorLength > 0 ){
            int errorStart = ((int)Header.ERROR_LENGTH+1)*4;
            error = System.Text.Encoding.ASCII.GetString( result, errorStart, result.Length-errorStart );
        }
        return result;
    }

    public static void IncreaseHeaderEntry( NativeArray<byte> result, ref int index, int type, int amount ){
        int byteIndex = ( (int)type )*4;
        byte[] bytes = {
            result[ byteIndex+3 ],
            result[ byteIndex+2 ],
            result[ byteIndex+1 ],
            result[ byteIndex   ]
        };
        int current = System.BitConverter.ToInt32( bytes, 0 );
        current += amount;
        bytes = System.BitConverter.GetBytes( current );
        result[ byteIndex+3 ] = bytes[0];
        result[ byteIndex+2 ] = bytes[1];
        result[ byteIndex+1 ] = bytes[2];
        result[ byteIndex   ] = bytes[3];
    }
    
    public static int GetManifestHeader( int type, NativeArray<byte> array ){
        int index = ( (int)type )*4;
        return System.BitConverter.ToInt32( new byte[]{
            array[ index+3 ],
            array[ index+2 ],
            array[ index+1 ],
            array[ index   ]
        }, 0 );
    }
    
    public static int GetManifestHeader( int type, byte[] array ){
        int index = ( (int)type )*4;
        return System.BitConverter.ToInt32( array, index );
    }

    public static void InitializeHeader( NativeArray<byte> result, ref int index, int headerCount ){
        for( index = 0; index<headerCount*4; index++ ){
            result[ index ] = 0;
        }
    }

    public static void WriteByte( NativeArray<byte> result, ref int index, byte b ){
        result[ index++ ] = b;
    }
    
    public static void WriteBytes( NativeArray<byte> result, ref int index, byte[] bytes ){
        for( int i=0; i<bytes.Length; i++ ){
            result[ index++ ] = bytes[i];
        }
    }

    public static void WriteInt( NativeArray<byte> result, ref int index, int i ){
        WriteBytes( result, ref index, System.BitConverter.GetBytes(i) );
    }

    public static void WriteShort( NativeArray<byte> result, ref int index, short i ){
        WriteBytes( result, ref index, System.BitConverter.GetBytes(i) );
    }

    public static void WriteFloat( NativeArray<byte> result, ref int index, float f ){
        WriteBytes( result, ref index, System.BitConverter.GetBytes(f) );
    }

    public static void WriteLong( NativeArray<byte> result, ref int index, long l ){
        WriteBytes( result, ref index, System.BitConverter.GetBytes(l) );
    }
    
    public static void WriteFloatArray( NativeArray<byte> result, ref int index, float[] array ){
        WriteInt( result, ref index, array.Length );
        foreach( float f in array ){
            WriteFloat( result, ref index, f );
        }
    }
    
    public static void WriteBoolArrayAsByteArray( NativeArray<byte> result, ref int index, bool[] array ){
        WriteInt( result, ref index, array.Length );
        foreach( bool b in array ){
            WriteByte( result, ref index, (byte)System.Convert.ToInt32(b) );
        }
    }
    
    public static void WriteDoubleArrayAsFloatArray( NativeArray<byte> result, ref int index, double[] array ){
        WriteInt( result, ref index, array.Length );
        foreach( double d in array ){
            WriteFloat( result, ref index, (float)d );
        }
    }
    
    public static void WriteFloatList( NativeArray<byte> result, ref int index, List<float> list ){
        WriteInt( result, ref index, list.Count );
        foreach( float f in list ){
            WriteFloat( result, ref index, f );
        }
    }
    
    public static void WriteIntArray( NativeArray<byte> result, ref int index, int[] array ){
        WriteInt( result, ref index, array.Length );
        foreach( int i in array ){
            WriteInt( result, ref index, i );
        }
    }
    
    public static void WriteIntList( NativeArray<byte> result, ref int index, List<int> array ){
        WriteInt( result, ref index, array.Count );
        foreach( int i in array ){
            WriteInt( result, ref index, i );
        }
    }

    public static void WriteString( NativeArray<byte> result, ref int index, string s ){
        var bytes = System.Text.Encoding.ASCII.GetBytes(s);
        WriteByte( result, ref index, (byte)bytes.Length );
        WriteBytes( result, ref index, bytes );
    }
}

}