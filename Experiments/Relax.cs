using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;

namespace DeluxePlugin
{
    public class Relax : MVRScript
    {
        public struct SavedControllerState
        {
            public FreeControllerV3.PositionState positionState;
            public FreeControllerV3.RotationState rotationState;
            public float jointDriveSpring;
        }

        Dictionary<string, SavedControllerState> saved = new Dictionary<string, SavedControllerState>();

        public override void Init()
        {
            try
            {
                CreateButton("Relax").button.onClick.AddListener(() =>
                {
                    containingAtom.freeControllers.ToList().ForEach((controller) =>
                    {
                        saved[controller.name] = new SavedControllerState
                        {
                            positionState = controller.currentPositionState,
                            rotationState = controller.currentRotationState,
                            jointDriveSpring = controller.jointRotationDriveSpring
                        };

                        controller.currentPositionState = FreeControllerV3.PositionState.Off;
                        controller.currentRotationState = FreeControllerV3.RotationState.Off;
                        controller.SetFloatParamValue("jointRotationDriveSpring", 0.0f);
                        controller.jointRotationDriveSpring = 0.0f;
                    });
                });

                CreateButton("Restore").button.onClick.AddListener(() =>
                {
                    containingAtom.freeControllers.ToList().ForEach((controller) =>
                    {
                        if (saved.ContainsKey(controller.name))
                        {
                            SavedControllerState state = saved[controller.name];
                            controller.currentPositionState = state.positionState;
                            controller.currentRotationState = state.rotationState;
                            controller.jointRotationDriveSpring = state.jointDriveSpring;
                        }
                    });
                });

            }
            catch (Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }
        }

    }
}