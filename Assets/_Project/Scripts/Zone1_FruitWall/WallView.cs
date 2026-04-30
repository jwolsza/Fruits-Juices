using UnityEngine;

namespace Project.Zone1.FruitWall
{
    public class WallView : MonoBehaviour
    {
        [Header("Visual")]
        [Tooltip("Optional. If null, WallView generates a 1x1 white square sprite at runtime that fully fills each cell.")]
        [SerializeField] Sprite cellSprite;
        [SerializeField] string sortingLayerName = "Default";
        [SerializeField] int sortingOrderBase = 0;

        FruitGrid grid;
        SpriteRenderer[,] cellRenderers;
        Color[,] lastColors;
        Sprite generatedSprite;
        float cellWidth;
        float cellHeight;
        // Anchor offset — local position of cell (0,0) center is (anchorX + 0.5*cellWidth, anchorY + 0.5*cellHeight, 0).
        // Negative anchor = wall extended to the left / down. Used so existing cells don't move when growing.
        float cellAnchorX;
        float cellAnchorY;

        public float CellWidth => cellWidth;
        public float CellHeight => cellHeight;

        public Vector3 GetCellWorldPosition(int cellX, int cellY)
        {
            if (cellRenderers == null || grid == null) return transform.position;
            if (cellX < 0 || cellX >= grid.Columns || cellY < 0 || cellY >= grid.Rows) return transform.position;
            var sr = cellRenderers[cellX, cellY];
            return sr != null ? sr.transform.position : transform.position;
        }

        public Vector2 GetCellWorldSize()
        {
            if (cellRenderers == null || grid == null || grid.Columns == 0 || grid.Rows == 0)
                return Vector2.one * 0.05f;
            var scale = cellRenderers[0, 0] != null ? cellRenderers[0, 0].transform.lossyScale : Vector3.one;
            return new Vector2(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
        }

        Sprite ResolveSprite()
        {
            if (cellSprite != null) return cellSprite;
            if (generatedSprite != null) return generatedSprite;

            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, mipChain: false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            generatedSprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, 1f, 1f),
                pivot: new Vector2(0.5f, 0.5f),
                pixelsPerUnit: 1f);
            return generatedSprite;
        }

        public void Initialize(FruitGrid grid, float wallWidth, float wallHeight)
        {
            this.grid = grid;
            cellRenderers = new SpriteRenderer[grid.Columns, grid.Rows];
            lastColors = new Color[grid.Columns, grid.Rows];

            cellWidth = wallWidth / grid.Columns;
            cellHeight = wallHeight / grid.Rows;
            cellAnchorX = 0f;
            cellAnchorY = 0f;

            for (int x = 0; x < grid.Columns; x++)
                for (int y = 0; y < grid.Rows; y++)
                    CreateCell(x, y);
        }

        /// <summary>
        /// Called AFTER grid was grown with addLeft/addRight/addTop. Shifts cellRenderers array
        /// data by addLeft (so old indices match new array layout), updates anchor for left growth
        /// (so existing cells stay at same world position), instantiates new sprites for new cells.
        /// </summary>
        public void OnGridGrew(int addLeft, int addRight, int addTop)
        {
            if (grid == null) return;
            if (addLeft <= 0 && addRight <= 0 && addTop <= 0) return;

            int oldCols = cellRenderers != null ? cellRenderers.GetLength(0) : 0;
            int oldRows = cellRenderers != null ? cellRenderers.GetLength(1) : 0;
            int newCols = grid.Columns;
            int newRows = grid.Rows;
            int xShift = Mathf.Max(0, addLeft);

            var newCellRenderers = new SpriteRenderer[newCols, newRows];
            var newLastColors = new Color[newCols, newRows];

            // Copy existing renderer references shifted by xShift in x.
            for (int x = 0; x < oldCols; x++)
                for (int y = 0; y < oldRows; y++)
                {
                    newCellRenderers[x + xShift, y] = cellRenderers[x, y];
                    newLastColors[x + xShift, y] = lastColors[x, y];
                }

            cellRenderers = newCellRenderers;
            lastColors = newLastColors;

            // Update anchor for left-growth so existing cells stay at same world position.
            // (Top growth doesn't shift existing cells, only adds at high y — anchor unchanged.)
            cellAnchorX -= addLeft * cellWidth;

            // Instantiate sprites for new (empty) cells.
            for (int x = 0; x < newCols; x++)
                for (int y = 0; y < newRows; y++)
                    if (cellRenderers[x, y] == null) CreateCell(x, y);
        }

        void CreateCell(int x, int y)
        {
            var go = new GameObject($"Cell_{x}_{y}");
            go.transform.SetParent(transform, worldPositionStays: false);
            go.transform.localPosition = new Vector3(
                cellAnchorX + x * cellWidth + cellWidth * 0.5f,
                cellAnchorY + y * cellHeight + cellHeight * 0.5f,
                0f);
            go.transform.localScale = new Vector3(cellWidth, cellHeight, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = ResolveSprite();
            sr.color = FruitColorPalette.EmptyColor;
            sr.sortingLayerName = sortingLayerName;
            sr.sortingOrder = sortingOrderBase;

            cellRenderers[x, y] = sr;
            lastColors[x, y] = sr.color;
        }

        void LateUpdate()
        {
            if (grid == null || cellRenderers == null) return;

            int cols = Mathf.Min(cellRenderers.GetLength(0), grid.Columns);
            int rows = Mathf.Min(cellRenderers.GetLength(1), grid.Rows);
            for (int x = 0; x < cols; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    var sr = cellRenderers[x, y];
                    if (sr == null) continue;
                    var cell = grid.GetCell(x, y);
                    Color desired = cell.HasValue
                        ? FruitColorPalette.GetColor(cell.Value)
                        : FruitColorPalette.EmptyColor;

                    if (lastColors[x, y] != desired)
                    {
                        sr.color = desired;
                        lastColors[x, y] = desired;
                    }
                }
            }
        }
    }
}
