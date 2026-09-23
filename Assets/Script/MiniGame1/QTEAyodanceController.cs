using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class QTEAyodanceController : MonoBehaviour
{
    public enum QTEKey { A, X, B, Y }

    [Header("Pengaturan Player / PC")]
    public int playerID = 1;

    [Header("Pengaturan Permainan")]
    public int totalQueueLength = 5;
    public int playerScore = 0;

    [Header("UI References (World Space Canvas)")]
    public Transform hudContainer;
    public GameObject iconPrefab;

    [Header("Sprite / Visual Key XBOX")]
    public Sprite spriteA;
    public Sprite spriteX;
    public Sprite spriteB;
    public Sprite spriteY;

    private List<QTEKey> visibleQueue = new List<QTEKey>();
    private List<GameObject> spawnedIcons = new List<GameObject>();

    void Start()
    {
        // SetupInputSystem() sudah dihapus total agar tidak ada kebocoran sinyal
        StartQTE();
    }

    public void StartQTE()
    {
        playerScore = 0;
        visibleQueue.Clear();

        for (int i = 0; i < totalQueueLength; i++)
        {
            visibleQueue.Add(GetRandomKey());
        }

        UpdateUI();
    }

    void Update()
    {
        QTEKey? inputKey = CheckPlayerInput();

        if (inputKey.HasValue)
        {
            if (visibleQueue.Count > 0 && inputKey.Value == visibleQueue[0])
            {
                playerScore += 10;
                Debug.Log($"PC {playerID}: Hit Benar! Skor: {playerScore}");

                visibleQueue.RemoveAt(0);
                visibleQueue.Add(GetRandomKey());
                UpdateUI();
            }
            else
            {
                Debug.Log($"PC {playerID}: Input Salah!");
            }
        }
    }

    QTEKey? CheckPlayerInput()
    {
        // 1. JALUR GAMEPAD HARDWARE ABSOLUT (Tidak akan tertukar)
        int gamepadIndex = playerID - 1;
        if (Gamepad.all.Count > gamepadIndex)
        {
            Gamepad myGamepad = Gamepad.all[gamepadIndex];

            // Pastikan gamepad tersebut benar-benar aktif
            if (myGamepad != null)
            {
                if (myGamepad.buttonSouth.wasPressedThisFrame) return QTEKey.A;
                if (myGamepad.buttonWest.wasPressedThisFrame) return QTEKey.X;
                if (myGamepad.buttonEast.wasPressedThisFrame) return QTEKey.B;
                if (myGamepad.buttonNorth.wasPressedThisFrame) return QTEKey.Y;
            }
        }

        // 2. JALUR KEYBOARD SPESIFIK (Sebagai cadangan test di Editor)
        if (Keyboard.current != null)
        {
            if (playerID == 1)
            {
                if (Keyboard.current.jKey.wasPressedThisFrame) return QTEKey.A;
                if (Keyboard.current.iKey.wasPressedThisFrame) return QTEKey.X;
                if (Keyboard.current.lKey.wasPressedThisFrame) return QTEKey.B;
                if (Keyboard.current.kKey.wasPressedThisFrame) return QTEKey.Y;
            }
            else if (playerID == 2)
            {
                if (Keyboard.current.aKey.wasPressedThisFrame) return QTEKey.A;
                if (Keyboard.current.xKey.wasPressedThisFrame) return QTEKey.X;
                if (Keyboard.current.bKey.wasPressedThisFrame) return QTEKey.B;
                if (Keyboard.current.yKey.wasPressedThisFrame) return QTEKey.Y;
            }
        }

        return null;
    }

    QTEKey GetRandomKey()
    {
        return (QTEKey)Random.Range(0, 4);
    }

    void UpdateUI()
    {
        foreach (var icon in spawnedIcons)
        {
            Destroy(icon);
        }
        spawnedIcons.Clear();

        for (int i = visibleQueue.Count - 1; i >= 0; i--)
        {
            GameObject obj = Instantiate(iconPrefab, hudContainer);

            obj.transform.localScale = Vector3.one;
            obj.transform.localPosition = new Vector3(obj.transform.localPosition.x, obj.transform.localPosition.y, 0f);

            Image img = obj.GetComponent<Image>();
            QTEKey key = visibleQueue[i];

            if (img != null)
            {
                img.sprite = key switch
                {
                    QTEKey.A => spriteA,
                    QTEKey.X => spriteX,
                    QTEKey.B => spriteB,
                    QTEKey.Y => spriteY,
                    _ => null
                };
            }
            spawnedIcons.Add(obj);
        }
    }
}