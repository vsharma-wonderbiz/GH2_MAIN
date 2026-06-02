import ReactECharts from "echarts-for-react";

interface KpiItem {
  kpiName: string;
  kpiValue: number;
  level: string;
}

interface KpiBarChartProps {
  data: KpiItem[];
  color?: string;
  height?: number;
}

export default function KpiBarChart({
  data,
  color = "#3b82f6",
  height = 300,
}: KpiBarChartProps) {

  const names  = data.map((k) => k.kpiName.replaceAll("_", " "));

  const values = data.map((k) => parseFloat(k.kpiValue.toFixed(2)));

  const option = {
    tooltip: {
      trigger: "axis",
      axisPointer: { type: "shadow" },
      backgroundColor: "#1f2937",
      borderColor: "#374151",
      borderWidth: 1,
      textStyle: { color: "#f9fafb", fontSize: 12 },
      formatter: (params: any[]) => {
        const p = params[0];
        return `<div style="font-size:11px;color:#9ca3af;margin-bottom:4px">${p.name}</div>
                <div style="font-weight:600">${p.value}</div>`;
      },
    },
    grid: { left: 12, right: 12, top: 12, bottom: 20, containLabel: true },
    xAxis: {
      type: "category",
      data: names,
      axisLine:  { show: false },
      axisTick:  { show: false },
      axisLabel: {
        color: "#9ca3af",
        fontSize: 11,
        rotate: 0,
        interval: 0,
        overflow: "None",
        width: 10,
      },
      splitLine: { show: false },
    },
    yAxis: {
      type: "value",
      axisLine:  { show: false },
      axisTick:  { show: false },
      axisLabel: { color: "#9ca3af", fontSize: 11 },
      splitLine: { lineStyle: { color: "#e5e7eb", type: "dashed" } },
    },
    series: [
      {
        type: "bar",
        data: values,
        barMaxWidth: 48,
        itemStyle: {
          color,
          borderRadius: [6, 6, 0, 0],
          opacity: 0.88,
        },
        emphasis: {
          itemStyle: { opacity: 1 },
        },
      },
    ],
  };

  return (
    <ReactECharts
      option={option}
      style={{ height: `${height}px`, width: "100%" }}
      notMerge
      lazyUpdate
    />
  );
}