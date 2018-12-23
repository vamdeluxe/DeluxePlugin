using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;

namespace DeluxePlugin.Powertools
{
    class BulkAudioLoader : MVRScript
    {
        List<NamedAudioClip> clips = new List<NamedAudioClip>();
        JSONStorableString loadLog;
        string lastTriedPath = "";
        public override void Init()
        {
            try
            {
                lastTriedPath = SuperController.singleton.currentLoadDir;
                loadLog = new JSONStorableString("log", "");
                CreateTextField(loadLog, true).height = 1200;

                JSONStorableString pathStorable = new JSONStorableUrl("audioPath", "", (string path) =>
                {
                    if (string.IsNullOrEmpty(path))
                    {
                        return;
                    }
                    /*
                    Debug.Log("savedPath: " + path);
                    Debug.Log("dataPath: " + Application.dataPath);
                    Debug.Log("savedir: " + SuperController.singleton.currentLoadDir);
                    Debug.Log("loaddir: " + SuperController.singleton.currentSaveDir);
                    Debug.Log("combined: " + Application.dataPath + "/../" + path);
                    */
                    lastTriedPath = LoadClips(path);
                });


                CreateButton("Load Audio From Folder").button.onClick.AddListener(() =>
                {
                    SuperController.singleton.GetDirectoryPathDialog((string path) => {
                        if (String.IsNullOrEmpty(path))
                        {
                            return;
                        }

                        pathStorable.val = path;
                    }, lastTriedPath);
                });

                CreateButton("Remove All Audio").button.onClick.AddListener(() =>
                {
                    URLAudioClipManager.singleton.RemoveAllClips();
                });
            }
            catch (Exception e)
            {
                SuperController.LogError("Exception caught: " + e);
            }
        }

        string LoadClips(string requestedPath)
        {
            loadLog.SetVal("");
            try
            {
                //  try as-is, when user selects a folder instead of being in the folder
                SuperController.singleton.GetFilesAtPath(requestedPath).ToList().ForEach((string fileName) =>
                {
                    if (fileName.Contains(".json"))
                    {
                        return;
                    }

                    if (!fileName.Contains(".mp3") && !fileName.Contains(".wav") && !fileName.Contains(".ogg"))
                    {
                        return;
                    }
                    clips.Add(LoadAudio(fileName));
                    loadLog.val += SuperController.singleton.NormalizePath(fileName) + "\n";
                });
                return requestedPath;
            }
            catch
            {
                //  if user is in the folder, figure out what path they meant
                string folderName = "\\" + requestedPath.Substring(requestedPath.LastIndexOf('\\') + 1) + "\\";
                requestedPath = requestedPath.Replace(folderName, "\\");
                Debug.Log("reqPath is now " + requestedPath);
                SuperController.singleton.GetFilesAtPath(requestedPath).ToList().ForEach((string fileName) =>
                {
                    if (fileName.Contains(".json"))
                    {
                        return;
                    }

                    if (!fileName.Contains(".mp3") && !fileName.Contains(".wav") && !fileName.Contains(".ogg"))
                    {
                        return;
                    }
                    clips.Add(LoadAudio(fileName));
                    loadLog.val += SuperController.singleton.NormalizePath(fileName) + "\n";
                });
                return requestedPath;
            }

        }

        NamedAudioClip LoadAudio(string path)
        {
            string localPath = SuperController.singleton.NormalizeLoadPath(path);
            NamedAudioClip existing = URLAudioClipManager.singleton.GetClip(localPath);
            if (existing != null)
            {
                return existing;
            }

            URLAudioClip clip = URLAudioClipManager.singleton.QueueClip(SuperController.singleton.NormalizeMediaPath(path));
            if (clip == null)
            {
                return null;
            }

            NamedAudioClip nac = URLAudioClipManager.singleton.GetClip(clip.uid);
            if (nac == null)
            {
                return null;
            }
            return nac;
        }


    }
}
