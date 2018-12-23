using System;
using System.Linq;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;

namespace DeluxePlugin.EmotionEngine
{
    public class ExpressionCreator: MVRScript
    {

        public Dictionary<string, string> morphGroupMap = new Dictionary<string, string>()
        {
            { "Brow", "Pose Controls/Head/Brow" },
            { "Cheeks", "Pose Controls/Head/Cheeks and Jaw" },
            { "Expressions", "Pose Controls/Head/Expressions" },
            { "Eyes", "Pose Controls/Head/Eyes" },
            { "Mouth", "Pose Controls/Head/Mouth" },
            { "Lips", "Pose Controls/Head/Mouth/Lips" },
            { "Tongue", "Pose Controls/Head/Mouth/Tongue" },
            { "Nose", "Pose Controls/Head/Nose" },
            { "Visemes", "Pose Controls/Head/Visemes" },
            //{ "Breathe", "Pose Controls/Chest/Breathe" }
        };

        NamedAudioClip currentClip = null;

        JSONStorableStringChooser audioChoice;

        JSONStorableFloat playback;
        JSONStorableFloat totalDuration;

        float testStartTime = 0.0f;

        List<MorphSelectUI> morphSelectors = new List<MorphSelectUI>();

        public List<ExpressionStep> steps = new List<ExpressionStep>();
        ExpressionStep selectedStep = null;

        bool isTesting = false;

        UIDynamicButton createKeyButton = null;
        UIDynamicButton deleteButton = null;
        UIDynamic footerSpacer = null;
        bool morphsCollapsed = false;

        bool deleteConfirmation = false;

        string PATH_WHEN_LOADED = "";


        public override void Init()
        {
            try
            {
                PATH_WHEN_LOADED = SuperController.singleton.currentLoadDir;

                SuperController sc = SuperController.singleton;

                CreateButton("New").button.onClick.AddListener(() =>
                {
                    audioChoice.SetVal("");
                    RemoveAllSteps();
                    RemoveAllSelectors();
                    CreateStep(0);
                    CreateStep(1);
                    playback.SetVal(0);
                    totalDuration.SetVal(totalDuration.defaultVal);
                });

                CreateButton("Save").button.onClick.AddListener(() =>
                {

                    if (steps.Count <= 0)
                    {
                        return;
                    }

                    List<ExpressionKeyframe> keyframes = steps.Select((step) => step.ToKeyFrame()).ToList();

                    string audioUID = "";
                    if (currentClip != null)
                    {
                        audioUID = currentClip.uid;
                    }

                    ExpressionAnimation animation = new ExpressionAnimation()
                    {
                        audio = audioUID,
                        duration = totalDuration.val,
                        keyframes = keyframes
                    };

                    JSONNode json = animation.GetJSON();
                    //Debug.Log(json.ToString());

                    //sc.activeUI = SuperController.ActiveUI.None;
                    sc.fileBrowserUI.defaultPath = PATH_WHEN_LOADED;
                    sc.fileBrowserUI.SetTextEntry(true);

                    sc.fileBrowserUI.Show((path) =>
                    {
                        //  cancel or invalid
                        if (string.IsNullOrEmpty(path))
                        {
                            return;
                        }

                        //  ensure extension
                        if (!path.EndsWith(".json"))
                        {
                            path += ".json";
                        }

                        sc.SaveStringIntoFile(path, json.ToString(""));
                        SuperController.LogMessage("Wrote expression file: " + path);
                    });

                    //  set default filename
                    if (sc.fileBrowserUI.fileEntryField != null)
                    {
                        sc.fileBrowserUI.fileEntryField.text = currentClip.uid + ".json";
                        sc.fileBrowserUI.ActivateFileNameField();
                    }


                });
                CreateSpacer().height = 12;

                CreateButton("Load", true).button.onClick.AddListener(() =>
                {
                    sc.fileBrowserUI.defaultPath = PATH_WHEN_LOADED;
                    sc.fileBrowserUI.SetTextEntry(false);
                    sc.fileBrowserUI.Show((path) =>
                    {
                        if (string.IsNullOrEmpty(path))
                        {
                            return;
                        }

                        string jsonString = sc.ReadFileIntoString(path);

                        /*
                        Example:

                        {
                           "audio" : "holding it 1.mp3",
                           "keyframes" : [
                              {
                                 "time" : "0",
                                 "Brow Down" : "0"
                              },
                              {
                                 "time" : "1",
                                 "Brow Down" : "1"
                              }
                           ]
                        }

                        */

                        JSONClass jsc = JSON.Parse(jsonString).AsObject;


                        string clipId = jsc["audio"].Value;

                        //  validate this audio clip exists
                        NamedAudioClip nac = URLAudioClipManager.singleton.GetClip(clipId);
                        if (nac == null)
                        {
                            //SuperController.LogError("Clip " + clipId + " is not loaded to scene. Add it first.");
                            //return;
                        }

                        audioChoice.SetVal(jsc["audio"].Value);
                        totalDuration.SetVal(jsc["duration"].AsFloat);

                        RemoveAllSteps();
                        RemoveAllSelectors();

                        JSONArray keyframes = jsc["keyframes"].AsArray;

                        HashSet<DAZMorph> morphSet = new HashSet<DAZMorph>();
                        GenerateDAZMorphsControlUI morphControl = GetMorphControl();

                        for (int i = 0; i < keyframes.Count; i++)
                        {
                            JSONClass keyframe = keyframes[i].AsObject;
                            float time = keyframe["time"].AsFloat;

                            ExpressionStep step = CreateStep(time);

                            keyframe.Keys.ToList().ForEach((key =>
                            {
                                if (key == "time")
                                {
                                    return;
                                }

                                string morphName = key;

                                DAZMorph morph = morphControl.GetMorphByDisplayName(morphName);
                                if (morph == null)
                                {
                                    SuperController.LogError("Expression has a morph not imported: " + morphName);
                                    return;
                                }

                                if (morphSet.Contains(morph)==false)
                                {
                                    morphSet.Add(morph);
                                    MorphSelectUI selector = CreateMorphSelector();
                                    selector.morphString.SetVal(morphName);
                                    selector.SetCollapse(morphsCollapsed);
                                }

                                float value = keyframe[morphName].AsFloat;
                                step.morphKeyframe[morph] = value;
                            }));
                        }

                        playback.SetVal(0);
                    });

                });
                CreateSpacer(true).height = 12;

                totalDuration = new JSONStorableFloat("duration", 2.0f, (float newDuration)=>
                {
                },0.0f, 10.0f, false);

                UIDynamicSlider slider = CreateSlider(totalDuration);
                slider.label = "Duration (based on audio clip if selected)";

                audioChoice = new JSONStorableStringChooser("clip", GetAudioClipIds(), "", "Audio Clip", (string clipId)=>{
                    NamedAudioClip nac = URLAudioClipManager.singleton.GetClip(clipId);
                    if (nac == null)
                    {
                        currentClip = null;
                        return;
                    }

                    currentClip = nac;
                    PlayAudioClipAtT(nac, 0);

                    totalDuration.SetVal(nac.sourceClip.length);
                });
                //RegisterStringChooser(audioChoice);

                UIDynamicPopup popup = CreateScrollablePopup(audioChoice);
                popup.popup.onOpenPopupHandlers += () =>
                {
                    audioChoice.choices = GetAudioClipIds();
                };

                playback = new JSONStorableFloat("time", 0, (float t) =>
                 {
                     if (isTesting == false)
                     {
                         if (currentClip != null)
                         {
                             SampleAudioClipAtT(currentClip, t);
                         }
                         SetMorphAtT(t);
                     }

                     createKeyButton.label = "Key At " + t;

                     CancelDeleteConfirmation();

                 }, 0, 1, true, true);

                UIDynamicSlider playbackSlider = CreateSlider(playback);
                playbackSlider.quickButtonsEnabled = false;
                playbackSlider.rangeAdjustEnabled = false;
                playbackSlider.defaultButtonEnabled = false;

                CreateButton("Test").button.onClick.AddListener(() =>
                {
                    if(isTesting == true)
                    {
                        isTesting = false;
                        return;
                    }

                    isTesting = true;
                    testStartTime = Time.time;
                    if (currentClip != null)
                    {
                        PlayAudioClipAtT(currentClip, 0);
                    }

                    CancelDeleteConfirmation();
                });

                createKeyButton = CreateButton("Key At 0");
                createKeyButton.button.onClick.AddListener(()=> {
                    CreateStep(playback.val);
                });

                CreateSpacer();


                UIDynamicButton addMorphButton = CreateButton("Add Morph", true);
                addMorphButton.button.onClick.AddListener(() =>
                {
                    CreateMorphSelector();
                });

                UIDynamicButton minimizeViewButton = CreateButton("Minimize Morph Controls", true);
                minimizeViewButton.button.onClick.AddListener(() =>
                {
                    morphsCollapsed = !morphsCollapsed;
                    morphSelectors.ForEach((selector) =>
                    {
                        selector.SetCollapse(morphsCollapsed);
                    });

                    CancelDeleteConfirmation();

                    minimizeViewButton.label = morphsCollapsed ? "Maximize Morph Controls" : "Minimize Morph Controls";
                });

                deleteButton = CreateButton("Delete Selected", false);
                deleteButton.button.onClick.AddListener(() =>
                {
                    if (selectedStep == null)
                    {
                        return;
                    }

                    if (deleteConfirmation == false)
                    {
                        BeginDeleteConfirmation();
                        return;
                    }
                    else
                    {
                        RemoveStep(selectedStep);
                        CancelDeleteConfirmation();
                    }
                });

                CreateStep(0);
                CreateStep(1);
                playback.SetVal(0);

            }
            catch (Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }
        }

        void Update()
        {
            if (isTesting)
            {
                float timeElapsed = Time.time - testStartTime;
                float t = timeElapsed / totalDuration.val;
                t = Mathf.Clamp01(t);

                playback.val = t;
                SetMorphAtT(t);

                if (t >= 1)
                {
                    isTesting = false;
                }

            }
        }

        ExpressionStep CreateStep(float t)
        {
            ExpressionStep newStep = new ExpressionStep(this, t);
            steps.Add(newStep);

            if (steps.Count == 0)
            {
                morphSelectors.ForEach((selector) =>
                {
                    if (selector.selectedMorph != null)
                    {
                        newStep.morphKeyframe[selector.selectedMorph] = selector.selectedMorph.startValue;
                    }
                });
            }
            else
            {
                morphSelectors.ForEach((selector) =>
                {
                    if (selector.selectedMorph != null)
                    {
                        newStep.morphKeyframe[selector.selectedMorph] = selector.morphValue.val;
                    }
                });
            }

            ReorderSteps();
            SelectStep(newStep);
            CancelDeleteConfirmation();
            return newStep;
        }

        MorphSelectUI CreateMorphSelector()
        {
            MorphSelectUI morphSelect = new MorphSelectUI(this);
            morphSelectors.Add(morphSelect);
            RegenerateFooter();

            CancelDeleteConfirmation();
            return morphSelect;
        }

        void SetMorphAtT(float t)
        {
            if (steps.Count <= 0)
            {
                return;
            }

            int current = GetPrevStep(t);
            ExpressionStep prevStep = steps[current];
            ExpressionStep nextStep = steps[Math.Min(current + 1, steps.Count-1)];
            prevStep.morphKeyframe.Keys.ToList().ForEach((morph) =>
            {
                float morphValue = GetValueBetweenSteps(t, morph, prevStep, nextStep);
                //morphSelectors.ForEach((selector) =>
                //{
                //    if (selector.selectedMorph == morph)
                //    {
                //        selector.morphValue.SetVal(morphValue);
                //    }
                //});
                morph.SetValue(morphValue);
            });
        }

        float GetValueBetweenSteps(float t, DAZMorph morph, ExpressionStep prevStep, ExpressionStep nextStep)
        {
            float prevValue = prevStep.morphKeyframe[morph];
            float nextValue = nextStep.morphKeyframe[morph];
            float prevT = prevStep.t;
            float nextT = nextStep.t;

            float tToPrev = t - prevT;
            float deltaT = nextT - prevT;
            if (deltaT <= 0)
            {
                return prevValue;
            }

            float morphAlpha = tToPrev / deltaT;
            float morphValue = prevValue + (nextValue - prevValue) * morphAlpha;
            return morphValue;
        }

        int GetPrevStep(float t)
        {
            for(int i=0; i < steps.Count; i++)
            {
                ExpressionStep step = steps[i];
                if(step.t >= t)
                {
                    return Math.Max(i - 1, 0);
                }
            }
            return steps.Count - 1;
        }

        void ReorderSteps()
        {
            steps = steps.OrderBy((ExpressionStep expressionStep) =>
            {
                return expressionStep.t;
            }).ToList();


            steps.ForEach((step) =>
            {
                step.RemoveSelectButton();
            });

            steps.ForEach((step) =>
            {
                step.CreateSelectButton();
            });
        }

        List<string> GetAudioClipIds()
        {
            List<NamedAudioClip> clips = URLAudioClipManager.singleton.GetCategoryClips("web");

            //  it could be null here......
            if (clips == null)
            {
                return new List<string>();
            }
            if (clips.Count == 0)
            {
                return new List<string>();
            }
            return clips.Select((clip) =>
            {
                return clip.uid;
            }).ToList();
        }

        public void RemoveStep(ExpressionStep step)
        {
            step.Remove();
            steps.Remove(step);
            if (selectedStep == step)
            {
                selectedStep = null;
            }
        }

        public void RemoveAllSteps()
        {
            steps.ForEach((step) =>
            {
                step.Remove();
            });

            steps.Clear();
            selectedStep = null;
        }

        public void RemoveAllSelectors()
        {
            //  have to make copy because remove also removes from the list via callback
            List<MorphSelectUI> copy = new List<MorphSelectUI>();
            morphSelectors.ForEach((selector) =>
            {
                copy.Add(selector);
            });

            morphSelectors.Clear();

            copy.ForEach((selector) =>
            {
                selector.Remove();
            });
        }

        public void DeselectAll()
        {
            steps.ForEach((step) =>
            {
                step.OnDeselect();
            });
        }

        public void SelectStep(ExpressionStep step)
        {
            DeselectAll();

            selectedStep = step;

            morphSelectors.ForEach((selector) =>
            {
                selector.OnSelect(step);
            });

            step.OnSelect();

            playback.SetVal(step.t);

            if (currentClip != null)
            {
                SampleAudioClipAtT(currentClip, step.t);
            }

            step.ApplyMorphs();

            CancelDeleteConfirmation();
        }

        void PlayAudioClipAtT(NamedAudioClip clip, float t)
        {
            if (clip == null)
            {
                Debug.Log("clip is null");
                return;
            }
            AudioSource testAudioSource = URLAudioClipManager.singleton.testAudioSource;
            testAudioSource.Stop();
            testAudioSource.time = clip.clipToPlay.length * t;
            URLAudioClipManager.singleton.TestClip(clip);
        }

        void SampleAudioClipAtT(NamedAudioClip clip, float t)
        {
            AudioSource testAudioSource = URLAudioClipManager.singleton.testAudioSource;
            testAudioSource.Stop();
            testAudioSource.time = clip.clipToPlay.length * t;
            URLAudioClipManager.singleton.TestClip(clip);
            testAudioSource.SetScheduledEndTime(AudioSettings.dspTime + 0.2f);
        }

        public GenerateDAZMorphsControlUI GetMorphControl()
        {
            DAZCharacterSelector dcs = containingAtom.GetStorableByID("geometry") as DAZCharacterSelector;
            GenerateDAZMorphsControlUI morphUI = dcs.morphsControlUI;
            return morphUI;
        }

        public List<string> GetMorphNamesByGroup(string group)
        {
            List<string> names = new List<string>();


            GenerateDAZMorphsControlUI morphControl = GetMorphControl();
            List<string> morphNames = morphControl.GetMorphDisplayNames();

            return names;
        }

        void OnDestroy()
        {
            DeselectAll();
            morphSelectors.ForEach((selector) =>
            {
                if (selector.selectedMorph != null)
                {
                    selector.selectedMorph.SetValue(selector.selectedMorph.startValue);
                }
            });
        }

        public List<string> GetExistingMorphNames()
        {
            HashSet<string> existingMorphNames = new HashSet<string>();
            steps.ForEach((step) =>
            {
                step.morphKeyframe.Keys.Select((morph) =>
                {
                    return morph.displayName;
                })
                .ToList()
                .ForEach((name) =>
                {
                    existingMorphNames.Add(name);
                });
            });

            return existingMorphNames.ToList();
        }

        public void OnMorphSelected(DAZMorph morph)
        {
            steps.ForEach((step) =>
            {
                step.morphKeyframe[morph] = 0;
            });
        }

        public void OnMorphValueChanged(DAZMorph morph, float value)
        {
            if (selectedStep != null)
            {
                selectedStep.morphKeyframe[morph] = value;
            }
        }

        public void OnMorphRemoved(DAZMorph morph)
        {
            steps.ForEach((step) =>
            {
                step.morphKeyframe.Remove(morph);
            });
        }

        public void OnMorphSelectorRemoved(MorphSelectUI selector)
        {
            morphSelectors.Remove(selector);
        }

        public void RegenerateFooter()
        {
            if (footerSpacer != null)
            {
                RemoveSpacer(footerSpacer);
            }

            footerSpacer = CreateSpacer(true);
            footerSpacer.height = 400.0f;
        }

        void BeginDeleteConfirmation()
        {
            deleteButton.label = "Press Again To Delete";
            deleteConfirmation = true;
        }

        void CancelDeleteConfirmation()
        {
            deleteButton.label = "Delete Selected";
            deleteConfirmation = false;
        }
    }


}
