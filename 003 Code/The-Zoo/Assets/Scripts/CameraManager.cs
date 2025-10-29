using Unity.Cinemachine;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [SerializeField] internal GameObject cameraPrefab;

    private readonly Vector2 range = new(-180, 180);

    internal CinemachineOrbitalFollow Orbit;
    
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
        Orbit = Instantiate(cameraPrefab).GetComponent<CinemachineOrbitalFollow>();

        DontDestroyOnLoad(Orbit.gameObject);

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
        Orbit.gameObject.SetActive(enable);
    }

    public void SetFollowTarget(Transform target)
    {
        Orbit.VirtualCamera.Follow = target;
        Orbit.VirtualCamera.LookAt = target;
    }

    public void LookAround()
    {
        Orbit.HorizontalAxis.Range = range;
    }

    public void LookMove()
    {
        Orbit.HorizontalAxis.Range = Vector2.zero;
    }

    public void SetEulerAngles(float angle)
    {
        Orbit.HorizontalAxis.Value = angle;
    }

    public float GetY()
    {
        return Orbit.transform.rotation.eulerAngles.y;
    }

    public void LookMoveSmooth(float targetYaw)
    {
        var current = Orbit.HorizontalAxis.Value;

        var smooth = Mathf.MoveTowardsAngle(current, targetYaw, 100 * Time.deltaTime);
        Orbit.HorizontalAxis.Value = smooth;
    }
}