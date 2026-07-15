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

    [Header("Death Cutscene")]
    [Tooltip("Where the boss is repositioned to before playing its Deadly_Grab animation.")]
    public Transform deathCutsceneBossPoint;
    [Tooltip("Where the player is repositioned to before playing its Deadly_Grab animation.")]
    public Transform deathCutscenePlayerPoint;

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
    public int attackDamage = 10;

    [Header("Jump Attack")]
    public float jumpAttackHeight = 5f;
    public float jumpAttackDuration = 1.2f;
    public float jumpAttackCooldown = 5f;
    public float jumpAttackDistance = 5f;
    public float firstJumpAttackDelay = 3f;
    [Tooltip("Random +/- offset applied to the jump attack cooldown so jumps don't land on a fixed rhythm.")]
    public float jumpAttackIntervalVariance = 1f;
    public int jumpAttackDamage = 20;

    [Header("Post-Jump Slide")]
    [Tooltip("Max number of quick slides the boss does after landing a jump attack (a random count from 1 up to this is picked each time). Each slide holds its direction toward the player and only stops once it crosses past them.")]
    public int postJumpSlideCount = 2;
    public float slideSpeed = 14f;
    public float slidePause = 0.1f;
    [Tooltip("Safety cap so a slide can't run forever if the player keeps pace with it.")]
    public float slideMaxDuration = 3f;
    [Tooltip("Clearance kept between the boss and a wall it stops at during chase/slide movement.")]
    public float wallCheckRadius = 0.15f;

    [Header("Phase 3 (Spike Enrage)")]
    [Tooltip("Multiplies chase/slide speed and divides jump timing once both spikes have hit the boss.")]
    public float phase3SpeedMultiplier = 1.8f;
    public int phase3Health = 20;

    [Header("Spike Avoidance")]
    [Tooltip("Drag the spike transforms the boss should steer away from while chasing.")]
    public Transform[] spikesToAvoid;
    public float spikeAvoidRadius = 3f;
    public float spikeAvoidStrength = 2f;

    [Header("Player Lock")]
    public Behaviour[] playerScriptsToDisable;

    [Header("Grounding")]
    [Tooltip("How high above the boss position to start the ground raycast.")]
    public float groundRayStartHeight = 8f;

    [Tooltip("How far downward to search for floor beneath the boss.")]
    public float groundRayDistance = 30f;

    [Tooltip("Layers used by the wall-avoidance check (chase/slide/jump landing clamping).")]
    public LayerMask groundMask = ~0;

    [Tooltip("Layers actually walkable as floor. Must exclude ceilings/overhead geometry, or SnapToGround will treat their underside-facing-up top surface as ground and put the boss on top of it.")]
    public LayerMask floorMask = ~0;
    [Tooltip("A ground hit more than this far ABOVE the boss's current height is ignored — stops SnapToGround from teleporting him onto overhead props (beehive, rims) whose tops the ray crosses. Issue: boss running around in mid-air.")]
    public float maxStepUp = 1.2f;
    [Tooltip("Kinematic pseudo-gravity: how fast the boss sinks when SnapToGround finds no floor under him (walking off an edge used to leave him striding on air forever).")]
    public float unsupportedFallSpeed = 12f;

    private Rigidbody rb;
    private Animator animator;
    private Collider groundingCollider;
    private Renderer bossRenderer;

    private BossState state = BossState.Waiting;
    private float nextAttackTime;
    private float nextJumpTime;
    private bool busy;

    private int spikeHitCount;
    private bool phase3Active;

    private float groundOffset;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int JumpHash = Animator.StringToHash("Jump");
    private static readonly int LandHash = Animator.StringToHash("Land");
    private static readonly int AttackHash = Animator.StringToHash("Attack");

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        groundingCollider = GetComponent<Collider>();
        bossRenderer = GetComponentInChildren<Renderer>();

        // Freeze Z position too: the boss lives on the 2.5D gameplay plane.
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        groundOffset = 0;
    }

    void Start()
    {
        // Intro waits until the player reaches the high-rise area.
    }

    void Update()
    {
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
            if (Time.time >= nextJumpTime)
            {
                StartCoroutine(JumpAttackRoutine());
                return;
            }

            float distance = Vector3.Distance(player.position, transform.position);

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
        direction.z = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        direction.Normalize();

        float speed = chaseSpeed;

        BossStuckInHoney honey = GetComponent<BossStuckInHoney>();
        bool stuckInHoney = honey != null && honey.IsInHoney;

        if (honey != null)
            speed = Mathf.Min(chaseSpeed, honey.GetCurrentSpeed());

        // Honey overrides avoidance on purpose: a slowed boss can be lured
        // straight into a spike instead of steering around it.
        if (!stuckInHoney)
            direction = ApplySpikeAvoidance(direction);

        Vector3 newPos = rb.position + direction * speed * Time.fixedDeltaTime;
        newPos = ClampHorizontalMove(rb.position, newPos);

        // Kinematic body: no gravity. Snap to the floor when there is one under
        // him; sink at unsupportedFallSpeed when there is not, instead of
        // striding on air at whatever height the last move left him.
        if (TryGetGroundHeight(newPos, out float groundY))
            newPos.y = groundY;
        else
            newPos.y -= unsupportedFallSpeed * Time.fixedDeltaTime;

        rb.MovePosition(newPos);

        animator.SetFloat(SpeedHash, speed);
        FacePlayer();
    }

    // Kinematic rigidbodies are never stopped by physics collision, so nothing
    // was preventing the boss from sliding straight through walls. This does
    // a horizontal sweep along the intended move and clips it at the first
    // solid hit (skipping the boss's own colliders).
    Vector3 ClampHorizontalMove(Vector3 from, Vector3 to)
    {
        Vector3 delta = to - from;
        Vector3 flatDelta = new Vector3(delta.x, 0f, delta.z);
        float distance = flatDelta.magnitude;

        if (distance < 0.0001f)
            return to;

        Vector3 direction = flatDelta / distance;
        Vector3 castOrigin = from + Vector3.up * (groundOffset > 0.01f ? groundOffset : 1.2f);

        // Plain raycast rather than a SphereCast: a wide sphere starting this close to
        // the boss constantly overlapped nearby clutter (rings, spikes, the hive) and
        // reported false hits at ~0 distance, which was clamping ALL horizontal
        // movement - chase included - to a standstill. Only look exactly as far as
        // this frame's intended move (no extra lookahead) so a wall a few frames
        // away doesn't prematurely stop movement that hasn't reached it yet.
        if (Physics.Raycast(castOrigin, direction, out RaycastHit hit, distance, groundMask, QueryTriggerInteraction.Ignore))
        {
            // A shallow hit normal means we swept into sloped ground, not a wall -
            // only steep/vertical surfaces should actually block horizontal movement.
            bool isWall = hit.normal.y < 0.5f;

            // A thin raycast (unlike the old SphereCast) has no volume, so even a
            // ~0 distance hit is meaningful here - it means a wall sits exactly at
            // "from" in the direction we're trying to move, which is precisely the
            // "starting right against a wall" case that must still be clamped.
            if (isWall && !hit.collider.transform.IsChildOf(transform))
            {
                float safeDistance = Mathf.Max(hit.distance - wallCheckRadius, 0f);
                Vector3 clamped = from + direction * safeDistance;
                clamped.y = to.y;
                return clamped;
            }
        }

        return to;
    }

    // Blends a repulsion push away from any nearby spikesToAvoid into the desired
    // chase direction, weighted so closer spikes push harder. Steers around them
    // rather than blocking movement outright.
    Vector3 ApplySpikeAvoidance(Vector3 desiredDirection)
    {
        if (spikesToAvoid == null || spikesToAvoid.Length == 0)
            return desiredDirection;

        Vector3 avoidance = Vector3.zero;

        foreach (Transform spike in spikesToAvoid)
        {
            if (spike == null)
                continue;

            Vector3 toSpike = transform.position - spike.position;
            toSpike.y = 0f;
            toSpike.z = 0f;

            float dist = toSpike.magnitude;

            if (dist < spikeAvoidRadius && dist > 0.001f)
            {
                float strength = 1f - (dist / spikeAvoidRadius);
                avoidance += toSpike.normalized * strength;
            }
        }

        if (avoidance.sqrMagnitude < 0.0001f)
            return desiredDirection;

        return (desiredDirection + avoidance * spikeAvoidStrength).normalized;
    }

    // Kinematic bodies also never get stopped by physics vertically, so nothing
    // was keeping the boss's jump arcs from clipping straight through a ceiling.
    // Casts straight up from a point on the arc and returns how much headroom is
    // actually available there.
    float GetHeightClearance(Vector3 position)
    {
        Vector3 castOrigin = position + Vector3.up * (groundOffset > 0.01f ? groundOffset : 0.2f);

        if (Physics.Raycast(castOrigin, Vector3.up, out RaycastHit hit, 100f, groundMask, QueryTriggerInteraction.Ignore))
        {
            if (!hit.collider.transform.IsChildOf(transform))
                return Mathf.Max(hit.distance - wallCheckRadius, 0f);
        }

        return Mathf.Infinity;
    }

    IEnumerator IntroSequence()
    {
        foreach(Behaviour script in playerScriptsToDisable)
        {
            if(script != null)
                script.enabled = false;
        }
        state = BossState.IntroJump;
        busy = true;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        animator.SetFloat(SpeedHash, 0f);
        animator.SetTrigger(JumpHash);

        Vector3 start = transform.position;
        Vector3 end = introLandingPoint != null ? SnapToGround(introLandingPoint.position) : SnapToGround(transform.position);

        float timer = 0f;

        while (timer < introJumpDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / introJumpDuration);

            Vector3 position = Vector3.Lerp(start, end, t);
            float bump = Mathf.Sin(t * Mathf.PI) * introJumpHeight;
            position.y += bump;

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

        nextJumpTime = Time.time + firstJumpAttackDelay + Random.Range(-jumpAttackIntervalVariance, jumpAttackIntervalVariance);

        state = BossState.Chase;
        busy = false;
    }

    IEnumerator TurnToPlayerRoutine()
    {
        if (player == null)
            yield break;

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;
        direction.z = 0f;

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

        if (player != null && Vector3.Distance(player.position, transform.position) <= attackRange)
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

    IEnumerator JumpAttackRoutine()
    {
        busy = true;
        state = BossState.Attack;

        animator.SetFloat(SpeedHash, 0f);
        animator.SetTrigger(JumpHash);

        Vector3 away = player.position - transform.position;
        away.y = 0f;
        away.z = 0f;
        away = away.sqrMagnitude > 0.0001f ? away.normalized : transform.forward;

        Vector3 rawLandingTarget = player.position + away * jumpAttackDistance;
        Vector3 clampedLandingTarget = ClampHorizontalMove(transform.position, rawLandingTarget);
        Vector3 landingPosition = SnapToGround(clampedLandingTarget);
        Vector3 start = transform.position;

        // Jump as high as needed to reach the player (issue #44): if they are
        // airborne above us, raise the arc so its peak clears their height.
        float requiredPeak = (player.position.y - start.y) + 1.5f;
        float arcHeight = Mathf.Max(jumpAttackHeight, requiredPeak);

        bool struckMidAir = false;

        float timer = 0f;

        while (timer < jumpAttackDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / jumpAttackDuration);

            Vector3 position = Vector3.Lerp(start, landingPosition, t);
            float bump = Mathf.Sin(t * Mathf.PI) * arcHeight;
            position.y += Mathf.Min(bump, GetHeightClearance(position));

            rb.MovePosition(position);

            // Contact damage while airborne — the point of jumping that high.
            if (!struckMidAir && player != null &&
                Vector3.Distance(transform.position, player.position) <= attackRange)
            {
                Health airHealth = player.GetComponent<Health>();
                if (airHealth != null)
                {
                    airHealth.TakeDamage(jumpAttackDamage);
                    struckMidAir = true;
                }
            }

            yield return null;
        }

        rb.position = landingPosition;

        animator.SetTrigger(LandHash);
        FacePlayer();

        if (!struckMidAir && player != null && Vector3.Distance(transform.position, player.position) <= attackRange + 1f)
        {
            Health health = player.GetComponent<Health>();
            if (health != null)
            {
                health.TakeDamage(jumpAttackDamage);
            }
        }

        yield return StartCoroutine(PostJumpSlideRoutine());

        nextJumpTime = Time.time + jumpAttackCooldown + Random.Range(-jumpAttackIntervalVariance, jumpAttackIntervalVariance);

        state = BossState.Chase;
        busy = false;
    }

    IEnumerator PostJumpSlideRoutine()
    {
        if (player == null)
            yield break;

        int slides = Random.Range(1, postJumpSlideCount + 1);

        for (int i = 0; i < slides; i++)
        {
            yield return StartCoroutine(SlideThroughPlayer());

            if (i < slides - 1)
                yield return new WaitForSeconds(slidePause);
        }
    }

    // Holds a single direction toward the player at the moment it starts, and
    // keeps sliding that way (ignoring any further player movement) until it
    // crosses past wherever the player currently is.
    IEnumerator SlideThroughPlayer()
    {
        if (player == null)
            yield break;

        float direction = player.position.x >= transform.position.x ? 1f : -1f;

        animator.SetFloat(SpeedHash, slideSpeed);
        FacePlayer();

        float timer = 0f;

        // The jump attack lands the boss ON the player's x, so a plain
        // "crossed past the player" check was true on the very first frame and
        // the slide ended instantly (issue #43). Require sliding a real
        // distance PAST the player before stopping.
        const float slideOvershoot = 1.5f;

        while (timer < slideMaxDuration)
        {
            timer += Time.deltaTime;

            Vector3 intendedPos = rb.position + new Vector3(direction * slideSpeed * Time.deltaTime, 0f, 0f);
            Vector3 nextPos = ClampHorizontalMove(rb.position, intendedPos);
            bool hitWall = Vector3.Distance(nextPos, intendedPos) > 0.001f;

            nextPos = SnapToGround(nextPos);
            rb.MovePosition(nextPos);

            if (hitWall)
                break;

            if (player == null)
                break;

            // Stop only once we are well past the player in the slide direction.
            float past = (nextPos.x - player.position.x) * direction;
            if (past >= slideOvershoot)
                break;

            yield return null;
        }

        animator.SetFloat(SpeedHash, 0f);
    }

    public void RegisterSpikeHit()
    {
        spikeHitCount++;

        if (spikeHitCount >= 2 && !phase3Active)
        {
            EnterPhase3();
        }
    }

    void EnterPhase3()
    {
        phase3Active = true;

        chaseSpeed *= phase3SpeedMultiplier;
        slideSpeed *= phase3SpeedMultiplier;
        jumpAttackDuration /= phase3SpeedMultiplier;
        jumpAttackCooldown /= phase3SpeedMultiplier;
        firstJumpAttackDelay /= phase3SpeedMultiplier;

        BossHealth bossHealth = GetComponent<BossHealth>();

        if (bossHealth != null)
        {
            bossHealth.EnterPhase3(phase3Health);
        }
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

        SetPlayerLocked(true);

        if (deathCutsceneBossPoint != null)
        {
            rb.position = deathCutsceneBossPoint.position;
            transform.rotation = deathCutsceneBossPoint.rotation;
        }

        if (player != null && deathCutscenePlayerPoint != null)
        {
            Rigidbody playerRb = player.GetComponent<Rigidbody>();

            if (playerRb != null)
            {
                playerRb.linearVelocity = Vector3.zero;
                playerRb.angularVelocity = Vector3.zero;
                playerRb.isKinematic = true;
                playerRb.position = deathCutscenePlayerPoint.position;
            }

            player.position = deathCutscenePlayerPoint.position;
            player.rotation = deathCutscenePlayerPoint.rotation;
        }

        animator.SetFloat(SpeedHash, 0f);
        animator.CrossFade("rig|Deadly_Grab(Boss)", 0.1f);

        if (player != null)
        {
            Animator playerAnimator = player.GetComponent<Animator>();

            if (playerAnimator != null)
            {
                playerAnimator.CrossFade("rigGirl|Deadly_Grab(Girl)", 0.1f);
            }
        }
    }

    void FacePlayer()
    {
        if (player == null)
            return;

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;
        direction.z = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(direction.normalized, Vector3.up) *
            Quaternion.Euler(0f, facingYawOffset, 0f);

        transform.rotation = targetRotation;
    }

    void SetPlayerLocked(bool locked)
    {
        if (playerScriptsToDisable == null)
            return;

        foreach (Behaviour script in playerScriptsToDisable)
        {
            if (script != null)
                script.enabled = !locked;
        }
    }

Vector3 SnapToGround(Vector3 position)
{
    if (TryGetGroundHeight(position, out float groundY))
        position.y = groundY;
    return position;
}

bool TryGetGroundHeight(Vector3 position, out float groundY)
{
    groundY = position.y;

    Vector3 rayStart = position + Vector3.up * groundRayStartHeight;
    float rayDistance = groundRayStartHeight + groundRayDistance;

    RaycastHit[] hits = Physics.RaycastAll(
        rayStart,
        Vector3.down,
        rayDistance,
        floorMask,
        QueryTriggerInteraction.Ignore
    );

    if (hits.Length == 0)
        return false;

    System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

    foreach (RaycastHit hit in hits)
    {
        // Skip anything that belongs to the boss itself
        if (hit.collider.transform.IsChildOf(transform))
            continue;

        // Skip surfaces well above the boss's current height (overhead props):
        // walking under the beehive must not teleport him on top of it.
        if (hit.point.y > position.y + maxStepUp)
            continue;

        groundY = hit.point.y + groundOffset;
        return true;
    }

    return false;
}

    float CalculateGroundOffset()
    {
        if (groundingCollider != null)
        {
            // Distance from the pivot down to the collider's actual bottom,
            // whatever the collider type/size/local offset happens to be.
            return transform.position.y - groundingCollider.bounds.min.y;
        }

        if (bossRenderer != null)
        {
            return transform.position.y - bossRenderer.bounds.min.y;
        }

        return 0f;
    }
}