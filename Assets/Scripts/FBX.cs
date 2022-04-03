using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace viva{

    public struct MuscleTemplate{
        public string boneName;
        public float mass;
        public float pitch;
        public float yaw;
        public float roll;
    }
    
    public class FBX: MonoBehaviour{
        private List<Model> m_rootModels = new List<Model>();
        public IList<Model> rootModels { get{ return m_rootModels.AsReadOnly(); } }


        public void _InternalAddRoot( Model model ){
            if( model == null ){
                Debug.LogError("Cannot add a null root model");
                return;
            }
            m_rootModels.Add( model );
        }

        public Model FindModel( string modelName ){
            foreach( var model in rootModels ){
                if( model.name == modelName ){
                    return model;
                }
            }
            return null;
        }

        public Model FindParentModel( Animation animation ){
            foreach( var model in rootModels ){
                if( System.Array.Exists( model.animations, elem=>elem==animation ) ){
                    return model;
                }
            }
            return null;
        }

        public Model FindParentModel( Transform transform ){
            foreach( var root in rootModels ){
                if( root.HasTransform( transform ) ) return root;
            }
            return null;
        }

        public Animation FindAnimation( string name ){
            foreach( var model in rootModels ){
                foreach( var anim in model.animations ){
                    if( anim.name == name ){
                        return anim;
                    }
                }
            }
            return null;
        }
    }
}