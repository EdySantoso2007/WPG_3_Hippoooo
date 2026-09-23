using System.Collections.Generic;
using UnityEngine.InputSystem;

// Data device yang sudah join di Room/Lobby (BUKAN gameobject player-nya,
// karena prefab player baru akan di-spawn nanti di scene minigame)
[System.Serializable]
public class JoinedPlayerData
{
    public string controlScheme;   // "Keyboard", "Gamepad", dll
    public InputDevice[] devices;  // device fisik yang dipakai player ini join
}

// Jembatan statis (static bridge) untuk membawa data device yang sudah
// join di scene Room/Lobby ke scene minigame.
public static class RoomBridge
{
    // Urut sesuai urutan join: index 0 = Merah, 1 = Biru, 2 = Hijau, 3 = Kuning
    public static List<JoinedPlayerData> JoinedPlayers = new List<JoinedPlayerData>();

    public static void ResetRoom()
    {
        JoinedPlayers.Clear();
    }
}