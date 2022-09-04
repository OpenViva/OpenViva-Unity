using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace viva
{

    public delegate IEnumerator OutlineCoroutine(Outline.Entry outlineEntry, Color color);


    public static class Outline
    {
        public class Entry
        {
            public Loli loli;
            public Coroutine outlineCoroutine;
            private readonly List<Material> lastTargetMats = new List<Material>();
            public global::Outline headOutline;
            public global::Outline bodyOutline;
            public global::Outline bodyLodOutline;

            public Entry(Loli _loli)
            {
                loli = _loli;

                if (loli.bodySMRs == null && loli.headSMR == null) return;
                if (loli.bodySMRs[0].gameObject.TryGetComponent<global::Outline>(out bodyOutline))
                {
                    CancelDestroy(bodyOutline);
                }
                else if (loli.bodySMRs[1].gameObject.TryGetComponent<global::Outline>(out bodyLodOutline))
                {
                    CancelDestroy(bodyLodOutline);
                }
                else if (loli.headSMR.gameObject.TryGetComponent<global::Outline>(out headOutline))
                {
                    CancelDestroy(headOutline);
                }
                else
                {
                    headOutline = loli.headSMR.gameObject.AddComponent<global::Outline>();
                    bodyOutline = loli.bodySMRs[0].gameObject.AddComponent<global::Outline>();
                    bodyLodOutline = loli.bodySMRs[1].gameObject.AddComponent<global::Outline>();
                }
                if (headOutline == null && bodyOutline == null && bodyLodOutline) return;

                bodyOutline.OutlineMode = global::Outline.Mode.OutlineVisible;
                bodyLodOutline.OutlineMode = global::Outline.Mode.OutlineVisible;
                headOutline.OutlineMode = global::Outline.Mode.OutlineVisible;
            }

            public void SetOutline(Color color, float width)
            {
                if (headOutline == null && bodyOutline == null && bodyLodOutline) return;
                bodyOutline.OutlineColor = color;
                bodyOutline.OutlineWidth = width;
                bodyLodOutline.OutlineColor = color;
                bodyLodOutline.OutlineWidth = width;
                headOutline.OutlineColor = color;
                headOutline.OutlineWidth = width;
            }
        }

        private struct DestroyEntry
        {
            public Coroutine coroutine;
            public int id;

            public DestroyEntry(Coroutine _coroutine, int _id)
            {
                coroutine = _coroutine;
                id = _id;
            }
        }

        private static List<Entry> outlineEntries = new List<Entry>();
        private static List<DestroyEntry> destroyQueue = new List<DestroyEntry>();

        private static void QueueForDestroy(global::Outline outline)
        {
            if (outline == null) return;

            var instanceId = outline.GetInstanceID();
            var entry = new DestroyEntry(GameDirector.instance.StartCoroutine(DestroyOnUpdate(instanceId, outline)), instanceId);
            destroyQueue.Add(entry);
        }

        private static IEnumerator DestroyOnUpdate(int id, global::Outline outline)
        {
            yield return null;

            for (int i = destroyQueue.Count; i-- > 0;)
            {
                var entry = destroyQueue[i];
                if (entry.id == id)
                {
                    destroyQueue.RemoveAt(i);
                }
            }
            if (outline) GameObject.DestroyImmediate(outline);
        }

        private static void CancelDestroy(global::Outline outline)
        {
            if (outline == null) return;
            for (int i = destroyQueue.Count; i-- > 0;)
            {
                var entry = destroyQueue[i];
                if (entry.id == outline.GetInstanceID())
                {
                    destroyQueue.RemoveAt(i);
                    GameDirector.instance.StopCoroutine(entry.coroutine);
                }
            }
        }

        public static Outline.Entry StartOutlining(Loli loli, Color color, OutlineCoroutine outlineCoroutine)
        {
            if (loli == null) return null;

            var entry = new Outline.Entry(loli);
            if (outlineCoroutine != null) entry.outlineCoroutine = GameDirector.instance.StartCoroutine(outlineCoroutine(entry, color));
            outlineEntries.Add(entry);
            return entry;
        }

        public static void StopOutlining(Entry entry)
        {
            if (entry == null) return;

            if (entry.outlineCoroutine != null) GameDirector.instance.StopCoroutine(entry.outlineCoroutine);
            QueueForDestroy(entry.bodyOutline);
            QueueForDestroy(entry.bodyLodOutline);
            QueueForDestroy(entry.headOutline);

            outlineEntries.Remove(entry);
        }

        public static IEnumerator Flash(Outline.Entry entry, Color color)
        {
            float timer = 0;
            float duration = 0.75f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float ratio = 1.0f - Mathf.Clamp01(timer / duration);
                ratio = 1.0f - Mathf.Pow(ratio, 4);
                entry.SetOutline(color * ratio, ratio * 8);
                yield return null;
            }
            Outline.StopOutlining(entry);
        }

        public static IEnumerator Constant(Entry outlineEntry, Color color)
        {
            while (true)
            {
                outlineEntry.SetOutline(color, Mathf.Sin(Time.time * 8.0f) * 2 + 4.0f);
                yield return null;
            }
        }
    }

}