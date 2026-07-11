using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using CamZKeeper.Core.Models;

namespace CamZKeeper.Desktop.Controls;

public partial class PropertyControl : System.Windows.Controls.UserControl
{
    public PropertyControl()
    {
        InitializeComponent();

        ValueSlider.ValueChanged += ValueSlider_ValueChanged;

        AutoCheckBox.Checked += AutoCheckBox_Changed;
        AutoCheckBox.Unchecked += AutoCheckBox_Changed;
    }

    public static readonly DependencyProperty DisplayNameProperty =
        DependencyProperty.Register(
            nameof(DisplayName),
            typeof(string),
            typeof(PropertyControl),
            new PropertyMetadata(string.Empty, OnPropertyChanged));

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(
            nameof(Value),
            typeof(int),
            typeof(PropertyControl),
            new PropertyMetadata(0, OnPropertyChanged));

    public static readonly DependencyProperty MinimumProperty =
        DependencyProperty.Register(
            nameof(Minimum),
            typeof(int),
            typeof(PropertyControl),
            new PropertyMetadata(0, OnPropertyChanged));

    public static readonly DependencyProperty MaximumProperty =
        DependencyProperty.Register(
            nameof(Maximum),
            typeof(int),
            typeof(PropertyControl),
            new PropertyMetadata(100, OnPropertyChanged));

    public static readonly DependencyProperty StepProperty =
        DependencyProperty.Register(
            nameof(Step),
            typeof(int),
            typeof(PropertyControl),
            new PropertyMetadata(1, OnPropertyChanged));

    public static readonly DependencyProperty SupportsAutoProperty =
        DependencyProperty.Register(
            nameof(SupportsAuto),
            typeof(bool),
            typeof(PropertyControl),
            new PropertyMetadata(false, OnPropertyChanged));

    public static readonly DependencyProperty IsAutoProperty =
        DependencyProperty.Register(
            nameof(IsAuto),
            typeof(bool),
            typeof(PropertyControl),
            new PropertyMetadata(false, OnPropertyChanged));

    public static readonly DependencyProperty IsDirtyProperty =
        DependencyProperty.Register(
            nameof(IsDirty),
            typeof(bool),
            typeof(PropertyControl),
            new PropertyMetadata(false, OnPropertyChanged));

    public string DisplayName
    {
        get => (string)GetValue(DisplayNameProperty);
        set => SetValue(DisplayNameProperty, value);
    }

    public int Value
    {
        get => (int)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public int Minimum
    {
        get => (int)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public int Maximum
    {
        get => (int)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public int Step
    {
        get => (int)GetValue(StepProperty);
        set => SetValue(StepProperty, value);
    }

    public bool SupportsAuto
    {
        get => (bool)GetValue(SupportsAutoProperty);
        set => SetValue(SupportsAutoProperty, value);
    }

    public bool IsAuto
    {
        get => (bool)GetValue(IsAutoProperty);
        set => SetValue(IsAutoProperty, value);
    }

    public bool IsDirty
    {
        get => (bool)GetValue(IsDirtyProperty);
        set => SetValue(IsDirtyProperty, value);
    }

    private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((PropertyControl)d).Refresh();
    }

    private bool _updating;

    private void Refresh()
    {
        _updating = true;

        PropertyName.Text = DisplayName;

        ValueSlider.Minimum = Minimum;
        ValueSlider.Maximum = Maximum;
        ValueSlider.Value = Value;

        var step = Step > 0 ? Step : 1;

        ValueSlider.TickFrequency = step;
        ValueSlider.SmallChange = step;
        ValueSlider.LargeChange = step;

        ValueText.Text = Value.ToString();

        AutoCheckBox.Visibility =
            SupportsAuto
                ? Visibility.Visible
                : Visibility.Collapsed;

        AutoCheckBox.IsChecked = IsAuto;

        ValueSlider.IsEnabled = !IsAuto;

        UnsavedIndicator.Visibility =
            IsDirty
                ? Visibility.Visible
                : Visibility.Collapsed;

        _updating = false;
    }

    public event EventHandler<int>? ValueChanged;

    public event EventHandler<bool>? AutoChanged;

    private void ValueSlider_ValueChanged(
        object sender,
        RoutedPropertyChangedEventArgs<double> e)
    {
        if (_updating)
            return;

        Value = (int)e.NewValue;

        ValueText.Text = Value.ToString();

        IsDirty = true;

        ValueChanged?.Invoke(this, Value);
    }

    private void AutoCheckBox_Changed(
        object sender,
        RoutedEventArgs e)
    {
        if (_updating)
            return;

        var requestedAuto = AutoCheckBox.IsChecked == true;

        AutoChanged?.Invoke(this, requestedAuto);
    }

    public void ApplyAutoResult(bool isAuto, int value)
    {
        Value = value;
        IsAuto = isAuto;
        IsDirty = true;
    }

    public UvcSetting? Setting { get; private set; }

    public void Bind(UvcSetting setting)
    {
        Setting = setting;

        DisplayName = setting.Name;
        Value = setting.Value;
        Minimum = setting.Minimum;
        Maximum = setting.Maximum;
        Step = setting.Step;
        SupportsAuto = setting.SupportsAuto;
        IsAuto = setting.IsAuto;
        IsDirty = setting.IsDirty;

        Refresh();
    }
}