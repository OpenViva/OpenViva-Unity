using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace viva
{
    [System.Serializable]
    public class Resolution
    {
        public int horizontal, vertical;
    }

    public partial class PauseMenu : UIMenu
    {
        [Header("Graphics")]
        [SerializeField]
        private Text reflectionDistanceText;
        [SerializeField]
        private GameObject FpsLimitContainer;
        [SerializeField]
        private Text resolutionText;
        [SerializeField]
        private Text lodDistanceText;
        public List<Resolution> resolutions = new List<Resolution>();
        private int selectedResolution;

        public void FindAutomaticResolution()
        {
            bool foundRes = false;
            for(int i = 0; i < resolutions.Count; i++)
            {
                if(Screen.width == resolutions[i].horizontal && Screen.height == resolutions[i].vertical)
                {
                    foundRes = true;
                    selectedResolution = i;
                    UpdateResolutionText();
                }
            }
            if (!foundRes)
            {
                Resolution resolution = new Resolution();
                resolution.horizontal = Screen.width;
                resolution.vertical = Screen.height;

                resolutions.Add(resolution);
                selectedResolution = resolutions.Count - 1;
                UpdateResolutionText();
            }
            ApplyResolution();
        }

        public void ApplyResolution()
        {
            Screen.SetResolution(resolutions[selectedResolution].horizontal, resolutions[selectedResolution].vertical, GameSettings.main.fullScreen);
        }

        public void shiftResLeft()
        {
            selectedResolution--;
            if(selectedResolution < 0)
            {
                selectedResolution = 0;
            }
            UpdateResolutionText();
            ApplyResolution();
        }

        public void shiftResRight()
        {
            selectedResolution++;
            if (selectedResolution > resolutions.Count - 1)
            {
                selectedResolution = resolutions.Count - 1;
            }
            UpdateResolutionText();
            ApplyResolution();
        }

        public void clickShiftReflectionDistance(int amount)
        {
            GameSettings.main.AdjustReflectionDistance(amount);
            UpdateReflectionDistanceText();
        }

        public void clickShiftFpsLimit(int amount)
        {
            GameSettings.main.AdjustFpsLimit(amount);
            UpdateFpsLimitText();
        }

        public void clickShiftLodDistance(float amount)
        {
            GameSettings.main.AdjustLODDistance(amount);
            UpdateLodDistanceText();
        }

        public void clickCycleAntiAliasing()
        {
            GameSettings.main.CycleAntiAliasing();
            UpdateAntiAliasingText();
        }

        public void clickCycleShadowLevel()
        {
            GameSettings.main.CycleShadowSetting();
            UpdateShadowLevelText();
        }

        public void clickToggleQuality()
        {
            GameSettings.main.CycleQualitySetting();
            UpdateToggleQualityText();
        }

        public void clickToggleScreenMode()
        {
            GameSettings.main.ToggleFullScreen();
            ApplyResolution();

            UpdateScreenModeText();
        }

        public void clickToggleVsync()
        {
            GameSettings.main.ToggleVsync();
            UpdateVsyncText();
        }

        public void ToggleFpsLimitContainer(bool enable)
        {
            Button[] buttons = FpsLimitContainer.GetComponentsInChildren<Button>();
            foreach(Button button in buttons)
            {
                button.interactable = enable;
            }
        }

        public void clickToggleClouds()
        {
            Text text = GetRightPageUIByMenu(Menu.GRAPHICS).transform.Find("Toggle Clouds").GetChild(0).GetComponent(typeof(Text)) as Text;
            GameSettings.main.toggleClouds = !GameSettings.main.toggleClouds;
            text.text = GameSettings.main.toggleClouds ? "Turn Off Clouds" : "Turn On Clouds";
            GameDirector.instance.RebuildCloudRendering();
        }

        private void UpdateReflectionDistanceText()
        {
            reflectionDistanceText.text = "" + GameSettings.main.reflectionDistance / 2 + "%";
        }

        private void UpdateFpsLimitText()
        {
            Text text = FpsLimitContainer.transform.GetChild(3).GetComponent(typeof(Text)) as Text;
            text.text = "" + GameSettings.main.fpsLimit;          
        }

        private void UpdateAntiAliasingText()
        {
            Text text = GetRightPageUIByMenu(Menu.GRAPHICS).transform.Find("Toggle Anti Aliasing").GetChild(0).GetComponent(typeof(Text)) as Text;
            switch (GameSettings.main.antiAliasing)
            {
                case 0:
                    text.text = "None";
                    break;
                case 2:
                    text.text = "2x";
                    break;
                case 4:
                    text.text = "4x";
                    break;
                case 8:
                    text.text = "8x";
                    break;
                default:
                    text.text = "Bad value";
                    Debug.LogError("[ERROR] Bad value on anti aliasing, Either delete or modify settings.cfg to a correct value");
                    break;
            }
        }

        private void UpdateShadowLevelText()
        {
            Text text = GetRightPageUIByMenu(Menu.GRAPHICS).transform.Find("Toggle Shadows").GetChild(0).GetComponent(typeof(Text)) as Text;
            switch (GameSettings.main.shadowLevel)
            {
                default:
                    text.text = "Disabled";
                    break;
                case 1:
                    text.text = "Low";
                    break;
                case 2:
                    text.text = "Medium";
                    break;
                case 3:
                    text.text = "High";
                    break;
                case 4:
                    text.text = "Very High";
                    break;
                case 5:
                    text.text = "Ultra";
                    break;
            }
        }
        private void UpdateToggleQualityText()
        {
            Text text = GetRightPageUIByMenu(Menu.GRAPHICS).transform.Find("Toggle Quality").GetChild(0).GetComponent(typeof(Text)) as Text;
            string[] names = QualitySettings.names;
            text.text = names[QualitySettings.GetQualityLevel()];
        }

        public void UpdateResolutionText()
        {
            resolutionText.text = resolutions[selectedResolution].horizontal.ToString() + " x " + resolutions[selectedResolution].vertical.ToString();
        }
        private void UpdateScreenModeText()
        {
            Text buttonText = GetRightPageUIByMenu(Menu.GRAPHICS).transform.Find("FullScreen").GetChild(0).GetComponent(typeof(Text)) as Text;
            buttonText.text = GameSettings.main.fullScreen ? "FullScreen" : "Windowed";
        }

        private void UpdateVsyncText()
        {
            Text buttonText = GetRightPageUIByMenu(Menu.GRAPHICS).transform.Find("Vsync").GetChild(0).GetComponent(typeof(Text)) as Text;
            buttonText.text = GameSettings.main.vSync ? "Enabled" : "Disabled";
        }

        private void UpdateLodDistanceText()
        {
            lodDistanceText.text = (GameSettings.main.lodDistance * 100f).ToString("00") + "%";
        }

    }

}