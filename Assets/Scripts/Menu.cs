using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace viva
{

    public class Menu : MonoBehaviour
    {

        [SerializeField]
        private Transform optionsContainer;

        public void SetHideOption(string optionName, bool show=false)
        {
            var option = optionsContainer.Find(optionName);
            if (option) option.gameObject.SetActive( show );
        }

        private void OnEnable()
        {
            var isMain = Scene.main.sceneSettings.type == "main";
            SetHideOption("Tasks",!isMain);
            SetHideOption("Quit",!isMain);
            SetHideOption("New",isMain);
            SetHideOption("Load",isMain);
            SetHideOption("Tutorial",isMain);
        }
    }

}