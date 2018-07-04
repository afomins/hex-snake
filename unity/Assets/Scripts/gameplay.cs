// ---------------------------------------------------------------------------------------------------------------------
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

// ---------------------------------------------------------------------------------------------------------------------
public class gameplay : MonoBehaviour {
    // -----------------------------------------------------------------------------------------------------------------
    // CONFIG
    [System.Serializable]
    public class CConfig : config.CConfigGeneral {
        // -------------------------------------------------------------------------------------------------------------
        public Vector2 spawn_pos;
        public CTile.EDirection spawn_dir;
        public int spawn_tail_size;
        public float y_offset;
        public int score;

        // -------------------------------------------------------------------------------------------------------------
        public path.CConfig path;
        public tail.CConfig tail;
        public compas.CConfig compas;
        public apple.CConfig apple;
        public level.CConfig level;
        public player.CConfig player;
        public rotating_camera.CConfig camera;

        // -------------------------------------------------------------------------------------------------------------
        public void Finalize() {
            path.Finalize();
            tail.Finalize();
            compas.Finalize();
            apple.Finalize();
            level.Finalize();
            player.Finalize();
            camera.Finalize();
        }
    }

    // -----------------------------------------------------------------------------------------------------------------
    // STATIC
    public static gameplay Instantiate(gameplay.CConfig cfg) {
        gameplay obj = utils.Instantiate(null, "gameplay").AddComponent<gameplay>();

        obj.m_player = player.Instantiate(cfg.player, "player");
        obj.m_camera = rotating_camera.Instantiate(cfg.camera, "camera", obj.m_player.gameObject);
        obj.m_level = level.Instantiate(cfg.level, "level");
        obj.m_path = path.Instantiate(cfg.path, "player-path");
        obj.m_tail = tail.Instantiate(cfg.tail, "player-tail", obj.m_path, obj.m_level);
        obj.m_compas = compas.Instantiate(cfg.compas, "compas");

        obj.m_config = cfg;
        obj.m_level_tags = obj.m_level.GetTagOwner();
        obj.SetPlayerPosition((int)cfg.spawn_pos.x, (int)cfg.spawn_pos.y, cfg.spawn_dir);

        for(int i = 0; i < cfg.spawn_tail_size; i++) {
            obj.m_tail.AddSegment();
        }

        return obj;
    }

    // -----------------------------------------------------------------------------------------------------------------
    // NON-STATIC
    public gameplay.CConfig m_config;

    private path m_path;
    private tail m_tail;
    private compas m_compas;
    private level m_level;
    private player m_player;
    private rotating_camera m_camera;

    private CTag.COwner m_level_tags;
    private CTile.EDirection m_direction;
    private CTile m_dest_tile, m_prev_tile, m_prev_frame_tile;

    // -----------------------------------------------------------------------------------------------------------------
    public void SetPlayerPosition(int tile_x, int tile_z, CTile.EDirection dir) {
        // Current tile
        CTile t = m_level.GetTileByPosXZ(tile_x, tile_z);
        utils.Assert(t != null, "No source tile :: pos={0}:{1}", tile_x, tile_z);

        // Initial position and rotation
        Vector3 pos = t.GetPosition();
        m_player.transform.position = new Vector3(pos.x, pos.y + 0.5f, pos.z);
        m_player.transform.eulerAngles = new Vector3(0.0f, CTile.DIR_EX[(int)m_direction].m_angle, 0.0f);

        // Destination
        SetPlayerDestination(t, dir);

        // Player trail
        m_path.Reset();
    }

    // -----------------------------------------------------------------------------------------------------------------
    public CTile.EDirection SetPlayerDestination(CTile cur_tile, CTile.EDirection dir) {
        CTile.EDirection dir_next = CTile.DIR_EX[(int)dir].m_next;
        CTile.EDirection dir_next_next = CTile.DIR_EX[(int)dir_next].m_next;
        CTile.EDirection dir_prev = CTile.DIR_EX[(int)dir].m_prev;
        CTile.EDirection dir_prev_prev = CTile.DIR_EX[(int)dir_prev].m_prev;
        CTile.EDirection dir_back = CTile.DIR_EX[(int)dir].m_opposite;
        CTile.EDirection[] dir_selection_order = { dir, dir_next, dir_prev, dir_next_next, dir_prev_prev, dir_back };

        CTile.EDirection final_dir = dir;
        foreach(CTile.EDirection d in dir_selection_order) {
            m_dest_tile = cur_tile.GetNeighbor(d);
            if(m_dest_tile != null && 
               !m_level_tags.IsTagSet(m_dest_tile, (int)CTile.ETag.T_BORDER)) {
                final_dir = d;
                break;
            }
        }

        utils.Assert(m_dest_tile != null, "Destination tile is null");

        m_direction = dir;
        m_player.SetDestination(m_dest_tile.GetPosition());

//        utils.Log("Set player destination :: cur={0}:{1} dest={2}:{3} dir={4}", 
//            cur_tile.m_x, cur_tile.m_z, m_dest_tile.m_x, m_dest_tile.m_z, dir);
        return final_dir;
    }

    // -----------------------------------------------------------------------------------------------------------------
    public CTile CreateApple() {
        // Find random default-only tile
        CTile t = null;
        while(t == null) {
            t = m_level.GetRandomTile();
            if(!m_level_tags.IsOnlyTag(t, (int)CTile.ETag.T_DEFAULT)) {
                t = null;
            }
        }

        // Set APPLE tag
        m_level_tags.SetTag(t, (int)CTile.ETag.T_APPLE);
        return t;
    }

    // -----------------------------------------------------------------------------------------------------------------
    private void Update() {
        // Turn right
        if(Input.GetKeyDown("right")) {
            m_direction = CTile.DIR_EX[(int)m_direction].m_next;

        // Turn left
        } else if(Input.GetKeyDown("left")) {
            m_direction = CTile.DIR_EX[(int)m_direction].m_prev;
        }

        // Current player position & underlying tile
        Vector3 cur_pos = m_player.transform.position;
        CTile cur_tile = m_level.GetTileByCoord(cur_pos.x, cur_pos.z);

        // Update compas
        m_compas.SetPosition(cur_pos);

        // Active tile is changed
        if(cur_tile != m_prev_frame_tile && m_prev_frame_tile != null) {
            // Update current and previously visited tiles
            m_prev_tile = m_prev_frame_tile;
            m_level_tags.SetTag(m_prev_tile, (int)CTile.ETag.T_VISITED);
            m_level_tags.SetTag(cur_tile, (int)CTile.ETag.T_CURRENT);

            // Update destination path
            m_level_tags.ClearTag((int)CTile.ETag.T_DEST_PATH);
            CTile t_glow = cur_tile;
            while(true) {
                t_glow = t_glow.GetNeighbor(m_direction);
                if(t_glow == null ||
                   !m_level_tags.SetTag(t_glow, (int)CTile.ETag.T_DEST_PATH)) {
                    break;
                }
            }

            // Update trail - finalize border vertex between tiles
            Vector3 tile_border = m_prev_frame_tile.GetPosition() + (cur_tile.GetPosition() - m_prev_frame_tile.GetPosition()) / 2;
            tile_border.y += 0.5f;
            m_path.AddVertex(tile_border, true);

        // Active tile was not changed
        } else {
            // Update trail - update head vertex
            m_path.AddVertex(cur_pos, false);
        }

        // Destination tile is reached
        CTile.ERelPosition rel_pos = cur_tile.GetRelPosition(m_dest_tile, m_direction);
        if(rel_pos == CTile.ERelPosition.RP_CURRENT ||
           rel_pos == CTile.ERelPosition.RP_BEHIND ||
           rel_pos == CTile.ERelPosition.RP_NONE) {
            m_direction = SetPlayerDestination(cur_tile, m_direction);
            m_prev_frame_tile = cur_tile;
        }

        // Save tile of previous frame
        m_prev_frame_tile = cur_tile;

        // Eat apple
        if(m_level_tags.IsTagSet(cur_tile, (int)CTile.ETag.T_APPLE)) {
            m_level_tags.UnsetTag(cur_tile, (int)CTile.ETag.T_APPLE);
            m_tail.AddSegment();
            m_config.score++;
        }

        // Create new apple
        if(m_level_tags.GetTagCnt((int)CTile.ETag.T_APPLE) == 0) {
            CTile t = CreateApple();
            m_compas.SetDestination(t.GetPosition());
        }

        // Apply level changeset
        Hashtable changeset = m_level_tags.GetChangeset();
        if(changeset.Count > 0) {
            // Update all tiles in changeset
            foreach(DictionaryEntry entry in changeset) {
                CTile tile = (CTile)entry.Key;
                UpdateLevelMaterial(tile);
                UpdateLevelGameplay(tile);
            }

            // Clear changeset
            changeset.Clear();
        }
    }

    // -----------------------------------------------------------------------------------------------------------------
    public void UpdateLevelMaterial(CTile t) {
        // Select proper material
        Material m = null;
//        if(m_level_tags.IsTagSet(t, (int)CTile.ETag.T_CURRENT))     m = m_level.GetSelectionMaterial(1); else
//        if(m_level_tags.IsTagSet(t, (int)CTile.ETag.T_TAIL))        m = m_level.GetSelectionMaterial(0); else
//        if(m_level_tags.IsTagSet(t, (int)CTile.ETag.T_VISITED))     m = m_level.GetSelectionMaterial(0); else
        if(m_level_tags.IsTagSet(t, (int)CTile.ETag.T_DEST_PATH))   m = m_level.GetHeightmapMaterial(t.GetHeight(), true); else
        if(m_level_tags.IsTagSet(t, (int)CTile.ETag.T_DEFAULT))     m = m_level.GetHeightmapMaterial(t.GetHeight(), false);

        // Update material
        if(m) {
            t.SetMaterial(m);
        }
    }

    // -----------------------------------------------------------------------------------------------------------------
    private void UpdateLevelGameplay(CTile t) {
        if(m_level_tags.IsTagSet(t, (int)CTile.ETag.T_APPLE)) {
            if(!t.HasApple()) {
                t.SetApple(apple.Instantiate(m_config.apple).gameObject);
            }

        } else {
            if(t.HasApple()) {
                GameObject apple_obj = t.UnsetApple();
                m_config.apple.mesh.Destroy(apple_obj);
            }
        }
    }
}
