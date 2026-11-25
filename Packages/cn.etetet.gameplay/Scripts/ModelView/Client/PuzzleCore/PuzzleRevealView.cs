using UnityEngine;

namespace ET.Client
{
    /// <summary>一个解谜模型对应的场景命中区域，随该模型释放。</summary>
    [ComponentOf(typeof(PuzzleRevealComponent))]
    public class PuzzleRevealView : Entity, IAwake<GameObject>, IDestroy
    {
        public Collider2D[] HitAreas { get; set; }
    }
}
