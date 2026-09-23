using Unity.Cinemachine;
using UnityEngine;

public sealed class TutorialShowcaseRig : MonoBehaviour
{
    [SerializeField]
    private CinemachineCamera showcaseCamera;

    [SerializeField]
    private int activePriority = 100;

    [SerializeField]
    private int inactivePriority = -10;

    private void Awake()
    {
        Deactivate();
    }

    public void Activate()
    {
        if (showcaseCamera != null)
        {
            showcaseCamera.Priority = activePriority;
        }
    }

    public void Deactivate()
    {
        if (showcaseCamera != null)
        {
            showcaseCamera.Priority = inactivePriority;
        }
    }
}