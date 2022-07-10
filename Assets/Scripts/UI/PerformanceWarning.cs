using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace viva
{
    public class PerformanceWarning : MonoBehaviour
    {
        [SerializeField]
        private GameObject PerformanceGO;

        private void Start()
        {
            PerformanceGO.SetActive(false);
        }

        private void Update()
        {
            int currQuality = QualitySettings.GetQualityLevel();
            bool enabled = currQuality >= 4 ? true : false;
            if (PerformanceGO != null)
            {
                PerformanceGO.SetActive(enabled);
            } 

        }
    }
}