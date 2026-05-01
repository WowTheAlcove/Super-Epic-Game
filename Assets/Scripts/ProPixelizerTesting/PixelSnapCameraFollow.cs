using ProPixelizer;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Camera))]
public class PixelSnapCameraFollow : MonoBehaviour
{
    [Tooltip("The boat's ObjectRenderSnapable component.")]
    public ObjectRenderSnapable TargetSnapable;

    private Vector3 _lastSnapOffset;

    private void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
    }

    private void LateUpdate()
    {
        // Re-apply the last known snap offset so that when Snap() saves
        // PreSnapCameraPosition next frame, it includes the offset
        transform.position += _lastSnapOffset;
    }

    private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (camera != GetComponent<Camera>())
            return;
        if (TargetSnapable == null)
            return;

        _lastSnapOffset = TargetSnapable.transform.position - TargetSnapable.WorldPositionPreSnap;
        Debug.Log(_lastSnapOffset);
    }
}