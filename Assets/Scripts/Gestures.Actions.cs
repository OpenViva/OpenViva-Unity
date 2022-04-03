using UnityEngine;
using UnityEditor;
using System.Collections;
using UnityEngine.UI;

using System.Collections.Generic;
 

namespace viva{

public delegate void GestureCallback( string gestureName, Character caller );


public partial class Gestures : MonoBehaviour { 
	

    [System.Serializable]
    public class GestureHand{

        public Transform target;
        public float signFlip = 1.0f;
        [HideInInspector] public Gestures gestures;

        private int follow_step = 0;
        private float follow_lastSwingTime = 0.0f;
        private int follow_targetSignForwardY = 0;
        private float follow_startForwardY = 0.0f;
        private float follow_lastRelativeForwardY;

        private float present_lastPalmResetTime = 0.0f;
        private float present_waitForReset = 0.0f;

        private float stop_lastPalmResetTime = 0.0f;
        private float stop_waitForReset = 0.0f;

        private int hello_step = 0;
        private float hello_lastRelativeUpX;
        private int hello_currWaveSign;
        private int hello_lastWaveSign;
        private float hello_lastWaveTime;
        private float hello_oldWaveTime;
        
        private Vector3 relativeForward;
        private Vector3 relativeUp;
        private Transform relativeFrame;

        private bool presented = false;
        
        public bool AttemptHello(){
            //hand point up and forward
            if( relativeUp.y > 0.6f && relativeForward.z > 0.6f ){
                
                const float minWaveDeltaX = 0.2f;
                if( relativeUp.x > hello_lastRelativeUpX+minWaveDeltaX ){
                    hello_currWaveSign = 1;
                    hello_lastRelativeUpX = relativeUp.x;
                }else if( relativeUp.x < hello_lastRelativeUpX-minWaveDeltaX ){
                    hello_currWaveSign = -1;
                    hello_lastRelativeUpX = relativeUp.x;
                }
                if( hello_currWaveSign != hello_lastWaveSign ){
                    float waveTime = Mathf.Abs( Time.time-hello_lastWaveTime );
                    if( Mathf.Abs( waveTime-hello_oldWaveTime ) < 0.125f ){
                        if( ++hello_step > 3 ){
                            hello_step = -4;
                            return true;
                        }
                    }else{
                        hello_step = 0;
                    }
                    hello_lastWaveTime = Time.time;
                    hello_oldWaveTime = waveTime;
                }
                hello_lastWaveSign = hello_currWaveSign;
            }else{
                if( hello_step != 0 ){
                    //Debug.LogError("!"+Mathf.Abs( 1.0f-relativeForward.y ));
                }
                hello_step = 0;
            }
            return false;
        }

        public bool? AttemptPresent(){
            if( relativeForward.y > 0.75f && relativeUp.z > 0.75f ){   //hand out

                Vector3 localHand = relativeFrame.InverseTransformPoint( target.position );
                if( Mathf.Abs( localHand.x ) < 0.3f &&    //hands x near head center
                    Mathf.Abs( localHand.y+0.2f ) < 0.2f &&    //hands y near head center
                    localHand.z > 0.2f                  //hands z in front of head
                    ){               
                    if( Time.time-present_lastPalmResetTime > 0.5f && present_waitForReset <= 0.0f ){
                        present_waitForReset = 0.3f;
                        presented = true;
                        return true;
                    }
                }else{
                    present_lastPalmResetTime = Time.time;
                }
            }else{
                present_lastPalmResetTime = Time.time;
                if( present_waitForReset > 0.0f ){
                    present_waitForReset -= Time.deltaTime;
                    if( present_waitForReset <= 0.0f && presented ){
                        presented = false;
                        return false;
                    }                
                }else{
                    present_waitForReset -= Time.deltaTime;
                }
            }
            return null;
        }

        public bool AttemptStop(){
            if( relativeUp.y > 0.6f && Mathf.Abs( relativeForward.x+signFlip*0.2f ) < 0.2f ){   //hand out
                Vector3 localHand = relativeFrame.InverseTransformPoint( target.position );
                if( Mathf.Abs( localHand.x ) < 0.3f &&    //hands x near head center
                    Mathf.Abs( localHand.y+0.2f ) < 0.2f &&    //hands y near head center
                    localHand.z > 0.3f                  //hands z in front of head
                    ){               
                    if( Time.time-stop_lastPalmResetTime > 0.5f && stop_waitForReset <= 0.0f ){
                        stop_waitForReset = 0.3f;
                        return true;
                    }
                }else{
                    stop_lastPalmResetTime = Time.time;
                }
            }else{
                stop_lastPalmResetTime = Time.time;
                if( stop_waitForReset > 0.0f ){
                    stop_waitForReset -= Time.deltaTime;
                    if( stop_waitForReset <= 0.0f ){
                        return false;
                    }                
                }else{
                    stop_waitForReset -= Time.deltaTime;
                }
            }
            return false;
        }
        
        public bool AttemptFollow(){
            if( follow_step < 5 ){
                if( Mathf.Abs(-0.4f*signFlip-relativeUp.x ) < 0.8f ){ //must twist hand to face up ~90 degrees
                    
                    Vector3 relativeForward = relativeFrame.InverseTransformDirection( target.forward );
                    if( relativeForward.z > 0.0f ){
                        follow_lastSwingTime = Time.time;
                        follow_step = 0;
                        return false;
                    }
                    int signForwardY = (int)Mathf.Sign( relativeForward.y-follow_lastRelativeForwardY );
                    if( signForwardY == 0 ){
                        signForwardY = -1;
                    }
                    if( follow_step == 0 ){
                        follow_targetSignForwardY = signForwardY;
                        follow_step++;
                        follow_lastSwingTime = Time.time-0.1f;
                        follow_startForwardY = relativeForward.y;

                    }else{
                        float swingDistance = Mathf.Abs( follow_startForwardY-relativeForward.y );
                        if( signForwardY != follow_targetSignForwardY ){
                            // Debug.Log("~X "+(Time.time-follow_lastSwingTime)+","+swingDistance+" @ "+relativeForward.y);
                            if( swingDistance > 0.15f ){
                                follow_targetSignForwardY = signForwardY;
                                if( Time.time-follow_lastSwingTime > 0.8f ){    //break conditions
                                    follow_step = 0;
                                }else if( Time.time-follow_lastSwingTime > 0.1f && Time.time-follow_lastSwingTime < 1f && swingDistance > 0.2f ){
                                    follow_step++;
                                    //Debug.Log("+"+follow_step);
                                    follow_lastSwingTime = Time.time;
                                    follow_startForwardY = relativeForward.y;
                                }
                            }
                        }
                    }
                    follow_lastRelativeForwardY = relativeForward.y;
                    
                }else{
                    follow_lastSwingTime = Time.time;
                    follow_step = 0;
                }
            }else{
                present_waitForReset = 1.0f; //cancel present
                follow_step = -4;
                return true;
            }
            return false;
        }

        public void ResetAll(){
            follow_step = 0;
            hello_step = 0;
        }

        
        public void CheckDetection(){ 
            if( !VivaPlayer.user || !VivaPlayer.user.camera ) return;
            relativeFrame = VivaPlayer.user.camera.transform;
            relativeUp = relativeFrame.InverseTransformDirection( target.up );
            relativeForward = relativeFrame.InverseTransformDirection( target.forward );

            string name = signFlip==1 ? "right" : "left";
            // Debug.LogError(name+" fw "+relativeForward);
            // Debug.LogError(name+" up "+relativeUp);

            if( AttemptHello() ){
                gestures.FireGesture( Gesture.HELLO, this );
            }else if( AttemptFollow() ){
                gestures.FireGesture( Gesture.FOLLOW, this );
            }else if( !VivaPlayer.user.isUsingKeyboard ){
                if( AttemptStop() ){
                    gestures.FireGesture( Gesture.STOP, this );
                }
            }
            bool? presenting = AttemptPresent();
            if( presenting.HasValue ){
                if( presenting.Value ){
                    gestures.FireGesture( Gesture.PRESENT_START, this );
                }else{
                    gestures.FireGesture( Gesture.PRESENT_END, this );
                }
            }
        }
    }
}

}