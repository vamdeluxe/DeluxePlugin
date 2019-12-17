using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine.Events;

namespace DeluxePlugin
{
    public class AmbientLightRig : MVRScript
    {
        List<Atom> lights = new List<Atom>();
        JSONStorableFloat radius;
        JSONStorableFloat count;
        JSONStorableBool forcePixel;
        JSONStorableStringChooser lightType;
        JSONStorableBool lookAt;
        JSONStorableFloat heightOffset;
        JSONStorableFloat sharedIntensity;
        JSONStorableFloat sharedRange;
        JSONStorableFloat sharedSpotAngle;
        JSONStorableFloat intensityCurveStart;
        JSONStorableFloat intensityCurveMid;
        JSONStorableFloat intensityCurveEnd;
        JSONStorableFloat intensityMidPoint;
        JSONStorableColor colorStart;
        JSONStorableColor colorEnd;

        public override void Init()
        {
            try
            {
                radius = new JSONStorableFloat("radius", 2, (float count)=>
                {
                    SetupLights();
                }, 0, 10, false);
                RegisterFloat(radius);
                CreateSlider(radius);

                count = new JSONStorableFloat("count", 4, (float count)=>
                {
                    ClearLights();
                    StartCoroutine(GenerateLights());
                }, 1, 20);
                RegisterFloat(count);
                CreateSlider(count);

                forcePixel = new JSONStorableBool("force pixel lights", true, (bool force) =>
                {
                    SetupLights();
                });
                RegisterBool(forcePixel);
                CreateToggle(forcePixel);

                lightType = new JSONStorableStringChooser("light type", new List<string> { "Point", "Spot" }, "Spot", "light type", (string type) =>
                 {
                     SetupLights();
                 });
                RegisterStringChooser(lightType);
                CreatePopup(lightType);

                lookAt = new JSONStorableBool("look at", true);
                RegisterBool(lookAt);
                CreateToggle(lookAt);

                heightOffset = new JSONStorableFloat("height offset", 0, (float offset) =>
                {
                    SetupLights();
                }, -4, 4, false);
                RegisterFloat(heightOffset);
                CreateSlider(heightOffset);

                sharedIntensity = new JSONStorableFloat("shared intensity", 1.8f, (float intensity) =>
                {
                    SetupLights();
                }, 0, 10, false);
                RegisterFloat(sharedIntensity);
                CreateSlider(sharedIntensity);

                sharedRange = new JSONStorableFloat("shared range", 5, (float intensity) =>
                {
                    SetupLights();
                }, 0, 25, false);
                RegisterFloat(sharedRange);
                CreateSlider(sharedRange);

                sharedSpotAngle = new JSONStorableFloat("shared spot angle", 80, (float intensity) =>
                {
                    SetupLights();
                }, 1, 180, false);
                RegisterFloat(sharedSpotAngle);
                CreateSlider(sharedSpotAngle);

                intensityCurveStart = new JSONStorableFloat("intensity curve start", 1, (float intensity) =>
                {
                    SetupLights();
                }, 0, 4, false);
                RegisterFloat(intensityCurveStart);
                CreateSlider(intensityCurveStart, true);

                intensityCurveMid = new JSONStorableFloat("intensity curve mid", 1, (float intensity) =>
                {
                    SetupLights();
                }, 0, 4, false);
                RegisterFloat(intensityCurveMid);
                CreateSlider(intensityCurveMid, true);

                intensityCurveEnd = new JSONStorableFloat("intensity curve end", 1, (float intensity) =>
                {
                    SetupLights();
                }, 0, 4, false);
                RegisterFloat(intensityCurveEnd);
                CreateSlider(intensityCurveEnd, true);

                intensityMidPoint = new JSONStorableFloat("intensity midPoint", 0.5f, (float intensity) =>
                {
                    SetupLights();
                }, 0, 1, false);
                RegisterFloat(intensityMidPoint);
                CreateSlider(intensityMidPoint, true);

                Color defaultRGB = new Color(1, 0.8941176470588235f, 0.7803921568627451f);
                float h, s, v = 0;
                Color.RGBToHSV(defaultRGB, out h, out s, out v);
                HSVColor startingColor = new HSVColor { H = h, S = s, V = v };

                colorStart = new JSONStorableColor("color start", startingColor, (float ch, float cs, float cv) =>
                {
                    SetupLights();
                });
                RegisterColor(colorStart);
                CreateColorPicker(colorStart, true);

                colorEnd = new JSONStorableColor("color end", startingColor, (float ch, float cs, float cv) =>
                {
                    SetupLights();
                });
                RegisterColor(colorEnd);
                CreateColorPicker(colorEnd, true);

                StartCoroutine(GenerateLights());


            }
            catch (Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }
        }

        private IEnumerator GenerateLights()
        {
            int lightsCount = Mathf.FloorToInt(count.val);
            for (int i = 0; i < lightsCount; i++) {
                StartCoroutine(GenerateLight((atom) =>
                {
                    lights.Add(atom);
                }));
            }
            yield return new WaitForSeconds(0.25f);
            SetupLights();
        }

        private void SetupLights()
        {
            int lightsCount = lights.Count;
            float totalRadians = 0;
            float radianPerIndex = 1.0f / lightsCount * 2 * Mathf.PI;

            float atomX = containingAtom.mainController.transform.position.x;
            float atomY = containingAtom.mainController.transform.position.y;
            float atomZ = containingAtom.mainController.transform.position.z;

            Keyframe[] keyframes = new Keyframe[] {
                new Keyframe(0, intensityCurveStart.val),
                new Keyframe(intensityMidPoint.val, intensityCurveMid.val),
                new Keyframe(1.0f, intensityCurveEnd.val),
            };
            AnimationCurve intensityCurve = new AnimationCurve(keyframes);

            Color rgbStart = Color.HSVToRGB(colorStart.val.H, colorStart.val.S, colorStart.val.V);
            Color rgbEnd = Color.HSVToRGB(colorEnd.val.H, colorEnd.val.S, colorEnd.val.V);

            int index = 0;
            foreach(var light in lights)
            {
                float x = atomX + Mathf.Cos(totalRadians) * radius.val;
                float y = atomY + heightOffset.val;
                float z = atomZ + Mathf.Sin(totalRadians) * radius.val;

                light.mainController.transform.position = new Vector3(x, y, z);
                light.parentAtom = containingAtom;
                totalRadians += radianPerIndex;

                var lightStore = light.GetStorableByID("Light");
                if (forcePixel.val)
                {
                    lightStore.SetStringChooserParamValue("renderType", "ForcePixel");
                }

                lightStore.SetStringChooserParamValue("type", lightType.val);

                lightStore.SetFloatParamValue("range", sharedRange.val);
                lightStore.SetFloatParamValue("spotAngle", sharedSpotAngle.val);

                float lerpAlpha = (float)index / lightsCount;
                float intensityFromCurve = intensityCurve.Evaluate(lerpAlpha);
                light.GetStorableByID("Light").SetFloatParamValue("intensity", sharedIntensity.val * intensityFromCurve);

                Color lerpedColor = Color.Lerp(rgbStart, rgbEnd, lerpAlpha);
                float h, s, v = 0;
                Color.RGBToHSV(lerpedColor, out h, out s, out v);

                lightStore.SetColorParamValue("color", new HSVColor { H = h, S = s, V = v });

                index++;
            }
        }

        IEnumerator GenerateLight(UnityAction<Atom> callback)
        {
            string id = Guid.NewGuid().ToString();
            StartCoroutine(SuperController.singleton.AddAtomByType("InvisibleLight", id));
            while(SuperController.singleton.GetAtomByUid(id) == null)
            {
                yield return new WaitForEndOfFrame();
            }

            callback(SuperController.singleton.GetAtomByUid(id));
        }

        private void Update()
        {
            foreach(var light in lights)
            {
                light.mainController.transform.LookAt(containingAtom.mainController.transform.position);
            }
        }

        private void ClearLights()
        {
            foreach (var light in lights)
            {
                SuperController.singleton.RemoveAtom(light);
            }
            lights.Clear();
        }

        private void OnDestroy()
        {
            base.Remove();
            ClearLights();
        }
    }
}