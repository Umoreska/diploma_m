
using UnityEngine;
using TMPro;
using System.Diagnostics;


public class InfiniteTerrainStatistic : MonoBehaviour
{
    [SerializeField] private MapGenerator map_generator;
    [SerializeField] private TMP_Text seed_text, fps_text, performance_text, chunks_count_text, active_chunks_count;
    [SerializeField] private InfiniteTerrainGeneration infinite_terrain;

    private float updateInterval = 1.0f; // Оновлення даних раз на секунду
    private float timeSinceLastUpdate = 0f;

    private Process currentProcess;
    
    private void Start() {
        seed_text.text = $"Seed: {map_generator.GetNoiseSeed()}";

        currentProcess = Process.GetCurrentProcess();
    }
    void Update() {
        timeSinceLastUpdate += Time.deltaTime;

        if (timeSinceLastUpdate >= updateInterval)
        {
            UpdatePerformanceData();
            timeSinceLastUpdate = 0f;
        }
    }


    private void UpdatePerformanceData() {
        // Використання пам'яті (RAM) у мегабайтах
        long memoryUsage = currentProcess.WorkingSet64 / (1024 * 1024);

        // Завантаження CPU у відсотках
        double cpuUsage = GetCPUUsage();
        infinite_terrain.GetInfoAboutChunks(out int chunk_count, out int active_chunk_count);

        
        fps_text.text = $"FPS: {1/Time.deltaTime}";
        performance_text.text = $"CPU Usage: {cpuUsage:F2}%";
        chunks_count_text.text = $"Chunk Count: {chunk_count}";
        active_chunks_count.text = $"Active Chunks: {active_chunk_count}";
    }

    private double GetCPUUsage() {
        // Отримання завантаження CPU (базова оцінка)
        return currentProcess.TotalProcessorTime.TotalMilliseconds / ((Time.timeSinceLevelLoad * System.Environment.ProcessorCount) * 100);
    }

}
