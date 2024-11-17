using System.Collections;
using Unity.Mathematics;
using UnityEngine;

public class PerlinNoiseGenerator
{
    
    public static  float[,] GenerateHeights(int _size, float _scale) {

        float[,] heights = new float[_size, _size];
        
        for (int x = 0; x < _size; x++) {
            for (int y = 0; y < _size; y++) {
                heights[x, y] = CalculateHeight(x, y, _scale, _size);
            }
        }
        return heights;
    }
    public static float CalculateHeight(int x, int y, float _scale, int _size) {
        float half_size = _size / 2;
        float xCoord = (x+half_size) / _scale;
        float yCoord = (y+half_size) / _scale;
        //float yCoord = (float)y / _size / _scale;

        return Mathf.PerlinNoise(xCoord, yCoord);
    }


    
}
