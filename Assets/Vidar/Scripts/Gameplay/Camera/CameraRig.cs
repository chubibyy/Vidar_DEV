using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class CameraRig : MonoBehaviour
{
    public enum Mode { Master, TPS }

    [Header("Links")]
    public Camera cam;

    [Header("Master View")]
    public Transform masterPivotP1;
    public Transform masterPivotP2;
    [Range(1f, 100f)] public float masterMoveSpeed = 25f;
    [Range(50f, 800f)] public float masterZoomSpeed = 300f;

    [Header("TPS View")]
    public Vector3 tpsOffset = new Vector3(0f, 2.0f, -4f);
    [Range(1f, 30f)] public float followLerp = 10f;

    private Mode _mode = Mode.Master;
    private Transform _currentMasterPivot;
    private Transform _tpsFollowTarget;

    void Start()
    {
        if (!cam) cam = Camera.main;

        // Choix auto du pivot master selon le côté local
        var tm = FindAnyObjectByType<TurnManager>(FindObjectsInactive.Include);
        int me = (tm != null) ? tm.IsReady ? tm.IsMyTurn() ? tm.IsMyTurn() ? 0 : 0 : 0 : 0 : 0; // dummy to silence analyzer (we'll set properly below)
        if (tm != null)
        {
            // si le local est player 0 → pivot P1, sinon P2
            int localIndex = -1;
            // méthode utilitaire interne du TM
            var getIdx = tm.GetType().GetMethod("GetLocalPlayerIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (getIdx != null) localIndex = (int)getIdx.Invoke(tm, null);

            _currentMasterPivot = (localIndex == 1) ? masterPivotP2 : masterPivotP1;
        }
        else
        {
            _currentMasterPivot = masterPivotP1 ? masterPivotP1 : masterPivotP2;
        }

        SetMode(Mode.Master);
        SnapToMasterPivot();
    }

    void LateUpdate()
    {
        if (!cam) return;

        if (_mode == Mode.TPS)
        {
            if (_tpsFollowTarget)
            {
                Vector3 targetPos = _tpsFollowTarget.position + tpsOffset;
                cam.transform.position = Vector3.Lerp(cam.transform.position, targetPos, Time.deltaTime * followLerp);
                cam.transform.LookAt(_tpsFollowTarget.position + Vector3.up * 1.2f);
            }
        }
        else // Master
        {
#if ENABLE_INPUT_SYSTEM
            var kb = Keyboard.current;
            var ms = Mouse.current;

            Vector3 move = Vector3.zero;
            if (kb != null)
            {
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  move.x -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move.x += 1f;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    move.z += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  move.z -= 1f;
            }

            if (_currentMasterPivot) _currentMasterPivot.position += move.normalized * masterMoveSpeed * Time.deltaTime;

            if (ms != null)
            {
                float scroll = ms.scroll.ReadValue().y; // lignes
                cam.transform.position += cam.transform.forward * (scroll * 0.01f) * masterZoomSpeed * Time.deltaTime;
            }

            if (_currentMasterPivot)
                cam.transform.LookAt(_currentMasterPivot.position);
#endif
        }
    }

    public void SetMode(Mode m)
    {
        _mode = m;
        if (_mode == Mode.Master)
        {
            _tpsFollowTarget = null;
            if (_currentMasterPivot) cam.transform.LookAt(_currentMasterPivot.position);
        }
    }

    public void Follow(Transform target)
    {
        _tpsFollowTarget = target;
        _mode = Mode.TPS;
    }

    public void SetMasterPivotForPlayer(int playerIndex)
    {
        _currentMasterPivot = (playerIndex == 1) ? masterPivotP2 : masterPivotP1;
    }

    public void SnapToMasterPivot()
    {
        if (_currentMasterPivot)
        {
            // place la caméra à une distance correcte et regarde le pivot
            Vector3 dir = (cam.transform.position - _currentMasterPivot.position).normalized;
            if (dir.sqrMagnitude < 0.01f) dir = new Vector3(0, 1, -1).normalized;
            float dist = 15f;
            cam.transform.position = _currentMasterPivot.position + dir * dist;
            cam.transform.LookAt(_currentMasterPivot.position);
        }
    }
}
