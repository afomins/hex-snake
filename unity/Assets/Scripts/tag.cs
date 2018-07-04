// ---------------------------------------------------------------------------------------------------------------------
using UnityEngine;
using System.Collections;
using System;

// ---------------------------------------------------------------------------------------------------------------------
public class CTag {
    // -----------------------------------------------------------------------------------------------------------------
    public class COwner {
        // -------------------------------------------------------------------------------------------------------------
        private CTag[] m_tags;
        private Hashtable m_changeset;

        // -------------------------------------------------------------------------------------------------------------
        public COwner(int tag_cnt) {
            m_changeset = new Hashtable();
            m_tags = new CTag[tag_cnt];
        }

        // -------------------------------------------------------------------------------------------------------------
        public void InitTag(int tag_idx, string name, int limit = 0, ELimitPolicy limit_policy = ELimitPolicy.LP_QUEUE) {
            m_tags[tag_idx] = new CTag(name, m_changeset, limit, limit_policy);
        }

        // -------------------------------------------------------------------------------------------------------------
        public CTag GetTag(int tag_idx) {
            return m_tags[tag_idx];
        }

        // -------------------------------------------------------------------------------------------------------------
        public bool SetTag(CTag.CObject obj, int tag_idx) {
            return GetTag(tag_idx).Add(obj);
        }

        // -------------------------------------------------------------------------------------------------------------
        public void UnsetTag(CTag.CObject obj, int tag_idx) {
            GetTag(tag_idx).Remove(obj);
        }

        // -------------------------------------------------------------------------------------------------------------
        public void ClearTag(int tag_idx) {
            GetTag(tag_idx).Clear();
        }

        // -------------------------------------------------------------------------------------------------------------
        public bool IsTagSet(CTag.CObject obj, int tag_idx) {
            return obj.IsTagSet(GetTag(tag_idx));
        }

        // -------------------------------------------------------------------------------------------------------------
        public bool IsOnlyTag(CTag.CObject obj, int tag_idx) {
            return obj.IsOnlyTag(GetTag(tag_idx));
        }

        // -------------------------------------------------------------------------------------------------------------
        public int GetTagCnt(int tag_idx) {
            return GetTag(tag_idx).GetListLen();
        }

        // -------------------------------------------------------------------------------------------------------------
        public Hashtable GetChangeset() {
            return m_changeset;
        }

        // -------------------------------------------------------------------------------------------------------------
        public void ClearChangeset() {
            m_changeset.Clear();
        }
    }

    // -----------------------------------------------------------------------------------------------------------------
    public class CObject {
        // -------------------------------------------------------------------------------------------------------------
        private string m_name;
        private Hashtable m_tags;

        // -------------------------------------------------------------------------------------------------------------
        public CObject(string name = "") {
            m_name = name;
            m_tags = new Hashtable();
        }

        // -------------------------------------------------------------------------------------------------------------
        public void SetTagName(string name) {
            m_name = name;
        }

        // -------------------------------------------------------------------------------------------------------------
        public string GetTagName() {
            return m_name;
        }

        // -------------------------------------------------------------------------------------------------------------
        public bool IsTagSet(CTag tag) {
            return m_tags.Contains(tag);
        }

        // -------------------------------------------------------------------------------------------------------------
        public void SetTag(CTag tag) {
            utils.Assert(!IsTagSet(tag), "Failed to set tag. Tag is already set :: tag={0} obj={1}", tag.GetName(), m_name);
            m_tags.Add(tag, null);
        }

        // -------------------------------------------------------------------------------------------------------------
        public void UnsetTag(CTag tag) {
            utils.Assert(IsTagSet(tag), "Failed to unset tag. Tag is not set :: tag={0} obj={1}", tag.GetName(), m_name);
            m_tags.Remove(tag);
        }

        // -------------------------------------------------------------------------------------------------------------
        public bool IsOnlyTag(CTag tag) {
            return (m_tags.Count == 1 && IsTagSet(tag));
        }
    }

    // -----------------------------------------------------------------------------------------------------------------
    public enum ELimitPolicy {
        LP_QUEUE,   // Pop old and add new when limit is reached
        LP_DENY     // Do not add new when limit is reached
    }

    // -----------------------------------------------------------------------------------------------------------------
    private string m_name;
    private int m_limit;
    private ELimitPolicy m_limit_policy;
    private utils.CCountedOrderedMap m_objects;
    private Hashtable m_changeset;

    // -----------------------------------------------------------------------------------------------------------------
    public CTag(string name, Hashtable changeset, int limit, ELimitPolicy limit_policy) {
        m_name = name;
        m_limit = limit;
        m_limit_policy = limit_policy;
        m_objects = new utils.CCountedOrderedMap();
        m_changeset = changeset;
    }

    // -----------------------------------------------------------------------------------------------------------------
    public string GetName() {
        return m_name;
    }

    // -----------------------------------------------------------------------------------------------------------------
    public CTag.CObject GetByIdx(int idx) {
        return (CTag.CObject)m_objects.GetKeyByIdx(idx);
    }

    // -----------------------------------------------------------------------------------------------------------------
    public bool Add(CTag.CObject t) {
        if(m_limit > 0 && m_limit == m_objects.GetOrderLen()) {
            // Limit is reached
            if(m_limit_policy == ELimitPolicy.LP_DENY) {
                // Deny new objects
                return false;
            } else {
                // Remove first object
                RemoveByIdx(0);
            }
        }

        // Set tag if it is not set yet
        int ref_cnt = m_objects.GetRefCnt(t);
        if(!t.IsTagSet(this)) {
            utils.Assert(ref_cnt == 0, "Failed to add tagged object. Ref-cnt not zero:: tag={0} obj={1} ref-cnt={2}",
                m_name, t.GetTagName(), ref_cnt);

            t.SetTag(this);
        } else {
            utils.Assert(ref_cnt > 0, "Failed to add tagged object. Ref-cnt is zero :: tag={0} obj={1} ref-cnt={2}",
                m_name, t.GetTagName(), ref_cnt);
        }

        // Add new object to list
        m_objects.Add(t, null);
        AddToChangeset(t);
        return true;
    }

    // -----------------------------------------------------------------------------------------------------------------
    public void RemoveByIdx(int idx) {
        CTag.CObject t = GetByIdx(idx);
        if(m_objects.GetRefCnt(t) == 1) {
            t.UnsetTag(this);
        }
        m_objects.RemoveByIdx(idx);
        AddToChangeset(t);
    }

    // -----------------------------------------------------------------------------------------------------------------
    public void Remove(CTag.CObject t) {
        t.UnsetTag(this);
        m_objects.RemoveByKey(t);
        AddToChangeset(t);
    }

    // -----------------------------------------------------------------------------------------------------------------
    public void Clear() {
        while(m_objects.GetOrderLen() > 0) {
            RemoveByIdx(0);
        }
    }

    // -----------------------------------------------------------------------------------------------------------------
    public int GetListLen() {
        return m_objects.GetOrderLen();
    }

    // -----------------------------------------------------------------------------------------------------------------
    private void AddToChangeset(CTag.CObject t) {
        m_changeset[t] = null;
    }
}
