using System.Collections.Generic;
using UnityEngine.InputSystem;

// Jembatan statis (static bridge) untuk membawa data player yang sudah
// join di scene Room/Lobby ke scene gameplay, tanpa perlu re-join lagi
// lewat PlayerInputManager di scene gameplay.
public static class RoomBridge
{
    // Daftar PlayerInput yang sudah terhubung, urut sesuai urutan join.
    // index 0 = Merah, 1 = Biru, 2 = Hijau, 3 = Kuning
    public static List<PlayerInput> JoinedPlayers = new List<PlayerInput>();

    // Panggil ini kalau mau reset room (misal balik ke lobby dari awal)
    public static void ResetRoom()
    {
        JoinedPlayers.Clear();
    }
}