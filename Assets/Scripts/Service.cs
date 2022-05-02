using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;

namespace viva{

using ServiceUserEntry = Tuple<Service.ServiceUser,Service.ServiceUser.SetupCallback>;

public abstract class Service: Mechanism {


    [System.Serializable]
    public class EmployeeInfo{
        public Vector3 localPos = Vector3.zero;
        public Vector3 localRootFacePos = Vector3.forward;
    }


    public class ServiceUser{

        public delegate void SetupCallback( Service service, EmployeeInfo info );

        public readonly Loli loli;
        public EmployeeInfo info { get; private set; }
        public Service service { get; private set; }

        public ServiceUser( Loli _loli, ref SetupCallback accessor ){
            loli = _loli;
            accessor = Setup;
        }

        private void Setup( Service _service, EmployeeInfo _info ){
            service = _service;
            info = _info;
        }
    }
    
    [SerializeField]
    private Texture2D employeeNametag;
    [SerializeField]
    private float employeeNametagYOffset; 
    [SerializeField]
    private ActiveBehaviors.Behavior m_targetBehavior;
    public ActiveBehaviors.Behavior targetBehavior { get{ return m_targetBehavior; } private set{ m_targetBehavior = value; } }
    [SerializeField]
    private List<EmployeeInfo> employeeInfos = new List<EmployeeInfo>();
    public int employeeInfosAvailable { get{ return employeeInfos.Count; } }
    
    private List<ServiceUser> activeServiceUsers = new List<ServiceUser>();
    // private MethodInfo onServiceMethod;

    private static List<ServiceUserEntry> serviceUserEntries = new List<ServiceUserEntry>();

    public override void OnMechanismAwake(){
    }

    public override sealed bool AttemptCommandUse( Loli targetLoli, Character commandSource ){
        if( targetLoli == null ){
            return false;
        }
        if( !targetLoli.IsHappy() || targetLoli.IsTired() ){	//must be happy and not tired
			targetLoli.active.idle.PlayAvailableRefuseAnimation();
			return false;
		}
        bool success = Employ( targetLoli );
        if( success ){
            targetLoli.active.SetTask( targetLoli.active.GetTask( targetBehavior ) );
            GameDirector.player.objectFingerPointer.selectedLolis.Remove( targetLoli );
            targetLoli.characterSelectionTarget.OnUnselected();
        }else{
            var playAnim = LoliUtility.CreateSpeechAnimation( targetLoli, AnimationSet.REFUSE, SpeechBubble.FULL );
            targetLoli.autonomy.Interrupt( playAnim );
        }
        return success;
    }

    protected abstract void OnInitializeEmployment( Loli targetLoli );
    

    private void OnTaskChange( Loli loli, ActiveBehaviors.Behavior newBehavior ){
        if( newBehavior != targetBehavior ){
            Unemploy( loli );
        }
    }

    private static ServiceUserEntry EnsureServiceUser( Loli loli ){
        if( loli == null ){
            return null;
        }
        for( int i=0; i<serviceUserEntries.Count; i++ ){
            if( serviceUserEntries[i]._1.loli == loli ){
                return serviceUserEntries[i];
            }
        }
        
        ServiceUser.SetupCallback accessor = null;
        var serviceUser = new ServiceUser( loli, ref accessor );
        var entry =  new ServiceUserEntry( serviceUser, accessor );
        serviceUserEntries.Add( entry );
        return entry;
    }

    public static int GetServiceIndex( Loli loli ){
        if( loli == null ){
            return -1;
        }
        var entry = EnsureServiceUser( loli );
        if( entry != null ){
            return GameDirector.instance.town.services.IndexOf( entry._1.service );
        }
        return -1;
    }

    public ServiceUser GetActiveServiceUser( int index ){
        if( index < 0 || index >= activeServiceUsers.Count ){
            return null;
        }
        return activeServiceUsers[index];
    }

    public override void OnDrawGizmosSelected(){
        base.OnDrawGizmosSelected();

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = Color.green;
        foreach( var employeeInfo in employeeInfos ){
            Vector3 worldPos = employeeInfo.localPos+Vector3.up*0.02f;
            Gizmos.DrawLine( worldPos, worldPos-Quaternion.Euler( 0.0f, +15, 0.0f )*employeeInfo.localRootFacePos*0.1f );
            Gizmos.DrawLine( worldPos, worldPos-employeeInfo.localRootFacePos*0.3f );
            Gizmos.DrawLine( worldPos, worldPos-Quaternion.Euler( 0.0f, -15, 0.0f )*employeeInfo.localRootFacePos*0.1f );
        }
    }

    private EmployeeInfo GetNextEmptyEmployeeInfo(){
        if( employeeInfos.Count > 0 ){
            return employeeInfos[0];
        }
        return null;
    }

    public EmployeeInfo GetEmployeeInfo( int index ){
        return employeeInfos[ index ];
    }

    public EmployeeInfo FindActiveEmployeeInfo( Loli loli ){
        if( loli == null ){
            return null;
        }
        foreach( var serviceUser in activeServiceUsers ){
            if( serviceUser.loli == loli ){
                return serviceUser.info;
            }
        }
        return null;
    }

    public bool Employ( Loli loli ){
        if( loli == null ){
            Debug.LogError("[Service] Cannot employ null loli");
            return false;
        }
        var entry = EnsureServiceUser( loli );
        if( entry == null ){
            Debug.LogError("[Service] Could not ensure service user");
            return false;
        }
        if( entry._1.service != null ){
            if( entry._1.service == this ){
                return true;
            }else{
                Debug.LogError("[Service] Loli already employed in a service");
                return false;
            }
        }
        EmployeeInfo targetInfo = GetNextEmptyEmployeeInfo();
        if( targetInfo == null ){
            return false;
        }

        //assign new employee info
        entry._2.Invoke( this, targetInfo );
        employeeInfos.Remove( targetInfo );
        activeServiceUsers.Add( entry._1 );
        loli.onTaskChange += OnTaskChange;

        if( activeServiceUsers.Count > 0 ){
            GameDirector.mechanisms.Add( this );
        }
        loli.SetNameTagTexture( employeeNametag, employeeNametagYOffset );
        OnInitializeEmployment( loli );
        return true;
    }

    private void Unemploy( Loli loli ){
        var entry = EnsureServiceUser( loli );
        if( entry == null ){
            Debug.LogError("[Service] Could not ensure service user");
            return;
        }
        if( entry._1.service != this ){
            Debug.LogError("[Service] Loli not employed to service "+this);
            return;
        }
        //restore employee info
        employeeInfos.Add( entry._1.info );
        entry._2.Invoke( null, null );
        activeServiceUsers.Remove( entry._1 );
        loli.onTaskChange -= OnTaskChange;

        loli.SetNameTagTexture( null, 0 );
        
        if( activeServiceUsers.Count == 0 ){
            GameDirector.mechanisms.Remove( this );
        }
    }
    
	public AutonomyMoveTo CreateGoToEmploymentPosition( Loli loli ){
		
		if( loli == null ){
			return null;
		}
		var employeeInfo = FindActiveEmployeeInfo( loli );
		if( employeeInfo == null ){
			Debug.LogError("[OnsenClerk] employee info is null");
			return null;
		}

		var receptionPos = transform.TransformPoint( employeeInfo.localPos );
		var walkToReception = new AutonomyMoveTo(
			loli.autonomy,
			"walk to reception",
			delegate( TaskTarget target ){ target.SetTargetPosition( receptionPos ); },
			0.1f,
			BodyState.STAND,
            delegate( TaskTarget target ){
                target.SetTargetPosition( receptionPos+transform.TransformDirection( employeeInfo.localRootFacePos ) );
            }
		);

		return walkToReception;
	}
}

}