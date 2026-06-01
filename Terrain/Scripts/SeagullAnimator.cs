using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SeagullAnimator : MonoBehaviour
{
    public float animSpeed = 1.2f;
    public float timeOffset;
    private MeshFilter meshFilter;
    private OceanDetailConfig config;
    private Mesh workingMesh;

    public void Initialize(OceanDetailConfig cfg)
    {
        config = cfg;
        if (meshFilter == null)
        {
            meshFilter = GetComponent<MeshFilter>();
        }
        if (meshFilter != null)
        {
            workingMesh = meshFilter.sharedMesh ?? new Mesh();
            meshFilter.sharedMesh = workingMesh;
            OceanDetailGenerator.UpdateSeagullMesh(workingMesh, config, 0f);
        }
    }

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
    }

    private void Start()
    {
        if (Mathf.Approximately(timeOffset, 0f))
        {
            timeOffset = Random.Range(0f, Mathf.PI * 2f);
        }
    }

    private void Update()
    {
        if (workingMesh == null || config == null)
        {
            return;
        }

        float angle = Mathf.Lerp(-10f, 25f, (Mathf.Sin(Time.time * animSpeed + timeOffset) + 1f) * 0.5f);
        OceanDetailGenerator.UpdateSeagullMesh(workingMesh, config, angle);
    }
}
