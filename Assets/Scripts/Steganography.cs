using UnityEngine;
using Unity.Collections;
using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;


namespace viva{


public partial class Steganography : MonoBehaviour {

	public abstract class UnpackedCard{

		public enum CardType{	//HARDCODED INTO PNGS DO NOT CHANGE ORDER
			CUSTOM_CLOTHING_TEXTURE,
			CUSTOM_CHARACTER_MODEL
		}

		private byte version;
		private CardType type;

		public byte GetVersion(){
			return version;
		}
		public CardType GetCardType(){
			return type;
		}

		public UnpackedCard( byte _version, CardType _type ){
			version = _version;
			type = _type;
		}

		protected abstract void OnDestroyAssociated();

		public void DestroyAssociated(){
			OnDestroyAssociated();
		}
	}

	public static Steganography main;
	public static int PACK_SIZE = 1024;
	public static int THUMBNAIL_SCALE = 8;
	public static int CARD_HEIGHT = (int)(PACK_SIZE*1.5f);
	private int embedID = Shader.PropertyToID("_Embed");

	public void Awake(){
#if UNITY_EDITOR
		if( main != null ){
			Debug.LogError("WARNING Multiple Steganographies loaded! Wasteful.");
		}
#endif
		main = this;
	}

	
	private static bool IsValidCardFormat( Texture2D texture, TextureFormat[] formats, ref string error ){
		bool valid = true;
		if( texture == null ){
			error += "\nERROR texture must not be null!";
			return false;
		}
		if( texture.width != PACK_SIZE || texture.height != CARD_HEIGHT ){
			error += "\nERROR "+texture.name+" texture must be "+PACK_SIZE+"x"+CARD_HEIGHT+" input:"+texture.width+"x"+texture.height;
			valid = false;
		}
		if( formats != null ){
			bool validFormat = false;
			foreach( TextureFormat format in formats ){
				if( texture.format == format ){
					validFormat = true;
					break;
				}
			}
			if( !validFormat ){
				error += "\nERROR "+texture.name+" wrong color format!";
				valid = false;
			}
		}
		if( texture.mipmapCount != 1 ){
			error += "\nERROR "+texture.name+" must not have mipmaps!";
			valid = false;
		}
		return valid;
	}

	private static bool UsesCompression( byte b ){	
		return b > 128;
	}

	private static byte ZeroLast3Bits( byte b ){
		return (byte)(b&0xF8);	//11111000
	}
	
	private static byte Extract8Bits( BitArray ba, int bitIndex ){

		int val = System.Convert.ToInt32( ba.Get( bitIndex+7 ) )<<7;
		val += System.Convert.ToInt32( ba.Get( bitIndex+6 ) )<<6;
		val += System.Convert.ToInt32( ba.Get( bitIndex+5 ) )<<5;
		val += System.Convert.ToInt32( ba.Get( bitIndex+4 ) )<<4;
		val += System.Convert.ToInt32( ba.Get( bitIndex+3 ) )<<3;
		val += System.Convert.ToInt32( ba.Get( bitIndex+2 ) )<<2;
		val += System.Convert.ToInt32( ba.Get( bitIndex+1 ) )<<1;
		val += System.Convert.ToInt32( ba.Get( bitIndex ) );
		return (byte)val;
	}


	private static byte Extract3Bits( BitArray ba, int bitIndex ){

		int val = System.Convert.ToInt32( ba.Get( bitIndex+2 ) )<<2;
		val += System.Convert.ToInt32( ba.Get( bitIndex+1 ) )<<1;
		val += System.Convert.ToInt32( ba.Get( bitIndex ) );
		return (byte)val;
	}

	private Texture2D CreateCard( byte[] rawData, string name ){
		Texture2D card = new Texture2D( PACK_SIZE, CARD_HEIGHT, TextureFormat.RGB24, false, true );
		card.name = name;
		card.LoadRawTextureData( rawData );
		card.Apply();

		card.filterMode = FilterMode.Point;
		card.wrapMode = TextureWrapMode.Clamp;

		return card;
	}

	public static string EnsureFolderExistence( string folder ){
		
		string directory = System.IO.Path.GetDirectoryName(Application.dataPath)+"/"+folder;
		if( !System.IO.Directory.Exists(directory) ){
			System.IO.Directory.CreateDirectory( directory );
			Debug.Log("Creating directory...");
		}
		return directory;
	}

	public static void SaveTexture( Texture2D image, string folder ){
		string path = EnsureFolderExistence( folder )+"/"+image.name+".png";
		Debug.Log("Saving "+path);
		System.IO.File.WriteAllBytes( path, image.EncodeToPNG() );
	}

	public static Texture2D AttemptSaveCardThumbnail( Texture2D target, string folder ){
		string error = null;
		bool valid = IsValidCardFormat( target, null, ref error );
		if( !valid ){
			Debug.LogError("Could not save thumbnail! "+error);
			return null;
		}
		//ensure data is RGB
		byte[] rgbData = new byte[ PACK_SIZE*CARD_HEIGHT*3 ];
		if( target.format == TextureFormat.ARGB32 ){
			byte[] argbTargetData = target.GetRawTextureData();
			int RGBByteIndex = 0;
			int RGBAByteIndex = 0;
			while( RGBByteIndex<rgbData.Length){
				RGBAByteIndex++;	//skip alpha
				rgbData[RGBByteIndex++] = argbTargetData[RGBAByteIndex++];
				rgbData[RGBByteIndex++] = argbTargetData[RGBAByteIndex++];
				rgbData[RGBByteIndex++] = argbTargetData[RGBAByteIndex++];
			}
		}else if( target.format == TextureFormat.RGB24 ){
			rgbData = target.GetRawTextureData();
		}else{
			Debug.LogError("ERROR Texture is not a valid thumbnail format!");
			return null;
		}

		//downsample 1/scale and save .PNG
		int thumbnailWidth = PACK_SIZE/THUMBNAIL_SCALE;
		int thumbnailHeight = CARD_HEIGHT/THUMBNAIL_SCALE;
		byte[] thumbnailData = new byte[ thumbnailWidth*thumbnailHeight*3 ]; //RGB thumbnail
		int sourceByte = 0;
		int targetByte = 0;
		int columnCount = 0;
		for( int i=0; i<thumbnailWidth*thumbnailHeight; i++ ){
			thumbnailData[ targetByte++ ] = rgbData[ sourceByte++ ];
			thumbnailData[ targetByte++ ] = rgbData[ sourceByte++ ];
			thumbnailData[ targetByte++ ] = rgbData[ sourceByte++ ];
			sourceByte += 3*(THUMBNAIL_SCALE-1); //skip 7 pixels
			if( ++columnCount >= thumbnailWidth ){
				columnCount = 0;
				sourceByte += (THUMBNAIL_SCALE-1)*(thumbnailWidth*THUMBNAIL_SCALE*3);	//skip row
			}
		}
		Texture2D thumbnail = new Texture2D( thumbnailWidth, thumbnailHeight, TextureFormat.RGB24, false, false );
		thumbnail.LoadRawTextureData( thumbnailData );
		thumbnail.Apply();
		thumbnail.name = target.name;
		SaveTexture( thumbnail, folder );
		return thumbnail;
	}

}

}