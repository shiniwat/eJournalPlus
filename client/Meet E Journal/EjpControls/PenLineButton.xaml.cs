using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SiliconStudio.Meet.EjpControls
{
    /// <summary>
    /// Interaction logic for PenLineButton.xaml
    /// </summary>
    public partial class PenLineButton : UserControl
    {
        private Dictionary<string, SolidColorBrush> _items;

        public event ActiveColorChanged ActiveTextColorChanged;
        public event SwitchButtonUnChecked OnSwitchButtonUnChecked;

        public bool IsActive
        {
            get { return (bool)this._b_toolButton.IsChecked; }
        }

        public Dictionary<string, SolidColorBrush> Items
        {
            get { return _items; }
            set
            {
                if (value.Count == 0)
                    throw new
                        ArgumentException("Pen Line Button: Attempt to set Items collection to zero-length list. This is not supported.");
                else
                {
                    _items = value;

                    this._cb_AvailableBrushes.Items.Clear();

                    foreach (KeyValuePair<string, SolidColorBrush> kv in value)
                    {
                        //Grid g = new Grid { Width = 55 };
                        Grid g = new Grid();
                        ColumnDefinition d1 = new ColumnDefinition();
                        d1.Width = new GridLength(32d);
                        ColumnDefinition d2 = new ColumnDefinition();
                        d2.Width = new GridLength(0, GridUnitType.Star);
                        g.ColumnDefinitions.Add(d1);
                        g.ColumnDefinitions.Add(d2);

                        Rectangle r = new Rectangle();
                        r.Width = 32;
                        r.Height = 20;
                        r.Fill = kv.Value;
                        r.Margin = new Thickness(2, 4, 2, 4);
                        r.HorizontalAlignment = HorizontalAlignment.Left;
                        r.MouseLeftButtonUp += new MouseButtonEventHandler(this.ColorIconClicked);
                        Grid.SetColumn(r, 0);

                        Label l = new Label();
                        l.Padding = new Thickness(0);
                        l.Margin = new Thickness(4, 4, 6, 4);
                        l.HorizontalAlignment = HorizontalAlignment.Right;
                        l.VerticalAlignment = VerticalAlignment.Center;
                        l.Content = kv.Key;
                        l.FontWeight = FontWeights.Bold;
                        Grid.SetColumn(l, 1);

                        g.Children.Add(r);
                        g.Children.Add(l);
                        g.MouseLeftButtonUp += new MouseButtonEventHandler(this.ColorIconClicked);
                        this._cb_AvailableBrushes.Items.Add(g);
                    }
                }
            }
        }

        public PenLineButton()
        {
            InitializeComponent();
            SolidColorBrush def = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));
            this._r_CurrentColor.Fill = def;
            this._r_CurrentColor.Tag = def;
            this.IsEnabledChanged += new DependencyPropertyChangedEventHandler(PenLineButton_IsEnabledChanged);
        }

        public void SetCurrentColor(SolidColorBrush color)
        {
            this._r_CurrentColor.Fill = color;
            this._r_CurrentColor.Tag = color;
        }

        public SolidColorBrush GetCurrentColor()
        {
            return this._r_CurrentColor.Tag as SolidColorBrush;
        }

        public void Depress()
        {
            this._b_toolButton.IsChecked = false;
        }

        public void Press()
        {
            this._b_toolButton.IsChecked = true;
        }

        private void PenLineButton_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ToggleButton tb = (ToggleButton)
                 this._cb_AvailableBrushes.Template.FindName(
                 "tgDlButton", this._cb_AvailableBrushes);
            if (tb != null)
            {
                Rectangle bg = (Rectangle)tb.Template.FindName("bgSquare", tb);
                if ((bool)e.NewValue == false)
                    bg.Fill = (DrawingBrush)this.Resources["bgClear_off"];
                else if ((bool)e.NewValue == true)
                {
                    if((bool)this._b_toolButton.IsChecked == true)
                        bg.Fill = (DrawingBrush)this.Resources["bgClear_pressed"];
                    else
                        bg.Fill = (DrawingBrush)this.Resources["bgClear"];
                }
            }
        }

        private void UnCheckControlParts(object sender, RoutedEventArgs e)
        {
            ToggleButton tb = (ToggleButton)
                             this._cb_AvailableBrushes.Template.FindName(
                             "tgDlButton", this._cb_AvailableBrushes);
            if (tb != null)
            {
                Rectangle bg = (Rectangle)tb.Template.FindName("bgSquare", tb);
                bg.Fill = (DrawingBrush)this.Resources["bgClear"];
            }

            if (this.OnSwitchButtonUnChecked != null)
                this.OnSwitchButtonUnChecked();
        }

        private void SetActiveColor(object sender, RoutedEventArgs e)
        {
            //this._b_toolButton.IsHitTestVisible = false;

            SolidColorBrush b = this._r_CurrentColor.Tag as SolidColorBrush;
            if (b != null)
                this.InvokeColorChangedEvent(b);
            else
                this.InvokeColorChangedEvent(Brushes.Black);

            ToggleButton tb = (ToggleButton)
                             this._cb_AvailableBrushes.Template.FindName(
                             "tgDlButton", this._cb_AvailableBrushes);
            if (tb != null)
            {
                Rectangle bg = (Rectangle)tb.Template.FindName("bgSquare", tb);
                bg.Fill = (DrawingBrush)this.Resources["bgClear_pressed"];
            }
        }

        private void SelectedColorChanged
            (object sender, SelectionChangedEventArgs e)
        {
            Grid g = e.AddedItems[0] as Grid;
            Rectangle r = g.Children[0] as Rectangle;
            this._r_CurrentColor.Fill = r.Fill;
            this._r_CurrentColor.Tag = r.Fill;
            this.InvokeColorChangedEvent(r.Fill as SolidColorBrush);

            this._b_toolButton.IsChecked = true;
        }

        private void ColorIconClicked(object sender, RoutedEventArgs e)
        {
            Grid g = new Grid();
            Rectangle r = new Rectangle();
            
            if(sender is Grid)
            {
                g = sender as Grid;
                r = g.Children[0] as Rectangle;
            }
            else if ( sender is Rectangle)
                r = sender as Rectangle;

            this._r_CurrentColor.Fill = r.Fill;
            this._r_CurrentColor.Tag = r.Fill;
            this.InvokeColorChangedEvent(r.Fill as SolidColorBrush);

            this._b_toolButton.IsChecked = true;
        }

        private void InvokeColorChangedEvent(SolidColorBrush newColor)
        {
            if (this.ActiveTextColorChanged != null)
                this.ActiveTextColorChanged.Invoke(this._r_CurrentColor.Tag as SolidColorBrush);
        }
    }
}
