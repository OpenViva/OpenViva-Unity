using System.Collections;
using UnityEngine;

namespace viva
{


    public partial class GameDirector : MonoBehaviour
    {

        public enum Music
        {
            NONE,   //first index music be left out NULL
            DAY_INDOOR,
            DAY_OUTDOOR,
            BATHING,
            NIGHT,
            EXPLORING,
            EXPLORING_NIGHT,
            SUSPENSE,
            ONSEN,
            TOWN
        }

        [Header("Music")]
        [SerializeField]
        private bool muteMusic = false;
        [SerializeField]
        private AudioSource musicSourceA;
        [SerializeField]
        private AudioSource musicSourceB;
        [SerializeField]
        private AudioSource globalSoundSource;
        [SerializeField]
        private AudioClip[] music = new AudioClip[System.Enum.GetValues(typeof(Music)).Length];

        private Music currentMusic = Music.NONE;
        private Coroutine fadeMusicCoroutine = null;
        private Music queuedMusic = Music.NONE;
        private Music lastMusic = Music.DAY_OUTDOOR;

        private bool lockMusic = false;
        private bool m_userIsIndoors = false;
        public bool userIsIndoors { get { return m_userIsIndoors; } }
        private bool userIsExploring = false;
        private bool userInOnsen = false;

        private bool userInTown = false;

        public bool IsMusicMuted()
        {
            return muteMusic;
        }

        public void SetMuteMusic(bool enable)
        {
            muteMusic = enable;
            if (muteMusic)
            {
                SetMusic(Music.NONE, 1.0f);
            }
            else
            {
                SetMusic(GetDefaultMusic(), 0.5f);
            }
        }

        public void LockMusic(bool _lockMusic)
        {
            lockMusic = _lockMusic;
        }

        public void SetMusic(Music newMusic, float fadeTime = 3.0f)
        {
            if (lockMusic)
            {
                return;
            }
            if (newMusic != Music.NONE)
            {
                lastMusic = newMusic;
            }
            if (muteMusic)
            {
                newMusic = Music.NONE;
            }
            queuedMusic = newMusic;
            if (currentMusic == newMusic)
            {
                return;
            }
            //Wait for last fade
            if (fadeMusicCoroutine != null)
            {
                return;
            }
            fadeMusicCoroutine = GameDirector.instance.StartCoroutine(FadeMusic(newMusic, fadeTime));
        }

        private void InitMusic()
        {
            musicSourceA.loop = true;
            musicSourceB.loop = true;
        }

        public void SetUserIsIndoors(bool indoors)
        {
            m_userIsIndoors = indoors;
            SetMusic(GetDefaultMusic());
            ambienceDirector.FadeAmbience();
        }

        public void SetUserInOnsen(bool onsen)
        {
            userInOnsen = onsen;
            SetMusic(GetDefaultMusic());
        }

        public void SetUserInTown(bool town)
        {
            userInTown = town;
            SetMusic(GetDefaultMusic());
        }

        public void SetUserIsExploring(bool exploring)
        {
            userIsExploring = exploring;
            SetMusic(GetDefaultMusic());
        }

        public Music GetDefaultMusic()
        {
            switch (GameDirector.skyDirector.daySegment)
            {
                case SkyDirector.DaySegment.MORNING:
                case SkyDirector.DaySegment.DAY:

                    if (userIsExploring)
                    {
                        return Music.EXPLORING;
                    }
                    if (userInOnsen)
                    {
                        return Music.ONSEN;
                    }
                    if (userInTown)
                    {
                        return Music.TOWN;
                    }
                    if (userIsIndoors)
                    {
                        return Music.DAY_INDOOR;
                    }
                    else
                    {
                        return Music.DAY_OUTDOOR;
                    }
                //case SkyDirector.DaySegment.MORNING:
                //	return Music.NONE;
                case SkyDirector.DaySegment.NIGHT:

                    if (userIsExploring)
                    {
                        return Music.EXPLORING_NIGHT;
                    }
                    if (userInOnsen)
                    {
                        return Music.ONSEN;
                    }
                    return Music.NIGHT;
            }
            return Music.NONE;
        }

        public void UpdateMusicVolume()
        {
            musicSourceB.volume = settings.musicVolume;
        }

        public void PlayGlobalSound(AudioClip clip)
        {
            globalSoundSource.PlayOneShot(clip);
        }

        private IEnumerator FadeMusic(Music newMusic, float fadeTime)
        {
            Debug.Log("[MUSIC] " + newMusic);
            float timer = fadeTime;

            //Fade music source from A to B
            musicSourceA.clip = music[(int)currentMusic];
            musicSourceA.time = musicSourceB.time;
            musicSourceB.volume = settings.musicVolume;
            musicSourceA.Play();

            musicSourceB.clip = music[(int)newMusic];
            musicSourceB.time = musicSourceA.time;
            musicSourceB.volume = 0.0f;
            musicSourceB.Play();

            while (timer > 0.0f)
            {
                timer = Mathf.Max(0.0f, timer - Time.deltaTime);
                musicSourceA.volume = (timer / fadeTime) * GameDirector.settings.musicVolume;
                musicSourceB.volume = (1.0f - timer / fadeTime) * GameDirector.settings.musicVolume;
                yield return null;
            }
            musicSourceA.volume = 0.0f;
            musicSourceA.Stop();
            musicSourceB.volume = GameDirector.settings.musicVolume;

            fadeMusicCoroutine = null;
            currentMusic = newMusic;

            if (queuedMusic != currentMusic)
            {
                SetMusic(queuedMusic);
            }
        }
    }

}