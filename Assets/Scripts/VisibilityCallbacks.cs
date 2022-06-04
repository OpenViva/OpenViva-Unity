using UnityEngine;
using UnityEngine.Events;

namespace viva
{


    public class VisibilityCallbacks : MonoBehaviour
    {

        public UnityEvent onVisible;
        public UnityEvent onInvisible;

        private bool visible = false;


        private void OnBecameVisible()
        {
            if (!visible)
            {
                onVisible?.Invoke();
                visible = true;
            }
        }
        private void OnBecameInvisible()
        {
            if (visible)
            {
                onInvisible?.Invoke();
                visible = false;
            }
        }

    }

}