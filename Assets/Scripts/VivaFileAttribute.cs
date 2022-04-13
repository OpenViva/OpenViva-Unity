using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace viva
{
	[AttributeUsage(AttributeTargets.Field|AttributeTargets.Property)]
	public class VivaFileAttribute : System.Attribute
	{
		//used for monobehaviour data and primitives
		public enum AssetStorage{
			UNKNOWN,
			STRING,
			COLOR,
			VECTOR2,
			VECTOR3,
			QUATERNION,
			VIVA_SESSION_ASSET,
			VIVA_SESSION_ASSET_ARRAY,
			VR_CONTROL_TYPE,
			CONTROL_TYPE,
			SINGLE,
			INT32,
			INT32_ARRAY,
			BOOLEAN,
			POKER_SUIT
		}

		public delegate void OnFinishDeserialize( object result );

		//used for non monobehaviour data (cards, etc.)
		public enum DataStorage{
			UNKNOWN,
			OUTFIT
		}
		
		public static DataStorage GetDataStorage( string variableType ){
			variableType = variableType.Replace("UnityEngine.","");
			variableType = variableType.Replace("viva.","");
			variableType = variableType.Replace("System.","");
			switch(variableType ){
			case "Outfit":
				return DataStorage.OUTFIT;
			}
			return DataStorage.UNKNOWN;
		}

		public static AssetStorage GetAssetStorage( string variableType ){
			variableType = variableType.Replace("UnityEngine.","");
			variableType = variableType.Replace("viva.","");
			variableType = variableType.Replace("System.","");
			switch(variableType ){
			case "String":
				return AssetStorage.STRING;
			case "Color":
				return AssetStorage.COLOR;
			case "Vector2":
				return AssetStorage.VECTOR2;
			case "Vector3":
				return AssetStorage.VECTOR3;
			case "Quaternion":
				return AssetStorage.QUATERNION;
			case "VivaSessionAsset":
				return AssetStorage.VIVA_SESSION_ASSET;
			case "VivaSessionAsset[]":
				return AssetStorage.VIVA_SESSION_ASSET_ARRAY;
			case "Player+VRControlType":
				return AssetStorage.VR_CONTROL_TYPE;
			case "Player+ControlType":
				return AssetStorage.CONTROL_TYPE;
			case "Single":
				return AssetStorage.SINGLE;
			case "Int32":
				return AssetStorage.INT32;
			case "Int32[]":
				return AssetStorage.INT32_ARRAY;
			case "Boolean":
				return AssetStorage.BOOLEAN;
			case "PokerCard+Suit":
				return AssetStorage.POKER_SUIT;
			}
			return AssetStorage.UNKNOWN;
		}
		private static string SerializeFloat( float value ){
			return value.ToString( System.Globalization.CultureInfo.InvariantCulture );
		}
		private static string SerializeInt( int value ){
			return value.ToString( System.Globalization.CultureInfo.InvariantCulture );
		}
		private static string SerializeEnum<T>( object obj ) where T:Enum{
			try{
				T enumVal = (T)obj;
				int enumNumVal = System.Convert.ToInt32( enumVal );
				return SerializeInt( enumNumVal );
			}catch( Exception e ){
				Debug.LogError( e );
				return null;
			}
		}
		public static string Serialize( object obj, AssetStorage storageType ){
			switch(storageType ){
			case AssetStorage.STRING:
				return (string)obj;
			case AssetStorage.COLOR:
				Color color = (Color)obj;
				if( color == null ){
					return null;
				}
				return SerializeFloat(color.r)+","+SerializeFloat(color.g)+","+SerializeFloat(color.b)+","+SerializeFloat(color.a);
			case AssetStorage.VECTOR2:
				Vector2 vec2 = (Vector2)obj;
				if( vec2 == null ){
					return null;
				}
				return SerializeFloat(vec2.x)+","+SerializeFloat(vec2.y);
			case AssetStorage.VECTOR3:
				Vector3 vec3 = (Vector3)obj;
				if( vec3 == null ){
					return null;
				}
				return SerializeFloat(vec3.x)+","+SerializeFloat(vec3.y)+","+SerializeFloat(vec3.z);
			case AssetStorage.QUATERNION:
				Quaternion quat = (Quaternion)obj;
				if( quat == null ){
					return null;
				}
				return SerializeFloat(quat.x)+","+SerializeFloat(quat.y)+","+SerializeFloat(quat.z)+","+SerializeFloat(quat.w);
			case AssetStorage.VIVA_SESSION_ASSET:
				VivaSessionAsset asset = (VivaSessionAsset)obj;
				if( asset == null ){
					return "";	//references can be valid as a null!
				}
				return asset.sessionReferenceName;
			case AssetStorage.VIVA_SESSION_ASSET_ARRAY:
				VivaSessionAsset[] assets = (VivaSessionAsset[])obj;
				if( assets == null ){
					return null;
				}
				string value = "";
				if( assets.Length > 0 ){
					if( assets[0] != null ){
						value += assets[0].sessionReferenceName;
					}
				}
				for( int i=1; i<assets.Length; i++ ){
					if( assets[i] != null ){
						value += ","+assets[i].sessionReferenceName;
					}
				}
				return value;
			case AssetStorage.VR_CONTROL_TYPE:
				return SerializeEnum<Player.VRControlType>( obj );
			case AssetStorage.CONTROL_TYPE:
				return SerializeEnum<Player.ControlType>( obj );
			case AssetStorage.SINGLE:
				float? f = (float)obj;
				if( f == null ){
					return null;
				}
				return SerializeFloat( f.Value );
			case AssetStorage.INT32:
				int? integer = (int)obj;
				if( integer == null ){
					return null;
				}
				return SerializeInt( integer.Value );
			case AssetStorage.INT32_ARRAY:
				int[] ints = (int[])obj;
				if( ints == null ){
					return null;
				}
				string intsVal = "";
				if( ints.Length > 0 ){
					intsVal += ints[0];
				}
				for( int i=1; i<ints.Length; i++ ){
					intsVal += ","+ints[i];
				}
				return intsVal;
			case AssetStorage.BOOLEAN:
				bool? b = (bool)obj;
				if( b == null ){
					return null;
				}
				return SerializeInt( System.Convert.ToInt32( b.Value ) );
			case AssetStorage.POKER_SUIT:
				return SerializeEnum<PokerCard.Suit>( obj );
			}
			Debug.LogError("[Serialization] Unhandled storage type: "+storageType);
			return null;
		}
		public static string Serialize( object obj, DataStorage storageType ){
			switch( storageType ){
			case DataStorage.OUTFIT:
				Outfit outfit = (Outfit)obj;
				if( outfit == null ){
					return "";
				}else{
					return outfit.Serialize();
				}
			}
			Debug.LogError("[Serialization] Unhandled storage type: "+storageType);
			return null;
		}

		private static float DeserializeFloat( string val ){
			float result;
			if( System.Single.TryParse( val, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out result ) ){
				return result;
			}
			return 0.0f;
		}
		private static int DeserializeInt( string val ){
			int result;
			if( System.Int32.TryParse( val, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out result ) ){
				return result;
			}
			return 0;
		}
		private static T DeserializeEnum<T>( string val ) where T:Enum{
			int enumIndex = DeserializeInt( val );
			return (T)(object)enumIndex;
		}

		public static object Deserialize( string rawVal, AssetStorage storageType ){
			switch(storageType ){
			case AssetStorage.STRING:
				return rawVal;
			case AssetStorage.COLOR:
				Color color = new Color();
				var colorWords = rawVal.Split(',');
				for( int i=0; i<4; i++ ){
					color[i] = DeserializeFloat(colorWords[i]);
				}
				return color;
			case AssetStorage.VECTOR2:
				Vector2 vec2 = new Vector2();
				var vec2Words = rawVal.Split(',');
				for( int i=0; i<2; i++ ){
					vec2[i] = DeserializeFloat(vec2Words[i]);
				}
				return vec2;
			case AssetStorage.VECTOR3:
				Vector3 vec3 = new Vector3();
				var vec3Words = rawVal.Split(',');
				for( int i=0; i<3; i++ ){
					vec3[i] = DeserializeFloat(vec3Words[i]);
				}
				return vec3;
			case AssetStorage.QUATERNION:
				Quaternion quat = new Quaternion();
				var quatWords = rawVal.Split(',');
				for( int i=0; i<4; i++ ){
					quat[i] = DeserializeFloat(quatWords[i]);
				}
				return quat;
			case AssetStorage.VIVA_SESSION_ASSET:
				if( rawVal == "" ){	//null references are empty string
					return null;
				}
				GameObject targetContainer = GameObject.Find(rawVal);
				if( targetContainer == null ){
					Debug.LogError("[PERSISTANCE] Could not dereference gameobject "+rawVal);
					return null;
				}
				VivaSessionAsset asset = targetContainer.GetComponent<VivaSessionAsset>();
				if( asset == null ){
					Debug.LogError("[PERSISTANCE] Could not find VivaSessionAsset from gameobject "+rawVal);
				}
				return asset;
			case AssetStorage.VIVA_SESSION_ASSET_ARRAY:
				if( rawVal.Length == 0 ){
					return new VivaSessionAsset[0];
				}
				var words = rawVal.Split(',');
				VivaSessionAsset[] assets = new VivaSessionAsset[ words.Length ];
				for( int i=0; i<words.Length; i++ ){
					assets[i] = Deserialize( words[i], AssetStorage.VIVA_SESSION_ASSET ) as VivaSessionAsset;
				}
				return assets;
			case AssetStorage.VR_CONTROL_TYPE:
				return DeserializeEnum<Player.VRControlType>( rawVal );
			case AssetStorage.CONTROL_TYPE:
				return DeserializeEnum<Player.ControlType>( rawVal );
			case AssetStorage.SINGLE:
				return DeserializeFloat( rawVal );
			case AssetStorage.INT32:
				return DeserializeInt( rawVal );
			case AssetStorage.INT32_ARRAY:
				if( rawVal.Length == 0 ){
					return new int[0];
				}
				var intWords = rawVal.Split(',');
				int[] ints = new int[ intWords.Length ];
				for( int i=0; i<intWords.Length; i++ ){
					ints[i] = DeserializeInt( intWords[i] );
				}
				return ints;
			case AssetStorage.BOOLEAN:
				return System.Convert.ToBoolean( DeserializeInt( rawVal ) );
			case AssetStorage.POKER_SUIT:
				return DeserializeEnum<PokerCard.Suit>( rawVal );
			}
			Debug.LogError("[Deserialization] Unhandled asset storage type: "+storageType);
			return null;
		}
		public static bool Deserialize( string rawVal, DataStorage storageType, OnFinishDeserialize onFinishDeserialize ){
			switch(storageType ){
			case DataStorage.OUTFIT:
				GameDirector.instance.StartCoroutine( Outfit.Deserialize( rawVal, onFinishDeserialize ) );
				return true;
			}
			Debug.LogError("[Deserialization] Unhandled data storage type: "+storageType);
			return false;
		}
	}
}
