using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;

namespace DeluxePlugin.DollMaker
{
    public class WorldUI : BaseModule
    {
        Atom UIAtom;

        string atomPrefix = "Doll Control";

        public WorldUI(DollMaker dm) : base(dm)
        {
            dm.StartCoroutine(CreateAtom("Empty", atomPrefix, (atom)=>
            {
                UIAtom = atom;

                FreeControllerV3 headControl = dm.containingAtom.GetStorableByID("headControl") as FreeControllerV3;
                Vector3 headPosition = headControl.transform.position;
                UIAtom.mainController.transform.position = headPosition;
                UIAtom.mainController.transform.Translate(new Vector3(-1.0f, .6f, 0), Space.Self);
                ui.canvas.transform.SetParent(UIAtom.mainController.transform, false);
            }));
        }

        protected override void OnContainingAtomRenamed(string newName)
        {
            if (UIAtom != null)
            {
                UIAtom.name = UIAtom.uid = GenerateAtomName(atomPrefix, newName);
            }
        }
    }
}
