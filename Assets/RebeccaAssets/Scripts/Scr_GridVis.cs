
using UnityEngine;

public class Scr_GridVis : MonoBehaviour
{
    public int gridSize = 10;
    public float cellSize = 1f;
    public float lineHeight = 0.015f;

    public Color gridColor = new Color(0.3f, 0.8f, 0.4f, 1f);

    void Start()
    {
        CreateGrid();
    }

    void CreateGrid()
    {
        // Draw grid lines in both directions.
        for (int i = 0; i <= gridSize; i++)
        {
            float offset = i * cellSize;

            CreateLine(
                new Vector3(offset, lineHeight, 0),
                new Vector3(offset, lineHeight, gridSize * cellSize)
            );

            CreateLine(
                new Vector3(0, lineHeight, offset),
                new Vector3(gridSize * cellSize, lineHeight, offset)
            );
        }
    }

    void CreateLine(Vector3 start, Vector3 end)
    {
        GameObject lineObject = new GameObject("GridLine");
        lineObject.transform.SetParent(transform, false);

        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.useWorldSpace = false;
        line.SetPosition(0, start);
        line.SetPosition(1, end);
        line.startWidth = 0.015f;
        line.endWidth = 0.015f;
        line.startColor = gridColor;
        line.endColor = gridColor;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        Material material = new Material(shader);
        material.color = gridColor;
        line.material = material;
    }
}