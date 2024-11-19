
using UnityEngine.UI;
using Cinemachine;
using UnityEngine;
using System.IO;
using TMPro;
using UnityEditor.Formats.Fbx.Exporter;
using Autodesk.Fbx;
using UnityEditor;


public class UIController : MonoBehaviour
{
    [SerializeField] private Transform mesh_transform;
    [SerializeField] private Erosion erosion;
    [SerializeField] private DrawMode draw_mode;
    [SerializeField] private MapDisplay map_display;
    [SerializeField] private GameObject[] trees;
    [SerializeField] private MapGenerator map_generator;
    [SerializeField] private GameObject size_input, draw_mode_input, scale_input, seed_input, offset_x_input, offset_y_input, roughness_input, octaves_input, persistance_input, 
                                    lacunarity_input, point_count_input, max_height_input, dla_initial_input, dla_steps_input, erosion_panel, trees_panel;
    [SerializeField] private TMP_Dropdown algorithm_dropdown, draw_mode_dropdown, size_dropdown;
    [SerializeField] private Slider scale_slider, seed_slider, offset_x_slider, offset_y_slider, roughness_slider, octaves_slider, persistance_slider, 
                                    lacunarity_slider, point_count_slider, max_height_slider, dla_initial_slider, dla_steps_slider, erosion_iterations_slider, 
                                    delta_time_slider, trees_seed_slider, trees_count_slider;
    [SerializeField] private CinemachineVirtualCamera camera_on_plane, camera_on_terrain;
    private bool generate_on_input_change=false, use_fallof=false;

    private HeightMapAlgorithm algorithm;

    private void Start() {
        //terrain_data = terrain.terrainData;
    }

    public void GenerateOnInputChange(bool generate_on_input_change) {
        this.generate_on_input_change = generate_on_input_change;
    }

    public void UseFalloff(bool use_fallof) {
        this.use_fallof = use_fallof;
    }

    public void InputChanged() {
        if(generate_on_input_change) {
            Generate();
        }
    }

    public void LookAtPlane() {
        camera_on_terrain.gameObject.SetActive(false);
        camera_on_plane.gameObject.SetActive(true);
        draw_mode_input.SetActive(true);
        max_height_input.SetActive(false);
        draw_mode_dropdown.value = (int)DrawMode.ColorMap;
        draw_mode = DrawMode.ColorMap;
    }
    public void LookAtTerrain() {
        camera_on_plane.gameObject.SetActive(false);
        camera_on_terrain.gameObject.SetActive(true);
        draw_mode_input.SetActive(false);
        max_height_input.SetActive(true);
        draw_mode = DrawMode.Mesh;
    }

 

    public void ErodeMap() {
        if(map.GetLength(0) != map.GetLength(1)) {
            Debug.LogWarning($"map width({map.GetLength(0)}) != map height({map.GetLength(1)})");
            Debug.Break();
            return;
        }
        int erosion_iteration = (int)erosion_iterations_slider.value;
        int size = map.GetLength(0);

        float[] map_array = new float[size*size];
        for(int i = 0; i<size; i++) {
            for(int j = 0; j<size; j++) {
                map_array[i*size + j] = map[i,j];
            }
        }
        
        bool reset_seed = false;
        erosion.Erode(map_array, size, erosion_iteration, reset_seed);

        for(int i = 0; i<size; i++) {
            for(int j = 0; j<size; j++) {
                map[i,j] = map_array[i*size + j];
            }
        }

        ShowResult();
    }

    GameObject[] placed_objects = null;
    public void PlaceTreesOnTerrain() {
        if(placed_objects != null) {
            for(int i = 0; i < placed_objects.Length; i++) {
                Destroy(placed_objects[i]); // use pool instead?
            }
        }
        
        int count = (int)trees_count_slider.value;
        float min_threshold_height = 0.3f; // read
        float max_threshold_height = 0.8f; // read
        int seed = (int)trees_seed_slider.value;

        placed_objects = PlacerOnTerrain.PlaceOnTerrain(map, count, min_threshold_height, max_threshold_height, seed, trees);

        float height_multiplier = max_height_slider.value;
        Vector3 mesh_pos = mesh_transform.position;
        for(int i = 0; i < placed_objects.Length; i++) {

            Vector3 new_tree_pos = placed_objects[i].transform.position; // , map_generator.terrain_data.mesh_height_curve
            Debug.Log($"map[{(int)new_tree_pos.x}, {(int)new_tree_pos.z}]");
            Debug.Log($"({new_tree_pos.x} - {map.GetLength(0)/2}) * {mesh_transform.localScale.x} + {mesh_pos.x}");
            new_tree_pos.y = new_tree_pos.y * map_generator.terrain_data.mesh_height_curve.Evaluate(map[(int)new_tree_pos.x, (int)new_tree_pos.z])*height_multiplier + mesh_pos.y;
            new_tree_pos.x = (new_tree_pos.x - map.GetLength(0)/2) * mesh_transform.localScale.x + mesh_pos.x;
            new_tree_pos.z = (new_tree_pos.z - map.GetLength(1)/2) * mesh_transform.localScale.x + mesh_pos.z; // ?
            placed_objects[i].transform.position = new_tree_pos;

            placed_objects[i].transform.localScale = mesh_transform.localScale;
        }
    }

    public void ExportMeshToFbx() {
        string filePath = Path.Combine(Application.dataPath, "MyTerrain.fbx");
        ExportToFbx(mesh_transform.gameObject, filePath);
    }


    private static void ExportToFbx(GameObject gameObject, string path) {

        ExportModelOptions exportSettings = new ExportModelOptions();
        exportSettings.ExportFormat = ExportFormat.Binary;
        exportSettings.KeepInstances = false;

        ModelExporter.ExportObject(path, gameObject, exportSettings);

        Debug.Log($"Object '{gameObject.name}' exported to FBX at: {path}");
    }    


    private float[,] map = null;
    public void Generate() {
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        sw.Start();

        int _size = (int)Mathf.Pow(2, size_dropdown.value+5);
        Debug.Log(algorithm_dropdown.value);
        algorithm = (HeightMapAlgorithm)algorithm_dropdown.value;
        switch(algorithm) {
            case HeightMapAlgorithm.PerlinNoise:
                map = PerlinNoiseGenerator.GenerateHeights(_size, scale_slider.value);
            break;
            case HeightMapAlgorithm.FractalPerlinNoise:
                FractalPerlinNoise.Noise perlin_noise_type = FractalPerlinNoise.Noise.UnityPerlin; // should i give user access to choose?
                Vector2 offset = new Vector2(offset_x_slider.value, offset_y_slider.value);
                map = FractalPerlinNoise.GenerateHeights(_size, 
                                                    (int)seed_slider.value, scale_slider.value, 
                                                    (int)octaves_slider.value, persistance_slider.value, lacunarity_slider.value, offset, 
                                                    FractalPerlinNoise.NormalizeMode.Local, perlin_noise_type);
            break;
            case HeightMapAlgorithm.DiamondSquare:
                map = DiamondSquareTerrain.GenerateHeights(_size+1, roughness_slider.value, (int)seed_slider.value, false);
            break;
            case HeightMapAlgorithm.Voronoi:
                map = VoronoiTerrain.GenerateHeights(_size, (int)point_count_slider.value, (int)seed_slider.value, false);
            break;
            case HeightMapAlgorithm.DLA:
                int initialGridSize = (int)dla_initial_slider.value;
                int stepAmount = (int)dla_steps_slider.value;
                map = DLA.RunDLA(initialGridSize, stepAmount);
                //int size = initialGridSize * (int)Mathf.Pow(DLA.UPSCALE_FACTOR, stepAmount); // res_size = initial * scale_factor^steps// just in case lol
                break;
            default:
                Debug.LogWarning("Using undefined algorithm: " + algorithm);
                Debug.Break();
            break;
            
        }
        sw.Stop();
        Debug.Log($"time for noise generation: {sw.ElapsedMilliseconds} ms");


        if(use_fallof) {
            float[,] falloff_map = FalloffGenerator.GenerateFalloffMap(map.GetLength(0));
            for(int i = 0; i < map.GetLength(0); i++) {
                for(int j = 0; j < map.GetLength(1); j++) {
                    map[i,j] -= falloff_map[i,j];
                }
            }
        }

        sw.Restart();
        ShowResult();
        sw.Stop();
        Debug.Log($"time for texture/mesh generation: {sw.ElapsedMilliseconds} ms");
    }

    private void ShowResult() {
        if(draw_mode == DrawMode.Mesh) {
            map_display.DrawMesh(map, max_height_slider.value, map_generator.terrain_data.mesh_height_curve, false);
        }else {
            if((DrawMode)draw_mode_dropdown.value == DrawMode.NoiseMap) {
                map_display.DrawNoiseMap(map);
            }else {
                map_display.DrawColorMap(map);
            }
        }
    }

    public void SetAlgorithm(int index) {
        size_input.SetActive(true);
        algorithm = (HeightMapAlgorithm)index;
        seed_input.SetActive(false);
        scale_input.SetActive(false);
        roughness_input.SetActive(false);
        offset_x_input.SetActive(false);
        offset_y_input.SetActive(false);
        octaves_input.SetActive(false);
        persistance_input.SetActive(false);
        lacunarity_input.SetActive(false);
        point_count_input.SetActive(false);
        dla_initial_input.SetActive(false);
        dla_steps_input.SetActive(false);

        switch(algorithm) {
            case HeightMapAlgorithm.PerlinNoise:
                scale_input.SetActive(true);
            break;
            case HeightMapAlgorithm.FractalPerlinNoise:
                scale_input.SetActive(true);
                seed_input.SetActive(true);
                octaves_input.SetActive(true);
                persistance_input.SetActive(true);
                lacunarity_input.SetActive(true);
                offset_x_input.SetActive(true);
                offset_y_input.SetActive(true);
            break;
            case HeightMapAlgorithm.DiamondSquare:
                seed_input.SetActive(true);
                roughness_input.SetActive(true);
            break;
            case HeightMapAlgorithm.Voronoi:
                seed_input.SetActive(true);
                point_count_input.SetActive(true);
            break;
            case HeightMapAlgorithm.DLA:
                size_input.SetActive(false);
                dla_initial_input.SetActive(true);
                dla_steps_input.SetActive(true);
            break;
            default:
                Debug.LogWarning("Set undefined algorithm: " + algorithm);
                Debug.Break();
            break;
        }
        
    }
}
