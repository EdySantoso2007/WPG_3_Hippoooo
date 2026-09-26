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

    private UIPanelState currentPanel;

    private void Start()
    {
        ShowPanel(UIPanelState.MainMenu);
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

    public void ShowPanel(UIPanelState panelToActive)
    {
        currentPanel = panelToActive;

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