using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public static class ArtifactSelectionPlan
{
    public const int DefaultArtifactCount = 7;
    public const int DefaultArtifactPoolSize = 18;

    private const string ArtifactIdsKey = "GCT555_SelectedArtifactIds";

    public static int[] GetOrCreateArtifactIds(int count = DefaultArtifactCount, int maxArtifactId = DefaultArtifactPoolSize)
    {
        if (TryLoadArtifactIds(out int[] artifactIds, count, maxArtifactId))
            return artifactIds;

        return GenerateAndSaveArtifactIds(count, maxArtifactId);
    }

    public static int[] GenerateAndSaveArtifactIds(int count = DefaultArtifactCount, int maxArtifactId = DefaultArtifactPoolSize)
    {
        int clampedCount = Mathf.Clamp(count, 0, Mathf.Max(0, maxArtifactId));
        int[] pool = new int[maxArtifactId];
        for (int i = 0; i < pool.Length; i++)
        {
            pool[i] = i + 1;
        }

        int[] selectedArtifactIds = new int[clampedCount];
        for (int i = 0; i < clampedCount; i++)
        {
            int selectedIndex = UnityEngine.Random.Range(i, pool.Length);
            selectedArtifactIds[i] = pool[selectedIndex];
            pool[selectedIndex] = pool[i];
            pool[i] = selectedArtifactIds[i];
        }

        SaveArtifactIds(selectedArtifactIds);
        return selectedArtifactIds;
    }

    public static bool TryLoadArtifactIds(out int[] artifactIds, int count = DefaultArtifactCount, int maxArtifactId = DefaultArtifactPoolSize)
    {
        artifactIds = null;
        string savedValue = PlayerPrefs.GetString(ArtifactIdsKey, string.Empty);
        if (string.IsNullOrEmpty(savedValue))
            return false;

        string[] parts = savedValue.Split(',');
        if (parts.Length < count)
            return false;

        List<int> parsedIds = new List<int>(count);
        HashSet<int> usedIds = new HashSet<int>();
        for (int i = 0; i < parts.Length && parsedIds.Count < count; i++)
        {
            if (!int.TryParse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out int artifactId))
                return false;

            if (artifactId < 1 || artifactId > maxArtifactId || !usedIds.Add(artifactId))
                return false;

            parsedIds.Add(artifactId);
        }

        if (parsedIds.Count != count)
            return false;

        artifactIds = parsedIds.ToArray();
        return true;
    }

    private static void SaveArtifactIds(int[] artifactIds)
    {
        if (artifactIds == null)
            return;

        string[] values = new string[artifactIds.Length];
        for (int i = 0; i < artifactIds.Length; i++)
        {
            values[i] = artifactIds[i].ToString(CultureInfo.InvariantCulture);
        }

        PlayerPrefs.SetString(ArtifactIdsKey, string.Join(",", values));
        PlayerPrefs.Save();
    }
}
