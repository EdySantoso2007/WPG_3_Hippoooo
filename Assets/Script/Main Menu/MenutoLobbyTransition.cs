using System.Collections;
using UnityEngine;
using Cinemachine;

public class MenutoLobbyTransition : MonoBehaviour
{
    [Header("UI Canvas Groups")]
    [SerializeField] private CanvasGroup menuCanvasGroup;
    [SerializeField] private CanvasGroup lobbyCanvasGroup;
    [SerializeField] private CanvasGroup settingsCanvasGroup;
    [SerializeField] private CanvasGroup creditsCanvasGroup;

    [Header("Fade & Delay Settings")]
    [SerializeField] private float fadeDuration = 0.8f;
    [SerializeField] private float delayBeforePanelFadeIn = 0.5f;

    [Header("Audio SFX")]
    [SerializeField] private AudioSource sfxAudioSource;
    [SerializeField] private AudioClip buttonClickSFX;

    [Header("Cinemachine Cameras")]
    [SerializeField] private CinemachineVirtualCamera menuCamera;
    [SerializeField] private CinemachineVirtualCamera lobbyCamera;
    [SerializeField] private CinemachineVirtualCamera settingsCamera;
    [SerializeField] private CinemachineVirtualCamera creditsCamera;

    [Header("Mouse Parallax Settings")]
    [SerializeField] private float maxRotationAngle = 3f;
    [SerializeField] private float smoothSpeed = 5f;

    private CinemachineVirtualCamera activeCamera;
    private CanvasGroup activeCanvasGroup;

    private Quaternion menuCamInitialRot;
    private Quaternion lobbyCamInitialRot;
    private Quaternion settingsCamInitialRot;
    private Quaternion creditsCamInitialRot;

    private bool isTransitioning = false;

    private void Start()
    {
        // Simpan rotasi awal kamera untuk efek parallax
        if (menuCamera != null) menuCamInitialRot = menuCamera.transform.localRotation;
        if (lobbyCamera != null) lobbyCamInitialRot = lobbyCamera.transform.localRotation;
        if (settingsCamera != null) settingsCamInitialRot = settingsCamera.transform.localRotation;
        if (creditsCamera != null) creditsCamInitialRot = creditsCamera.transform.localRotation;

        // Set keadaan awal (Main Menu aktif)
        activeCamera = menuCamera;
        activeCanvasGroup = menuCanvasGroup;

        ResetCameraPriorities();
        if (menuCamera != null) menuCamera.Priority = 10;

        SetupCanvasGroup(menuCanvasGroup, 1f, true);
        SetupCanvasGroup(lobbyCanvasGroup, 0f, false);
        SetupCanvasGroup(settingsCanvasGroup, 0f, false);
        SetupCanvasGroup(creditsCanvasGroup, 0f, false);
    }

    private void Update()
    {
        ApplyMouseParallax(menuCamera, menuCamInitialRot);
        ApplyMouseParallax(lobbyCamera, lobbyCamInitialRot);
        ApplyMouseParallax(settingsCamera, settingsCamInitialRot);
        ApplyMouseParallax(creditsCamera, creditsCamInitialRot);
    }

    private void ApplyMouseParallax(CinemachineVirtualCamera vcam, Quaternion initialRotation)
    {
        if (vcam == null || !vcam.gameObject.activeInHierarchy) return;

        float mouseX = (Input.mousePosition.x / Screen.width - 0.5f) * 2f;
        float mouseY = (Input.mousePosition.y / Screen.height - 0.5f) * 2f;

        mouseX = Mathf.Clamp(mouseX, -1f, 1f);
        mouseY = Mathf.Clamp(mouseY, -1f, 1f);

        Quaternion targetRotation = initialRotation * Quaternion.Euler(-mouseY * maxRotationAngle, mouseX * maxRotationAngle, 0f);

        vcam.transform.localRotation = Quaternion.Slerp(
            vcam.transform.localRotation, 
            targetRotation, 
            Time.deltaTime * smoothSpeed
        );
    }

    private void PlayButtonSFX()
    {
        if (sfxAudioSource != null && buttonClickSFX != null)
        {
            sfxAudioSource.PlayOneShot(buttonClickSFX);
        }
    }

    // --- FUNGSI NAVIGASI TOMBOL ---

    public void OnPlayButtonPressed()
    {
        NavigateTo(lobbyCamera, lobbyCanvasGroup);
    }

    public void OnSettingsButtonPressed()
    {
        NavigateTo(settingsCamera, settingsCanvasGroup);
    }

    public void OnCreditsButtonPressed()
    {
        NavigateTo(creditsCamera, creditsCanvasGroup);
    }

    public void OnBackToMenuButtonPressed()
    {
        NavigateTo(menuCamera, menuCanvasGroup);
    }

    // --- FUNGSI QUIT GAME ---
    public void OnQuitButtonPressed()
    {
        PlayButtonSFX();

        #if UNITY_EDITOR
            // Jika di-run lewat Unity Editor, hentikan Play Mode
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            // Jika diketik/di-build jadi executable (.exe / apk), tutup aplikasi
            Application.Quit();
        #endif
    }

    // --- ALUR TRANSISI UTAMA ---

    private void NavigateTo(CinemachineVirtualCamera targetCam, CanvasGroup targetCanvas)
    {
        if (isTransitioning || targetCam == activeCamera) return;

        PlayButtonSFX();
        StartCoroutine(TransitionRoutine(targetCam, targetCanvas));
    }

    private IEnumerator TransitionRoutine(CinemachineVirtualCamera targetCam, CanvasGroup targetCanvas)
    {
        isTransitioning = true;

        // 1. Ubah Priority Kamera
        ResetCameraPriorities();
        if (targetCam != null) targetCam.Priority = 10;

        // 2. Fade Out Panel Saat Ini
        if (activeCanvasGroup != null)
        {
            yield return StartCoroutine(FadeCanvasGroup(activeCanvasGroup, activeCanvasGroup.alpha, 0f, fadeDuration));
            SetupCanvasGroup(activeCanvasGroup, 0f, false);
        }

        // 3. Jeda waktu menunggu pergerakan kamera Cinemachine
        yield return new WaitForSeconds(delayBeforePanelFadeIn);

        // 4. Fade In Panel Tujuan
        if (targetCanvas != null)
        {
            targetCanvas.gameObject.SetActive(true);
            yield return StartCoroutine(FadeCanvasGroup(targetCanvas, 0f, 1f, fadeDuration));
            SetupCanvasGroup(targetCanvas, 1f, true);
        }

        // Update state aktif
        activeCamera = targetCam;
        activeCanvasGroup = targetCanvas;

        isTransitioning = false;
    }

    private void ResetCameraPriorities()
    {
        if (menuCamera != null) menuCamera.Priority = 0;
        if (lobbyCamera != null) lobbyCamera.Priority = 0;
        if (settingsCamera != null) settingsCamera.Priority = 0;
        if (creditsCamera != null) creditsCamera.Priority = 0;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float startAlpha, float endAlpha, float duration)
    {
        float timeElapsed = 0f;
        while (timeElapsed < duration)
        {
            timeElapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(startAlpha, endAlpha, timeElapsed / duration);
            yield return null;
        }
        cg.alpha = endAlpha;
    }

    private void SetupCanvasGroup(CanvasGroup cg, float alpha, bool interactable)
    {
        if (cg == null) return;
        cg.alpha = alpha;
        cg.interactable = interactable;
        cg.blocksRaycasts = interactable;
        cg.gameObject.SetActive(alpha > 0f);
    }
}