using UnityEngine;

public class FPSCounter : MonoBehaviour
{
    int m_FPS;
    int m_SmoothedFPS;
    float m_MeasuredFPSAtTime;

    GUIStyle m_Style;

    Rect m_TextAreaRect;

    const float k_Width = 100;
    const float k_Height = 24;
    const float k_Margin = 20;

    void Update()
    {
        m_FPS = Mathf.RoundToInt(1f / Time.smoothDeltaTime);

        if (Time.realtimeSinceStartup >= m_MeasuredFPSAtTime + 1)
        {
            m_MeasuredFPSAtTime = Time.realtimeSinceStartup;
            m_SmoothedFPS = m_FPS;
        }
    }

    void OnGUI()
    {
        if (m_TextAreaRect == null)
            m_TextAreaRect = new(k_Margin, k_Margin, k_Width, k_Height);

        if (m_Style == null)
            m_Style = new GUIStyle()
            {
                normal = new GUIStyleState()
                {
                    textColor = Color.white,
                },
                fontSize = 24
            };

        GUI.enabled = false;
        GUI.TextField(m_TextAreaRect, $"{m_SmoothedFPS}, {m_FPS}", m_Style);
        GUI.enabled = true;
    }
}
