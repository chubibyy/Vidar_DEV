using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(CharacterController))]
public class HeroController : NetworkBehaviour
{
    private CharacterController _cc;
    public float moveSpeed = 5f;
    public float turnSpeed = 360f;

    private void Awake() { _cc = GetComponent<CharacterController>(); }

    void Update()
    {
        // Autoriser le contrôle seulement :
        // - si on "possède" cet objet (Ownership côté client)
        // - et si c'est notre tour
        if (!IsOwner) return;

        var tm = FindAnyObjectByType<TurnManager>(FindObjectsInactive.Include);
        if (tm == null || !tm.IsMyTurn()) return;

        // Input basique (WSAD / flèches) - New Input System mappez sur axes "Horizontal/Vertical" si nécessaire
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 dir = new Vector3(h, 0, v);
        if (dir.sqrMagnitude > 0.001f)
        {
            // orientation + déplacement local (client-side pour MVP; serveur reste source d’état macro)
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), turnSpeed * Time.deltaTime);
            _cc.Move(dir.normalized * moveSpeed * Time.deltaTime);
        }
    }
}
