using System.Collections;
using UnityEngine;
using Unity.Netcode;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class CameraRig : MonoBehaviour
{
    public enum Mode { Master, TPS }

    [Header("References")]
    [SerializeField] private Camera cam;
    public Camera Cam => cam;

    [Header("Master View")]
    [SerializeField] private Transform masterPivotP1; // côté joueur 0
    [SerializeField] private Transform masterPivotP2; // côté joueur 1
    [Range(1f,100f)] public float masterMoveSpeed = 25f;
    [Range(50f,800f)] public float masterZoomSpeed = 300f;
    [Range(5f,50f)]  public float defaultMasterDistance = 18f;
    [Range(20f,80f)] public float masterLookAngle = 45f;

    [Header("TPS View")]
    public Vector3 tpsOffset = new Vector3(0f, 2f, -4f);
    [Range(1f,30f)] public float followLerp = 10f;

    [Header("Server Options")]
    public bool enableServerObserver = false;

    private Mode _mode = Mode.Master;
    private Transform _currentMasterPivot;
    private Transform _tpsFollowTarget;
    private float _currentZoomDist;

    private bool _isDedicatedServer;
    private bool _initialized;

    void Awake()
    {
        if (!cam) cam = Camera.main;
        if (!cam) { Debug.LogError("[CameraRig] Aucune Camera trouvée."); enabled = false; return; }
        if (!masterPivotP1 || !masterPivotP2) { Debug.LogError("[CameraRig] Master pivots non assignés."); enabled = false; return; }

        _currentZoomDist = defaultMasterDistance;

        var nm = NetworkManager.Singleton;
        _isDedicatedServer = nm && nm.IsServer && !nm.IsClient; // serveur dédié pur
    }

    void OnEnable()
    {
        // (ré)initialisation à chaque chargement de scène
        StartCoroutine(InitWhenReady());
    }

    IEnumerator InitWhenReady()
    {
        _initialized = false;

        if (_isDedicatedServer)
        {
            if (!enableServerObserver)
            {
                cam.enabled = false;
                enabled = false;
                yield break;
            }
            _currentMasterPivot = masterPivotP1;
            SetMode(Mode.Master);
            SnapToMasterPivot();
            _initialized = true;
            yield break;
        }

        // CLIENT : attendre que TurnManager existe ET que l'index local soit connu
        TurnManager tm = null;
        int localIdx = -1;

        // attend que le TM soit présent
        while (tm == null)
        {
            tm = FindAnyObjectByType<TurnManager>(FindObjectsInactive.Include);
            yield return null;
        }

        // attend que la liste des joueurs soit répliquée (index != -1)
        var timeout = 3f; // sécurité (3s max)
        while (timeout > 0f)
        {
            localIdx = tm.GetLocalPlayerIndexPublic();
            if (localIdx >= 0) break;
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }
        if (localIdx < 0) localIdx = 0; // fallback doux

        // Choisit le bon pivot selon l'index
        _currentMasterPivot = (localIdx == 1) ? masterPivotP2 : masterPivotP1;

        SetMode(Mode.Master);
        SnapToMasterPivot();
        _initialized = true;
    }

    void LateUpdate()
    {
        if (!_initialized) return;
        if (_mode == Mode.TPS) UpdateTPS();
        else UpdateMaster();
    }

    // ---------- Master ----------
    private void UpdateMaster()
    {
        if (!_currentMasterPivot) return;

#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        var mouse = Mouse.current;

        if (kb != null)
        {
            Vector3 move = Vector3.zero;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  move.x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move.x += 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    move.z += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  move.z -= 1f;

            if (move.sqrMagnitude > 0.01f)
                _currentMasterPivot.position += move.normalized * masterMoveSpeed * Time.deltaTime;

            if (kb.rKey.wasPressedThisFrame)
                SnapToMasterPivot();
        }

        if (mouse != null)
        {
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                _currentZoomDist = Mathf.Clamp(_currentZoomDist - (scroll * 0.1f) * masterZoomSpeed * Time.deltaTime, 5f, 50f);
                PositionMasterCamera();
            }
        }
#else
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 move = new Vector3(h,0,v);
        if (move.sqrMagnitude > 0.01f)
            _currentMasterPivot.position += move.normalized * masterMoveSpeed * Time.deltaTime;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            _currentZoomDist = Mathf.Clamp(_currentZoomDist - scroll * masterZoomSpeed * Time.deltaTime, 5f, 50f);
            PositionMasterCamera();
        }

        if (Input.GetKeyDown(KeyCode.R)) SnapToMasterPivot();
#endif
        MaintainMasterLookAt();
    }

    private void PositionMasterCamera()
    {
        if (!_currentMasterPivot || !cam) return;
        float ang = masterLookAngle * Mathf.Deg2Rad;
        float hDist = _currentZoomDist * Mathf.Cos(ang);
        float vDist = _currentZoomDist * Mathf.Sin(ang);
        Vector3 offset = new Vector3(0f, vDist, -hDist);
        cam.transform.position = _currentMasterPivot.position + offset;
        cam.transform.LookAt(_currentMasterPivot.position);
    }

    private void MaintainMasterLookAt()
    {
        if (!_currentMasterPivot || !cam) return;
        Vector3 lookDir = _currentMasterPivot.position - cam.transform.position;
        if (lookDir.sqrMagnitude > 0.0001f)
        {
            Quaternion t = Quaternion.LookRotation(lookDir);
            cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, t, Time.deltaTime * 5f);
        }
    }

    public void SnapToMasterPivot()
    {
        _currentZoomDist = defaultMasterDistance;
        PositionMasterCamera();
    }

    // ---------- TPS ----------
    private void UpdateTPS()
    {
        if (!_tpsFollowTarget || !cam) { SetMode(Mode.Master); return; }

        Vector3 targetPos = _tpsFollowTarget.position + _tpsFollowTarget.TransformDirection(tpsOffset);
        cam.transform.position = Vector3.Lerp(cam.transform.position, targetPos, Time.deltaTime * followLerp);

        Vector3 look = _tpsFollowTarget.position + Vector3.up * 1.2f;
        Quaternion rot = Quaternion.LookRotation(look - cam.transform.position);
        cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, rot, Time.deltaTime * followLerp);
    }

    // ---------- API ----------
    public void SetMode(Mode m)
    {
        _mode = m;
        if (_mode == Mode.Master) _tpsFollowTarget = null;
    }

    public void Follow(Transform target)
    {
        if (!target) return;
        _tpsFollowTarget = target;
        _mode = Mode.TPS;
    }

    /// <summary>Si besoin, tu peux forcer un refresh manuel (ex: après reconnection).</summary>
    public void RefreshSide()
    {
        StopAllCoroutines();
        StartCoroutine(InitWhenReady());
    }
}
