using UnityEngine;
using Cinemachine;
using Fusion;
using UnityEngine.InputSystem;
using FusionHelpers;

namespace TeamBasedShooter
{
    public class CameraController : NetworkBehaviour
    {
        [SerializeField] private Transform lookAtTarget;
        [SerializeField] private Transform followTarget;
        [SerializeField] private float aimTransitionSpeed = 5f;
        
        [Header("Camera Settings")]
        [SerializeField] private float defaultFOV = 60f;
        [SerializeField] private float aimingFOV = 35f;
        [SerializeField] private float defaultCameraDistance = 3f;
        [SerializeField] private float aimingCameraDistance = 1.8f;
        [SerializeField] private float defaultScreenX = 0.35f;
        [SerializeField] private float aimingScreenX = 0.65f; 

        private Transform mainCameraTransform;

        private CinemachineVirtualCamera activeVirtualCamera;
        private CinemachineFramingTransposer cinemachineFramingTransposer;

        private bool isAiming = false;
        private float currentFOV;
        private float currentCameraDistance;
        private float currentScreenX;

        public override void Spawned()
        {
            if (HasInputAuthority)
            {
                mainCameraTransform = GameObject.FindGameObjectWithTag("MainCamera").transform;
                activeVirtualCamera = mainCameraTransform.GetComponent<CinemachineBrain>().ActiveVirtualCamera.VirtualCameraGameObject.GetComponent<CinemachineVirtualCamera>();

                activeVirtualCamera.Follow = followTarget;
                activeVirtualCamera.LookAt = lookAtTarget;

                cinemachineFramingTransposer = activeVirtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
                
                currentFOV = defaultFOV;
                currentCameraDistance = defaultCameraDistance;
                currentScreenX = defaultScreenX;
                
                activeVirtualCamera.m_Lens.FieldOfView = currentFOV;
                if (cinemachineFramingTransposer != null)
                {
                    cinemachineFramingTransposer.m_CameraDistance = currentCameraDistance;
                    cinemachineFramingTransposer.m_ScreenX = currentScreenX;
                }

                activeVirtualCamera.Priority = 1;
            }
            else
            {
                if (activeVirtualCamera != null)
                {
                    activeVirtualCamera.Priority = 0;
                }
            }
        }

        private void Update()
        {
            if (activeVirtualCamera != null)
            {
                UpdateAimingCamera();
            }
        }

        public void IsAiming(bool _isAiming)
        {
            isAiming = _isAiming;
        }

        private void UpdateAimingCamera()
        {
            // Update FOV for aiming
            float targetFOV = isAiming ? aimingFOV : defaultFOV;
            currentFOV = Mathf.Lerp(currentFOV, targetFOV, Time.deltaTime * aimTransitionSpeed);
            activeVirtualCamera.m_Lens.FieldOfView = isAiming ? 25f : currentFOV;
            
            // Update camera distance and position for aiming
            if (cinemachineFramingTransposer != null)
            {
                // Adjust camera distance
                float targetDistance = isAiming ? aimingCameraDistance : defaultCameraDistance;
                currentCameraDistance = Mathf.Lerp(currentCameraDistance, targetDistance, Time.deltaTime * aimTransitionSpeed);
                cinemachineFramingTransposer.m_CameraDistance = currentCameraDistance;
                
                // Adjust horizontal position (screen X)
                float targetScreenX = isAiming ? aimingScreenX : defaultScreenX;
                currentScreenX = Mathf.Lerp(currentScreenX, targetScreenX, Time.deltaTime * aimTransitionSpeed);
                cinemachineFramingTransposer.m_ScreenX = isAiming ? 0.15f : 0.35f;
            }
        }
    }
}
