using System;
using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 通用输入组件系统，负责采集原始输入并发布输入事件
    /// </summary>
    [EntitySystemOf(typeof(InputComponent))]
    public static partial class InputComponentSystem
    {
        /// <summary>
        /// 松开前允许被识别为点击的最大按压时长，单位秒
        /// </summary>
        private const float ClickDurationThreshold = 0.35f;

        /// <summary>
        /// 松开前允许被识别为点击的最大屏幕位移，单位像素
        /// </summary>
        private const float ClickScreenDistanceThreshold = 12f;

        /// <summary>
        /// 按压后判定为拖拽手势的最小屏幕位移，单位像素
        /// </summary>
        private const float DragStartScreenDistanceThreshold = 12f;

        /// <summary>
        /// 初始化通用输入组件的默认状态
        /// </summary>
        [EntitySystem]
        private static void Awake(this InputComponent self)
        {
            self.MainCamera = null;
            self.PhysicsLayerMask = Physics2D.AllLayers;
            self.IsPointerPressed = false;
            self.IsPointerDragging = false;
            self.PressScreenPosition = Vector2.zero;
            self.PressWorldPosition = Vector3.zero;
            self.PressStartTime = 0f;
            self.PressHitResults = Array.Empty<InputHitResult>();
            self.ActivePointerId = -1;
            self.IsTouchPointer = false;
        }

        /// <summary>
        /// 清理通用输入组件缓存的运行时状态
        /// </summary>
        [EntitySystem]
        private static void Destroy(this InputComponent self)
        {
            self.MainCamera = null;
            self.ClearPress();
        }

        /// <summary>
        /// 每帧采集主触点或鼠标输入，并在失焦或暂停时取消当前手势
        /// </summary>
        [EntitySystem]
        private static void Update(this InputComponent self)
        {
            if (!Application.isFocused || Time.timeScale <= 0f || !self.TryGetMainCamera(out Camera mainCamera))
            {
                self.CancelPress();
                return;
            }

            if (self.IsPointerPressed && self.IsTouchPointer)
            {
                for (int i = 0; i < Input.touchCount; ++i)
                {
                    Touch touch = Input.GetTouch(i);
                    if (touch.fingerId != self.ActivePointerId)
                    {
                        continue;
                    }

                    self.ProcessPointer(mainCamera, touch.position, false,
                        touch.phase == TouchPhase.Ended, touch.phase == TouchPhase.Canceled, true, touch.fingerId);
                    return;
                }

                self.CancelPress();
                return;
            }

            if (Input.touchCount > 0)
            {
                if (self.IsPointerPressed)
                {
                    self.CancelPress();
                }

                for (int i = 0; i < Input.touchCount; ++i)
                {
                    Touch touch = Input.GetTouch(i);
                    if (touch.phase != TouchPhase.Began)
                    {
                        continue;
                    }

                    self.ProcessPointer(mainCamera, touch.position, true, false, false, true, touch.fingerId);
                    break;
                }

                return;
            }

            bool down = Input.GetMouseButtonDown(0);
            bool released = Input.GetMouseButtonUp(0);
            bool cancelled = self.IsPointerPressed && !Input.GetMouseButton(0) && !released;
            self.ProcessPointer(mainCamera, Input.mousePosition, down, released, cancelled, false, -1);
        }

        /// <summary>
        /// 将同一指针的按下、移动和释放转换为通用事件，释放在 UI 上时取消手势
        /// </summary>
        /// <param name="self">输入组件</param>
        /// <param name="mainCamera">世界坐标转换使用的相机</param>
        /// <param name="screenPosition">当前指针屏幕坐标</param>
        /// <param name="down">本帧是否刚按下</param>
        /// <param name="released">本帧是否正常释放</param>
        /// <param name="cancelled">当前指针是否被取消</param>
        /// <param name="isTouch">当前指针是否来自触屏</param>
        /// <param name="pointerId">触点标识，鼠标使用 -1</param>
        private static void ProcessPointer(this InputComponent self, Camera mainCamera, Vector2 screenPosition,
            bool down, bool released, bool cancelled, bool isTouch, int pointerId)
        {
            if (cancelled)
            {
                self.CancelPress();
                return;
            }

            if (!down && !self.IsPointerPressed)
            {
                return;
            }

            Vector3 worldPosition = self.ScreenToWorldPosition(mainCamera, screenPosition);
            InputHitResult[] hitResults = BuildHitResults(worldPosition, self.PhysicsLayerMask);

            if (down && !IsPointerOverUI(screenPosition, pointerId))
            {
                self.BeginPress(screenPosition, worldPosition, hitResults);
                self.ActivePointerId = pointerId;
                self.IsTouchPointer = isTouch;
                PointerInputContext pointerContext = self.CreatePointerContext(screenPosition, worldPosition, hitResults);
                EventSystem.Instance.Publish(self.Scene(), new PointerDown() { Context = pointerContext });
            }

            if (self.IsPointerPressed && !down && !released)
            {
                PointerInputContext pointerContext = self.CreatePointerContext(screenPosition, worldPosition, hitResults);
                if (!self.IsPointerDragging && self.HasExceededDragStartThreshold(screenPosition))
                {
                    self.BeginDrag();
                    EventSystem.Instance.Publish(self.Scene(), new PointerDragStart() { Context = pointerContext });
                }
                else if (self.IsPointerDragging)
                {
                    EventSystem.Instance.Publish(self.Scene(), new PointerDrag() { Context = pointerContext });
                }

                EventSystem.Instance.Publish(self.Scene(), new PointerHold() { Context = pointerContext });
            }

            if (self.IsPointerPressed && released)
            {
                if (IsPointerOverUI(screenPosition, pointerId))
                {
                    self.CancelPress();
                    return;
                }

                PointerInputContext pointerContext = self.CreatePointerContext(screenPosition, worldPosition, hitResults);
                if (self.IsPointerDragging)
                {
                    EventSystem.Instance.Publish(self.Scene(), new PointerDragEnd() { Context = pointerContext });
                }
                else if (self.IsClick(screenPosition))
                {
                    EventSystem.Instance.Publish(self.Scene(), new PointerClick() { Context = pointerContext });
                }

                EventSystem.Instance.Publish(self.Scene(), new PointerUp() { Context = pointerContext });
                self.ClearPress();
            }
        }

        /// <summary>
        /// 检测当前指针是否命中可拦截世界输入的 UGUI 元素，忽略仅用于阻挡 UI 穿透的 UIBlock
        /// </summary>
        /// <param name="screenPosition">当前指针屏幕坐标</param>
        /// <param name="pointerId">触点标识，鼠标使用 -1</param>
        /// <returns>指针是否被真实 UI 遮挡</returns>
        private static bool IsPointerOverUI(Vector2 screenPosition, int pointerId)
        {
            // 该文件中裸写的 EventSystem 是 ET 事件总线，这里必须用全限定名访问 UGUI 的事件系统
            UnityEngine.EventSystems.EventSystem uiEventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (uiEventSystem == null)
            {
                return false;
            }

            UnityEngine.EventSystems.PointerEventData pointerEventData = new(uiEventSystem)
            {
                position = screenPosition,
                pointerId = pointerId,
            };

            using var raycastResults = ListComponent<UnityEngine.EventSystems.RaycastResult>.Create();
            uiEventSystem.RaycastAll(pointerEventData, raycastResults);
            foreach (UnityEngine.EventSystems.RaycastResult raycastResult in raycastResults)
            {
                if (raycastResult.gameObject == null)
                {
                    continue;
                }

                // UIBlock 只负责阻挡 UI 之间的穿透，不应该阻挡世界输入
                if (raycastResult.gameObject.GetComponent<YIUIFramework.UIBlock>() != null)
                {
                    continue;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// 获取当前可用于输入转换的主相机
        /// </summary>
        /// <param name="self">输入组件自身</param>
        /// <param name="mainCamera">返回找到的主相机</param>
        /// <returns>是否成功找到可用主相机</returns>
        private static bool TryGetMainCamera(this InputComponent self, out Camera mainCamera)
        {
            mainCamera = self.MainCamera;
            if (mainCamera != null && mainCamera.isActiveAndEnabled)
            {
                return true;
            }

            mainCamera = Camera.main;
            self.MainCamera = mainCamera;
            return mainCamera != null && mainCamera.isActiveAndEnabled;
        }

        /// <summary>
        /// 将当前屏幕坐标转换为二维交互使用的世界坐标
        /// </summary>
        /// <param name="self">输入组件自身</param>
        /// <param name="mainCamera">执行转换使用的主相机</param>
        /// <param name="screenPosition">待转换的屏幕坐标</param>
        /// <returns>转换后的世界坐标</returns>
        private static Vector3 ScreenToWorldPosition(this InputComponent self, Camera mainCamera, Vector2 screenPosition)
        {
            Vector3 screenPoint = new Vector3(screenPosition.x, screenPosition.y, Mathf.Abs(mainCamera.transform.position.z));
            Vector3 worldPosition = mainCamera.ScreenToWorldPoint(screenPoint);
            worldPosition.z = 0f;
            return worldPosition;
        }

        /// <summary>
        /// 记录一次新的按压开始状态
        /// </summary>
        /// <param name="self">输入组件自身</param>
        /// <param name="screenPosition">本次按压开始时的屏幕坐标</param>
        /// <param name="worldPosition">本次按压开始时的世界坐标</param>
        /// <param name="hitResults">本次按压开始时命中的全部结果数组</param>
        private static void BeginPress(this InputComponent self, Vector2 screenPosition, Vector3 worldPosition, InputHitResult[] hitResults)
        {
            self.IsPointerPressed = true;
            self.IsPointerDragging = false;
            self.PressScreenPosition = screenPosition;
            self.PressWorldPosition = worldPosition;
            self.PressStartTime = Time.unscaledTime;
            self.PressHitResults = hitResults ?? Array.Empty<InputHitResult>();
        }

        /// <summary>
        /// 将当前按压标记为已经进入拖拽手势
        /// </summary>
        /// <param name="self">输入组件自身</param>
        private static void BeginDrag(this InputComponent self)
        {
            self.IsPointerDragging = true;
        }

        /// <summary>
        /// 清理一次按压结束后的状态缓存
        /// </summary>
        /// <param name="self">输入组件自身</param>
        private static void ClearPress(this InputComponent self)
        {
            self.IsPointerPressed = false;
            self.IsPointerDragging = false;
            self.PressScreenPosition = Vector2.zero;
            self.PressWorldPosition = Vector3.zero;
            self.PressStartTime = 0f;
            self.PressHitResults = Array.Empty<InputHitResult>();
            self.ActivePointerId = -1;
            self.IsTouchPointer = false;
        }

        /// <summary>
        /// 取消未完成的按压并通知业务层回退拖拽和选中表现
        /// </summary>
        /// <param name="self">输入组件</param>
        private static void CancelPress(this InputComponent self)
        {
            if (!self.IsPointerPressed)
            {
                return;
            }

            PointerInputContext context = self.CreatePointerContext(self.PressScreenPosition, self.PressWorldPosition,
                Array.Empty<InputHitResult>());
            self.ClearPress();
            EventSystem.Instance.Publish(self.Scene(), new PointerCancel() { Context = context });
        }

        /// <summary>
        /// 根据当前组件状态与命中结果组装通用输入上下文
        /// </summary>
        /// <param name="self">输入组件自身</param>
        /// <param name="screenPosition">当前屏幕坐标</param>
        /// <param name="worldPosition">当前世界坐标</param>
        /// <param name="hitResults">当前命中结果数组</param>
        /// <returns>可直接发布给业务层的输入上下文</returns>
        private static PointerInputContext CreatePointerContext(this InputComponent self, Vector2 screenPosition, Vector3 worldPosition, InputHitResult[] hitResults)
        {
            return new PointerInputContext()
            {
                Button = PointerButton.Left,
                ScreenPosition = screenPosition,
                WorldPosition = worldPosition,
                PressScreenPosition = self.PressScreenPosition,
                PressWorldPosition = self.PressWorldPosition,
                Duration = self.IsPointerPressed ? Time.unscaledTime - self.PressStartTime : 0f,
                HitResults = hitResults,
                PressHitResults = self.PressHitResults ?? Array.Empty<InputHitResult>(),
            };
        }

        /// <summary>
        /// 判断当前按压是否已经超过通用拖拽起始阈值
        /// </summary>
        /// <param name="self">输入组件自身</param>
        /// <param name="screenPosition">当前指针屏幕坐标</param>
        /// <returns>当前按压是否应进入拖拽手势</returns>
        private static bool HasExceededDragStartThreshold(this InputComponent self, Vector2 screenPosition)
        {
            return Vector2.Distance(self.PressScreenPosition, screenPosition) > DragStartScreenDistanceThreshold;
        }

        /// <summary>
        /// 判断本次按压抬起是否满足点击阈值
        /// </summary>
        /// <param name="self">输入组件自身</param>
        /// <param name="releaseScreenPosition">抬起时的屏幕坐标</param>
        /// <returns>是否应视为一次点击</returns>
        private static bool IsClick(this InputComponent self, Vector2 releaseScreenPosition)
        {
            float duration = Time.unscaledTime - self.PressStartTime;
            float distance = Vector2.Distance(self.PressScreenPosition, releaseScreenPosition);
            return duration <= ClickDurationThreshold && distance <= ClickScreenDistanceThreshold;
        }

        /// <summary>
        /// 查询当前世界坐标下全部二维碰撞命中，并补齐 Unity 到 ET 的桥接信息
        /// </summary>
        /// <param name="worldPosition">待检测的世界坐标</param>
        /// <param name="physicsLayerMask">命中检测使用的 LayerMask</param>
        /// <returns>当前坐标命中的全部结果数组</returns>
        private static InputHitResult[] BuildHitResults(Vector3 worldPosition, int physicsLayerMask)
        {
            Collider2D[] colliders = Physics2D.OverlapPointAll(worldPosition, physicsLayerMask);
            if (colliders == null || colliders.Length == 0)
            {
                return Array.Empty<InputHitResult>();
            }

            InputHitResult[] hitResults = new InputHitResult[colliders.Length];
            for (int i = 0; i < colliders.Length; ++i)
            {
                Collider2D collider2D = colliders[i];
                GameObject gameObject = collider2D.gameObject;
                GameObjectEntityRef entityRef = gameObject.GetComponent<GameObjectEntityRef>();
                if (entityRef == null)
                {
                    entityRef = gameObject.GetComponentInParent<GameObjectEntityRef>();
                }

                hitResults[i] = new InputHitResult()
                {
                    Collider2D = collider2D,
                    GameObject = gameObject,
                    Transform = collider2D.transform,
                    Tag = gameObject.tag,
                    Entity = entityRef != null ? entityRef.Entity : null,
                };
            }

            return hitResults;
        }
    }
}
