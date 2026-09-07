using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Cripta
{
    public static class Entrada
    {
        public static Vector2 Movimiento()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard tec = Keyboard.current;
            if (tec == null) return Vector2.zero;
            float x = 0f;
            float y = 0f;
            if (tec.dKey.isPressed || tec.rightArrowKey.isPressed) x += 1f;
            if (tec.aKey.isPressed || tec.leftArrowKey.isPressed) x -= 1f;
            if (tec.wKey.isPressed || tec.upArrowKey.isPressed) y += 1f;
            if (tec.sKey.isPressed || tec.downArrowKey.isPressed) y -= 1f;
            return new Vector2(x, y);
#else
            return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#endif
        }

        public static Vector2 Mirada()
        {
#if ENABLE_INPUT_SYSTEM
            Mouse raton = Mouse.current;
            if (raton == null) return Vector2.zero;
            return raton.delta.ReadValue() * 0.05f;
#else
            return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 1.2f;
#endif
        }

        public static bool SaltoPresionado()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard tec = Keyboard.current;
            return tec != null && tec.spaceKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Space);
#endif
        }

        public static bool InteraccionPresionada()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard tec = Keyboard.current;
            return tec != null && tec.eKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.E);
#endif
        }

        public static bool CorrerMantenido()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard tec = Keyboard.current;
            return tec != null && tec.leftShiftKey.isPressed;
#else
            return Input.GetKey(KeyCode.LeftShift);
#endif
        }

        public static bool EscapePresionado()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard tec = Keyboard.current;
            return tec != null && tec.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        public static bool ClicPresionado()
        {
#if ENABLE_INPUT_SYSTEM
            Mouse raton = Mouse.current;
            return raton != null && raton.leftButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(0);
#endif
        }
    }
}
