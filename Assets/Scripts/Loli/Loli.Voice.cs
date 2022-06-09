using System.Collections;
using UnityEngine;


namespace viva
{


    public partial class Loli : Character
    {

        [SerializeField]
        private AudioSource voiceSource;

        private SoundSet.StableSoundRandomizer[] voiceRandomizers = null;
        private VoiceLine lastVoice = VoiceLine.HUMPH;  //default
        private float lastRandomSpeakTime = 0.0f;
        private bool bindVoiceToCurrentAnim = false;
        private Coroutine stopVoiceCoroutine = null;
        private Voice voice = null;

        [SerializeField]
        private Voice[] voices = new Voice[System.Enum.GetValues(typeof(Voice.VoiceType)).Length];
        [SerializeField]
        private SoundSet clapping;

        private void PlayClapSound()
        {
            SoundManager.main.RequestHandle(rightLoliHandState.transform.position).PlayOneShot(clapping.GetRandomAudioClip());
        }

        public bool IsSpeaking(VoiceLine voiceEnum)
        {
            return (voiceSource.isPlaying && lastVoice == voiceEnum);
        }

        public bool IsSpeakingAtAll()
        {
            return voiceSource.isPlaying;
        }

        public void SpeakAtRandomIntervals(VoiceLine voiceEnum, float minInterval, float additionalRandomInterval)
        {

            if (!IsSpeakingAtAll())
            {
                if (Time.time - lastRandomSpeakTime > 0.0f)
                {
                    lastRandomSpeakTime = Time.time + minInterval + Random.value * additionalRandomInterval;
                    Speak(voiceEnum, false);
                }
            }
        }

        public void StopSpeaking()
        {
            if (stopVoiceCoroutine != null)
            {
                GameDirector.instance.StopCoroutine(stopVoiceCoroutine);
                stopVoiceCoroutine = null;
                voiceSource.Stop();
                return;
            }
            stopVoiceCoroutine = GameDirector.instance.StartCoroutine(StopSpeakingFadeOff());
        }

        private IEnumerator StopSpeakingFadeOff()
        {

            float fadeTime = 0.1f;
            while (fadeTime > 0.0f)
            {
                fadeTime -= Time.deltaTime;
                voiceSource.volume = fadeTime / 0.1f;
                yield return null;
            }
            voiceSource.Stop();
            voiceSource.volume = 1.0f;
            stopVoiceCoroutine = null;
        }

        public void RebuildVoice()
        {
            if (headModel.voiceIndex > voices.Length)
            {
                Debug.LogError("[LOLI] voice index out of bounds!");
                return;
            }
            Voice newVoice = voices[(int)headModel.voiceIndex];
            if (newVoice == null)
            {
                Debug.LogError("[LOLI] Could not load voice at " + headModel.voiceIndex);
                return;
            }
            voice = newVoice;

            voiceRandomizers = new SoundSet.StableSoundRandomizer[voice.voiceLines.Length];
            for (int i = 0; i < voice.voiceLines.Length; i++)
            {
                voiceRandomizers[i] = new SoundSet.StableSoundRandomizer(voice.voiceLines[i]);
            }
        }

        public AudioClip GetNextVoiceLine(VoiceLine voiceEnum)
        {
            if (voiceRandomizers == null)
            {
                Debug.LogError("[LOLI] Voice randomizers is null");
                return null;
            }
            SoundSet voiceSet = voice.voiceLines[(int)voiceEnum];
            SoundSet.StableSoundRandomizer voiceRandomizer = voiceRandomizers[(int)voiceEnum];
            lastVoice = voiceEnum;

            if (voiceSet.sounds.Length == 0)
            {
                return null;
            }
            return voiceSet.GetAudioClip(voiceRandomizers[(int)voiceEnum].GetNextStableRandomIndex());
        }

        public void Speak(VoiceLine voiceEnum, bool bindToCurrentAnim = false)
        {

            AudioClip voiceLine = GetNextVoiceLine(voiceEnum);
            if (voiceLine == null)
            {
                return;
            }

            if (stopVoiceCoroutine != null)
            {
                GameDirector.instance.StopCoroutine(stopVoiceCoroutine);
                voiceSource.Stop();
                voiceSource.volume = 1.0f;
                stopVoiceCoroutine = null;
            }

            bindVoiceToCurrentAnim = bindToCurrentAnim;

            voiceSource.clip = voiceLine;
            voiceSource.Play();
        }
    }

}