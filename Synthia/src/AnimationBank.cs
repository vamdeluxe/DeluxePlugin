using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;

namespace DeluxePlugin.Synthia
{
    public class AnimationBank
    {
        public Dictionary<string, Animation> bank = new Dictionary<string, Animation>();

        private Synthia plugin;

        public AnimationBank(Synthia synthia)
        {
            this.plugin = synthia;
        }

        public void Add( Animation animation)
        {
            bank[animation.id] = animation;
        }

        public void Remove(string id)
        {
            bank.Remove(id);
        }

        public Animation Get(string id)
        {
            if (bank.ContainsKey(id) == false)
            {
                return null;
            }
            return bank[id];
        }

        public List<string> GetIdList()
        {
            var list = new List<string>();
            foreach(var entry in bank)
            {
                list.Add(entry.Key);
            }
            return list;
        }
    }

    public class Animation
    {
        Synthia synthia;
        public Animation(string path, Synthia synthia, string id = null)
        {
            if (id != null)
            {
                this.id = id;
            }
            else
            {
                this.id = PathExtSynthia.GetFileNameWithoutExtension(path);
            }

            Load(path);
            endFrame = bvh.nFrames - 1;

            this.synthia = synthia;

            synthia.StartCoroutine(PostConstructor());
        }

        IEnumerator PostConstructor()
        {
            yield return new WaitForEndOfFrame();

            loopStorable = new JSONStorableBool("loop", loop, (bool shouldLoop) =>
            {
                loop = shouldLoop;
            });

            restartOnAnimationChangeStorable = new JSONStorableBool("restart on animation change", restartOnAnimationChange, (bool restart) =>
            {
                restartOnAnimationChange = restart;
            });

            startFrameStorable = new JSONStorableFloat("start frame", startFrame, (float newStartFrame) =>
            {
                startFrame = Mathf.FloorToInt(newStartFrame);
                startFrame = (int)Mathf.Min(startFrame, endFrame);
            }, 0, bvh.nFrames-1, true);

            endFrameStorable = new JSONStorableFloat("end frame", endFrame, (float newEndFrame) =>
            {
                endFrame = Mathf.FloorToInt(newEndFrame);
                endFrame = (int) Mathf.Max(startFrame, endFrame);
            }, 0, bvh.nFrames-1, true);

            rootMotionXZStorable = new JSONStorableBool("root motion", rootMotionXZ, (bool newRootMotion) =>
            {
                rootMotionXZ = newRootMotion;
                SuperController.LogMessage(id + " set root motion " + rootMotionXZ);
            });

            rootMotionYStorable = new JSONStorableBool("root motion (up/down)", rootMotionY, (bool newRootMotion) =>
            {
                rootMotionY = newRootMotion;
            });

            AddControls();
        }

        public void Load(string path)
        {
            this.path = path;
            bvh = new BvhFile(path);
        }

        public string id;
        public string path;
        public BvhFile bvh;
        public float yaw = 0;

        public bool loop = false;
        JSONStorableBool loopStorable;

        public bool restartOnAnimationChange = false;
        JSONStorableBool restartOnAnimationChangeStorable;

        public bool rootMotionXZ = false;
        JSONStorableBool rootMotionXZStorable;

        public bool rootMotionY = false;
        JSONStorableBool rootMotionYStorable;

        public int startFrame = 0;
        JSONStorableFloat startFrameStorable;

        public int endFrame = 0;
        JSONStorableFloat endFrameStorable;

        UIDynamic spacer;
        UIDynamicTextField idLabel;
        UIDynamicToggle loopToggle;
        UIDynamicToggle restartOnAnimationChangeToggle;
        UIDynamicToggle rootMotionXZToggle;
        UIDynamicToggle rootMotionYToggle;
        UIDynamicSlider startFrameSlider;
        UIDynamicSlider endFrameSlider;
        UIDynamicButton playButton;
        UIDynamicButton removeButton;


        public void AddControls()
        {
            spacer = synthia.CreateSpacer();
            spacer.height = 24;

            // TODO id just so happens to match here
            // perhaps actually pass in the id here and let animation hold on to it?


            playButton = synthia.CreateButton("Play " + id);
            playButton.button.onClick.AddListener(() =>
            {
                synthia.ForcePlayAnimation(this);
            });
            playButton.buttonColor = new Color(.3f, 0.9f, 0.33f);


            idLabel = synthia.CreateTextField(new JSONStorableString("id", id));
            idLabel.height = 4;

            loopToggle = synthia.CreateToggle(loopStorable);
            restartOnAnimationChangeToggle = synthia.CreateToggle(restartOnAnimationChangeStorable);
            startFrameSlider = synthia.CreateSlider(startFrameStorable);
            endFrameSlider = synthia.CreateSlider(endFrameStorable);
            rootMotionXZToggle = synthia.CreateToggle(rootMotionXZStorable);
            rootMotionYToggle = synthia.CreateToggle(rootMotionYStorable);

            removeButton = synthia.CreateButton("Remove " + id);
            removeButton.button.onClick.AddListener(() =>
            {
                synthia.animations.Remove(id);
                RemoveControls();
            });
            removeButton.buttonColor = new Color(.8f, 0.3f, 0.2f);
        }

        public void RemoveControls()
        {
            synthia.RemoveTextField(idLabel);
            synthia.RemoveButton(playButton);
            synthia.RemoveToggle(loopToggle);
            synthia.RemoveToggle(restartOnAnimationChangeToggle);
            synthia.RemoveToggle(rootMotionXZToggle);
            synthia.RemoveToggle(rootMotionYToggle);
            synthia.RemoveSlider(startFrameSlider);
            synthia.RemoveSlider(endFrameSlider);
            synthia.RemoveButton(removeButton);
            synthia.RemoveSpacer(spacer);
        }

        public void UpdateDebug()
        {
            if (this.idLabel != null)
            {
                if (synthia.animator.animation == this)
                {
                    idLabel.text = id + " " + synthia.animator.frame + "/" + bvh.nFrames;
                }
                else
                {
                    idLabel.text = id;
                }
            }
        }

        public JSONClass Serialize()
        {
            JSONClass js = new JSONClass();
            js["path"] = path;
            js["id"] = id;
            js["loop"] = loop.ToString();
            js["restartOnAnimationChange"] = restartOnAnimationChange.ToString();
            js["rootMotionXY"] = rootMotionXZ.ToString();
            js["rootMotionZ"] = rootMotionY.ToString();
            js["startFrame"] = startFrame.ToString();
            js["endFrame"] = endFrame.ToString();
            return js;
        }

        public void Deserialize(JSONClass data)
        {
            this.path = data["path"];
            this.id = data["id"];
            this.loop = data["loop"].AsBool;
            this.restartOnAnimationChange = data["restartOnAnimationChange"].AsBool;
            this.rootMotionXZ = data["rootMotionXY"].AsBool;
            this.rootMotionY = data["rootMotionZ"].AsBool;
            this.startFrame = data["startFrame"].AsInt;
            this.endFrame = data["endFrame"].AsInt;
        }
    }
}
