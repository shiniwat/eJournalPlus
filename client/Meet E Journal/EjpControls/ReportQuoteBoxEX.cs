using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SiliconStudio.Meet.EjpControls
{
    public class ReportQuoteBoxEX : Control
    {
        public static readonly DependencyProperty QuoteContentProperty;
        public static readonly DependencyProperty QuoteTitleProperty;
        public static readonly DependencyProperty FillProperty;

        public string QuoteContent
        {
            get { return (string)GetValue(QuoteContentProperty); }
            set { SetValue(QuoteContentProperty, value); }
        }

        public string QuoteTitle
        {
            get { return (string)GetValue(QuoteTitleProperty); }
            set { SetValue(QuoteTitleProperty, value); }
        }
       
        public Brush Fill
        {
            get { return (Brush)GetValue(FillProperty); }
            set { SetValue(FillProperty, value); }
        }

        static ReportQuoteBoxEX()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(ReportQuoteBoxEX), new FrameworkPropertyMetadata(
                    typeof(ReportQuoteBoxEX)));

            ReportQuoteBoxEX.QuoteContentProperty =
                DependencyProperty.Register("QuoteContent",
                typeof(string),
                typeof(ReportQuoteBoxEX),
                new UIPropertyMetadata(string.Empty, new PropertyChangedCallback(QuoteChanged)));

            ReportQuoteBoxEX.QuoteTitleProperty =
                DependencyProperty.Register("QuoteTitle",
                typeof(string),
                typeof(ReportQuoteBoxEX),
                new UIPropertyMetadata(string.Empty, new PropertyChangedCallback(TitleChanged)));

            ReportQuoteBoxEX.FillProperty =
                DependencyProperty.Register("Fill",
                typeof(Brush), 
                typeof(ReportQuoteBoxEX),
                new UIPropertyMetadata(Brushes.Red, new PropertyChangedCallback(FillChanged)));

        }

        static void QuoteChanged(DependencyObject property, DependencyPropertyChangedEventArgs args)
        {
            
        }

        static void TitleChanged(DependencyObject property, DependencyPropertyChangedEventArgs args)
        {

        }

        static void FillChanged(DependencyObject property, DependencyPropertyChangedEventArgs args)
        {

        }
    }
}
