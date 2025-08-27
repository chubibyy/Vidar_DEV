using UnityEngine;
using Unity.Netcode;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem; // Mouse.current, etc.
#endif

public class PlacementClient : MonoBehaviour
{
    public PlayerDeck deck;
    public CameraRig cameraRig;
    public LayerMask placementMask = ~0; // couche du sol
    public Transform spawnZoneP1;
    public Transform spawnZoneP2;

    private CardDefinition _selectedCard;

    void Update()
    {
        if (_selectedCard == null || cameraRig == null || cameraRig.cam == null) return;
        if (IsPointerOverUI()) return; // évite les clics sur l'UI

        // === New Input System ===
        #if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 pos = Mouse.current.position.ReadValue();
            var ray = cameraRig.cam.ScreenPointToRay(pos);
            TryPlaceAtRay(ray);
        }
        #else
        // === Legacy / Both ===
        if (Input.GetMouseButtonDown(0))
        {
            var ray = cameraRig.cam.ScreenPointToRay(Input.mousePosition);
            TryPlaceAtRay(ray);
        }
        #endif
    }

    private void TryPlaceAtRay(Ray ray)
    {
        if (Physics.Raycast(ray, out var hit, 500f, placementMask))
        {
            var tm = FindAnyObjectByType<TurnManager>(FindObjectsInactive.Include);
            if (tm != null)
            {
                tm.PlaceHeroServerRpc(_selectedCard.cardId, hit.point);
                _selectedCard = null; // on sort du mode placement (une carte = un placement)
            }
        }
    }

    private bool IsPointerOverUI()
    {
        // Suffisant avec InputSystemUIInputModule comme avec StandaloneInputModule
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    // Appelé par les boutons de cartes (index 0..4)
    public void SelectCardByIndex(int index)
    {
        if (deck == null || deck.startingCards == null) return;
        if (index < 0 || index >= deck.startingCards.Length) return;

        _selectedCard = deck.startingCards[index];
        Debug.Log($"[Placement] Carte sélectionnée: {_selectedCard.displayName}");
        // Assure la vue Master pour choisir l’emplacement tranquillement (optionnel)
        var rig = cameraRig;
        if (rig != null) rig.SetMode(CameraRig.Mode.Master);
    }
}
