using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace TX.VisualManager
{
    /// <summary>
    /// 用于辅助完成控件在可见、不可见之间切换
    /// 并播放出现、消失动画
    /// </summary>
    public class VisibilityAnimationManager : PropertyAnimationManager
    {
        private bool hideAnimatePlaying = false;
        private bool showAnimatePlaying = false;

        private Storyboard ShowElementStoryboard
        {
            get
            {
                if (showElementStoryboard != null) return showElementStoryboard;

                var storyboard = new Storyboard();
                var duration = new Duration(TimeSpan.FromMilliseconds(AnimationDuration));
                var opacityAnimation = new DoubleAnimation() { Duration = duration, From = 0, To = 1 };
                var scaleAnimationX = new DoubleAnimation() { Duration = duration, From = scaleValue, To = 1 };
                var scaleAnimationY = new DoubleAnimation() { Duration = duration, From = scaleValue, To = 1 };

                Storyboard.SetTarget(opacityAnimation, element);
                Storyboard.SetTargetProperty(opacityAnimation, "Opacity");
                Storyboard.SetTarget(scaleAnimationX, element);
                Storyboard.SetTargetProperty(scaleAnimationX, "(UIElement.RenderTransform).(CompositeTransform.ScaleX)");
                Storyboard.SetTarget(scaleAnimationY, element);
                Storyboard.SetTargetProperty(scaleAnimationY, "(UIElement.RenderTransform).(CompositeTransform.ScaleY)");

                storyboard.Children.Add(opacityAnimation);
                storyboard.Children.Add(scaleAnimationX);
                storyboard.Children.Add(scaleAnimationY);
                storyboard.Completed += (o, e) => 
                {
                    element.Visibility = Visibility.Visible;
                    ShowAnimationCompleted?.Invoke(o, e);
                    showAnimatePlaying = false;
                };

                return showElementStoryboard = storyboard;
            }
        }
        private Storyboard showElementStoryboard = null;

        private Storyboard HideElementStoryboard
        {
            get
            {
                if (hideElementStoryboard != null) return hideElementStoryboard;

                var storyboard = new Storyboard();
                var duration = new Duration(TimeSpan.FromMilliseconds(AnimationDuration));
                var opacityAnimation = new DoubleAnimation() { Duration = duration, From = 1, To = 0 };
                var scaleAnimationX = new DoubleAnimation() { Duration = duration, From = 1, To = scaleValue };
                var scaleAnimationY = new DoubleAnimation() { Duration = duration, From = 1, To = scaleValue };

                Storyboard.SetTarget(opacityAnimation, element);
                Storyboard.SetTargetProperty(opacityAnimation, "Opacity");
                Storyboard.SetTarget(scaleAnimationX, element);
                Storyboard.SetTargetProperty(scaleAnimationX, "(UIElement.RenderTransform).(CompositeTransform.ScaleX)");
                Storyboard.SetTarget(scaleAnimationY, element);
                Storyboard.SetTargetProperty(scaleAnimationY, "(UIElement.RenderTransform).(CompositeTransform.ScaleY)");

                storyboard.Children.Add(opacityAnimation);
                storyboard.Children.Add(scaleAnimationX);
                storyboard.Children.Add(scaleAnimationY);
                storyboard.Completed += (o, e) => 
                {
                    element.Visibility = Visibility.Collapsed;
                    HideAnimationCompleted?.Invoke(o, e);
                    hideAnimatePlaying = false;
                };

                return hideElementStoryboard = storyboard;
            }
        }
        private Storyboard hideElementStoryboard = null;

        /// <summary>
        /// 获取当前控制的元素
        /// </summary>
        public UIElement Element
        {
            get { return element; }
        }
        private UIElement element;

        /// <summary>
        /// 设置控件动画中缩放的执行程度，不得小于0或大于1
        /// </summary>
        public float ScaleValue
        {
            get { return scaleValue; }
            set
            {
                if (value > 0 && value <= 1)
                {
                    scaleValue = value;
                    showElementStoryboard = hideElementStoryboard = null;
                }
            }
        }
        private float scaleValue;

        /// <summary>
        /// 显示和隐藏动画的时间
        /// </summary>
        public double AnimationDuration
        {
            get { return animationDuration; }
            set
            {
                if (value > 0 && value <= 100000)
                {
                    animationDuration = value;
                    showElementStoryboard = hideElementStoryboard = null;
                }
            }
        }
        private double animationDuration;

        /// <summary>
        /// 显示动画结束
        /// </summary>
        public event EventHandler<object> ShowAnimationCompleted;

        /// <summary>
        /// 显示动画开始
        /// </summary>
        public event Action ShowAnimationBeginning;

        /// <summary>
        /// 隐藏动画结束
        /// </summary>
        public event EventHandler<object> HideAnimationCompleted;

        /// <summary>
        /// 隐藏动画开始
        /// </summary>
        public event Action HideAnimationBeginning;

        public VisibilityAnimationManager(
            UIElement element,
            float scaleValue = 0.85f,
            double duration = 200)
        {
            this.element = element;
            element.RenderTransform = new CompositeTransform();
            ScaleValue = scaleValue;
            AnimationDuration = duration;
        }

        /// <summary>
        /// 播放显示动画并将Visibility置为Visible
        /// </summary>
        public void Show()
        {
            if (element.Visibility == Visibility.Visible || showAnimatePlaying) return;
            ShowAnimationBeginning?.Invoke();
            showAnimatePlaying = true;
            ShowElementStoryboard.Begin();
        }

        /// <summary>
        /// 播放隐藏动画并将Visibility置为Collapsed
        /// </summary>
        public void Hide()
        {
            if (element.Visibility == Visibility.Collapsed || hideAnimatePlaying) return;
            HideAnimationBeginning?.Invoke();
            hideAnimatePlaying = true;
            HideElementStoryboard.Begin();
        }
    }
}
