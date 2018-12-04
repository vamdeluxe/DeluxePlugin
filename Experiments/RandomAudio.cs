using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;

namespace DeluxePlugin
{
    class RandomAudio : MVRScript
    {
        List<NamedAudioClip> clips = new List<NamedAudioClip>();
        JSONStorableFloat clipCount;

        public override void Init()
        {
            try
            {
                AudioSourceControl source = containingAtom.GetStorableByID("AudioSource") as AudioSourceControl;
                if (source == null)
                {
                    SuperController.LogError("This plugin only works on AudioSource");
                    return;
                }

                clipCount = new JSONStorableFloat("clipCount", 0, 0, 100, false, false);
                RegisterFloat(clipCount);

                JSONStorableAction audioClipAction = new JSONStorableAction("Play Random", ()=> {
                    if (clips.Count == 0)
                    {
                        return;
                    }
                    source.PlayNow(clips[UnityEngine.Random.Range(0, clips.Count)]);
                });
                CreateButton("Play Random").button.onClick.AddListener(()=>
                {
                    audioClipAction.actionCallback();
                });
                RegisterAction(audioClipAction);

                UIDynamicButton addClipButton = CreateButton("Add Clip");
                addClipButton.buttonColor = new Color(0, 1, 0);
                addClipButton.button.onClick.AddListener(() =>
                {
                    BuildClipUI(clips.Count);
                });


                //GetStringParamNames().ForEach((s) => Debug.Log(s));
                //Debug.Log(GetStringChooserParamNames().Count);

            }
            catch (Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }
        }

        void Start()
        {
            try
            {
                int count = (int)GetFloatParamValue("clipCount");
                Debug.Log(count);
                for (int i = 0; i < count; i++)
                {
                    JSONStorableStringChooser chooser = BuildClipUI(i);
                }
            }
            catch (Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }
        }

        JSONStorableStringChooser BuildClipUI(int index)
        {
            NamedAudioClip clip = null;
            JSONStorableStringChooser chooser = new JSONStorableStringChooser("clip_" + index, GetClipIds(), null, "Choose Audio Clip", (string choice) =>
            {
                clip = URLAudioClipManager.singleton.GetClip(choice);
                clips.Add(clip);

                clipCount.val = clips.Count;

                Debug.Log("updating choice " + choice);
            });
            RegisterStringChooser(chooser);
            UIDynamicPopup popup = CreateScrollablePopup(chooser, true);
            UIDynamicButton remove = CreateButton("Remove Clip Above", true);
            remove.button.onClick.AddListener(() =>
            {
                RemoveButton(remove);
                Destroy(popup.gameObject);

                clips.Remove(clip);
                clipCount.val = clips.Count;
            });

            return chooser;
        }

        List<string> GetClipIds()
        {
            if (URLAudioClipManager.singleton.GetCategoryClips("web").Count == 0)
            {
                return new List<string>();
            }
            return URLAudioClipManager.singleton.GetCategoryClips("web")
                .Where((clip)=>clips.Contains(clip)==false)
                .Select((clip) => clip.uid).ToList();
        }

    }
}
