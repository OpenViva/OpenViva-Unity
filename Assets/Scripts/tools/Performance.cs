using System;
using UnityEngine;
using System.Diagnostics;
 
 public static class Performance
 {
    public enum FrameRateCategory
    {
        Unplayable,
        VeryBad,
        Bad,
        Average,
        Good,
        VeryGood,
        Count,
    }
    public static Func<int> GetMemoryUsage = null;
    public static Func<int> GetGarbageCollections = null;
    private static Stopwatch Stopwatch = Stopwatch.StartNew();
    private static int frames;
    public static int TargetFrameRate = 60;
    private static int[] frameBuckets = new int[6];
    private static float[] frameBucketFractions = new float[6];

    public static FrameRateCategory FramerateCategory => Performance.CategorizeFrameRate(Performance.FrameCountLastSecond);

    public static int FrameCountLastSecond { get; private set; }

    public static double AvgFrameTimeLastSecond => 1000 / Performance.FrameCountLastSecond;

    public static int MemoryUsage { get; private set; }

    public static int GarbageCollections { get; private set; }

    public static float SecondsSinceLastConnection { get; private set; }

    public static int[] CategorizedFrameCount => Performance.frameBuckets;

    internal static void Frame()
    {
      ++Performance.frames;
      if (Performance.Stopwatch.Elapsed.TotalSeconds < 1.0)
        return;
      Performance.OneSecond(Performance.Stopwatch.Elapsed.TotalSeconds);
      Performance.Stopwatch.Reset();
      Performance.Stopwatch.Start();
    }

    private static void OneSecond(double timelapse)
    {
      Performance.FrameCountLastSecond = Performance.frames;
      Performance.frames = 0;
      Performance.MemoryUsage = Performance.GetMemoryUsage == null ? (int) (GC.GetTotalMemory(false) / 1024L / 1024L) : Performance.GetMemoryUsage();
      Performance.GarbageCollections = Performance.GetGarbageCollections == null ? GC.CollectionCount(0) : Performance.GetGarbageCollections();
      Performance.UpdateFrameBuckets();
    }

    public static FrameRateCategory CategorizeFrameRate(int i)
    {
      if (i < Performance.TargetFrameRate / 4)
        return FrameRateCategory.Unplayable;
      if (i < Performance.TargetFrameRate / 2)
        return FrameRateCategory.VeryBad;
      if (i < Performance.TargetFrameRate - 10)
        return FrameRateCategory.Bad;
      if (i < Performance.TargetFrameRate + 10)
        return FrameRateCategory.Average;
      return i < Performance.TargetFrameRate + 30 ? FrameRateCategory.Good : FrameRateCategory.VeryGood;
    }

    private static void UpdateFrameBuckets()
    {
      ++Performance.frameBuckets[(int) FramerateCategory];
      int frameR = 0;
      for (int i = 0; i < Performance.frameBuckets.Length; ++i)
        frameR += Performance.frameBuckets[i];
      for (int i = 0; i < Performance.frameBuckets.Length; ++i)
        Performance.frameBucketFractions[i] = Performance.frameBuckets[i] / frameR;
    }

    public static int GetFrameCount(FrameRateCategory category) => Performance.frameBuckets[(int) category];

    public static float GetFrameFraction(FrameRateCategory category) => Performance.frameBucketFractions[(int) category];
  }