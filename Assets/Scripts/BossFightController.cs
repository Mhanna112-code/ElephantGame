using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(BossHealth))]
public class BossFightController : MonoBehaviour
{
    public enum BossState
    {
        Waiting,
        IntroJump,
        Turning,
        Chase,
        Attack,
        Dead
    }

    [Header("References")]
    public Transform player;
    public Transform introLandingPoint;

    [Header("Trigger")]
    [Tooltip("Boss intro (jump + camera cutscene) starts once the player's Y crosses above this transform.")]
    public Transform highRisePlatform;

    [Header("Facing")]
    [Tooltip("Use 0 if the model faces the right way. Use 180 if it faces backward.")]
    public float facingYawOffset = 0f;

    [Header("Intro Jump")]
    public float introJumpDuration = 1.2f;
    public float introJumpHeight = 2.5f;
    public float landPauseBeforeTurn = 0.6f;
    public float turnDuration = 0.75f;
    public float idleAfterTurn = 0.25f;

    [Header("Movement")]
    public float chaseSpeed = 3.5f;
    public float attackRange = 1.5f;

    [Header("Attack")]
    public float attackCooldown = 2f;
    public float attackWindup = 0.35f;
    public int attackDamage = 1;

    [Header("Player Lock")]
    public Behaviour[] playerScriptsToDisable;

    private Rigidbody rb;
    private Animator animator;
    private BossState state = BossState.Waiting;
    private float nextAttackTime;
    private bool busy;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int JumpHash = Animator.StringToHash("Jump");
    private static readonly int LandHash = Animator.StringToHash("Land");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int DieHash = Animator.StringToHash("Die");

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

        // Freeze Z position too: the boss lives on the 2.5D gameplay plane. Part
        // of issue #40 (hopping off-axis, landing misaligned with the floor).
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void Start()
    {
        // Intro no longer fires immediately on scene load - it waits for the
        // player to cross above highRisePlatform (see Update()).
    }
    private string lastAnimationState = ""; 
    void PrintCurrentAnimationState()
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        if (animator.IsInTransition(0))
            return;

        foreach (AnimatorClipInfo clip in animator.GetCurrentAnimatorClipInfo(0))
        {
            if (clip.clip.name != lastAnimationState)
            {
                lastAnimationState = clip.clip.name;
                Debug.Log("Animation: " + lastAnimationState);
            }
        }
    }

    void Update()
    {
        Debug.Log("state: " + state);
        PrintCurrentAnimationState();
        if (state == BossState.Dead || busy || player == null)
            return;

        if (state == BossState.Waiting)
        {
            if (highRisePlatform != null && player.position.y > highRisePlatform.position.y)
            {
                SetPlayerLocked(true);
                StartCoroutine(IntroSequence());
            }
            return;
        }

        if (state == BossState.Chase)
        {
            float distance = Vector3.Distance(transform.position, player.position);

            if (distance <= attackRange && Time.time >= nextAttackTime)
            {
                StartCoroutine(AttackRoutine());
            }
        }
    }

    void FixedUpdate()
    {
        if (state != BossState.Chase || busy || player == null)
            return;

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;
        direction.z = 0f; // stay on the gameplay plane (issue #40)

        if (direction.sqrMagnitude < 0.0001f)
            return;

        direction.Normalize();

        // honey slows the boss down (issue #41 / beehive fight design)
        float speed = chaseSpeed;
        BossStuckInHoney honey = GetComponent<BossStuckInHoney>();
        if (honey != null)
            speed = Mathf.Min(chaseSpeed, honey.GetCurrentSpeed());

        rb.MovePosition(rb.position + direction * speed * Time.fixedDeltaTime);

        animator.SetFloat(SpeedHash, speed);

        FacePlayer();
    }

    IEnumerator IntroSequence()
    {
        state = BossState.IntroJump;
        busy = true;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        animator.SetFloat(SpeedHash, 0f);
        animator.SetTrigger(JumpHash);

        Vector3 start = transform.position;
        Vector3 end = introLandingPoint != null ? introLandingPoint.position : transform.position;

        float timer = 0f;

        while (timer < introJumpDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / introJumpDuration);

            Vector3 position = Vector3.Lerp(start, end, t);
            position.y += Mathf.Sin(t * Mathf.PI) * introJumpHeight;

            transform.position = position;
            yield return null;
        }

        transform.position = end;

        animator.SetTrigger(LandHash);
        animator.SetFloat(SpeedHash, 0f);

        yield return new WaitForSeconds(landPauseBeforeTurn);

        state = BossState.Turning;

        yield return StartCoroutine(TurnToPlayerRoutine());

        yield return new WaitForSeconds(idleAfterTurn);

        SetPlayerLocked(false);

        state = BossState.Chase;
        busy = false;
    }

    IEnumerator TurnToPlayerRoutine()
    {
        if (player == null)
            yield break;

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            yield break;

        Quaternion startRot = transform.rotation;
        Quaternion targetRot = Quaternion.LookRotation(direction.normalized, Vector3.up) * Quaternion.Euler(0f, facingYawOffset, 0f);

        float timer = 0f;

        while (timer < turnDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / turnDuration);

            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        transform.rotation = targetRot;
        animator.SetFloat(SpeedHash, 0f);
    }

    IEnumerator AttackRoutine()
    {
        busy = true;
        state = BossState.Attack;
        nextAttackTime = Time.time + attackCooldown;

        animator.SetFloat(SpeedHash, 0f);
        FacePlayer();
        animator.SetTrigger(AttackHash);

        yield return new WaitForSeconds(attackWindup);

        if (player != null && Vector3.Distance(transform.position, player.position) <= attackRange)
        {
            Health health = player.GetComponent<Health>();
            if (health != null)
            {
                health.TakeDamage(attackDamage);
            }
        }

        yield return new WaitForSeconds(0.2f);

        state = BossState.Chase;
        busy = false;
    }

    public void Die()
    {
        if (state == BossState.Dead)
            return;

        state = BossState.Dead;
        busy = true;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        animator.SetFloat(SpeedHash, 0f);
        animator.SetTrigger(DieHash);
    }

    void FacePlayer()
    {
        if (player == null)
            return;

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up) * Quaternion.Euler(0f, facingYawOffset, 0f);
        transform.rotation = targetRotation;
    }

    void SetPlayerLocked(bool locked)
    {
        if (playerScriptsToDisable == null)
            return;

        foreach (Behaviour script in playerScriptsToDisable)
        {
            if (script != null)
            {
                script.enabled = !locked;
            }
        }
    }
}