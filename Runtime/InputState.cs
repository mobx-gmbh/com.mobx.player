using MobX.CursorManagement;
using MobX.Inspector;
using MobX.Mediator.Callbacks;
using MobX.Mediator.States;
using MobX.UI;
using MobX.Utilities.Types;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace MobX.Player
{
    public class InputState : State<InputState>, IHideCursor, IConfineCursor, ILockCursor
    {
        #region Settings

        [Foldout("Input")]
        [SerializeField] [Required] private InputActionAsset inputActionAsset;
        [SerializeField] [Required] private HideCursorProvider cursorHide;
        [SerializeField] [Required] private ConfineCursorProvider cursorConfines;
        [SerializeField] [Required] private LockCursorProvider cursorLocks;

        [Header("Input Actions")]
        [Tooltip("Action maps that are enabled when this context is activated")]
        [SerializeField] private List<InputActionMapName> activeActionMaps = new() {"Debug", "General"};

        [Header("Cursor")]
        [Tooltip("When enabled, blocks the cursor visibility while the context is active")]
        [SerializeField] private bool hideCursor;
        [SerializeField] private bool hideCursorWithGamepad = true;
        [SerializeField] private bool hideCursorOnNavigationInput;
        [Tooltip("The cursors lock mode while this context is active")]
        [SerializeField] private CursorLockMode cursorLockMode;

        [Header("Selection & UI")]
        [SerializeField] private bool clearSelectionOnMouseMovement;
        [SerializeField] private bool disableNavigationEvents;

        private Action _handleControllerScheme;
        private Action _handleDesktopScheme;
        private Action _handleMouseMovement;
        private Action _handleNavigationInput;
        private InputState _previousInputState;

        #endregion


        #region Enter

        protected sealed override void OnStateEnter(InputState previousState)
        {
            _previousInputState = previousState;
            inputActionAsset.Enable();
            foreach (var inputActionMap in inputActionAsset.actionMaps)
            {
                inputActionMap.Disable();
            }
            foreach (var actionMapName in activeActionMaps)
            {
                var actionMap = inputActionAsset.FindActionMap(actionMapName, true);
                actionMap.Enable();
            }

            _handleControllerScheme ??= HandleControllerScheme;
            _handleDesktopScheme ??= HandleDesktopScheme;
            _handleMouseMovement ??= HandleMouseInput;
            _handleNavigationInput ??= HandleNavigationInput;

            Controls.BecameControllerScheme += _handleControllerScheme;
            Controls.BecameDesktopScheme += _handleDesktopScheme;
            Controls.MouseInputReceived += _handleMouseMovement;
            Controls.NavigationInputReceived += _handleNavigationInput;
            Controls.EnableNavigationEvents = !disableNavigationEvents;

            if (Controls.IsGamepadScheme)
            {
                HandleControllerScheme();
            }
            else
            {
                HandleDesktopScheme();
            }

            switch (cursorLockMode)
            {
                case CursorLockMode.Confined:
                    cursorConfines.Add(this);
                    break;
                case CursorLockMode.Locked:
                    cursorLocks.Add(this);
                    break;
            }
        }

        #endregion


        #region Exit

        protected sealed override void OnStateExit(InputState nextState)
        {
            Shutdown();
        }

        [CallbackOnApplicationQuit]
        private void Shutdown()
        {
            Controls.BecameControllerScheme -= _handleControllerScheme;
            Controls.BecameDesktopScheme -= _handleDesktopScheme;
            Controls.MouseInputReceived -= _handleMouseMovement;
            Controls.NavigationInputReceived -= _handleNavigationInput;
            cursorHide.Remove(this);
            cursorLocks.Remove(this);
            cursorConfines.Remove(this);
        }

        #endregion


        #region Callbacks

        private void HandleControllerScheme()
        {
            if (hideCursorWithGamepad)
            {
                cursorHide.Add(this);
            }
            else
            {
                cursorHide.Remove(this);
            }
        }

        private void HandleDesktopScheme()
        {
            if (hideCursor)
            {
                cursorHide.Add(this);
            }
            else
            {
                cursorHide.Remove(this);
            }
        }

        private void HandleMouseInput()
        {
            if (clearSelectionOnMouseMovement)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
            if (hideCursor)
            {
                return;
            }
            if (hideCursorWithGamepad && Controls.IsGamepadScheme)
            {
                return;
            }
            if (hideCursorOnNavigationInput)
            {
                cursorHide.Remove(this);
            }
        }

        private void HandleNavigationInput()
        {
            if (hideCursor)
            {
                return;
            }
            if (hideCursorWithGamepad && Controls.IsGamepadScheme)
            {
                return;
            }
            if (hideCursorOnNavigationInput)
            {
                cursorHide.Add(this);
            }
        }

        #endregion


        #region State Extension

        public void Deactivate()
        {
            StateMachine.SetState(_previousInputState);
        }

        #endregion
    }
}