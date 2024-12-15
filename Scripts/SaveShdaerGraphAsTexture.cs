using UnityEngine;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif
public class SaveShaderGraphAsTexture : MonoBehaviour
{
    public Material shaderGraphMaterial; // Material created from Shader Graph
    public RenderTexture renderTexture;  // RenderTexture for shader output

    public Texture SaveShaderGraphToTexture(string filePath, int size) {

        RenderTexture renderTexture = RenderTexture.GetTemporary(size, size);
        Graphics.Blit(null, renderTexture, shaderGraphMaterial);  

        Texture2D texture = new Texture2D(size, size);
        RenderTexture.active = renderTexture;
        texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture.Apply();

        filePath = ChangeFileName(filePath, "texture1.png");
        File.WriteAllBytes(filePath, texture.EncodeToPNG());
#if UNITY_EDITOR
        //AssetDatabase.Refresh();
#endif

        return texture;

    }

    public string ChangeFileName(string path, string new_name) {
        string directory = Path.GetDirectoryName(path);
        return Path.Combine(directory, new_name);
    }

    public Texture2D SaveTextureWithCamera(string path) {
        
         // Створення текстури з Render Texture
        RenderTexture.active = renderTexture;
        Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false);
        texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture.Apply();
        RenderTexture.active = null;

        // Збереження текстури в файл
        path = ChangeFileName(path, "texture_not_flipped.png");
        File.WriteAllBytes(path, texture.EncodeToPNG());

        Debug.Log("Color Map saved to: " + Application.dataPath + "/SavedColorMap.png");

        return texture;
    }
}
