using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using System.Linq;



namespace DeluxePlugin.HeadIdle
{
    public class HeadIdle : MVRScript
    {
        JSONClass poses;
        float lastPoseChange = 0;
        float poseChangeFrequency = 5.0f;

        FreeControllerV3 headControl;

        Quaternion startRotation;
        Quaternion targetRotation;

        float animationDuration = 2.0f;

        public override void Init()
        {
            try
            {
                string CONFIG_PATH = GetPluginPath() + "/positions.json";
                poses = JSON.Parse(SuperController.singleton.ReadFileIntoString(CONFIG_PATH)).AsObject;
                headControl = containingAtom.GetStorableByID("headControl") as FreeControllerV3;
                CreateButton("Save Pose").button.onClick.AddListener(() =>
                {

                    Vector3 localEuler = headControl.transform.localEulerAngles;
                    JSONArray positions = poses["positions"].AsArray;
                    JSONClass vector = new JSONClass();
                    vector["x"] = localEuler.x.ToString();
                    vector["y"] = localEuler.y.ToString();
                    vector["z"] = localEuler.z.ToString();
                    positions.Add(vector);
                    SuperController.singleton.SaveStringIntoFile(CONFIG_PATH, poses.ToString("  "));
                });

                targetRotation = headControl.transform.localRotation;
            }
            catch (Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }
        }

        void Update()
        {
            if((Time.time - lastPoseChange) >= poseChangeFrequency)
            {
                lastPoseChange = Time.time;

                JSONArray positions = poses["positions"].AsArray;
                int randomIndex = UnityEngine.Random.Range(0, positions.Count);

                JSONClass jsonEuler = positions[randomIndex].AsObject;


                Vector3 targetEuler = new Vector3(jsonEuler["x"].AsFloat, jsonEuler["y"].AsFloat, jsonEuler["z"].AsFloat);
                targetRotation = Quaternion.Euler(targetEuler.x, targetEuler.y, targetEuler.z);
                startRotation = headControl.transform.localRotation;

                animationDuration = UnityEngine.Random.Range(0.2f, 0.8f);
                poseChangeFrequency = UnityEngine.Random.Range(4.0f, 7.0f);

                float dist = Quaternion.Angle(startRotation, targetRotation);
                animationDuration += dist / 10.0f;
                animationDuration = Mathf.Max(animationDuration, 0.8f);
            }

            float elapsed = Time.time - lastPoseChange;
            //float alpha = CubicEaseInOut(elapsed / animationDuration);
            float alpha = elapsed / animationDuration;
            alpha = Mathf.Clamp01(alpha);
            headControl.transform.localRotation = Quaternion.Slerp(headControl.transform.localRotation, targetRotation, alpha);
        }

        static public float CubicEaseInOut(float p)
        {
            if (p < 0.5f)
            {
                return 4 * p * p * p;
            }
            else
            {
                float f = ((2 * p) - 2);
                return 0.5f * f * f * f + 1;
            }
        }

        string GetPluginPath()
        {
            SuperController.singleton.currentSaveDir = SuperController.singleton.currentLoadDir;
            string pluginId = this.storeId.Split('_')[0];
            MVRPluginManager manager = containingAtom.GetStorableByID("PluginManager") as MVRPluginManager;
            string pathToScriptFile = manager.GetJSON(true, true)["plugins"][pluginId].Value;
            string pathToScriptFolder = pathToScriptFile.Substring(0, pathToScriptFile.LastIndexOfAny(new char[] { '/', '\\' }));
            return pathToScriptFolder;
        }
    }
}
