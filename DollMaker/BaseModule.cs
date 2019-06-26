using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;

namespace DeluxePlugin.DollMaker
{
    public class BaseModule
    {
        protected DollMaker dm;

        public Atom atom;
        public UI ui;
        public Transform moduleUI;
        protected GridLayoutGroup moduleButtonsLayout;

        string oldContainerName;

        List<Atom> generatedAtoms = new List<Atom>();

        public BaseModule(DollMaker dm)
        {
            this.dm = dm;
            atom = dm.containingAtom;
            ui = dm.ui;

            GameObject moduleLayerObject = new GameObject("module UI", typeof(RectTransform));
            moduleLayerObject.transform.SetParent(ui.canvas.transform, false);
            moduleUI = moduleLayerObject.transform;
            moduleUI.transform.localPosition = Vector3.zero;
            moduleUI.transform.localScale = Vector3.one;
            moduleUI.transform.localEulerAngles = Vector3.zero;
            moduleUI.transform.localPosition = new Vector3(0, -300, 0);
            moduleUI.GetComponent<RectTransform>().pivot = new Vector2(0, 0);

            dm.modules.Add(this);

            oldContainerName = dm.containingAtom.uid;
            SuperController.singleton.onAtomUIDRenameHandlers += HandleRename;

            float buttonHeight = 60;
            moduleButtonsLayout = ui.CreateGridLayout(1200, buttonHeight);
            moduleButtonsLayout.cellSize = new Vector2(300, buttonHeight);
            moduleButtonsLayout.GetComponent<RectTransform>().pivot = new Vector2(0, 0);
            moduleButtonsLayout.transform.SetParent(moduleUI, false);
            moduleButtonsLayout.transform.localPosition = new Vector3(0, 0, 0);

        }

        protected UIDynamicButton CreateModuleButton(string name)
        {
            UIDynamicButton button = ui.CreateButton(name, 120, 80, moduleButtonsLayout.transform);
            UI.ColorButton(button, Color.white, new Color(0.77f, 0.64f, 0.26f));
            return button;
        }

        void HandleRename(string old, string newName)
        {
            if (old != oldContainerName)
            {
                return;
            }

            OnContainingAtomRenamed(newName);

            oldContainerName = newName;
        }

        protected virtual IEnumerator CreateAtom(string atomType, string prefix, Action<Atom> onAtomCreated, bool keep = false)
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

        protected virtual void OnContainingAtomRenamed(string newName)
        {

        }

        public virtual void OnModuleActivate()
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

            SuperController.singleton.onAtomUIDRenameHandlers -= HandleRename;
        }
    }
}
