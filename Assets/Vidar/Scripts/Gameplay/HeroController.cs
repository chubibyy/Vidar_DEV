using UnityEngine;
using Unity.Netcode;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CharacterController))]
public class HeroController : NetworkBehaviour
{
    private CharacterController _cc;
    public float moveSpeed = 5f;
    public float turnSpeed = 360f;

    private void Awake() { _cc = GetComponent<CharacterController>(); }

    void Update()
    {
        if (!IsOwner) return;

        var tm = FindAnyObjectByType<TurnManager>(FindObjectsInactive.Include);
        if (tm == null || !tm.IsMyTurn()) return;

        float h = 0f, v = 0f;

#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null)
        {
            h += kb.aKey.isPressed ? -1f : 0f;
            h += kb.dKey.isPressed ?  1f : 0f;
            v += kb.sKey.isPressed ? -1f : 0f;
            v += kb.wKey.isPressed ?  1f : 0f;
        }
#else
        h = Input.GetAxis("Horizontal");
        v = Input.GetAxis("Vertical");
#endif

        Vector3 dir = new Vector3(h, 0, v);
        if (dir.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), turnSpeed * Time.deltaTime);
            _cc.Move(dir.normalized * moveSpeed * Time.deltaTime);
        }
    }
}
