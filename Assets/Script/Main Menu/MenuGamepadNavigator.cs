using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Tempel di panel Main Menu (objek yang di-aktif/nonaktifkan saat menu dibuka).
// Setiap panel ini aktif, pilihan otomatis kembali ke tombol paling atas (PLAY).
public class MenuGamepadNavigator : MonoBehaviour
{
    [Header("Tombol (urut dari atas ke bawah: Play, Setting, Quit, Credit)")]
    public List<Button> buttons = new List<Button>();
    public bool wrapAround = true;

    [Header("Hover (teks mengembang)")]
    public float hoverScale = 1.25f;
    public float scaleSpeed = 12f;

    [Header("Input")]
    [Range(0.3f, 0.9f)] public float stickThreshold = 0.6f;
    [Tooltip("Input diabaikan sebentar setelah panel muncul, supaya tekanan A dari panel sebelumnya tidak tembus.")]
    public float inputDelayOnEnable = 0.2f;
    [Tooltip("(Opsional) Kalau diisi, input diabaikan selama CanvasGroup ini tidak interactable.")]
    public CanvasGroup canvasGroup;

    [Header("SFX (opsional)")]
    public AudioSource audioSource;
    public AudioClip moveSfx;
    [Range(0f, 1f)] public float sfxVolume = 1f;

    private Transform[] labels;
    private Vector3[] baseScales;
    private int index;
    private float enableTime;
    private bool stickHeld;

    void Awake()
    {
        labels = new Transform[buttons.Count];
        baseScales = new Vector3[buttons.Count];

        for (int i = 0; i < buttons.Count; i++)
        {
            if (buttons[i] == null) continue;

            TMP_Text t = buttons[i].GetComponentInChildren<TMP_Text>();
            labels[i] = t != null ? t.transform : buttons[i].transform;
            baseScales[i] = labels[i].localScale;

            // Hover mouse ikut memindahkan pilihan
            MenuHoverRelay relay = buttons[i].gameObject.GetComponent<MenuHoverRelay>();
            if (relay == null) relay = buttons[i].gameObject.AddComponent<MenuHoverRelay>();
            relay.owner = this;
            relay.index = i;
        }
    }

    void OnEnable()
    {
        enableTime = Time.unscaledTime;
        stickHeld = true; // analog harus kembali ke tengah dulu sebelum bisa menggeser
        index = FindValid(0, 1);
    }

    void OnDisable()
    {
        // Kembalikan ukuran teks seperti semula
        for (int i = 0; i < labels.Length; i++)
            if (labels[i] != null) labels[i].localScale = baseScales[i];
    }

    void Update()
    {
        UpdateHoverScale();

        if (Time.unscaledTime < enableTime + inputDelayOnEnable) return;
        if (canvasGroup != null && !canvasGroup.interactable) return;

        int move = ReadVerticalInput();
        if (move != 0) Move(move);

        if (ReadConfirm() && IsValid(index))
        {
            buttons[index].onClick.Invoke();
        }
    }

    // ---------- Hover ----------

    void UpdateHoverScale()
    {
        for (int i = 0; i < labels.Length; i++)
        {
            if (labels[i] == null) continue;

            Vector3 target = (i == index) ? baseScales[i] * hoverScale : baseScales[i];
            labels[i].localScale = Vector3.Lerp(labels[i].localScale, target, Time.unscaledDeltaTime * scaleSpeed);
        }
    }

    public void SetIndex(int newIndex)
    {
        if (!IsValid(newIndex) || newIndex == index) return;
        index = newIndex;
        PlayMoveSfx();
    }

    // ---------- Input ----------

    // -1 = naik, +1 = turun, 0 = tidak ada
    int ReadVerticalInput()
    {
        int dir = 0;

        Gamepad pad = Gamepad.current;
        if (pad != null)
        {
            if (pad.dpad.up.wasPressedThisFrame) dir = -1;
            else if (pad.dpad.down.wasPressedThisFrame) dir = 1;

            // Analog kiri: satu dorongan = satu langkah, harus kembali ke tengah untuk langkah berikutnya
            float y = pad.leftStick.ReadValue().y;
            if (Mathf.Abs(y) < stickThreshold * 0.5f)
            {
                stickHeld = false;
            }
            else if (!stickHeld && Mathf.Abs(y) >= stickThreshold)
            {
                stickHeld = true;
                if (dir == 0) dir = y > 0f ? -1 : 1;
            }
        }

        // Keyboard (untuk testing di Editor)
        Keyboard kb = Keyboard.current;
        if (kb != null && dir == 0)
        {
            if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) dir = -1;
            else if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) dir = 1;
        }

        return dir;
    }

    bool ReadConfirm()
    {
        Gamepad pad = Gamepad.current;
        if (pad != null && pad.buttonSouth.wasPressedThisFrame) return true; // A

        Keyboard kb = Keyboard.current;
        if (kb != null && kb.enterKey.wasPressedThisFrame) return true;

        return false;
    }

    void Move(int dir)
    {
        int next = FindNext(index, dir);
        if (next == index) return;

        index = next;
        PlayMoveSfx();
    }

    // ---------- Util ----------

    bool IsValid(int i)
    {
        return i >= 0 && i < buttons.Count && buttons[i] != null
               && buttons[i].gameObject.activeInHierarchy && buttons[i].interactable;
    }

    int FindValid(int start, int dir)
    {
        for (int n = 0; n < buttons.Count; n++)
        {
            int i = ((start + n * dir) % buttons.Count + buttons.Count) % buttons.Count;
            if (IsValid(i)) return i;
        }
        return 0;
    }

    int FindNext(int from, int dir)
    {
        int i = from;
        for (int n = 0; n < buttons.Count; n++)
        {
            i += dir;

            if (wrapAround) i = (i % buttons.Count + buttons.Count) % buttons.Count;
            else if (i < 0 || i >= buttons.Count) return from;

            if (IsValid(i)) return i;
        }
        return from;
    }

    void PlayMoveSfx()
    {
        if (moveSfx != null && audioSource != null)
            audioSource.PlayOneShot(moveSfx, sfxVolume);
    }
}

// Ditambahkan otomatis ke tiap tombol oleh MenuGamepadNavigator
public class MenuHoverRelay : MonoBehaviour, IPointerEnterHandler
{
    [HideInInspector] public MenuGamepadNavigator owner;
    [HideInInspector] public int index;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (owner != null) owner.SetIndex(index);
    }
}