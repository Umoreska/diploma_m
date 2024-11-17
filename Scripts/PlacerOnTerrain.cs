//using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;

public class PlacerOnTerrain
{  

    public static GameObject[] PlaceOnTerrain(float[,] height_map, int count, float min_threshold_height, float max_threshold_height, int seed, GameObject[] prefabs) {
        
        List<GameObject> placedObjects = new List<GameObject>();

        int mapWidth = height_map.GetLength(0);
        int mapHeight = height_map.GetLength(1);
        
        Random.InitState(seed);


        List<Vector2Int> placeble_places = new List<Vector2Int>();
        for(int i = 0; i < mapWidth; i++) {
            for(int j = 0; j < mapHeight; j++) {
                if(height_map[i,j] >= min_threshold_height && height_map[i,j] <= max_threshold_height){
                    placeble_places.Add(new Vector2Int(i, j));
                }
            }
        }
        
        int placeble_places_count = placeble_places.Count;
        for (int i = 0; i < count; i++) {

            Vector2Int place = placeble_places[Random.Range(0, placeble_places_count)];
            float x = place.x;
            float z = place.y;
            float height = height_map[(int)x, (int)z];

            //x += Random.Range(-1f, 1f); // DONT!
            //z += Random.Range(-1f, 1f);
            Vector3 position = new Vector3(x, height, z);
            Debug.Log($"placing tree on {position}");

            GameObject instance = GameObject.Instantiate(prefabs[Random.Range(0, prefabs.Length-1)], position, Quaternion.identity); // instancing random prefab

            // Randomize it !!!


            // Add the instance to the list
            placedObjects.Add(instance);
        }

        return placedObjects.ToArray();    
    }
}