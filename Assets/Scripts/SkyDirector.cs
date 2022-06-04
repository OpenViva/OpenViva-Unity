using System.Collections.Generic;
using UnityEngine;

namespace viva
{


    public partial class SkyDirector : MonoBehaviour
    {


        [System.Serializable]
        public class FogOverride
        {
            public Color color = Color.blue;
            public float density = 0.5f;
        }

        public class DayNightCycleCallback
        {
            public delegate void CallbackFunction();

            public CallbackFunction function;
            public float phaseIndex;
            public int nextTriggerDay;

            public DayNightCycleCallback(CallbackFunction _function, float _phaseIndex)
            {
                function = _function;
                phaseIndex = _phaseIndex;
                nextTriggerDay = GameDirector.skyDirector.GetCurrentDay();
            }
        }

        public enum DaySegment
        {
            MORNING,
            DAY,
            NIGHT,
            NONE,
        }
        private const float M_PI2 = Mathf.PI * 2.0f;

        [Header("Day Night Cycle")]
        [SerializeField]
        private Light m_sun;
        public Light sun { get { return m_sun; } }
        [Range(0.0f, M_PI2)]
        [SerializeField]
        public float firstLoadDayOffset = 0.0f;
        [SerializeField]
        private Material sunMaterial;
        [SerializeField]
        private float sunYaw = 100.0f;
        [SerializeField]
        private ReflectionProbe environmentMapProbe;
        [SerializeField]
        private DayNightCycle.Phase m_defaultDayNightPhase;
        public DayNightCycle.Phase defaultDayNightPhase { get { return m_defaultDayNightPhase; } }

        private FogOverride fogOverride = null;


        public void SetFogOverride(FogOverride _fogOverride)
        {
            fogOverride = _fogOverride;
            if (fogOverride != null)
            {
                RenderSettings.fogColor = fogOverride.color;
                RenderSettings.fogDensity = fogOverride.density;
            }
            else
            {
                ApplyDayNightCyclePhase(CalculatePhaseIndex());
            }
        }

        private float[] dayNightCycleSpeeds = new float[]{  //every M_PI2 is a day cycle
        M_PI2/(12.0f*60.0f),
        M_PI2/(24.0f*60.0f),
        M_PI2/(40.0f*60.0f),
        M_PI2/(60.0f*60.0f),
        0.0f,
    };

        private float dayNightCycleSpeed = 0.0f;
        private float m_sunPitchRadian = 0.0f;
        public float sunPitchRadian { get { return m_sunPitchRadian; } }
        private float cycleUpdateTimer = 0.0f;
        private const float cycleUpdateFramerate = 1.0f / 12.0f;
        private DaySegment m_currentDaySegment = DaySegment.NONE;
        public DaySegment daySegment { get { return m_currentDaySegment; } }

        public int GetCurrentDay()
        {
            return Mathf.FloorToInt(GameDirector.settings.worldTime / M_PI2);
        }

        public Light GetSun()
        {
            return sun;
        }

        [SerializeField]
        private DayNightCycle dayNightCycle;
        private static int skyTintID = Shader.PropertyToID("_SkyTint");
        private static int nightSkyID = Shader.PropertyToID("_NightSky");
        private static int nightBlendID = Shader.PropertyToID("_NightBlend");
        private static int atmosphereThicknessID = Shader.PropertyToID("_AtmosphereThickness");
        private static int exposureID = Shader.PropertyToID("_Exposure");
        private static int cloudColorAID = Shader.PropertyToID("_CloudColorA");
        private static int cloudColorBID = Shader.PropertyToID("_CloudColorB");
        private Color baseToonAmbience;
        private List<DayNightCycleCallback> cycleCallbacks = new List<DayNightCycleCallback>();
        private bool debugMode = false;
        public bool isNight { get { return GameDirector.settings.worldTime % M_PI2 > Mathf.PI; } }
        public bool nightModeSet = false;


        private void FixedUpdate()
        {

            cycleUpdateTimer += Time.deltaTime;
            if (cycleUpdateTimer < cycleUpdateFramerate)
            {  //don't update every call
                return;
            }
            cycleUpdateTimer = cycleUpdateTimer % cycleUpdateFramerate;

#if UNITY_EDITOR
            if (debugMode)
            {
            }
            else
            {
                GameDirector.settings.ShiftWorldTime(Time.deltaTime * dayNightCycleSpeed);
            }
#else
        GameDirector.settings.ShiftWorldTime( Time.deltaTime*dayNightCycleSpeed );
#endif
        }

        public void AddDayNightCycleCallback(DayNightCycleCallback callback)
        {
            cycleCallbacks.Add(callback);
            ///TODO: Fix why some mechanisms are awaking TWICE
        }

        private void onEnable()
        {
            UpdateWorldTime();
            GameDirector.instance.ambienceDirector.InitializeAmbience();
            UpdateDayNightCycleSpeed();
            GameDirector.lampDirector.UpdateDaySegmentLampState(true);
            //m_currentDaySegment = DaySegment.DAY;
            // DebugModeInit();

            nightModeSet = !isNight;
        }

        public void UpdateWorldTime()
        {
            m_sunPitchRadian = GameDirector.settings.worldTime % M_PI2;
            m_currentDaySegment = CalculateCurrentDayNightSegment();
        }

        public void UpdateDayNightCycleSpeed()
        {
            dayNightCycleSpeed = dayNightCycleSpeeds[GameDirector.settings.dayNightCycleSpeedIndex];
        }

        private void OnDaySegmentChange()
        {
            Debug.Log("[Day Cycle] " + daySegment);
            GameDirector.instance.ambienceDirector.FadeAmbience();
            GameDirector.instance.SetMusic(GameDirector.instance.GetDefaultMusic());   //update music
            GameDirector.lampDirector.UpdateDaySegmentLampState(false);
        }

        private DaySegment CalculateCurrentDayNightSegment()
        {
            if (m_sunPitchRadian < M_PI2 * 0.175f)
            {
                return DaySegment.MORNING;
            }
            else if (m_sunPitchRadian > M_PI2 * 0.5f)
            {
                return DaySegment.NIGHT;
            }
            else
            {
                return DaySegment.DAY;
            }
        }

        public float CalculatePhaseIndex()
        {
            return m_sunPitchRadian * 10.0f / M_PI2;
        }

        private void DebugModeInit()
        {
            if (debugMode)
            {
                return;
            }
            debugMode = true;
            var c = new InputActions_viva();
            c.Enable();
            c.keyboard.w.performed += ctx => DebugIncrease(1);
            c.keyboard.s.performed += ctx => DebugIncrease(-1);
        }

        public void DebugIncrease(int shift)
        {
            float pieces = dayNightCycle.phases.Length / M_PI2;
            float time = Mathf.RoundToInt(GameDirector.settings.worldTime * pieces) / pieces + shift / pieces;
            if (time < 0.0f)
            {
                time = M_PI2;
            }
            GameDirector.settings.SetWorldTime(time);
            m_sunPitchRadian = GameDirector.settings.worldTime % M_PI2;
            Debug.LogError("[CYCLE] " + CalculatePhaseIndex());
        }

        public void ApplyDayNightCycle()
        {

            if (!enabled)
            {
                return;
            }
            //update sun rotation every frame
            m_sunPitchRadian = GameDirector.settings.worldTime % M_PI2;
            UpdateDayNightCycleSunRotation();

            //check if daySegment changed
            DaySegment currentDaySegment = CalculateCurrentDayNightSegment();
            if (currentDaySegment != this.m_currentDaySegment)
            {
                this.m_currentDaySegment = currentDaySegment;
                OnDaySegmentChange();
            }
            //call back registered callbacks
            //TODO: Sort by usage
            int day = GetCurrentDay();
            float phaseIndex = CalculatePhaseIndex();
            for (int i = cycleCallbacks.Count; i-- > 0;)
            {
                var callback = cycleCallbacks[i];
                if (phaseIndex >= callback.phaseIndex && callback.nextTriggerDay <= day)
                {
                    callback.nextTriggerDay = day + 1;
                    if (callback.function == null)
                    {
                        cycleCallbacks.RemoveAt(i);
                    }
                    else
                    {
                        callback.function();
                    }
                }
            }

            ApplyDayNightCyclePhase(phaseIndex);
        }

        private void UpdateDayNightCycleSunRotation()
        {
            float sunPitch = m_sunPitchRadian * Mathf.Rad2Deg;
            sun.transform.localRotation = Quaternion.Euler(sunPitch, sunYaw, 0.0f);
        }

        private void ApplyDayNightCyclePhase(float phaseIndex)
        {

            DayNightCycle.Phase curr = dayNightCycle.phases[((int)phaseIndex) % dayNightCycle.phases.Length];
            DayNightCycle.Phase prev = dayNightCycle.phases[((int)phaseIndex - 1 + dayNightCycle.phases.Length) % dayNightCycle.phases.Length];
            float blend = phaseIndex - Mathf.Floor(phaseIndex);

            RenderSettings.ambientLight = Color.LerpUnclamped(prev.ambience, curr.ambience, blend);
            sun.color = Color.LerpUnclamped(prev.sunColor, curr.sunColor, blend);
            sun.intensity = sun.color.a;
            if (fogOverride == null)
            {
                RenderSettings.fogColor = Color.LerpUnclamped(prev.fogColor, curr.fogColor, blend);
                RenderSettings.fogDensity = Mathf.LerpUnclamped(prev.fogColor.a, curr.fogColor.a, blend) * 0.01f;
            }
            sunMaterial.SetFloat(atmosphereThicknessID, Mathf.LerpUnclamped(prev.atmosphereThickness, curr.atmosphereThickness, blend));
            sunMaterial.SetFloat(exposureID, Mathf.LerpUnclamped(prev.exposure, curr.exposure, blend));
            sunMaterial.SetColor(skyTintID, Color.LerpUnclamped(prev.skyTint, curr.skyTint, blend));
            sunMaterial.SetColor(nightSkyID, Color.LerpUnclamped(prev.nightSky, curr.nightSky, blend));
            GameDirector.instance.raymarchingCloudsMat.SetColor(cloudColorAID, Color.LerpUnclamped(prev.cloudColorA, curr.cloudColorA, blend));
            GameDirector.instance.raymarchingCloudsMat.SetColor(cloudColorBID, Color.LerpUnclamped(prev.cloudColorB, curr.cloudColorB, blend));
            baseToonAmbience = Color.LerpUnclamped(prev.toonAmbience, curr.toonAmbience, blend);

            float nightBlend = Tools.GetClampedRatio(5.0f, 6.0f, phaseIndex) - Tools.GetClampedRatio(9.0f, 10.0f, phaseIndex);
            sunMaterial.SetFloat(nightBlendID, nightBlend);

            if (environmentMapProbe)
            {
                if (blend < 0.5f)
                {
                    environmentMapProbe.customBakedTexture = prev.environmentMap;
                }
                else
                {
                    environmentMapProbe.customBakedTexture = curr.environmentMap;
                }
            }

            //update characters ambience
            for (int i = 0; i < GameDirector.characters.objects.Count; i++)
            {
                Loli loli = GameDirector.characters.objects[i] as Loli;
                if (loli)
                {
                    loli.ApplyToonAmbience(GameDirector.instance.mainCamera.transform.position, baseToonAmbience);
                }
            }
        }

        public void OverrideDayNightCycleLighting(DayNightCycle.Phase phase, Quaternion sunRotation)
        {

            RenderSettings.ambientLight = phase.ambience;
            sun.color = phase.sunColor;
            sun.intensity = sun.color.a;
            if (fogOverride == null)
            {
                RenderSettings.fogColor = phase.fogColor;
                RenderSettings.fogDensity = phase.fogColor.a;
            }
            sunMaterial.SetFloat(atmosphereThicknessID, phase.atmosphereThickness);
            sunMaterial.SetFloat(exposureID, phase.exposure);
            sunMaterial.SetColor(skyTintID, phase.skyTint);
            sunMaterial.SetColor(nightSkyID, phase.nightSky);
            baseToonAmbience = phase.toonAmbience;

            sunMaterial.SetFloat(nightBlendID, 0.0f);
            sun.transform.localRotation = sunRotation;

            //update characters ambience
            for (int i = 0; i < GameDirector.characters.objects.Count; i++)
            {
                Loli loli = GameDirector.characters.objects[i] as Loli;
                if (loli)
                {
                    loli.ApplyToonAmbience(GameDirector.instance.mainCamera.transform.position, baseToonAmbience);
                }
            }
        }

        public void SetSkyMaterial(Material material)
        {
            if (material == null)
            {
                RenderSettings.skybox = sunMaterial;
            }
            else
            {
                RenderSettings.skybox = material;
            }
        }

        public void RestoreDayNightCycleLighting()
        {
            float phaseIndex = CalculatePhaseIndex();
            UpdateDayNightCycleSunRotation();
            ApplyDayNightCyclePhase(phaseIndex);
        }
    }

}