using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;

namespace DeluxePlugin
{
    public class OMeter : MVRScript
    {
        public override void Init()
        {
            try
            {
                JSONStorableFloat oMeterStorable = new JSONStorableFloat("<3", 0, 0, 1, true, false);
                oMeterStorable.storeType = JSONStorableParam.StoreType.Full;

                UIDynamicSlider oMeterSlider = CreateSlider(oMeterStorable, true);
                oMeterSlider.defaultButtonEnabled = false;
                oMeterSlider.quickButtonsEnabled = false;
                oMeterSlider.rangeAdjustEnabled = false;
                RegisterFloat(oMeterStorable);

            }
            catch (Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }
        }
    }
}
