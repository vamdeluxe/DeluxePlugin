using System;
using System.Linq;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;


namespace DeluxePlugin
{
    public class JointMorphControl : MVRScript
    {
        public override void Init()
        {
            try
            {
                //List<DAZBone> bones = containingAtom.GetStorableIDs()
                //.Where((id) =>
                //{
                //    DAZBone bone = containingAtom.GetStorableByID(id) as DAZBone;
                //    if (bone == null)
                //    {
                //        return false;
                //    }
                //    return true;
                //})
                //.ToList()
                //.Select((id) =>
                //{
                //    return containingAtom.GetStorableByID(id) as DAZBone;
                //})
                //.ToList();

            }
            catch (Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }
        }
    }
}
