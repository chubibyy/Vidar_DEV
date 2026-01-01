using UnityEngine;
using Unity.Netcode;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlacementClient : MonoBehaviour
{
    // public PlayerDeck deck; // REMOVED
    public CameraRig cameraRig;
    public LayerMask placementMask = ~0;
    public Transform spawnZoneP1;
    public Transform spawnZoneP2;

    private CardDefinition _selectedCard;

    void Update()
    {
        if (_selectedCard == null || cameraRig == null || cameraRig.Cam == null) return;
        if (IsPointerOverUI()) return;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 pos = Mouse.current.position.ReadValue();
            var ray = cameraRig.Cam.ScreenPointToRay(pos);
            TryPlaceAtRay(ray);
        }
#else
        if (Input.GetMouseButtonDown(0))
        {
            var ray = cameraRig.Cam.ScreenPointToRay(Input.mousePosition);
            TryPlaceAtRay(ray);
        }
#endif
    }

    private void TryPlaceAtRay(Ray ray)
    {
        if (Physics.Raycast(ray, out var hit, 500f, placementMask))
        {
            Debug.Log($"[Placement] Ray hit {hit.collider.name} @ {hit.point}");
            var tm = FindAnyObjectByType<TurnManager>(FindObjectsInactive.Include);
            if (tm != null)
            {
                Debug.Log($"[Placement] send PlaceHeroServerRpc card={_selectedCard?.cardId}");
                tm.PlaceHeroServerRpc(_selectedCard.cardId, hit.point);
                _selectedCard = null;
            }
        }
        else
        {
            Debug.Log("[Placement] Raycast n’a rien touché (sol sans collider ? layer mask ?)");
        }
    }

    private bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    /// <summary>
    /// Selects a card by ID to be placed on the next click.
    /// </summary>
    public void SelectCard(int cardId)
    {
        if (DeckManager.Instance == null)
        {
            Debug.LogError("DeckManager not found!");
            return;
        }

        var def = DeckManager.Instance.GetCardDef(cardId);
        if (def == null) return;

        _selectedCard = def;
        Debug.Log($"[Placement] Carte sélectionnée: {_selectedCard.displayName}");

        if (cameraRig) cameraRig.SetMode(CameraRig.Mode.Master);
    }
}
