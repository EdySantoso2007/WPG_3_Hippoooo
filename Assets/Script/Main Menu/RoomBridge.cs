using System.Collections.Generic;
using UnityEngine.InputSystem;

// Data device yang sudah join di Room/Lobby (BUKAN gameobject player-nya,
// karena prefab player baru akan di-spawn nanti di scene minigame)
[System.Serializable]
public class JoinedPlayerData
{
    public string controlScheme;   // "Keyboard", "Gamepad", dll
    public InputDevice[] devices;  // device fisik yang dipakai player ini join
    public bool eliminated;        // true = sudah dipecat, tidak ikut ronde berikutnya
}

// Jembatan statis (static bridge) untuk membawa data device yang sudah
// join di scene Room/Lobby ke scene minigame.
public static class RoomBridge
{
    // Urut sesuai urutan join: index 0 = Merah, 1 = Biru, 2 = Hijau, 3 = Kuning.
    // Urutan TIDAK PERNAH berubah selama satu sesi (yang dipecat hanya ditandai eliminated),
    // jadi index = slot warna = index Drop Zone.
    public static List<JoinedPlayerData> JoinedPlayers = new List<JoinedPlayerData>();

    public static void ResetRoom()
    {
        JoinedPlayers.Clear();
    }

    // ---------- Eliminasi ----------

    /// <summary>Jumlah pemain yang belum dipecat.</summary>
    public static int ActiveCount
    {
        get
        {
            int n = 0;
            foreach (JoinedPlayerData d in JoinedPlayers)
                if (!d.eliminated) n++;
            return n;
        }
    }

    /// <summary>Slot ini diisi pemain yang masih ikut main?</summary>
    public static bool IsSlotActive(int slot)
    {
        return slot >= 0 && slot < JoinedPlayers.Count && !JoinedPlayers[slot].eliminated;
    }

    /// <summary>Tandai pemain di slot ini sebagai dipecat.</summary>
    public static void Eliminate(int slot)
    {
        if (slot >= 0 && slot < JoinedPlayers.Count)
            JoinedPlayers[slot].eliminated = true;
    }

    // ---------- Device ----------

    // Index player (urutan join) yang memiliki device ini. -1 kalau device belum join.
    public static int GetPlayerIndex(InputDevice device)
    {
        if (device == null) return -1;

        for (int i = 0; i < JoinedPlayers.Count; i++)
        {
            InputDevice[] devices = JoinedPlayers[i].devices;
            if (devices == null) continue;

            foreach (InputDevice d in devices)
            {
                if (d != null && d.deviceId == device.deviceId)
                    return i;
            }
        }
        return -1;
    }

    public static bool IsDeviceJoined(InputDevice device)
    {
        return GetPlayerIndex(device) >= 0;
    }
}