using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerActionSetter : MonoBehaviour
{
    [SerializeField] private NoiseData noise_data;
    [SerializeField] private PlayerController player;

    private void Start() {
        player.SetActionOnKeyE(LoadHeightMapEditor);
        player.SetActionOnKeyR(ReloadWithRandomSeed);
    }

    private void LoadHeightMapEditor() {
        ScenesManager.LoadScene(ScenesManager.Scenes.HeightMapsGeneration);
    }

    private void ReloadWithRandomSeed() {
        noise_data.seed = Random.Range(int.MinValue, int.MaxValue);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
