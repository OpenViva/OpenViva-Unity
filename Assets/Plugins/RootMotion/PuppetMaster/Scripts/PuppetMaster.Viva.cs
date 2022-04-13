using UnityEngine;

namespace RootMotion.Dynamics
{
    public partial class PuppetMaster : MonoBehaviour{
        
        public void SimulatePhysics( float stepMult ){
            Read();
            for( int i=0; i<muscles.Length; i++ ){
                var muscle = muscles[i];
                muscle.PinRotation( pinWeight );
                muscle.SetMuscleRotation( muscleWeight*muscleSpring, muscleDamper );
                // muscles[i].Update( pinWeight, muscleWeight*muscleSpring, muscleDamper, Time.fixedDeltaTime*stepMult );
            }
        }

        public void ClearVelocities(){
            foreach( Muscle muscle in muscles ){
                muscle.rigidbody.velocity = Vector3.zero;
                muscle.rigidbody.angularVelocity = Vector3.zero;
            }
        }

        public void SetEnableInternalCollisions( bool enable ){
            if( enable ){
                ResetInternalCollisions();
            }else{
                IgnoreInternalCollisions();
            }
        }

        public void SetEnableGravity( bool enable ){
            foreach( var muscle in muscles ){
                muscle.rigidbody.useGravity = enable;
            }
        }
        
        public void ApplyCurrentPose(){

            // Mapping
            if (mappingWeight > 0f){
                for( int i = 0; i<muscles.Length; i++ ){
                    muscles[i].Map( mappingWeight );
                }
            }else{
                // Moving to Target when in Kinematic mode
                // if (activeMode == Mode.Kinematic) MoveToTarget();
            }

            StoreTargetMappedState(); //@todo no need to do this all the time
            // foreach (Muscle m in muscles) m.CalculateMappedVelocity();   //WAS USED TO STORE VELOCITIES WHEN RAGDOLLING STARTS
        }
    }
}
