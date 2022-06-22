using System.Collections;
using UnityEngine;

namespace viva
{


    public partial class PauseMenu : UIMenu
    {


        [SerializeField]
        private Animator bookAnimator;
        [SerializeField]
        private Transform UI_pageR;
        [SerializeField]
        private Transform UI_pageL;
        [SerializeField]
        private Vector3 keyboardOrientPosition;
        [SerializeField]
        private Vector3 keyboardOrientRotation;
        [SerializeField]
        private AudioClip openSound;
        [SerializeField]
        private AudioClip closeSound;
        [SerializeField]
        private AudioClip nextSound;
        [SerializeField]
        private AudioClip prevSound;

        private Coroutine bookAnimFinishCoroutine = null;
        private delegate void AnimationFinished();
        public Menu targetOnOpenMenuTab = PauseMenu.Menu.ROOT;

        private void PlayBookAnimation(string name, AnimationFinished callback)
        {

            bookAnimator.CrossFade(name, 0.0f);
            if (bookAnimFinishCoroutine != null)
            {
                GameDirector.instance.StopCoroutine(bookAnimFinishCoroutine);
            }
            bookAnimFinishCoroutine = GameDirector.instance.StartCoroutine(BookAnimFinishCoroutine(callback));
        }

        private IEnumerator BookAnimFinishCoroutine(AnimationFinished callback)
        {
            yield return new WaitForSeconds(1.0f);
            bookAnimFinishCoroutine = null;
            callback();
        }

        public void SetTargetOnOpenMenuTab(PauseMenu.Menu newTarget)
        {
            targetOnOpenMenuTab = newTarget;
        }

        private void OnOpenBookFinished()
        {
            SetPauseMenu(targetOnOpenMenuTab);
            targetOnOpenMenuTab = PauseMenu.Menu.ROOT;  //reset target
            IsPauseMenuOpen = true;
        }

        private void OnCloseBookFinished()
        {
            ContinueTutorial(MenuTutorial.WAIT_TO_EXIT_CHECKLIST);
            SetMenuActive(Menu.NONE, false);
            gameObject.SetActive(false);
            IsPauseMenuOpen = false;
        }
    }

}