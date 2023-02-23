// Magica Cloth 2.
// Copyright (c) 2023 MagicaSoft.
// https://magicasoft.jp

namespace MagicaCloth2
{
    /// <summary>
    /// Planeコライダーコンポーネント
    /// Y軸方向に対する無限平面
    /// Plane Collider.
    /// Infinite plane for the Y-axis direction.
    /// </summary>
    public class MagicaPlaneCollider : ColliderComponent
    {
        public override ColliderManager.ColliderType GetColliderType()
        {
            return ColliderManager.ColliderType.Plane;
        }

        public override void DataValidate()
        {
        }
    }
}
