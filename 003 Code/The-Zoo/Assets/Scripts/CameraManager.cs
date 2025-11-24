using Unity.Cinemachine;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [SerializeField] internal GameObject cameraPrefab;

    private readonly Vector2 range = new(-180, 180);

    internal CinemachineOrbitalFollow orbit;
    internal CinemachineInputAxisController controller;
    
    public static CameraManager Instance { get; private set; }

    public void Awake()
    {
        if (!Instance)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Start()
    {
        var cam = Instantiate(cameraPrefab);
        orbit = cam.GetComponent<CinemachineOrbitalFollow>();
        controller = cam.GetComponent<CinemachineInputAxisController>();

        DontDestroyOnLoad(cam);

        EnableCamera(false);
    }

    public void Initialize(Transform tr)
    {
        SetFollowTarget(tr);

        LookMove();

        SetEulerAngles(tr.rotation.eulerAngles.y);
    }

    public void EnableCamera(bool enable)
    {
        orbit.gameObject.SetActive(enable);
    }

    public void EnableControl(bool enable)
    {
        controller.enabled = enable;

        orbit.HorizontalAxis.Value = 0;
    }

    public void SetFollowTarget(Transform target)
    {
        orbit.VirtualCamera.Follow = target;
        orbit.VirtualCamera.LookAt = target;
    }

    public void LookAround()
    {
        orbit.HorizontalAxis.Range = range;
    }

    public void LookMove()
    {
        orbit.HorizontalAxis.Range = Vector2.zero;
    }

    public void SetEulerAngles(float angle)
    {
        orbit.HorizontalAxis.Value = angle;
    }
}