using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 图鉴中可容纳两侧拼图的一行
    /// </summary>
    public partial class ArchiveItemContentComponent : Entity
    {
        public const int ItemsPerSide = 3;
        public const int Capacity = ItemsPerSide * 2;
        public List<EntityRef<ArchiveItemComponent>> Items;
    }
}
