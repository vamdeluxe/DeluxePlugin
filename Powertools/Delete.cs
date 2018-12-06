using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;

namespace DeluxePlugin.Powertools
{
    public class Delete : MVRScript
    {
        public override void Init()
        {
            try
            {
                CreateButton("Deleted Selected (Control+Delete)").button.onClick.AddListener(DeletedSelected);
            }
            catch (Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }
        }

        private void DeletedSelected()
        {
            Atom selected = SuperController.singleton.GetSelectedAtom();

            if (selected == null)
            {
                return;
            }

            SuperController.singleton.RemoveAtom(selected);
        }

        void Update()
        {
            try
            {
                if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                {
                    if (Input.GetKeyDown(KeyCode.Delete))
                    {
                        DeletedSelected();
                    }
                }
            }
            catch (Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }
        }

    }
}
