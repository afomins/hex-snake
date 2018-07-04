// ---------------------------------------------------------------------------------------------------------------------
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

// ---------------------------------------------------------------------------------------------------------------------
public class CTile : CTag.CObject {
    // -----------------------------------------------------------------------------------------------------------------
    public enum ETag {
        T_CURRENT,      // Currently selected tile
        T_VISITED,      // Previously visited tiles
        T_DEST_PATH,    // Destination path
        T_DEFAULT,      // Default tile
        T_APPLE,        // Tile with apple
        T_BORDER,       // Border tile
        T_TAIL,         // Tile occupied by tail
        T_MAX
    }

    // -----------------------------------------------------------------------------------------------------------------
    public enum EDirection {
        DIR_1,          // 1:  right - up
        DIR_3,          // 3:  right
        DIR_5,          // 5:  right - down
        DIR_7,          // 7:  left - down
        DIR_9,          // 9:  left
        DIR_11,         // 11: left - up
        DIR_MAX
    }

    // -----------------------------------------------------------------------------------------------------------------
    public class CDirectionEx {
        // -------------------------------------------------------------------------------------------------------------
        public CTile.EDirection m_opposite, m_next, m_prev;
        public Vector2 m_pos;
        public float m_angle;

        // -------------------------------------------------------------------------------------------------------------
        public CDirectionEx(float angle, EDirection opposite, EDirection next, EDirection prev) {
            m_opposite = opposite;
            m_next = next;
            m_prev = prev;
            m_angle = angle;
            m_pos.Set((float)Math.Cos(angle), (float)Math.Sin(angle));
        }
    }

    // -----------------------------------------------------------------------------------------------------------------
    public static CDirectionEx[] DIR_EX = {
        //               angle   opposite           next               previous                  direction
        new CDirectionEx(030.0f, EDirection.DIR_7,  EDirection.DIR_3,  EDirection.DIR_11),    // 1:  right - up
        new CDirectionEx(090.0f, EDirection.DIR_9,  EDirection.DIR_5,  EDirection.DIR_1),     // 3:  right
        new CDirectionEx(150.0f, EDirection.DIR_11, EDirection.DIR_7,  EDirection.DIR_3),     // 5:  right - down
        new CDirectionEx(210.0f, EDirection.DIR_1,  EDirection.DIR_9,  EDirection.DIR_5),     // 7:  left - down
        new CDirectionEx(270.0f, EDirection.DIR_3,  EDirection.DIR_11, EDirection.DIR_7),     // 9:  left
        new CDirectionEx(330.0f, EDirection.DIR_5,  EDirection.DIR_1,  EDirection.DIR_9),     // 11: left - up
    };

    // -----------------------------------------------------------------------------------------------------------------
    public enum ERelPosition {
        RP_BEHIND,      // Target tile is behind current
        RP_FORWARD,     // Target tile is in fornt of current
        RP_CURRENT,     // Target tile is current
        RP_NONE         // Target tile is not on the same line as current
    }

    // -----------------------------------------------------------------------------------------------------------------
    private GameObject m_body_obj, m_apple_obj;
    private CTile[] m_neighbors;
    private float m_height;
    private Vector3 m_pos;
    private utils.CVector2i m_idx;

    // -----------------------------------------------------------------------------------------------------------------
    public CTile(int x, int z, Vector3 pos, GameObject body_obj) {
        m_neighbors = new CTile[(int)CTile.EDirection.DIR_MAX];
        m_idx = new utils.CVector2i(x, z);
        m_pos = pos;
        m_body_obj = body_obj;
        m_body_obj.transform.position = pos;
        SetTagName(body_obj.name);
    }

    // -----------------------------------------------------------------------------------------------------------------
    public CTile GetNeighbor(EDirection dir) {
        return m_neighbors[(int)dir];
    }

    // -----------------------------------------------------------------------------------------------------------------
    public float GetHeight() {
        return m_height;
    }

    // -----------------------------------------------------------------------------------------------------------------
    public Vector3 GetPosition() {
        return m_pos;
    }

    // -----------------------------------------------------------------------------------------------------------------
    public utils.CVector2i GetIdx() {
        return m_idx;
    }

    // -----------------------------------------------------------------------------------------------------------------
    public void SetMaterial(Material m) {
        m_body_obj.renderer.material = m;
    }

    // -----------------------------------------------------------------------------------------------------------------
    public void SetHeight(float height, float unit_size) {
        m_height = height;
        m_pos.y = height * unit_size;

        // Update mesh 
        m_body_obj.transform.localScale = new Vector3(1.0f, 1.0f, m_pos.y + 0.1f);
    }

    // -----------------------------------------------------------------------------------------------------------------
    public void SetNeighbor(EDirection dir, CTile neighbor) {
        m_neighbors[(int)dir] = neighbor;
    }

    // -----------------------------------------------------------------------------------------------------------------
    public void GetTilePosXZByOffset(CTile.EDirection dir, int offset, ref int x, ref int z) {
        // Convert negative offset to positive by inverting direction
        if(offset < 0) {
            offset = -offset;
            dir = CTile.DIR_EX[(int)dir].m_opposite;
        }

        // Handle positive offset
        int z_parity = utils.GetParity(m_idx.y);
        int off_parity = utils.GetParity(offset);
        int z_parity_inv = ((z_parity == 0) ? 1 : 0);
        int half_offset_positive = (offset + z_parity) / 2;
        int half_offset_negative = offset / 2 + ((off_parity == 1) ? z_parity_inv : 0);

        if(dir == CTile.EDirection.DIR_3) {          // 3: right
            x = m_idx.x + offset;
            z = m_idx.y;
        } else if(dir == CTile.EDirection.DIR_9) {   // 9: left
            x = m_idx.x - offset;
            z = m_idx.y;
        } else if(dir == CTile.EDirection.DIR_1) {   // 1: right - up
            x = m_idx.x + half_offset_positive;
            z = m_idx.y + offset;
        } else if(dir == CTile.EDirection.DIR_5) {   // 5: right - down
            x = m_idx.x + half_offset_positive;
            z = m_idx.y - offset;
        } else if(dir == CTile.EDirection.DIR_7) {   // 7: left - down
            x = m_idx.x - half_offset_negative;
            z = m_idx.y - offset;
        } else if(dir == CTile.EDirection.DIR_11) {  // 11: left - up
            x = m_idx.x - half_offset_negative;
            z = m_idx.y + offset;
        } else {                                     // ???: should not happen
            x = z = 0;
            utils.Assert(true, "Invalid direction");
        }
    }

    // -----------------------------------------------------------------------------------------------------------------
    public CTile.ERelPosition GetRelPosition(CTile target, CTile.EDirection dir) {
        // Easy case - target is reached
        CTile.ERelPosition rel_pos = CTile.ERelPosition.RP_NONE;
        if(target == this) {
            rel_pos = CTile.ERelPosition.RP_CURRENT;

        // Medium case - horizontal/vertical
        } else if(dir == CTile.EDirection.DIR_3) {           // 3: right
            if(target.m_idx.y == m_idx.y) {
                rel_pos = (target.m_idx.x > m_idx.x) ? 
                    CTile.ERelPosition.RP_FORWARD : 
                    CTile.ERelPosition.RP_BEHIND;
            }

        } else if(dir == CTile.EDirection.DIR_9) {           // 9: left
            if(target.m_idx.y == m_idx.y) {
                rel_pos = (target.m_idx.x < m_idx.x) ? 
                    CTile.ERelPosition.RP_FORWARD : 
                    CTile.ERelPosition.RP_BEHIND;
            }

        // Hard case - diagonal
        } else {
            // Diff to destination
            int z_diff = target.m_idx.y - m_idx.y;
            int x_diff = target.m_idx.x - m_idx.x;

            // Initial relative position is set to behind and offset is set to negative
            int offset = (z_diff < 0) ? z_diff : -z_diff;
            rel_pos = CTile.ERelPosition.RP_BEHIND;

            // Update relative position
            if(dir == CTile.EDirection.DIR_1) {              // 1: right - up
                if(x_diff >= 0 && z_diff > 0) {
                    rel_pos = CTile.ERelPosition.RP_FORWARD;
                }
            } else if(dir == CTile.EDirection.DIR_5) {       // 5: right - down
                if(x_diff >= 0 && z_diff < 0) {
                    rel_pos = CTile.ERelPosition.RP_FORWARD;
                }
            } else if(dir == CTile.EDirection.DIR_7) {       // 7: left - down
                if(x_diff <= 0 && z_diff < 0) {
                    rel_pos = CTile.ERelPosition.RP_FORWARD;
                }
            } else if(dir == CTile.EDirection.DIR_11) {      // 11: left - up
                if(x_diff <= 0 && z_diff > 0) {
                    rel_pos = CTile.ERelPosition.RP_FORWARD;
                }
            } else {
                utils.Assert(true, "Invalid diff");             // ???: should not happen
            }

            // If relative position was changed then offset should be inverted
            if(rel_pos == CTile.ERelPosition.RP_FORWARD) {
                offset = -offset;
            }

            // Find coordinates of correct tile with given offset
            int x = 0, z = 0;
            GetTilePosXZByOffset(dir, offset, ref x, ref z);

            // If correct tile is not our target tile then ignore
            if(x != target.m_idx.x || z != target.m_idx.y) {
                rel_pos = CTile.ERelPosition.RP_NONE;
            }
        }
        return rel_pos;
    }

    // -----------------------------------------------------------------------------------------------------------------
    public void SetApple(GameObject apple_obj) {
        utils.Assert(!HasApple(), "Failed to set apple. Apple already set.");
        utils.Log("Creating apple :: pos={0}:{1}", m_idx.x, m_idx.y);
        m_apple_obj = apple_obj;
        m_apple_obj.transform.position = new Vector3(m_pos.x, m_pos.y + 0.5f, m_pos.z);
    }

    // -----------------------------------------------------------------------------------------------------------------
    public GameObject UnsetApple() {
        utils.Assert(HasApple(), "Failed unset apple. Apple not set.");
        utils.Log("Deleting apple :: pos={0}:{1}", m_idx.x, m_idx.y);
        GameObject tmp = m_apple_obj;
        m_apple_obj = null;
        return tmp;
    }

    // -----------------------------------------------------------------------------------------------------------------
    public bool HasApple() {
        return (m_apple_obj != null);
    }
}
