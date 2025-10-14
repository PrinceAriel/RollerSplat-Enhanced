using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallController : MonoBehaviour
{
    [Header("Movement")]
    public Rigidbody rb;
    public float speed = 15;
    public int minSwipeRecognition = 500;
    
    [Header("Visual Effects")]
    public ParticleSystem paintSplatterPrefab;
    public ParticleSystem trailEffect;
    public ParticleSystem wallImpactEffect;
    public GameObject paintTrailPrefab;
    
    [Header("Audio")]
    public AudioClip swipeSound;
    public AudioClip paintSound;
    public AudioClip wallHitSound;
    public AudioClip comboSound;

    private bool isTraveling;
    private Vector3 travelDirection;
    private Vector2 swipePosLastFrame;
    private Vector2 swipePosCurrentFrame;
    private Vector2 currentSwipe;
    private Vector3 nextCollisionPosition;
    private Color solveColor;
    
    // Enhanced features
    private int comboCounter = 0;
    private float comboTimer = 0f;
    private float comboTimeWindow = 0.3f;
    private List<GameObject> paintTrails = new List<GameObject>();
    private float lastPaintTime;
    private AudioSource audioSource;

    private void Start()
    {
        solveColor = Random.ColorHSV(.5f, 1);
        GetComponent<MeshRenderer>().material.color = solveColor;
        
        // Setup audio
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 0; // 2D sound
        
        // Initialize trail effect
        if (trailEffect != null)
        {
            var main = trailEffect.main;
            main.startColor = solveColor;
            trailEffect.Play();
        }
    }

    private void Update()
    {
        // Update combo timer
        if (comboTimer > 0)
        {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0)
            {
                ResetCombo();
            }
        }
    }

    private void FixedUpdate()
    {
        if (isTraveling)
        {
            rb.velocity = travelDirection * speed;
            
            // Add slight rotation for visual interest
            transform.Rotate(travelDirection * speed * 2, Space.World);
        }

        // Paint the ground with enhanced feedback
        Collider[] hitColliders = Physics.OverlapSphere(transform.position - (Vector3.up/2), .05f);
        int i = 0;
        while (i < hitColliders.Length)
        {
            GroundPiece ground = hitColliders[i].transform.GetComponent<GroundPiece>();

            if (ground && !ground.isColored)
            {
                ground.Colored(solveColor);
                
                // Enhanced painting feedback
                OnPaintGround(ground.transform.position);
                
                // Increment combo
                comboCounter++;
                comboTimer = comboTimeWindow;
                
                // Play combo sound at higher combos
                if (comboCounter > 3 && comboCounter % 3 == 0)
                {
                    PlaySound(comboSound, 1f + (comboCounter * 0.05f));
                }
            }

            i++;
        }

        // Create paint trail
        if (isTraveling && paintTrailPrefab != null && Time.time - lastPaintTime > 0.1f)
        {
            CreatePaintTrail();
            lastPaintTime = Time.time;
        }

        if (nextCollisionPosition != Vector3.zero)
        {
            if (Vector3.Distance(transform.position, nextCollisionPosition) < 1)
            {
                isTraveling = false;
                travelDirection = Vector3.zero;
                nextCollisionPosition = Vector3.zero;
            }
        }

        if (isTraveling)
            return;

        // Swipe mechanism (unchanged logic)
        if (Input.GetMouseButton(0))
        {
            swipePosCurrentFrame = new Vector2(Input.mousePosition.x, Input.mousePosition.y);

            if (swipePosLastFrame != Vector2.zero)
            {
                currentSwipe = swipePosCurrentFrame - swipePosLastFrame;

                if (currentSwipe.sqrMagnitude < minSwipeRecognition)
                    return;

                currentSwipe.Normalize();

                if (currentSwipe.x > -0.5f && currentSwipe.x < 0.5f)
                {
                    SetDestination(currentSwipe.y > 0 ? Vector3.forward : Vector3.back); 
                }   

                if (currentSwipe.y > -0.5f && currentSwipe.y < 0.5f)
                {
                    SetDestination(currentSwipe.x > 0 ? Vector3.right : Vector3.left);
                }
            }

            swipePosLastFrame = swipePosCurrentFrame;
        }

        if (Input.GetMouseButtonUp(0))
        {
            swipePosLastFrame = Vector2.zero;
            currentSwipe = Vector2.zero;
        }
    }

    private void SetDestination(Vector3 direction)
    {
        travelDirection = direction;

        RaycastHit hit;
        if (Physics.Raycast(transform.position, direction, out hit, 100f))
        {
            nextCollisionPosition = hit.point;
        }

        isTraveling = true;
        
        // Play swipe sound
        PlaySound(swipeSound, 1f);
        
        // Add camera shake
        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.ShakeCamera(0.05f, 0.1f);
        }
    }

    private void OnPaintGround(Vector3 position)
    {
        // Spawn paint splatter particles
        if (paintSplatterPrefab != null)
        {
            ParticleSystem splatter = Instantiate(paintSplatterPrefab, position, Quaternion.identity);
            var main = splatter.main;
            main.startColor = solveColor;
            splatter.Play();
            Destroy(splatter.gameObject, 2f);
        }
        
        // Play paint sound with pitch variation
        float pitch = 1f + (comboCounter * 0.02f);
        PlaySound(paintSound, Mathf.Min(pitch, 1.5f));
    }

    private void CreatePaintTrail()
    {
        if (paintTrailPrefab != null)
        {
            GameObject trail = Instantiate(paintTrailPrefab, transform.position, Quaternion.identity);
            trail.GetComponent<Renderer>().material.color = solveColor;
            paintTrails.Add(trail);
            Destroy(trail, 3f);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Wall impact effects
        if (collision.gameObject.CompareTag("Wall"))
        {
            // Play wall hit sound
            PlaySound(wallHitSound, 1f);
            
            // Spawn wall impact particles
            if (wallImpactEffect != null)
            {
                ContactPoint contact = collision.contacts[0];
                ParticleSystem impact = Instantiate(wallImpactEffect, contact.point, Quaternion.LookRotation(contact.normal));
                impact.Play();
                Destroy(impact.gameObject, 2f);
            }
            
            // Screen shake on wall hit
            if (CameraShake.Instance != null)
            {
                CameraShake.Instance.ShakeCamera(0.1f, 0.15f);
            }
        }
    }

    private void ResetCombo()
    {
        if (comboCounter > 5)
        {
            // Show combo notification
            if (GameManager.singleton != null)
            {
                GameManager.singleton.ShowComboText(comboCounter);
            }
        }
        comboCounter = 0;
    }

    private void PlaySound(AudioClip clip, float pitch)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.pitch = pitch;
            audioSource.PlayOneShot(clip, 0.5f);
        }
    }

    public int GetComboCounter()
    {
        return comboCounter;
    }
}