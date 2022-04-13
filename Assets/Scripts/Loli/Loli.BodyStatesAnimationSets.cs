using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace viva{

public enum BodyState{
	NONE,
	OFFBALANCE,	//Do not change order
	STAND,
	STANDING_HUG,
	FLOOR_SIT,
	BATHING_RELAX,
	BATHING_IDLE,
	BATHING_ON_KNEES,
	BATHING_STAND,
	CRAWL_TIRED,
	AWAKE_PILLOW_UP,
	AWAKE_PILLOW_SIDE_RIGHT,
	AWAKE_PILLOW_SIDE_LEFT,
	SLEEP_PILLOW_SIDE_RIGHT,
	SLEEP_PILLOW_SIDE_LEFT,
	SLEEP_PILLOW_UP,
	HORSEBACK,
	SQUAT,
	RELAX
}

public enum AnimationSet{
	IDLE_HAPPY,
	IDLE_ANGRY,
	AGREE,
	REFUSE,
	IMPRESSED,
	POKE_FACE_SOFT_RIGHT,
	POKE_FACE_HARD_RIGHT,
	HEADPAT_START_HAPPY,
	HEADPAT_START_ANGRY,
	HEADPAT_END_WANTED_MORE,
	HEADPAT_END_FULFILL,
	HEADPAT_END_ANGRY,
	HEADPAT_BRUSH_AWAY,
	HEADPAT_ASK_FOR_MORE,
	HEADPAT_CANCEL_SUCCESS,
	HEADPAT_SUCCESS,
	HEADPAT_IDLE_HAPPY,
	HEADPAT_IDLE_ANGRY,
	PICKUP_RIGHT_LEFT,
	SWAP,
	CONFUSED,
	STARTLED_HAPPY,
	STARTLED_ANGRY,
	CHEEK_KISS_HAPPY_RIGHT_LEFT,
	CHEEK_KISS_ANGRY_RIGHT_LEFT,
	CHEEK_KISS_ANGRY_TO_ANGRY_RIGHT_LEFT,
	CHEEK_KISS_ANGRY_TO_HAPPY_RIGHT_LEFT
}

public enum PropertyValue{
	PICKUP_DISTANCE,
}


public partial class Loli : Character{

	public class BodyStateAnimationSet{
		
		public readonly bool checkBalance;
		private readonly Dictionary<AnimationSet,List<Animation>> bodyStateAnimations = new Dictionary<AnimationSet, List<Animation>>();
		public readonly Dictionary<PropertyValue,float> propertyValues = new Dictionary<PropertyValue, float>();
		public readonly Dictionary<BodyState,Animation> bodyStateConnections = new  Dictionary<BodyState, Animation>();


		public BodyStateAnimationSet( bool _checkBalance ){
			checkBalance = _checkBalance;
		}

		public void AddAnimation( AnimationSet animSet, Loli.Animation animation ){
			if( bodyStateAnimations.TryGetValue( animSet, out List<Animation> animations ) ){
				animations.Add( animation );
			}else{
				bodyStateAnimations[ animSet ] = new List<Animation>(){ animation };
			}
		}

		public List<Animation> GetAnimationSetList( AnimationSet animSet ){
			if( bodyStateAnimations.TryGetValue( animSet, out List<Animation> animations ) ){
				return animations;
			}else{
				return null;
			}
		}

		public void SetAnimationSetList( AnimationSet animSet, List<Loli.Animation> list ){
			bodyStateAnimations[ animSet ] = list;
		}

		public Animation GetRandomAnimationSet( AnimationSet animSet ){
			if( bodyStateAnimations.TryGetValue( animSet, out List<Animation> animations ) ){
				return animations[ Random.Range( 0, animations.Count ) ];
			}
			return Loli.Animation.NONE;
		}

		public Animation GetAnimationSet( AnimationSet animSet, int index ){
			if( bodyStateAnimations.TryGetValue( animSet, out List<Animation> animations ) ){
				return animations[ Mathf.Min( animations.Count-1, index ) ];
			}
			return Loli.Animation.NONE;
		}
	}
	
	private static BodyStateAnimationSet[] GenerateBodyStateAnimationSets(){
		
		var bodyStateAnimationSets = new BodyStateAnimationSet[ System.Enum.GetValues(typeof(BodyState)).Length ];

		var nullBodyState = new BodyStateAnimationSet( false );
		bodyStateAnimationSets[ (int)BodyState.NONE ] = nullBodyState;
		
		var offbalance = new BodyStateAnimationSet( false );
		bodyStateAnimationSets[ (int)BodyState.OFFBALANCE ] = offbalance;

		var stand = new BodyStateAnimationSet( true );
		bodyStateAnimationSets[ (int)BodyState.STAND ] = stand;
		stand.AddAnimation( AnimationSet.IDLE_HAPPY, Loli.Animation.STAND_HAPPY_IDLE1 );
		stand.AddAnimation( AnimationSet.IDLE_HAPPY, Loli.Animation.STAND_HAPPY_IDLE2 );
		stand.AddAnimation( AnimationSet.IDLE_HAPPY, Loli.Animation.STAND_HAPPY_IDLE3 );
		stand.AddAnimation( AnimationSet.IDLE_ANGRY, Loli.Animation.STAND_ANGRY_IDLE1 );
		stand.AddAnimation( AnimationSet.AGREE, Loli.Animation.STAND_AGREE );
		stand.AddAnimation( AnimationSet.REFUSE, Loli.Animation.STAND_REFUSE );
		stand.AddAnimation( AnimationSet.IMPRESSED, Loli.Animation.STAND_IMPRESSED1 );
		stand.AddAnimation( AnimationSet.POKE_FACE_SOFT_RIGHT, Loli.Animation.STAND_POKE_FACE_1_RIGHT );
		stand.AddAnimation( AnimationSet.POKE_FACE_HARD_RIGHT, Loli.Animation.STAND_POKE_FACE_2_RIGHT );
		stand.AddAnimation( AnimationSet.POKE_FACE_HARD_RIGHT, Loli.Animation.STAND_POKE_FACE_3_RIGHT );
		stand.AddAnimation( AnimationSet.HEADPAT_START_HAPPY, Loli.Animation.STAND_HEADPAT_HAPPY_START );
		stand.AddAnimation( AnimationSet.HEADPAT_START_ANGRY, Loli.Animation.STAND_HEADPAT_ANGRY_LOOP );
		stand.AddAnimation( AnimationSet.HEADPAT_END_WANTED_MORE, Loli.Animation.STAND_HEADPAT_HAPPY_WANTED_MORE );
		stand.AddAnimation( AnimationSet.HEADPAT_END_FULFILL, Loli.Animation.STAND_HEADPAT_HAPPY_WANTED_MORE );
		stand.AddAnimation( AnimationSet.HEADPAT_END_ANGRY, Loli.Animation.STAND_HEADPAT_ANGRY_END );
		stand.AddAnimation( AnimationSet.HEADPAT_BRUSH_AWAY, Loli.Animation.STAND_HEADPAT_ANGRY_BRUSH_AWAY );
		stand.AddAnimation( AnimationSet.HEADPAT_BRUSH_AWAY, Loli.Animation.STAND_HEADPAT_CLIMAX_TO_CANCEL_RIGHT );
		stand.AddAnimation( AnimationSet.HEADPAT_BRUSH_AWAY, Loli.Animation.STAND_HEADPAT_CLIMAX_TO_CANCEL_LEFT );
		stand.AddAnimation( AnimationSet.HEADPAT_ASK_FOR_MORE, Loli.Animation.STAND_HEADPAT_HAPPY_IDLE_SAD_RIGHT );
		stand.AddAnimation( AnimationSet.HEADPAT_ASK_FOR_MORE, Loli.Animation.STAND_HEADPAT_HAPPY_IDLE_SAD_LEFT );
		stand.AddAnimation( AnimationSet.HEADPAT_SUCCESS, Loli.Animation.STAND_HEADPAT_CLIMAX_TO_HAPPY );
		stand.AddAnimation( AnimationSet.HEADPAT_IDLE_HAPPY, Loli.Animation.STAND_HEADPAT_HAPPY_LOOP );
		stand.AddAnimation( AnimationSet.HEADPAT_IDLE_ANGRY, Loli.Animation.STAND_HEADPAT_ANGRY_LOOP );
		stand.AddAnimation( AnimationSet.PICKUP_RIGHT_LEFT, Loli.Animation.STAND_PICKUP_RIGHT );
		stand.AddAnimation( AnimationSet.PICKUP_RIGHT_LEFT, Loli.Animation.STAND_PICKUP_LEFT );
		stand.AddAnimation( AnimationSet.SWAP, Loli.Animation.STAND_HAPPY_CHANGE_ITEM_HANDS );
		stand.AddAnimation( AnimationSet.CONFUSED, Loli.Animation.STAND_SEARCH_RIGHT );
		stand.AddAnimation( AnimationSet.STARTLED_HAPPY, Loli.Animation.STAND_FACE_PROX_HAPPY_SURPRISE );
		stand.AddAnimation( AnimationSet.STARTLED_ANGRY, Loli.Animation.STAND_FACE_PROX_ANGRY_SURPRISE );
		stand.AddAnimation( AnimationSet.CHEEK_KISS_HAPPY_RIGHT_LEFT, Loli.Animation.STAND_KISS_HAPPY_CHEEK_RIGHT );
		stand.AddAnimation( AnimationSet.CHEEK_KISS_HAPPY_RIGHT_LEFT, Loli.Animation.STAND_KISS_HAPPY_CHEEK_LEFT );
		stand.AddAnimation( AnimationSet.CHEEK_KISS_ANGRY_RIGHT_LEFT, Loli.Animation.STAND_KISS_ANGRY_CHEEK_RIGHT );
		stand.AddAnimation( AnimationSet.CHEEK_KISS_ANGRY_RIGHT_LEFT, Loli.Animation.STAND_KISS_ANGRY_CHEEK_LEFT );
		stand.AddAnimation( AnimationSet.CHEEK_KISS_ANGRY_TO_ANGRY_RIGHT_LEFT, Loli.Animation.STAND_KISS_ANGRY_RIGHT_TO_ANGRY );
		stand.AddAnimation( AnimationSet.CHEEK_KISS_ANGRY_TO_ANGRY_RIGHT_LEFT, Loli.Animation.STAND_KISS_ANGRY_LEFT_TO_ANGRY );
		stand.AddAnimation( AnimationSet.CHEEK_KISS_ANGRY_TO_HAPPY_RIGHT_LEFT, Loli.Animation.STAND_KISS_ANGRY_RIGHT_TO_HAPPY );
		stand.AddAnimation( AnimationSet.CHEEK_KISS_ANGRY_TO_HAPPY_RIGHT_LEFT, Loli.Animation.STAND_KISS_ANGRY_LEFT_TO_HAPPY );
		stand.propertyValues[ PropertyValue.PICKUP_DISTANCE ] = 0.42f;
		stand.bodyStateConnections[ BodyState.FLOOR_SIT ] = Animation.STAND_TO_SIT_FLOOR;
		stand.bodyStateConnections[ BodyState.SQUAT ] = Animation.STAND_TO_SQUAT;


		var standingHug = new BodyStateAnimationSet( true );
		bodyStateAnimationSets[ (int)BodyState.STANDING_HUG ] = standingHug;
		standingHug.AddAnimation( AnimationSet.IDLE_HAPPY, Loli.Animation.STAND_HUG_HAPPY_LOOP );
		standingHug.AddAnimation( AnimationSet.IDLE_ANGRY, Loli.Animation.STAND_HUG_ANGRY_LOOP );
		standingHug.bodyStateConnections[ BodyState.STAND ] = Animation.STAND_HAPPY_IDLE1;
		
		
		var floorSit = new BodyStateAnimationSet( true );
		bodyStateAnimationSets[ (int)BodyState.FLOOR_SIT ] = floorSit;
		floorSit.AddAnimation( AnimationSet.IDLE_HAPPY, Loli.Animation.FLOOR_SIT_LOCOMOTION_HAPPY );
		floorSit.AddAnimation( AnimationSet.IDLE_ANGRY, Loli.Animation.FLOOR_SIT_LOCOMOTION_ANGRY );
		floorSit.AddAnimation( AnimationSet.REFUSE, Loli.Animation.FLOOR_SIT_REFUSE );
		floorSit.AddAnimation( AnimationSet.AGREE, Loli.Animation.FLOOR_SIT_AGREE );
		floorSit.AddAnimation( AnimationSet.IMPRESSED, Loli.Animation.FLOOR_SIT_IMPRESSED1 );
		floorSit.AddAnimation( AnimationSet.HEADPAT_START_HAPPY, Loli.Animation.FLOOR_SIT_HEADPAT_HAPPY_START );
		floorSit.AddAnimation( AnimationSet.HEADPAT_START_ANGRY, Loli.Animation.FLOOR_SIT_HEADPAT_ANGRY_LOOP );
		floorSit.AddAnimation( AnimationSet.HEADPAT_END_WANTED_MORE, Loli.Animation.FLOOR_SIT_HEADPAT_HAPPY_WANTED_MORE );
		floorSit.AddAnimation( AnimationSet.HEADPAT_BRUSH_AWAY, Loli.Animation.FLOOR_SIT_HEADPAT_ANGRY_BRUSH_AWAY );
		floorSit.AddAnimation( AnimationSet.HEADPAT_SUCCESS, Loli.Animation.FLOOR_SIT_HEADPAT_ANGRY_CLIMAX_TO_HAPPY );
		floorSit.AddAnimation( AnimationSet.HEADPAT_IDLE_HAPPY, Loli.Animation.FLOOR_SIT_HEADPAT_HAPPY_LOOP );
		floorSit.AddAnimation( AnimationSet.HEADPAT_IDLE_ANGRY, Loli.Animation.FLOOR_SIT_HEADPAT_ANGRY_LOOP );
		floorSit.AddAnimation( AnimationSet.PICKUP_RIGHT_LEFT, Loli.Animation.FLOOR_SIT_REACH_RIGHT );
		floorSit.AddAnimation( AnimationSet.PICKUP_RIGHT_LEFT, Loli.Animation.FLOOR_SIT_REACH_LEFT );
		floorSit.propertyValues[ PropertyValue.PICKUP_DISTANCE ] = 0.225f;
		floorSit.bodyStateConnections[ BodyState.STAND ] = Animation.FLOOR_SIT_TO_STAND;
		floorSit.bodyStateConnections[ BodyState.CRAWL_TIRED ] = Animation.FLOOR_SIT_TO_CRAWL;

		
		var bathtub = new BodyStateAnimationSet( true );
		bodyStateAnimationSets[ (int)BodyState.BATHING_IDLE ] = bathtub;
		bathtub.AddAnimation( AnimationSet.IDLE_HAPPY, Loli.Animation.BATHTUB_HAPPY_IDLE_LOOP );
		bathtub.AddAnimation( AnimationSet.IDLE_HAPPY, Loli.Animation.BATHTUB_HAPPY_IDLE2 );
		bathtub.AddAnimation( AnimationSet.IDLE_HAPPY, Loli.Animation.BATHTUB_HAPPY_IDLE3 );
		bathtub.AddAnimation( AnimationSet.IDLE_HAPPY, Loli.Animation.BATHTUB_HAPPY_IDLE4 );
		bathtub.AddAnimation( AnimationSet.IDLE_ANGRY, Loli.Animation.BATHTUB_ANGRY_IDLE_LOOP );
		bathtub.AddAnimation( AnimationSet.REFUSE, Loli.Animation.STAND_REFUSE );
		bathtub.AddAnimation( AnimationSet.IMPRESSED, Loli.Animation.STAND_IMPRESSED1 );
		bathtub.AddAnimation( AnimationSet.POKE_FACE_SOFT_RIGHT, Loli.Animation.BATHTUB_IDLE_FACE_POKE_1_RIGHT );
		bathtub.AddAnimation( AnimationSet.POKE_FACE_HARD_RIGHT, Loli.Animation.BATHTUB_IDLE_FACE_POKE_2_RIGHT );
		bathtub.AddAnimation( AnimationSet.HEADPAT_START_HAPPY, Loli.Animation.BATHTUB_HEADPAT_HAPPY_IDLE );
		bathtub.AddAnimation( AnimationSet.HEADPAT_START_ANGRY, Loli.Animation.BATHTUB_HEADPAT_ANGRY_IDLE );
		bathtub.AddAnimation( AnimationSet.HEADPAT_BRUSH_AWAY, Loli.Animation.BATHTUB_HEADPAT_BRUSH_AWAY );
		bathtub.AddAnimation( AnimationSet.HEADPAT_SUCCESS, Loli.Animation.BATHTUB_HEADAPAT_ANGRY_PROPER_TO_HAPPY );
		bathtub.AddAnimation( AnimationSet.HEADPAT_IDLE_HAPPY, Loli.Animation.BATHTUB_HEADPAT_HAPPY_IDLE);
		bathtub.AddAnimation( AnimationSet.HEADPAT_IDLE_ANGRY, Loli.Animation.BATHTUB_HEADPAT_ANGRY_IDLE);
		


		
		
		var horseback = new BodyStateAnimationSet( false );
		bodyStateAnimationSets[ (int)BodyState.HORSEBACK ] = horseback;
		horseback.AddAnimation( AnimationSet.IDLE_ANGRY, Loli.Animation.HORSEBACK_IDLE_LOOP);
		horseback.AddAnimation( AnimationSet.IDLE_HAPPY, Loli.Animation.HORSEBACK_IDLE_LOOP);
		horseback.bodyStateConnections[ BodyState.STAND ] = Animation.HORSEBACK_TO_STAND;

		//BATHTUB RELAX
		//BATHTUB ON KNEES
		//BATHTUB TOWEL IDLE

		
		
		var floorFaceUp = new BodyStateAnimationSet( true );
		bodyStateAnimationSets[ (int)BodyState.OFFBALANCE ] = floorFaceUp;
		floorFaceUp.AddAnimation( AnimationSet.IDLE_HAPPY, Loli.Animation.FLOOR_FACE_UP_IDLE);
		floorFaceUp.AddAnimation( AnimationSet.IDLE_ANGRY, Loli.Animation.FLOOR_FACE_UP_IDLE);
		
		
		var squat = new BodyStateAnimationSet( true );
		bodyStateAnimationSets[ (int)BodyState.SQUAT ] = squat;
		squat.AddAnimation( AnimationSet.IDLE_HAPPY, Loli.Animation.SQUAT_LOCOMOTION_HAPPY);
		squat.AddAnimation( AnimationSet.IDLE_ANGRY, Loli.Animation.SQUAT_LOCOMOTION_ANGRY);
		squat.AddAnimation( AnimationSet.IMPRESSED, Loli.Animation.SQUAT_IMPRESSED1);
		squat.AddAnimation( AnimationSet.POKE_FACE_SOFT_RIGHT, Loli.Animation.SQUAT_FACE_POKE_1_RIGHT);
		squat.AddAnimation( AnimationSet.POKE_FACE_HARD_RIGHT, Loli.Animation.SQUAT_FACE_POKE_2_RIGHT);
		squat.AddAnimation( AnimationSet.HEADPAT_START_HAPPY, Loli.Animation.SQUAT_HEADPAT_HAPPY_START);
		squat.AddAnimation( AnimationSet.HEADPAT_START_ANGRY, Loli.Animation.SQUAT_HEADPAT_ANGRY_START);
		squat.AddAnimation( AnimationSet.HEADPAT_END_WANTED_MORE, Loli.Animation.SQUAT_HEADPAT_HAPPY_WANTED_MORE);
		squat.AddAnimation( AnimationSet.HEADPAT_END_FULFILL, Loli.Animation.SQUAT_HEADPAT_HAPPY_SATISFACTION);
		squat.AddAnimation( AnimationSet.HEADPAT_BRUSH_AWAY, Loli.Animation.SQUAT_HEADPAT_ANGRY_BRUSH_AWAY);
		squat.AddAnimation( AnimationSet.HEADPAT_BRUSH_AWAY, Loli.Animation.SQUAT_HEADPAT_CLIMAX_TO_CANCEL_RIGHT);
		squat.AddAnimation( AnimationSet.HEADPAT_BRUSH_AWAY, Loli.Animation.SQUAT_HEADPAT_CLIMAX_TO_CANCEL_LEFT);
		squat.AddAnimation( AnimationSet.HEADPAT_ASK_FOR_MORE, Loli.Animation.SQUAT_HEADPAT_HAPPY_IDLE_SAD_RIGHT);
		squat.AddAnimation( AnimationSet.HEADPAT_ASK_FOR_MORE, Loli.Animation.SQUAT_HEADPAT_HAPPY_IDLE_SAD_LEFT);
		squat.AddAnimation( AnimationSet.HEADPAT_SUCCESS, Loli.Animation.SQUAT_HEADPAT_CLIMAX_TO_HAPPY);
		squat.AddAnimation( AnimationSet.HEADPAT_IDLE_HAPPY, Loli.Animation.SQUAT_HEADPAT_HAPPY_LOOP);
		squat.AddAnimation( AnimationSet.HEADPAT_IDLE_ANGRY, Loli.Animation.SQUAT_HEADPAT_ANGRY_LOOP);
		squat.bodyStateConnections[ BodyState.RELAX ] = Animation.SQUAT_TO_RELAX;
		squat.bodyStateConnections[ BodyState.STAND ] = Animation.SQUAT_TO_STAND;
		
		
		var relaxSquat = new BodyStateAnimationSet( true );
		bodyStateAnimationSets[ (int)BodyState.RELAX ] = relaxSquat;
		relaxSquat.AddAnimation( AnimationSet.IDLE_HAPPY, Loli.Animation.RELAX_IDLE_LOOP);
		relaxSquat.AddAnimation( AnimationSet.IDLE_ANGRY, Loli.Animation.RELAX_IDLE_LOOP);
		relaxSquat.AddAnimation( AnimationSet.POKE_FACE_SOFT_RIGHT, Loli.Animation.RELAX_TO_SQUAT_STARTLE);
		relaxSquat.AddAnimation( AnimationSet.POKE_FACE_HARD_RIGHT, Loli.Animation.RELAX_TO_SQUAT_STARTLE);
		relaxSquat.AddAnimation( AnimationSet.HEADPAT_START_HAPPY, Loli.Animation.RELAX_TO_SQUAT_STARTLE);
		relaxSquat.AddAnimation( AnimationSet.HEADPAT_START_ANGRY, Loli.Animation.RELAX_TO_SQUAT_STARTLE);
		relaxSquat.AddAnimation( AnimationSet.HEADPAT_IDLE_HAPPY, Loli.Animation.RELAX_TO_SQUAT_STARTLE);
		relaxSquat.AddAnimation( AnimationSet.HEADPAT_IDLE_ANGRY, Loli.Animation.RELAX_TO_SQUAT_STARTLE);
		relaxSquat.bodyStateConnections[ BodyState.SQUAT ] = Animation.RELAX_TO_SQUAT;


		var crawlTired = new BodyStateAnimationSet( true );
		bodyStateAnimationSets[ (int)BodyState.CRAWL_TIRED ] = crawlTired;
		crawlTired.AddAnimation( AnimationSet.IDLE_HAPPY, Loli.Animation.CRAWL_TIRED_IDLE);
		crawlTired.AddAnimation( AnimationSet.IDLE_ANGRY, Loli.Animation.CRAWL_TIRED_IDLE);
		crawlTired.bodyStateConnections[ BodyState.AWAKE_PILLOW_SIDE_RIGHT ] = Animation.CRAWL_TIRED_TO_LAY_PILLOW_SIDE_HAPPY_RIGHT;
		crawlTired.bodyStateConnections[ BodyState.AWAKE_PILLOW_SIDE_LEFT ] = Animation.CRAWL_TIRED_TO_LAY_PILLOW_SIDE_HAPPY_LEFT;
		crawlTired.bodyStateConnections[ BodyState.STAND ] = Animation.CRAWL_TIRED_TO_STAND;

		
		var awakeSidePillowRight = new BodyStateAnimationSet( true );
		bodyStateAnimationSets[ (int)BodyState.AWAKE_PILLOW_SIDE_RIGHT ] = awakeSidePillowRight;
		awakeSidePillowRight.AddAnimation( AnimationSet.IDLE_HAPPY, Loli.Animation.AWAKE_PILLOW_SIDE_HAPPY_IDLE_RIGHT);
		awakeSidePillowRight.AddAnimation( AnimationSet.IDLE_ANGRY, Loli.Animation.AWAKE_PILLOW_SIDE_HAPPY_IDLE_RIGHT);
		awakeSidePillowRight.AddAnimation( AnimationSet.HEADPAT_START_HAPPY, Loli.Animation.AWAKE_PILLOW_SIDE_HAPPY_IDLE_RIGHT);
		awakeSidePillowRight.bodyStateConnections[ BodyState.SLEEP_PILLOW_SIDE_RIGHT ] = Animation.AWAKE_PILLOW_SIDE_TO_SLEEP_PILLOW_SIDE_RIGHT;
		awakeSidePillowRight.bodyStateConnections[ BodyState.AWAKE_PILLOW_UP ] = Animation.AWAKE_PILLOW_SIDE_TO_AWAKE_PILLOW_UP_RIGHT;

		
		var awakeSidePillowLeft = new BodyStateAnimationSet( true );
		bodyStateAnimationSets[ (int)BodyState.AWAKE_PILLOW_SIDE_LEFT ] = awakeSidePillowLeft;
		awakeSidePillowLeft.AddAnimation( AnimationSet.IDLE_HAPPY, Loli.Animation.AWAKE_PILLOW_SIDE_HAPPY_IDLE_LEFT);
		awakeSidePillowLeft.AddAnimation( AnimationSet.IDLE_ANGRY, Loli.Animation.AWAKE_PILLOW_SIDE_HAPPY_IDLE_LEFT);
		awakeSidePillowLeft.AddAnimation( AnimationSet.HEADPAT_START_HAPPY, Loli.Animation.AWAKE_PILLOW_SIDE_HAPPY_IDLE_LEFT);
		awakeSidePillowLeft.bodyStateConnections[ BodyState.SLEEP_PILLOW_SIDE_LEFT ] = Animation.AWAKE_PILLOW_SIDE_TO_SLEEP_PILLOW_SIDE_LEFT;
		awakeSidePillowLeft.bodyStateConnections[ BodyState.AWAKE_PILLOW_UP ] = Animation.AWAKE_PILLOW_SIDE_TO_AWAKE_PILLOW_UP_LEFT;
		
		var awakePillowUp = new BodyStateAnimationSet( true );
		bodyStateAnimationSets[ (int)BodyState.AWAKE_PILLOW_UP ] = awakePillowUp;
		awakePillowUp.AddAnimation( AnimationSet.IDLE_HAPPY, Loli.Animation.AWAKE_HAPPY_PILLOW_UP_IDLE);
		awakePillowUp.AddAnimation( AnimationSet.IDLE_ANGRY, Loli.Animation.AWAKE_ANGRY_PILLOW_UP_IDLE);
		awakePillowUp.bodyStateConnections[ BodyState.CRAWL_TIRED ] = Animation.AWAKE_PILLOW_UP_TO_CRAWL_TIRED;
		
		
		var sleepSidePillowRight = new BodyStateAnimationSet( true );
		bodyStateAnimationSets[ (int)BodyState.SLEEP_PILLOW_SIDE_RIGHT ] = sleepSidePillowRight;
		sleepSidePillowRight.AddAnimation( AnimationSet.IDLE_HAPPY, Loli.Animation.SLEEP_PILLOW_SIDE_IDLE_RIGHT);
		sleepSidePillowRight.AddAnimation( AnimationSet.IDLE_ANGRY, Loli.Animation.SLEEP_PILLOW_SIDE_IDLE_RIGHT);
		sleepSidePillowRight.AddAnimation( AnimationSet.HEADPAT_START_HAPPY, Loli.Animation.SLEEP_PILLOW_SIDE_HEADPAT_START_RIGHT);
		sleepSidePillowRight.AddAnimation( AnimationSet.HEADPAT_BRUSH_AWAY, Loli.Animation.SLEEP_PILLOW_SIDE_TO_AWAKE_ANGRY_PILLOW_UP_RIGHT);
		sleepSidePillowRight.AddAnimation( AnimationSet.POKE_FACE_SOFT_RIGHT, Loli.Animation.SLEEP_PILLOW_SIDE_BOTHER_RIGHT);
		sleepSidePillowRight.AddAnimation( AnimationSet.POKE_FACE_HARD_RIGHT, Loli.Animation.SLEEP_PILLOW_SIDE_TO_SLEEP_PILLOW_UP_RIGHT);
		sleepSidePillowRight.bodyStateConnections[ BodyState.SLEEP_PILLOW_UP ] = Animation.SLEEP_PILLOW_SIDE_TO_SLEEP_PILLOW_UP_RIGHT;


		var sleepSidePillowLeft = new BodyStateAnimationSet( true );
		bodyStateAnimationSets[ (int)BodyState.SLEEP_PILLOW_SIDE_LEFT ] = sleepSidePillowLeft;
		sleepSidePillowLeft.AddAnimation( AnimationSet.IDLE_HAPPY, Loli.Animation.SLEEP_PILLOW_SIDE_IDLE_LEFT);
		sleepSidePillowLeft.AddAnimation( AnimationSet.IDLE_ANGRY, Loli.Animation.SLEEP_PILLOW_SIDE_IDLE_LEFT);
		sleepSidePillowLeft.AddAnimation( AnimationSet.HEADPAT_START_HAPPY, Loli.Animation.SLEEP_PILLOW_SIDE_HEADPAT_START_LEFT);
		sleepSidePillowLeft.AddAnimation( AnimationSet.HEADPAT_BRUSH_AWAY, Loli.Animation.SLEEP_PILLOW_SIDE_TO_AWAKE_ANGRY_PILLOW_UP_LEFT);
		sleepSidePillowLeft.AddAnimation( AnimationSet.POKE_FACE_SOFT_RIGHT, Loli.Animation.SLEEP_PILLOW_SIDE_BOTHER_LEFT);
		sleepSidePillowLeft.AddAnimation( AnimationSet.POKE_FACE_HARD_RIGHT, Loli.Animation.SLEEP_PILLOW_SIDE_TO_SLEEP_PILLOW_UP_LEFT);
		sleepSidePillowLeft.bodyStateConnections[ BodyState.SLEEP_PILLOW_UP ] = Animation.SLEEP_PILLOW_SIDE_TO_SLEEP_PILLOW_UP_LEFT;
		

		var sleepPillowUp = new BodyStateAnimationSet( true );
		bodyStateAnimationSets[ (int)BodyState.SLEEP_PILLOW_UP ] = sleepPillowUp;
		sleepPillowUp.AddAnimation( AnimationSet.IDLE_HAPPY, Loli.Animation.SLEEP_PILLOW_UP_IDLE);
		sleepPillowUp.AddAnimation( AnimationSet.IDLE_ANGRY, Loli.Animation.SLEEP_PILLOW_UP_IDLE);
		sleepPillowUp.bodyStateConnections[ BodyState.AWAKE_PILLOW_UP ] = Animation.SLEEP_PILLOW_UP_TO_AWAKE_HAPPY_PILLOW_UP;
		// sleepPillowUp.bodyStateConnections[ BodyState.AWAKE_PILLOW_UP ] = Animation.SLEEP_PILLOW_UP_TO_AWAKE_ANGRY_PILLOW_UP;
		
		
		//MAKE sPECIAL FUNCTIONS (e.g. relax poking makes stand up)

		// if( bodyStateInfos.Length != System.Enum.GetValues(typeof(BodyState)).Length ){
		// 	Debug.LogError("INSUFFICIENT BODY STATES INITIALIZED "+bodyStateInfos.Length+"/"+System.Enum.GetValues(typeof(BodyState)).Length);
		// 	Debug.Break();
		// }
		return bodyStateAnimationSets;
    }

}

}