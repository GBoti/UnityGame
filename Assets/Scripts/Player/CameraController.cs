using UnityEngine;
using UnityEngine.EventSystems;
using DishevelledBadger.FlashFrostVale.Networking;

namespace DishevelledBadger.FlashFrostVale.Player // Player-specific components
{
    // Manages camera movement, rotation, zoom, and following for the player.
    public class CameraController : MonoBehaviour
    {
        public static CameraController instance; // Singleton instance for easy access (local player's camera).
        public Transform followTransform;       // If set, camera will follow this transform.
        public Transform cameraTransform;       // The actual child Camera object's transform.

        // Movement, speed, and smoothing parameters.
        public float normalSpeed;    // Default movement speed.
        public float fastSpeed;      // Movement speed when holding shift.
        public float movementSpeed;  // Current movement speed.
        public float movementTime;   // Time for smooth position/rotation interpolation.
        public float rotationAmount; // Degrees to rotate per key press.
        public float fovZoomAmount;  // Amount FOV changes per mouse scroll tick.

        // Target state for smooth camera transitions.
        public Vector3 newPosition;         // Target position for the camera rig.
        public Quaternion newRotation;      // Target rotation for the camera rig.
        public float fovNewZoom;            // Target Field of View for the camera.

        // Variables for mouse drag controls.
        public Vector3 dragStartPosition;     // World position where mouse drag started.
        public Vector3 dragCurrentPosition;   // Current world position during mouse drag.
        public Vector3 rotateStartPosition;   // Screen position where mouse rotation started.
        public Vector3 rotateCurrentPosition; // Current screen position during mouse rotation.

        void Start()
        {
            // Set static instance only if this CameraController belongs to the local player.
            NetworkGamePlayerFFV networkPlayer = GetComponentInParent<NetworkGamePlayerFFV>();
            if (networkPlayer != null && networkPlayer.isLocalPlayer)
            {
                instance = this; // Assign local player's camera controller to static instance.
            }

            // Initialize target state with current camera rig state.
            newPosition = transform.position;
            newRotation = transform.rotation;
            if (cameraTransform != null && cameraTransform.gameObject.GetComponent<Camera>() != null)
            {
                fovNewZoom = cameraTransform.gameObject.GetComponent<Camera>().fieldOfView;
            }
        }

        void Update()
        {
            // If following a target, lock position to it.
            if (followTransform != null)
            {
                transform.position = followTransform.position;
            }
            else // Otherwise, allow free camera control.
            {
                HandleMouseInput();    // Process mouse inputs for zoom, pan, rotate.
                HandleMovementInput(); // Process keyboard inputs for movement and apply smoothing.
            }

            // Press Escape to stop following a target.
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                followTransform = null;
            }
        }

        // Handles mouse input for zoom, panning (middle mouse), and rotation (right mouse).
        void HandleMouseInput()
        {
            // Mouse Scroll Wheel: Zoom (FOV change).
            if (Input.mouseScrollDelta.y != 0 && !EventSystem.current.IsPointerOverGameObject()) // Ignore scroll over UI.
            {
                fovNewZoom -= Input.mouseScrollDelta.y * fovZoomAmount;
                fovNewZoom = Mathf.Clamp(fovNewZoom, 2f, 60f); // Clamp FOV to min/max values.
            }

            // Middle Mouse Button: Drag to Pan.
            if (Input.GetMouseButtonDown(2)) // Middle mouse button down: record drag start position.
            {
                Plane plane = new Plane(Vector3.up, Vector3.zero); // Define a plane at y=0.
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (plane.Raycast(ray, out float entry)) dragStartPosition = ray.GetPoint(entry);
            }
            if (Input.GetMouseButton(2)) // Middle mouse button held: calculate pan.
            {
                Plane plane = new Plane(Vector3.up, Vector3.zero);
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (plane.Raycast(ray, out float entry))
                {
                    dragCurrentPosition = ray.GetPoint(entry);
                    newPosition = transform.position + dragStartPosition - dragCurrentPosition; // Update target position.
                }
            }

            // Right Mouse Button: Drag to Rotate.
            if (Input.GetMouseButtonDown(1)) // Right mouse button down: record rotation start position.
            {
                rotateStartPosition = Input.mousePosition;
            }
            if (Input.GetMouseButton(1)) // Right mouse button held: calculate rotation.
            {
                rotateCurrentPosition = Input.mousePosition;
                Vector3 difference = rotateStartPosition - rotateCurrentPosition;
                rotateStartPosition = rotateCurrentPosition; // Update start for next frame's difference.
                newRotation *= Quaternion.Euler(Vector3.up * (-difference.x / 5f)); // Apply rotation around Y-axis.
            }
        }

        // Handles keyboard input for movement (WASD/Arrows, Q/E for rotation) and applies smoothed transforms.
        void HandleMovementInput()
        {
            // Speed modifier (Shift key).
            movementSpeed = Input.GetKey(KeyCode.LeftShift) ? fastSpeed : normalSpeed;

            // Keyboard pan controls (WASD / Arrow keys).
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) newPosition += (transform.forward * movementSpeed);
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) newPosition += (transform.forward * -movementSpeed);
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) newPosition += (transform.right * -movementSpeed);
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) newPosition += (transform.right * movementSpeed);

            if (Input.GetKey(KeyCode.P)) Debug.Log("Camera is at: " + newPosition + "\n"); // Debug position.

            // Clamp camera position to map boundaries if MapManager instance exists.
            if (DishevelledBadger.FlashFrostVale.Networking.MapManager.Instance != null)
            {
                var mm = DishevelledBadger.FlashFrostVale.Networking.MapManager.Instance;
                newPosition.x = Mathf.Clamp(newPosition.x, mm.syncedMapLeftEdge, mm.syncedMapRightEdge);
                newPosition.z = Mathf.Clamp(newPosition.z, mm.syncedMapBottomEdge, mm.syncedMapTopEdge);
            }

            // Keyboard rotation controls (Q / E).
            if (Input.GetKey(KeyCode.Q)) newRotation *= Quaternion.Euler(Vector3.up * rotationAmount);
            if (Input.GetKey(KeyCode.E)) newRotation *= Quaternion.Euler(Vector3.up * -rotationAmount);

            // Smoothly interpolate camera rig's position and rotation to target values.
            transform.position = Vector3.Lerp(transform.position, newPosition, Time.deltaTime * movementTime);
            transform.rotation = Quaternion.Lerp(transform.rotation, newRotation, Time.deltaTime * movementTime);

            // Apply FOV zoom to the actual camera object.
            if (cameraTransform != null && cameraTransform.gameObject.GetComponent<Camera>() != null)
            {
                cameraTransform.gameObject.GetComponent<Camera>().fieldOfView = fovNewZoom;
            }
        }
    }
}