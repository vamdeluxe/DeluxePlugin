using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;

namespace DeluxePlugin.Dollmaster
{
    public class ThrustAutoController : BaseModule
    {
        public JSONStorableBool autoThrustEnabled;
        public JSONStorableFloat autoThrustHoldDuration;
        public JSONStorableFloat autoThrustVariance;

        float lastChangeTime = 0;

        public ThrustAutoController(DollmasterPlugin dm) : base(dm)
        {
            autoThrustEnabled = new JSONStorableBool("auto thrust", false);
            dm.RegisterBool(autoThrustEnabled);
            UIDynamicToggle autoThrustToggle = ui.CreateToggle("Auto Thrust", 180, 40);
            autoThrustEnabled.toggle = autoThrustToggle.toggle;
            autoThrustToggle.transform.Translate(0.415f, -0.0430f, 0, Space.Self);
            autoThrustToggle.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            autoThrustToggle.labelText.color = new Color(1, 1, 1);

            autoThrustHoldDuration = new JSONStorableFloat("auto thrust hold duration", 4.0f, 1.0f, 10.0f, false);
            dm.RegisterFloat(autoThrustHoldDuration);
            dm.CreateSlider(autoThrustHoldDuration);

            autoThrustVariance = new JSONStorableFloat("auto thrust range", 2f, 0.1f, 5f, false);
            dm.RegisterFloat(autoThrustVariance);
            dm.CreateSlider(autoThrustVariance);

            dm.CreateSpacer();
        }

        public override void Update()
        {
            base.Update();
            if (autoThrustEnabled.val == false)
            {
                return;
            }

            if(Time.time - lastChangeTime > autoThrustHoldDuration.val)
            {
                lastChangeTime = Time.time;

                dm.thrustController.slider.slider.value += UnityEngine.Random.Range(-autoThrustVariance.val, autoThrustVariance.val);
                if(dm.thrustController.slider.slider.value < 1)
                {
                    dm.thrustController.slider.slider.value = 1;
                }
            }
        }
    }
}
