using UnityEngine;

public class BossAnimation : MonoBehaviour
{
    [SerializeField] private Animator animator;

    [Header("Auto Play")]
    [SerializeField] private bool autoPlay = true;
    [SerializeField] private float minInterval = 3f;
    [SerializeField] private float maxInterval = 7f;

    [Header("Nama State Idle (sesuai di Animator)")]
    [SerializeField] private string idleStateName = "Armature|BossIdle";

    private float timer;

    void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        ResetTimer();
    }

    void Update()
    {
        if (!autoPlay) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        // Hanya picu animasi baru kalau boss sedang Idle dan tidak sedang transisi
        bool isIdle = animator.GetCurrentAnimatorStateInfo(0).IsName(idleStateName)
                      && !animator.IsInTransition(0);
        if (!isIdle) return;

        if (Random.value < 0.5f) PlayAngry();
        else PlayLaugh();

        ResetTimer();
    }

    void ResetTimer()
    {
        timer = Random.Range(minInterval, maxInterval);
    }

    public void PlayAngry() => animator.SetTrigger("Angry");
    public void PlayLaugh() => animator.SetTrigger("Laugh");
}