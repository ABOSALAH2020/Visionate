using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ShapeAttributeController : MonoBehaviour
{
    public RayMarchCamera rayMarchCamera; // Reference to RayMarchCamera script

    private int selectedSphereIndex = 0; // Index of the currently selected sphere
    private int selectedBoxIndex = 0; // Index of the currently selected box

    private bool isEditingSphere = true; // Flag to track if we're editing a sphere or a box
    private float moveSpeed = 0.5f; // Movement speed (increase this value to speed up the movement)

private void Update()
{
    if (rayMarchCamera == null || (rayMarchCamera.spheres == null && rayMarchCamera.boxes == null)) return;

    // Switch between spheres and boxes with Tab
    if (Input.GetKeyDown(KeyCode.Tab))
    {
        isEditingSphere = !isEditingSphere;

        // Ensure the selected index is within bounds
        if (isEditingSphere)
        {
            selectedSphereIndex = Mathf.Clamp(selectedSphereIndex, 0, rayMarchCamera.spheres.Count - 1);
        }
        else
        {
            selectedBoxIndex = Mathf.Clamp(selectedBoxIndex, 0, rayMarchCamera.boxes.Count - 1);
        }
    }

    // Cycle through SDFs with Left/Right Arrow keys
    if (Input.GetKeyDown(KeyCode.D))
    {
        if (isEditingSphere && rayMarchCamera.spheres.Count > 0)
        {
            selectedSphereIndex = (selectedSphereIndex + 1) % rayMarchCamera.spheres.Count;
        }
        else if (!isEditingSphere && rayMarchCamera.boxes.Count > 0)
        {
            selectedBoxIndex = (selectedBoxIndex + 1) % rayMarchCamera.boxes.Count;
        }
    }
    else if (Input.GetKeyDown(KeyCode.A))
    {
        if (isEditingSphere && rayMarchCamera.spheres.Count > 0)
        {
            selectedSphereIndex = (selectedSphereIndex - 1 + rayMarchCamera.spheres.Count) % rayMarchCamera.spheres.Count;
        }
        else if (!isEditingSphere && rayMarchCamera.boxes.Count > 0)
        {
            selectedBoxIndex = (selectedBoxIndex - 1 + rayMarchCamera.boxes.Count) % rayMarchCamera.boxes.Count;
        }
    }

    // Now apply movement or size adjustments to the currently selected SDF
    if (isEditingSphere && rayMarchCamera.spheres.Count > 0)
    {
        // Get the currently selected sphere
        Vector4 selectedSphere = rayMarchCamera.spheres[selectedSphereIndex];

        // Move sphere along the x, y, z axes
        if (Input.GetKey(KeyCode.UpArrow)) selectedSphere.y += moveSpeed;  // Move up along Y-axis
        if (Input.GetKey(KeyCode.DownArrow)) selectedSphere.y -= moveSpeed; // Move down along Y-axis
        if (Input.GetKey(KeyCode.LeftArrow)) selectedSphere.z += moveSpeed; // Move left along Z-axis
        if (Input.GetKey(KeyCode.RightArrow)) selectedSphere.z -= moveSpeed; // Move right along Z-axis

        // Shift + Left/Right Arrow to control X axis
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            if (Input.GetKey(KeyCode.LeftArrow)) selectedSphere.x -= moveSpeed; // Move left along X-axis
            if (Input.GetKey(KeyCode.RightArrow)) selectedSphere.x += moveSpeed; // Move right along X-axis
        }

        // Ctrl + Up/Down to adjust W (radius)
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            if (Input.GetKey(KeyCode.UpArrow)) selectedSphere.w += moveSpeed;  // Increase radius (W)
            if (Input.GetKey(KeyCode.DownArrow)) selectedSphere.w -= moveSpeed; // Decrease radius (W)
        }

        // Update the sphere in the RayMarchCamera's list
        rayMarchCamera.spheres[selectedSphereIndex] = selectedSphere;
    }
    else if (!isEditingSphere && rayMarchCamera.boxes.Count > 0)
    {
        // Get the currently selected box
        Vector4 selectedBox = rayMarchCamera.boxes[selectedBoxIndex];

        // Move box along the x, y, z axes
        if (Input.GetKey(KeyCode.UpArrow)) selectedBox.y += moveSpeed;  // Move up along Y-axis
        if (Input.GetKey(KeyCode.DownArrow)) selectedBox.y -= moveSpeed; // Move down along Y-axis
        if (Input.GetKey(KeyCode.LeftArrow)) selectedBox.z += moveSpeed; // Move left along Z-axis
        if (Input.GetKey(KeyCode.RightArrow)) selectedBox.z -= moveSpeed; // Move right along Z-axis

        // Shift + Left/Right Arrow to control X axis
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            if (Input.GetKey(KeyCode.LeftArrow)) selectedBox.x -= moveSpeed; // Move left along X-axis
            if (Input.GetKey(KeyCode.RightArrow)) selectedBox.x += moveSpeed; // Move right along X-axis
        }

        // Ctrl + Up/Down to adjust W (size)
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            if (Input.GetKey(KeyCode.UpArrow)) selectedBox.w += moveSpeed;  // Increase size (W)
            if (Input.GetKey(KeyCode.DownArrow)) selectedBox.w -= moveSpeed; // Decrease size (W)
        }

        // Update the box in the RayMarchCamera's list
        rayMarchCamera.boxes[selectedBoxIndex] = selectedBox;
    }

    // Reflect the changes in the Inspector
    if (!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(rayMarchCamera);
}


    private void OnGUI()
    {
        // Display which shape is currently being edited (sphere or box)
        GUI.Label(new Rect(10, 10, 300, 20), $"Editing {(isEditingSphere ? "Sphere" : "Box")} Index: {(isEditingSphere ? selectedSphereIndex : selectedBoxIndex)}");
        GUI.Label(new Rect(10, 30, 300, 20), $"Use Tab to switch shapes.");
    }

    private void OnDrawGizmos()
    {
        // Visualize spheres and boxes in the scene for better feedback
        if (rayMarchCamera != null)
        {
            Gizmos.color = Color.green;
            foreach (var sphere in rayMarchCamera.spheres)
            {
                Gizmos.DrawWireSphere(new Vector3(sphere.x, sphere.y, sphere.z), sphere.w);
            }

            Gizmos.color = Color.blue;
            foreach (var box in rayMarchCamera.boxes)
            {
                Gizmos.DrawWireCube(new Vector3(box.x, box.y, box.z), new Vector3(box.w, box.w, box.w)); // Assuming 'w' is the size
            }
        }
    }
}
