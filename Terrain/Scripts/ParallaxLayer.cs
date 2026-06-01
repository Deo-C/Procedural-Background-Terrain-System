using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    [SerializeField] private float speedRatio = 0.5f;
    [SerializeField] private SpriteRenderer layerRenderer;

    private float spriteWidth;

    private void Awake()
    {
        CacheWidth();
    }

    private void CacheWidth()
    {
        if (layerRenderer != null)
        {
            spriteWidth = layerRenderer.bounds.size.x;
        }
    }

    public void SetSpeed(float ratio)
    {
        speedRatio = Mathf.Clamp01(ratio);
    }

    public void UpdatePosition(float cameraDeltaX)
    {
        UpdatePosition(cameraDeltaX, float.NaN);
    }

    public void UpdatePosition(float cameraDeltaX, float cameraLeftEdge)
    {
        transform.position -= new Vector3(cameraDeltaX * speedRatio, 0f, 0f);

        if (layerRenderer == null || float.IsNaN(cameraLeftEdge) || spriteWidth <= 0f)
        {
            return;
        }

        float rightEdge = layerRenderer.bounds.max.x;
        if (rightEdge < cameraLeftEdge)
        {
            float shiftCount = Mathf.Ceil((cameraLeftEdge - rightEdge) / spriteWidth) + 1f;
            transform.position += new Vector3(spriteWidth * shiftCount, 0f, 0f);
        }
    }
}
