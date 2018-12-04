using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;

namespace DeluxePlugin
{
    class HipThrust : MVRScript
    {

        JSONStorableFloat thrustAmount;
        JSONStorableFloat grindAmount;

        Atom thrustAtom;
        CycleForceProducerV2 cycleForce;
        string THRUST_FORCE_NAME = "Hip Thrust Force";

        Atom audioSourceAtom;
        AudioSourceControl audioSource;
        string AUDIO_SOURCE_NAME = "Thrust Audio";
        float nextSFXTime = 0.0f;

        JSONStorableStringChooser skinSlapSFX;
        NamedAudioClip skinSlapClip;

        JSONStorableStringChooser grindSFX;
        NamedAudioClip grindClip;

        public float thrustValue
        {
            get
            {
                if (thrustAmount == null)
                {
                    return 0;
                }
                else
                {
                    return thrustAmount.val;
                }
            }
        }

        public float grindValue
        {
            get
            {
                if(grindAmount == null)
                {
                    return 0;
                }
                else
                {
                    return grindAmount.val;
                }
            }
        }

        public override void Init()
        {
            try
            {
                THRUST_FORCE_NAME += ":" + containingAtom.name;
                AUDIO_SOURCE_NAME += ":" + containingAtom.name;

                /*
                List<string> peopleIds = SuperController.singleton.GetAtoms()
                .Where(atom => atom.GetStorableByID("geometry") != null && (atom != containingAtom))
                .Select(atom => atom.name).ToList();

                JSONStorableStringChooser receiverChoiceJSON = new JSONStorableStringChooser("target", peopleIds, peopleIds.Count>0?peopleIds[0]:null, "Thrust Target", delegate (string receiverName)
                {
                    Atom receiver = GetAtomById(receiverName);
                    StartCoroutine(CreateThrustForce(receiver));
                });
                receiverChoiceJSON.storeType = JSONStorableParam.StoreType.Full;
                UIDynamicPopup dp = CreateScrollablePopup(receiverChoiceJSON, false);
                RegisterStringChooser(receiverChoiceJSON);
                */

                StartCoroutine(CreateThrustForce());

                CreateButton("Select Thrust Force", true).button.onClick.AddListener(() =>
                {
                    if (thrustAtom != null)
                    {
                        SelectController(thrustAtom.mainController);
                    }
                });


                thrustAmount = new JSONStorableFloat("thrustAmount", 0f, (float value)=> { }, 0f, 1f, true, true);
                thrustAmount.storeType = JSONStorableParam.StoreType.Full;
                RegisterFloat(thrustAmount);
                CreateSlider(thrustAmount,true);

                grindAmount = new JSONStorableFloat("grindAmount", 0f, (float value) => { }, 0f, 1f, true, true);
                thrustAmount.storeType = JSONStorableParam.StoreType.Full;
                RegisterFloat(grindAmount);
                CreateSlider(grindAmount, true);

                skinSlapSFX = new JSONStorableStringChooser("SkinSlapSound", GetAllAudioClipIds(), null, "Skin Slap Sound", (string uid)=>{
                    skinSlapClip = URLAudioClipManager.singleton.GetClip(uid);
                });
                skinSlapSFX.storeType = JSONStorableParam.StoreType.Full;
                CreateScrollablePopup(skinSlapSFX).popup.onOpenPopupHandlers += () =>
                {
                    skinSlapSFX.choices = GetAllAudioClipIds();
                };
                RegisterStringChooser(skinSlapSFX);

                grindSFX = new JSONStorableStringChooser("GrindSoundFile", GetAllAudioClipIds(), null, "Grind Sound", (string uid) => {
                    grindClip = URLAudioClipManager.singleton.GetClip(uid);
                });
                grindSFX.storeType = JSONStorableParam.StoreType.Full;
                CreateScrollablePopup(grindSFX).popup.onOpenPopupHandlers += () =>
                {
                    grindSFX.choices = GetAllAudioClipIds();
                };
                RegisterStringChooser(grindSFX);

                StartCoroutine(CreateAudioSource());
            }
            catch (Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }
        }



        IEnumerator CreateThrustForce()
        {
            //  need to wait a bit before attempting to create in case we are reloading the script and the previous hip thrust already exists
            yield return new WaitForSeconds(0.5f);

            thrustAtom = GetAtomById(THRUST_FORCE_NAME);
            if (thrustAtom == null)
            {
                SuperController.singleton.StartCoroutine(SuperController.singleton.AddAtomByType("CycleForce", THRUST_FORCE_NAME, true));
                yield return new WaitWhile(() => GetAtomById(THRUST_FORCE_NAME) != null);
                thrustAtom = GetAtomById(THRUST_FORCE_NAME);
            }

            SetupThrustTransform(thrustAtom);
        }

        void SetupThrustTransform(Atom thrustAtom)
        {
            CycleForceProducerV2 cf = thrustAtom.GetStorableByID("ForceProducer") as CycleForceProducerV2;
            cf.SetForceReceiver(containingAtom.name + ":hip");

            FreeControllerV3 cfControl = thrustAtom.mainController;
            FreeControllerV3 headControl = containingAtom.freeControllers.First((controller) => controller.name == "headControl");
            FreeControllerV3 hipControl = containingAtom.freeControllers.First((controller) => controller.name == "hipControl");

            cfControl.transform.SetPositionAndRotation(hipControl.transform.position, hipControl.transform.rotation);

            cfControl.transform.LookAt(headControl.transform);
            cfControl.transform.Rotate(0, 180, 0, Space.Self);
            cfControl.transform.Rotate(new Vector3(0, 0, 1), 90, Space.Self);
            cfControl.transform.Rotate(new Vector3(1, 0, 0), -90, Space.Self);
            cfControl.transform.Rotate(new Vector3(0, 0, 1), 90, Space.Self);

            cfControl.currentPositionState = FreeControllerV3.PositionState.ParentLink;
            cfControl.currentRotationState = FreeControllerV3.RotationState.ParentLink;

            Rigidbody rb = SuperController.singleton.RigidbodyNameToRigidbody(containingAtom.name + ":hipControl");
            cfControl.SelectLinkToRigidbody(rb, FreeControllerV3.SelectLinkState.PositionAndRotation);

            HideController(thrustAtom);

            cycleForce = cf;
        }


        IEnumerator CreateAudioSource()
        {
            yield return new WaitForSeconds(0.5f);

            audioSourceAtom = GetAtomById(AUDIO_SOURCE_NAME);
            if (audioSourceAtom == null)
            {
                SuperController.singleton.StartCoroutine(SuperController.singleton.AddAtomByType("AudioSource", AUDIO_SOURCE_NAME, true));
                yield return new WaitWhile(() => GetAtomById(AUDIO_SOURCE_NAME) != null);
                audioSourceAtom = GetAtomById(AUDIO_SOURCE_NAME);
            }

            if (audioSourceAtom != null)
            {
                audioSource = audioSourceAtom.GetStorableByID("AudioSource") as AudioSourceControl;
                if (audioSource != null)
                {
                    FreeControllerV3 hipControl = containingAtom.freeControllers.First((controller) => controller.name == "hipControl");
                    if (hipControl != null)
                    {
                        FreeControllerV3 sfxControl = audioSourceAtom.mainController;
                        sfxControl.transform.SetPositionAndRotation(hipControl.transform.position, hipControl.transform.rotation);
                        sfxControl.currentPositionState = FreeControllerV3.PositionState.ParentLink;
                        sfxControl.currentRotationState = FreeControllerV3.RotationState.ParentLink;
                        Rigidbody rb = SuperController.singleton.RigidbodyNameToRigidbody("Person:hipControl");
                        sfxControl.SelectLinkToRigidbody(rb, FreeControllerV3.SelectLinkState.PositionAndRotation);

                        audioSource.volume = 0.5f;
                    }
                }
                HideController(audioSourceAtom);
            }
        }


        void Update()
        {

            if (cycleForce == null)
            {
                return;
            }

            if (thrustAmount.val > 0)
            {
                cycleForce.forceQuickness = Remap(thrustAmount.val, 0.0f, 1.0f, 0.7f, 6.0f);
                cycleForce.forceFactor = Remap(thrustAmount.val, 0.0f, 1.0f, 500, 600);
                cycleForce.forceDuration = Remap(thrustAmount.val, 0.0f, 1.0f, 0.9f, 1.0f);
            }
            else
            {
                cycleForce.forceQuickness = 0.0f;
            }


            if (grindAmount.val > 0)
            {
                cycleForce.torqueFactor = Remap(grindAmount.val, 0.0f, 1.0f, 0.0f, 120.0f);
                cycleForce.torqueQuickness = Remap(grindAmount.val, 0.0f, 1.0f, 0.7f, 6.0f);
            }
            else
            {
                cycleForce.torqueQuickness = 0.0f;
            }

            cycleForce.period = Remap(Mathf.Clamp01(thrustAmount.val + grindAmount.val), 0.0f, 1.0f, 2.2f, 0.3f);

            if (audioSource != null)
            {
                if (Time.fixedTime > nextSFXTime && (grindAmount.val> 0 || thrustAmount.val> 0))
                {
                    if (cycleForce.forceQuickness > 4.4f)
                    {
                        if (skinSlapClip != null)
                        {
                            audioSource.pitch = UnityEngine.Random.Range(0.95f, 1.05f);
                            audioSource.PlayNow(skinSlapClip);
                        }
                    }
                    else
                    if (cycleForce.forceQuickness > 0.5f || (cycleForce.torqueFactor > 1.0f && cycleForce.appliedTorque.magnitude >= 10.0f))
                    {
                        if (grindClip != null)
                        {
                            audioSource.pitch = UnityEngine.Random.Range(0.95f, 1.05f);
                            audioSource.PlayNow(grindClip);
                        }
                    }
                    nextSFXTime = Time.fixedTime + cycleForce.period;
                }
            }
        }

        void OnDestroy()
        {
            if (thrustAtom != null)
            {
                SuperController.singleton.RemoveAtom(thrustAtom);
                thrustAtom = null;
            }

            if (audioSourceAtom != null)
            {
                SuperController.singleton.RemoveAtom(audioSourceAtom);
                audioSourceAtom = null;
            }
        }

        private float Remap(float x, float x1, float x2, float y1, float y2)
        {
            var m = (y2 - y1) / (x2 - x1);
            var c = y1 - m * x1; // point of interest: c is also equal to y2 - m * x2, though float math might lead to slightly different results.

            return m * x + c;
        }

        private List<string> GetAllAudioClipIds()
        {
            return URLAudioClipManager.singleton.GetCategoryClips("web").Select((clip) => clip.uid).ToList();
        }

        public void HideController(Atom atom)
        {
            FreeControllerV3 controller = atom.GetStorableByID("control") as FreeControllerV3;
            controller.deselectedMeshScale = 0.000f;

            SphereCollider collider = controller.GetComponent<SphereCollider>();
            collider.radius = 0.0f;
            collider.enabled = false;
        }

        public float GetHipIntensity()
        {
            return Mathf.Max(thrustValue, grindValue);
        }
    }
}
