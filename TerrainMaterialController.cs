using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class TerrainMaterialController : MonoBehaviour
{
    [SerializeField] private Material terrain_material;
    [SerializeField] private Slider snow_bottom, snow_edge, mountain_slope_power, rock_bottom, rock_edge, sand_top, sand_edge, ground_slope_power;

    private void Start() {
        can_change=false;
        // set initial
        snow_bottom.value = terrain_material.GetFloat("_snow_bottom");
        snow_edge.value = terrain_material.GetFloat("_snow_edge");
        mountain_slope_power.value = terrain_material.GetFloat("_mountain_slope_power");
        rock_bottom.value = terrain_material.GetFloat("_rock_bottom");
        
        rock_edge.value = terrain_material.GetFloat("_rock_edge");
        sand_top.value = terrain_material.GetFloat("_sand_top");
        sand_edge.value = terrain_material.GetFloat("_sand_edge");
        ground_slope_power.value = terrain_material.GetFloat("_ground_slope_power");

        StartCoroutine(Delay(0.25f, ()=> can_change=true));
    }

    
    private IEnumerator Delay(float time, System.Action action) {
        yield return new WaitForSeconds(time);
        action?.Invoke();
    }

    private bool can_change;
    public void UpdateShader() {
        if(can_change == false) return; // initializing calls this and fucks everything up

        terrain_material.SetFloat("_snow_bottom", snow_bottom.value);
        terrain_material.SetFloat("_snow_edge", snow_edge.value);
        terrain_material.SetFloat("_mountain_slope_power", mountain_slope_power.value);
        terrain_material.SetFloat("_rock_bottom", rock_bottom.value);

        terrain_material.SetFloat("_rock_edge", rock_edge.value);
        terrain_material.SetFloat("_sand_top", sand_top.value);
        terrain_material.SetFloat("_sand_edge", sand_edge.value);
        terrain_material.SetFloat("_ground_slope_power", ground_slope_power.value);
    }
}
