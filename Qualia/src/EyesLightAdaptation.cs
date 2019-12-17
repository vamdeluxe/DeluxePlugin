using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using System.Linq;

namespace DeluxePlugin.Qualia
{
    public class EyesLightAdaptation : MVRScript
    {
        EyesControl eyesControl;
        GameObject debugObject;
        CapsuleCollider faceCollider;

        DAZBone headBone;

        DAZMorph squintMorph;
        DAZMorph leftSquintMorph;
        DAZMorph rightSquintMorph;

        Transform leftEyeReference;
        Transform rightEyeReference;

        DAZMorph pupilDilationMorph;

        JSONStorableFloat squintSpeed = new JSONStorableFloat("Squint Speed", 20, 1, 100, true);
        JSONStorableFloat dilationSpeed = new JSONStorableFloat("Dilation Speed", 1, 0.1f, 4, false);

        public override void Init()
        {
            eyesControl = containingAtom.GetStorableByID("Eyes") as EyesControl;

            faceCollider = containingAtom.GetComponentsInChildren<CapsuleCollider>().ToList().Find(collider => collider.name == "AutoColliderAutoCollidersFaceCentral1Hard");

            headBone = containingAtom.GetStorableByID("head") as DAZBone;

            leftEyeReference = new GameObject().transform;
            leftEyeReference.SetParent(headBone.transform, false);
            leftEyeReference.localPosition = new Vector3(-0.05f, 0.05f, 0.05f);

            rightEyeReference = new GameObject().transform;
            rightEyeReference.SetParent(headBone.transform, false);
            rightEyeReference.localPosition = new Vector3(0.05f, 0.05f, 0.05f);

            #region Debug
            //debugObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            //GameObject.Destroy(debugObject.GetComponent<Collider>());
            //debugObject.transform.SetParent(headBone.transform, false);
            //debugObject.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            //debugObject.transform.localPosition = new Vector3(-0.05f, 0.05f, 0.05f);

            //float radius = faceCentralCollider.radius;
            //float height = faceCentralCollider.height;
            //debugObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            //debugObject.transform.SetParent(faceCentralCollider.transform, false);
            //debugObject.transform.parent = faceCentralCollider.gameObject.transform;
            //debugObject.transform.localScale = new Vector3(radius, height * 0.5f, radius);
            //GameObject.Destroy(debugObject.GetComponent<Collider>());
            #endregion

            JSONStorable geometry = containingAtom.GetStorableByID("geometry");
            DAZCharacterSelector character = geometry as DAZCharacterSelector;
            GenerateDAZMorphsControlUI morphControl = character.morphsControlUI;
            leftSquintMorph = morphControl.GetMorphByDisplayName("Eyes Squint Left");
            rightSquintMorph = morphControl.GetMorphByDisplayName("Eyes Squint Right");
            pupilDilationMorph = morphControl.GetMorphByDisplayName("PupilDilation");

            RegisterFloat(squintSpeed);
            CreateSlider(squintSpeed);
        }

        void OnDestroy()
        {
            if (debugObject != null)
            {
                GameObject.Destroy(debugObject);
            }

            if (leftEyeReference != null)
            {
                GameObject.Destroy(leftEyeReference.gameObject);
            }

            if (rightEyeReference != null)
            {
                GameObject.Destroy(rightEyeReference.gameObject);
            }
        }

        void Update()
        {
            SquintFromLight();
        }

        void SquintFromLight() {
            var atoms = SuperController.singleton.GetAtoms();

            float maxSquintValueLeft = 0;
            float maxSquintValueRight = 0;

            foreach (var atom in atoms)
            {
                if (atom.type == "InvisibleLight")
                {
                    var lightStore = atom.GetStorableByID("Light");
                    float intensity = lightStore.GetFloatParamValue("intensity");
                    float range = lightStore.GetFloatParamValue("range");

                    RaycastHit hit;
                    Vector3 start = atom.mainController.transform.position;
                    Vector3 end = faceCollider.transform.position;
                    Vector3 direction = end - start;
                    bool didHit = Physics.Raycast(start, direction, out hit);
                    if (didHit && hit.rigidbody.name == "head")
                    {
                        float squintPower = intensity * intensity * Mathf.Sqrt(range);

                        float distToLeftEye = (start - leftEyeReference.position).magnitude;
                        float distToRightEye = (start - rightEyeReference.position).magnitude;

                        float squintValueLeft = 1 / (distToLeftEye * distToLeftEye) * squintPower;
                        float squintValueRight = 1 / (distToRightEye * distToRightEye) * squintPower;

                        maxSquintValueLeft = Math.Max(maxSquintValueLeft, squintValueLeft);
                        maxSquintValueRight = Math.Max(maxSquintValueRight, squintValueRight);
                    }
                }
            }

            float squintMorphValueLeft = Mathf.Clamp(maxSquintValueLeft * 0.05f, 0, 1.0f);
            leftSquintMorph.morphValue += (squintMorphValueLeft - leftSquintMorph.morphValue) * Time.deltaTime * squintSpeed.val;

            float squintMorphValueRight = Mathf.Clamp(maxSquintValueRight * 0.05f, 0, 1.0f);
            rightSquintMorph.morphValue += (squintMorphValueRight - rightSquintMorph.morphValue) * Time.deltaTime * squintSpeed.val;

            float maxSquintValue = Mathf.Max(maxSquintValueLeft, maxSquintValueRight);
            float targetDilation = Mathf.Clamp(1.0f - maxSquintValue * 0.1f, -1, 1);
            pupilDilationMorph.morphValue += (targetDilation - pupilDilationMorph.morphValue) * Time.deltaTime * dilationSpeed.val;
        }
    }
}
