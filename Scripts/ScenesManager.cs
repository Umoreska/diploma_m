using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenesManager : MonoBehaviour
{
    public enum Scenes{
        HeightMapsGeneration, InfiniteTerrainGeneration
    }

    private static int InfiniteTerrainSeed = 0;
    
    public static void LoadScene(Scenes scene) {
        SceneManager.LoadScene((int)scene);    
    }

    public int GetSeed() => InfiniteTerrainSeed;
    public void SetSeed(int seed) => InfiniteTerrainSeed = seed;
}
