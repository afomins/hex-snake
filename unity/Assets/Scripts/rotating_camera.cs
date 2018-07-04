// ---------------------------------------------------------------------------------------------------------------------
using UnityEngine;
using System.Collections;

// ---------------------------------------------------------------------------------------------------------------------
public class rotating_camera : MonoBehaviour {
    // -----------------------------------------------------------------------------------------------------------------
    // CONFIG
    [System.Serializable]
    public class CConfig : config.CConfigGeneral {
        // -------------------------------------------------------------------------------------------------------------
        public GameObject pref_obj;
        public bool is_rotating;
        public float height;
        public float rotate_radius;
        public float look_at_radius;
        public float rotate_angle;
        public Vector3 translate_speed;
        public Vector3 rotate_speed;
    }

    // -----------------------------------------------------------------------------------------------------------------
    // STATIC
    public static rotating_camera Instantiate(rotating_camera.CConfig cfg, string name, GameObject owner) {
        rotating_camera obj = utils.Instantiate(cfg.pref_obj, name).AddComponent<rotating_camera>();

        obj.m_config = cfg;
        obj.m_owner = owner;
        obj.m_tmp_transform = new GameObject(name + "-tmp-transform");
        obj.m_rotate_tween = new utils.CVector3Tween();
        obj.m_translate_tween = new utils.CVector3Tween();

        obj.UpdateTransform();
        obj.transform.eulerAngles = obj.m_tmp_transform.transform.eulerAngles;
        obj.transform.position = obj.m_tmp_transform.transform.position;

        return obj;
    }

    // -----------------------------------------------------------------------------------------------------------------
    // NON-STATIC
    public rotating_camera.CConfig m_config;
    private GameObject m_owner, m_tmp_transform;
    private utils.CVector3Tween m_rotate_tween, m_translate_tween;

    // -----------------------------------------------------------------------------------------------------------------
    private void Update() {
        UpdateTransform();

        // Rotate
        m_rotate_tween.Start(m_tmp_transform.transform.eulerAngles, m_config.rotate_speed, true);
        if(m_rotate_tween.Update(transform.eulerAngles, Time.deltaTime)) {
            transform.eulerAngles = m_rotate_tween.m_result;
            transform.Rotate(m_rotate_tween.m_result - transform.eulerAngles);
        }

        // Translate
        m_translate_tween.Start(m_tmp_transform.transform.position, m_config.translate_speed, false);
        if(m_translate_tween.Update(transform.position, Time.deltaTime)) {
            transform.position = m_translate_tween.m_result;
        }
    }

    // -----------------------------------------------------------------------------------------------------------------
    private void UpdateTransform() {
        Vector3 p = m_owner.transform.position;
        Vector3 f = m_owner.transform.forward; f.y = 0.0f; f.Normalize();
        Vector3 look_at_pos = p + f * m_config.look_at_radius;

        if(m_config.is_rotating) {
            m_tmp_transform.transform.position = new Vector3(
                look_at_pos.x - f.x * m_config.rotate_radius, 
                look_at_pos.y + m_config.height, 
                look_at_pos.z - f.z * m_config.rotate_radius);

            m_tmp_transform.transform.RotateAround(look_at_pos, Vector3.up, m_config.rotate_angle);
            m_tmp_transform.transform.LookAt(look_at_pos);
        } else {
            m_tmp_transform.transform.position = new Vector3(
                look_at_pos.x, look_at_pos.y + m_config.height, look_at_pos.z);

            m_tmp_transform.transform.RotateAround(Vector3.forward, Vector3.up, 0.0f);
            m_tmp_transform.transform.LookAt(look_at_pos);
        }
    }
}
