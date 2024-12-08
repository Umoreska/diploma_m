using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using System;
using System.Diagnostics.Tracing;
public enum TestMode {
    None, Noise, Mesh, ChunkUpdate
}
public class Tester : MonoBehaviour
{
    [SerializeField] private TestMode test_mode;
    [SerializeField] private int size = 241;
    [SerializeField] private float noiseScale = 50f;
    [SerializeField] private int octaves = 4;
    [SerializeField] private float heightMultiplier;
    [SerializeField] private AnimationCurve mesh_height_curve;
    [SerializeField] private Material mesh_material, terrain_material;
    [SerializeField] private InfiniteTerrainGeneration infiniteTerrainGeneration;
    [SerializeField] private GameObject player;
    [SerializeField] private float player_speed, rotation_speed;
    [SerializeField] private bool append=false;
    void Start()
    {
        infiniteTerrainGeneration.enabled = false;
        switch(test_mode) {
            case TestMode.Noise:
                // test noise generation
                //TestBuiltInNoiseAndImprovedNoiseSpeed();
                DatasetCreator.CreateHeightMapCSV("HeightMapData.csv");
            break;
            case TestMode.Mesh:
                // test mesh creation
                TestMeshTerrainSpeedGeneration();
            break;
            case TestMode.ChunkUpdate:
                infiniteTerrainGeneration.enabled = true;
                StartCoroutine(MovePlayerOnTerrain());
                StartCoroutine(CreateChunkData("ChunkData.csv", 1f, 100));
            break;
        }
    }

    private IEnumerator MovePlayerOnTerrain() {
        while(true){
            player.transform.position += player_speed * Time.deltaTime * Vector3.forward;
            player.transform.Rotate(0, rotation_speed * Time.deltaTime, 0);
            yield return null;
        }
    }

    private IEnumerator CreateChunkData(string csv_file_name, float delta_time, int rows_count=1) {
        List<DatasetCreator.CSVChunkData> data = new List<DatasetCreator.CSVChunkData>();
        int rows_written = 0;
        while(rows_written < rows_count) {            

            float timer=0;
            int fps_counter=0;
            while(timer < delta_time) {
                timer += Time.deltaTime;
                fps_counter++;
                yield return null;
            }

            infiniteTerrainGeneration.GetInfoAboutChunks(out int all, out int active);
            data.Add(new DatasetCreator.CSVChunkData{
                UpdateMode = infiniteTerrainGeneration.UpdateMode,
                ChunkCount = all,
                ActiveChunkCount = active,
                FPS = fps_counter
            });
            rows_written++;
            Debug.Log($"row created: {rows_written}");
        }
        Debug.Log("Saving to csv");
        DatasetCreator.WriteToCsv(csv_file_name, data, append);
    }

    public void TestMeshTerrainSpeedGeneration() {

        if (Application.isPlaying) {
            var mesh_object = GameObject.Find("Mesh Chunk");
            var terrain_object = GameObject.Find("Terrain Chunk");
            if(mesh_object != null) {
                Destroy(mesh_object);
            }
            if(terrain_object != null) {
                Destroy(terrain_object);
            }
        }
        

        float[,] height_map = FractalPerlinNoise.GenerateHeights(size, 0, noiseScale, octaves, 0.5f , 2f, Vector2.zero, FractalPerlinNoise.NormalizeMode.Global, FractalPerlinNoise.Noise.UnityPerlin);

        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();


        int width = height_map.GetLength(0);
        int height = height_map.GetLength(1);
        Color[] mesh_color_map = new Color[width * height];
        Color[] terrain_color_map = new Color[width * height];
        for(int i = 0; i < height; i++) {
            for(int j = 0; j < width; j++) {
                mesh_color_map[i*width + j] = Color.Lerp(Color.black, Color.white, height_map[j,i]);
                terrain_color_map[i*width + j] = Color.Lerp(Color.black, Color.white, height_map[i,j]);
            }
        }

        // my mesh
        sw.Start();
        GameObject meshObject = new("Mesh Chunk");
        
        var meshRenderer = meshObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = mesh_material;
        meshRenderer.sharedMaterial.mainTexture = MapDisplay.CreateTexture(mesh_color_map, size, size);

        var meshFilter = meshObject.AddComponent<MeshFilter>();
        MeshData mesh_data = MeshGenerator.GenerateTerrainMesh(height_map, heightMultiplier, mesh_height_curve, 0, false);

        meshFilter.sharedMesh = mesh_data.CreateMesh();
        sw.Stop();
        Debug.Log($"creation time of own mesh terrain: {sw.ElapsedMilliseconds} ms");


        sw.Restart();
        GameObject terrainObject = new("Terrain Chunk");
        terrainObject.transform.position = new Vector3(size/2, 0, -size/2);

        Terrain terrain = terrainObject.AddComponent<Terrain>();        
        TerrainCollider terrainCollider = terrainObject.AddComponent<TerrainCollider>();        
        TerrainData terrainData = terrain.terrainData = new TerrainData();

        terrain.materialTemplate = terrain_material;
        terrain.materialTemplate.mainTexture = MapDisplay.CreateTexture(terrain_color_map, size, size);

         // Create a new TerrainLayer
        TerrainLayer terrainLayer = new TerrainLayer();
        terrainLayer.diffuseTexture = MapDisplay.CreateTexture(terrain_color_map, size, size);
        terrainLayer.tileSize = new Vector2(size, size); // Adjust tile size
        terrainData.terrainLayers = new TerrainLayer[] { terrainLayer };

        terrainData.heightmapResolution = size + 1;
        terrainData.size = new Vector3(size, heightMultiplier, size);
        terrainData.SetHeights(0, 0, height_map);

        terrainCollider.terrainData = terrainData;

        terrain.terrainData = terrainData;
        sw.Stop();
        Debug.Log($"creation time of unity terrain: {sw.ElapsedMilliseconds} ms");
    }

    public void TestBuiltInNoiseAndImprovedNoiseSpeed() {
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        
        sw.Start();
        float[,] height_map = FractalPerlinNoise.GenerateHeights(size, 0, noiseScale, octaves, 0.5f , 2f, Vector2.zero, FractalPerlinNoise.NormalizeMode.Global, FractalPerlinNoise.Noise.UnityPerlin);
        sw.Stop();
        Debug.Log($"creation time of unity noise: {sw.ElapsedMilliseconds} ms");

        sw.Restart();
        height_map = FractalPerlinNoise.GenerateHeights(size, 0, noiseScale, octaves, 0.5f , 2f, Vector2.zero, FractalPerlinNoise.NormalizeMode.Global, FractalPerlinNoise.Noise.FastNoiseLiteSimplex);
        sw.Stop();
        Debug.Log($"creation time of simplex noise: {sw.ElapsedMilliseconds} ms");

    }
}
