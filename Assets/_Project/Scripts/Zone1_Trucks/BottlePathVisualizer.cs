using System.Collections.Generic;
using UnityEngine;
using Project.Zone2.Bottling;

namespace Project.Zone1.Trucks
{
    /// <summary>
    /// Wizualizuje drogi między pathStartPoint a butelkami przy użyciu LineRendererów —
    /// jeden polyline per rząd: [pathStart → rowEntry → rowExit].
    /// Y wszystkich punktów wymuszane z pathStartPoint.
    /// </summary>
    public class BottlePathVisualizer : MonoBehaviour
    {
        [SerializeField] Zone2Manager zone2Manager;
        [SerializeField] Transform pathStartPoint;

        [Header("Line style")]
        [SerializeField] Material lineMaterial;
        [SerializeField] Color lineColor = new(1f, 0.85f, 0.2f, 1f);
        [SerializeField] float lineWidth = 0.08f;
        [SerializeField] bool useWorldSpace = true;

        readonly List<LineRenderer> lines = new();
        int lastRowCount = -1;
        Vector3 lastStartPos;

        void LateUpdate()
        {
            if (zone2Manager == null || pathStartPoint == null) return;

            int wanted = zone2Manager.ActiveRowCount;
            Vector3 startPos = pathStartPoint.position;

            if (wanted != lastRowCount || startPos != lastStartPos)
            {
                EnsureLineCount(wanted);
                lastRowCount = wanted;
                lastStartPos = startPos;
            }

            for (int row = 0; row < wanted; row++)
            {
                Vector3 entry = zone2Manager.GetRowEntryWorldPositionByRow(row);
                Vector3 exit = zone2Manager.GetRowExitWorldPositionByRow(row);
                entry.y = startPos.y;
                exit.y = startPos.y;

                var lr = lines[row];
                lr.positionCount = 3;
                lr.SetPosition(0, startPos);
                lr.SetPosition(1, entry);
                lr.SetPosition(2, exit);
            }
        }

        void EnsureLineCount(int count)
        {
            while (lines.Count < count)
            {
                var go = new GameObject($"PathLine_Row{lines.Count}");
                go.transform.SetParent(transform, false);
                var lr = go.AddComponent<LineRenderer>();
                lr.useWorldSpace = useWorldSpace;
                lr.startWidth = lineWidth;
                lr.endWidth = lineWidth;
                lr.startColor = lineColor;
                lr.endColor = lineColor;
                lr.numCapVertices = 4;
                lr.numCornerVertices = 4;
                if (lineMaterial != null) lr.material = lineMaterial;
                lr.alignment = LineAlignment.TransformZ;
                lines.Add(lr);
            }
            for (int i = 0; i < lines.Count; i++)
                if (lines[i] != null) lines[i].enabled = i < count;
        }
    }
}
