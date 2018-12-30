using UnityEngine;
using System.Collections.Generic;

namespace DeluxePlugin.PoseToPose
{
    public class StorableStringList
    {
        const int maxCount = 100;
        public List<JSONStorableString> storables = new List<JSONStorableString>();
        public StorableStringList(MVRScript script, string prefix)
        {
            for (int i = 0; i < maxCount; i++)
            {
                JSONStorableString store = new JSONStorableString(prefix + i, "");
                storables.Add(store);
                script.RegisterString(store);
            }
        }

        public JSONStorableString GetNext()
        {
            int nextIndex = FindNextOpen();
            if (nextIndex < 0)
            {
                Debug.LogWarning("Ran out of space in storable string list");
                return null;
            }
            JSONStorableString next = storables[nextIndex];
            return next;
        }

        int FindNextOpen()
        {
            for (int i = 0; i < storables.Count; i++)
            {
                if (storables[i].val == storables[i].defaultVal)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
