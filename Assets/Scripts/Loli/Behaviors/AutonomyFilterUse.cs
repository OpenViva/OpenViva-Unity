using System.Collections;
using UnityEngine;


namespace viva
{


    public class AutonomyFilterUse : Autonomy.Task
    {

        private readonly FilterUse filterUse;
        private readonly float delayUse;
        private bool finishedUse = false;
        private bool failIfOccupied = false;

        public int queueIndex
        {
            get
            {
                if (filterUse != null)
                {
                    return filterUse.GetQueueIndex(self);
                }
                else
                {
                    return 0;
                }
            }
        }


        public AutonomyFilterUse(Autonomy _autonomy, string _name, FilterUse _filterUse, float _delayUse, bool _failIfOccupied = false) : base(_autonomy, _name)
        {
            filterUse = _filterUse;
            delayUse = _delayUse;
            failIfOccupied = _failIfOccupied;

            onRemovedFromQueue += FinishUse;
        }

        private void FinishUse()
        {
            if (!finishedUse)
            {
                finishedUse = true;
                GameDirector.instance.StartCoroutine(DelayRemoveOwner(delayUse));
            }
        }

        private IEnumerator DelayRemoveOwner(float delay)
        {
            yield return new WaitForSeconds(delay);
            filterUse.RemoveOwner(self);
        }

        public override bool? Progress()
        {
            if (filterUse == null)
            {
                return false;
            }
            if (!finishedUse)
            {
                filterUse.SetOwner(self);
            }
            if (filterUse.owner == self)
            {
                return true;
            }
            if (failIfOccupied)
            {
                return false;
            }
            else
            {
                return null;
            }
        }
    }

}