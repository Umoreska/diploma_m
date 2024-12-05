using UnityEngine;
using GLTFast;
using GLTFast.Export;

public class GLTFastExporter : MonoBehaviour {

    public async static void SimpleExport(GameObject[] game_objects, string path) {

        // GameObjectExport lets you create glTFs from GameObject hierarchies
        var export = new GameObjectExport();

        // Add a scene
        //export.AddScene(rootLevelNodes);
        export.AddScene(game_objects);

        // Async glTF export
        bool success = await export.SaveToFileAndDispose(path);

        if(!success) {
            Debug.LogError("Something went wrong exporting a glTF");
        }
    }
}