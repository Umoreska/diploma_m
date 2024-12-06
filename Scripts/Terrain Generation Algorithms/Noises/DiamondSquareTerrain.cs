using UnityEngine;

public class DiamondSquareTerrain
{
    public static float[,] GenerateHeights(int _size, float _roughness, int seed, bool random = true) {
        if(IsPowerOfTwo(_size-1) == false) {
            Debug.LogWarning($"_size-1 is not power of 2: {_size-1}");    
            return null;
        }

        float[,] heights = new float[_size, _size];
        int edge = _size - 1;

        if(random == false) {
            Random.InitState(seed);
        }

        // Ініціалізуємо кути
        heights[0, 0] = Random.Range(0f, 1f);
        heights[0, edge] = Random.Range(0f, 1f);
        heights[edge, 0] = Random.Range(0f, 1f);
        heights[edge, edge] = Random.Range(0f, 1f);

        int stepSize = edge;
        float scale = _roughness;

        while (stepSize > 1)
        {
            // Diamond step
            for (int x = 0; x < edge; x += stepSize)
            {
                for (int y = 0; y < edge; y += stepSize)
                {
                    float avg = (heights[x, y] +
                                heights[x + stepSize, y] +
                                heights[x, y + stepSize] +
                                heights[x + stepSize, y + stepSize]) * 0.25f;
                    heights[x + stepSize / 2, y + stepSize / 2] = avg + Random.Range(-scale, scale);
                }
            }

            // Square step
            for (int x = 0; x <= edge; x += stepSize / 2)
            {
                for (int y = (x + stepSize / 2) % stepSize; y <= edge; y += stepSize)
                {
                    float avg = (heights[(x - stepSize / 2 + _size) % _size, y] +
                                heights[(x + stepSize / 2) % _size, y] +
                                heights[x, (y + stepSize / 2) % _size] +
                                heights[x, (y - stepSize / 2 + _size) % _size]) * 0.25f;
                    heights[x, y] = avg + Random.Range(-scale, scale);

                    // Handle borders
                    if (x == 0) heights[edge, y] = heights[x, y];
                    if (y == 0) heights[x, edge] = heights[x, y];
                }
            }

            stepSize /= 2;
            scale /= 2f;
        }

        //SmoothHeights(heights);
        NormalizeHeights(heights);

        return heights;
    }

    public static bool IsPowerOfTwo(int number) {
        if (number <= 0) {
            return false;
        }
        return (number & (number - 1)) == 0;
    }

    private static void SmoothHeights(float[,] heights) {

        int width = heights.GetLength(0);
        int height = heights.GetLength(1);

        for (int x = 1; x < width - 1; x++) {
            for (int y = 1; y < height - 1; y++) {

                float sum = 0f;
                int count = 0;

                for (int i = -1; i <= 1; i++) {
                    for (int j = -1; j <= 1; j++) {
                        sum += heights[x + i, y + j];
                        count++;
                    }
                }

                heights[x, y] = sum / count;
            }
        }
    }

    public static void NormalizeHeights(float[,] heights) {
        int width = heights.GetLength(0);
        int height = heights.GetLength(1);
        float min = float.MaxValue;
        float max = float.MinValue;

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                if (heights[x, y] < min) {
                    min = heights[x, y];
                }

                if (heights[x, y] > max) {
                    max = heights[x, y];
                }
            }
        }

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                heights[x, y] = Mathf.InverseLerp(min, max, heights[x,y]);
            }
        }
    }



    
}
