
using UnityEngine.UI;
using Cinemachine;
using UnityEngine;
using TMPro;
using UnityEditor.Formats.Fbx.Exporter;
using Autodesk.Fbx;
using SimpleFileBrowser;


public class UIController : MonoBehaviour
{
    [SerializeField] private Transform mesh_transform, water_transform;
    [SerializeField] private float min_water_height=-13f;
    private MeshCollider mesh_collider;
    [SerializeField] private GameObject player_prefab, editor_cameras, noise_settings, return_to_editor_hint, place_player_btn;
    private PlayerController player=null;
    [SerializeField] private Erosion erosion;
    [SerializeField] private DrawMode draw_mode;
    [SerializeField] private MapDisplay map_display;
    [SerializeField] private GameObject[] trees;
    [SerializeField] private MapGenerator map_generator;
    [SerializeField] private TMP_InputField infinite_seed_input;
    [SerializeField] private GameObject size_input, draw_mode_input, scale_input, seed_input, offset_x_input, offset_y_input, roughness_input, octaves_input, persistance_input, 
                                    lacunarity_input, point_count_input, max_height_input, water_height_input, dla_initial_input, dla_steps_input, erosion_panel, trees_panel;
    [SerializeField] private TMP_Dropdown algorithm_dropdown, draw_mode_dropdown, size_dropdown;
    [SerializeField] private Slider scale_slider, seed_slider, offset_x_slider, offset_y_slider, roughness_slider, octaves_slider, persistance_slider, 
                                    lacunarity_slider, point_count_slider, max_height_slider, dla_initial_slider, dla_steps_slider, erosion_iterations_slider, 
                                    trees_seed_slider, trees_count_slider, merge_ratio_slider;
    [SerializeField] private CinemachineVirtualCamera camera_on_plane, camera_on_terrain;
    private bool generate_on_input_change=false, use_fallof=false;

    private HeightMapAlgorithm algorithm;

    private void Start() {
        mesh_collider = mesh_transform.GetComponent<MeshCollider>();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
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

    public void ChangeWaterHeight(float height) {
        water_transform.localPosition = new Vector3(0, min_water_height+height*2, 0);
    }

    public void PlacePlayer() {
        if(player == null) {
            player = Instantiate(player_prefab).GetComponent<PlayerController>();
            player.SetActionOnKeyE(ReturnToEditor);
        }
        player.gameObject.SetActive(true);
        editor_cameras.SetActive(false);
        player.transform.position = mesh_transform.position;
        player.PlacePlayerOnTerrainSurface();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void ReturnToEditor() {
        player.gameObject.SetActive(false);
        return_to_editor_hint.SetActive(false);
        editor_cameras.SetActive(true);
        noise_settings.SetActive(true);
        place_player_btn.SetActive(true);


        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void LookAtPlane() {
        camera_on_terrain.gameObject.SetActive(false);
        camera_on_plane.gameObject.SetActive(true);

        camera_on_plane.transform.localPosition = Vector3.zero;

        draw_mode_input.SetActive(true);
        max_height_input.SetActive(false);
        water_height_input.SetActive(false);

        draw_mode_dropdown.value = (int)DrawMode.ColorMap;
        draw_mode = DrawMode.ColorMap;
    }
    public void LookAtTerrain() {
        camera_on_plane.gameObject.SetActive(false);
        camera_on_terrain.gameObject.SetActive(true);

        camera_on_terrain.transform.localPosition = Vector3.zero;

        draw_mode_input.SetActive(false);
        max_height_input.SetActive(true);
        water_height_input.SetActive(true);

        draw_mode = DrawMode.Mesh;
    }

    public void DrawMeshWithMap() {
        if(map == null) {
            return;
        }
        bool use_flat_shading = false; // read from user
        map_display.DrawMesh(map, max_height_slider.value, map_generator.terrain_data.mesh_height_curve, use_flat_shading);
        UpdateMeshColliderAndWater();
    }

 

    public void ErodeMap() {
        if(map == null) {
            return;
        }

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

    public void MergeMaps() {

        float[,] merged_map = null;

        float ratio = merge_ratio_slider.value; // how strong new map rewrite old values
        if(ratio > 1 || ratio < 0) {
            Debug.LogWarning($"ratio is out of bound: {ratio}, clamping");
            ratio = Mathf.Clamp(ratio, 0, 1);
        }

        int size = (int)Mathf.Pow(2, size_dropdown.value+5);
        algorithm = (HeightMapAlgorithm)algorithm_dropdown.value;
        float[,] new_map = GenerateMap(size, algorithm);
        
        if(map == null) {
            map = new_map;
            ShowResult();
            return;
        }
        size = map.GetLength(0);
        merged_map = new float[size,size];

        if(map.GetLength(0) == new_map.GetLength(0)) { // thanks god.. 
            for(int i = 0; i < size; i++) {
                for(int j = 0; j < size; j++) {
                    merged_map[i,j] = CalculateMergedValue(ratio, map[i,j], new_map[i,j]);
                }
            }

        }else { // fuck
            Debug.Log($"they have different size: {map.GetLength(0)}:{new_map.GetLength(0)}");
            
            if(map.GetLength(0) < new_map.GetLength(0)) { // just cut unused part of new_map

                int half_diff = new_map.GetLength(0)/2 - map.GetLength(0)/2;
                for(int i = 0; i < size; i++) {
                    for(int j = 0; j < size; j++) {
                        merged_map[i,j] = CalculateMergedValue(ratio, map[i,j], new_map[i+half_diff,j+half_diff]);
                    }
                }
            }else {
                new_map = UpscaleHeightMap(new_map, size);
                for(int i = 0; i < size; i++) {
                    for(int j = 0; j < size; j++) {
                        merged_map[i,j] = CalculateMergedValue(ratio, map[i,j], new_map[i,j]);
                    }
                }
            }
        }

        map = merged_map;
        ShowResult();
    }

    public static float[,] UpscaleHeightMap(float[,] heightMap, int newSize) {
        int oldSize = heightMap.GetLength(0);
        if (newSize <= oldSize) {
            throw new System.ArgumentException("New size must be greater than the old size.");
        }

        float[,] newHeightMap = new float[newSize, newSize];

        // Масштабування коефіцієнтів
        float scale = (float)(oldSize - 1) / (newSize - 1);

        for (int x = 0; x < newSize; x++)
        {
            for (int y = 0; y < newSize; y++)
            {
                // Позиція у старій мапі (з float-координатами)
                float oldX = x * scale;
                float oldY = y * scale;

                // Індекси у старій мапі
                int x0 = Mathf.FloorToInt(oldX);
                int y0 = Mathf.FloorToInt(oldY);
                int x1 = Mathf.Min(x0 + 1, oldSize - 1);
                int y1 = Mathf.Min(y0 + 1, oldSize - 1);

                // Відстань до наступної точки
                float dx = oldX - x0;
                float dy = oldY - y0;

                // Білінійна інтерполяція
                float top = Mathf.Lerp(heightMap[x0, y0], heightMap[x1, y0], dx);
                float bottom = Mathf.Lerp(heightMap[x0, y1], heightMap[x1, y1], dx);
                float value = Mathf.Lerp(top, bottom, dy);

                newHeightMap[x, y] = value;
            }
        }
        return newHeightMap;
    }


    private float CalculateMergedValue(float ratio, float old_value, float new_value) {
        return old_value*(1-ratio) + new_value*ratio;
        // old*0.9 + new*0.1
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

    public void ShowSaveDialog() {
         // Set filters (optional)
		// It is sufficient to set the filters just once (instead of each time before showing the file browser dialog), 
		// if all the dialogs will be using the same filters
		//FileBrowser.SetFilters( true, new FileBrowser.Filter( "Images", ".jpg", ".png" ), new FileBrowser.Filter( "Text Files", ".txt", ".pdf" ) );
		FileBrowser.SetFilters( true, new FileBrowser.Filter( "3D Files", ".fbx") );

		// Set default filter that is selected when the dialog is shown (optional)
		// Returns true if the default filter is set successfully
		// In this case, set Images filter as the default filter
		FileBrowser.SetDefaultFilter( ".fbx" );

		// Set excluded file extensions (optional) (by default, .lnk and .tmp extensions are excluded)
		// Note that when you use this function, .lnk and .tmp extensions will no longer be
		// excluded unless you explicitly add them as parameters to the function
		//FileBrowser.SetExcludedExtensions( ".lnk", ".tmp", ".zip", ".rar", ".exe" );

		// Add a new quick link to the browser (optional) (returns true if quick link is added successfully)
		// It is sufficient to add a quick link just once
		// Name: Users
		// Path: C:\Users
		// Icon: default (folder icon)
		FileBrowser.AddQuickLink( "Users", "C:\\Users", null );
		
		// !!! Uncomment any of the examples below to show the file browser !!!

		// Example 1: Show a save file dialog using callback approach
		// onSuccess event: not registered (which means this dialog is pretty useless)
		// onCancel event: not registered
		// Save file/folder: file, Allow multiple selection: false
		// Initial path: "C:\", Initial filename: "Screenshot.png"
		// Title: "Save As", Submit button text: "Save"
		FileBrowser.ShowSaveDialog( (paths)=> {
            //ExportMeshToFbx(paths[0]);            
            FBXExporter.ExportSingleObject(mesh_transform.gameObject, paths[0]);
        }, null, FileBrowser.PickMode.Files, false, "C:\\", "terrain.fbx", "Save As", "Save" );

		// Example 2: Show a select folder dialog using callback approach
		// onSuccess event: print the selected folder's path
		// onCancel event: print "Canceled"
		// Load file/folder: folder, Allow multiple selection: false
		// Initial path: default (Documents), Initial filename: empty
		// Title: "Select Folder", Submit button text: "Select"
		// FileBrowser.ShowLoadDialog( ( paths ) => { Debug.Log( "Selected: " + paths[0] ); },
		//						   () => { Debug.Log( "Canceled" ); },
		//						   FileBrowser.PickMode.Folders, false, null, null, "Select Folder", "Select" );

		// Example 3: Show a select file dialog using coroutine approach
		// StartCoroutine( ShowLoadDialogCoroutine() );
    }

    public void ExportMeshToFbx(string filePath) {
        //string filePath = Path.Combine(Application.dataPath, "MyTerrain.fbx");
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
    private float[,] GenerateMap(int _size, HeightMapAlgorithm algorithm) {
        float[,] height_map = null;
        Debug.Log(algorithm_dropdown.value);
        
        switch(algorithm) {
            case HeightMapAlgorithm.PerlinNoise:
                height_map = PerlinNoiseGenerator.GenerateHeights(_size, scale_slider.value);
            break;
            case HeightMapAlgorithm.FractalPerlinNoise:
                FractalPerlinNoise.Noise perlin_noise_type = FractalPerlinNoise.Noise.UnityPerlin; // should i give user access to choose?
                Vector2 offset = new Vector2(offset_x_slider.value, offset_y_slider.value);
                height_map = FractalPerlinNoise.GenerateHeights(_size, 
                                                    (int)seed_slider.value, scale_slider.value, 
                                                    (int)octaves_slider.value, persistance_slider.value, lacunarity_slider.value, offset, 
                                                    FractalPerlinNoise.NormalizeMode.Local, perlin_noise_type);
            break;
            case HeightMapAlgorithm.DiamondSquare:
                height_map = DiamondSquareTerrain.GenerateHeights(_size+1, roughness_slider.value, (int)seed_slider.value, false);
            break;
            case HeightMapAlgorithm.Voronoi:
                height_map = VoronoiTerrain.GenerateHeights(_size, (int)point_count_slider.value, (int)seed_slider.value, false);
            break;
            case HeightMapAlgorithm.DLA:
                int initialGridSize = (int)dla_initial_slider.value;
                int stepAmount = (int)dla_steps_slider.value;
                height_map = DLA.RunDLA(initialGridSize, stepAmount);
                //int size = initialGridSize * (int)Mathf.Pow(DLA.UPSCALE_FACTOR, stepAmount); // res_size = initial * scale_factor^steps// just in case lol
                break;
            default:
                Debug.LogWarning("Using undefined algorithm: " + algorithm);
                Debug.Break();
            break;            
        }

        return height_map;
    }
    public void Generate() {
        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        sw.Start();
        
        int _size = (int)Mathf.Pow(2, size_dropdown.value+5);
        algorithm = (HeightMapAlgorithm)algorithm_dropdown.value;
        map = GenerateMap(_size, algorithm);
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
            bool use_flat_shading = false; // read from user
            map_display.DrawMesh(map, max_height_slider.value, map_generator.terrain_data.mesh_height_curve, use_flat_shading);
            UpdateMeshColliderAndWater();
        }else {
            if((DrawMode)draw_mode_dropdown.value == DrawMode.NoiseMap) {
                map_display.DrawNoiseMap(map);
            }else {
                map_display.DrawColorMap(map);
            }
        }
    }

    public void UpdateMeshColliderAndWater() {
        mesh_collider.sharedMesh = null;
        mesh_collider.sharedMesh = mesh_transform.GetComponent<MeshFilter>().sharedMesh;

        float scale = mesh_transform.transform.localScale.x;
        water_transform.localScale = new Vector3(map.GetLength(0)/scale, 1, map.GetLength(1)/scale);
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



    public void LoadInfiniteTerrain() {
        int seed = 42;
        if(int.TryParse(infinite_seed_input.text, out seed)) {
            map_generator.ChangeNoiseSeed(seed);
        }else {
            map_generator.ChangeNoiseSeed(seed);
        }            
        ScenesManager.LoadScene(ScenesManager.Scenes.InfiniteTerrainGeneration);
    }

}
