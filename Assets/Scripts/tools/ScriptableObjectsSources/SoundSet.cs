using UnityEngine;

namespace viva
{


    [System.Serializable]
    [CreateAssetMenu(fileName = "SoundSet", menuName = "Sound Set", order = 1)]
    public class SoundSet : ScriptableObject
    {

        public AudioClip[] sounds;

        public AudioClip GetRandomAudioClip()
        {
            return GetAudioClip(Random.Range(0, sounds.Length));
        }

        public AudioClip GetAudioClip(int index)
        {
            if (sounds.Length == 0)
            {
                return null;
            }
            return sounds[index];
        }

        public class StableSoundRandomizer
        {

            public bool[] usedIndices;
            public int soundsPlayed = 0;

            public StableSoundRandomizer(SoundSet set)
            {
                usedIndices = new bool[set.sounds.Length];
            }

            public int GetNextStableRandomIndex()
            {
                //Play random non-repeating clip entry
                if (soundsPlayed == usedIndices.Length)
                {
                    soundsPlayed = 0;
                    for (int i = 0; i < usedIndices.Length; i++)
                    {
                        usedIndices[i] = false;
                    }
                }
                soundsPlayed++;

                int index = Random.Range(0, usedIndices.Length - 1);
                while (usedIndices[index])
                {
                    index++;
                    if (index == usedIndices.Length)
                    {
                        index = 0;
                    }
                }
                //set used to 'true'
                usedIndices[index] = true;
                return index;
            }
        }
    }

}