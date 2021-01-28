using System;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace TX.Controls
{
    public class WrapStackPanel : StackPanel
    {
        protected override Size MeasureOverride(Size availableSize)
        {
            double horizontalSpace = (Margin.Left + Margin.Right + Padding.Left + Padding.Right);
            double verticalSpace = (Margin.Top + Margin.Bottom + Padding.Top + Padding.Bottom);

            availableSize.Width -= horizontalSpace;
            availableSize.Height -= verticalSpace;
            availableSize.Width = Math.Max(availableSize.Width, 0.0);
            availableSize.Height = Math.Max(availableSize.Height, 0.0);

            if (Orientation == Orientation.Horizontal)
            {
                double heightOfTopRows = 0.0f;
                double thisRowHeight = 0.0f;
                double thisRowWidth = 0.0f;
                double maximumRowWidth = 0.0f;
                foreach (UIElement child in Children)
                {
                    var childAvailableSize = new Size(availableSize.Width,
                        availableSize.Height - heightOfTopRows - thisRowHeight);
                    child.Measure(childAvailableSize);
                    var childDesiredSize = child.DesiredSize;
                    if (childDesiredSize.Width > availableSize.Width - thisRowWidth)
                    {
                        maximumRowWidth = Math.Max(thisRowWidth, maximumRowWidth);
                        // place child to next row
                        heightOfTopRows += thisRowHeight;
                        thisRowHeight = childDesiredSize.Height;
                        thisRowWidth = childDesiredSize.Width;
                    }
                    else
                    {
                        // append child to the end
                        thisRowHeight = Math.Max(thisRowHeight, childDesiredSize.Height);
                        thisRowWidth += childDesiredSize.Width;
                    }
                }
                // take width of last row into account
                maximumRowWidth = Math.Max(thisRowWidth, maximumRowWidth);
                return new Size(maximumRowWidth + horizontalSpace, heightOfTopRows + thisRowHeight + verticalSpace);
            }
            else if (Orientation == Orientation.Vertical)
            {
                double widthOfLeftColumns = 0.0f;
                double thisColumnWidth = 0.0f;
                double thisColumnHeight = 0.0f;
                double maximumColumnHeight = 0.0f;
                foreach (UIElement child in Children)
                {
                    var childAvailableSize = new Size(availableSize.Height,
                        availableSize.Width - widthOfLeftColumns - thisColumnWidth);
                    child.Measure(childAvailableSize);
                    var childDesiredSize = child.DesiredSize;
                    if (childDesiredSize.Height > availableSize.Height - thisColumnHeight)
                    {
                        maximumColumnHeight = Math.Max(thisColumnHeight, maximumColumnHeight);
                        // place child to next column
                        widthOfLeftColumns += thisColumnWidth;
                        thisColumnWidth = childDesiredSize.Width;
                        thisColumnHeight = childDesiredSize.Height;
                    }
                    else
                    {
                        // append child to the end
                        thisColumnWidth = Math.Max(thisColumnWidth, childDesiredSize.Width);
                        thisColumnHeight += childDesiredSize.Height;
                    }
                }
                // take height of last column into account
                maximumColumnHeight = Math.Max(thisColumnHeight, maximumColumnHeight);
                return new Size(widthOfLeftColumns + thisColumnWidth + horizontalSpace, 
                    maximumColumnHeight + verticalSpace);
            }
            else return Size.Empty;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            finalSize.Width -= (Margin.Left + Margin.Right + Padding.Left + Padding.Right);
            finalSize.Height -= (Margin.Top + Margin.Bottom + Padding.Top + Padding.Bottom);
            finalSize.Width = Math.Max(finalSize.Width, 0.0);
            finalSize.Height = Math.Max(finalSize.Height, 0.0);
            double leftOffset = Padding.Left;
            double topOffset = Padding.Top;

            if (Orientation == Orientation.Horizontal)
            {
                double heightOfTopRows = 0.0f;
                double thisRowHeight = 0.0f;
                double thisRowWidth = 0.0f;
                foreach (UIElement child in Children)
                {
                    var childDesiredSize = child.DesiredSize;
                    if (childDesiredSize.Width > finalSize.Width - thisRowWidth)
                    {
                        // place child to next row
                        heightOfTopRows += thisRowHeight;
                        thisRowHeight = childDesiredSize.Height;
                        thisRowWidth = childDesiredSize.Width;
                    }
                    else
                    {
                        // append child to the end
                        thisRowHeight = Math.Max(thisRowHeight, childDesiredSize.Height);
                        thisRowWidth += childDesiredSize.Width;
                    }
                    Point anchorPoint = new Point(
                        leftOffset + thisRowWidth - childDesiredSize.Width, 
                        topOffset + heightOfTopRows
                    );
                    child.Arrange(new Rect(anchorPoint, childDesiredSize));
                }
                return finalSize;
            }
            else if (Orientation == Orientation.Vertical)
            {
                double widthOfLeftColumns = 0.0f;
                double thisColumnWidth = 0.0f;
                double thisColumnHeight = 0.0f;
                foreach (UIElement child in Children)
                {
                    var childDesiredSize = child.DesiredSize;
                    if (childDesiredSize.Height > finalSize.Height - thisColumnHeight)
                    {
                        // place child to next column
                        widthOfLeftColumns += thisColumnWidth;
                        thisColumnWidth = childDesiredSize.Width;
                        thisColumnHeight = childDesiredSize.Height;
                    }
                    else
                    {
                        // append child to the end
                        thisColumnWidth = Math.Max(thisColumnWidth, childDesiredSize.Width);
                        thisColumnHeight += childDesiredSize.Height;
                    }
                    Point anchorPoint = new Point(
                        leftOffset + widthOfLeftColumns, 
                        topOffset + thisColumnHeight - childDesiredSize.Height
                    );
                    child.Arrange(new Rect(anchorPoint, childDesiredSize));
                }
                return finalSize;
            }
            else return finalSize;
        }
    }
}
