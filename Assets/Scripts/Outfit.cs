using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace viva
{



    public class Outfit : SerializedTaskData
    {

        public class ClothingOverride
        {

            public Texture2D texture;
            public string cardFilename;

            public ClothingOverride(Texture2D _texture, string _cardFilename)
            {
                texture = _texture;
                cardFilename = _cardFilename;
            }
        }

        private List<ClothingPreset> clothingPieces = new List<ClothingPreset>();
        public Dictionary<ClothingPreset, Outfit.ClothingOverride> clothingOverrides = new Dictionary<ClothingPreset, Outfit.ClothingOverride>();
        public Color fingerNailColor = new Color(1.0f, 0.76f, 0.74f);
        public Color toeNailColor = new Color(1.0f, 0.62f, 0.58f);


        public static Outfit Create(string[] clothingPieces, bool allowNude)
        {

            //inject anti-nude clothing pieces
            string[] preface;
            if (allowNude)
            {
                preface = new string[]{
            };
            }
            else
            {
                preface = new string[]{
                "shirt 1",
                "panty 1"
            };
            }
            string[] clothePieces;
            if (clothingPieces != null)
            {
                clothePieces = new string[clothingPieces.Length + preface.Length];
                preface.CopyTo(clothePieces, 0);
                clothingPieces.CopyTo(clothePieces, preface.Length);
            }
            else
            {
                clothePieces = preface;
            }
            Outfit outfit = new Outfit();
            for (int i = 0; i < clothePieces.Length; i++)
            {
                ClothingPreset newPiece = GameDirector.instance.FindClothing(clothePieces[i]);
                if (newPiece == null)
                {
                    Debug.LogError("ERROR could not find clothing piece " + clothePieces[i]);
                    return null;    //cannot have any null clothingpieces!
                }
                else
                {
                    outfit.AdditiveClothingPiece(newPiece, null);
                }
            }
            return outfit;
        }

        public string Serialize()
        {

            string result = "" + clothingPieces.Count + "*" + clothingOverrides.Count + "*";
            foreach (var clothingPiece in clothingPieces)
            {
                result += clothingPiece.name + "*";
            }
            foreach (var clothingOverride in clothingOverrides)
            {
                result += clothingOverride.Value.cardFilename + "*";
            }
            return result;
        }

        public static IEnumerator Deserialize(string rawVal, VivaFileAttribute.OnFinishDeserialize onFinishDeserialize)
        {

            Outfit result = null;
            if (rawVal != "")
            {
                result = new Outfit();
                int waiting = 0;
                try
                {
                    var words = rawVal.Split('*');
                    int clothingPieceCount = System.Int32.Parse(words[0]);
                    int clothingOverridesCount = System.Int32.Parse(words[1]);
                    int wordIndex = 2;
                    for (int i = 0; i < clothingPieceCount; i++)
                    {
                        result.clothingPieces.Add(GameDirector.instance.FindClothing(words[wordIndex++]));
                    }

                    waiting = clothingOverridesCount;
                    for (int i = 0; i < clothingOverridesCount; i++)
                    {
                        var request = new Wardrobe.LoadClothingCardRequest(words[wordIndex++]);
                        GameDirector.instance.StartCoroutine(Wardrobe.main.HandleLoadClothingCard(request, delegate
                        {
                            if (request.error != null)
                            {
                                Debug.LogError(request.error);
                            }
                            else
                            {
                                result.clothingOverrides[request.clothingPreset] = request.clothingOverride;
                            }
                            waiting--;
                        })
                        );
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError(e);
                    result = null;
                }
                while (waiting > 0)
                {
                    yield return null;
                }
            }

            onFinishDeserialize.Invoke(result);
        }

        public void AdditiveClothingPiece(ClothingPreset clothingPiece, Outfit.ClothingOverride clothingOverride)
        {
            if (clothingPiece == null)
            {
                Debug.LogError("ERROR Cannot set clothingPiece as null!");
                return;
            }
            clothingPieces.Add(clothingPiece);
            if (clothingOverride != null)
            {
                clothingOverrides[clothingPiece] = clothingOverride;
            }
        }

        public bool RemoveAllClothingPieces(ClothingPreset.WearType wearType)
        {

            bool removed = false;
            for (int i = clothingPieces.Count; i-- > 0;)
            {
                if (clothingPieces[i].wearType == wearType)
                {
                    clothingOverrides.Remove(clothingPieces[i]);
                    clothingPieces.RemoveAt(i);
                    removed = true;
                }
            }
            return removed;
        }

        //Replaces and removes if same wear type clothing exists 
        public void WearClothingPiece(Loli self, ClothingPreset clothingPiece, Outfit.ClothingOverride clothingOverride, int startTestAt = 0)
        {
            if (clothingPiece == null)
            {
                Debug.LogError("ERROR cannot wear a null clothing piece!");
                return;
            }

            switch (clothingPiece.wearType)
            {
                case ClothingPreset.WearType.GROIN:
                    if (RemoveAllClothingPieces(ClothingPreset.WearType.FULL_BODY))
                    {
                        WearClothingPiece(self, GameDirector.instance.FindClothing("shirt 1"), null);
                    }
                    break;
                case ClothingPreset.WearType.TORSO:
                    if (RemoveAllClothingPieces(ClothingPreset.WearType.FULL_BODY))
                    {
                        WearClothingPiece(self, GameDirector.instance.FindClothing("panty 1"), null);
                    }
                    break;
                case ClothingPreset.WearType.FULL_BODY:
                    RemoveAllClothingPieces(ClothingPreset.WearType.GROIN);
                    RemoveAllClothingPieces(ClothingPreset.WearType.TORSO);
                    break;
            }
            bool replaced = false;
            for (int i = startTestAt; i < clothingPieces.Count; i++)
            {
                ClothingPreset candidate = clothingPieces[i];
                if (candidate.wearType == clothingPiece.wearType)
                {
                    clothingPieces[i] = clothingPiece;

                    clothingOverrides.Remove(candidate);
                    replaced = true;
                    break;
                }
            }
            if (!replaced)
            {
                clothingPieces.Add(clothingPiece);
            }
            if (clothingOverride != null)
            {
                clothingOverrides[clothingPiece] = clothingOverride;
            }
        }

        public int GetClothingPieceCount()
        {
            return clothingPieces.Count;
        }

        public void RemoveClothingPiece(int index)
        {
            if (index >= clothingPieces.Count || index < 0)
            {
                Debug.Log("ERROR Bad index! " + index);
                return;
            }
            clothingOverrides.Remove(clothingPieces[index]);
            clothingPieces.RemoveAt(index);
        }

        public ClothingPreset GetClothingPiece(int index)
        {
            if (index >= clothingPieces.Count || index < 0)
            {
                Debug.Log("ERROR Bad index! " + index);
                return null;
            }
            return clothingPieces[index];
        }
    }

}