using System.Collections.Generic;
using UnityEngine;

public class ParallaxController : MonoBehaviour
{
    [SerializeField] private List<ParallaxLayer> layers = new List<ParallaxLayer>();
    [SerializeField] private Camera cam;

    private float lastCamX;

    private void Awake()
    {
        if (cam == null)
        {
            cam = Camera.main;
        }
        if (cam != null)
        {
            lastCamX = cam.transform.position.x;
        }
    }

    public void Initialize(TerrainModeConfig config)
    {
        if (cam == null)
        {
            cam = Camera.main;
        }

        if (config != null && config.parallaxSpeedRatios != null)
        {
            for (int i = 0; i < layers.Count; i++)
            {
                float ratio = i < config.parallaxSpeedRatios.Length ? config.parallaxSpeedRatios[i] : layers[i] != null ? 0.5f : 0f;
                if (layers[i] != null)
                {
                    layers[i].SetSpeed(ratio);
                }
            }
        }

        if (cam != null)
        {
            lastCamX = cam.transform.position.x;
        }
    }

    private void LateUpdate()
    {
        if (cam == null)
        {
            return;
        }

        float camX = cam.transform.position.x;
        float delta = camX - lastCamX;
        if (Mathf.Approximately(delta, 0f))
        {
            return;
        }

        float cameraLeftEdge = cam.transform.position.x - cam.orthographicSize * cam.aspect;
        for (int i = 0; i < layers.Count; i++)
        {
            if (layers[i] != null)
            {
                layers[i].UpdatePosition(delta, cameraLeftEdge);
            }
        }

        lastCamX = camX;
    }
}
