using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;

namespace MVRPlugin {
	public class ShowSelectedControllerHUD : MVRScript {

		// IMPORTANT - DO NOT make custom enums. The dynamic C# complier crashes Unity when it encounters these for
		// some reason

		protected Canvas dynCanvas;
		protected Text dynText;

		// IMPORTANT - DO NOT OVERRIDE Awake() as it is used internally by MVRScript - instead use Init() function which
		// is called right after creation
		public override void Init() {
			try {
				// put init code in here
				SuperController.LogMessage("ShowSelectedControllerHUD Loaded");

				// create custom JSON storable params here if you want them to be stored with scene JSON
				// types are JSONStorableFloat, JSONStorableBool, JSONStorableString, JSONStorableStringChooser
				// JSONStorableColor

				GameObject g = new GameObject();
				dynCanvas = g.AddComponent<Canvas>();
				dynCanvas.renderMode = RenderMode.WorldSpace;
				// only use AddCanvas if you want to interact with the UI - no needed if display only
				SuperController.singleton.AddCanvas(dynCanvas);
				CanvasScaler cs = g.AddComponent<CanvasScaler>();
				cs.scaleFactor = 100.0f;
				cs.dynamicPixelsPerUnit = 1f;
				GraphicRaycaster gr = g.AddComponent<GraphicRaycaster>();
				RectTransform rt = g.GetComponent<RectTransform>();
				rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 500f);
				rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 100f);
				g.transform.localScale = new Vector3(0.0006f, 0.0006f, 0.0006f);
				g.transform.localPosition = new Vector3(.15f, .2f, .7f);

				// anchor to head for HUD effect
				Transform headCenter = SuperController.singleton.centerCameraTarget.transform;
				rt.SetParent(headCenter, false);
                //dynCanvas.transform.SetParent(headCenter, false);

                GameObject g2 = new GameObject();
				g2.name = "Text";
				g2.transform.parent = g.transform;
				g2.transform.localScale = Vector3.one;
				g2.transform.localPosition = Vector3.zero;
				g2.transform.localRotation = Quaternion.identity;
				Text t = g2.AddComponent<Text>();
				RectTransform rt2 = g2.GetComponent<RectTransform>();
				rt2.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 500f);
				rt2.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 100f);
				t.alignment = TextAnchor.MiddleCenter;
				t.horizontalOverflow = HorizontalWrapMode.Overflow;
				t.verticalOverflow = VerticalWrapMode.Overflow;
				Font ArialFont = (Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");
				t.font = ArialFont;
				t.fontSize = 24;
				t.text = "Test";
				t.enabled = true;
				t.color = Color.white;
				dynText = t;
				g.name = "Canvas";

			}
			catch (Exception e) {
				SuperController.LogError("Exception caught: " + e);
			}
		}

		// Update is called with each rendered frame by Unity
		void Update() {
			try {
                dynCanvas.renderMode = RenderMode.WorldSpace;
                // this displays the selected controller name in the hud text
                FreeControllerV3 fcv3 = SuperController.singleton.GetSelectedController();
				if (fcv3 != null) {
					dynText.text = fcv3.containingAtom.name + ":" + fcv3.name;
				} else {
					dynText.text = "";
				}
			}
			catch (Exception e) {
				SuperController.LogError("Exception caught: " + e);
			}
		}

		// OnDestroy is where you should put any cleanup
		// if you registered objects to supercontroller or atom, you should unregister them here
		void OnDestroy() {
			if (dynCanvas != null) {
				//SuperController.singleton.RemoveCanvas(dynCanvas);
				Destroy(dynCanvas.gameObject);
			}
		}

	}
}