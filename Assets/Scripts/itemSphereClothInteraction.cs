using System.Collections.Generic;
using UnityEngine;


namespace viva
{

    public class itemSphereClothInteraction : MonoBehaviour
    {

        public Cloth cloth;
        [Range(1, 4)]
        [SerializeField]
        private int maxColliders = 3;
        [SerializeField]
        private float minimumRadius = 0.04f;

        private Set<SphereCollider> sphereColliders = new Set<SphereCollider>();
        private Set<CapsuleCollider> capsuleColliders = new Set<CapsuleCollider>();


        private void OnTriggerEnter(Collider collider)
        {
            var newSphere = collider.GetComponentInChildren<SphereCollider>();
            if (newSphere && !newSphere.isTrigger && newSphere.radius > 0.04f && sphereColliders.Count < maxColliders)
            {
                sphereColliders.Add(newSphere);
                UpdateSphereArray();
            }

            var newCapsule = collider.GetComponentInChildren<CapsuleCollider>();
            if (newCapsule && capsuleColliders.Count < maxColliders)
            {
                capsuleColliders.Add(newCapsule);
                UpdateCapsuleArray();
            }
        }

        private void OnTriggerExit(Collider collider)
        {
            var newSphere = collider.GetComponentInChildren<SphereCollider>();
            if (newSphere && !newSphere.isTrigger)
            {
                sphereColliders.Remove(newSphere);
                UpdateSphereArray();
            }
            var newCapsule = collider.GetComponentInChildren<CapsuleCollider>();
            if (newCapsule)
            {
                capsuleColliders.Remove(newCapsule);
                UpdateCapsuleArray();
            }
        }

        private void UpdateSphereArray()
        {
            var list = new List<ClothSphereColliderPair>();
            foreach (var sphere in sphereColliders.objects)
            {
                list.Add(new ClothSphereColliderPair(sphere));
            }
            cloth.sphereColliders = list.ToArray();
        }

        private void UpdateCapsuleArray()
        {
            for (int i = capsuleColliders.Count; i-- > 0;)
            {
                if (capsuleColliders.objects[i] == null)
                {
                    capsuleColliders.objects.RemoveAt(i);
                }
            }

            cloth.capsuleColliders = capsuleColliders.objects.ToArray();
        }
    }

}