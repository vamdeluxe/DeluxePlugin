using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using System.Linq;


namespace DeluxePlugin
{
    public class Submotion : MVRScript
    {
        Dictionary<FreeControllerV3, float> initialRotationSpring = new Dictionary<FreeControllerV3, float>();
        Dictionary<FreeControllerV3, float> initialPositionSpring = new Dictionary<FreeControllerV3, float>();

        JSONStorableFloat rotationAmount;
        JSONStorableFloat rotationSpeed;

        JSONStorableFloat positionAmount;
        JSONStorableFloat positionSpeed;
        public override void Init()
        {
            try
            {
                containingAtom.freeControllers.ToList().ForEach((controller) =>
                {
                    initialRotationSpring[controller] = controller.RBHoldRotationSpring;
                    initialPositionSpring[controller] = controller.RBHoldPositionSpring;
                });

                rotationAmount = new JSONStorableFloat("rotationAmount", 300.0f, 0.0f, 500.0f);
                RegisterFloat(rotationAmount);
                CreateSlider(rotationAmount);

                rotationSpeed = new JSONStorableFloat("rotationSpeed", 0.3f, 0.0f, 10.0f);
                RegisterFloat(rotationSpeed);
                CreateSlider(rotationSpeed);

                positionAmount = new JSONStorableFloat("positionAmount", 400.0f, 0.0f, 500.0f);
                RegisterFloat(positionAmount);
                CreateSlider(positionAmount);

                positionSpeed = new JSONStorableFloat("positionSpeed", 0.45f, 0.0f, 10.0f);
                RegisterFloat(positionSpeed);
                CreateSlider(positionSpeed);
            }
            catch (Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }
        }

        void Update()
        {
            int index = 0;
            initialRotationSpring.Keys.ToList().ForEach((controller) =>
            {
                float initialRotationValue = initialRotationSpring[controller];

                float initialPositionValue = initialPositionSpring[controller];

                float rn = Mathf.PerlinNoise(Time.time * rotationSpeed.val, index);

                controller.RBHoldRotationSpring = initialRotationValue + rn * rotationAmount.val;

                float pn = Mathf.PerlinNoise(Time.time * positionSpeed.val, index + 1000);
                controller.RBHoldPositionSpring = initialPositionValue + pn * positionAmount.val;

                index+=100;
            });


        }
    }
}
