using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using ClientVodenko.Models;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

public class HistoryChartViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private ISeries[] _chartSeries = [];
    public ISeries[] ChartSeries
    {
        get => _chartSeries;
        set { _chartSeries = value; PropertyChanged?.Invoke(this, new(nameof(ChartSeries))); }
    }

    public Axis[] XAxes { get; set; } = new[]
    {
        new Axis
        {
            Name = "Time",
            NamePaint = new SolidColorPaint(SKColors.Gray),
            LabelsPaint = new SolidColorPaint(SKColors.Gray),
            TextSize = 11,
            Labeler = value => new DateTime((long)value).ToString("HH:mm:ss")
        }
    };

    public Axis[] YAxes { get; set; } = new[]
    {
        new Axis
        {
            Name = "Value",
            NamePaint = new SolidColorPaint(SKColors.Gray),
            LabelsPaint = new SolidColorPaint(SKColors.Gray),
            TextSize = 11
        }
    };

    public void LoadData(List<VodenkoDto> podaci)
    {
        // linija za Valve Position
        var valveValues = podaci.Select(p => new DateTimePoint(p.Time_Saved, (double)p.Valve_Position));

        // linija za Level Setpoint
        var levelValues = podaci.Select(p => new DateTimePoint(p.Time_Saved, (double)p.Actual_Level));

        ChartSeries = new ISeries[]
        {
            new LineSeries<DateTimePoint>
            {
                Name = "Valve Position",
                Values = new ObservableCollection<DateTimePoint>(valveValues),
                Stroke = new SolidColorPaint(SKColor.Parse("#00BFFF")) { StrokeThickness = 2 },
                Fill = null,
                GeometrySize = 0,  // bez točkica na liniji
            },
            new LineSeries<DateTimePoint>
            {
                Name = "Water level",
                Values = new ObservableCollection<DateTimePoint>(levelValues),
                Stroke = new SolidColorPaint(SKColor.Parse("#FF4444")) { StrokeThickness = 2 },
                Fill = null,
                GeometrySize = 0,
            }
        };
    }
}