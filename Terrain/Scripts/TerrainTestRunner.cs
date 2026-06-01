using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class TerrainTestRunner : MonoBehaviour
{
    [SerializeField] private TerrainModeConfig dagModu;
    [SerializeField] private TerrainModeConfig colModu;
    [SerializeField] private TerrainModeConfig okyansuModu;
    [SerializeField] private float cameraMoveSpeed = 5f;
    [SerializeField] private Camera targetCamera;

    private void Awake()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        if (kb == null)
        {
            return;
        }

        if (kb.digit1Key.wasPressedThisFrame && dagModu != null)
            TerrainManager.Instance?.SwitchMode(dagModu);
        if (kb.digit2Key.wasPressedThisFrame && colModu != null)
            TerrainManager.Instance?.SwitchMode(colModu);
        if (kb.digit3Key.wasPressedThisFrame && okyansuModu != null)
            TerrainManager.Instance?.SwitchMode(okyansuModu);

        if (targetCamera != null && kb.rightArrowKey.isPressed)
        {
            targetCamera.transform.position += Vector3.right * cameraMoveSpeed * Time.deltaTime;
        }
#else
        if (Input.GetKeyDown(KeyCode.Alpha1) && dagModu != null)
            TerrainManager.Instance?.SwitchMode(dagModu);
        if (Input.GetKeyDown(KeyCode.Alpha2) && colModu != null)
            TerrainManager.Instance?.SwitchMode(colModu);
        if (Input.GetKeyDown(KeyCode.Alpha3) && okyansuModu != null)
            TerrainManager.Instance?.SwitchMode(okyansuModu);

        if (targetCamera != null && Input.GetKey(KeyCode.RightArrow))
        {
            targetCamera.transform.position += Vector3.right * cameraMoveSpeed * Time.deltaTime;
        }
#endif
    }
}
