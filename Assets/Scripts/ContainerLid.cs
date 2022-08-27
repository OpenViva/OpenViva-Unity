using System.Collections;
using UnityEngine;

namespace viva
{


    public class ContainerLid : Item
    {

        [SerializeField]
        private Item.Type targetContainerType = Item.Type.NONE;
        [SerializeField]
        private Container currentContainer = null;
        [SerializeField]
        private AudioClip lidOnSound = null;
        [SerializeField]
        private AudioClip lidOffSound = null;

        private Coroutine closeJarCoroutine = null;
        private Coroutine tempDisablePhysicsCoroutine = null;

        protected override void OnItemAwake()
        {
            awakeWithoutRigidBody = currentContainer != null;
            if (currentContainer != null)
            {
                currentContainer.SetLid(this);
            }
        }

        public override void OnPostPickup()
        {
            if (currentContainer != null)
            {
                SoundManager.main.RequestHandle(transform.position).PlayOneShot(lidOffSound);
                currentContainer.SetLid(null);
                //temp disable physics so it doesn't collide with jar as it comes off of it
                tempDisablePhysicsCoroutine = GameDirector.instance.StartCoroutine(TempDisablePhysics(0.4f));
                currentContainer = null;
            }
        }

        public override void OnPreDrop()
        {
            //cancel temp disable physics if dropped
            if (tempDisablePhysicsCoroutine != null)
            {
                GameDirector.instance.StopCoroutine(tempDisablePhysicsCoroutine);
                tempDisablePhysicsCoroutine = null;
            }
        }

        public override bool CanBePlacedInRestParent()
        {
            return currentContainer == null;
        }

        private IEnumerator TempDisablePhysics(float seconds)
        {

            int oldLayer = gameObject.layer;
            gameObject.layer = WorldUtil.noneLayer;
            yield return new WaitForSeconds(seconds);
            gameObject.layer = oldLayer;
            tempDisablePhysicsCoroutine = null;
        }

        protected void OnCollisionEnter(Collision collision)
        {
            if (mainOccupyState != null)
            {
                return;
            }
            if (currentContainer != null)
            {
                return;
            }
            //Find jar to touch and close
            for (int i = 0; i < collision.contactCount; i++)
            {
                ContactPoint point = collision.GetContact(i);
                Container jar = Tools.SearchTransformAncestors<Container>(point.otherCollider.transform);
                if (jar == null)
                {
                    continue;
                }
                if (jar.isLidClosed)
                {
                    continue;
                }
                if (!jar.IsPointWithinLidArea(point.point))
                {
                    continue;
                }
                if (!jar.allowLid)
                {
                    continue;
                }
                if (jar.settings.itemType != targetContainerType)
                {
                    continue;
                }
                SoundManager.main.RequestHandle(transform.position).PlayOneShot(lidOnSound);
                currentContainer = jar;
                // ParentToTransform( currentContainer.transform );
                if (closeJarCoroutine != null)
                {
                    GameDirector.instance.StopCoroutine(closeJarCoroutine);
                }
                closeJarCoroutine = GameDirector.instance.StartCoroutine(CloseJarAnimation());
                return; //consume
            }
        }

        private IEnumerator CloseJarAnimation()
        {

            const float animDuration = 0.3f;    //seconds
            TransformBlend animBlend = new TransformBlend();
            animBlend.SetTarget(true, transform, true, true, 0.0f, 1.0f, animDuration);
            while (!animBlend.blend.finished)
            {
                if (currentContainer == null)
                {
                    break;
                }
                Vector3 jarLidPos = Vector3.up * (currentContainer.lidHeight + currentContainer.lidPlacementHeightOffset);
                animBlend.Blend(jarLidPos, Quaternion.identity);
                yield return null;
            }
            currentContainer.SetLid(this);
            closeJarCoroutine = null;
        }
    }

}