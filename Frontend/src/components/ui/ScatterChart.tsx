import { formatDateToShort } from "@/utils/time";
import ReactECharts from "echarts-for-react";

export function ScatterChart({ data, height = 300 }: any) {
  if (!data?.values || data.values.length === 0) {
    return <div>No data</div>;
  }

  //  Safe formatter wrapper
  const safeFormat = (input: any) => {
    if (!input) return "NA";
    const d = new Date(input);
    if (isNaN(d.getTime())) return "NA";
    return formatDateToShort(d.toDateString());
  };

  //  If endTime missing → calculate (start + 6 days)
  const getEndDate = (start: any, end: any) => {
    if (end) return end;
    const d = new Date(start);
    if (isNaN(d.getTime())) return null;
    d.setDate(d.getDate() + 6);
    return d;
  };

  //  Values
  const values = data.values
    .map((e: any) => Number(e.value))
    .filter((v: number) => !isNaN(v));

  const min = values.length ? Math.min(...values) : 0;
  const max = values.length ? Math.max(...values) : 0;

  const minWithMargin = min - (min * 0.02);
  const maxWithMargin = max + (max * 0.02);

  //  Stacks
  const stacks = [
    ...new Set(data.values.map((d: any) => d.assetname)),
  ];

  //  Weeks sorted
  const weeks = [
    ...new Set(data.values.map((d: any) => d.weekNumber)),
  ].sort((a: number, b: number) => a - b);

  //  Map week -> label
  const weekLabelMap = new Map<number, string>();

  data.values.forEach((d: any) => {
    if (!weekLabelMap.has(d.weekNumber)) {
      const start = safeFormat(d.startTime);
      const end = safeFormat(getEndDate(d.startTime, d.endTime));

      const label = `W-${d.weekNumber} (${start} - ${end})`;
      weekLabelMap.set(d.weekNumber, label);
    }
  });

  //  Series
  const series = weeks.map((week: number) => {
    const weekData = data.values.filter(
      (d: any) => d.weekNumber === week
    );

    const formattedData = stacks.map((stack: string, index: number) => {
      const point = weekData.find((d: any) => d.assetname === stack);
      return [index, point ? Number(point.value) : null];
    });

    return {
      name: weekLabelMap.get(week) || `Week ${week}`, //  MUST match legend
      type: "scatter",
      data: formattedData,
    };
  });

  const option = {
    xAxis: {
      type: "category",
      name: "Stacks",
      data: stacks,
    },
    yAxis: {
      type: "value",
      name: "KPI Value",
      min: minWithMargin.toFixed(1),
      max: maxWithMargin.toFixed(1),
      scale: true,
    },
    tooltip: {
      trigger: "item",
    },
    legend: {
      data: Array.from(weekLabelMap.values()),
    },
    series: series,
  };

  return (
    <div>
      <ReactECharts option={option} style={{ height }} />
    </div>
  );
}