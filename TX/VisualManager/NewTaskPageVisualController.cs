using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TX.Models;
using TX.NetWork.NetWorkAnalysers;
using Windows.UI.Xaml.Controls;

namespace TX.VisualManager
{
    //NewTaskPage对应一个Controller
    //Analyser Dispose时要还原对Controller的修改
    //所有函数都应lock(operationLock)，保证不会执行到一半的时候修改权限
    //所有函数都不应执行过长时间
    public class NewTaskPageVisualController
    {
        private object operationLock = new object();

        private Collection<AbstractAnalyser> analysers = new Collection<AbstractAnalyser>();

        private ObservableCollection<LinkAnalysisMessage> linkAnalysisMessages;
        private VisibilityAnimationManager threadLayoutVisibilityManager;
        private VisibilityAnimationManager comboBoxLayoutVisibilityManager;
        private Dictionary<string, LinkAnalysisMessage> existMessages
            = new Dictionary<string, LinkAnalysisMessage>();
        private Button submitButton;
        private TextBlock recommendedNameBlock;
        private ComboBox comboBox;
        private ObservableCollection<PlainTextComboBoxData> comboBoxItems;
        public Action<PlainTextComboBoxData> ComboBoxSelectionChanged { private get; set; }

        /// <summary>
        /// 对Analyser的权限管理：每个函数第一个参数要求传入正在调用的Analyser
        /// 当当前调用的Analyser没有修改权限时，修改将不会生效
        /// 有权限的Analyser或null才能修改其他Analyser的权限
        /// </summary>
        /// 
        public bool CheckPermission(AbstractAnalyser analyser)
        {
            return analysers.Contains(analyser);
        }

        public void RegistAnalyser(AbstractAnalyser me, AbstractAnalyser analyser)
        {
            lock (operationLock)
            {
                if (me != null && !CheckPermission(me)) return;
                if (analyser == null) throw new Exception("不能为null添加权限");
                analysers.Add(analyser);
            }
        }

        public void RemoveAnalyser(AbstractAnalyser analyser)
        {
            lock (operationLock)
            {
                if (analyser == null) throw new Exception("不能为null移除权限");
                while (analysers.Contains(analyser))
                    analysers.Remove(analyser);
                GC.Collect();
            }
        }

        public void RemoveAllAnalysers()
        {
            analysers.Clear();
            GC.Collect();
        }

        public NewTaskPageVisualController(ObservableCollection<LinkAnalysisMessage> linkAnalysisMessages,
            VisibilityAnimationManager threadLayoutVisibilityManager,
            VisibilityAnimationManager comboBoxLayoutVisibilityManager,
            Button submitButton,
            TextBlock recommendedNameBlock,
            ComboBox comboBox,
            ObservableCollection<PlainTextComboBoxData> comboBoxItems)
        {
            this.linkAnalysisMessages = linkAnalysisMessages;
            this.threadLayoutVisibilityManager = threadLayoutVisibilityManager;
            this.comboBoxLayoutVisibilityManager = comboBoxLayoutVisibilityManager;
            this.submitButton = submitButton;
            this.recommendedNameBlock = recommendedNameBlock;
            this.comboBox = comboBox;
            comboBox.SelectionChanged += (s, e) =>
            {
                ComboBoxSelectionChanged?.Invoke((PlainTextComboBoxData)((ComboBox)s).SelectedItem);
            };
            this.comboBoxItems = comboBoxItems;
        }

        public void SetComboBoxSelectionChangedListener(AbstractAnalyser me, Action<PlainTextComboBoxData> listener)
        {
            lock(operationLock)
            {
                if (me != null && !CheckPermission(me)) return;
                this.ComboBoxSelectionChanged = listener;
            }
        }

        public void ClearComboBoxItem(AbstractAnalyser me)
        {
            lock (operationLock)
            {
                if (me != null && !CheckPermission(me)) return;
                comboBoxItems.Clear();
            }
        }

        public void AddComboBoxItem(AbstractAnalyser me, PlainTextComboBoxData item)
        {
            lock (operationLock)
            {
                if (me != null && !CheckPermission(me)) return;
                comboBoxItems.Add(item);
            }
        }

        public void UpdateMessage(AbstractAnalyser me, string key, LinkAnalysisMessage message)
        {
            lock (operationLock)
            {
                if (me != null && !CheckPermission(me)) return;
                if (existMessages.ContainsKey(key))
                {
                    LinkAnalysisMessage intermes = existMessages[key];
                    if (intermes.Equals(message))
                        return;
                    for (int i = 0; i < linkAnalysisMessages.Count; ++i)
                        if (linkAnalysisMessages[i].Equals(intermes))
                        {
                            linkAnalysisMessages.RemoveAt(i);
                            linkAnalysisMessages.Insert(i, message);
                            break;
                        }
                }
                else linkAnalysisMessages.Add(message);
                existMessages[key] = message;
            }
        }

        public void RemoveMessage(AbstractAnalyser me, string key)
        {
            lock (operationLock)
            {
                if (me != null && !CheckPermission(me)) return;
                if (existMessages.ContainsKey(key))
                {
                    linkAnalysisMessages.Remove(existMessages[key]);
                    existMessages.Remove(key);
                }
            }
        }

        public void SetThreadLayoutVisibility(AbstractAnalyser me, bool visible)
        {
            lock (operationLock)
            {
                if (me != null && !CheckPermission(me)) return;
                if (visible) threadLayoutVisibilityManager.Show();
                else threadLayoutVisibilityManager.Hide();
            }
        }

        public void SetComboBoxLayoutVisibility(AbstractAnalyser me, bool visible)
        {
            lock (operationLock)
            {
                if (me != null && !CheckPermission(me)) return;
                if (visible) comboBoxLayoutVisibilityManager.Show();
                else comboBoxLayoutVisibilityManager.Hide();
            }
        }

        public void SetSubmitButtonEnabled(AbstractAnalyser me, bool enable)
        {
            lock (operationLock)
            {
                if (me != null && !CheckPermission(me)) return;
                if (enable == submitButton.IsEnabled) return;
                submitButton.IsEnabled = enable;
            }
        }

        public void SetRecommendedName(AbstractAnalyser me, string name, double opacity)
        {
            lock (operationLock)
            {
                if (me != null && !CheckPermission(me)) return;
                recommendedNameBlock.Text = name;
                recommendedNameBlock.Opacity = opacity;
            }
        }
    }
}
