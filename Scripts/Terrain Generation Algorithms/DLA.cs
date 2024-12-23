using UnityEngine;
using System.Collections.Generic;

public class DLA : MonoBehaviour{
    [SerializeField] private MapDisplay map_display;
    static GameObject cubes_parent = null; 
    public const int UPSCALE_FACTOR = 2;    // Фактор збільшення розміру сітки
    static int step = 1; // how often this dla was upscaled already
    static readonly Vector2Int[] offsets = { new Vector2Int(0,1), new Vector2Int(1,0), new Vector2Int(0,-1), new Vector2Int(-1,0) };
    static List<Pixel> pixels;
    static public Pixel mainPixel;
    static Pixel[,] grid;
    static float[] image;

    public static float[,] RunDLA(int start_size, int upscale_count) {
        //Random.InitState(42);
        step = 1;

        // parent for cubes that are used for pixels visualisation
        if(cubes_parent == null) {
            cubes_parent = GameObject.Find("cubes_parent");
            if(cubes_parent == null) {
                cubes_parent = new GameObject();
            }
        }else {
            foreach(Transform child in cubes_parent.transform) {
                GameObject.Destroy(child.gameObject);
            }
        }

        // Початкова генерація
        grid = new Pixel[start_size, start_size];
        image = new float[start_size*start_size];
        pixels = new List<Pixel>();
        
        // Початковий піксель в центрі
        Vector2Int start_pos = new Vector2Int(start_size / 2, start_size / 2);

        mainPixel = new Pixel(start_pos, null, Pixel.PixelType.MAIN);
        grid[start_pos.x, start_pos.y] = mainPixel;
        pixels.Add(mainPixel);
        


        System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();        
        sw.Start();

        AddPixels(PixelsPerStep(grid.GetLength(0), 0));
        Debug.Log($"step: {0}. added: {PixelsPerStep(grid.GetLength(0), 0)}. spent: {sw.ElapsedMilliseconds} ms"); 
        UpscaleTexture();

        sw.Stop();

        //PrintPixelsOnScene(0);

        // Генерація пікселів і зв'язків на кожному етапі
        for (int step = 0; step < upscale_count; step++) {
            sw.Restart();

            UpscaleGrid();
            AddPixels(PixelsPerStep(grid.GetLength(0), step+1));
            UpscaleTexture();

            
            sw.Stop();
            Debug.Log($"step: {step+1}. added: {PixelsPerStep(grid.GetLength(0), step+1)}. spent: {sw.ElapsedMilliseconds} ms");
        }

        //PrintPixelsOnScene(0);

        int n = image.Length;
        int size = (int)Mathf.Sqrt(n);
        if (size * size != n) {
            Debug.LogWarning($"(int)sqrt(image.Length)^2 != image.Length. in other words, image is not a square");
            Debug.Break();
            return new float[0,0]; // wtf is that
        }
        //image = MyBlur(image, size);
        image = GaussBlur(image, size);

        // 1d -> 2d
        float[,] matrix = new float[size, size];
        for (int i = 0; i < size; i++) {
            for (int j = 0; j < size; j++) {
                matrix[i, j] = image[i * size + j];
            }
        }

        // finding min & max
        float min = int.MaxValue;
        float max = int.MinValue;
        for(int i = 0; i < matrix.GetLength(0); i++) {
            for(int j = 0; j < matrix.GetLength(1); j++) {
                if(matrix[i,j] > max) {
                    max = matrix[i,j];
                }
                if(matrix[i,j] < min) {
                    min = matrix[i,j];
                }
            }
        }

        // normilizing
        for(int i = 0; i < matrix.GetLength(0); i++) {
            for(int j = 0; j < matrix.GetLength(1); j++) {
                matrix[i,j] = Mathf.InverseLerp(min, max, matrix[i,j]);
            }
        }


        return matrix;
        
        /* Example of a generation:
        DLA dla(12);
        dla.AddPixels(12);
        dla.UpscaleTexture();
        
        dla.Upscale(); // 24
        dla.AddPixels(24);
        dla.UpscaleTexture();
        
        dla.Upscale(); // 48
        dla.AddPixels(9 * 24); // 216
        dla.UpscaleTexture();

        dla.Upscale(); // 96
        dla.AddPixels(9 * 9 * 24); // 1944
        dla.UpscaleTexture();

        dla.Upscale(); // 192
        dla.AddPixels(3 * 9 * 9 * 24); // 5832
        dla.UpscaleTexture();
        
        dla.GenTexture();

        Note that the steps UpscaleTexture(); and Upscale(); have to be used consecutively.
        */
    }
    public static int PixelsPerStep(int size, int step) {
        //return (int)(12 * Mathf.Pow(2f, step % 2) * Mathf.Pow(9f, Mathf.Floor(step / 2.0f)));
        return size * (step == 0 ? 1 : step);
    }


    static void AddPixels(int amount=1, bool on_edge=false) {
        if(amount < 1) {
            return;
        }
        Vector2Int randomPos;
        int width = grid.GetLength(0);
        int height = grid.GetLength(1);

        for(int i = 0; i < amount; i++) {
            if(on_edge) {
                 // Randomly choose which edge to pick from: top, bottom, left, or right
                int edge = Random.Range(0, 4);
                randomPos = edge switch
                {
                    // Top edge
                    0 => new Vector2Int(Random.Range(0, width), 0),
                    // Bottom edge
                    1 => new Vector2Int(Random.Range(0, width), height - 1),
                    // Left edge
                    2 => new Vector2Int(0, Random.Range(0, height)),
                    // Right edge
                    3 => new Vector2Int(width - 1, Random.Range(0, height)),
                    _ => Vector2Int.zero,// Fallback
                };
            }
            else {
                randomPos = new Vector2Int(Random.Range(0, width), Random.Range(0, height));
                while(grid[randomPos.x, randomPos.y] != null) { // change position untill grid element is null 
                    randomPos = new Vector2Int(Random.Range(0, width), Random.Range(0, height));
                }
            }
            MovePixelToConnection(randomPos);
        }
    }



    static void MovePixelToConnection(Vector2Int pos)
    {
        int size = grid.GetLength(0);
        while (grid[pos.x, pos.y] == null) 
        {
            foreach(var offset in offsets) {
                if(pos.x + offset.x > 0 && pos.x + offset.x < size-1 && pos.y + offset.y > 0 && pos.y + offset.y < size-1) {
                    if(grid[pos.x + offset.x, pos.y + offset.y] != null)
                    {
                        //pixels.emplace_back(new Pixel());
                        //pixels.back()->position = pos;
                        //pixels.back()->parent = grid[pos.x + offset.x][pos.y + offset.y];
                        //grid[pos.x + offset.x][pos.y + offset.y]->children.emplace_back(pixels.back());
                        Pixel parent = grid[pos.x + offset.x, pos.y + offset.y];
                        Pixel new_pixel = new Pixel(pos, parent, Pixel.PixelType.New);
                        pixels.Add(new_pixel);

                        //grid[pos.x][pos.y] = pixels.back();
                        grid[pos.x, pos.y] = new_pixel;
                        break;
                    }
                }
            }

            if(grid[pos.x, pos.y] == null) {
                pos += offsets[Random.Range(0, 4)];
            }

            // Correct possible overshoots over the grid.
            pos.x = Mathf.Clamp(pos.x, 0, size-1);
            pos.y = Mathf.Clamp(pos.y, 0, size-1);

        }
    }

    static void UpscaleGrid() {
        int oldSize = grid.GetLength(0);
        int newSize = oldSize * UPSCALE_FACTOR;
        grid = new Pixel[newSize, newSize];

        CreateConnections(mainPixel);

        foreach(var pixel in pixels) {
            grid[pixel.position.x, pixel.position.y] = pixel;
        }
    }

    static private void CreateConnections(Pixel pixel) {

        Pixel[] children = pixel.children.ToArray();
        pixel.children.Clear();

        foreach(Pixel child in children) {
            Vector2Int connecter_pos = pixel.position * UPSCALE_FACTOR + (child.position - pixel.position);
            Pixel connecter = new Pixel(connecter_pos, pixel, Pixel.PixelType.Mid);
            pixels.Add(connecter);
            // jiggle time !!!
            Vector2Int ortho = child.position - pixel.position;
            if(ortho.x > 1 ) {
                ortho.x = 1;
            }
            if(ortho.y > 1) {
                ortho.y = 1;
            }
            ortho = new Vector2Int(ortho.y, ortho.x); // swap x and y

            if(ortho.x != 0)
                ortho.x = ortho.x / Mathf.Abs(ortho.x);
            if(ortho.y != 0)
                ortho.y = ortho.y / Mathf.Abs(ortho.y);

            int ran = Random.Range(0, 11);
            if(ran >= 9) connecter.position += ortho;
            else if(ran >= 7) connecter.position -= ortho;
            // finish up linking
            //connecter.parent = pixel;
            child.parent = connecter;
            //pixel.children.Add(connector)
            connecter.children.Add(child);            

            CreateConnections(child);
        }
        pixel.type = Pixel.PixelType.Old;
        pixel.position *= UPSCALE_FACTOR;
    }

    private static float[] GaussBlur(float[] image, int image_size) {
        List<float> new_image_list = new List<float>();
        for(int x = 0; x < image_size; x++) {
            for(int y = 0; y < image_size; y++) {

                int mx = x * image_size;
                int bx = x > 0 ? (x-1) * image_size : mx;
                int ax = x < image_size-1 ? (x+1) * image_size : mx;

                int my = y;
                int by = y > 0 ? y-1 : my;
                int ay = y < image_size-1 ? y+1 : my;
                
                //     min                      middle                         max
                float bxby = image[bx + by]; float bxmy = image[bx + my]; float bxay = image[bx + ay];
                float mxby = image[mx + by]; float mxmy = image[mx + my]; float mxay = image[mx + ay];
                float axby = image[ax + by]; float axmy = image[ax + my]; float axay = image[ax + ay];             

                float minWeight = 4f * (Random.Range(1, 11) / 10f);
                float midWeight = 8f * (Random.Range(1, 11) / 10f);
                float maxWeight = 16f * (Random.Range(1, 11) / 10f);
                
                float v = 1f / (4f*minWeight + 4f*midWeight + maxWeight) * (
                        minWeight*bxby + midWeight*bxmy + minWeight*bxay +
                        midWeight*mxby + maxWeight*mxmy + midWeight*mxay +
                        minWeight*axby + midWeight*axmy + minWeight*axay
                );

                new_image_list.Add(v);
            }
        }
        return new_image_list.ToArray();
    }

    static void UpscaleTexture() {

        CalculateValues();

        int image_size = grid.GetLength(0);

        // place current grid on the image
        for(int x=0; x<image_size; x++) {
            for(int y=0; y<image_size; y++) {
            
                float v = grid[x,y] != null ? 1f - 1f / (1f + 0.5f * grid[x,y].value) : 0f;

                int index = x * image_size + y;
                float fv = 1f - 1f / (1f + (1f / step) * v + 1.25f * image[index]);

                image[index + 0] = fv; // why +0 tho??

            }
        }
        ++step;

        // upscale the current image
        List<float> new_image_list = new List<float>();
        float multiplier = 1f / UPSCALE_FACTOR;
        
        for(int x = 0; x < image_size * UPSCALE_FACTOR; x++) {
            for(int y = 0; y < image_size * UPSCALE_FACTOR; y++) {

                int mx = (int)Mathf.Floor(x * multiplier) * image_size; // mx - middle pixel.x
                int bx = x > 0 ? (int)Mathf.Floor((x - 1) * multiplier) * image_size : mx; // bx - pixel.x on the left from middle.x one

                int my = (int)Mathf.Floor(y * multiplier); // same but with y coordinate
                int by = y > 0 ? (int)Mathf.Floor((y - 1) * multiplier) : my;
                
                //Debug.Log($"image length: {image.Length}; mx: {mx}; bx: {bx}; my: {my}; by: {by};\nbx+by: {bx+by}; bx+my: {bx+my}; mx+by: {mx+by}; mx+my: {mx+my}");
                float v = 0.25f * image[bx + by] + 0.25f * image[bx + my] + 0.25f * image[mx + by] + 0.25f * image[mx + my];

                new_image_list.Add(v);
            }
        }

        image = new_image_list.ToArray();
        new_image_list.Clear();

        image_size *= UPSCALE_FACTOR;
        // bluring image using a convolution aproximation of gaussian blur
        //image = MyBlur(image, image_size);
        image = GaussBlur(image, image_size);
    }


    static private void CalculateValues() {
        foreach(var pixel in pixels) {
            if(pixel.children.Count == 0) {
                int value = 1;
                pixel.type = Pixel.PixelType.Last;
                pixel.value = value;
                Pixel parent = pixel.parent;
                while(parent != null) {
                    value++;
                    parent.value = value;
                    parent = parent.parent;
                }
            }
        }
    }
    public class Pixel{  
        public enum PixelType{
            None ,New, Mid, Old, Last, MAIN
        }   
        public PixelType type; // for visualisation   
        public Vector2Int position;
        public Pixel parent; // this pixel is connected to parent
        public List<Pixel> children; // pixels connected to this one
        public float value;
        public Pixel(Vector2Int position, Pixel parent, PixelType type) {
            this.position = position;
            this.type = type;

            this.parent = parent;

            children = new List<Pixel>();
            parent?.children.Add(this);
            
        }

    }

}

