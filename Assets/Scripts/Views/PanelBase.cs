using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
namespace Views
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class PanelBase : MonoBehaviour
    {
	    // 存储控件的字典
	    private readonly Dictionary<Type, Dictionary<string, UIBehaviour>> m_Elements = new Dictionary<Type, Dictionary<string, UIBehaviour>>();
        private readonly Dictionary<Type, HashSet<UIBehaviour>> m_ElementUnnamed = new Dictionary<Type, HashSet<UIBehaviour>>();
        
        private CanvasGroup m_canvasGroup;

        // 此类支持访问的控件类型
        public static readonly Type[] SupportTypes = { 
            // 基础控件
            typeof(Image),
            typeof(Text),
            typeof(RawImage),
            typeof(TMP_Text),
            typeof(TextMeshProUGUI),
            typeof(GridLayoutGroup),
            typeof(CanvasGroup),
            // 组合控件
            typeof(Button),
            typeof(Toggle),
            typeof(InputField),
            typeof(Slider),
            typeof(ScrollRect),
            typeof(Dropdown),
            typeof(Selectable),
        };

        // 默认层级
        public virtual int DefaultLayer => 0;

        protected virtual void Awake()
        {
            foreach (Type type in SupportTypes)
                AddElements(type);
            m_canvasGroup = GetComponent<CanvasGroup>();
        }

        /// <summary>
        /// 得到该面板上记录的某一类型的控件
        /// </summary>
        /// <typeparam name="T">控件类型</typeparam>
        /// <param name="elementName">控件名</param>
        public T GetElement<T>(string elementName) where T : UIBehaviour
        {
            // 检查是否存在该类型控件
            if (m_Elements.ContainsKey(typeof(T)))
            {
                // 获取同名控件
                m_Elements[typeof(T)].TryGetValue(elementName, out UIBehaviour element);
    #if UNITY_EDITOR
                if (element == null)
                    Debug.LogError($"面板{this.name}上没有名为{elementName}的{typeof(T).Name}控件");
    #endif
                return element as T;
            }

            // 没有找到该类型的控件时 检查自身是否支持该类型
            else
            {
    #if UNITY_EDITOR
                bool support = false;
                foreach (Type type in SupportTypes)
                {
                    if (type == typeof(T))
                    {
                        support = true;
                        break;
                    }
                }
                if (support)
                    Debug.LogError($"面板{this.name}上没有{typeof(T).Name}控件");
                else
                    Debug.LogError($"PanelBase不支持记录{typeof(T).Name}控件");
    #endif
                return null;
            }
        }

        public RectTransform GetTransform(string objectName)
        {
            if (name == objectName)
                return transform as RectTransform;
            return GetComponentsInChildren<RectTransform>().FirstOrDefault(child => child.gameObject.name == objectName);
        }

        /// <summary>
        /// 为一个控件添加EventTrigger事件
        /// </summary>
        /// <typeparam name="T">控件类型</typeparam>
        /// <param name="elementName">控件名</param>
        /// <param name="eventID">事件类型</param>
        /// <param name="func">事件触发时的处理函数</param>
        public void AddEntry<T>(string elementName, EventTriggerType eventID, UnityAction<BaseEventData> func) where T : UIBehaviour
        {
            T element = GetElement<T>(elementName);
            if (element == null)
                return;

            AddEntry(element.transform, eventID, func);
        }

        public void AddEntry(Transform element, EventTriggerType eventID, UnityAction<BaseEventData> func)
        {
	        // 添加EventTrigger组件
	        EventTrigger trigger = element.GetOrAddComponent<EventTrigger>();
	        // 添加Entry
	        EventTrigger.Entry entry = new EventTrigger.Entry()
	        {
		        eventID = eventID
	        };
	        entry.callback.AddListener(func);
	        trigger.triggers.Add(entry);
	    }

        /// <summary>
        /// 显示此面板
        /// </summary>
        public virtual void Show()
        {
            m_canvasGroup.interactable = true;
            m_canvasGroup.blocksRaycasts = true;
            m_canvasGroup.alpha = 1;
        }

        /// <summary>
        /// 隐藏此面板
        /// </summary>
        public virtual void Hide()
        {
            m_canvasGroup.interactable = false;
            m_canvasGroup.blocksRaycasts = false;
            m_canvasGroup.alpha = 0;
        }
        
        
        // 记录面板上某一类型的控件
        private void AddElements(Type type)
        {
            foreach (Component component in GetAllComponents())
            {
	            if (!(component is UIBehaviour element))
		            continue;
	            if (!m_Elements.ContainsKey(type))
                    m_Elements.Add(type, new Dictionary<string, UIBehaviour>());
                if (m_Elements[type].ContainsKey(element.name))
                {
    #if UNITY_EDITOR
                Debug.LogError($"面板{name}上存在多个名为{element.name}的{type.Name}控件");
    #endif
                }
                else
                {
                    m_Elements[type].Add(element.name, element);
                }

                if (!m_ElementUnnamed.ContainsKey(type))
                    m_ElementUnnamed.Add(type, new HashSet<UIBehaviour>());
                m_ElementUnnamed[type].Add(element);
            }

            IEnumerable<Component> GetAllComponents()
            {
	            foreach (Component component in GetComponents(type))
		            yield return component;
	            foreach (Component component in GetComponentsInChildren(type, true))
		            yield return component;
            }
        }
        
    }

    /// <summary>
    /// 带开启参数的面板类
    /// </summary>
    /// <typeparam name="TParam"></typeparam>
    public abstract class PanelBase<TParam> : PanelBase
    {
        public virtual void Show(TParam param){ Show();}
	    public sealed override void Show() { base.Show(); }
    }
}