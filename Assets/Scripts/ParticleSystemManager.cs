using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace viva{


public class ParticleSystemManager : MonoBehaviour{

    public static ParticleSystemManager main;

    [SerializeField]
    public ParticleSystem[] templates;

    public void Awake(){
        main = this;
    }

    private static ParticleSystem FindTemplate( string templateName ){
        foreach( var template in main.templates ){
            if( template.name == templateName ){
                return template;
            }
        }
        return null;
    }

    public static ParticleSystem CreateParticleSystem( string templateName, Vector3 position, Transform parent=null ){
        var template = FindTemplate( templateName );
        if( template ){
            var pSys = ParticleSystem.Instantiate( template, position, Quaternion.identity, parent );
            return pSys;
        }
        return null;
    }
}

}