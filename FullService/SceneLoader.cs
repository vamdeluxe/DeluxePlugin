using System;
using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;


namespace DeluxePlugin.FullService
{
    public class SceneLoader
    {
        JSONClass reference = null;
        List<Atom> sceneOnlyAtoms = new List<Atom>();
        Atom parentAtom;

        public SceneLoader(string referenceName, Atom parentAtom)
        {
            reference = LoadSceneJSONFromName(referenceName);
            this.parentAtom = parentAtom;
        }

        JSONClass LoadSceneJSONFromName(string sceneName)
        {
            string fileName = sceneName + ".json";
            string normalizedPath = SuperController.singleton.NormalizeLoadPath("./" + fileName);

            //  have to normalize twice...
            string absPath = SuperController.singleton.ReadFileIntoString(SuperController.singleton.NormalizeLoadPath(normalizedPath));

            JSONClass scene = JSON.Parse(absPath) as JSONClass;
            return scene;
        }

        public void LoadSequence(string sceneName)
        {
            RemoveSceneOnlyAtoms();

            SuperController.singleton.PauseSimulation(5, "Loading " + sceneName);

            JSONClass scene = LoadSceneJSONFromName(sceneName);

            //  copy poses for everything not in reference scene
            GetOverlaps(scene).ForEach((branchAtomJSON)=>
            {
                string id = branchAtomJSON["id"].Value;
                Atom atom = SuperController.singleton.GetAtomByUid(id);
                if (atom == null)
                {
                    return;
                }

                if (atom == parentAtom)
                {
                    return;
                }

                atom.PreRestore();
                atom.Restore(branchAtomJSON, true, false, true);
                atom.LateRestore(branchAtomJSON, true, false, true);
                atom.PostRestore();

            });

            SuperController.singleton.StartCoroutine(CloneAtoms(GetBranchOnly(scene)));

        }

        public IEnumerator CloneAtoms(List<JSONClass> atomJSON)
        {
            for(int i=0; i<atomJSON.Count; i++)
            {
                JSONClass atomSource = atomJSON[i];
                yield return CloneAtom(atomSource);
            }
        }

        public IEnumerator CloneAtom(JSONClass atomJSON)
        {
            string uid = atomJSON["type"].Value;
            yield return SuperController.singleton.AddAtomByType(uid, atomJSON["id"].Value);
            Atom cloned = SuperController.singleton.GetAtomByUid(uid);
            if (cloned == null)
            {
                yield return null;
            }

            cloned.PreRestore();
            cloned.Restore(atomJSON as JSONClass, true, true, true);
            cloned.LateRestore(atomJSON as JSONClass, true, true, true);
            cloned.PostRestore();

            sceneOnlyAtoms.Add(cloned);
        }

        public void RemoveSceneOnlyAtoms()
        {
            sceneOnlyAtoms.ForEach((atom) =>
            {
                if (atom == null)
                {
                    return;
                }
                SuperController.singleton.RemoveAtom(atom);
            });

            sceneOnlyAtoms = new List<Atom>();
        }

        public List<JSONClass> GetOverlaps(JSONClass branch)
        {
            List<JSONClass> ids = new List<JSONClass>();

            JSONArray branchAtoms = branch["atoms"].AsArray;
            JSONArray referenceAtoms = reference["atoms"].AsArray;

            for(int i=0; i < branchAtoms.Count; i++)
            {
                JSONClass branchAtom = branchAtoms[i] as JSONClass;
                for(int s=0; s<referenceAtoms.Count; s++)
                {
                    JSONClass referenceAtom = referenceAtoms[s] as JSONClass;
                    if(branchAtom["id"].Value == referenceAtom["id"].Value)
                    {
                        ids.Add(branchAtom);
                        break;
                    }
                }
            }

            return ids;
        }

        public List<JSONClass> GetBranchOnly(JSONClass branch)
        {
            List<JSONClass> ids = new List<JSONClass>();

            JSONArray branchAtoms = branch["atoms"].AsArray;
            JSONArray referenceAtoms = reference["atoms"].AsArray;

            for (int i = 0; i < branchAtoms.Count; i++)
            {
                JSONClass branchAtom = branchAtoms[i] as JSONClass;
                bool found = false;
                for (int s = 0; s < referenceAtoms.Count; s++)
                {
                    JSONClass referenceAtom = referenceAtoms[s] as JSONClass;
                    if (branchAtom["id"].Value == referenceAtom["id"].Value)
                    {
                        found = true;
                        break;
                    }
                }

                if (found == false)
                {
                    ids.Add(branchAtom);
                }
            }

            return ids;
        }
    }
}
