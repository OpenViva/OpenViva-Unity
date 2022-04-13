// using UnityEngine;
// using UnityEditor;
// using System.Collections;

// using UnityEngine.XR;
 
// namespace viva{

// public enum Occupation{
// 	HEAD,
// 	HAND_RIGHT,
// 	HAND_LEFT,
// 	SHOULDER_RIGHT,
// 	SHOULDER_LEFT
// }

// public enum OccupyType{
// 	CLEAR 		= 1,
// 	OBJECT 		= 2,
// 	HANDHOLD	= 4,
// 	CHOPSTICKS	= 8,
// 	ANIMATION	= 16
// }

// public class Registry : MonoBehaviour {

//     public delegate void OccupyCallback( Character self, Occupation occupation, OccupyType action );

// 	public static bool occupyStatePersists( OccupyType state ){
// 		switch( state ){
// 			case OccupyType.OBJECT:
// 			case OccupyType.HANDHOLD:
// 				return true;
// 		}
// 		return false;
// 	}

// 	private int GetBitMaskFromOccupation( Occupation occupation ){
// 		return 1<<(int)occupation;
// 	}

// 	private Occupation GetOccupationFromBitMask( int bit_mask ){
// 		switch( bit_mask ){
// 		case 1:
// 			return Occupation.HEAD;
// 		case 2:
// 			return Occupation.HAND_RIGHT;
// 		case 4:
// 			return Occupation.HAND_LEFT;
// 		case 8:
// 			return Occupation.SHOULDER_RIGHT;
// 		case 16:
// 			return Occupation.SHOULDER_LEFT;
// 		}
// 		Debug.LogError("UNHANDLED OCCUPATION BIT MASK");
// 		return Occupation.HEAD;
// 	}

//     protected int occupiesBitMask = 0;
// 	private OccupyCallback[] callbacks = new OccupyCallback[ System.Enum.GetValues(typeof(Occupation)).Length ];
// 	private OccupyType[] occupyStates = new OccupyType[]{
// 		OccupyType.CLEAR,
// 		OccupyType.CLEAR,
// 		OccupyType.CLEAR,
// 		OccupyType.CLEAR,
// 		OccupyType.CLEAR
// 	};

// 	public OccupyType GetOccupyState( Occupation occupation ){
// 		return occupyStates[ (int)occupation ];
// 	}

//     public string toString(){
		
// 		string t = "";
// 		for( int i=0; i<4; i++ ){
// 			t += (occupiesBitMask>>i)&1;
// 		}
// 		return t;
// 	}

// 	public void Unregister( Character self, Occupation occupation, OccupyType newState ){
// 		Unregister( self, GetBitMaskFromOccupation( occupation ), newState );
// 	}

// //register not allowed to run after an unregister
// 	private bool RECURSION_LOCK = false;

// 	private void Unregister( Character self, int bit_mask, OccupyType newState ){
// 		RECURSION_LOCK = true;
// 		//remove bits from mask
// 		for( int i=0; i<4; i++ ){
// 			int bit = 1<<i;
// 			if( (bit_mask&bit) != 0 ){
// 				if( (occupiesBitMask&bit) != 0 ){
// 					occupiesBitMask &= ~bit;	//turn off bit
// 					OccupyCallback callback = callbacks[i];
// 					if( callback != null ){
// 						occupyStates[i] = newState;

// 						callback( self, GetOccupationFromBitMask( bit ), newState );
// 						callbacks[i] = null;
// 					}
// 				}
// 			}
// 		}
// 		RECURSION_LOCK = false;
// 	}
// 	public void register(  Character self, Occupation occupation, OccupyCallback callback, OccupyType newState ){
// 		register( self, GetBitMaskFromOccupation( occupation ), callback, newState );
// 	}
// 	private void register(  Character self, int bit_mask, OccupyCallback callback, OccupyType newState ){	//Debug.Log("Reg "+bit_mask+" "+newState);

// 		if( RECURSION_LOCK ){
// 			Debug.LogError("###ERROR### unregister->register recursion detected!");
// 			Debug.Break();
// 		}
// 		//unregister previously occupying
// 		Unregister( self, bit_mask, newState );
		
// 		//register bit
// 		occupiesBitMask |= bit_mask;
// 		for( int i=0; i<4; i++ ){
// 			int bit = 1<<i;
// 			if( (bit_mask&bit) != 0 ){	//match
// 				callbacks[i] = callback;
// 				occupyStates[i] = newState;
// 			}
// 		}
// 	}
// 	public bool IsCallbackRegistered( Occupation index, Registry.OccupyCallback callback ){
// 		return callbacks[(int)index] == callback;
// 	}
// }

// }