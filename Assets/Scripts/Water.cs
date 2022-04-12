using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace viva{

public class Water : MonoBehaviour{

    public class InstanceCounter{
        public VivaInstance instance;
        public List<Collider> colliders;
        public List<Collider> splashed = new List<Collider>();
        public float totalVolume;

        public InstanceCounter( VivaInstance _instance, Collider _collider ){
            instance = _instance;
            colliders = new List<Collider>(){ _collider };
            
            var item = instance as Item;
            if( item ){
                foreach( var collider in item.colliders ){
                    totalVolume += Tools.ApproximateVolume( collider );
                }
            }
            var character = instance as Character;
            if( character ){
                foreach( var muscle in character.ragdoll.muscles ){
                    totalVolume += Tools.ApproximateVolume( muscle.colliders[0] );
                }
            }
        }
    }

    [SerializeField]
    public float dampening = 0.85f;
    [SerializeField]
    private ParticleSystem loadFX;
    [SerializeField]
    private ParticleSystem auraFX;
    [SerializeField]
    private float minimumLoadSqVel = 1f;
    [SerializeField]
    private float characterBuoyancyAlpha = 1.4f;
    [SerializeField]
    private bool m_disableSwimming = false;
    public bool disableSwimming { get{ return m_disableSwimming; } }
    [SerializeField]
    private bool m_buoyancy = true;
    public bool buoyancy { get{ return m_buoyancy; } }
    
    private int clearCounter = 0;
    public float surfaceY { get; private set; }


    private void Awake(){
        UpdateSurfaceY();
        enabled = false;

        Sound.PreloadSet( "water" );
    }

    private void UpdateSurfaceY(){
        var colliders = gameObject.GetComponentsInChildren<Collider>();
        if( colliders.Length == 0 ) return;

        var collider = colliders[0];
        var bounds = new Bounds( collider.bounds.center, collider.bounds.size );
        for( int i=1; i<colliders.Length; i++ ){
            collider = colliders[i];
            bounds.Encapsulate( collider.bounds );
        }
        surfaceY = bounds.max.y;
    }
    
    private List<InstanceCounter> instancesInWater = new List<InstanceCounter>();

    public void OnTriggerEnter( Collider collider ){
        var instance = collider.GetComponentInParent<VivaInstance>();
        if( instance ){
            InstanceCounter entry = null;
            for( int i=0; i<instancesInWater.Count; i++ ){
                var candidate = instancesInWater[i];
                if( candidate.instance == instance ){
                    entry = candidate;
                    break;
                }
            }
            if( entry != null ){
                entry.colliders.Add( collider );
            }else{
                instancesInWater.Add( new InstanceCounter( instance, collider ) );
                (instance as Character)?.onWater.Invoke( this );
            }
            enabled = true;
        }
    }

    public void OnTriggerExit( Collider collider ){
        var instance = collider.GetComponentInParent<VivaInstance>();
        if( instance ){
            InstanceCounter entry = null;
            for( int i=0; i<instancesInWater.Count; i++ ){
                var candidate = instancesInWater[i];
                if( candidate.instance == instance ){
                    entry = candidate;
                    break;
                }
            }
            if( entry != null ){
                entry.colliders.Remove( collider );
                if( entry.colliders.Count == 0 ){
                    instancesInWater.Remove( entry );
                    (entry.instance as Character)?.onWater.Invoke( null );
                }
            }
        }
    }

    public void FixedUpdate(){
        if( instancesInWater.Count == 0 ){
            enabled = false;
            return;
        }
        
        for( int i=instancesInWater.Count; i-->0; ){
            var entry = instancesInWater[i];
            for( int j=entry.colliders.Count; j-->0; ){
                var collider = entry.colliders[j];
                if( collider == null ){
                    entry.colliders.RemoveAt(j);
                    continue;
                }
            }
            if( entry.colliders.Count == 0 ){
                instancesInWater.RemoveAt(i);
                (entry.instance as Character)?.onWater.Invoke( null );
                continue;
            }
            if( entry.totalVolume <= Mathf.Epsilon ) continue;

            var item = entry.instance as Item;
            if( item ){
                foreach( var collider in item.colliders ){
                    ApplyBuoyancy( entry, collider, item.rigidBody, entry.totalVolume, 2f );
                }
                continue;
            }
            var character = entry.instance as Character;
            if( character ){            
                foreach( var muscle in character.ragdoll.muscles ){ 
                    ApplyBuoyancy( entry, muscle.colliders[0], muscle.rigidBody, entry.totalVolume, 1.4f*characterBuoyancyAlpha );
                }
                if(buoyancy){
                    if( character.isBiped ) character.ragdoll.movementBody.AddForce( Vector3.up*(surfaceY-character.biped.upperSpine.rigidBody.worldCenterOfMass.y)*0.04f, ForceMode.VelocityChange );
                }
                //Probably unnecessary
                else{
                    characterBuoyancyAlpha = 0;
                }   
                ApplyBuoyancy( entry, character.ragdoll.capsuleCollider, character.ragdoll.movementBody, 0.5f, characterBuoyancyAlpha );                   
            }
        }
    }

    public float DEBUG = 10f;

    private void ApplyBuoyancy( InstanceCounter entry, Collider collider, Rigidbody rigidBody, float totalVolume, float floatMult ){
        if( !collider ) return;
        if( !rigidBody ) return;
        if( rigidBody.IsSleeping() ) return;

        float volume = Tools.ApproximateVolume( collider );
        if( volume <= Mathf.Epsilon ) return;

        var submergePercent = CalculateSubmergePercent( collider, out Vector3 center, out float radius );
        rigidBody.AddForceAtPosition( -Physics.gravity*rigidBody.mass*submergePercent*floatMult, center, ForceMode.Force );

        float sqVel = rigidBody.GetPointVelocity( center ).sqrMagnitude;
        if( sqVel > minimumLoadSqVel ){
            var vel = Mathf.Sqrt( sqVel );
            if( !entry.splashed.Contains( collider ) ){
                if( submergePercent > 0.1f ){
                    entry.splashed.Add( collider );
                    
                    var handle = Sound.Create( center, null );
                    handle.pitch = 0.85f+Random.value*0.3f;
                    handle.volume = Tools.RemapClamped( 0.2f, 1.2f, 0.1f, 1f, radius*vel );
                    handle.Play( Sound.Load("water",radius*vel>2f ? "big splash":"medium splash" ) );

                    SplashLoad( collider, center, radius, vel );
                    SplashAura( center, radius );
                }
            }else if( submergePercent < 0.05f ){
                entry.splashed.Remove( collider );
            }
        }else{
            if( submergePercent <= 0f ){
                entry.splashed.Remove( collider );
            }
        }

        
        rigidBody.velocity *= Mathf.Pow( dampening, submergePercent );
        rigidBody.angularVelocity *= Mathf.Pow( dampening, submergePercent );
    }

    public void SplashLoad( Collider collider, Vector3 center, float radius, float velocity ){
        var emitParams = new ParticleSystem.EmitParams();
        center.y = surfaceY;
        emitParams.position = new Vector3( center.x, surfaceY, center.z );
        emitParams.startSize = radius*2.5f;
        for( int i=0; i<3; i++ ){
            emitParams.velocity = (Random.onUnitSphere+Vector3.up)*Mathf.Clamp( velocity*0.4f, 0.1f, 2f );
            loadFX?.Emit(emitParams, 1);
        }

        SplashAura( center, radius );
    }

    public void SplashAura( Vector3 center, float radius ){
        if( radius < 0.1f ) return;
        center.y = surfaceY;
        var auraParams = new ParticleSystem.EmitParams();
        auraParams.position = center;
        auraParams.startSize = radius*8f;
        auraFX?.Emit( auraParams, 1 );
    }

    private float CalculateSubmergePercentSphere( float y, float radius ){
        var h = surfaceY-y;
        var area = Tools.CircleArea( radius );
        if( area <= Mathf.Epsilon ) return 0;
        
        var sectorArea = Tools.CircleSectorArea( radius, h );
        return 1f-sectorArea/area;
    }

    private float CalculateSubmergePercent( Collider collider, out Vector3 center, out float radius ){
        var sc = collider as SphereCollider;
        if( sc ){
            center = collider.transform.TransformPoint( sc.center );
            radius = sc.transform.lossyScale.y*sc.radius;
            return CalculateSubmergePercentSphere( center.y, radius );
        }
        var cc = collider as CapsuleCollider;
        if( cc ){
            var heightOnly = Mathf.Max( cc.height, cc.radius*2 )/2-cc.radius;
            var heightSide = Tools.DirToVector3( cc.direction )*heightOnly;
            var side1 = CalculateSubmergePercentSphere( cc.transform.TransformPoint( cc.center+heightSide ).y, cc.transform.lossyScale.y*cc.radius );
            var side2 = CalculateSubmergePercentSphere( cc.transform.TransformPoint( cc.center-heightSide ).y, cc.transform.lossyScale.y*cc.radius );
            center = collider.transform.TransformPoint( cc.center );
            radius = cc.transform.lossyScale.y*Mathf.Max( cc.radius*2, cc.height );
            return ( side1+side2 )/2f;
        }
        var bc = collider as BoxCollider;
        if( bc ){
            center = collider.transform.TransformPoint( bc.center );
            radius = bc.transform.lossyScale.y*bc.size.magnitude;
            return CalculateSubmergePercentSphere( center.y, radius );
        }
        var mc = collider as MeshCollider;
        if( mc ){
            center = collider.transform.TransformPoint( mc.sharedMesh.bounds.center );
            radius = mc.transform.lossyScale.y*mc.sharedMesh.bounds.size.magnitude;
            return CalculateSubmergePercentSphere( center.y, radius );
        }
        center = Vector3.zero;
        radius = 0f;
        return 0;
    }
}

}