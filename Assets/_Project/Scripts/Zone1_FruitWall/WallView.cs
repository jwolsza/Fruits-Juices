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

        public float CellWidth => cellWidth;
        public float CellHeight => cellHeight;

        public Vector3 GetCellWorldPosition(int cellX, int cellY)
        {
            if (cellRenderers == null || grid == null) return transform.position;
            if (cellX < 0 || cellX >= grid.Columns || cellY < 0 || cellY >= grid.Rows) return transform.position;
            var renderer = cellRenderers[cellX, cellY];
            return renderer != null ? renderer.transform.position : transform.position;
        }

        public Vector2 GetCellWorldSize()
        {
            if (cellRenderers == null || grid == null || grid.Columns == 0 || grid.Rows == 0)
                return Vector2.one * 0.05f;
            var scale = cellRenderers[0, 0].transform.lossyScale;
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

            for (int x = 0; x < grid.Columns; x++)
                for (int y = 0; y < grid.Rows; y++)
                    CreateCell(x, y);
        }

        /// <summary>
        /// After grid was resized to (newCols, newRows), instantiate sprites for newly added cells.
        /// Existing cells (x<oldCols, y<oldRows) are preserved with their world positions and fruits.
        /// </summary>
        public void ResyncToGrid()
        {
            if (grid == null) return;
            int oldCols = cellRenderers != null ? cellRenderers.GetLength(0) : 0;
            int oldRows = cellRenderers != null ? cellRenderers.GetLength(1) : 0;
            int newCols = grid.Columns;
            int newRows = grid.Rows;
            if (newCols == oldCols && newRows == oldRows) return;

            var newCellRenderers = new SpriteRenderer[newCols, newRows];
            var newLastColors = new Color[newCols, newRows];

            for (int x = 0; x < oldCols && x < newCols; x++)
                for (int y = 0; y < oldRows && y < newRows; y++)
                {
                    newCellRenderers[x, y] = cellRenderers[x, y];
                    newLastColors[x, y] = lastColors[x, y];
                }

            cellRenderers = newCellRenderers;
            lastColors = newLastColors;

            for (int x = 0; x < newCols; x++)
                for (int y = 0; y < newRows; y++)
                    if (cellRenderers[x, y] == null) CreateCell(x, y);
        }

        void CreateCell(int x, int y)
        {
            var go = new GameObject($"Cell_{x}_{y}");
            go.transform.SetParent(transform, worldPositionStays: false);
            go.transform.localPosition = new Vector3(
                x * cellWidth + cellWidth * 0.5f,
                y * cellHeight + cellHeight * 0.5f,
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

            int cols = cellRenderers.GetLength(0);
            int rows = cellRenderers.GetLength(1);
            for (int x = 0; x < cols && x < grid.Columns; x++)
            {
                for (int y = 0; y < rows && y < grid.Rows; y++)
                {
                    var renderer = cellRenderers[x, y];
                    if (renderer == null) continue;
                    var cell = grid.GetCell(x, y);
                    Color desired = cell.HasValue
                        ? FruitColorPalette.GetColor(cell.Value)
                        : FruitColorPalette.EmptyColor;

                    if (lastColors[x, y] != desired)
                    {
                        renderer.color = desired;
                        lastColors[x, y] = desired;
                    }
                }
            }
        }
    }
}
