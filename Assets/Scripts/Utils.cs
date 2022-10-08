using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace viva
{

    public static class Util
    {

        public static bool IsImmovable(Collider collider)
        {
            if (collider == null) return false;
            if (collider.gameObject.layer == WorldUtil.wallsMask) return true;
            return false;
        }

        public static void IgnorePhysics(Collider[] a, Collider[] b, bool ignore)
        {
            foreach (var cA in a)
            {
                foreach (var cB in b)
                {
                    Physics.IgnoreCollision(cA, cB, ignore);
                }
            }
        }

        public static void RemoveNulls<T>(List<T> list)
        {
            if (list == null) return;
            for (int i = list.Count; i-- > 0;)
            {
                if (list[i] == null) list.RemoveAt(i);
            }
        }

        public static void IgnorePhysics(Collider a, Collider[] b, bool ignore)
        {
            foreach (var cB in b)
            {
                Physics.IgnoreCollision(a, cB, ignore);
            }
        }

        public static Character GetCharacter(Rigidbody rigidbody)
        {
            if (!rigidbody) return null;
            return rigidbody.gameObject.GetComponentInParent<Character>();
        }

        public static Character GetCharacter(Collider collider)
        {
            if (!collider) return null;
            return collider.gameObject.GetComponentInParent<Character>();
        }

        public static Item GetItem(Rigidbody rigidbody)
        {
            if (rigidbody && rigidbody.TryGetComponent<Item>(out Item item))
            {
                return item;
            }
            return null;
        }

        public static Light SetupLight(GameObject target)
        {
            if (target == null)
            {
                //Debugger.LogError("Cannot create a light with a null target");
                Debug.LogError("Cannot create a light with a null target");
                return null;
            }
            var light = target.GetComponent<Light>();
            if (light == null) light = target.AddComponent<Light>();

            light.useColorTemperature = false;
            light.intensity = 400;
            light.range = 4;

            return light;
        }

        private static float ParseFloat(string str)
        {
            return float.Parse(str, System.Globalization.CultureInfo.InvariantCulture);
        }
    }

}