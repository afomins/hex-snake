// ---------------------------------------------------------------------------------------------------------------------
using UnityEngine;
using System.Collections;

// ---------------------------------------------------------------------------------------------------------------------
public class main : MonoBehaviour {
    // -----------------------------------------------------------------------------------------------------------------
    public config m_config;
    private gameplay m_gameplay;

    // -----------------------------------------------------------------------------------------------------------------
    private void Start() {
        m_config.gameplay.Finalize();
        m_gameplay = gameplay.Instantiate(m_config.gameplay);
    }

    // -----------------------------------------------------------------------------------------------------------------
    private void OnGUI() {
        string score_str = string.Format("Score: {0}", m_gameplay.m_config.score);
        string info_str = "hex-snake v0.1\n (c) 2014, Alex Fomin, afomins@gmail.com";

        GUI.skin.box.alignment = TextAnchor.MiddleRight;
        GUI.Box(new Rect(Screen.width - 100, 0, 100, 50), score_str);
        GUI.Box(new Rect(Screen.width - 300, Screen.height - 50, 300, 50), info_str);
    }
}
