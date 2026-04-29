using UnityEngine;

namespace Project.Zone1.FruitWall
{
    public class WallView : MonoBehaviour
    {
        [Header("Visual")]
        [SerializeField] Sprite cellSprite;
        [SerializeField] float cellSize = 0.05f;
        [SerializeField] string sortingLayerName = "Default";
        [SerializeField] int sortingOrderBase = 0;

        FruitGrid grid;
        SpriteRenderer[,] cellRenderers;
        Color[,] lastColors;

        public void Initialize(FruitGrid grid)
        {
            this.grid = grid;
            cellRenderers = new SpriteRenderer[grid.Columns, grid.Rows];
            lastColors = new Color[grid.Columns, grid.Rows];

            for (int x = 0; x < grid.Columns; x++)
            {
                for (int y = 0; y < grid.Rows; y++)
                {
                    var go = new GameObject($"Cell_{x}_{y}");
                    go.transform.SetParent(transform, worldPositionStays: false);
                    go.transform.localPosition = new Vector3(
                        x * cellSize + cellSize * 0.5f,
                        y * cellSize + cellSize * 0.5f,
                        0f);
                    go.transform.localScale = Vector3.one * cellSize;

                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = cellSprite;
                    sr.color = FruitColorPalette.EmptyColor;
                    sr.sortingLayerName = sortingLayerName;
                    sr.sortingOrder = sortingOrderBase;

                    cellRenderers[x, y] = sr;
                    lastColors[x, y] = sr.color;
                }
            }
        }

        void LateUpdate()
        {
            if (grid == null || cellRenderers == null) return;

            for (int x = 0; x < grid.Columns; x++)
            {
                for (int y = 0; y < grid.Rows; y++)
                {
                    var cell = grid.GetCell(x, y);
                    Color desired = cell.HasValue
                        ? FruitColorPalette.GetColor(cell.Value)
                        : FruitColorPalette.EmptyColor;

                    if (lastColors[x, y] != desired)
                    {
                        cellRenderers[x, y].color = desired;
                        lastColors[x, y] = desired;
                    }
                }
            }
        }
    }
}
