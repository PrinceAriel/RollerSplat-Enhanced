using System.Collections;
using UnityEngine;

public class GroundPiece : MonoBehaviour
{
    [Header("State")]
    public bool isColored = false;
    
    [Header("Visual Effects")]
    public bool useColorAnimation = true;
    public float colorAnimationSpeed = 3f;
    public bool useScalePulse = true;
    public float pulseAmount = 1.1f;
    
    [Header("Materials")]
    public Material uncoloredMaterial;
    public Material coloredMaterial;
    
    private MeshRenderer meshRenderer;
    private Color targetColor;
    private Color originalColor;
    private Vector3 originalScale;
    private bool isAnimating = false;

    private void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        originalColor = meshRenderer.material.color;
        originalScale = transform.localScale;
    }

    public void Colored(Color color)
    {
        if (isColored) return;
        
        targetColor = color;
        isColored = true;
        
        // Start color animation
        if (useColorAnimation)
        {
            StartCoroutine(AnimateColor());
        }
        else
        {
            meshRenderer.material.color = color;
        }
        
        // Pulse animation
        if (useScalePulse && !isAnimating)
        {
            StartCoroutine(PulseScale());
        }
        
        // Check level completion
        FindObjectOfType<GameManager>().CheckComplete();
    }

    private IEnumerator AnimateColor()
    {
        float t = 0;
        Color startColor = meshRenderer.material.color;
        
        while (t < 1f)
        {
            t += Time.deltaTime * colorAnimationSpeed;
            meshRenderer.material.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }
        
        meshRenderer.material.color = targetColor;
    }

    private IEnumerator PulseScale()
    {
        isAnimating = true;
        
        // Scale up
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * 8f;
            transform.localScale = Vector3.Lerp(originalScale, originalScale * pulseAmount, t);
            yield return null;
        }
        
        // Scale back down
        t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * 4f;
            transform.localScale = Vector3.Lerp(originalScale * pulseAmount, originalScale, t);
            yield return null;
        }
        
        transform.localScale = originalScale;
        isAnimating = false;
    }

    // Add glow effect when painted
    public void AddGlowEffect()
    {
        StartCoroutine(GlowPulse());
    }

    private IEnumerator GlowPulse()
    {
        Material mat = meshRenderer.material;
        
        // Enable emission
        mat.EnableKeyword("_EMISSION");
        
        for (int i = 0; i < 3; i++)
        {
            // Glow up
            float t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime * 3f;
                mat.SetColor("_EmissionColor", targetColor * Mathf.Lerp(0, 0.5f, t));
                yield return null;
            }
            
            // Glow down
            t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime * 3f;
                mat.SetColor("_EmissionColor", targetColor * Mathf.Lerp(0.5f, 0, t));
                yield return null;
            }
        }
        
        mat.SetColor("_EmissionColor", Color.black);
    }
}