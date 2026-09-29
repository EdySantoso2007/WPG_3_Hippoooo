using UnityEngine;
using UnityEngine.InputSystem;

public class UIManager : MonoBehaviour
{
    public enum UIPanelState
    {
        MainMenu,
        ModeSelection,
        Room,
        Setting
    }

    [Header("UI Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject modeSelectionPanel;
    [SerializeField] private GameObject roomPanel;
    [SerializeField] private GameObject settingPanel;

    [Header("SFX")]
    [SerializeField] private AudioClip clickSfx;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
    private AudioSource audioSource;
    private bool playSfxOnShow = false;

    private UIPanelState currentPanel;

    private void Start()
    {
        audioSource = GetComponentInChildren<AudioSource>();

        ShowPanel(UIPanelState.MainMenu);
        playSfxOnShow = true; // setelah panel awal tampil, perpindahan panel berikutnya berbunyi
    }

    private void Update()
    {
        if (currentPanel == UIPanelState.MainMenu)
        {
            // --- Gamepad ---
            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                if (gamepad.buttonSouth.wasPressedThisFrame) OpenModeSelection(); // A
                if (gamepad.buttonNorth.wasPressedThisFrame) OpenSettings();      // Y
                if (gamepad.buttonEast.wasPressedThisFrame) QuitGame();           // B
            }

            // --- Keyboard (testing only) ---
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.fKey.wasPressedThisFrame) OpenModeSelection(); // F = Play (testing)
            }
        }
        else if (currentPanel == UIPanelState.Setting)
        {
            var gamepad = Gamepad.current;
            if (gamepad != null && gamepad.buttonEast.wasPressedThisFrame) OpenMainMenu(); // B = kembali
        }
        else if (currentPanel == UIPanelState.ModeSelection)
        {
            var gamepad = Gamepad.current;
            if (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame) OpenRoom(); // A = masuk room
            if (gamepad != null && gamepad.buttonEast.wasPressedThisFrame) OpenMainMenu(); // B = kembali
        }
        else if (currentPanel == UIPanelState.Room)
        {
            var gamepad = Gamepad.current;
            if (gamepad != null && gamepad.buttonEast.wasPressedThisFrame) OpenModeSelection(); // B = kembali
        }
    }

    // Bisa juga dipanggil dari OnClick tombol lain yang tidak pindah panel
    public void PlayClickSfx()
    {
        if (clickSfx == null) return;

        if (audioSource != null)
        {
            audioSource.PlayOneShot(clickSfx, sfxVolume);
        }
        else
        {
            Vector3 pos = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            AudioSource.PlayClipAtPoint(clickSfx, pos, sfxVolume);
        }
    }

    public void ShowPanel(UIPanelState panelToActive)
    {
        currentPanel = panelToActive;

        if (playSfxOnShow) PlayClickSfx();

        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (modeSelectionPanel != null) modeSelectionPanel.SetActive(false);
        if (roomPanel != null) roomPanel.SetActive(false);
        if (settingPanel != null) settingPanel.SetActive(false);

        switch (panelToActive)
        {
            case UIPanelState.MainMenu:
                if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
                break;
            case UIPanelState.ModeSelection:
                if (modeSelectionPanel != null) modeSelectionPanel.SetActive(true);
                break;
            case UIPanelState.Room:
                if (roomPanel != null) roomPanel.SetActive(true);
                break;
            case UIPanelState.Setting:
                if (settingPanel != null) settingPanel.SetActive(true);
                break;
        }
    }

    public void OpenMainMenu() => ShowPanel(UIPanelState.MainMenu);
    public void OpenModeSelection() => ShowPanel(UIPanelState.ModeSelection);
    public void OpenRoom() => ShowPanel(UIPanelState.Room);
    public void OpenSettings() => ShowPanel(UIPanelState.Setting);

    public void QuitGame()
    {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}