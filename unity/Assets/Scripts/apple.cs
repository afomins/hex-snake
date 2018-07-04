// ---------------------------------------------------------------------------------------------------------------------
using UnityEngine;
using System;

// ---------------------------------------------------------------------------------------------------------------------
public class apple : MonoBehaviour {
    // -----------------------------------------------------------------------------------------------------------------
    // CONFIG
    [System.Serializable]
    public class CConfig : config.CConfigGeneral {
        // -------------------------------------------------------------------------------------------------------------
        public GameObject pref_obj;
        public float rotate_speed;
        public Vector3 rotate_vector;

        // -------------------------------------------------------------------------------------------------------------
        public utils.CMesh mesh;
        public void Finalize() {
            mesh = new utils.CMesh(pref_obj);
        }
    }

    // -----------------------------------------------------------------------------------------------------------------
    // STATIC
    public static apple Instantiate(apple.CConfig cfg) {
        apple obj = cfg.mesh.Instantiate("apple").AddComponent<apple>();
        obj.m_config = cfg;
        return obj;
    }

    // -----------------------------------------------------------------------------------------------------------------
    // NON-STATIC
    public apple.CConfig m_config;

    // -----------------------------------------------------------------------------------------------------------------
    void Update() {
        transform.Rotate(
            m_config.rotate_vector, 
            m_config.rotate_speed * Time.deltaTime);
    }
}
