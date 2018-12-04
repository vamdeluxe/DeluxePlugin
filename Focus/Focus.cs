using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;

namespace DeluxePlugin {
	public class Focus : MVRScript {

        public static float DEFAULT_SCALE = 0.6f;
        public static float HIGHLIGHTED_SCALE = 0.25f;

        Dictionary<FreeControllerV3, float> scaleMapping = new Dictionary<FreeControllerV3, float>();

        JSONStorableBool focusOn;

        public override void Init() {
			try {
                focusOn = new JSONStorableBool("focusOn", false);
                focusOn.storeType = JSONStorableParam.StoreType.Full;
                focusOn.setCallbackFunction = DoSetFocus;

                RegisterBool(focusOn);
                CreateToggle(focusOn);
                DoSetFocus(focusOn.val);


                containingAtom.freeControllers.ToList().ForEach((controller) =>
                {
                    Debug.Log(controller.name);
                    Debug.Log(controller.transform.position.x + ", " + controller.transform.position.y + ", " + controller.transform.position.z);
                });
            }
			catch (Exception e) {
				SuperController.LogError("Exception caught: " + e);
			}
		}

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.BackQuote))
            {
                focusOn.SetVal(!focusOn.val);
            }
        }

        void DoSetFocus(bool on)
        {
            SuperController.singleton.GetAtoms().ForEach((atom) =>
            {
                atom.freeControllers.ToList().ForEach((controller) =>
                {
                    SetControllerVisibility(controller, !on);
                });

                if (atom.mainController != null)
                {
                    SetControllerVisibility(atom.mainController, !on);
                }

                if (atom.masterController != null)
                {
                    SetControllerVisibility(atom.masterController, !on);
                }
            });

        }

		void OnDestroy() {
            DoSetFocus(false);
        }

        private bool AllowVisibility(FreeControllerV3 controller)
        {
            bool anyControlOn = controller.currentPositionState == FreeControllerV3.PositionState.On && controller.currentRotationState == FreeControllerV3.RotationState.On;
            bool interactiveInPlay = controller.interactableInPlayMode;
            bool isAtomOn = controller.containingAtom.on;
            return anyControlOn && interactiveInPlay && isAtomOn;
        }


        public void SetControllerVisibility(FreeControllerV3 controller, bool visible)
        {
            if (AllowVisibility(controller))
            {
                visible = true;
            }


            if (scaleMapping.ContainsKey(controller) == false)
            {
                scaleMapping.Add(controller, controller.deselectedMeshScale);
            }

            controller.deselectedMeshScale = visible ? scaleMapping[controller] : 0.0f;

            controller.highlightedScale = visible ? HIGHLIGHTED_SCALE : 0.0f;
            controller.unhighlightedScale = visible ? HIGHLIGHTED_SCALE : 0.0f;

            controller.meshScale = visible ? DEFAULT_SCALE : 0.0f;

            SphereCollider collider = controller.GetComponent<SphereCollider>();
            collider.radius = visible ? 0.11f : 0.0f;
            collider.enabled = visible;

            if (visible)
            {
                scaleMapping.Remove(controller);
            }

        }

    }
}