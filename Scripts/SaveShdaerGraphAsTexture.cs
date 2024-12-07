using UnityEngine;
using System.IO;

public class SaveShaderGraphAsTexture : MonoBehaviour
{
    public Material shaderGraphMaterial; // Material created from Shader Graph
    public RenderTexture renderTexture;  // RenderTexture for shader output

    public void SaveShaderGraphToTexture(string filePath) {
        // Ensure RenderTexture and Material are assigned
        if (renderTexture == null || shaderGraphMaterial == null)
        {
            Debug.LogError("RenderTexture or Shader Graph Material is not assigned!");
            return;
        }

        // Render the Shader Graph material to the RenderTexture
        RenderTexture.active = renderTexture;
        Graphics.Blit(null, renderTexture, shaderGraphMaterial);

        // Create a Texture2D to save the RenderTexture's contents
        Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);

        // Copy RenderTexture data into Texture2D
        texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture.Apply();

        // Encode Texture2D to PNG format
        byte[] pngData = texture.EncodeToPNG();

        // Save the PNG to file
        File.WriteAllBytes(filePath.Replace("terrain.gltf", "texture.png"), pngData);

        Debug.Log("Shader Graph output saved as texture at: " + filePath);

        // Cleanup
        RenderTexture.active = null;
        Destroy(texture);
    }
}
