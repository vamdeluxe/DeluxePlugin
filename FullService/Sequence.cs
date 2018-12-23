using System;
using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine.UI;

namespace DeluxePlugin.FullService
{
    public class Sequence
    {
        public Dictionary<string,string> branches = new Dictionary<string,string>();

        private List<Atom> buttons = new List<Atom>();

        private Atom parentAtom;

        public static float DISTANCE_BETWEEN_BUTTONS = .2f;

        public delegate void OnBranchSelectedEvent(string branchName, Sequence sequence);

        public static OnBranchSelectedEvent onBranchSelected;

        public Sequence(SceneBranch scene, JSONClass branchesObject)
        {
            this.parentAtom = scene.GetContainingAtom();

            branchesObject.Keys.ToList().ForEach((string key) =>
            {
                branches[key] = branchesObject[key];
            });

            //for (int i = 0; i < branchesArray.Count; i++)
            //{
            //    JSONNode branch = branchesArray[i];
            //    branches.Add(branch.Value);
            //}
        }

        public void GenerateButtons()
        {
            int index = 1;
            branches.Keys.ToList().ForEach((string name) =>
            {
                string branchName = branches[name];
                string branchLabel = name;
                SuperController.singleton.StartCoroutine(GenerateButton(branchLabel, branchName, index++));
            });
        }

        private IEnumerator GenerateButton(string label, string branchName, int index)
        {
            string atomId = Guid.NewGuid().ToString();
            yield return SuperController.singleton.AddAtomByType("UIButton", atomId);

            Atom atom = SuperController.singleton.GetAtomByUid(atomId);
            buttons.Add(atom);

            atom.GetStorableByID("Text").SetStringParamValue("text", label);
            atom.mainController.transform.SetPositionAndRotation(parentAtom.mainController.transform.position, parentAtom.mainController.transform.rotation);
            atom.SetParentAtom(parentAtom.uid);

            atom.mainController.transform.Translate(0, index * DISTANCE_BETWEEN_BUTTONS, 0);

            UIButtonTrigger ubt = atom.GetStorableByID("Trigger") as UIButtonTrigger;
            ubt.button.onClick.AddListener(() =>
            {
                if (onBranchSelected != null)
                {
                    onBranchSelected.Invoke(branchName, this);
                }
            });

            atom.mainController.canGrabPosition = false;
            atom.mainController.canGrabRotation = false;

        }

        public void DestroyButtons()
        {
            buttons.ForEach((button) =>
            {
                if (button == null)
                {
                    return;
                }
                SuperController.singleton.RemoveAtom(button);
            });
            buttons = new List<Atom>();
        }
    }
}
