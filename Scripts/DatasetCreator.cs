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
    static public void CreateHeightMapCSV(string csv_file_name) {
        // Список для зберігання даних про ландшафти
        List<CSVHeightMapData> height_map_data = new();

        for(int pow_of_2 = 6; pow_of_2 <= 10; pow_of_2++) {
            int size = (int)Mathf.Pow(2, pow_of_2);
            for(int scale = 10; scale <= 100; scale+=10) { // for fractal
                for(int octave = 1; octave <= 10; octave++) {
                    
                    System.Diagnostics.Stopwatch sw = new();
                    sw.Start();
                    float[,] height = FractalPerlinNoise.GenerateHeights(size, 0, scale, octave, 0.5f, 2f, Vector2.zero, FractalPerlinNoise.NormalizeMode.Local, FractalPerlinNoise.Noise.UnityPerlin);
                    sw.Stop();
                    height_map_data.Add(new CSVHeightMapData
                    {
                        Algorithm = "FractalPerlinNoise",
                        Size = size,
                        Scale_Roughness_PointCount_StartSize = scale,
                        Octaves = octave,
                        Time = sw.ElapsedMilliseconds
                    });
                }                
            }
            for(int roughness = 1; roughness <= 10; roughness+=2) { // for diamond-square
                    
                System.Diagnostics.Stopwatch sw = new();
                sw.Start();
                float[,] heights = DiamondSquareTerrain.GenerateHeights(size+1, roughness, 0);
                sw.Stop();
                height_map_data.Add(new CSVHeightMapData
                {
                    Algorithm = "DiamondSquare",
                    Size = size,
                    Scale_Roughness_PointCount_StartSize = roughness,
                    Octaves = 1,
                    Time = sw.ElapsedMilliseconds
                });
                              
            }
            for(int points_count = 10; points_count <= 100; points_count+=10) {// for voronoi
                
                System.Diagnostics.Stopwatch sw = new();
                sw.Start();
                float[,] heights = VoronoiTerrain.GenerateHeights(size, points_count, 0);
                sw.Stop();
                height_map_data.Add(new CSVHeightMapData
                {
                    Algorithm = "Voronoi",
                    Size = size,
                    Scale_Roughness_PointCount_StartSize = points_count,
                    Octaves = 1,
                    Time = sw.ElapsedMilliseconds
                });
            }                            
            for(int start_size = 16; start_size <= size; start_size*=2) {// for DLA
                int upscale_count = (int)(Mathf.Log(size, 2) - Mathf.Log(start_size, 2));
                System.Diagnostics.Stopwatch sw = new();
                sw.Start();
                float[,] heights = DLA.RunDLA(start_size, upscale_count);
                sw.Stop();
                height_map_data.Add(new CSVHeightMapData
                {
                    Algorithm = "DLA",
                    Size = size,
                    Scale_Roughness_PointCount_StartSize = start_size,
                    Octaves = upscale_count,
                    Time = sw.ElapsedMilliseconds
                });
            }    
        }
         
        WriteToCsv(csv_file_name, height_map_data);
    }

    public static void WriteToCsv(string filePath, List<CSVHeightMapData> data, bool append=false) {
        using (var writer = new StreamWriter(filePath, append)) {
            string first_line = "Algorithm;Size;Scale_Roughness_PointCount_StartSize;Octaves;Time";
            writer.WriteLine(first_line);
            // Приклад циклу для додавання рядків
            for (int i = 0; i < data.Count; i++) {
                // Створюємо рядок для запису
                string line = $"{data[i].Algorithm};{data[i].Size};{data[i].Scale_Roughness_PointCount_StartSize};{data[i].Octaves};{data[i].Time}".Replace(",", ".");

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
        public int Scale_Roughness_PointCount_StartSize;
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
