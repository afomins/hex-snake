// ---------------------------------------------------------------------------------------------------------------------
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

// ---------------------------------------------------------------------------------------------------------------------
public class utils {
    // -----------------------------------------------------------------------------------------------------------------
    public class CVector4b {
        public bool x, y, z, w;
        public CVector4b (bool value) { Init(value); }
        public void Init(bool value) { x = y = z = w = value; }
    }

    // -----------------------------------------------------------------------------------------------------------------
    public class CVector2i {
        public int x, y;
        public CVector2i (int x, int y) { Set(x, y); }
        public void Set(int _x, int _y) { x = _x; y = _y; }
    }

    // -----------------------------------------------------------------------------------------------------------------
    public class CMesh {
        // -------------------------------------------------------------------------------------------------------------
        private GameObject m_pref_obj;
        private int m_ref_cnt;

        // -------------------------------------------------------------------------------------------------------------
        public CMesh(GameObject pref_obj) { 
            m_pref_obj = pref_obj;
        }

        // -------------------------------------------------------------------------------------------------------------
        public GameObject Instantiate(string name) {
            m_ref_cnt++;
            if(m_pref_obj == null)
                return null;
            else {
                GameObject obj = main.Instantiate(
                    m_pref_obj, m_pref_obj.transform.position, m_pref_obj.transform.rotation) as GameObject;
                obj.name = name;
                return obj;
            }
        }

        // -------------------------------------------------------------------------------------------------------------
        public void Destroy(GameObject obj) {
            m_ref_cnt--;
            main.Destroy(obj);
        }

        // -------------------------------------------------------------------------------------------------------------
        public void ResetPosition(GameObject obj) {
            Vector3 p = m_pref_obj.transform.position;
            obj.transform.position = new Vector3(p.x, p.y, p.z); 
        }

        // -------------------------------------------------------------------------------------------------------------
        public void ResetRotation(GameObject obj) {
            Quaternion r = m_pref_obj.transform.rotation;
            obj.transform.rotation = new Quaternion(r.x, r.y, r.z, r.w); 
        }

        // -------------------------------------------------------------------------------------------------------------
        public void Reset(GameObject obj) {
            ResetPosition(obj);
            ResetRotation(obj);
        }

        // -------------------------------------------------------------------------------------------------------------
        public Vector3 GetBounds() {
            return m_pref_obj.renderer.bounds.size;
        }
    }

    // -----------------------------------------------------------------------------------------------------------------
    public static GameObject Instantiate(GameObject pref_obj, string name = "noname-bastard") {
        GameObject obj = (pref_obj != null) ?
            main.Instantiate(pref_obj, pref_obj.transform.position, pref_obj.transform.rotation) as GameObject :
            new GameObject();
        obj.name = name;
        return obj;
    }

    // -----------------------------------------------------------------------------------------------------------------
    public static void Assert(bool condition, string format = "fuck", params object[] args) {
        if(condition) return;
        Debug.Log(string.Format(format, args));
        throw new Exception();
    }

    // -----------------------------------------------------------------------------------------------------------------
    public static void Log(string format, params object[] args) {
        Debug.Log(string.Format(format, args));
    }

    // -----------------------------------------------------------------------------------------------------------------
    public static float GetDistanceXZ(float x0, float z0, float x1, float z1) {
        float x = (x1 - x0);
        float z = (z1 - z0);
        return (float)Math.Sqrt(x * x + z * z);
    }

    // -----------------------------------------------------------------------------------------------------------------
    public static float GetDistanceXYZ(float x0, float y0, float z0, float x1, float y1, float z1) {
        float x = (x1 - x0);
        float y = (y1 - y0);
        float z = (z1 - z0);
        return (float)Math.Sqrt(x * x + y * y + z * z);
    }

    // -----------------------------------------------------------------------------------------------------------------
    public static int GetParity(int val) {
        int parity = val % 2;
        return (parity < 0) ? -parity : parity;
    }

    // -----------------------------------------------------------------------------------------------------------------
    // Taken from: http://answers.unity3d.com/questions/24983/how-to-calculate-the-angle-between-two-vectors.html
    public static float GetVectorAngle(Vector3 dir, Vector3 forward) {
        // the vector that we want to measure an angle from
        Vector3 referenceForward = forward;

        // the vector perpendicular to referenceForward (90 degrees clockwise)
        // (used to determine if angle is positive or negative)
        Vector3 referenceRight= Vector3.Cross(Vector3.up, referenceForward);

        // the vector of interest
        Vector3 newDirection = dir;

        // Get the angle in degrees between 0 and 180
        float angle = Vector3.Angle(newDirection, referenceForward);

        // Determine if the degree value should be negative.  Here, a positive value
        // from the dot product means that our vector is on the right of the reference vector   
        // whereas a negative value means we're on the left.
        float sign = Mathf.Sign(Vector3.Dot(newDirection, referenceRight));

        return sign * angle;
    }

    // -----------------------------------------------------------------------------------------------------------------
    public static void NormalizeAngle(float cur, ref float dest) {
        float diff = dest - cur;
             if(diff > 180.0f)  dest -= 360.0f;
        else if(diff < -180.0f) dest += 360.0f;
    }

    // -----------------------------------------------------------------------------------------------------------------
    public static bool TweenAngleBySpeed(float cur_val, float dest_val, float speed, float time_delta, 
                                         ref float result) {
        utils.NormalizeAngle(cur_val, ref dest_val);
        return utils.TweenBySpeed(cur_val, dest_val, speed, time_delta, ref result);
    }

    // -----------------------------------------------------------------------------------------------------------------
    public static bool TweenBySpeed(float cur_val, float dest_val, float speed, float time_delta, ref float result) {
        // Save current value as result
        result = cur_val;

        // Initial distance from current value to destination
        float old_diff = dest_val - result;

        // Update result value 
        if(cur_val < dest_val) {
            // Move forward
            result += speed * time_delta;
        } else if(cur_val > dest_val) {
            // Move backward
            result -= speed * time_delta;
        } else {
            // Destination is reached - stop
            return false;
        }

        // Distance from new current value to destination
        float new_diff = dest_val - result;

        // Check whether destination is reached
        if((old_diff >= 0.0f && new_diff <= 0.0f) ||
           (old_diff <= 0.0f && new_diff >= 0.0f)) {
            // Different signs indicate that that destination is reached - stop
            result = dest_val;
            return false;
        } else {
            // Destination not reached - continue
            return true;
        }
    }

    // -----------------------------------------------------------------------------------------------------------------
    public static bool TweenByTime(float init_val, float dest_val, float init_time, float duration, float cur_time, 
                                   ref float result) {
        // Get elapsed time since beginning
        float time_diff = cur_time - init_time;

        // Check whether tweening durtion is over
        if(time_diff >= duration) {
            // Duration is over - stop
            result = dest_val;
            return false;
        }

        // Update result value
        float diff = dest_val - init_val;
        float time_diff_prc = time_diff / duration;
        result = diff * time_diff_prc;

        // Duration not over - continue
        return true;
    }

    // -----------------------------------------------------------------------------------------------------------------
    public class CVector3Tween {
        public Vector3 m_result;

        public utils.CVector4b m_active_axis;
        private Vector3 m_dest;
        private Vector3 m_speed;
        private bool m_is_angle;

        // -------------------------------------------------------------------------------------------------------------
        public CVector3Tween() {
            m_active_axis = new utils.CVector4b(true);
        }

        // -------------------------------------------------------------------------------------------------------------
        public void Start(Vector3 dest, Vector3 speed, bool is_angle) {
            m_dest = dest;
            m_speed = speed;
            m_is_angle = is_angle;
            m_active_axis.Init(true);
        }

        // -------------------------------------------------------------------------------------------------------------
        public bool Update(Vector3 cur, float time_delta) {
            // Check whether tweening is over
            if(!m_active_axis.w) return false;

            // Tween only active coordinates
            if(m_is_angle) {
                if(m_active_axis.x) m_active_axis.x = utils.TweenAngleBySpeed(cur.x, m_dest.x, m_speed.x, time_delta, ref m_result.x);
                if(m_active_axis.y) m_active_axis.y = utils.TweenAngleBySpeed(cur.y, m_dest.y, m_speed.y, time_delta, ref m_result.y);
                if(m_active_axis.z) m_active_axis.z = utils.TweenAngleBySpeed(cur.z, m_dest.z, m_speed.z, time_delta, ref m_result.z);
            } else {
                if(m_active_axis.x) m_active_axis.x = utils.TweenBySpeed(cur.x, m_dest.x, m_speed.x, time_delta, ref m_result.x);
                if(m_active_axis.y) m_active_axis.y = utils.TweenBySpeed(cur.y, m_dest.y, m_speed.y, time_delta, ref m_result.y);
                if(m_active_axis.z) m_active_axis.z = utils.TweenBySpeed(cur.z, m_dest.z, m_speed.z, time_delta, ref m_result.z);
            }

            // If all axis are inactive then tweening is over
            m_active_axis.w = (m_active_axis.x || m_active_axis.y || m_active_axis.z);
            return true;
        }
    }

    // -----------------------------------------------------------------------------------------------------------------
    public class CLookAtTween {
        public Vector3 m_result;

        private utils.CVector4b m_active_axis;
        private Vector3 m_dest_pos;
        private float m_speed;

        // -------------------------------------------------------------------------------------------------------------
        public CLookAtTween() {
            m_active_axis = new utils.CVector4b(false);
        }

        // -------------------------------------------------------------------------------------------------------------
        public void Start(Vector3 dest_pos, float speed) {
            m_dest_pos = dest_pos;
            m_speed = speed;
            m_active_axis.Init(true);
        }

        // -------------------------------------------------------------------------------------------------------------
        public bool Update(Vector3 cur_pos, Vector3 cur_rot, float time_delta) {
            // Check whether tweening is over
            if(!m_active_axis.w) return false;

            // Convert destination vector to euler angles
            Vector3 dest_rot = m_dest_pos - cur_pos;
            dest_rot = Quaternion.LookRotation(dest_rot).eulerAngles;

            // Tween only active coordinates
            if(m_active_axis.x) m_active_axis.x = utils.TweenAngleBySpeed(cur_rot.x, dest_rot.x, m_speed, time_delta, ref m_result.x);
            if(m_active_axis.y) m_active_axis.y = utils.TweenAngleBySpeed(cur_rot.y, dest_rot.y, m_speed, time_delta, ref m_result.y);
            if(m_active_axis.z) m_active_axis.z = utils.TweenAngleBySpeed(cur_rot.z, dest_rot.z, m_speed, time_delta, ref m_result.z);

            // If all axis are inactive then tweening is over
            m_active_axis.w = (m_active_axis.x || m_active_axis.y || m_active_axis.z);
            return true;
        }
    }

    // -----------------------------------------------------------------------------------------------------------------
    public class CCountedOrderedMap {
        // -------------------------------------------------------------------------------------------------------------
        public List<object> m_order;    // Ordered list of elements (same element might be added more than once)
        public Hashtable m_map;         // Unique elements
        public Hashtable m_ref_cnt;     // Reference counters of elements in ordered list

        // -------------------------------------------------------------------------------------------------------------
        public CCountedOrderedMap() {
            m_order = new List<object>();
            m_map = new Hashtable();
            m_ref_cnt = new Hashtable();
        }

        // -------------------------------------------------------------------------------------------------------------
        public bool HasKey(object key) {
            return m_map.Contains(key);
        }

        // -------------------------------------------------------------------------------------------------------------
        public int GetOrderLen() {
            return m_order.Count;
        }

        // -------------------------------------------------------------------------------------------------------------
        public int GetKeyCnt() {
            return m_map.Count;
        }

        // -------------------------------------------------------------------------------------------------------------
        public int GetRefCnt(object key) {
            return (HasKey(key)) ? (int)m_ref_cnt[key] : 0;
        }

        // -------------------------------------------------------------------------------------------------------------
        public object GetValueByKey(object key) {
            utils.Assert(HasKey(key), "Failed to get COM value. Key not in map :: key={0}", key);
            return m_map[key];
        }

        // -------------------------------------------------------------------------------------------------------------
        public object GetValueByIdx(int idx) {
            utils.Assert(idx >= 0 && idx < GetOrderLen(), "Failed to get COM value. Wrong index :: idx={0} size={1}",
                idx, GetOrderLen());
            return GetValueByKey(m_order[idx]);
        }

        // -------------------------------------------------------------------------------------------------------------
        public object GetKeyByIdx(int idx) {
            utils.Assert(idx >= 0 && idx < GetOrderLen(), "Failed to get COM key. Wrong index :: idx={0} size={1}",
                idx, GetOrderLen());
            return m_order[idx];
        }

        // -------------------------------------------------------------------------------------------------------------
        public void Add(object key, object val) {
            int cnt = GetRefCnt(key);
            if(cnt == 0) {
                m_map.Add(key, val);
            }

            m_ref_cnt[key] = cnt + 1;
            m_order.Add(key);
        }

        // -------------------------------------------------------------------------------------------------------------
        public void RemoveByIdx(int idx) {
            // Get key by index
            object key = GetKeyByIdx(idx);

            // Get reference counter
            int cnt = GetRefCnt(key);
            utils.Assert(cnt > 0, "Failed to delete COM entry. Invalid reference counter :: idx={0} key={1}", idx, key);

            // Remove from order list
            m_order.RemoveAt(idx);

            // Last reference - remove
            if(cnt == 1) {
                m_map.Remove(key);
                m_ref_cnt.Remove(key);

            // Not last refence - decrement reference counter
            } else {
                m_ref_cnt[key] = cnt - 1;
            }
        }

        // -------------------------------------------------------------------------------------------------------------
        public void RemoveByKey(object key) {
            int cnt = GetRefCnt(key);
            while(m_order.Remove(key)) {    // XXX: Inefficient
                cnt--;
            }

            utils.Assert(cnt == 0, "Failed to delete COM entry by key. Invalid reference counter :: key={0} ref-cnt={1}",
                key, cnt);

            m_map.Remove(key);
            m_ref_cnt.Remove(key);
        }
    }
}
