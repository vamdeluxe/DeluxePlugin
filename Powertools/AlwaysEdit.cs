using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;

namespace DeluxePlugin.Powertools
{
    public class AlwaysEdit : MVRScript
    {
        void Update()
        {
            try
            {
                if (SuperController.singleton.editModeToggle.isOn == false)
                {
                    SuperController.singleton.editModeToggle.isOn = true;
                }
            }
            catch (Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }
        }



    }
}
