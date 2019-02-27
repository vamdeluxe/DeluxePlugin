using System;
using System.Linq;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;

namespace DeluxePlugin.EmotionEngine
{
    [Serializable]
    public class ExpressionAnimation
    {
        const float ANIMATION_LERP_RATE = 30.0f;
        const float TRANSITION_ANIMATION_DURATION = 0.1f;
        const float FADE_OUT_TIME = 0.8f;
        const float COOLDOWN_DELAY = 4.0f;

        public string audio = "";
        public NamedAudioClip nac;
        public float duration = 0;

        public List<ExpressionKeyframe> keyframes = new List<ExpressionKeyframe>();

        public ExpressionAnimation() { }
        GenerateDAZMorphsControlUI morphControl;

        public ExpressionAnimation(GenerateDAZMorphsControlUI morphControl, string jsonString)
        {
            this.morphControl = morphControl;

            JSONClass obj = JSON.Parse(jsonString).AsObject;
            audio = obj["audio"].Value;
            duration = obj["duration"].AsFloat;
            JSONArray keyframesData = obj["keyframes"].AsArray;
            for(int i=0; i<keyframesData.Count; i++)
            {
                ExpressionKeyframe keyframe = new ExpressionKeyframe();

                JSONClass keyframeData = keyframesData[i].AsObject;

                keyframeData.Keys.ToList().ForEach((keyName) =>
                {
                    float value = keyframeData[keyName].AsFloat;
                    if (keyName == "time")
                    {
                        keyframe.time = value;
                    }
                    else
                    {
                        keyframe.morphValues[keyName] = value;
                    }
                });

                keyframes.Add(keyframe);
            }

            nac = URLAudioClipManager.singleton.GetClip(audio);
        }

        public ExpressionAnimation(GenerateDAZMorphsControlUI morphControl, ExpressionAnimation stopping, ExpressionAnimation starting)
        {
            this.morphControl = morphControl;

            ExpressionKeyframe stoppingFrame = stopping.keyframes[0];
            ExpressionKeyframe startingFrame = starting.keyframes[0];

            List<string> stoppingMorphs = stoppingFrame.morphValues.Keys.ToList();
            List<string> startingMorphs = startingFrame.morphValues.Keys.ToList();
            List<string> notInStarting = stoppingMorphs.Where((stoppingName) =>
            {
                return (startingMorphs.Contains(stoppingName) == false);
            }).ToList();

            ExpressionKeyframe startFrame = new ExpressionKeyframe()
            {
                time = 0
            };
            ExpressionKeyframe endFrame = new ExpressionKeyframe()
            {
                time = 1
            };

            notInStarting.ForEach((morphName) =>
            {
                DAZMorph morph = morphControl.GetMorphByDisplayName(morphName);
                startFrame.morphValues[morphName] = morph.appliedValue;
                endFrame.morphValues[morphName] = morph.startValue;
            });

            keyframes.Add(startFrame);
            keyframes.Add(endFrame);
            duration = TRANSITION_ANIMATION_DURATION;
        }

        public JSONClass GetJSON()
        {
            JSONClass obj = new JSONClass();

            obj["audio"] = audio;
            obj["duration"] = duration.ToString();

            JSONArray keyframesArray = new JSONArray();
            keyframes.ForEach((keyframe) =>
            {
                keyframesArray.Add(keyframe.GetJSON());
            });
            obj["keyframes"] = keyframesArray;

            return obj;
        }


        float playStartTime = 0;
        public bool isPlaying = false;

        public void Start()
        {
            if (keyframes.Count <= 0)
            {
                isPlaying = false;
                return;
            }
            isFading = false;
            isPlaying = true;
            playStartTime = Time.time;
        }

        public void Stop()
        {
            if (keyframes.Count <= 0)
            {
                return;
            }

            isPlaying = false;
            ExpressionKeyframe step = keyframes[0];
            step.morphValues.Keys.ToList().ForEach((morphName) =>
            {
                DAZMorph morph = morphControl.GetMorphByDisplayName(morphName);
                if (morph != null)
                {
                    //morph.SetValue(morph.startValue);
                    morph.SetDefaultValue();
                }
            });
        }

        public bool Update()
        {
            float timeSoFar = Time.time - playStartTime;
            float t = timeSoFar / duration;
            if (t >= 1)
            {
                isPlaying = false;

                //float timeSinceEnded = Time.time - (playStartTime + duration);
                //if (timeSinceEnded >= COOLDOWN_DELAY)
                //{
                //    return true;
                //}
                return true;
            }
            SetMorphAtT(t);

            return false;
        }

        public bool isFading = false;
        float fadeStartTime = 0;
        public void StartFadeOut()
        {
            if (keyframes.Count <= 0)
            {
                return;
            }
            isFading = true;
            fadeStartTime = Time.time;
        }

        public bool UpdateFadeOut()
        {
            if (keyframes.Count <= 0)
            {
                return true;
            }

            isFading = true;
            ExpressionKeyframe step = keyframes[0];
            step.morphValues.Keys.ToList().ForEach((morphName) =>
            {
                DAZMorph morph = morphControl.GetMorphByDisplayName(morphName);

                float currentValue = morph.morphValue;
                float morphValue = morph.startValue;
                float lerpedValue = currentValue + (morphValue - currentValue) * Time.deltaTime * ANIMATION_LERP_RATE;
                morph.SetValue(lerpedValue);
            });

            if((Time.time - fadeStartTime) > FADE_OUT_TIME)
            {
                isFading = false;
                return true;
            }

            return false;

        }


        void SetMorphAtT(float t)
        {
            if (keyframes.Count <= 0)
            {
                return;
            }

            int current = GetPrevStep(t);
            ExpressionKeyframe prevStep = keyframes[current];
            ExpressionKeyframe nextStep = keyframes[Math.Min(current + 1, keyframes.Count - 1)];
            prevStep.morphValues.Keys.ToList().ForEach((morphName) =>
            {
                DAZMorph morph = morphControl.GetMorphByDisplayName(morphName);

                float currentValue = morph.appliedValue;
                float morphValue = GetValueBetweenSteps(t, morphName, prevStep, nextStep);
                float lerpedValue = currentValue + (morphValue - currentValue) * Time.deltaTime * ANIMATION_LERP_RATE;
                morph.SetValue(lerpedValue);
            });
        }

        float GetValueBetweenSteps(float t, string morphName, ExpressionKeyframe prevStep, ExpressionKeyframe nextStep)
        {
            float prevValue = prevStep.morphValues[morphName];
            float nextValue = nextStep.morphValues[morphName];
            float prevT = prevStep.time;
            float nextT = nextStep.time;

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
            for (int i = 0; i < keyframes.Count; i++)
            {
                ExpressionKeyframe step = keyframes[i];
                if (step.time >= t)
                {
                    return Math.Max(i - 1, 0);
                }
            }
            return keyframes.Count - 1;
        }
    }

    [Serializable]
    public class ExpressionKeyframe
    {
        public float time = 0;
        public Dictionary<string, float> morphValues = new Dictionary<string, float>();

        public JSONClass GetJSON()
        {
            JSONClass obj = new JSONClass();
            obj["time"] = time.ToString();

            morphValues.Keys.ToList().ForEach((morphName) =>
            {
                float value = morphValues[morphName];
                obj[morphName] = value.ToString();
            });

            return obj;
        }
    }
}
