import React from "react";
import ReactECharts from "echarts-for-react";
import { type StackEfficiency } from "@/api/analyticsApi";

const ACTUAL_COLOR = "#3b82f6";   // solid line
const PREDICTED_COLOR = "#f97316"; // dashed line

const EfficiencyChart: React.FC<StackEfficiency> = ({ actual, predicted }) => {
  if (!actual || actual.length === 0) return null;

  // xAxis ab "value" type hai (category nahi), isliye data ko
  // [hour, efficiency] pairs ki tarah dena hoga - taaki x-position
  // asli operationalHour number ke hisaab se proportionally place ho.
  // Isse dono series (actual + predicted) ko same numeric scale pe
  // sahi equal spacing milegi, chahe hour-gaps uneven hi kyu na hon.

  const actualData: [number, number | null][] = [
    ...actual.map((d) => [d.operationalHour, d.efficiency] as [number, number]),
    ...predicted.map((d) => [d.operationalHour, null] as [number, null]),
  ];

  // predictedData: actual segment ke liye null, phir real predicted values.
  // Last actual index pe explicitly actual ka last point set kar diya -
  // yehi shared point solid aur dashed line ko bina gap ke jodta hai.
  const predictedData: [number, number | null][] = [
    ...actual.map((d) => [d.operationalHour, null] as [number, null]),
    ...predicted.map((d) => [d.operationalHour, d.efficiency] as [number, number]),
  ];
  predictedData[actual.length - 1] = [
    actual[actual.length - 1].operationalHour,
    actual[actual.length - 1].efficiency,
  ];

  const allHours = [
    ...actual.map((d) => d.operationalHour),
    ...predicted.map((d) => d.operationalHour),
  ];

  const option = {
    tooltip: { trigger: "axis" },
    legend: { bottom: 0 },
    grid: {
      left: "3%",
      right: "3%",
      top: "5%",
      bottom: "15%",
      containLabel: true,
    },
    xAxis: {
      type: "value",              // category -> value: ab true numeric spacing
      name: "Operational Hours",
      nameLocation: "middle",
      nameGap: 30,
      min: allHours[0],
      max: allHours[allHours.length - 1],
    },
    yAxis: {
      type: "value",
      scale: true,
      name: "Efficiency (%)",
      nameLocation: "middle",
      nameGap: 40,
      // agar efficiency range fixed pata hai (jaise 60-80%), to
      // explicit min/max de sakta hai for extra clean slope:
      // min: 55,
      // max: 85,
    },
    dataZoom: [{ type: "inside" }, { type: "slider" }],
    series: [
      {
        name: "Actual",
        type: "line",
        smooth: false,           // seedhi line - koi curve nahi
        showSymbol: false,
        sampling: "lttb",
        color: ACTUAL_COLOR,
        lineStyle: { width: 3 },
        connectNulls: false,
        data: actualData,
      },
      {
        name: "Predicted",
        type: "line",
        smooth: false,           // ye pehle se seedhi thi
        showSymbol: false,
        sampling: "lttb",
        color: PREDICTED_COLOR,
        connectNulls: false,
        lineStyle: { type: "dashed", width: 3 },
        data: predictedData,
      },
    ],
  };

  return <ReactECharts option={option} style={{ width: "100%", height: "100%" }} />;
};

export default EfficiencyChart;