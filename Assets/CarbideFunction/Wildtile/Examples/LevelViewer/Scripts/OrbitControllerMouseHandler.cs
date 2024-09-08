using UnityEngine;

public class OrbitControllerMouseHandler
{
    private OrbitController orbitController = null;
    private bool isControllingOrbiting = false;
    private Vector2 lastFrameMousePosition = Vector2.zero;

    public OrbitControllerMouseHandler(OrbitController orbitController)
    {
        this.orbitController = orbitController;
    }

    public void StartOrbiting()
    {
        isControllingOrbiting = true;
    }

    public void StopOrbiting()
    {
        isControllingOrbiting = false;
    }

    public void Update()
    {
        var mouseDelta = UpdateCachedMousePosition();

        if (isControllingOrbiting)
        {
            ApplyDeltaToOrbit(mouseDelta);
        }
    }

    private void ApplyDeltaToOrbit(Vector2 delta)
    {
        orbitController.ApplyDeltaToOrbitPosition(delta);
    }

    private Vector2 UpdateCachedMousePosition()
    {
        var mouseDelta = (Vector2)Input.mousePosition - lastFrameMousePosition;
        lastFrameMousePosition = Input.mousePosition;

        return mouseDelta;
    }
}
