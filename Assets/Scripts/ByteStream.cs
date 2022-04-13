using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;


namespace viva{

    public class ByteStreamReader{
        
        private byte[] data = null;
        public int index = 0;

        public ByteStreamReader( string filepath ){
            data = File.ReadAllBytes( filepath );
        }

        public ByteStreamReader( byte[] _data ){
            data = _data;
            if( data == null ){
                throw new System.Exception("ByteStreamReader received a null array!");
            }
        }

        public void Skip( int indices ){
            index += indices;
        }

        public int ReadSigned4ByteInt(){
            byte[] result = new byte[4];
            result[0] = data[index++];
            result[1] = data[index++];
            result[2] = data[index++];
            result[3] = data[index++];
            return System.BitConverter.ToInt32( result, 0 );
        }

        public byte ReadUnsigned1ByteInt(){
            return data[index++];
        }
        public ushort ReadUnsigned2ByteInt(){
            ushort v = System.BitConverter.ToUInt16( data, index );
            index += 2;
            return v;
        }
        
        public float ReadFloat(){
            float v = System.BitConverter.ToSingle( data, index );
            index += 4;
            return v;
        }
        
        public float Read1ByteNormalFloat(){// -1~1
            return ( (float)data[index++]/255.0f )*2.0f-1.0f;
        }

        public float ReadUnsigned1ByteNormalFloat(){// 0~1
            return (float)data[index++]/255.0f;
        }

        public string ReadUTF8String( int length ){
            string result = Tools.UTF8ByteArrayToString( data, index, length );
            index += length;
            return result;
        }

        public byte[] ReadBytes( int length ){
            byte[] result = new byte[ length ];
            for( int i=0; i<length; i++ ){
                result[i] = data[index++];
            }
            return result;
        }
    }

    public class ByteStreamWriter{
        
        private byte[] data = null;
        public int index = 0;
        private readonly int growSize;

        public ByteStreamWriter( int startSize, int _growSize ){
            data = new byte[ startSize ];
            growSize = _growSize;
        }

        private void Allocate( int amount ){
            if( index+amount >= data.Length ){
                byte[] newData = new byte[ data.Length+growSize+amount ];
                System.Array.Copy( data, newData, index );
                data = newData;
            }
        }
        
        public void WriteByte( byte b ){
            Allocate(1);
            data[index++] = b;
        }

        public void Write4ByteFloat( float f ){

            Allocate( 4 );
            
            byte[] floatBytes = System.BitConverter.GetBytes( f );
            System.Array.Copy( floatBytes, 0, data, index, 4 );
            index += 4;
        }

        public void WriteByteArray( byte[] array ){

            Allocate( array.Length+4 );
            
            byte[] lengthArray = System.BitConverter.GetBytes( array.Length );
            System.Array.Copy( lengthArray, 0, data, index, 4 );
            index += 4;

            for( int i=0; i<array.Length; i++ ){
                data[index++] = array[i];
            }
        }

        public void WriteNormal1ByteFloat( float f ){    //f must be -1~1
            WriteByte( (byte)( Mathf.RoundToInt(f*128.0f)+127 ) );
        }

        public void WriteUnsignedNormal1ByteFloat( float f ){    //f must be -1~1
            WriteByte( (byte)Mathf.RoundToInt( Mathf.Min(255,f*256.0f) ) );
        }

        public void WriteUTF8String( string s ){
            if( s.Length > 255 ){
                Debug.LogError("ERROR CANNOT SAVE STRING LONGER THAN MAX ENCODING!");
                return;
            }
            byte[] sb = Tools.UTF8ToByteArray( s );
            Allocate( sb.Length+1 );
            data[index++] = (byte)sb.Length;
            System.Array.Copy( sb, 0, data, index, sb.Length );
            index += sb.Length;
        }

        public byte[] ToArray(){
            byte[] final = new byte[ index ];
            System.Array.Copy( data, final, index );
            return final;
        }
    }


}