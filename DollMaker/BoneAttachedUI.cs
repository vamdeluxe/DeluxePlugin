using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using System.Linq;

namespace DeluxePlugin.DollMaker
{
    class BoneAttachedUI
    {
        public UIDynamicButton button;
        DAZBone bone;
        Vector3 offset;
        public BoneAttachedUI(string title, DAZBone bone, UI ui, Vector3 offset, Transform uiParent)
        {
            this.bone = bone;
            button = ConfigButton(ui.CreateButton(title.ToUpper(), 120, 60, uiParent));
            this.offset = offset;
        }

        UIDynamicButton ConfigButton(UIDynamicButton button)
        {
            button.buttonColor = DollMaker.BG_COLOR;
            button.textColor = DollMaker.FG_COLOR;
            button.buttonText.fontSize = 30;
            return button;
        }

        public void Update()
        {
            button.transform.position = bone.transform.position + bone.containingAtom.transform.TransformPoint(offset);
        }
    }
}
