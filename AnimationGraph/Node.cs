using System;
using UnityEngine;
using System.Collections.Generic;
using SimpleJSON;

namespace DeluxePlugin.AnimationGraph
{
    class Node
    {
        public AnimationPattern ap;
        private UI ui;
        public UIDynamicButton connectButton;
        public UIDynamicButton disconnectButton;

        public List<Transition> transitions = new List<Transition>();

        public Node(UI ui, AnimationPattern ap)
        {
            this.ap = ap;
            this.ui = ui;

            connectButton = ui.CreateButton("Connect", 100, 50);
            disconnectButton = ui.CreateButton("Disconnect All", 100, 50);
            disconnectButton.transform.SetParent(connectButton.transform);
            disconnectButton.transform.Translate(0, -0.08f, 0);

            UIDynamicButton playButton = ui.CreateButton("Play", 100, 50);
            playButton.transform.SetParent(connectButton.transform);
            playButton.transform.Translate(0, -0.16f, 0);

            playButton.button.onClick.AddListener(() =>
            {
                ap.SetBoolParamValue("on", true);

                if (ap.GetBoolParamValue("pause") == true)
                {
                    playButton.label = "Stop";
                    ap.ResetAnimation();
                }
                else
                {
                    playButton.label = "Play";
                    ap.SetBoolParamValue("on", false);
                }

                ap.TogglePause();

            });
        }

        public void Update()
        {
            connectButton.transform.position = ap.containingAtom.mainController.transform.position;
            connectButton.transform.Translate(0, -0.05f, 0, Space.World);
        }

        public Atom atom
        {
            get { return ap.containingAtom; }
        }

        public Transition GetRandomTransition()
        {
            if (transitions.Count == 0)
            {
                return null;
            }

            int randomIndex = UnityEngine.Random.Range(0, transitions.Count);
            return transitions[randomIndex];
        }

        public void Stop()
        {
            ap.Pause();
            ap.SetBoolParamValue("on", false);
        }

        public void Start()
        {
            ap.SetBoolParamValue("on", true);
            ap.ResetAndPlay();
        }

    }
}
