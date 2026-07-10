using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Camera))]
public class BossCutsceneCamera : MonoBehaviour
{
    [Header("Player")]
    public Transform player;

    [Header("Trigger")]
    public Transform highRisePlatform;

    [Header("Boss")]
    public Transform boss;

    [Header("Follow")]
    public Transform followTarget;

    public Vector3 gameplayOffset = new Vector3(0f, 2f, -10f);
    public Vector3 cutsceneOffset = new Vector3(0f, 2.5f, -12f);

    [Header("Smoothing")]
    public float gameplaySmooth = 4f;
    public float cutsceneSmooth = 3f;

    [Header("Zoom")]
    public float gameplayOrthoSize = 6f;
    public float cutsceneOrthoSize = 8f;

    [Header("Cutscene")]
    public float cutsceneDuration = 3f;

    private Camera cam;

    private bool cutscenePlaying;
    private bool cutsceneFinished;

    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;
    private float originalZoom;


    void Awake()
    {
        cam = GetComponent<Camera>();

        followTarget = player;

        originalZoom = gameplayOrthoSize;
    }


    void Update()
    {
        if (!cutsceneFinished &&
            !cutscenePlaying &&
            player.position.y > highRisePlatform.position.y)
        {
            StartCoroutine(PlayBossIntro());
        }
    }


    void LateUpdate()
    {
        if (followTarget == null)
            return;


        Vector3 offset = cutscenePlaying ? 
            cutsceneOffset : 
            gameplayOffset;


        float smooth = cutscenePlaying ?
            cutsceneSmooth :
            gameplaySmooth;


        Vector3 desiredPosition = followTarget.position + offset;


        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            smooth * Time.deltaTime
        );


        float targetZoom = cutscenePlaying ?
            cutsceneOrthoSize :
            originalZoom;


        cam.orthographicSize = Mathf.Lerp(
            cam.orthographicSize,
            targetZoom,
            5f * Time.deltaTime
        );
    }



    IEnumerator PlayBossIntro()
    {
        cutscenePlaying = true;


        // Save current player camera position
        originalCameraPosition = transform.position;
        originalCameraRotation = transform.rotation;


        // Focus boss
        if (boss != null)
        {
            followTarget = boss;
        }


        yield return new WaitForSeconds(cutsceneDuration);



        // Return camera to player
        followTarget = player;


        cutscenePlaying = false;


        // Smoothly return to player view
        float returnTime = 1.5f;
        float timer = 0;


        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;


        Vector3 endPosition = player.position + gameplayOffset;


        while (timer < returnTime)
        {
            timer += Time.deltaTime;

            float t = timer / returnTime;


            transform.position = Vector3.Lerp(
                startPosition,
                endPosition,
                t
            );


            transform.rotation = Quaternion.Lerp(
                startRotation,
                originalCameraRotation,
                t
            );


            yield return null;
        }


        transform.position = endPosition;
        transform.rotation = originalCameraRotation;


        cutsceneFinished = true;
    }
}