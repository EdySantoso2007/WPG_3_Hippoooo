using UnityEngine;

/// <summary>
/// Ditambahkan otomatis oleh ConveyorBelt ke kotak yang menyentuh conveyor.
/// Menyimpan segmen mana yang terakhir menggerakkan kotak, supaya di area tumpang tindih
/// hanya satu segmen (prioritas tertinggi) yang menentukan arah kotak.
/// </summary>
public class ConveyorRider : MonoBehaviour
{
    [HideInInspector] public float lastFixedTime = -1f;
    [HideInInspector] public int lastPriority;
}