using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using System.Linq;

namespace DeluxePlugin.Dollmaster
{
    public class Montage
    {
        public string name;
        public JSONArray montageJSON;
        public List<JSONClass> poses = new List<JSONClass>();

        public Montage(string name, JSONArray montageJSON)
        {
            this.name = name;
            this.montageJSON = montageJSON;
        }

        public JSONClass GetJSON()
        {
            JSONClass node = new JSONClass();
            node["name"] = name;
            node["positions"] = montageJSON;
            JSONArray poseArray = new JSONArray();
            poses.ForEach((poseJSON) =>
            {
                poseArray.Add(poseJSON);
            });
            node["poses"] = poseArray;
            return node;
        }

        public void Apply()
        {
            for(int i=0; i<montageJSON.Count; i++)
            {
                JSONClass atomJSON = montageJSON[i].AsObject;
                Atom foundAtom = SuperController.singleton.GetAtomByUid(atomJSON["id"]);

                if (foundAtom.GetStorableByID("AnimationPattern") != null){
                    AnimationPattern ap = foundAtom.GetStorableByID("AnimationPattern") as AnimationPattern;
                    ap.SyncStepNames();
                    ap.autoSyncStepNamesJSON.SetVal(false);
                }

                if (foundAtom == null)
                {
                    SuperController.LogError("Montage referenced an atom that doesn't exist " + atomJSON["id"]);
                    return;
                }

                if(atomJSON["id"].Value.Contains("Doll Control"))
                {
                    continue;
                }

                foundAtom.PreRestore();
                foundAtom.RestoreTransform(atomJSON);
                foundAtom.Restore(atomJSON, restorePhysical: true, restoreAppearance: false, restoreCore: false, presetAtoms: montageJSON);
                foundAtom.LateRestore(atomJSON, restorePhysical: true, restoreAppearance: false, restoreCore: false);
                foundAtom.PostRestore();
                SuperController.singleton.PauseSimulation(5, "Loading Montage");
            }
        }

        public void AddPose(JSONClass poseJSON)
        {
            poses.Add(poseJSON);
        }

        public List<string> GetPoseNames()
        {
            int index = 0;
            return poses.Select((pose) =>
            {
                index++;
                return "Pose " + index;
            }).ToList();
        }
    }
}
