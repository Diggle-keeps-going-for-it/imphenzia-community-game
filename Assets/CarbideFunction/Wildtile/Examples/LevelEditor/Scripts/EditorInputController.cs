using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using StarterAssets;

public class EditorInputController : MonoBehaviour
{
    [SerializeField] private LevelEditorComponent editor;
    [SerializeField] private StarterAssetsInputs input;
    [SerializeField] private Camera mainCamera;

    private void OnEnable()
    {
        input.createTile += OnCreateTile;
        input.deleteTile += OnDeleteTile;
    }

    private void OnDisable()
    {
        input.createTile -= OnCreateTile;
        input.deleteTile -= OnDeleteTile;

        editor.DisableHighlight();
    }

    private void Update()
    {
        UpdateTileHighlighter();
    }

    private void OnCreateTile()
    {
        editor.AddTileAtCursor(mainCamera, GetCursorPosition());
    }

    private void OnDeleteTile()
    {
        editor.RemoveTileAtCursor(mainCamera, GetCursorPosition());
    }

    private Vector2 GetCursorPosition()
    {
        return (Vector2)Input.mousePosition;
    }

    private void UpdateTileHighlighter()
    {
        editor.HighlightMousePosition(mainCamera, GetCursorPosition());
    }
}
