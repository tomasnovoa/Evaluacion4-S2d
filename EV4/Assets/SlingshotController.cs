using UnityEngine;
using UnityEngine.InputSystem;
using PUCV.PhysicEngine2D;

public class SlingshotController : MonoBehaviour
{
    [Header("Referencias")]
    public Transform anchorPoint;
    public GameObject projectilePrefab;       
    public PhysicsManager2D physicsManager;

    [Header("Ajustes de disparo")]
    public float maxPullDistance = 3f;
    public float launchPower = 10f;

    [Header("Debug")]
    public int trajectorySegments = 30;

    private CustomRigidbody2D _currentRb;
    private Vector2 _currentLaunchVelocity;
    private bool _isDragging;

    private void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
            StartDrag();
        else if (Mouse.current.leftButton.isPressed && _isDragging)
            UpdateDrag();
        else if (Mouse.current.leftButton.wasReleasedThisFrame && _isDragging)
            Release();
    }

    private void StartDrag()
    {

        if (_currentRb == null)
        {
            if (projectilePrefab == null || anchorPoint == null)
                return;

            GameObject go = Instantiate(projectilePrefab, anchorPoint.position, Quaternion.identity);
            _currentRb = go.GetComponent<CustomRigidbody2D>();
            if (_currentRb == null)
                _currentRb = go.AddComponent<CustomRigidbody2D>();

            _currentRb.velocity = Vector2.zero;
        }

        _isDragging = true;
    }

    private void UpdateDrag()
    {
        if (_currentRb == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(mousePos);
        mouseWorld.z = 0f;

        Vector2 anchorPos = anchorPoint.position;
        Vector2 pullDir = (Vector2)mouseWorld - anchorPos;

        if (pullDir.magnitude > maxPullDistance)
            pullDir = pullDir.normalized * maxPullDistance;

        _currentRb.SetWoldPosition(anchorPos + pullDir);
        _currentRb.velocity = Vector2.zero;

        _currentLaunchVelocity = -pullDir * launchPower;
    }

    private void Release()
    {
        _isDragging = false;

        if (_currentRb != null)
        {
            _currentRb.velocity = _currentLaunchVelocity;

            // Destruye el obj
            Destroy(_currentRb.gameObject, 30f);

            _currentRb = null;
        }
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        if (_currentRb == null) return;
        if (physicsManager == null) return;

        Vector2 g = physicsManager.globalGravity;

        Gizmos.color = Color.yellow;

        Vector2 startPos = _currentRb.GetWorldPosition();
        Vector2 v0 = _currentLaunchVelocity;

        float dt = 0.1f;
        Vector3 prev = startPos;

        for (int i = 1; i <= trajectorySegments; i++)
        {
            float t = i * dt;
            Vector2 p = startPos + v0 * t + 0.5f * g * (t * t);
            Gizmos.DrawLine(prev, p);
            prev = p;
        }
    }
}


