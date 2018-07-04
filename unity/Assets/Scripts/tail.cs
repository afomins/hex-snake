// ---------------------------------------------------------------------------------------------------------------------
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

// ---------------------------------------------------------------------------------------------------------------------
public class tail : MonoBehaviour {
    // -----------------------------------------------------------------------------------------------------------------
    // CONFIG
    [System.Serializable]
    public class CConfig : config.CConfigGeneral {
        // -------------------------------------------------------------------------------------------------------------
        public GameObject pref_obj;
        public float segment_pad;

        // -------------------------------------------------------------------------------------------------------------
        public utils.CMesh mesh;
        public void Finalize() {
            mesh = new utils.CMesh(pref_obj);
        }
    }

    // -----------------------------------------------------------------------------------------------------------------
    // STATIC
    public static tail Instantiate(tail.CConfig config, string name, path path_obj, level level_obj) {
        tail obj = utils.Instantiate(null, name).AddComponent<tail>();

        obj.m_config = config;
        obj.m_level = level_obj;
        obj.m_level_tags = level_obj.GetTagOwner();
        obj.m_path = path_obj;
        obj.m_segment_len = config.mesh.GetBounds().z;
        obj.m_segments = new LinkedList<GameObject>();

        return obj;
    }

    // -----------------------------------------------------------------------------------------------------------------
    // NON-STATIC
    private tail.CConfig m_config;

    private path m_path;
    private int m_segment_cnt;
    private float m_segment_len;
    private LinkedList<GameObject> m_segments;
    private CTag.COwner m_level_tags;
    private level m_level;

    // -----------------------------------------------------------------------------------------------------------------
    public void AddSegment() {
        string name = string.Format("seg-{0}", m_segment_cnt++);
        GameObject seg = m_config.mesh.Instantiate(name);
        seg.transform.parent = this.transform;
        m_segments.AddLast(seg);
    }

    // -----------------------------------------------------------------------------------------------------------------
    public void Reset() {
        // Release segmetns
        for(LinkedListNode<GameObject> it = m_segments.First; it != null; it = it.Next) {
            m_config.mesh.Destroy(it.Value);
        }

        // Clear list
        m_segments.Clear();
    }

    // -----------------------------------------------------------------------------------------------------------------
    private void Update() {
        // Remove TILE tag from level
        m_level_tags.ClearTag((int)CTile.ETag.T_TAIL);

        // Reset path search
        m_path.ResetOffsetSearch();

        float offset = m_segment_len + m_config.segment_pad;
        Vector3 offset_pos = new Vector3();
        bool eol_reached = false;
        for(LinkedListNode<GameObject> it = m_segments.First; it != null; it = it.Next) {
            // Search for next segment offset position
            if(!eol_reached) {
                eol_reached = !m_path.SearchNextOffset(offset, ref offset_pos);
            }

            // Update segment according to offset
            GameObject segment = it.Value;
            if(!eol_reached) {
                offset += (m_segment_len + m_config.segment_pad);
                segment.transform.position = offset_pos;

                CTile t = m_level.GetTileByCoord(offset_pos.x, offset_pos.z);
                m_level_tags.SetTag(t, (int)CTile.ETag.T_TAIL);


            // Ignore segment if EOL is reached
            } else {
                 segment.transform.position = new Vector3();
            }
        }

        // Increase path if EOL was reached
        if(eol_reached) {
            m_path.IncreasePath();
        }
    }
}
