using UnityEngine;
using Project.Core;

namespace Project.Zone1.FruitWall
{
    public static class FruitColorPalette
    {
        public static readonly Color EmptyColor = new Color(0.10f, 0.10f, 0.10f, 0.0f);

        public static Color GetColor(FruitType type)
        {
            switch (type)
            {
                case FruitType.Apple:      return new Color(0.85f, 0.10f, 0.10f);
                case FruitType.Orange:     return new Color(1.00f, 0.55f, 0.00f);
                case FruitType.Lemon:      return new Color(0.95f, 0.90f, 0.20f);
                case FruitType.Strawberry: return new Color(0.95f, 0.30f, 0.55f);
                case FruitType.Grape:      return new Color(0.55f, 0.20f, 0.75f);
                case FruitType.Banana:     return new Color(0.95f, 0.80f, 0.30f);
                case FruitType.Kiwi:       return new Color(0.55f, 0.75f, 0.30f);
                case FruitType.Pineapple:  return new Color(0.95f, 0.85f, 0.45f);
                case FruitType.Watermelon: return new Color(0.40f, 0.75f, 0.45f);
                case FruitType.Mango:      return new Color(0.95f, 0.60f, 0.20f);
                default: return Color.gray;
            }
        }
    }
}
