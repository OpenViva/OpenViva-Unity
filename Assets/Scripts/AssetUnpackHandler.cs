using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace viva{


public class AssetUnpackHandler : MonoBehaviour{

    [SerializeField]
    private ParticleSystem assetUnpackParticleSystem;

    private List<ImportRequest> requests = new List<ImportRequest>();
    private ParticleSystem.Particle[] particles;


    public void Handle( ImportRequest request ){
        if( request == null ) return;
        requests.Add( request );
        request._internalOnImported += delegate{ requests.Remove( request ); BuildParticleList(); };
        BuildParticleList();
    }

    private void BuildParticleList(){
        particles = new ParticleSystem.Particle[ requests.Count ];
        for( int i=0; i<particles.Length; i++ ){
            float lifetime = Mathf.Infinity;
            var particle = new ParticleSystem.Particle();
            particle.position = Vector3.up*i;
            particle.startSize = 1.0f;
            particle.remainingLifetime = lifetime;
            particle.startLifetime = lifetime;
            particle.startColor = Color.grey;
            particles[i] = particle;
        }
        assetUnpackParticleSystem.SetParticles( particles );
    }
}

}