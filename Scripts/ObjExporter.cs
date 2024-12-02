using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class ObjExporter
{
    public static void ExportMeshToObj(GameObject gameObject, string filePath)
    {
        MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogError("No MeshFilter or Mesh found on the GameObject.");
            return;
        }

        Mesh mesh = meshFilter.sharedMesh;
        StringBuilder objString = new StringBuilder();

        // Write OBJ file header
        objString.AppendLine("# Exported by ObjExporter");
        objString.AppendLine("o " + gameObject.name);

        // Export vertices
        foreach (Vector3 vertex in mesh.vertices)
        {
            Vector3 transformedVertex = gameObject.transform.TransformPoint(vertex);
            objString.AppendLine($"v {transformedVertex.x} {transformedVertex.y} {transformedVertex.z}");
        }

        // Export normals
        foreach (Vector3 normal in mesh.normals)
        {
            Vector3 transformedNormal = gameObject.transform.TransformDirection(normal);
            objString.AppendLine($"vn {transformedNormal.x} {transformedNormal.y} {transformedNormal.z}");
        }

        // Export UVs
        foreach (Vector2 uv in mesh.uv)
        {
            objString.AppendLine($"vt {uv.x} {uv.y}");
        }

        // Export triangles
        for (int i = 0; i < mesh.subMeshCount; i++)
        {
            int[] triangles = mesh.GetTriangles(i);
            for (int j = 0; j < triangles.Length; j += 3)
            {
                // OBJ format uses 1-based indexing
                objString.AppendLine($"f {triangles[j] + 1}/{triangles[j] + 1}/{triangles[j] + 1} " +
                                     $"{triangles[j + 1] + 1}/{triangles[j + 1] + 1}/{triangles[j + 1] + 1} " +
                                     $"{triangles[j + 2] + 1}/{triangles[j + 2] + 1}/{triangles[j + 2] + 1}");
            }
        }

        // Write to file
        File.WriteAllText(filePath, objString.ToString());
        Debug.Log($"OBJ file successfully exported to {filePath}");
    }
}
