using UnityEngine;

/// <summary>
/// Padanan worker_hippo.gd (Godot). Pasang di root model hippo di FiredScene.
/// </summary>
public class WorkerHippo : MonoBehaviour
{
    [Tooltip("Index pemain: 0 Merah, 1 Biru, 2 Hijau, 3 Kuning (sama dengan urutan join & PlayerManager.playerTextures).")]
    [Range(0, 3)] public int playerIndex = 0;

    [Tooltip("Kosongkan = otomatis cari Animator di anak.")]
    public Animator animator;

    // ID properti material (URP Lit: _BaseMap/_BaseColor, Built-in: _MainTex/_Color)
    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    /// <summary>Mainkan reaksi (Trigger atau nama State di Animator).</summary>
    public void PlayReaction(string animName)
    {
        if (!AnimatorUtil.Fire(animator, animName))
            Debug.LogWarning($"[WorkerHippo] Animasi '{animName}' tidak ditemukan di worker index {playerIndex}.", this);
        else
            Debug.Log($"[WorkerHippo] Worker {playerIndex} memainkan: {animName}");
    }

    /// <summary>Ganti texture sesuai warna pemain (sama dengan PlayerManager.ApplyTexture).</summary>
    public void ApplyTexture(Texture2D texture)
    {
        if (texture == null) return;

        Transform root = animator != null ? animator.transform : transform;
        foreach (Renderer r in root.GetComponentsInChildren<Renderer>())
        {
            foreach (Material m in r.materials) // salinan material per renderer
            {
                if (m.HasProperty(BaseMapId))
                {
                    m.SetTexture(BaseMapId, texture);
                    if (m.HasProperty(BaseColorId)) m.SetColor(BaseColorId, Color.white);
                }
                else if (m.HasProperty(MainTexId))
                {
                    m.SetTexture(MainTexId, texture);
                    if (m.HasProperty(ColorId)) m.SetColor(ColorId, Color.white);
                }
            }
        }
    }
}

public static class AnimatorUtil
{
    /// <summary>
    /// Coba SetTrigger kalau ada parameter Trigger bernama 'name'.
    /// Kalau tidak ada, coba CrossFade ke State dengan nama tersebut.
    /// </summary>
    public static bool Fire(Animator animator, string name)
    {
        if (animator == null || string.IsNullOrEmpty(name)) return false;

        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            if (p.type == AnimatorControllerParameterType.Trigger && p.name == name)
            {
                animator.SetTrigger(name);
                return true;
            }
        }

        int hash = Animator.StringToHash(name);
        if (animator.HasState(0, hash))
        {
            animator.CrossFade(hash, 0.1f, 0);
            return true;
        }
        return false;
    }
}