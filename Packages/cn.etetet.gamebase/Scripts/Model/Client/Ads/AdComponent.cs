using MongoDB.Bson.Serialization.Attributes;

namespace ET.Client
{
    /// <summary>
    /// 挂载在客户端场景的广告能力组件
    /// </summary>
    [ComponentOf(typeof(Scene))]
    public class AdComponent : Entity, IAwake<AdService>, ILateUpdate, IDestroy
    {
        /// <summary>
        /// 随组件释放的运行时广告服务
        /// </summary>
        [BsonIgnore]
        public AdService Service;
    }
}
