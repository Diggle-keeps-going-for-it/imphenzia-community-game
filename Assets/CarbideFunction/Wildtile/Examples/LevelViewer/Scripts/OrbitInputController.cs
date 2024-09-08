using UnityEngine;
using StarterAssets;

public class OrbitInputController : MonoBehaviour
{
    [SerializeField] private OrbitController _orbitController;
    [SerializeField] private StarterAssetsInputs _input;

    private void Update()
    {
        Move();
    }

    private void Move()
    {
        if (_input.isLookModifierHeld)
        {
            _orbitController.ApplyDeltaToOrbitPosition(_input.look);
        }
    }
}
