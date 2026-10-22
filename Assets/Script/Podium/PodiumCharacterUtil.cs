using UnityEngine;

// Fungsi bantu untuk memunculkan hippo "pajangan" (tanpa kontrol / fisika) di scene Podium & Dipecat.
public static class PodiumCharacterUtil
{
    // Property material (URP Lit = _BaseMap, Built-in = _MainTex)
    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    /// <summary>
    /// Munculkan salinan prefab di posisi tertentu. Script gameplay, Rigidbody, Collider, dan AudioSource dibuang.
    /// controllerOverride (opsional) mengganti Animator Controller supaya pakai controller khusus podium.
    /// </summary>
    public static GameObject Spawn(GameObject prefab, Transform parent, Vector3 position, Quaternion rotation,
                                   Texture2D texture, RuntimeAnimatorController controllerOverride)
    {
        if (prefab == null)
        {
            Debug.LogError("[PodiumCharacterUtil] Character Prefab belum diisi!");
            return null;
        }

        // Dibuat di bawah induk NONAKTIF dulu, supaya Awake/OnEnable script player
        // (PlayerInput, PlayerMovement, dll) tidak sempat jalan sebelum dibuang.
        GameObject holder = new GameObject("PodiumHolder");
        holder.SetActive(false);

        GameObject go = Object.Instantiate(prefab, holder.transform);
        Strip(go);
        ApplyTexture(go, texture);

        Animator anim = go.GetComponentInChildren<Animator>(true);
        if (anim != null)
        {
            if (controllerOverride != null) anim.runtimeAnimatorController = controllerOverride;
            anim.applyRootMotion = false; // supaya animasi tidak menggeser karakter dari titiknya
        }

        // Pindahkan keluar dari holder (menjadi aktif di sini)
        go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(position, rotation);
        if (!go.activeSelf) go.SetActive(true);

        Object.Destroy(holder);
        return go;
    }

    /// <summary>Langsung mainkan state Animator berdasarkan nama state-nya.</summary>
    public static void PlayState(GameObject go, string stateName)
    {
        if (go == null || string.IsNullOrEmpty(stateName)) return;

        Animator anim = go.GetComponentInChildren<Animator>();
        if (anim == null || !anim.isActiveAndEnabled || anim.runtimeAnimatorController == null) return;

        int hash = Animator.StringToHash(stateName);
        if (!anim.HasState(0, hash))
        {
            Debug.LogWarning("[PodiumCharacterUtil] State '" + stateName + "' tidak ditemukan di Animator Controller.");
            return;
        }

        anim.Play(hash, 0, 0f);
    }

    private static void Strip(GameObject go)
    {
        // 1. Script (PlayerInput, PlayerMovement, PlayerInteraction, dst), kecuali UI / TMP
        foreach (MonoBehaviour mb in go.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb == null) continue;

            string ns = mb.GetType().Namespace;
            if (ns != null && (ns.StartsWith("TMPro") || ns.StartsWith("UnityEngine.UI"))) continue;

            Object.DestroyImmediate(mb);
        }

        // 2. Fisika & audio (harus setelah script, karena PlayerMovement butuh Rigidbody)
        foreach (Rigidbody r in go.GetComponentsInChildren<Rigidbody>(true)) Object.DestroyImmediate(r);
        foreach (Collider c in go.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
        foreach (AudioSource a in go.GetComponentsInChildren<AudioSource>(true)) Object.DestroyImmediate(a);
    }

    private static void ApplyTexture(GameObject player, Texture2D texture)
    {
        if (texture == null) return;

        Animator animator = player.GetComponentInChildren<Animator>(true);
        Transform modelRoot = animator != null ? animator.transform : player.transform;

        foreach (Renderer r in modelRoot.GetComponentsInChildren<Renderer>(true))
        {
            foreach (Material m in r.materials)
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