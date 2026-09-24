using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace GPGR.Runtime
{
    /// <summary>
    /// Space, Enter, 마우스 좌클릭. inputactions 에셋은 쓰지 않는다.
    /// </summary>
    public sealed class PlayerHitSource : HitSource
    {
        internal override void Poll(double songBeat)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                Edge(keyboard.spaceKey, songBeat);
                Edge(keyboard.enterKey, songBeat);
                Edge(keyboard.numpadEnterKey, songBeat);
            }

            Mouse mouse = Mouse.current;
            if (mouse != null)
                Edge(mouse.leftButton, songBeat);

            // 터치 자리. 지금은 켜지 않는다. 켜면 같은 RaisePressed / RaiseReleased 경로다.
            // Touchscreen screen = Touchscreen.current;
            // if (screen != null)
            //     Edge(screen.primaryTouch.press, songBeat);
        }

        void Edge(ButtonControl control, double songBeat)
        {
            if (control == null)
                return;
            if (control.wasPressedThisFrame)
                RaisePressed(songBeat);
            if (control.wasReleasedThisFrame)
                RaiseReleased(songBeat);
        }
    }
}
