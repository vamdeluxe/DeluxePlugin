using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;

namespace DeluxePlugin {
	public class TemplateSimple : MVRScript {

		public override void Init() {
			try {

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

	}
}