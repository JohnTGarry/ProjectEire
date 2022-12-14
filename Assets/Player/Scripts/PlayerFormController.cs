using System.Collections.Generic;
using UnityEngine;

public class PlayerFormController : MonoBehaviour
{
    [SerializeField] private Sprite _humanSprite;
    [SerializeField] private Sprite _foxSprite;
    [SerializeField] private SpriteRenderer _spriteRenderer;

    private BoxCollider2D[] _colliderList;
    public AnimalForm _playerForm { get; private set; }

    public enum AnimalForm
    {
        HUMAN,
        FOX
    }

    private Dictionary<AnimalForm, float> _animalSpritePositionYDict = new Dictionary<AnimalForm, float>
    {
        { AnimalForm.HUMAN, 0.45f },
        { AnimalForm.FOX, 0.22f },
    };

    void Start()
    {
        _colliderList = transform.GetComponentsInChildren<BoxCollider2D>();
    }

    void Update()
    {
        if (Input.GetKeyDown("e"))
        {
            SetAnimalForm();
            SwitchSprites();
            SetPosition();
            SwitchColliders();
        }
    }

    private void SetAnimalForm()
    {
        switch (_playerForm)
        {
            case AnimalForm.HUMAN:
                _playerForm = AnimalForm.FOX;
                return;
            default:
                _playerForm = AnimalForm.HUMAN;
                return;
        }
    }

    private void SwitchSprites()
    {
        switch (_playerForm)
        {
            case AnimalForm.HUMAN:
                _spriteRenderer.sprite = _humanSprite;
                return;
            case AnimalForm.FOX:
                _spriteRenderer.sprite = _foxSprite;
                return;
            default:
                return;
        }
    }

    private void SetPosition()
    {
        Vector3 newPosition = new Vector3(transform.position.x, _animalSpritePositionYDict[_playerForm], transform.position.z);
        transform.SetPositionAndRotation(newPosition, transform.rotation);
    }

    private void SwitchColliders()
    {
        foreach (BoxCollider2D collider in _colliderList)
        {
            collider.enabled = !collider.enabled;
        }
    }
}
