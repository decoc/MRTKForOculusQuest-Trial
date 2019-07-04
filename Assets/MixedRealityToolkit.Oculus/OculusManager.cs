using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Input.UnityInput;
using Microsoft.MixedReality.Toolkit.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UInput = UnityEngine.Input;

namespace Microsoft.MixedReality.Toolkit.Oculus.Input
{
    /// <summary>
    /// Manages Oculus Quest Devices using unity's input system.
    /// </summary>
    [MixedRealityDataProvider(
    typeof(IMixedRealityInputSystem),
    SupportedPlatforms.Android,
    "Oculus Device Manager")]
    public class OculusManager : BaseInputDeviceManager
    {
        public OculusManager(
            IMixedRealityServiceRegistrar registrar,
            IMixedRealityInputSystem inputSystem,
            string name = null,
            uint priority = DefaultPriority,
            BaseMixedRealityProfile profile = null) : base(registrar, inputSystem, name, priority, profile) {}

        private const float DeviceRefreshInterval = 3.0f;

        protected static readonly Dictionary<string, GenericJoystickController> ActiveControllers = new Dictionary<string, GenericJoystickController>();

        private float deviceRefreshTimer;
        private string[] lastDeviceList;

        #region Controller Utilities

        /// <inheritdoc />
        public override void Update()
        {
            deviceRefreshTimer += Time.unscaledDeltaTime;

            if (deviceRefreshTimer >= DeviceRefreshInterval)
            {
                deviceRefreshTimer = 0.0f;
                RefreshDevices();
            }

            foreach (var controller in ActiveControllers)
            {
                controller.Value?.UpdateController();
            }
        }

        /// <inheritdoc />
        public override void Disable()
        {
            IMixedRealityInputSystem inputSystem = Service as IMixedRealityInputSystem;

            foreach (var genericJoystick in ActiveControllers)
            {
                if (genericJoystick.Value != null)
                {
                    inputSystem?.RaiseSourceLost(genericJoystick.Value.InputSource, genericJoystick.Value);
                }
            }

            ActiveControllers.Clear();
        }

        /// <inheritdoc/>
        public override IMixedRealityController[] GetActiveControllers()
        {
            return ActiveControllers.Values.ToArray<IMixedRealityController>();
        }

        private void RefreshDevices()
        {
            IMixedRealityInputSystem inputSystem = Service as IMixedRealityInputSystem;

            var joystickNames = UInput.GetJoystickNames();

            if (joystickNames.Length <= 0) { return; }

            if (lastDeviceList != null && joystickNames.Length == lastDeviceList.Length)
            {
                for (int i = 0; i < lastDeviceList.Length; i++)
                {
                    if (joystickNames[i].Equals(lastDeviceList[i])) { continue; }

                    if (ActiveControllers.ContainsKey(lastDeviceList[i]))
                    {
                        var controller = GetOrAddController(lastDeviceList[i]);

                        if (controller != null)
                        {
                            inputSystem?.RaiseSourceLost(controller.InputSource, controller);
                        }

                        ActiveControllers.Remove(lastDeviceList[i]);
                    }
                    else { Debug.Log("Controller does not contains kye"); }
                }
            }

            for (var i = 0; i < joystickNames.Length; i++)
            {
                if (string.IsNullOrEmpty(joystickNames[i]))
                {
                    continue;
                }

                if (!ActiveControllers.ContainsKey(joystickNames[i]))
                {
                    var controller = GetOrAddController(joystickNames[i]);

                    if (controller != null)
                    {
                        inputSystem?.RaiseSourceDetected(controller.InputSource, controller);
                    }
                }
            }

            lastDeviceList = joystickNames;
        }

        protected virtual GenericJoystickController GetOrAddController(string joystickName)
        {
            // If a device is already registered with the ID provided, just return it.
            if (ActiveControllers.ContainsKey(joystickName))
            {
                var controller = ActiveControllers[joystickName];
                Debug.Assert(controller != null);
                return controller;
            }

            Handedness controllingHand;

            if (joystickName.Contains("Left"))
            {
                controllingHand = Handedness.Left;
            }
            else if (joystickName.Contains("Right"))
            {
                controllingHand = Handedness.Right;
            }
            else
            {
                controllingHand = Handedness.None;
            }

            var currentControllerType = GetCurrentControllerType(joystickName);
            Type controllerType;

            if (currentControllerType == SupportedControllerType.OculusTouch)
            {
                controllerType = typeof(OculusTouchController);
            }
            else
            {
                return null;
            }

            IMixedRealityInputSystem inputSystem = Service as IMixedRealityInputSystem;

            var pointers = RequestPointers(currentControllerType, controllingHand);
            var inputSource = inputSystem?.RequestNewGenericInputSource($"{currentControllerType} Controller {controllingHand}", pointers, InputSourceType.Controller);
            var detectedController = Activator.CreateInstance(controllerType, TrackingState.NotTracked, controllingHand, inputSource, null) as GenericJoystickController;

            if (detectedController == null)
            {
                Debug.LogError($"Failed to create {controllerType.Name} controller");
                return null;
            }

            if (!detectedController.SetupConfiguration(controllerType))
            {
                // Controller failed to be set up correctly.
                Debug.LogError($"Failed to set up {controllerType.Name} controller");
                // Return null so we don't raise the source detected.
                return null;
            }

            for (int i = 0; i < detectedController.InputSource?.Pointers?.Length; i++)
            {
                detectedController.InputSource.Pointers[i].Controller = detectedController;
            }

            ActiveControllers.Add(joystickName, detectedController);
            return detectedController;
        }

        protected virtual SupportedControllerType GetCurrentControllerType(string joystickName)
        {
            if (string.IsNullOrEmpty(joystickName) || !joystickName.Contains("Oculus"))
            {
                return 0;
            }
            else
            {
                return SupportedControllerType.OculusTouch;
            }
        }

        #endregion
    }
}
