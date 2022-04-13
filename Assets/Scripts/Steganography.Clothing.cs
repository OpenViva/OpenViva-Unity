using UnityEngine;
using Unity.Collections;
using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;


namespace viva{


public partial class Steganography : MonoBehaviour {

	public class UnpackedClothingTexture: UnpackedCard{

		public readonly Texture2D texture;
		public readonly string clothingPieceName;
		public readonly string clothingTextureName;

		public UnpackedClothingTexture( byte _version, Texture2D _texture, string _clothingPieceName, string _clothingTextureName )
						:base(_version,CardType.CUSTOM_CLOTHING_TEXTURE){
			texture = _texture;
			clothingPieceName = _clothingPieceName;
			clothingTextureName = _clothingTextureName;
		}

		protected override void OnDestroyAssociated(){
			Destroy( texture );
		}
	}

	public class PackLossyTextureRequest{

		public string error;
		public readonly Texture2D target;
		public readonly Texture2D destination;
		public readonly byte[] metadata;
		public Texture2D result;
		public readonly byte LOSSY_TEXTURE_VERSION = 1;

		public PackLossyTextureRequest( Texture2D _target, Texture2D _destination, string clothingPieceName, string clothingTextureName ){
			target = _target;
			destination = _destination;
			metadata = CreateMetadata( clothingPieceName, clothingTextureName );
		}

		private byte[] CreateMetadata( string clothingPieceName, string clothingTextureName ){

			//calculate incoming size
			int size = 5+clothingPieceName.Length+clothingTextureName.Length;
			if( size > 256 ){
				Debug.LogError("ERROR too much metadata!");
				return null;
			}
			var ms = new System.IO.MemoryStream( 256 );
			//bytes have range of -128~128 (256)
			ms.WriteByte( LOSSY_TEXTURE_VERSION );
			ms.WriteByte( (byte)UnpackedCard.CardType.CUSTOM_CLOTHING_TEXTURE );	//sign for is clothing texture metadata
			ms.WriteByte( (byte)clothingPieceName.Length );
			ms.Write( Tools.UTF8ToByteArray( clothingPieceName ), 0, clothingPieceName.Length );
			ms.WriteByte( (byte)clothingTextureName.Length );
			ms.Write( Tools.UTF8ToByteArray( clothingTextureName ), 0, clothingTextureName.Length );
			return ms.ToArray();
		}
	}

    public class UnpackClothingTextureRequest{

        public readonly Texture2D cardTexture;
        public UnpackedClothingTexture result;
		public string error = null;

        public UnpackClothingTextureRequest( Texture2D _cardTexture ){
            cardTexture = _cardTexture;
        }
    }
	
	[SerializeField]
	public Texture2D clothingPackingMask;
	
	[SerializeField]
	public Texture2D clothingCardBorderAlpha;
	
	[SerializeField]
	public Texture2D clothingCardBorderGrey;

	public IEnumerator ExecuteUnpackClothingCard( UnpackClothingTextureRequest request ){

		if( request.cardTexture == null ){
			request.error = "Target texture is null!";
			yield break;
		}
		string error = null;
		bool valid = IsValidCardFormat( request.cardTexture, new TextureFormat[]{ TextureFormat.ARGB32, TextureFormat.RGB24 }, ref error );
		valid &= IsValidCardFormat( clothingPackingMask, new TextureFormat[]{ TextureFormat.Alpha8 }, ref error );

		if( !valid ){
			request.error = error;
			yield break;
		}
		
		//ensure data is RGB
		byte[] targetData = request.cardTexture.GetRawTextureData();
		byte[] targetByteArray = null;
		if( request.cardTexture.format == TextureFormat.ARGB32 ){
			byte[] rgbTargetData = new byte[ (targetData.Length*3)/4 ];
			int RGBByteIndex = 0;
			int RGBAByteIndex = 0;
			while( RGBByteIndex<rgbTargetData.Length){
				RGBAByteIndex++;	//skip alpha
				rgbTargetData[RGBByteIndex++] = targetData[RGBAByteIndex++];
				rgbTargetData[RGBByteIndex++] = targetData[RGBAByteIndex++];
				rgbTargetData[RGBByteIndex++] = targetData[RGBAByteIndex++];
			}
			targetByteArray = rgbTargetData;
		}else{	//RGB24
			targetByteArray = targetData;
		}
		//Read first 2 bytes as header RG
		byte version = targetByteArray[0];
		byte cardType = targetByteArray[1];

		if( cardType != (int)UnpackedCard.CardType.CUSTOM_CLOTHING_TEXTURE ){
			request.error = "Wrong card type. Expected clothing card!";
			yield break;
		}

		byte[] metadata = new byte[ 256 ];	//HARDCODED NUMBER
		for( int i=0; i<metadata.Length; i++ ){	
			metadata[i] = targetByteArray[i];
		}
		
		///Initialize Job
		byte[] maskData = clothingPackingMask.GetRawTextureData();
		NativeArray<byte> nativeMaskData = ConvertToNativeArray( maskData );
        NativeArray<byte> nativeTargetArray = ConvertToNativeArray( targetByteArray );
		var unpackTextureData = new UnpackClothingCardJob( nativeMaskData, nativeTargetArray );
        JobHandle jobHandle = unpackTextureData.Schedule();

        while( true ){
            if( !jobHandle.IsCompleted ){
                yield return null;
                continue;
            }
            break;
        }
        jobHandle.Complete();

		byte[] destDataRGB = new byte[ PACK_SIZE*PACK_SIZE*3 ];
        unpackTextureData.resultData.CopyTo( destDataRGB );

        nativeMaskData.Dispose();
        nativeTargetArray.Dispose();
        unpackTextureData.resultData.Dispose();

        //build final Texture
		Texture2D sourceTexture = new Texture2D( PACK_SIZE, PACK_SIZE, TextureFormat.ARGB32, false, false );
		yield return new WaitForSeconds( 0.03f );

		int finalDestDataByteIndex = 0;
		int byteIndex = 0;
		byte[] destData = new byte[ PACK_SIZE*PACK_SIZE*4];

		for( int i=0; i<PACK_SIZE*PACK_SIZE; i++ ){
			destData[finalDestDataByteIndex++] = (byte)(System.Convert.ToInt32( destDataRGB[byteIndex+2]!=255 )*255);
			destData[finalDestDataByteIndex++] = destDataRGB[byteIndex++];
			destData[finalDestDataByteIndex++] = destDataRGB[byteIndex++];
			destData[finalDestDataByteIndex++] = destDataRGB[byteIndex++];
		}

		sourceTexture.LoadRawTextureData( destData );
		yield return new WaitForSeconds( 0.03f );
		sourceTexture.Compress(false);
		sourceTexture.Apply();
		sourceTexture.wrapMode = TextureWrapMode.Clamp;

		//parse metadata
		byte clothingPieceNameLength = metadata[2];
		string clothingPieceName = Tools.ByteArrayToUTF8( metadata, 3, clothingPieceNameLength ); 
		byte clothingTextureNameLength = metadata[3+clothingPieceNameLength];
		string clothingTextureName = Tools.ByteArrayToUTF8( metadata, 4+clothingPieceNameLength, clothingTextureNameLength );
		request.result = new UnpackedClothingTexture( version, sourceTexture, clothingPieceName, clothingTextureName );
	}


	public IEnumerator ExecutePackClothingCard( PackLossyTextureRequest request ){
		
		//store 3 source bits into lowest 3 bits of target byte
		//SOURCE:   88	2 8-bit bytes
		//TARGET:   33	1 6-bit byte
		//for every 6 bits (1 saved byte) you need 16 bits (2 bytes) for storage (1:2 storage ratio)
		//lower 2 bits of the target image are trimmed away for storage optimization (-1.5625% color accuracy)
		bool valid = true;
		if( request.target.width != PACK_SIZE || request.target.height != PACK_SIZE ){
			Debug.LogError("ERROR embedding only supports "+PACK_SIZE+"x"+PACK_SIZE+" target textures");
			valid = false;
		}
		valid &= IsValidCardFormat( request.destination, new TextureFormat[]{ TextureFormat.RGB24 }, ref request.error );
		valid &= IsValidCardFormat( clothingCardBorderAlpha, new TextureFormat[]{ TextureFormat.Alpha8 }, ref request.error );
		valid &= IsValidCardFormat( clothingCardBorderGrey, new TextureFormat[]{ TextureFormat.Alpha8 }, ref request.error );
		valid &= IsValidCardFormat( clothingPackingMask, new TextureFormat[]{ TextureFormat.Alpha8 }, ref request.error );

		if( request.metadata == null ){
			request.error += "\nERROR no metadata specified!";
			valid = false;
		}else if( request.metadata.Length >= 256 ){
			request.error += "\nERROR too much metadata given of size: "+request.metadata.Length;
			valid = false;
		}

		if( !valid ){
			yield break;
		}
		NativeArray<byte> nativeTargetColorArray = ConvertToNativeArray( request.target.GetRawTextureData() );
		NativeArray<byte> nativeMaskData = ConvertToNativeArray( clothingPackingMask.GetRawTextureData() );
        NativeArray<byte> nativeDestArray = ConvertToNativeArray( request.destination.GetRawTextureData() );
        NativeArray<byte> nativeCadBorderAlphaArray = ConvertToNativeArray( clothingCardBorderAlpha.GetRawTextureData() );
        NativeArray<byte> nativeCadBorderGreyArray = ConvertToNativeArray( clothingCardBorderGrey.GetRawTextureData() );
		var packTextureData = new PackClothingCardJob(
			nativeTargetColorArray,
			nativeMaskData,
			nativeCadBorderAlphaArray,
			nativeCadBorderGreyArray,
			nativeDestArray
		);
        JobHandle jobHandle = packTextureData.Schedule();
		
        while( true ){
            if( !jobHandle.IsCompleted ){
                yield return null;
                continue;
            }
            break;
        }
        jobHandle.Complete();

		byte[] destData = new byte[ nativeDestArray.Length ];
        packTextureData.nativeDestArray.CopyTo( destData );

        nativeTargetColorArray.Dispose();
        nativeMaskData.Dispose();
        nativeDestArray.Dispose();
        nativeCadBorderAlphaArray.Dispose();
        nativeCadBorderGreyArray.Dispose();

		//override pixels with metadata, bypasses compression
		for( int i=0; i<request.metadata.Length; i++ ){
			destData[i] = request.metadata[i];
		}

		request.result = CreateCard( destData, request.target.name );;
	}
}

}