using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;

namespace DeluxePlugin {
	public class TemplateStarter : MVRScript {

		public override void Init() {
			try {

			}
			catch (Exception e) {
				SuperController.LogError("Exception caught: " + e);
			}
		}
	}
}