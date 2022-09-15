using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

[CustomEditor(typeof(Grid))]
public class GridEditor : Editor
{

    public enum SideEdit { Left, Right, Up, Down, Finish}
    public SideEdit side;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        Grid grid = (Grid)target;

        GUILayout.Label("----Edit Tools----");
        side = (SideEdit)EditorGUILayout.EnumPopup("Wall", side);

        if (GUILayout.Button("Generate Grid"))
        {
            grid.GenerateGrid();
            grid.GenerateMesh();
        }
        if (GUILayout.Button("Generate Mesh"))
        {
            grid.GenerateMesh();
        }
    }

    public void OnSceneGUI()
    {
        // Gets the data from the grid.
        Grid grid = (Grid)target;
        Camera cmr = Camera.current;
        Event e = Event.current;

        // Makes a ray to find the grid position based on the mouse.
        Vector2 mousePos = e.mousePosition;
        mousePos.y = Screen.height - mousePos.y - 40f;
        Ray ray = cmr.ScreenPointToRay(mousePos);

        // Gets the index on the grid of the mouse.
        Vector2Int indexV2 = new Vector2Int(Mathf.RoundToInt(ray.origin.x / grid.blockSize + grid.stageSize.x * 0.5f),
                                            Mathf.RoundToInt(ray.origin.y / grid.blockSize + grid.stageSize.y * 0.5f));
        indexV2.x = Mathf.Clamp(indexV2.x, 0, grid.stageSize.x - 1);
        indexV2.y = Mathf.Clamp(indexV2.y, 0, grid.stageSize.y - 1);
        int index = grid.V2ToInt(indexV2);

        if (e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
        {
            if (e.alt || e.control || e.shift)
                return;

            // If the middle button is pressed, make the currently selected side a wall
            if (e.button == 2)
            {
                switch (side)
                {
                    case SideEdit.Left:
                        grid.grid[index].leftBlocked = true;
                        break;
                    case SideEdit.Right:
                        grid.grid[index].rightBlocked = true;
                        break;
                    case SideEdit.Up:
                        grid.grid[index].upBlocked = true;
                        break;
                    case SideEdit.Down:
                        grid.grid[index].downBlocked = true;
                        break;
                    case SideEdit.Finish:
                        grid.grid[index].isFinish = true;
                        break;
                }
                grid.GenerateMesh();
            }
            // If the left button is pressed, make the currently selected side empty
            else if (e.button == 1)
            {
                switch (side)
                {
                    case SideEdit.Left:
                        grid.grid[index].leftBlocked = false;
                        break;
                    case SideEdit.Right:
                        grid.grid[index].rightBlocked = false;
                        break;
                    case SideEdit.Up:
                        grid.grid[index].upBlocked = false;
                        break;
                    case SideEdit.Down:
                        grid.grid[index].downBlocked = false;
                        break;
                    case SideEdit.Finish:
                        grid.grid[index].isFinish = false;
                        break;
                }
                grid.GenerateMesh();
            }
            else return;

            e.Use();
            Undo.RecordObject(grid, "Changed Block");

        }
    }

}
