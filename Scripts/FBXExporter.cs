using Autodesk.Fbx;
using UnityEngine;

public class FBXExporter : MonoBehaviour
{
    public static void ExportSingleObject(GameObject targetObject, string fileName) {
        using (FbxManager fbxManager = FbxManager.Create())
        {
            // Configure IO settings
            fbxManager.SetIOSettings(FbxIOSettings.Create(fbxManager, Globals.IOSROOT));

            // Create an exporter
            using (FbxExporter exporter = FbxExporter.Create(fbxManager, "Exporter"))
            {
                // Initialize exporter
                if (!exporter.Initialize(fileName, -1, fbxManager.GetIOSettings()))
                {
                    Debug.LogError("Failed to initialize FBX Exporter.");
                    return;
                }

                // Create a new FBX scene
                FbxScene fbxScene = FbxScene.Create(fbxManager, "MyScene");

                // Export the GameObject hierarchy
                ExportGameObject(fbxScene, targetObject);

                // Export the scene to file
                exporter.Export(fbxScene);
                Debug.Log($"Successfully exported {targetObject.name} to {fileName}");
            }
        }
    }

    private static void ExportGameObject(FbxScene fbxScene, GameObject gameObject)
    {
        // Create a root node in the FBX scene
        FbxNode fbxRootNode = fbxScene.GetRootNode();

        // Export the hierarchy
        FbxNode fbxNode = ExportNode(fbxScene, gameObject);
        fbxRootNode.AddChild(fbxNode);
    }

    private static FbxNode ExportNode(FbxScene fbxScene, GameObject gameObject)
    {
        // Create a new node for this GameObject
        FbxNode fbxNode = FbxNode.Create(fbxScene, gameObject.name);

        // Set the node's transform
        var localPosition = gameObject.transform.localPosition;
        var localRotation = gameObject.transform.localRotation.eulerAngles;
        var localScale = gameObject.transform.localScale;

        fbxNode.LclTranslation.Set(new FbxDouble3(localPosition.x, localPosition.y, localPosition.z));
        fbxNode.LclRotation.Set(new FbxDouble3(localRotation.x, localRotation.y, localRotation.z));
        fbxNode.LclScaling.Set(new FbxDouble3(localScale.x, localScale.y, localScale.z));

        // Export the mesh (if the GameObject has one)
        MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            FbxMesh fbxMesh = ExportMesh(fbxScene, meshFilter.sharedMesh);
            fbxNode.SetNodeAttribute(fbxMesh);
        }

        // Recursively export children
        foreach (Transform child in gameObject.transform)
        {
            FbxNode childNode = ExportNode(fbxScene, child.gameObject);
            fbxNode.AddChild(childNode);
        }

        return fbxNode;
    }

    private static FbxMesh ExportMesh(FbxScene fbxScene, Mesh mesh)
    {
        // Create an FBX mesh
        FbxMesh fbxMesh = FbxMesh.Create(fbxScene, mesh.name);

        // Set the control points (vertices)
        fbxMesh.InitControlPoints(mesh.vertexCount);

        //FbxVector4[] controlPoints = fbxMesh.GetControlPoints();
        FbxVector4[] controlPoints = new FbxVector4[fbxMesh.GetControlPointsCount()];
        for(int i = 0; i < controlPoints.Length; i++) {
            controlPoints[i] = fbxMesh.GetControlPointAt(i);
        }

        for (int i = 0; i < mesh.vertexCount; i++)
        {
            Vector3 vertex = mesh.vertices[i];
            controlPoints[i] = new FbxVector4(vertex.x, vertex.y, vertex.z);
        }

        // Set the polygon indices
        for (int i = 0; i < mesh.triangles.Length; i += 3)
        {
            fbxMesh.BeginPolygon();
            fbxMesh.AddPolygon(mesh.triangles[i]);
            fbxMesh.AddPolygon(mesh.triangles[i + 1]);
            fbxMesh.AddPolygon(mesh.triangles[i + 2]);
            fbxMesh.EndPolygon();
        }

        // Optionally export normals, UVs, etc.
        ExportNormals(fbxMesh, mesh);
        ExportUVs(fbxMesh, mesh);

        return fbxMesh;
    }

    private static void ExportNormals(FbxMesh fbxMesh, Mesh mesh)
    {
        if (mesh.normals.Length == 0) return;

        FbxLayer layer = fbxMesh.GetLayer(0);
        if (layer == null)
        {
            fbxMesh.CreateLayer();
            layer = fbxMesh.GetLayer(0);
        }

        FbxLayerElementNormal normals = FbxLayerElementNormal.Create(fbxMesh, "Normals");
        normals.SetMappingMode(FbxLayerElement.EMappingMode.eByControlPoint);
        normals.SetReferenceMode(FbxLayerElement.EReferenceMode.eDirect);

        foreach (Vector3 normal in mesh.normals)
        {
            normals.GetDirectArray().Add(new FbxVector4(normal.x, normal.y, normal.z));
        }

        layer.SetNormals(normals);
    }

    private static void ExportUVs(FbxMesh fbxMesh, Mesh mesh)
    {
        if (mesh.uv.Length == 0) return;

        FbxLayer layer = fbxMesh.GetLayer(0);
        if (layer == null)
        {
            fbxMesh.CreateLayer();
            layer = fbxMesh.GetLayer(0);
        }

        FbxLayerElementUV uvs = FbxLayerElementUV.Create(fbxMesh, "UVs");
        uvs.SetMappingMode(FbxLayerElement.EMappingMode.eByControlPoint);
        uvs.SetReferenceMode(FbxLayerElement.EReferenceMode.eDirect);

        foreach (Vector2 uv in mesh.uv)
        {
            uvs.GetDirectArray().Add(new FbxVector2(uv.x, uv.y));
        }

        layer.SetUVs(uvs, FbxLayerElement.EType.eTextureDiffuse);
    }
}
