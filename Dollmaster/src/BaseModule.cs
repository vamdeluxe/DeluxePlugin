using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;

namespace DeluxePlugin.Dollmaster
{
    public class BaseModule
    {
        protected DollmasterPlugin dm;

        protected Atom atom;
        protected UI ui;

        List<Atom> generatedAtoms = new List<Atom>();

        public BaseModule(DollmasterPlugin dm)
        {
            this.dm = dm;
            atom = dm.containingAtom;
            ui = dm.ui;

            dm.modules.Add(this);
        }

        public void HandleRename(string old, string newName)
        {
            OnContainingAtomRenamed(old, newName);
        }

        protected virtual IEnumerator CreateAtom(string atomType, string prefix, Action<Atom> onAtomCreated, bool keep=false)
        {
            //  need to wait a bit before creating if we are just loading into the scene, as the previous atom may have not been fully destroyed!!
            yield return new WaitForSeconds(1.0f);

            string atomId = GenerateAtomName(prefix, dm.containingAtom.uid);
            Atom atom = SuperController.singleton.GetAtomByUid(atomId);
            if (atom == null)
            {
                yield return SuperController.singleton.AddAtomByType(atomType, atomId);
                atom = SuperController.singleton.GetAtomByUid(atomId);
            }

            if (atom != null)
            {
                if (keep == false)
                {
                    generatedAtoms.Add(atom);
                }
                onAtomCreated(atom);
            }
        }

        protected string GenerateAtomName(string prefix, string suffix)
        {
            return prefix + " " + suffix;
        }

        protected virtual void OnContainingAtomRenamed(string oldName, string newName)
        {

        }

        public virtual void Update()
        {

        }

        public virtual void OnDestroy()
        {
            generatedAtoms.ForEach((atom) =>
            {
                if (atom == null)
                {
                    return;
                }
                SuperController.singleton.RemoveAtom(atom);
            });
            generatedAtoms.Clear();
        }
    }
}
