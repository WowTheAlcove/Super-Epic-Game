// PixelSnapCameraTarget.cs
// Attach this to a new empty GameObject (NOT the camera itself).
// Point your Cinemachine virtual camera's Follow/LookAt target at this GameObject.
// Set 'Target' to your player's root transform.

using UnityEngine;

namespace ProPixelizer
{
    [AddComponentMenu("ProPixelizer/PixelSnapCameraTarget")]
    public class PixelSnapCameraTarget : MonoBehaviour
    {
        [Tooltip("The transform to follow (e.g. your player root).")]
        public Transform Target;

        [Tooltip("The Camera used to determine snapping axes. Assign your main camera.")]
        public Camera Camera;

        [Tooltip("Reference to the ProPixelizerCamera on your camera, used to read world-space pixel size.")]
        public ProPixelizerCamera ProPixelizerCamera;

        private void LateUpdate()
        {
            if (Target == null || Camera == null || ProPixelizerCamera == null)
                return;

            float pixelSize = ProPixelizerCamera.LowResPixelSizeWS;

            if (pixelSize <= float.Epsilon)
            {
                transform.position = Target.position;
                return;
            }

            // Build the matrix using only the camera's ROTATION and a fixed origin
            // of Vector3.zero. The camera's actual position is never used, so the
            // camera chasing this proxy can never feed back into the calculation.
            // We only need the orientation to define which axes to snap along.
            Matrix4x4 camToWorld = Matrix4x4.TRS(Vector3.zero, Camera.transform.rotation, Vector3.one);
            Matrix4x4 worldToCam = camToWorld.inverse;

            // Convert target's world position into rotation-only camera space.
            Vector3 camSpacePos = worldToCam.MultiplyPoint3x4(Target.position);

            // Snap camera-space X and Y (the axes the camera can see).
            // Leave Z (depth) unsnapped — snapping it causes diagonal jitter
            // in isometric views where depth projects onto the screen.
            camSpacePos.x = Mathf.Round(camSpacePos.x / pixelSize) * pixelSize;
            camSpacePos.y = Mathf.Round(camSpacePos.y / pixelSize) * pixelSize;

            // Convert back to world space and assign to the proxy.
            transform.position = camToWorld.MultiplyPoint3x4(camSpacePos);
        }
    }
}