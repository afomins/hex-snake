// ---------------------------------------------------------------------------------------------------------------------
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

// ---------------------------------------------------------------------------------------------------------------------
public class compas : MonoBehaviour {
    // -----------------------------------------------------------------------------------------------------------------
    // CONFIG
    [System.Serializable]
    public class CConfig : config.CConfigGeneral {
        // -------------------------------------------------------------------------------------------------------------
        public GameObject arrow_pref_obj;
        public float tween_speed;
        public float rotate_radius;

        // -------------------------------------------------------------------------------------------------------------
        public utils.CMesh arrow_mesh;
        public void Finalize() {
            arrow_mesh = new utils.CMesh(arrow_pref_obj);
        }
    }

    // -----------------------------------------------------------------------------------------------------------------
    // STATIC
    public static compas Instantiate(compas.CConfig cfg, string name) {
        compas obj = utils.Instantiate(null, name).AddComponent<compas>();

        obj.m_config = cfg;
        obj.m_arrow_obj = cfg.arrow_mesh.Instantiate("arrow");
        obj.m_arrow_obj.transform.parent = obj.transform;
        obj.m_arrow_obj.transform.localPosition = new Vector3(0.0f, 0.5f, cfg.rotate_radius);

        return obj;
    }

    // -----------------------------------------------------------------------------------------------------------------
    // NON-STATIC
    public compas.CConfig m_config;

    private GameObject m_arrow_obj;
    private Vector3 m_dest;
    private float m_rotate_angle;

    // -----------------------------------------------------------------------------------------------------------------
    private void Update() {
        // Calculate rotation angle from compas center to destiantion
        Vector3 dest_vect = m_dest - transform.position;
        float dest_angle = utils.GetVectorAngle(dest_vect, Vector3.forward);
        utils.TweenAngleBySpeed(m_rotate_angle, dest_angle, m_config.tween_speed, Time.deltaTime, ref m_rotate_angle);

        // Rotate compas
        transform.rotation = new Quaternion();
        transform.Rotate(new Vector3(0.0f, m_rotate_angle, 0.0f));
    }

    // -----------------------------------------------------------------------------------------------------------------
    public void SetPosition(Vector3 pos) {
        transform.position = pos;
    }

    // -----------------------------------------------------------------------------------------------------------------
    public void SetDestination(Vector3 dest) {
        m_dest = dest;
    }
}
