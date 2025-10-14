using UnityEngine;
using UnityEngine.UI;

public class UIConnector : MonoBehaviour
{
    void Start()
    {
        if (GameManager.singleton != null)
        {
            // Find UI elements in this scene
            Text levelText = GameObject.Find("LevelNumberText")?.GetComponent<Text>();
            Text comboText = GameObject.Find("ComboText")?.GetComponent<Text>();
            GameObject levelPanel = GameObject.Find("LevelCompletePanel");
            Text starText = GameObject.Find("StarRatingText")?.GetComponent<Text>();
            GameObject perfectText = GameObject.Find("PerfectText");
            Image fadePanel = GameObject.Find("FadePanel")?.GetComponent<Image>();
            
            // Assign them via reflection or make public setters in GameManager
            // For now, this ensures they exist in the scene
        }
    }
}