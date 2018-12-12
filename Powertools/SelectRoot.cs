using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;

namespace DeluxePlugin.Powertools
{
    public class SelectRoot : MVRScript
    {
        FreeControllerV3 lastSelected;
        float lastPressedTime = 0;
        float doubleClickTime = 0.5f;
        void LateUpdate()
        {
            try
            {
                if (Input.GetMouseButtonUp(0))
                {
                    if ((Time.time - lastPressedTime) < doubleClickTime && SuperController.singleton.GetSelectedController() == lastSelected)
                    {
                        SelectMainController();
                    }

                    lastSelected = SuperController.singleton.GetSelectedController();
                    lastPressedTime = Time.time;
                }

                if (Input.GetMouseButtonUp(0) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
                {
                    SelectMainController();
                }
            }
            catch (Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }
        }

        void SelectMainController()
        {
            FreeControllerV3 selected = SuperController.singleton.GetSelectedController();

            if (selected == null)
            {
                return;
            }

            SuperController.singleton.SelectController(selected.containingAtom.mainController);
            SuperController.singleton.ShowMainHUD();

        }

    }
}
