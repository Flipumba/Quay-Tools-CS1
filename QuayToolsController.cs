using ColossalFramework.UI;
using UnityEngine;

namespace QuayTools
{
    /// <summary>
    /// Listens for the hotkey, finds the segment under the cursor and requests a flip.
    /// Also draws a short status message on screen.
    /// </summary>
    public class QuayToolsController : MonoBehaviour
    {
        private const float MessageSeconds = 2.5f;

        private volatile string _message = string.Empty;
        private float _messageTime = -100f;
        private GUIStyle _style;

        private void Update()
        {
            FenceHeight.Drain();
            if (!Settings.QuickFlipEnabled) return;
            if (!Input.GetKeyDown(Settings.Hotkey)) return;
            if (!IsCtrlHeld()) return;
            if (UIView.IsInsideUI()) return; // don't act when the mouse is over game UI

            ushort segmentId;
            if (!NetRaycaster.TryGetSegmentUnderCursor(out segmentId))
            {
                ShowMessage(Loc.T("nothing"));
                return;
            }

            bool wholeChain = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            SegmentFlipper.RequestFlip(segmentId, wholeChain, ShowMessage);
        }

        private static bool IsCtrlHeld()
        {
            return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        }

        // May be called from the simulation thread: only touches simple fields.
        private void ShowMessage(string text)
        {
            _message = text;
            _messageTime = -1f; // real time is set on the main thread in OnGUI
        }

        private void OnGUI()
        {
            if (string.IsNullOrEmpty(_message)) return;

            if (_messageTime < 0f)
            {
                _messageTime = Time.realtimeSinceStartup;
            }

            if (Time.realtimeSinceStartup - _messageTime > MessageSeconds)
            {
                _message = string.Empty;
                return;
            }

            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.box);
                _style.fontSize = 16;
                _style.alignment = TextAnchor.MiddleCenter;
                _style.normal.textColor = Color.white;
            }

            float w = 360f, h = 36f;
            GUI.Box(new Rect((Screen.width - w) / 2f, Screen.height * 0.12f, w, h), _message, _style);
        }
    }
}
