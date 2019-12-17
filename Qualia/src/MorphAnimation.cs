using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using System.Linq;
using UnityEngine.Events;

namespace DeluxePlugin.Qualia
{
    public class MorphAnimation : MVRScript
    {
        public static readonly Dictionary<char, float> valueLookup = new Dictionary<char, float>
        {
            { '.', 0.0f },
            { 'o', 0.5f },
            { 'O', 1.0f },
        };

        class Animation
        {
            public DAZMorph morph;
            public AnimationCurve curve;
            public float duration = 0;

            public Animation(DAZMorph morph, string animString, float scale)
            {
                this.morph = morph;

                List<char> frames = new List<char>(animString);

                List<Keyframe> keyframes = new List<Keyframe>();

                Keyframe? lastKeyframe = null;
                char lastChar = '\0';
                float time = 0;
                float timeIncrement = 0.08f;
                //float inTangent = 0.0f;
                //float outTangent = 0.0f;
                //float inWeight = 0.0f;
                //float outWeight = 0.0f;
                foreach (var character in frames)
                {
                    if (valueLookup.ContainsKey(character) == false)
                    {
                        SuperController.LogError("Morphanimation: Bad frame character: " + character);
                        throw new Exception("Bad frame character: " + character);
                    }

                    float value = valueLookup[character] * scale;

                    if (lastKeyframe == null)
                    {
                        lastChar = character;
                        lastKeyframe = new Keyframe(0, value);
                        keyframes.Add(lastKeyframe.Value);
                    }

                    // character change
                    if (lastChar != character)
                    {
                        //keyframes.Add(new Keyframe(time - timeIncrement, lastKeyframe.Value.value));

                        lastKeyframe = new Keyframe(time, value);
                        keyframes.Add(lastKeyframe.Value);

                        lastChar = character;
                    }

                    time += timeIncrement;
                }

                duration = time - timeIncrement;

                //foreach(var k in keyframes)
                //{
                //    Debug.Log(k.time + " " + k.value);
                //}

                curve = new AnimationCurve(keyframes.ToArray());

                for(int i=0; i<curve.keys.Length; i++)
                {
                    curve.SmoothTangents(i, 0.5f);
                }

            }
        }

        class Playback
        {
            List<Animation> animations;
            Coroutine coroutine;
            float startTime = 0;
            float endTime = 0;

            public Playback(JSONClass json, GenerateDAZMorphsControlUI morphControl)
            {
                float scale = 1;
                if (json.HasKey("scale"))
                {
                    scale = json["scale"].AsFloat;
                }

                var keys = json["keys"].AsObject;
                List<Animation> animations = new List<Animation>();
                foreach (var morphName in keys.Keys.ToList())
                {
                    var animString = keys[morphName].Value;
                    var morph = morphControl.GetMorphByDisplayName(morphName);
                    if (morph == null)
                    {
                        SuperController.LogError("MorphAnimation: morph not found: " + morphName);
                        continue;
                    }

                    animations.Add(new Animation(morph, animString, scale));
                }

                this.animations = animations;
            }

            public void Play()
            {
                if (coroutine != null)
                {
                    SuperController.singleton.StopCoroutine(coroutine);
                }
                startTime = Time.time;
                endTime = Time.time + animations[0].duration;
                coroutine = SuperController.singleton.StartCoroutine(FrameAdvance());
            }

            IEnumerator FrameAdvance()
            {
                while (Time.time < endTime)
                {
                    float elapsed = Time.time - startTime;
                    foreach(var animation in animations)
                    {
                        float value = animation.curve.Evaluate(elapsed);
                        animation.morph.morphValue += (value - animation.morph.morphValue) * Time.deltaTime * 150f;
                    }
                    yield return new WaitForEndOfFrame();
                }

                foreach(var animation in animations)
                {
                    animation.morph.morphValue = 0;
                }
                //SuperController.singleton.StopCoroutine(coroutine);
            }
        }

        List<Playback> affectations = new List<Playback>();
        float nextAffectationTime = Time.time;

        public override void Init()
        {
            JSONStorable geometry = containingAtom.GetStorableByID("geometry");
            DAZCharacterSelector character = geometry as DAZCharacterSelector;
            GenerateDAZMorphsControlUI morphControl = character.morphsControlUI;

            var pluginPath = GetPluginPath();
            var path = pluginPath + "/animations";
            var files = SuperController.singleton.GetFilesAtPath(path);
            foreach(var file in files)
            {
                var fileName = PathExtQualia.GetFileNameWithoutExtension(file);
                CreateButton("Test " + fileName).button.onClick.AddListener(() =>
                {
                    var json = JSON.Parse(SuperController.singleton.ReadFileIntoString(file)).AsObject;
                    Playback pb = new Playback(json, morphControl);
                    pb.Play();
                });
            }

            List<string> twitches = new List<string>()
            {
                "cheekcrease",
                "cheekeyeflex",
                "cheekflexleft",
                "cheekflexright",
                "lipspart",
                "nosewrinkle",
                "nostrilflare",
                "smallsmile",
                "mediumsmile",
                "bigsmile",
            };

            foreach(var twitchFile in twitches)
            {
                var p = pluginPath + "/animations/" + twitchFile + ".json";
                var json = JSON.Parse(SuperController.singleton.ReadFileIntoString(p)).AsObject;
                affectations.Add(new Playback(json, morphControl));
            }
        }

        void Update()
        {
            if(Time.time > nextAffectationTime)
            {
                nextAffectationTime = Time.time + UnityEngine.Random.Range(2.0f, 5.0f);
                int index = UnityEngine.Random.Range(0, affectations.Count);
                affectations[index].Play();
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
