import React from "react";
import ReactECharts from "echarts-for-react";

interface SingleSeries {
  time: string;
  value: number;
  stackName?: string;
}

interface Props {
  chartData: SingleSeries[];
  normalized?: boolean;
}

const COLORS = ["#3b82f6", "#f97316", "#22c55e", "#a855f7", "#ef4444"];

const SignalChart: React.FC<Props> = ({ chartData, normalized = false }) => {
  if (!chartData || chartData.length === 0) return null;

  // Detect if multi-series (compare mode) by checking distinct stackNames
  const stackNames = [...new Set(chartData.map((d) => d.stackName).filter(Boolean))];
  const isMultiSeries = stackNames.length > 1;

  let series: any[];
  let xAxisData: string[];

  if (isMultiSeries) {
    // Build a map: time -> { [stackName]: value }
    const timeSet = new Set(chartData.map((d) => d.time));
    xAxisData = [...timeSet].sort();

    series = stackNames.map((name, i) => {
      const seriesData = chartData.filter((d) => d.stackName === name);
      const dataMap = Object.fromEntries(seriesData.map((d) => [d.time, d.value]));
      return {
        name,
        type: "line",
        smooth: true,
        showSymbol: false,
        sampling: "lttb",
        color: COLORS[i % COLORS.length],
        data: xAxisData.map((t) => dataMap[t] ?? null),
      };
    });
  } else {
    // Single series (normal mode)
    xAxisData = chartData.map((d) => d.time);
    const values = chartData.map((d) => d.value);
    const min = Math.min(...values);
    const max = Math.max(...values);
    series = [
      {
        data: values,
        type: "line",
        smooth: true,
        showSymbol: false,
        sampling: "lttb",
        color: COLORS[0],
        areaStyle: { opacity: 0.08 },
      },
    ];
  }

  const option = {
    tooltip: { trigger: "axis" },
    legend: isMultiSeries ? { bottom: 0 } : undefined,
    grid: {
      left: "3%", right: "3%", top: "5%",
      bottom: isMultiSeries ? "15%" : "10%",
      containLabel: true,
    },
    xAxis: { type: "category", data: xAxisData },
    yAxis: { type: "value", scale: true },
    dataZoom: [{ type: "inside" }, { type: "slider" }],
    series,
  };

  return <ReactECharts option={option} style={{ width: "100%", height: "100%" }} />;
};

export default SignalChart;