using System.Collections.Generic;

[System.Serializable]
public class PlayerResult
{
    public int playerIndex;    // 0 = Merah, 1 = Biru, 2 = Hijau, 3 = Kuning (sama dengan urutan join)
    public string playerName;
    public int score;
    public int rank;           // 1 = juara. Skor sama = rank sama.
}

// Jembatan statis untuk membawa skor akhir dari scene minigame ke scene Podium.
public static class ScoreBridge
{
    // Sudah terurut dari skor tertinggi ke terendah
    public static List<PlayerResult> Results = new List<PlayerResult>();

    public static bool HasResults { get { return Results.Count > 0; } }

    public static void SetResults(List<PlayerResult> raw)
    {
        Results.Clear();
        Results.AddRange(raw);

        // Skor tertinggi dulu; kalau seri, yang indexnya lebih kecil didahulukan
        Results.Sort((a, b) =>
        {
            int c = b.score.CompareTo(a.score);
            return c != 0 ? c : a.playerIndex.CompareTo(b.playerIndex);
        });

        // Beri nomor rank (skor sama = rank sama)
        for (int i = 0; i < Results.Count; i++)
        {
            if (i > 0 && Results[i].score == Results[i - 1].score)
                Results[i].rank = Results[i - 1].rank;
            else
                Results[i].rank = i + 1;
        }
    }

    public static void Clear()
    {
        Results.Clear();
    }
}