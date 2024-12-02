using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenesManager : MonoBehaviour
{
    public enum Scenes{
        HeightMapsGeneration, InfiniteTerrainGeneration
    }
    
    public static void LoadScene(Scenes scene) {
        SceneManager.LoadScene((int)scene);    
    }
}
