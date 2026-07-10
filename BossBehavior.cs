using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BossMovement : MonoBehaviour
{
    public Transform player;


    [Header("Intro Jump")]
    public Transform introStartPoint;
    public Transform introLandingPoint;
    public float introJumpHeight = 5f;
    public float introJumpDuration = 1.5f;
    public float introPause = 1f;

    public Behaviour[] playerScriptsToDisable;



    [Header("Movement")]
    public float moveSpeed = 4f;
    public float attackDistance = 2f;



    [Header("Punch")]
    public float punchCooldown = 2f;
    public float punchDamage = 10f;



    [Header("Jump Attack")]
    public float jumpHeight = 5f;
    public float jumpDuration = 1.2f;
    public float jumpCooldown = 5f;
    public float jumpDistance = 5f;
    public float firstJumpDelay = 3f;



    private Animator animator;
    private Rigidbody rb;


    private bool attacking;
    private bool introFinished;


    private float nextPunchTime;
    private float nextJumpTime;



    private static readonly int SpeedHash =
        Animator.StringToHash("Speed");

    private static readonly int PunchHash =
        Animator.StringToHash("Punch");

    private static readonly int JumpHash =
        Animator.StringToHash("Jump");

    private static readonly int LandHash =
        Animator.StringToHash("Land");



    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();

        rb.interpolation = RigidbodyInterpolation.Interpolate;

        StartCoroutine(IntroJump());
    }



    void Update()
    {
        if(player == null)
            return;


        if(!introFinished || attacking)
            return;



        float distance =
            Vector3.Distance(
                transform.position,
                player.position
            );


        // Jump from anywhere
        if(Time.time >= nextJumpTime)
        {
            StartCoroutine(JumpAttack());
            return;
        }



        if(distance <= attackDistance)
        {
            if(Time.time >= nextPunchTime)
            {
                StartCoroutine(Punch());
            }
        }
        else
        {
            ChasePlayer();
        }
    }



    IEnumerator IntroJump()
    {
        attacking = true;


        foreach(Behaviour script in playerScriptsToDisable)
        {
            if(script != null)
                script.enabled = false;
        }



        Vector3 start =
            introStartPoint.position;

        Vector3 end =
            introLandingPoint.position;



        rb.isKinematic = true;


        rb.position = start;


        animator.SetTrigger(JumpHash);



        float timer = 0;


        while(timer < introJumpDuration)
        {
            timer += Time.deltaTime;


            float t =
                timer / introJumpDuration;


            Vector3 pos =
                Vector3.Lerp(
                    start,
                    end,
                    t
                );


            pos.y +=
                Mathf.Sin(t * Mathf.PI)
                * introJumpHeight;



            rb.MovePosition(pos);


            yield return null;
        }



        rb.position = end;


        animator.SetTrigger(LandHash);


        yield return new WaitForSeconds(introPause);


        rb.isKinematic = false;


        FacePlayer();



        foreach(Behaviour script in playerScriptsToDisable)
        {
            if(script != null)
                script.enabled = true;
        }



        nextJumpTime =
            Time.time + firstJumpDelay;


        introFinished = true;
        attacking = false;
    }





    void ChasePlayer()
    {
        Vector3 direction =
            player.position -
            transform.position;


        direction.y = 0;


        direction.Normalize();



        rb.MovePosition(
            rb.position +
            direction *
            moveSpeed *
            Time.deltaTime
        );


        animator.SetFloat(
            SpeedHash,
            moveSpeed
        );


        FacePlayer();
    }





    IEnumerator Punch()
    {
        attacking = true;


        animator.SetFloat(
            SpeedHash,
            0
        );


        FacePlayer();


        animator.SetTrigger(PunchHash);



        yield return new WaitForSeconds(.4f);



        if(Vector3.Distance(
            transform.position,
            player.position)
            <= attackDistance)
        {
            Health health =
                player.GetComponent<Health>();

            if(health != null)
                health.TakeDamage(
                    punchDamage
                );
        }



        nextPunchTime =
            Time.time + punchCooldown;


        attacking = false;
    }





    IEnumerator JumpAttack()
    {
        attacking = true;


        animator.SetFloat(
            SpeedHash,
            0
        );


        animator.SetTrigger(JumpHash);



        Vector3 landingPosition =
            player.position;



        Vector3 away =
            (player.position -
            transform.position).normalized;


        landingPosition +=
            away * jumpDistance;



        // Find floor height
        RaycastHit fhit;


        if(Physics.Raycast(
            landingPosition + Vector3.up * 20,
            Vector3.down,
            out fhit,
            50f))
        {
            landingPosition.y =
                fhit.point.y;
        }



        Vector3 start =
            transform.position;



        float timer = 0;



        while(timer < jumpDuration)
        {
            timer += Time.deltaTime;


            float t =
                timer / jumpDuration;



            Vector3 pos =
                Vector3.Lerp(
                    start,
                    landingPosition,
                    t
                );


            pos.y +=
                Mathf.Sin(
                    t * Mathf.PI
                ) *
                jumpHeight;



            rb.MovePosition(pos);



            yield return null;
        }



        rb.position =
            landingPosition;



        animator.SetTrigger(LandHash);



        Collider[] hits =
            Physics.OverlapSphere(
                transform.position,
                2f
            );


        foreach(Collider hit in hits)
        {
            if(hit.transform == player)
            {
                Health health =
                    player.GetComponent<Health>();

                if(health != null)
                    health.TakeDamage(20);
            }
        }



        nextJumpTime =
            Time.time + jumpCooldown;


        attacking = false;
    }





    void FacePlayer()
    {
        Vector3 direction =
            player.position -
            transform.position;


        direction.y = 0;


        if(direction != Vector3.zero)
        {
            transform.rotation =
                Quaternion.LookRotation(
                    direction
                );
        }
    }
}