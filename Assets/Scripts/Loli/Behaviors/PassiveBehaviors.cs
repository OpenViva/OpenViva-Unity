using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace viva{


public partial class PassiveBehaviors : Job{

	private enum Behavior{
		KISSING,
		LEWD,
		HEADPAT,
		HANDHOLD,
		POKE,
		ENVIRONMENT,
		CLOTHING,
		TIRED,
		HUG_LISTENER,
		POKER_LISTENER,
		SCARED,
		PROXIMITY
	}
	

    public class PassiveTask: Task{
		
		private protected float runInterval;
		private float intervalTimer = 0.0f;

        public PassiveTask( Loli _self, float _runInterval ):base(_self,JobType.PASSIVE){
			runInterval = _runInterval;
        }

		public bool UpdateIntervalTimer(){
			intervalTimer += Time.deltaTime;
			return intervalTimer >= runInterval;
		}

		public void ResetIntervalTimer(){
			if( runInterval == 0.0f ){
				intervalTimer = 0.0f;
			}else{
				intervalTimer %= runInterval;
			}
		}
    }
	

	public readonly PassiveBehaviorSettings settings;
	private PassiveTask[] passives = new PassiveTask[ System.Enum.GetValues(typeof(Behavior)).Length ];
	public Set<Character> nearbyCharacters { get; private set; } = new Set<Character>();
	public List<Tuple<Item,int>> nearbyItems { get; private set; } = new List<Tuple<Item,int>>();

	public PassiveBehaviors( Loli _self, PassiveBehaviorSettings _passiveBehaviorSettings ):base(_self,Job.JobType.PASSIVE){
		
		settings = _passiveBehaviorSettings;

		passives[ (int)Behavior.KISSING ] = new KissingBehavior( self );
		passives[ (int)Behavior.LEWD ] = new LewdBehavior( self );
		passives[ (int)Behavior.HEADPAT ] = new HeadpatBehavior( self );
		passives[ (int)Behavior.HANDHOLD ] = new HandholdBehavior( self );
		passives[ (int)Behavior.POKE ] = new PokeBehavior( self );
		passives[ (int)Behavior.ENVIRONMENT ] = new EnvironmentBehavior( self );
		passives[ (int)Behavior.CLOTHING ] = new ClothingBehavior( self );
		passives[ (int)Behavior.TIRED ] = new TiredBehavior( self );
		passives[ (int)Behavior.HUG_LISTENER ] = new HugListenerBehavior( self );
		passives[ (int)Behavior.POKER_LISTENER ] = new PokerListenerBehavior( self );
		passives[ (int)Behavior.SCARED ] = new ScaredBehaviour( self );
		passives[ (int)Behavior.PROXIMITY ] = new ProximityBehavior( self );
	}
	
	public KissingBehavior kissing { get{ return passives[(int)Behavior.KISSING] as KissingBehavior; } }
	public LewdBehavior lewd { get{ return passives[(int)Behavior.LEWD] as LewdBehavior; } }
	public HeadpatBehavior headpat { get{ return passives[(int)Behavior.HEADPAT] as HeadpatBehavior; } }
	public HandholdBehavior handhold { get{ return passives[(int)Behavior.HANDHOLD] as HandholdBehavior; } }
	public PokeBehavior poke { get{ return passives[(int)Behavior.POKE] as PokeBehavior; } }
	public EnvironmentBehavior environment { get{ return passives[(int)Behavior.ENVIRONMENT] as EnvironmentBehavior; } }
	public ClothingBehavior clothing { get{ return passives[(int)Behavior.CLOTHING] as ClothingBehavior; } }
	public TiredBehavior tired { get{ return passives[(int)Behavior.TIRED] as TiredBehavior; } }
	public HugListenerBehavior hug { get{ return passives[(int)Behavior.HUG_LISTENER] as HugListenerBehavior; } }
	public ScaredBehaviour scared { get{ return passives[(int)Behavior.SCARED] as ScaredBehaviour; } }
	public ProximityBehavior proximity { get{ return passives[(int)Behavior.PROXIMITY] as ProximityBehavior; } }

	private List<PassiveTask> passiveTasksToExecute = new List<PassiveTask>();

	//does not run every frame
    public override void OnFixedUpdate(){
		for( int i=0; i<passives.Length; i++ ){
			passives[i].OnFixedUpdate();
		}
	}
	//runs before anything else
    public override void OnUpdate(){
		
		//reset execute queue every frame
		passiveTasksToExecute.Clear();
		for( int i=0; i<passives.Length; i++ ){
			var passive = passives[i];
			if( passive.UpdateIntervalTimer() ){
				passiveTasksToExecute.Add( passive );
			}
		}
		for( int i=0; i<passiveTasksToExecute.Count; i++ ){
			passiveTasksToExecute[i].OnUpdate();
		}
	}
    public override void OnLateUpdate(){
		for( int i=0; i<passiveTasksToExecute.Count; i++ ){
			passiveTasksToExecute[i].OnLateUpdate();
		}
	}
    public override void OnLateUpdatePostIK(){
		for( int i=0; i<passiveTasksToExecute.Count; i++ ){
			var passive = passiveTasksToExecute[i];
			passive.OnLateUpdatePostIK();
			passive.ResetIntervalTimer();
		}

	}
    public override void OnAnimationChange( Loli.Animation oldAnim, Loli.Animation newAnim ){
		for( int i=0; i<passives.Length; i++ ){
			passives[i].OnAnimationChange( oldAnim, newAnim );
		}
	}
	public void OnItemTriggerEnter( Item item ){
		if( item == null ){
			return;
		}
		foreach( var tuple in nearbyItems ){
			if( tuple._1 == item ){
				tuple._2++;
				return;
			}
		}
		nearbyItems.Add( new Tuple<Item,int>( item, 1 ) );
		if( item.settings.itemType == Item.Type.CHARACTER ){
			nearbyCharacters.Add( item.mainOwner );
			return;
		}
	}
	public void OnItemTriggerExit( Item item ){
		if( item == null ){
			return;
		}
		for( int i=0; i<nearbyItems.Count; i++ ){
			var tuple = nearbyItems[i];
			if( nearbyItems[i]._1 == item ){
				if( --tuple._2 == 0 ){
					nearbyItems.RemoveAt(i);
					if( item.settings.itemType == Item.Type.CHARACTER ){
						nearbyCharacters.Remove( item.mainOwner );
						return;
					}
					break;
				}
			}
		}
	}
	
	public void OnGesture( Item source, ObjectFingerPointer.Gesture gesture ){
		
		//stop at first accepted gesture
		for( int i=0; i<passives.Length; i++ ){
			if( passives[i].OnGesture( source, gesture ) ){
				break;
			}
		}
	}
}

}