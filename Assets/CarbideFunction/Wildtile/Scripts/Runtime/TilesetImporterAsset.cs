using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace CarbideFunction.Wildtile
{

/// <summary>
/// This asset stores the import settings for a model so that the model can be reliably reimported to as a <see cref="Tileset"/> asset.
/// </summary>
[CreateAssetMenu(fileName="New Tileset Importer", menuName= MenuConstants.topMenuName+"Tileset Importer", order=MenuConstants.orderBase + 0)]
public class TilesetImporterAsset : ScriptableObject
{
    /// <summary>
    /// The model asset that this importer will read from. The model should have an empty root object and one child per tile, each with mesh renderers on them.
    /// </summary>
    [SerializeField]
    public GameObject sourceModel;

    /// <summary>
    /// Prefabs automatically imported from the <see cref="sourceModel"/> model, these will be overwritten whenever the source model is reimported (either from a model preprocess or a full reimport).
    /// </summary>
    [SerializeField]
    [HideInInspector]
    public List<GameObject> rawImportedModelPrefabs = new List<GameObject>();

    /// <summary>
    /// These prefab variants are based on the prefabs in <see cref="rawImportedModelPrefabs"/>. They are created and destroyed when the <see cref="sourceModel"/> adds and removes sub-models, but are not overwritten when the model changes.
    ///
    /// Designers and artists can modify these variants and their changes will remain even if new objects are added to the source model.
    /// </summary>
    [SerializeField]
    [Tooltip(userEditablePrefabsTooltip)]
    public List<GameObject> userEditableImportedModelPrefabVariants = new List<GameObject>();

    /// <summary>
    /// Extra prefabs to be read alongside the prefab variants in <see cref="userEditedImportedModelPrefabVariants"/> that are created from the <see cref="sourceModel"/> field.
    /// </summary>
    [SerializeField]
    public List<GameObject> extraPrefabs = new List<GameObject>();

    /// <summary>
    /// Settings specific to interpreting the models as tile modules.
    /// </summary>
    [SerializeField]
    public ImporterSettings.Settings importerSettings = new ImporterSettings.Settings();

    /// <summary>
    /// A postprocessor creator will be run after the tileset is imported to create a postprocessor instance.
    ///
    /// The postprocessor is run after <see cref="GridPlacer"/> has placed the modules in memory as a <see cref="Postprocessing.PostprocessableMap"/>, and converts the map into in-game Unity objects.
    /// </summary>
    [SerializeField]
    public Postprocessing.PostprocessorCreator postprocessorCreator = null;

    /// <summary>
    /// Where to store the imported data.
    ///
    /// The importer will create module prefabs in a directory next to this Tileset asset. e.g. if the Tileset is at "/Assets/Tilesets/LandscapeTileset.asset", a module prefab would be stored at "/Assets/Tilesets/LandscapeTileset/FlatGround.prefab"
    /// </summary>
    [SerializeField]
    [FormerlySerializedAs("destinationMarchingCubes")]
    public Tileset destinationTileset;

    internal void NotifyChanged()
    {
        NotifyInstanceChanged(this);
    }

    static private void NotifyInstanceChanged(TilesetImporterAsset instance)
    {
        onTilesetChanged?.Invoke(instance);
    }

    /// <summary>
    /// The receiver signature for when a tileset importer asset is reimported.
    /// </summary>
    public delegate void OnTilesetChanged(TilesetImporterAsset instance);

    /// <summary>
    /// This event will be fired whenever the tileset is reimported, whether through code or through the user clicking the "Reimport" button in Unity.
    /// </summary>
    static public OnTilesetChanged onTilesetChanged = null;

    internal const string userEditablePrefabsTooltip = 
        "Contains prefabs that are extracted from the model you've selected for Source Model. You can double click the prefabs in this list to edit them.\n" +
        "\n" +
        "If you want to add prefabs/modules manually, use the Extra Prefabs field.";
}

}
