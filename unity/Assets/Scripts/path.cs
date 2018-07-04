// ---------------------------------------------------------------------------------------------------------------------
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

// ---------------------------------------------------------------------------------------------------------------------
public class path : MonoBehaviour {
    // -----------------------------------------------------------------------------------------------------------------
    public class CVertexNode {
        // -------------------------------------------------------------------------------------------------------------
        private GameObject m_obj;
        private Vector3 m_pos;

        // -------------------------------------------------------------------------------------------------------------
        public CVertexNode(Vector3 pos, GameObject obj) {
            m_obj = obj;
            SetPos(pos);
        }

        // -------------------------------------------------------------------------------------------------------------
        public void SetPos(Vector3 pos) {
            m_pos = pos;
            if(m_obj != null) {
                m_obj.transform.position = pos;
            }
        }

        // -------------------------------------------------------------------------------------------------------------
        public Vector3 GetPos() {
            return m_pos;
        }

        // -------------------------------------------------------------------------------------------------------------
        public void Release(utils.CMesh mesh) {
            if(m_obj != null) {
                mesh.Destroy(m_obj);
            }
        }

        // -------------------------------------------------------------------------------------------------------------
        public float GetDistanceTo(CVertexNode v) {
            return utils.GetDistanceXYZ(m_pos.x, m_pos.y, m_pos.z, v.m_pos.x, v.m_pos.y, v.m_pos.z);
        }
    }

    // -----------------------------------------------------------------------------------------------------------------
    public class CEdgeNode {
        // -------------------------------------------------------------------------------------------------------------
        private CVertexNode m_v0;
        private CVertexNode m_v1;
        private float m_length;
        private Vector3 m_unit_vect;

        // -------------------------------------------------------------------------------------------------------------
        public CEdgeNode(CVertexNode v0, CVertexNode v1) {
            m_v0 = v0;
            m_v1 = v1;
            Update();
        }

        // -------------------------------------------------------------------------------------------------------------
        public void Update() {
            // Update edge length
            m_length = m_v0.GetDistanceTo(m_v1);

            // Update unit vector pointing from edge beginning to edge end
            m_unit_vect = m_v1.GetPos() - m_v0.GetPos();
            m_unit_vect.Normalize();
        }

        // -------------------------------------------------------------------------------------------------------------
        public float GetLength() {
            return m_length;
        }

        // -------------------------------------------------------------------------------------------------------------
        public CVertexNode GetVertex(int idx) {
            return (idx == 0) ? m_v0 : m_v1;
        }

        // -------------------------------------------------------------------------------------------------------------
        public Vector3 GetUnitVector() {
            return m_unit_vect;
        }
    }

    // -----------------------------------------------------------------------------------------------------------------
    // CONFIG
    [System.Serializable]
    public class CConfig : config.CConfigGeneral {
        // -------------------------------------------------------------------------------------------------------------
        public GameObject pref_obj;
        public float line_width;
        public Material line_material;
        public int max_vertices;

        // -------------------------------------------------------------------------------------------------------------
        public utils.CMesh mesh;
        public void Finalize() {
            mesh = new utils.CMesh(pref_obj);
        }
    }

    // -----------------------------------------------------------------------------------------------------------------
    // STATIC
    public static path Instantiate(path.CConfig cfg, string name) {
        path obj = utils.Instantiate(null, name).AddComponent<path>();

        obj.m_config = cfg;
        obj.m_vertices = new LinkedList<CVertexNode>();
        obj.m_edges = new LinkedList<CEdgeNode>();

        // Line renderer
        if(cfg.line_width > 0.01f) {
            GameObject lr_obj = new GameObject("line-renderer");
            obj.m_line_renderer = lr_obj.AddComponent<LineRenderer>();
            obj.m_line_renderer.SetWidth(cfg.line_width, cfg.line_width);
            obj.m_line_renderer.transform.parent = obj.transform;
            obj.m_line_renderer.material = cfg.line_material;
        }
        return obj;
    }

    // -----------------------------------------------------------------------------------------------------------------
    public path.CConfig m_config;

    private int m_max_vertices;
    private LineRenderer m_line_renderer;
    private LinkedList<CVertexNode> m_vertices;
    private LinkedList<CEdgeNode> m_edges;
    private LinkedListNode<CEdgeNode> m_search_it;
    private float m_search_offset;
    private int m_vertex_cnt;
    private bool m_increase_len;

    // -----------------------------------------------------------------------------------------------------------------
    private CVertexNode AddVertexNode(Vector3 pos) {
        string name = string.Format("vertex-{0}", m_vertex_cnt++);
        GameObject obj = m_config.mesh.Instantiate(name);

        if(obj) {
            obj.transform.parent = this.transform;
        }

        return m_vertices.AddFirst(new CVertexNode(pos, obj)).Value;
    }

    // -----------------------------------------------------------------------------------------------------------------
    private void ReleaseVertexNode(CVertexNode node) {
        node.Release(m_config.mesh);
    }

    // -----------------------------------------------------------------------------------------------------------------
    public void Reset() {
        // Clear edge list
        m_edges.Clear();

        // Release vertices
        for(LinkedListNode<CVertexNode> it = m_vertices.First; it != null; it = it.Next) {
            ReleaseVertexNode(it.Value);
        }

        // Clear vertex list
        m_vertices.Clear();

        // Clear line renderer
        if(m_line_renderer != null) {
            m_line_renderer.SetVertexCount(0);
        }
    }

    // -----------------------------------------------------------------------------------------------------------------
    public void AddVertex(Vector3 pos, bool finalize_head) {
        bool rebuild_line_renderer = false;

        // Add first vertex
        if(m_vertices.Count == 0) {
            CVertexNode v1 = AddVertexNode(pos);
            CVertexNode v0 = AddVertexNode(pos);
            m_edges.AddFirst(new CEdgeNode(v0, v1));
            rebuild_line_renderer = true;

        // Finalize head
        } else if(finalize_head) {
            CVertexNode v1 = m_vertices.First.Value;
            v1.SetPos(pos);
            CVertexNode v0 = AddVertexNode(pos);
            m_edges.AddFirst(new CEdgeNode(v0, v1));
            rebuild_line_renderer = true;

        // Update head
        } else {
            m_vertices.First.Value.SetPos(pos);
            m_edges.First.Value.Update();
            rebuild_line_renderer = false;
        }

        // Take care of max length
        if(m_vertices.Count > m_config.max_vertices) {
            if(m_increase_len) {
                m_increase_len = false;
                m_config.max_vertices++;
            } else {
                ReleaseVertexNode(m_vertices.Last.Value);
                m_vertices.RemoveLast();
                m_edges.RemoveLast();
            }
        }

        // Update line renderer
        if(m_line_renderer != null) {
            // Rebuild line renderer
            m_line_renderer.SetVertexCount(m_vertices.Count);
            if(rebuild_line_renderer) {
                int idx = 0;
                m_line_renderer.SetVertexCount(m_vertices.Count);

                // Build line renderer from edges
                for(LinkedListNode<CEdgeNode> it = m_edges.First; it != null; it = it.Next) {
                    CEdgeNode e = it.Value;

                    if(idx == 0) {
                        m_line_renderer.SetPosition(idx++, e.GetVertex(0).GetPos());
                        m_line_renderer.SetPosition(idx++, e.GetVertex(1).GetPos());
                    } else {
                        m_line_renderer.SetPosition(idx++, e.GetVertex(1).GetPos());
                    }
                }
            // Update only head
            } else {
                m_line_renderer.SetPosition(0, pos);
            }
        }
    }

    // -----------------------------------------------------------------------------------------------------------------
    public void ResetOffsetSearch() {
        m_search_it = m_edges.First;
        m_search_offset = 0.0f;
    }

    // -----------------------------------------------------------------------------------------------------------------
    public bool SearchNextOffset(float offset, ref Vector3 offset_pos) {
        // Start with with current edge and search fitting edge
        while(m_search_it != null) {
            CEdgeNode e = m_search_it.Value;
            float edge_len = e.GetLength();

            // Check whether offset is inside the boundaries of current edge
            if(offset > m_search_offset && 
               offset < m_search_offset + edge_len) {
                // Bingo
                float diff = offset - m_search_offset;
                offset_pos = e.GetVertex(0).GetPos() + e.GetUnitVector() * diff;
                return true;
            }

            // Continue with next edge
            m_search_offset += edge_len;
            m_search_it = m_search_it.Next;
        }

        // Not found
        return false;
    }

    // -----------------------------------------------------------------------------------------------------------------
    public void IncreasePath() {
        m_increase_len = true;
    }
}
