using UnityEngine;


namespace viva
{

    public class WaterSplashParticleCallback : MonoBehaviour
    {
        private void OnParticleCollision(GameObject gameObject)
        {
            WaterSplashParticleCallback.OnWaterCollision(gameObject, transform.position);
        }

        public static void OnWaterCollision(GameObject gameObject, Vector3 sourcePos)
        {
            CharacterCollisionCallback ccc = gameObject.GetComponent<CharacterCollisionCallback>();
            if (ccc)
            {
                Player player = ccc.owner as Player;
                if (player)
                {
                    GameDirector.instance.postProcessing.DisplayScreenEffect(GamePostProcessing.Effect.SPLASH);
                    return;
                }
            }
            Loli loli = gameObject.GetComponent<Loli>();
            if (loli)
            {
                loli.passive.environment.AttemptReactToSubstanceSpill(SubstanceSpill.Substance.WATER, sourcePos);
            }
        }
    }

}