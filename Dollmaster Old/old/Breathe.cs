using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;

namespace DeluxePlugin
{
    public class Breathe : MVRScript
    {
        public float breath = 0.0f;
        private float breatheCylce = -0.1f;
        private float breathLerp = 0.16f;

        private DAZMorph breatheMorph;
        private FreeControllerV3 chest;
        private FreeControllerV3 head;

        public JSONStorableFloat intensity;

        public override void Init()
        {
            try
            {
                JSONStorable js = containingAtom.GetStorableByID("geometry");
                if (js != null)
                {
                    DAZCharacterSelector dcs = js as DAZCharacterSelector;
                    GenerateDAZMorphsControlUI morphUI = dcs.morphsControlUI;
                    if (morphUI != null)
                    {
                        breatheMorph = morphUI.GetMorphByDisplayName("Breath1");
                    }
                }

                chest = containingAtom.GetStorableByID("chestControl") as FreeControllerV3;
                if (chest != null)
                {
                    chest.jointRotationDriveSpring = 60.0f;
                    chest.jointRotationDriveDamper = 0.5f;
                }

                head = containingAtom.GetStorableByID("headControl") as FreeControllerV3;
                if (head != null)
                {
                    head.RBHoldPositionSpring = 4000.0f;
                }

                intensity = new JSONStorableFloat("intensity", 0, 0, 1, true, true);
                intensity.storeType = JSONStorableParam.StoreType.Full;
                CreateSlider(intensity);
                RegisterFloat(intensity);
            }
            catch (Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }
        }

        public void Update()
        {
            float iv = intensity.val;

            breath += (iv - breath) * Time.deltaTime * breathLerp;
            breath = Mathf.Clamp01(breath);

            breatheCylce += Time.deltaTime * Mathf.Clamp((1.27f + breath * 7.0f), 0.0f, 20.0f);
            if (breatheMorph != null)
            {
                float power = Mathf.Clamp(breath, 0.5f, 0.7f);
                float cycle = Mathf.Sin(breatheCylce) * power;
                breatheMorph.morphValue = cycle;
                //Debug.Log("breathing");
            }

            if (chest != null)
            {
                float power = Remap(breath, 0.0f, 1.0f, -10, 10);
                float cycle = Mathf.Sin(breatheCylce * 2.0f + 0.4f) * -power;
                chest.jointRotationDriveXTarget = cycle;
            }

            if (head != null)
            {
                //head.RBHoldPositionSpring = 2000;
                //head.RBHoldRotationSpring = 120;

                //head.RBHoldPositionSpring = Remap(breath, 0.3f, 0.6f, 4000, 2000);
                //head.RBHoldRotationSpring = Remap(breath, 0.3f, 0.6f, 120, 50);
            }
        }

        private float Remap(float x, float x1, float x2, float y1, float y2)
        {
            var m = (y2 - y1) / (x2 - x1);
            var c = y1 - m * x1;

            return m * x + c;
        }
    }
}
