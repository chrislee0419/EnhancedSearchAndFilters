using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using HMUI;
using VRUIControls;
using HarmonyLib;
using IPA.Utilities;
using BaseInputModule = UnityEngine.EventSystems.BaseInputModule;

namespace EnhancedSearchAndFilters.Search
{
    internal class LaserPointerInputManager : MonoBehaviour
    {
        private PointerEventData _pointerEventData;
        private List<RaycastResult> _raycastResults = new List<RaycastResult>();
        private List<Component> _componentsList = new List<Component>();

        private LaserPointer _laserPointer;

        private const int OffHandPointerId = 2;

        protected void Awake()
        {
            _laserPointer = new GameObject("LaserPointerHandler").AddComponent<LaserPointer>();
            _laserPointer.transform.SetParent(this.transform);
        }

        private void OnEnable()
        {
            ProcessHookPatch.ProcessHook += Process;
            _laserPointer.gameObject.SetActive(true);
        }

        private void OnDisable()
        {
            ProcessHookPatch.ProcessHook -= Process;
            _laserPointer.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            ProcessHookPatch.ProcessHook -= Process;
        }

        public void Process(VRInputModule vrInputModule)
        {
            VRController offHandController = _laserPointer.OffHandController;
            if (!_laserPointer.IsInitialized || offHandController == null)
                return;

            EventSystem eventSystem = vrInputModule.GetField<EventSystem, BaseInputModule>("m_EventSystem");
            if (_pointerEventData == null)
                _pointerEventData = new PointerEventData(eventSystem) { pointerId = OffHandPointerId };

            // perform raycast
            _pointerEventData.Reset();
            _pointerEventData.pointerCurrentRaycast = new RaycastResult
            {
                worldPosition = offHandController.position,
                worldNormal = offHandController.forward
            };
            _pointerEventData.scrollDelta = Vector2.zero;

            eventSystem.RaycastAll(_pointerEventData, _raycastResults);

            // reimplementation of FindFirstRaycast
            _pointerEventData.pointerCurrentRaycast = default;
            for (int i = 0; i < _raycastResults.Count; ++i)
            {
                if (_raycastResults[i].gameObject != null)
                {
                    _pointerEventData.pointerCurrentRaycast = _raycastResults[i];
                    break;
                }
            }

            _pointerEventData.delta = _pointerEventData.pointerCurrentRaycast.screenPosition - _pointerEventData.position;
            _pointerEventData.position = _pointerEventData.pointerCurrentRaycast.screenPosition;

            // handle pointer enter/exit on gameobjects
            HandlePointerExitAndEnter(vrInputModule, _pointerEventData, _pointerEventData.pointerCurrentRaycast.gameObject);

            // send raycast results to laser pointer
            _laserPointer.Process(_pointerEventData);
        }

        // reimplementation of VRInputModule's HandlePointerExitAndEnter
        // only functional change should be that we target the off-hand controller for haptics
        private void HandlePointerExitAndEnter(VRInputModule vrInputModule, PointerEventData eventData, GameObject newEnterTarget)
        {
            if (newEnterTarget == null || eventData.pointerEnter == null)
            {
                foreach (var hovered in eventData.hovered)
                    ExecuteEvents.Execute(hovered, eventData, ExecuteEvents.pointerExitHandler);
                eventData.hovered.Clear();

                if (newEnterTarget == null)
                {
                    eventData.pointerEnter = null;
                    return;
                }
            }

            // at this point, newEnterTarget cannot be null
            if (eventData.pointerEnter == newEnterTarget)
                return;

            GameObject commonRoot = null;
            Transform t = null;
            if (eventData.pointerEnter != null)
            {
                // reimplementation of BaseInputModule.FindCommonRoot
                Transform t1 = eventData.pointerEnter.transform;
                bool found = false;
                while (t1 != null)
                {
                    Transform t2 = newEnterTarget.transform;
                    while (t2 != null)
                    {
                        if (t1 == t2)
                        {
                            commonRoot = t1.gameObject;
                            found = true;
                            break;
                        }

                        t2 = t2.parent;
                    }

                    if (found)
                        break;
                    else
                        t1 = t1.parent;
                }

                t = eventData.pointerEnter.transform;
                while (t != null && commonRoot?.transform != t)
                {
                    ExecuteEvents.Execute(t.gameObject, eventData, ExecuteEvents.pointerExitHandler);
                    eventData.hovered.Remove(t.gameObject);
                    t = t.parent;
                }
            }

            if (!vrInputModule.userInteractionEnabled)
                return;

            bool hasTriggeredHapticPulse = false;

            eventData.pointerEnter = newEnterTarget;
            t = newEnterTarget.transform;

            while (t != null && t.gameObject != commonRoot)
            {
                _componentsList.Clear();
                t.gameObject.GetComponents(_componentsList);

                if (!hasTriggeredHapticPulse)
                {
                    foreach (var component in _componentsList)
                    {
                        Selectable selectable = component as Selectable;
                        Interactable interactable = component as Interactable;
                        if ((selectable != null && selectable.isActiveAndEnabled && selectable.interactable) ||
                            (interactable != null && interactable.isActiveAndEnabled && interactable.interactable))
                        {
                            vrInputModule.GetField<VRPlatformHelper, VRInputModule>("_vrPlatformHelper").TriggerHapticPulse(_laserPointer.OffHandController.node, 0.25f);
                            hasTriggeredHapticPulse = true;
                            break;
                        }
                    }
                }

                ExecuteEvents.Execute(t.gameObject, eventData, ExecuteEvents.pointerEnterHandler);
                eventData.hovered.Add(t.gameObject);
                t = t.parent;
            }
        }

        private class LaserPointer : MonoBehaviour
        {
            private VRPointer _originalPointer;
            private VRController _rightController;
            private VRController _leftController;

            public VRController OffHandController { get; private set; }
            public bool IsInitialized { get; private set; } = false;

            private Transform _laserPointerTransform;

            private static Transform _pointerPrefab;
            private static float _defaultPointerLength;
            private static float _pointerWidth;

            private void Awake()
            {
                Init();
                if (!IsInitialized)
                    Logger.log.Warn("Unable to initialize LaserPointerManager for two handed typing");
            }

            private void Init()
            {
                _originalPointer = Resources.FindObjectsOfTypeAll<VRPointer>().FirstOrDefault();
                if (_originalPointer == null)
                    return;

                _rightController = _originalPointer.GetField<VRController, VRPointer>("_rightVRController");
                _leftController = _originalPointer.GetField<VRController, VRPointer>("_leftVRController");
                //_rightController = Resources.FindObjectsOfTypeAll<VRController>().FirstOrDefault(x => x.name == "ControllerRight");
                //_leftController = Resources.FindObjectsOfTypeAll<VRController>().FirstOrDefault(x => x.name == "ControllerLeft");

                _pointerPrefab = _originalPointer.GetField<Transform, VRPointer>("_laserPointerPrefab");
                _defaultPointerLength = _originalPointer.GetField<float, VRPointer>("_defaultLaserPointerLength");
                _pointerWidth = _originalPointer.GetField<float, VRPointer>("_laserPointerWidth");

                IsInitialized = _rightController != null && _leftController != null && _originalPointer != null && _pointerPrefab != null;
            }

            private void OnEnable()
            {
                if (IsInitialized && OffHandController != null)
                {
                    _laserPointerTransform = Instantiate(_pointerPrefab, OffHandController.transform, false);
                    SetPositionAndScale(_defaultPointerLength);
                }
            }

            private void OnDisable()
            {
                if (_laserPointerTransform != null)
                {
                    Destroy(_laserPointerTransform.gameObject);
                    _laserPointerTransform = null;
                }
            }

            private void LateUpdate()
            {
                if (!IsInitialized)
                    return;

                VRController lastOffHandController = OffHandController;
                VRController currentMainController = _originalPointer.GetField<VRController, VRPointer>("_vrController");
                OffHandController = currentMainController == _rightController ? _leftController : _rightController;

                if (lastOffHandController != OffHandController)
                {
                    if (_laserPointerTransform == null)
                        _laserPointerTransform = Instantiate(_pointerPrefab, OffHandController.transform, false);
                    else
                        _laserPointerTransform.SetParent(OffHandController.transform, false);
                    SetPositionAndScale(_defaultPointerLength);
                }
            }

            private void SetPositionAndScale(float pointerLength)
            {
                _laserPointerTransform.localPosition = new Vector3(0f, 0f, pointerLength / 2f);
                _laserPointerTransform.localScale = new Vector3(_pointerWidth * 0.3f, _pointerWidth * 0.3f, pointerLength);
            }

            public void Process(PointerEventData eventData)
            {
                if (float.IsNaN(eventData.pointerCurrentRaycast.worldPosition.x) || _laserPointerTransform == null)
                    return;

                if (eventData.pointerCurrentRaycast.gameObject != null)
                    SetPositionAndScale(eventData.pointerCurrentRaycast.distance);
                else
                    SetPositionAndScale(_defaultPointerLength);
            }
        }

        [HarmonyPatch(typeof(VRInputModule), "Process")]
        private class ProcessHookPatch
        {
            public static event Action<VRInputModule> ProcessHook;

            private static void Postfix(VRInputModule __instance)
            {
                ProcessHook?.Invoke(__instance);
            }
        }
    }
}
