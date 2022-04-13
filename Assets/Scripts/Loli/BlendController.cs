using System.Collections;
using System.Collections.Generic;
using UnityEngine;




namespace viva{


    public class BlendController{

        public delegate float OnIKControl( BlendController blendController );
        
        private float easeDuration;
        public readonly LoliHandState targetHandState;
        public readonly Loli.ArmIK armIK;
        public readonly Loli.ArmIK.RetargetingInfo retargetingInfo = new Loli.ArmIK.RetargetingInfo();
        public Loli.Animation targetAnimation;
        private readonly Tools.EaseBlend boundaryEase = new Tools.EaseBlend();
        private readonly OnIKControl controlCallback;
        private LoliHandState.HoldBlendMax holdBlendMax = new LoliHandState.HoldBlendMax();
        private bool registered = false;
        private bool listening = false;
        

        public BlendController( LoliHandState _targetHandState, Loli.Animation _targetAnim, OnIKControl _controlCallback, float _easeDuration=0.4f ){
            targetHandState = _targetHandState;
            if( targetHandState != null ){
                armIK = new Loli.ArmIK( targetHandState.holdArmIK );
                targetAnimation = _targetAnim;
                controlCallback = _controlCallback;
                Restore();

                //fire current animation state in case already in targetAnim
                Loli self = targetHandState.owner as Loli;
                ListenForAnimation( self.lastAnim, self.currentAnim );
            }

            easeDuration = _easeDuration;
        }

        public void Restore(){
            if( listening ){
                return;
            }
            listening = true;
            Loli self = targetHandState.owner as Loli;
            self.onAnimationChange += ListenForAnimation;
        }

        private void ListenForAnimation( Loli.Animation oldAnim, Loli.Animation newAnim ){

            if( newAnim == targetAnimation ){
                Register();
                return;
            }
            if( oldAnim == targetAnimation ){
                Unregister();
                return;
            }
        }

        private void Register(){
            if( registered ){
                return;
            }
            registered = true;

            Loli self = targetHandState.owner as Loli;
            self.AddModifyAnimationCallback( OnModifyAnimation );
            if( boundaryEase.getTarget() != 1.0f ){
                boundaryEase.StartBlend( 1.0f, easeDuration );
            }
        }

        private void Unregister(){
            if( !registered ){
                return;
            }
            registered = false;

            if( boundaryEase.getTarget() != 0.0f ){
                boundaryEase.StartBlend( 0.0f, easeDuration );
                Loli self = targetHandState.owner as Loli;
                self.onAnimationChange -= ListenForAnimation;
                listening = false;
            }
        }

        private void OnModifyAnimation(){
            boundaryEase.Update( Time.fixedDeltaTime );
            if( boundaryEase.value == 0.0f ){
                Loli self = targetHandState.owner as Loli;
                self.RemoveModifyAnimationCallback( OnModifyAnimation );
                targetHandState.RequestUnsetHoldBlendMax( holdBlendMax );
            }else{
                float desiredBlend = controlCallback( this );
                //negative values will cancel out regular hold retargeting
                if( desiredBlend < 0.0f ){
                    holdBlendMax.value = 1.0f-Mathf.Min( boundaryEase.value, -desiredBlend );
                    targetHandState.RequestSetHoldBlendMax( holdBlendMax );
                }else{
                    armIK.Apply( retargetingInfo, Mathf.Min( boundaryEase.value, desiredBlend ) );
                }
            }
        }
    }
}