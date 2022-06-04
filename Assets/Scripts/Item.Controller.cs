using System.Collections;
using UnityEngine;

namespace viva
{


    public partial class Item : VivaSessionAsset
    {

        public enum EnableMode
        {
            NONE,
            PERSIST,
            TEMPORARY
        }

        private Coroutine logicCooldownCoroutine = null;
        private EnableMode m_enableMode = EnableMode.NONE;
        public EnableMode enableMode { get { return m_enableMode; } }


        public void EnableItemLogic()
        {
            //stop any disable cooldowns
            if (logicCooldownCoroutine != null)
            {
                GameDirector.instance.StopCoroutine(logicCooldownCoroutine);
                logicCooldownCoroutine = null;
            }
            if (GameDirector.items.Contains(this))
            {
                return;
            }
            m_enableMode = EnableMode.PERSIST;
            InitializeItemLogic();
        }

        private void InitializeItemLogic()
        {
            SetEnableStatusBar(true);
            GameDirector.items.Add(this);
        }

        public void DisableItemLogic()
        {
            if (settings.permanentlyEnabled)
            {
                return;
            }
            if (!GameDirector.items.Contains(this))
            {
                return;
            }
            m_enableMode = EnableMode.NONE;
            //Items will persist in their "on" state after being called for disabling to permit momentary ownerless logic (like throwing items)
            StartItemLogicCooldown(true, 5.0f);
        }

        public void EnableItemLogicTemporarily(float time)
        {
            if (settings.permanentlyEnabled)
            {
                return;
            }
            //temporary only allowed if enable source is not PERSIST
            if (m_enableMode == EnableMode.PERSIST)
            {
                return;
            }
            if (m_enableMode == EnableMode.NONE && !GameDirector.items.Contains(this))
            {
                InitializeItemLogic();
            }
            else
            {
                SetEnableStatusBar(true);
            }
            m_enableMode = EnableMode.TEMPORARY;
            StartItemLogicCooldown(false, time);
        }

        private void StartItemLogicCooldown(bool disableStatusBarAtBeginning, float timer)
        {
            if (logicCooldownCoroutine != null)
            {
                GameDirector.instance.StopCoroutine(logicCooldownCoroutine);
            }
            logicCooldownCoroutine = GameDirector.instance.StartCoroutine(ItemLogicCooldown(disableStatusBarAtBeginning, timer));
        }

        private IEnumerator ItemLogicCooldown(bool disableStatusBarAtBeginning, float timer)
        {
            if (disableStatusBarAtBeginning)
            {
                SetEnableStatusBar(false);
            }
            yield return new WaitForSeconds(timer);

            if (this == null)
            {
                yield break;
            }
            if (!disableStatusBarAtBeginning)
            {
                SetEnableStatusBar(false);
            }
            OnUnregisterItemLogic();
            GameDirector.items.Remove(this);
            m_enableMode = EnableMode.NONE;
            logicCooldownCoroutine = null;
        }
    }

}