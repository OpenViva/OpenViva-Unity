using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Unity.Jobs;
using Unity.Collections;
using Fbx;



namespace viva{

public partial class TextureRequest: ImportRequest{

    private struct ExecuteReadTexture : IJob {

        public NativeArray<byte> result;
        public NativeArray<byte> filepathBuffer;
        public int index;

        public ExecuteReadTexture( NativeArray<byte> _buffer, NativeArray<byte> _filepathBuffer ){
            result = _buffer;
            filepathBuffer = _filepathBuffer;
            index = 0;
            BufferUtil.InitializeHeader( result, ref index, BufferUtil.manifestHeaderCount );
        }

        public void Execute(){
            var filepathBytes = new byte[ filepathBuffer.Length ];
            filepathBuffer.CopyTo( filepathBytes );
            string filepath = System.Text.Encoding.UTF8.GetString( filepathBytes );

            byte[] fileData = null;
            string error = null;
            try{
                fileData = File.ReadAllBytes( filepath );

                if( fileData.Length+index > result.Length ){
                    throw new System.Exception("File too big "+fileData.Length+"/"+result.Length);
                }
            }catch( System.Exception e ){
                error = e.ToString();
            }
            
            if( error == null ){
                for( int i=0; i<fileData.Length; i++ ){
                    result[ index++ ] = fileData[i];
                }
            }else{
                var errorBytes = System.Text.Encoding.ASCII.GetBytes( error );
                BufferUtil.IncreaseHeaderEntry( result, ref index, (int)BufferUtil.Header.ERROR_LENGTH, errorBytes.Length );

                index = ((int)BufferUtil.Header.ERROR_LENGTH+1)*4;   //override everything past ERROR_LENGTH for the error string
                BufferUtil.WriteBytes( result, ref index, errorBytes );
            }
            BufferUtil.IncreaseHeaderEntry( result, ref index, (int)BufferUtil.Header.READ_LENGTH, index );
        }
    }
}
}