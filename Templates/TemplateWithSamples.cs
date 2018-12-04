using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;

namespace DeluxePlugin {
	public class TemplateWithSamples : MVRScript {

		// IMPORTANT - DO NOT make custom enums. The dynamic C# complier crashes Unity when it encounters these for
		// some reason

		// IMPORTANT - DO NOT OVERRIDE Awake() as it is used internally by MVRScript - instead use Init() function which
		// is called right after creation
		public override void Init() {
			try {
				// put init code in here
				SuperController.LogMessage("Template Loaded");

				// create custom JSON storable params here if you want them to be stored with scene JSON
				// types are JSONStorableFloat, JSONStorableBool, JSONStorableString, JSONStorableStringChooser
				// JSONStorableColor

				// JSONStorableFloat example
				JSONStorableFloat jfloat = new JSONStorableFloat("FooParam", 0f, DummyFloatCallback, 0f, 5f, true);
				RegisterFloat(jfloat);
				dslider = CreateSlider(jfloat);

				// button example
				dbutton = CreateButton("FooButton");
				if (dbutton != null) {
					dbutton.button.onClick.AddListener(DummyButtonCallback);
				}

				// JSONStorableColor example
				HSVColor hsvc = HSVColorPicker.RGBToHSV(1f, 0f, 0f);
				JSONStorableColor jcolor = new JSONStorableColor("FooColor", hsvc, DummyColorCallback);
				RegisterColor(jcolor);
				CreateColorPicker(jcolor);

				// JSONStorableString example
				jstring = new JSONStorableString("FooString", "");
				// register tells engine you want value saved in json file during save and also make it available to
				// animation/trigger system
				//RegisterString(jstring);
				dtext = CreateTextField(jstring);

				// JSONStorableStringChooser example
				List<string> choices = new List<string>();
				choices.Add("None");
				choices.Add("Choice1");
				choices.Add("Choice2");
				choices.Add("Choice3");
				JSONStorableStringChooser jchooser = new JSONStorableStringChooser("FooChooser", choices, "None", "Choose Form Of Destruction", DummyChooserCallback);
				UIDynamicPopup udp = CreatePopup(jchooser, true);
				//CreateScrollablePopup(jchooser, true);
				udp.labelWidth = 300f;

				JSONStorableFloat jfloat2 = new JSONStorableFloat("FooParam2", 0f, 0f, 1f);
				CreateSlider(jfloat2, true);

				// spacer example
				UIDynamic spacer = CreateSpacer(true);
				// give the popup some room
				spacer.height = 400f;

				// JSONStorableToggle example
				JSONStorableBool jbool = new JSONStorableBool("FooBool", true);
				CreateToggle(jbool, true);
			}
			catch (Exception e) {
				SuperController.LogError("Exception caught: " + e);
			}
		}

		// Start is called once before Update or FixedUpdate is called and after Init()
		void Start() {
			try {
				// put code in here
			}
			catch (Exception e) {
				SuperController.LogError("Exception caught: " + e);
			}
		}

		// Update is called with each rendered frame by Unity
		void Update() {
			try {
				// put code in here
			}
			catch (Exception e) {
				SuperController.LogError("Exception caught: " + e);
			}
		}

		// FixedUpdate is called with each physics simulation frame by Unity
		void FixedUpdate() {
			try {
				// put code in here
			}
			catch (Exception e) {
				SuperController.LogError("Exception caught: " + e);
			}
		}

		// OnDestroy is where you should put any cleanup
		// if you registered objects to supercontroller or atom, you should unregister them here
		void OnDestroy() {
		}


		// ***********************************************************************************
		// Code below is just for this example
		// ***********************************************************************************

		protected JSONStorableString jstring;
		protected UIDynamicButton dbutton;
		protected UIDynamicSlider dslider;
		protected UIDynamicTextField dtext;

		public static void DummyButtonCallback() {
			SuperController.LogMessage("DummyButtonCallback called");
		}

		protected void DummyFloatCallback(JSONStorableFloat jf) {
			//SuperController.LogMessage("Float param " + jfloat.name + " set to " + jfloat.val);
			if (jstring != null) {
				jstring.val = "\nFloat param " + jf.name + " set to " + jf.val;
			}
		}

		protected void DummyColorCallback(JSONStorableColor jcolor) {
			if (dbutton != null) {
				dbutton.buttonColor = jcolor.colorPicker.currentColor;
			}
		}

		protected void DummyChooserCallback(string s) {
			if (jstring != null) {
				jstring.val = "\nYou have chosen " + s;
			}
		}

	}
}