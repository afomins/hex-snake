// ---------------------------------------------------------------------------------------------------------------------
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

// ---------------------------------------------------------------------------------------------------------------------
public class level : MonoBehaviour {
    // -----------------------------------------------------------------------------------------------------------------
    // CONFIG
    [System.Serializable]
    public class CConfig : config.CConfigGeneral {
        // -------------------------------------------------------------------------------------------------------------
        public Color color_start;
        public Color color_end;
        public int color_cnt;
        public Texture2D heightmap;
        public GameObject tile_pref_obj;
        public float tile_gap_size;
        public float tile_unit_size;

        // -------------------------------------------------------------------------------------------------------------
        public utils.CMesh tile_mesh;
        public void Finalize() {
            tile_mesh = new utils.CMesh(tile_pref_obj);
        }
    }

    // -----------------------------------------------------------------------------------------------------------------
    // STATIC
    public static level Instantiate(level.CConfig cfg, string name) {
        level obj = utils.Instantiate(null, name).AddComponent<level>();

        obj.m_config = cfg;
        obj.BuildMaterials();
        obj.BuildTiles();

        return obj;
    }

    // -----------------------------------------------------------------------------------------------------------------
    // NON-STATIC
    public level.CConfig m_config;

    private CTile[,] m_tiles;
    private int m_width, m_height;
    private float m_tile_width, m_tile_width_50;
    private float m_tile_height, m_tile_height_75, m_tile_height_50, m_tile_height_25;

    private Material[] m_materials;
    private Material[] m_materials_glow;
    private Material[] m_materials_selected;

    private CTag.COwner m_tag_owner;

    // -----------------------------------------------------------------------------------------------------------------
    private void BuildMaterials() {
        Color col_start = m_config.color_start;
        Color col_end = m_config.color_end;
        int col_cnt = m_config.color_cnt;
        Shader sh = Shader.Find("Specular");

        // Heightmap materials
        Color diff = col_end - col_start;
        m_materials = new Material[col_cnt];
        m_materials_glow = new Material[col_cnt];
        for(int i = 0; i < col_cnt; i++) {
            // General material
            Material m = m_materials[i] = new Material(sh);
            m.color = (col_start + diff * ((i + 1.0f) / col_cnt));

            // Glow material
            Material mg = m_materials_glow[i] = new Material(sh);
            mg.color = m.color * 1.3f;
        }

        // Selection materials
        Color[] selection_color = {Color.green, Color.red, Color.blue, Color.white, Color.yellow};
        m_materials_selected = new Material[selection_color.Length];
        for(int i = 0; i < selection_color.Length; i++) {
            Material ms = m_materials_selected[i] = new Material(sh);
            ms.color = selection_color[i];
        }
    }

    // -----------------------------------------------------------------------------------------------------------------
    public Material GetHeightmapMaterial(float height, bool glow) {
        utils.Assert(height >= 0.0f && height <= 1.0f, 
            "Failed to get heightmap material :: height={0:F2}", height);

        int idx = (int)(height * (float)m_materials.Length);
        if(idx >= m_materials.Length) idx = m_materials.Length - 1;
        return glow ? m_materials_glow[idx] : m_materials[idx];
    }

    // -----------------------------------------------------------------------------------------------------------------
    public Material GetSelectionMaterial(int idx) {
        utils.Assert(idx >= 0 && idx < m_materials_selected.Length, 
            "Failed to get selection material :: idx={0} size={1}", idx, m_materials_selected.Length);

        return m_materials_selected[idx];
    }

    // -----------------------------------------------------------------------------------------------------------------
    private CTile CreateTile(int x, int z, Texture2D heightmap) {
        float height = heightmap.GetPixel(x, z).grayscale;
        Vector3 pos = GetTilePosXZ(x, z);

        GameObject mesh_obj = m_config.tile_mesh.Instantiate(string.Format("tile-{0}-{1}", x, z));
        CTile t = m_tiles[x, z] = new CTile(x, z, pos, mesh_obj);
        mesh_obj.transform.parent = this.transform;

        t.SetHeight(height, m_config.tile_unit_size);
        m_tag_owner.SetTag(t, (int)CTile.ETag.T_DEFAULT);
        return t;
    }

    // -----------------------------------------------------------------------------------------------------------------
    private void BuildTiles() {
        Texture2D heightmap = m_config.heightmap;
        float tile_gap = m_config.tile_gap_size;
        float tile_unit = m_config.tile_unit_size;
        float tw = m_config.tile_mesh.GetBounds().x;
        float th = m_config.tile_mesh.GetBounds().z;

        // Tile width
        m_tile_width = tw + tile_gap;
        m_tile_width_50 = m_tile_width * 0.5f;

        // Tile height
        m_tile_height = th + tile_gap;
        m_tile_height_75 = m_tile_height * 0.75f;
        m_tile_height_50 = m_tile_height * 0.5f;
        m_tile_height_25 = m_tile_height * 0.25f;

        // Level size
        m_width = heightmap.width;
        m_height = heightmap.height;

        utils.Log("Tile :: width={0:F2} height={1:F2} gap={2:F2} unit={3:F2}", tw, th, tile_gap, tile_unit);
        utils.Log("Map :: width={0} height={1}", heightmap.width, heightmap.height);

        // Init tags
        m_tag_owner = new CTag.COwner((int)CTile.ETag.T_MAX);
        m_tag_owner.InitTag((int)CTile.ETag.T_CURRENT, "current", 1, CTag.ELimitPolicy.LP_QUEUE);
        m_tag_owner.InitTag((int)CTile.ETag.T_VISITED, "visited", 10, CTag.ELimitPolicy.LP_QUEUE);
        m_tag_owner.InitTag((int)CTile.ETag.T_DEST_PATH, "dest-path", 25, CTag.ELimitPolicy.LP_DENY);
        m_tag_owner.InitTag((int)CTile.ETag.T_DEFAULT, "default");
        m_tag_owner.InitTag((int)CTile.ETag.T_APPLE, "apple");
        m_tag_owner.InitTag((int)CTile.ETag.T_BORDER, "border");
        m_tag_owner.InitTag((int)CTile.ETag.T_TAIL, "tail");

        // Build tiles
        m_tiles = new CTile[m_width, m_height];
        for(int z = 0; z < m_height; z++) {
            for(int x = 0; x < m_width; x++) {
                CreateTile(x, z, heightmap);
            }
        }

        // Link together neightbor tiles
        for(int z = 0; z < m_height; z++) {
            for(int x = 0; x < m_width; x++) {
                CTile t = m_tiles[x, z];
                for(int idx = (int)CTile.EDirection.DIR_1; idx < (int)CTile.EDirection.DIR_MAX; idx++) {
                    CTile.EDirection dir = (CTile.EDirection)idx;
                    t.SetNeighbor(dir, GetTileByOffset(t, dir, 1));
                }
            }
        }

        // Create vertical border
        for(int z = 0; z < m_height; z++) {
            m_tag_owner.SetTag(m_tiles[0, z], (int)CTile.ETag.T_BORDER);
            m_tag_owner.SetTag(m_tiles[m_width - 1, z], (int)CTile.ETag.T_BORDER);
        }

        // Create horizontal border
        for(int x = 0; x < m_height; x++) {
            m_tag_owner.SetTag(m_tiles[x, 0], (int)CTile.ETag.T_BORDER);
            m_tag_owner.SetTag(m_tiles[x, m_height - 1], (int)CTile.ETag.T_BORDER);
        }
    }

    // -----------------------------------------------------------------------------------------------------------------
    private Vector3 GetTilePosXZ(int x, int z) {
        return new Vector3(
            x * m_tile_width + utils.GetParity(z) * m_tile_width_50 + m_tile_width_50,
            0.0f,
            z * m_tile_height_75 + m_tile_height_50);
    }

    // -----------------------------------------------------------------------------------------------------------------
    public CTile GetTileByPosXZ(int x, int z) {
        if(x < 0 || z < 0 || x >= m_width || z >= m_height) return null;
        return m_tiles[x, z];
    }

    // -----------------------------------------------------------------------------------------------------------------
    public CTile GetTileByCoord(float x, float z) {
        // Primary candidate
        int row_idx = (int)(z / m_tile_height_75);
        int row_parity = utils.GetParity(row_idx);
        float col_offset = (x - row_parity * m_tile_width_50);
        int col_idx = (int)(col_offset / m_tile_width);

        // Special case for negative columns
        if(col_offset < 0.0f) col_idx--;

        // Position of primary candidate
        Vector3 pos = GetTilePosXZ(col_idx, row_idx);

        float distance = utils.GetDistanceXZ(x, z, pos.x, pos.z);

        // Alternative candidate exists if XZ is overlapped by bounding box of another tile
        float row_low_bound = row_idx * m_tile_height_75;
        if(z < row_low_bound + m_tile_height_25) {
            // Find alternative candidate
            int row_alt_idx = row_idx - 1;
            int col_alt_idx = 0;

            if(row_parity == 1) { // Odd row
                col_alt_idx = (x < pos.x) ? col_idx : col_idx + 1;
            } else { // Even row
                col_alt_idx = (x < pos.x) ? col_idx - 1: col_idx;
            }

            // Position of alternative candidate
            Vector3 pos_alt = GetTilePosXZ(col_alt_idx, row_alt_idx);

            // Select canditate that has less distance to XZ coordinate
            float dist_alt = utils.GetDistanceXZ(x, z, pos_alt.x, pos_alt.z);
            if(dist_alt < distance) {
                row_idx = row_alt_idx;
                col_idx = col_alt_idx;
            }
        }
        return GetTileByPosXZ(col_idx, row_idx);
    }

    // -----------------------------------------------------------------------------------------------------------------
    public CTile GetTileByOffset(CTile t, CTile.EDirection dir, int offset) {
        int x = 0, z = 0;
        t.GetTilePosXZByOffset(dir, offset, ref x, ref z);
        return GetTileByPosXZ(x, z);
    }

    // -----------------------------------------------------------------------------------------------------------------
    public CTag.COwner GetTagOwner() {
        return m_tag_owner;
    }

    // -----------------------------------------------------------------------------------------------------------------
    public CTile GetRandomTile(int border_size = 2) {
        int x = UnityEngine.Random.Range(border_size, m_width - border_size - 1);
        int z = UnityEngine.Random.Range(border_size, m_height - border_size - 1);
        return m_tiles[x, z];
    }
}
