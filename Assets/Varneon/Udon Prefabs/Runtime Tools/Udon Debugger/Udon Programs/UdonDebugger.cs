#pragma warning disable 649

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using UnityEngine.UI;

namespace Varneon.UdonPrefabs.RuntimeTools
{
    public class UdonDebugger : UdonSharpBehaviour
    {
        [SerializeField]
        private Text text;

        [SerializeField, Range(1, 100)]
        private int MaxLines = 40;

        [SerializeField]
        private KeyCode 
            KeyControl = KeyCode.LeftControl,
            KeyClear = KeyCode.Backspace,
            KeyHide = KeyCode.U,
            KeyTextUpscale = KeyCode.KeypadPlus,
            KeyTextDownscale = KeyCode.KeypadMinus;

        private void Start()
        {
            text.text = string.Format("You are using Varneon's Udon debugger.\nControls:\nToggle Debugger: {0} + {1}\nClear Log: {0} + {2}\nUpscale Text: {0} + {3}\nDownscale Text: {0} + {4}", KeyControl, KeyHide, KeyClear, KeyTextUpscale, KeyTextDownscale);
        }

        private void Update()
        {
            if (Input.GetKey(KeyControl))
            {
                if (Input.GetKeyDown(KeyClear)) Clear();
                else if(Input.GetKeyDown(KeyHide)) Toggle();
                else if (Input.GetKeyDown(KeyTextUpscale)) UpscaleText();
                else if (Input.GetKeyDown(KeyTextDownscale)) DownscaleText();
            }
        }

        public void Toggle() { text.gameObject.SetActive(!text.gameObject.activeSelf); }
        public void UpscaleText() { text.fontSize++; }
        public void DownscaleText() { text.fontSize--; }

        public void WriteLine(string message)
        {
            text.text += $"\n{message}";
            if (text.text.Split('\n').Length > MaxLines)
            {
                text.text = text.text.Remove(0, text.text.IndexOf('\n') + 1);
            }
        }

        public void Clear() { text.text = string.Empty; }
    }
}