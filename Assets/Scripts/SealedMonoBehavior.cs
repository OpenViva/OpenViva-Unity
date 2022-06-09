using UnityEngine;

namespace viva
{


    public abstract class SealedMonoBehavior : MonoBehaviour
    {

        protected virtual void Awake()
        {
            enabled = false;
        }
        protected virtual void Start()
        {
        }
        public virtual void FixedUpdate()
        {
        }
        public virtual void Update()
        {
        }
        public virtual void LateUpdate()
        {
        }
        public virtual void OnEnable()
        {
        }
        public virtual void OnDisable()
        {
        }
        public virtual void OnDestroy()
        {
        }
    }

}