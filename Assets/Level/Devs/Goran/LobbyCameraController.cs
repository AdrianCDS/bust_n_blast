using UnityEngine;
using Fusion;
using Cinemachine;

public class LobbyCameraController : NetworkBehaviour
{
    [SerializeField] private Camera localPlayerCamera;

    private CinemachineVirtualCamera thirdPersonCamera;

    public override void Spawned()
    {
        if (localPlayerCamera != null)
        {
            localPlayerCamera.gameObject.SetActive(false);
        }

        thirdPersonCamera = GameObject.Find("ThirdPersonCamera")?.GetComponent<CinemachineVirtualCamera>();

        if (HasInputAuthority)
        {
            if (thirdPersonCamera != null)
            {
                thirdPersonCamera.Priority = 0;
            }

            if (localPlayerCamera != null)
            {
                localPlayerCamera.gameObject.SetActive(true);
                localPlayerCamera.depth = 10;
            }
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        RestoreMainCamera();
    }

    private void OnDestroy()
    {
        RestoreMainCamera();
    }

    private void RestoreMainCamera()
    {
        if (HasInputAuthority && thirdPersonCamera != null)
        {
            thirdPersonCamera.Priority = 10;
        }
    }
}