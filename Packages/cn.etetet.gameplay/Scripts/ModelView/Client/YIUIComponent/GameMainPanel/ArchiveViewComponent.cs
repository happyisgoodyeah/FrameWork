using System.Collections.Generic;
using UnityEngine.UI;

namespace ET.Client
{
    /// <summary>图鉴条目的选择事件</summary>
    public struct ArchiveItemSelected
    {
        public EntityRef<ArchiveItemComponent> Item;
    }

    /// <summary>
    /// 图鉴视图的列表和选中状态
    /// </summary>
    public partial class ArchiveViewComponent : Entity, IDynamicEvent<ArchiveItemSelected>
    {
        public ScrollRect ScrollRect;
        public List<EntityRef<ArchiveItemContentComponent>> Contents;
        public EntityRef<ArchiveItemComponent> SelectedItem;
    }
}
