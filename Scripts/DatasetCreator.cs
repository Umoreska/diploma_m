using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class DatasetCreator : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void CreateHeightMapCSV(string csv_file_name) {
        // Список для зберігання даних про ландшафти
        List<CSVHeightMapData> terrainDataList = new();

        for(int pow_of_2 = 7; pow_of_2 <= 10; pow_of_2++) {
            int size = (int)Mathf.Pow(2, pow_of_2);
            for(int scale = 1; scale <= 100; scale++) { // for fractal
                for(int octave = 1; octave <= 4; octave++) {
                    
                    System.Diagnostics.Stopwatch sw = new();
                    sw.Start();
                    float[,] height = FractalPerlinNoise.GenerateHeights(size, 0, scale, octave, 0.5f, 2f, Vector2.zero, FractalPerlinNoise.NormalizeMode.Local, FractalPerlinNoise.Noise.UnityPerlin);
                    sw.Stop();
                    terrainDataList.Add(new CSVHeightMapData
                    {
                        Algorithm = "FractalPerlinNoise",
                        Size = size,
                        Scale_Roughness_PointCount = scale,
                        Octaves = octave,
                        Time = sw.ElapsedMilliseconds
                    });
                }
                
            }
        }
         
        WriteToCsv(csv_file_name, terrainDataList);
    }

    public static void WriteToCsv(string filePath, List<CSVHeightMapData> data, bool append=false) {
        using (var writer = new StreamWriter(filePath, append)) {
            string first_line = "Algorithm;Size;Scale_Roughness_PointCount;Octaves;Time";
            writer.WriteLine(first_line);
            // Приклад циклу для додавання рядків
            for (int i = 0; i < data.Count; i++) {
                // Створюємо рядок для запису
                string line = $"{data[i].Algorithm};{data[i].Size};{data[i].Scale_Roughness_PointCount};{data[i].Octaves};{data[i].Time}".Replace(",", ".");

                // Записуємо рядок у файл
                writer.WriteLine(line);
            }
        } 
    }

    public static void WriteToCsv(string filePath, List<CSVChunkData> data, bool append=false) {
        using (var writer = new StreamWriter(filePath, append)) {
            if(append == false) {
                string first_line = "UpdateMode;ChunkCount;ActiveChunkCount;FPS";
                writer.WriteLine(first_line);
            }
            // Приклад циклу для додавання рядків
            for (int i = 0; i < data.Count; i++) {
                // Створюємо рядок для запису
                string line = $"{data[i].UpdateMode};{data[i].ChunkCount};{data[i].ActiveChunkCount};{data[i].FPS}".Replace(",", ".");

                // Записуємо рядок у файл
                writer.WriteLine(line);
            }
        }
        Debug.Log($"DONE WRITING TO {filePath}");
    }


    public struct CSVHeightMapData{
        public string Algorithm;
        public int Size;
        public int Scale_Roughness_PointCount;
        public int Octaves;
        public float Time;
    }
    public struct CSVChunkData{
        public string UpdateMode;
        public int ChunkCount;
        public int ActiveChunkCount;
        public int FPS;
    }


}
