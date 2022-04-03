using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace viva{


public class SpillListener: MonoBehaviour{

    private Item parent;
    private float spillTime = 0;
    public AttributesReturnCallback onSpillAttributes;


    public void _InternalSetup( AttributesReturnCallback _onSpillAttributes, Item _parent ){
        parent = _parent;
        onSpillAttributes = _onSpillAttributes;
        enabled = false;    //only run whenever collision happens
    }

    private void OnCollisionEnter( Collision collision ){
        enabled = true;
    }

    private void FixedUpdate(){
        if( !parent || onSpillAttributes == null ){
            GameObject.Destroy( this );
            return;
        }
        CheckSpill();
    }

    private void CheckSpill(){
        float downFactor = Vector3.Dot( parent.rigidBody.transform.up, Vector3.up );
        if( downFactor < 0.55f ){
            spillTime = Mathf.Min( 1, spillTime+Time.deltaTime*3f );
            if( spillTime == 1f ){
                spillTime = 0f;
                Spill.Create( onSpillAttributes.Invoke(), parent.rigidBody.worldCenterOfMass, 3f, "smoke", parent );
                onSpillAttributes = null;
            }
        }else{
            spillTime = Mathf.Max( 0, spillTime-Time.deltaTime*6f );
            if( parent.rigidBody.IsSleeping() ) enabled = false;
        }
    }
}

public class Spill: MonoBehaviour{

    public _InternalSpillGroup _internalSpillGroup;
    private float lifespan = 2f;
    private bool active = true;
    private ParticleSystem particleSystem = null;

    public Attribute[] Consume(){
        if( _internalSpillGroup != null && _internalSpillGroup.attributes != null ){
            var toConsume = _internalSpillGroup.attributes;
            _internalSpillGroup.Consume();
            _internalSpillGroup = null;
            return toConsume;
        }
        lifespan = 0;
        return null;
    }

    private void OnCollisionEnter( Collision collision ){
        if( collision.rigidbody ){
            if( collision.rigidbody.TryGetComponent<Item>( out Item item ) ){
                if( _internalSpillGroup == null || _internalSpillGroup._internalSourceItem == item ) return;   //ignore source item

                var validAttribute = Consume();
                if( validAttribute != null ) item.onReceiveSpill.Invoke( validAttribute );
                return;
            }else if( collision.rigidbody.TryGetComponent<Spill>( out Spill otherSpill ) ){
                return; //ignore other spill collisions
            }
        }
        _internalSpillGroup = null;
    }

    public void FixedUpdate(){
        lifespan -= Time.deltaTime;
        if( lifespan < 0 ){
            if( active ){
                active = false;
                Destroy( gameObject, 1 );
            }
        }
    }

    public void SetParticleSystemTemplate( ParticleSystem _particleSystem ){
        if( particleSystem ){
            GameObject.DestroyImmediate( particleSystem );
        }
        particleSystem = _particleSystem;
        if( particleSystem ){
            particleSystem.transform.SetParent( transform, true );
            particleSystem.transform.localPosition = Vector3.zero;
        }
    }

    public class _InternalSpillGroup{
        public Attribute[] attributes;
        public Spill[] spills;
        public Item _internalSourceItem;

        public _InternalSpillGroup( Attribute[] _attributes, Vector3 spawnPos, float lifespan, string effectTemplate, Item _sourceItem, Character _sourceCharacter ){
            foreach( var _attribute in _attributes ){
                if( _attribute == null ) throw new System.Exception("Cannot create a spill with an attributes array that has a null entry");
            }
            var particleCount = 4;
            spills = new Spill[ particleCount ];
            for( int i=0; i<particleCount; i++ ){
                var spillContainer = new GameObject("SPILL");
                spillContainer.transform.position = spawnPos+Random.insideUnitSphere*0.02f;
                spillContainer.layer = WorldUtil.itemsLayer;

                var spill = spillContainer.AddComponent<Spill>();
                spill._internalSpillGroup = this;

                var sc = spillContainer.AddComponent<SphereCollider>();
                sc.radius = 0.01f;

                var rigidBody = spillContainer.AddComponent<Rigidbody>();
                rigidBody.drag = 2f;
                rigidBody.mass = 0.1f;

                if( _sourceCharacter && _sourceCharacter.isBiped ){
                    Util.IgnorePhysics( sc, _sourceCharacter.biped.rightHand.colliders, true );
                    Util.IgnorePhysics( sc, _sourceCharacter.biped.rightArm.colliders, true );
                    Util.IgnorePhysics( sc, _sourceCharacter.biped.leftHand.colliders, true );
                    Util.IgnorePhysics( sc, _sourceCharacter.biped.leftArm.colliders, true );
                }

                spill.SetParticleSystemTemplate( ParticleSystemManager.CreateParticleSystem( effectTemplate, Vector3.zero ) );
            }
            attributes = _attributes;
            _internalSourceItem = _sourceItem;
        }

        public void Consume(){
            attributes = null;
            foreach( var spill in spills ){
                if( spill ) GameObject.Destroy( spill );
            }
        }
    }

    public static void Create( Attribute[] _attributes, Vector3 spawnPos, float lifespan, string effectTemplate, Item sourceItem, Character _sourceCharacter=null ){
        if( _attributes == null || _attributes.Length == 0 ) return;
        new _InternalSpillGroup( _attributes, spawnPos, lifespan, effectTemplate, sourceItem, _sourceCharacter );
    }
}

}