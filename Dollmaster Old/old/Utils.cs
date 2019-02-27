using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;

namespace DeluxePlugin
{
    class Utils
    {
        public static string currentDirectory = "";

        public static List<string> GetPeopleAtomIds(Atom containingAtom=null)
        {
            return SuperController.singleton.GetAtoms()
            .Where(atom => atom.GetStorableByID("geometry") != null && (containingAtom!=null || atom != containingAtom) )
            .Select(atom => atom.name).ToList();
        }

        public static string GetSelectedAtomId()
        {
            Atom atom = SuperController.singleton.GetSelectedAtom();
            if (atom == null)
            {
                return null;
            }

            return atom.name;
        }

        public static long GetTime()
        {
            return GetTimestamp();
        }

        // Get current timestamp
        public static long GetTimestamp()
        {
            return DateTime.UtcNow.Ticks;
        }

        // Time passed in seconds since given timestamp
        public static float TimeSince(long timestamp)
        {
            long duration = DateTime.UtcNow.Ticks - timestamp;
            return (float)new TimeSpan(duration).TotalSeconds;
        }

        public static MVRScript GetPluginStorableById(Atom atom, string id)
        {
            string storableIdName = atom.GetStorableIDs().FirstOrDefault((string storeId) =>
            {
                if (string.IsNullOrEmpty(storeId))
                {
                    return false;
                }
                return storeId.Contains(id);
            });

            if (storableIdName == null)
            {
                return null;
            }

            return atom.GetStorableByID(storableIdName) as MVRScript;
        }

        public static string GetSceneDirectory()
        {
            if (string.IsNullOrEmpty(currentDirectory))
            {
                currentDirectory = SuperController.singleton.currentLoadDir;
            }

            return currentDirectory;
        }

        public static string AbsPath(string localPath)
        {
            return Application.dataPath + "/../" + GetSceneDirectory() + "/" + localPath;
        }

        public static NamedAudioClip LoadAudioClip(string localFilePath)
        {
            NamedAudioClip existing = URLAudioClipManager.singleton.GetClip(localFilePath);
            if (existing != null)
            {
                return existing;
            }

            URLAudioClip clip = URLAudioClipManager.singleton.QueueClip(localFilePath, localFilePath);
            return URLAudioClipManager.singleton.GetClip(clip.uid);
        }

        public static NamedAudioClip LoadAudioClipAbs(string absFilePath)
        {
            string localPath = SuperController.singleton.NormalizePath(absFilePath);
            NamedAudioClip existing = URLAudioClipManager.singleton.GetClip(localPath);
            if (existing != null)
            {
                return existing;
            }

            URLAudioClip clip = URLAudioClipManager.singleton.QueueClip(absFilePath, localPath);
            return URLAudioClipManager.singleton.GetClip(clip.uid);
        }
    }
}
