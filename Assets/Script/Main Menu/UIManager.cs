using UnityEngine;

public class UIManager : MonoBehaviour
{
    // Enum untuk memudahkan identifikasi panel yang ada
    public enum UIPanelState
    {
        MainMenu,
        ModeSelection,
        Room
    }

    [Header("UI Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject modeSelectionPanel;
    [SerializeField] private GameObject roomPanel;

    private void Start()
    {
        // Tampilkan Main Menu saat game pertama kali dijalankan
        ShowPanel(UIPanelState.MainMenu);
    }

    /// <summary>
    /// Fungsi pusat untuk mengatur panel mana yang aktif
    /// </summary>
    public void ShowPanel(UIPanelState panelToActive)
    {
        // Nonaktifkan semua panel terlebih dahulu
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (modeSelectionPanel != null) modeSelectionPanel.SetActive(false);
        if (roomPanel != null) roomPanel.SetActive(false);

        // Aktifkan panel yang dipilih
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
        }
    }

    // --- Fungsi Helper untuk Dipasang di Button OnClick Event ---

    public void OpenMainMenu()
    {
        ShowPanel(UIPanelState.MainMenu);
    }

    public void OpenModeSelection()
    {
        ShowPanel(UIPanelState.ModeSelection);
    }

    public void OpenRoom()
    {
        ShowPanel(UIPanelState.Room);
    }

    public void QuitGame()
    {
        // Untuk keluar dari aplikasi saat game sudah di-build (.exe / .apk / .app)
        Application.Quit();

        // Khusus saat testing di dalam Unity Editor (opsional)
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}