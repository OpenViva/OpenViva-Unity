using System.Collections;
using UnityEngine;


namespace viva
{

    public class OnsenPool : Mechanism
    {

        [SerializeField]
        private ChangingRoom m_changingRoom;
        private ChangingRoom changingRoom { get { return m_changingRoom; } }
        [SerializeField]
        private OnsenReception m_onsenReception;
        public OnsenReception onsenReception { get { return m_onsenReception; } }
        [SerializeField]
        private Mesh waterFloorSampleMesh;
        [SerializeField]
        private Vector3[] randomFloorSamplePoints;


        public override void OnMechanismAwake()
        {
            //GameDirector.player.vivaControls.Keyboard.wave.performed += delegate
            //{
            //    for (int i = 1; i < GameDirector.characters.objects.Count; i++)
            //    {
            //        var loli = GameDirector.characters.objects[i] as Loli;
            //        if (loli)
            //        {
            //            loli.BeginRagdollMode(0.2f, Loli.Animation.FALLING_LOOP);
            //        }
            //    }

            //    GameDirector.instance.StartCoroutine(testNumerator());
            //};
        }

        private IEnumerator testNumerator()
        {

            var request = new Wardrobe.LoadClothingCardRequest("test_swimsuit.png");
            yield return GameDirector.instance.StartCoroutine(Wardrobe.main.HandleLoadClothingCard(request, null));
            Outfit resetOutfit = Outfit.Create(
            new string[]{
                },
                true
            );
            resetOutfit.AdditiveClothingPiece(request.clothingPreset, request.clothingOverride);

            (GameDirector.characters.objects[1] as Loli).SetOutfit(resetOutfit);
            (GameDirector.characters.objects[2] as Loli).SetOutfit(resetOutfit);
            (GameDirector.characters.objects[3] as Loli).SetOutfit(resetOutfit);
            (GameDirector.characters.objects[4] as Loli).SetOutfit(resetOutfit);
        }

        public Vector3 GetRandomWaterFloorPoint()
        {
            int triangles = randomFloorSamplePoints.Length / 3;
            int index = Random.Range(0, triangles) * 3;

            Vector3 a = transform.TransformPoint(randomFloorSamplePoints[index++]); ;
            Vector3 b = transform.TransformPoint(randomFloorSamplePoints[index++]);
            Vector3 c = transform.TransformPoint(randomFloorSamplePoints[index]);

            Vector3 d = a + (b - a) * Random.value;
            return d + (c - d) * Random.value;
        }

        public override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = new Color(0.0f, 1.0f, 0.0f, 0.7f);
            Gizmos.DrawMesh(waterFloorSampleMesh, 0, transform.position, transform.rotation);
        }

        public override bool AttemptCommandUse(Loli targetLoli, Character commandSource)
        {
            var basket = changingRoom.GetNextEmptyBasket();
            if (basket == null)
            {
                return false;
            }
            targetLoli.active.onsenSwimming.swimmingSession.activePoolAsset = this;
            targetLoli.active.onsenSwimming.swimmingSession.activeBasketAsset = basket;
            targetLoli.active.SetTask(targetLoli.active.onsenSwimming);
            return true;
        }

        public override void EndUse(Character targetCharacter)
        {
        }
    }

}