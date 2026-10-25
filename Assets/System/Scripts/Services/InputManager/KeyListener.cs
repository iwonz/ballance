#if UNITY_ANDROID && !UNITY_EDITOR
using Input = Ballance2.InputCompatibility.AndroidInput;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;

/*
* Copyright(c) 2021  mengyu
*
* Module:
* KeyListener.cs
* 
* Purpose:
* Keyboard event listener.
* 
* Convenient keyboard event subscriptions.
* 
* Example:
* ```csharp
* var lisnster = KeyListener.Get(this.gameObject);
* lisnster.AddKeyListen(KeyCode.A, (key, downed) => { 
*   if (downed) {
*     // Raised when A is pressed.
*   }
* });
* ```
*
* For one-shot key input, use GameUIManager.WaitKey instead of creating a separate listener.
*
* Author:
* mengyu
*
*/

namespace Ballance2.Services.InputManager
{
    /// <summary>
    /// Keyboard event listener.
    /// </summary>
    public class KeyListener : MonoBehaviour
    {
        private Gamepad currentGamepad;
        private Joystick currentJoystick;
        private DpadControl currentJoystickDpad;
        private ButtonControl[] currentJoystickButtonControls;
        private AxisControl currentJoystickRightStickAxisX;
        private AxisControl currentJoystickRightStickAxisY;

        /// <summary>
        /// Keyboard event callback.
        /// </summary>
        /// <param name="downed">Whether the key is pressed.</param>
        public delegate void KeyDelegate(KeyCode key, bool downed);


        /// <summary>
        /// Create a key event listener on a GameObject.
        /// </summary>
        /// <param name="go">GameObject.</param>
        /// <returns>Event listener instance.</returns>
        public static KeyListener Get(GameObject go)
        {
            KeyListener listener = go.GetComponent<KeyListener>();
            if (listener == null) listener = go.AddComponent<KeyListener>();
            return listener;
        }

        private class KeyListenerItem
        {
            public KeyCode key;
            public bool downed = false;
            public KeyDelegate callBack;
            public int id;
            public GamepadButton[] gamepadButtons;

            public bool ContainsGamepadButton(GamepadButton gamepadButton)
            {
                return Array.IndexOf(gamepadButtons, gamepadButton) >= 0;
            }
        }

        private LinkedList<KeyListenerItem> items = new LinkedList<KeyListenerItem>();
        [SerializeField]
        private bool isListenKey = true;
        private int listenKeyId = 0;

        /// <summary>
        /// Enable listening.
        /// </summary>
        public bool IsListenKey { get { return isListenKey; } set { isListenKey = value; } }

        /// <summary>
        /// Suppress keyboard events while UI is active.
        /// </summary>
        public bool DisableWhenUIFocused = true;

        /// <summary>
        /// Allow multiple keyboard events at once; otherwise only one is sent, with later registrations taking priority.
        /// </summary>
        public bool AllowMultipleKey = false;

        public KeyListener()
        {
            currentGamepad = Gamepad.current;
            currentJoystick = Joystick.current;
            if (currentJoystick != null)
            {
                var joystickChildren = currentJoystick.children.ToArray();
                currentJoystickDpad = joystickChildren.FirstOrDefault(t => t is DpadControl) as DpadControl;

                currentJoystickRightStickAxisX = joystickChildren.FirstOrDefault(t => t.path.EndsWith("/z")) as AxisControl;
                currentJoystickRightStickAxisY = joystickChildren.FirstOrDefault(t => t.path.EndsWith("/rz")) as AxisControl;

                currentJoystickButtonControls = joystickChildren.Where(t => t is ButtonControl).Cast<ButtonControl>().ToArray();
                if (currentJoystickButtonControls.Length < 15)
                    currentJoystickButtonControls = currentJoystickButtonControls.Concat(new ButtonControl[15 - currentJoystickButtonControls.Length]).ToArray();
            }
        }

        /// <summary>
        /// Resend currently held-key events.
        /// </summary>
        public void ReSendPressingKey()
        {
            LinkedListNode<KeyListenerItem> cur = items.Last;
            while (cur != null)
            {
                var item = cur.Value;
                if (item.downed)
                {
                    var isKeydown = IsKeyOrGamepadButtonsDown(item);
                    if (isKeydown)
                    {
                        item.callBack(item.key, true);
                    }
                    else
                    {
                        item.downed = false;
                        item.callBack(item.key, false);
                    }
                }
                else
                    item.callBack(item.key, false);
                cur = cur.Previous;
            }
        }

        /// <summary>
        /// Add a key listener.
        /// </summary>
        /// <param name="key">Key code.</param>
        /// <param name="callBack">Callback.</param>
        /// <returns>Listener ID; remove it with DeleteKeyListen.</returns>
        public int AddKeyListen(KeyCode key, KeyDelegate callBack, params GamepadButton[] gamepadButtons)
        {
            listenKeyId++;

            KeyListenerItem item = new KeyListenerItem();
            item.callBack = callBack;
            item.key = key;
            item.gamepadButtons = gamepadButtons;
            item.id = listenKeyId;

            // Traverse backward and insert beside matching keys.
            LinkedListNode<KeyListenerItem> cur = items.Last;
            while (cur != null)
            {
                if (cur.Value.key == key)
                {
                    items.AddAfter(cur, item);
                    return listenKeyId;
                }
                cur = cur.Previous;
            }
            // Append when no matching key exists.
            items.AddLast(item);
            return listenKeyId;
        }

        /// <summary>
        /// Remove a listener.
        /// </summary>
        /// <param name="id">ID returned by the key listener registration.</param>
        public void DeleteKeyListen(int id)
        {
            // Remove from the linked list.
            int count = 0;
            LinkedListNode<KeyListenerItem> cur = items.First;
            while (cur != null)
            {
                if (cur.Value.id == id)
                {
                    items.Remove(cur);
                    return;
                }
                cur = cur.Next;
                count++;

                if (count > items.Count)
                    break;
            }
        }
        /// <summary>
        /// Clear all key listeners.
        /// </summary>
        public void ClearKeyListen()
        {
            items.Clear();
        }

        private ButtonControl GetGamepadButtonControl(GamepadButton button)
        {
            if (currentGamepad != null)
            {
                switch (button)
                {
                    case GamepadButton.LeftStick:
                        return currentGamepad.leftStickButton;
                    case GamepadButton.RightStick:
                        return currentGamepad.rightStickButton;
                    case GamepadButton.LeftShoulder:
                        return currentGamepad.leftShoulder;
                    case GamepadButton.RightShoulder:
                        return currentGamepad.rightShoulder;
                    case GamepadButton.LeftTrigger:
                        return currentGamepad.leftTrigger;
                    case GamepadButton.RightTrigger:
                        return currentGamepad.rightTrigger;
                    case GamepadButton.DpadUp:
                        return currentGamepad.dpad.up;
                    case GamepadButton.DpadDown:
                        return currentGamepad.dpad.down;
                    case GamepadButton.DpadLeft:
                        return currentGamepad.dpad.left;
                    case GamepadButton.DpadRight:
                        return currentGamepad.dpad.right;
                    case GamepadButton.North:
                        return currentGamepad.yButton;
                    case GamepadButton.South:
                        return currentGamepad.aButton;
                    case GamepadButton.West:
                        return currentGamepad.xButton;
                    case GamepadButton.East:
                        return currentGamepad.bButton;
                    case GamepadButton.Select:
                        return currentGamepad.selectButton;
                    case GamepadButton.Start:
                        return currentGamepad.startButton;
                }
            }
            else if (currentJoystick != null)
            {
                if (currentJoystickButtonControls == null)
                    return null;
                switch (button)
                {
                    case GamepadButton.LeftStick:
                        return currentJoystickButtonControls[13];
                    case GamepadButton.RightStick:
                        return currentJoystickButtonControls[14];
                    case GamepadButton.LeftShoulder:
                        return currentJoystickButtonControls[6];
                    case GamepadButton.RightShoulder:
                        return currentJoystickButtonControls[7];
                    case GamepadButton.LeftTrigger:
                        return currentJoystickButtonControls[8];
                    case GamepadButton.RightTrigger:
                        return currentJoystickButtonControls[9];
                    case GamepadButton.DpadUp:
                        return currentJoystickDpad?.up;
                    case GamepadButton.DpadDown:
                        return currentJoystickDpad?.down;
                    case GamepadButton.DpadLeft:
                        return currentJoystickDpad?.left;
                    case GamepadButton.DpadRight:
                        return currentJoystickDpad?.right;
                    case GamepadButton.North:
                        return currentJoystickButtonControls[4];
                    case GamepadButton.South:
                        return currentJoystickButtonControls[0];
                    case GamepadButton.West:
                        return currentJoystickButtonControls[3];
                    case GamepadButton.East:
                        return currentJoystickButtonControls[1];
                    case GamepadButton.Select:
                        return currentJoystickButtonControls[10];
                    case GamepadButton.Start:
                        return currentJoystickButtonControls[11];
                }
            }
            return null;
        }

        private float GetLeftStickXValue()
        {
            if (currentGamepad?.leftStick?.x != null)
                return currentGamepad.leftStick.x.value;
            else if (currentJoystick?.stick?.x != null)
                return currentJoystick.stick.x.value;
            return 0;
        }

        private float GetLeftStickYValue()
        {
            if (currentGamepad?.leftStick?.y != null)
                return currentGamepad.leftStick.y.value;
            else if (currentJoystick?.stick?.y != null)
                return currentJoystick.stick.y.value;
            return 0;
        }

        private float GetRightStickXValue()
        {
            if (currentGamepad?.rightStick?.x != null)
                return currentGamepad.rightStick.x.value;
            else if (currentJoystick != null && currentJoystickRightStickAxisX != null)
                return currentJoystickRightStickAxisX.value;
            return 0;
        }

        private float GetRightStickYValue()
        {
            if (currentGamepad?.rightStick?.y != null)
                return currentGamepad.rightStick.y.value;
            else if (currentJoystick != null && currentJoystickRightStickAxisY != null)
                return currentJoystickRightStickAxisY.value;
            return 0;
        }

        private bool IsKeyOrGamepadButtonsDown(KeyListenerItem item)
        {
            var isKeydown = false;
            // Check the keyboard first.
            isKeydown = Input.GetKey(item.key);
            if (isKeydown)
                return isKeydown;
            // Then check gamepad buttons.
            if (item.gamepadButtons != null && item.gamepadButtons.Length > 0)
            {
                foreach (var button in item.gamepadButtons)
                {
                    isKeydown = IsGamepadButtonPressed(button);
                    if (isKeydown)
                        break;
                }
            }
            if (isKeydown)
                return isKeydown;
            // Finally check joystick input.
            if (item.ContainsGamepadButton(GamepadButton.DpadLeft))
                isKeydown = GetLeftStickXValue() < -0.4;
            else if (item.ContainsGamepadButton(GamepadButton.DpadRight))
                isKeydown = GetLeftStickXValue() > 0.4;
            else if (item.ContainsGamepadButton(GamepadButton.DpadUp))
                isKeydown = GetLeftStickYValue() > 0.4;
            else if (item.ContainsGamepadButton(GamepadButton.DpadDown))
                isKeydown = GetLeftStickYValue() < -0.4;
            else if (item.ContainsGamepadButton(GamepadButton.Y))
                isKeydown = Math.Abs(GetRightStickYValue()) >= 0.4;
            else if (item.ContainsGamepadButton(GamepadButton.LeftShoulder))
                isKeydown = GetRightStickXValue() < -0.4;
            else if (item.ContainsGamepadButton(GamepadButton.RightShoulder))
                isKeydown = GetRightStickXValue() > 0.4;
            return isKeydown;
        }

        private bool IsGamepadButtonPressed(GamepadButton button)
        {
            var buttonControl = GetGamepadButtonControl(button);
            if (buttonControl == null)
                return false;
            return buttonControl.isPressed;
        }

        private void Update()
        {
            if (isListenKey)
            {
                // Exclude active GUI interaction.
                if (DisableWhenUIFocused && (EventSystem.current.IsPointerOverGameObject() || GUIUtility.hotControl != 0))
                    return;
                
                // Traverse backward so later registrations are processed first.
                LinkedListNode<KeyListenerItem> cur = items.Last;
                KeyCode lastPressedKey = KeyCode.None;
                while (cur != null)
                {
                    var item = cur.Value;
                    var isKeydown = IsKeyOrGamepadButtonsDown(item);

                    if (isKeydown && !item.downed)
                    {
                        if (!AllowMultipleKey && lastPressedKey == item.key)
                        {
                            // Suppress a duplicate key event when multiple dispatch is disabled.
                            if (item.downed)
                            {
                                item.downed = false;
                                item.callBack(item.key, false);
                            }
                        }
                        else
                        {
                            item.downed = true;
                            item.callBack(item.key, true);

                            if (!AllowMultipleKey)
                                lastPressedKey = item.key;
                        }
                    }
                    if (!isKeydown && item.downed)
                    {
                        item.downed = false;
                        item.callBack(item.key, false);
                    }
                    cur = cur.Previous;
                }
            }
        }
    }
}
