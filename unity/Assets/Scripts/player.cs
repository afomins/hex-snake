// ---------------------------------------------------------------------------------------------------------------------
using UnityEngine;
using System.Collections;
using System;

// ---------------------------------------------------------------------------------------------------------------------
public class player : MonoBehaviour {
    // -----------------------------------------------------------------------------------------------------------------
    // CONFIG
    [System.Serializable]
    public class CConfig : config.CConfigGeneral {
        // -------------------------------------------------------------------------------------------------------------
        public GameObject pref_obj;
        public float rotate_speed;
        public float translate_speed;

        // -------------------------------------------------------------------------------------------------------------
        public utils.CMesh mesh;
        public void Finalize() {
            mesh = new utils.CMesh(pref_obj);
        }
    }

    // -----------------------------------------------------------------------------------------------------------------
    // STATIC
    public static player Instantiate(player.CConfig cfg, string name) {
        player obj = cfg.mesh.Instantiate(name).AddComponent<player>();

        obj.m_config = cfg;
        obj.m_look_at = new utils.CLookAtTween();

        return obj;
    }

    // -----------------------------------------------------------------------------------------------------------------
    // NON-STATIC
    public player.CConfig m_config;
    private utils.CLookAtTween m_look_at;

    // -----------------------------------------------------------------------------------------------------------------
    private void Update() {
        // Rotate
        if(m_look_at.Update(transform.position, transform.eulerAngles, Time.deltaTime)) {
            transform.eulerAngles = m_look_at.m_result;
        }

        // Translate
        float step = Time.deltaTime * m_config.translate_speed;
        transform.position = transform.position + transform.forward * step;
    }

    // -----------------------------------------------------------------------------------------------------------------
    public void SetDestination(Vector3 dest_pos) {
        dest_pos.y += 0.5f;
        m_look_at.Start(dest_pos, m_config.rotate_speed);
    }
}
