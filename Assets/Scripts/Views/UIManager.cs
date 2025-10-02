using System.Collections.Generic;
using UnityEngine;
namespace Views
{
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private PanelBase[] panelRegistry;

        private readonly Stack<PanelBase> _stack = new();
        private readonly Dictionary<string, PanelBase> _map = new();

        void Awake()
        {
            _map.Clear();
            foreach (var p in panelRegistry)
            {
                if (!p) continue;
                var id = nameof(p);
                if (!_map.TryAdd(id, p))
                    Debug.LogWarning($"重复的 PanelId: {id}");

                p.Hide(); // 初始全部隐藏
            }
        }

        /// 打开面板：可选择是否清空当前栈
        public void Push(string id, bool clearStack = false, object param = null)
        {
            if (!_map.TryGetValue(id, out var next))
            {
                Debug.LogError($"未注册的面板: {id}");
                return;
            }

            if (clearStack)
                ClearStackInternal();


            next.Show();
            _stack.Push(next);
        }

        /// 返回上一层
        public void Back()
        {
            if (_stack.Count <= 1) return;

            var cur = _stack.Pop();
            cur.Hide();

            var prev = _stack.Peek();
            prev.Show();
        }

        /// 关闭到根（保留最底层）
        public void PopToRoot()
        {
            while (_stack.Count > 1)
            {
                var cur = _stack.Pop();
                cur.Hide();
            }
            _stack.Peek().Show();
        }

        /// 清空所有（通常用于回到主菜单前）
        public void ClearAll()
        {
            ClearStackInternal();
        }

        private void ClearStackInternal()
        {
            while (_stack.Count > 0)
            {
                var cur = _stack.Pop();
                cur.Hide();
            }
        }

        public bool TryGet(string id, out PanelBase panel) => _map.TryGetValue(id, out panel);
        public PanelBase Current => _stack.Count > 0 ? _stack.Peek() : null;
    }

}