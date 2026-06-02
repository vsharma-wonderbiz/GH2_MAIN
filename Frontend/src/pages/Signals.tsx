import React, { useState, useEffect } from "react";
import DatePicker from "react-datepicker";
import "react-datepicker/dist/react-datepicker.css";
import SignalChart from "@/components/ui/SignalChart";
import { SignalMultiSelect } from "@/components/ui/SignalMultiSelect";

import {
  getPlants,
  getStacksByPlant,
  getMappingsByStack,
} from "../api/assetApi";
import type { Plant, Stack, Mapping } from "../api/assetApi";

import { getAnalyticsData, getKpisByStack } from "../api/analyticsApi";
import type { AnalyticsResponse, KPIItem } from "../api/analyticsApi";

import { getTimeRange } from "../utils/time";

// ─────────────────────────────────────────────────────────────────────────────
// Types
// ─────────────────────────────────────────────────────────────────────────────

// rowId is the key fix: each compare row owns exactly one payload entry,
// identified by rowId — NOT by AssetName or TagName (which can repeat).
interface Payload {
  rowId: number;
  AssetName: string;
  TagName: string;
  StartTime: string;
  EndTime: string;
}

// ─────────────────────────────────────────────────────────────────────────────
// Format helpers
// ─────────────────────────────────────────────────────────────────────────────

/**
 * Normal mode: one series per tagName (signals are always distinct here
 * because the multi-select enforces uniqueness within a single stack).
 */
function formatToChartData(
  responses: AnalyticsResponse[],
  timeRange: "1h" | "24h" | "7d" | "30d" | "custom"
): { time: string; value: number; stackName: string }[] {
  return responses.flatMap((response) =>
    (response.values ?? []).map((point) => ({
      time: formatTime(point.timeStamp, timeRange),
      value: point.value ?? 0,
      stackName: response.tagName,
    }))
  );
}

/**
 * Compare mode: series label = "StackName · TagName" so that two rows
 * with the SAME signal on DIFFERENT stacks still produce two distinct lines.
 */
function formatToChartDataCompare(
  responses: (AnalyticsResponse & { _seriesLabel: string })[],
  timeRange: "1h" | "24h" | "7d" | "30d" | "custom"
): { time: string; value: number; stackName: string }[] {
  return responses.flatMap((response) =>
    (response.values ?? []).map((point) => ({
      time: formatTime(point.timeStamp, timeRange),
      value: point.value ?? 0,
      stackName: response._seriesLabel, // unique per stack+signal pair
    }))
  );
}

function formatTime(
  timeStamp: string,
  timeRange: "1h" | "24h" | "7d" | "30d" | "custom"
): string {
  const date = new Date(timeStamp);
  return timeRange === "1h" || timeRange === "24h"
    ? date.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" })
    : date.toLocaleString([], {
        day: "2-digit",
        month: "short",
        hour: "2-digit",
        minute: "2-digit",
      });
}

// ─────────────────────────────────────────────────────────────────────────────
// Constants
// ─────────────────────────────────────────────────────────────────────────────
const COMPARE_COLORS = ["#3b82f6", "#f97316", "#22c55e", "#a855f7", "#ef4444"];

// ─────────────────────────────────────────────────────────────────────────────
// ChartLoader
// ─────────────────────────────────────────────────────────────────────────────
function ChartLoader() {
  return (
    <div className="flex flex-col items-center justify-center w-full h-full gap-3">
      <div className="w-10 h-10 border-4 border-blue-200 border-t-blue-500 rounded-full animate-spin" />
      <p className="text-sm text-gray-400 dark:text-gray-500">
        Loading signal data…
      </p>
    </div>
  );
}

// ─────────────────────────────────────────────────────────────────────────────
// Main Signals component
// ─────────────────────────────────────────────────────────────────────────────
export default function Signals() {
  const [timeRange, setTimeRange] = useState<"1h" | "24h" | "7d" | "30d" | "custom">("24h");
  const [customStart, setCustomStart] = useState<Date | null>(null);
  const [customEnd, setCustomEnd] = useState<Date | null>(null);

  const [plants, setPlants] = useState<Plant[]>([]);
  const [stacks, setStacks] = useState<Stack[]>([]);
  const [signals, setSignals] = useState<Mapping[]>([]);

  const [selectedPlant, setSelectedPlant] = useState<number | "">("");
  const [selectedStack, setSelectedStack] = useState<number | "">("");
  const [selectedSignals, setSelectedSignals] = useState<string[]>([]);

  const [chartData, setChartData] = useState<{ time: string; value: number; stackName: string }[]>([]);
  const [kpiData, setKpiData] = useState<KPIItem[]>([]);
  const [chartLoading, setChartLoading] = useState(false);

  // ── Compare mode ──────────────────────────────────────────────────────────
  const [compareMode, setCompareMode] = useState(false);

  // comparePayload is now keyed by rowId — one entry per compare row.
  const [comparePayload, setComparePayload] = useState<Payload[]>([]);

  const [compareRows, setCompareRows] = useState<
    {
      id: number;
      stackId: number | "";
      stackName: string;
      signals: Mapping[];
      selectedSignal: string;
      color: string;
    }[]
  >([]);

  // ── Fetch plants on mount ─────────────────────────────────────────────────
  useEffect(() => {
    getPlants()
      .then(setPlants)
      .catch((err) => console.error("Failed to fetch plants:", err));
  }, []);

  // ── Build payload helper ──────────────────────────────────────────────────
  const buildPayload = (
    rowId: number,
    assetName: string,
    tagName: string
  ): Payload | null => {
    if (timeRange === "custom") {
      if (!customStart || !customEnd) return null;
      return {
        rowId,
        AssetName: assetName,
        TagName: tagName,
        StartTime: customStart.toISOString(),
        EndTime: customEnd.toISOString(),
      };
    }
    const time = getTimeRange(timeRange);
    if (!time) return null;
    return {
      rowId,
      AssetName: assetName,
      TagName: tagName,
      StartTime: time.startTime,
      EndTime: time.endTime,
    };
  };

  // ── Plant change ──────────────────────────────────────────────────────────
  const handlePlantChange = async (e: React.ChangeEvent<HTMLSelectElement>) => {
    const plantId = Number(e.target.value);
    setSelectedPlant(plantId);
    setSelectedStack("");
    setSelectedSignals([]);
    setChartData([]);
    setSignals([]);
    setStacks([]);
    if (!plantId) return;
    try {
      const data = await getStacksByPlant(plantId);
      setStacks(data);
    } catch (err) {
      console.error("Failed to fetch stacks:", err);
    }
  };

  // ── Stack change ──────────────────────────────────────────────────────────
  const handleStackChange = async (e: React.ChangeEvent<HTMLSelectElement>) => {
    const stackId = Number(e.target.value);
    setSelectedStack(stackId);
    setSelectedSignals([]);
    setChartData([]);
    if (!stackId) return;
    try {
      const mappingData = await getMappingsByStack(stackId);
      setSignals(mappingData);

      const stack = stacks.find((s) => s.assetId === stackId);
      if (stack) {
        try {
          const kpiResponse = await getKpisByStack(stack.name);
          if (kpiResponse?.data) setKpiData(kpiResponse.data);
        } catch {
          // KPIs are non-critical
        }
      }
    } catch (err) {
      console.error("Failed to fetch stack data:", err);
    }
  };

  // ── Fetch all selected signals in parallel (normal mode) ─────────────────
  const handleFetch = async () => {
    if (!selectedStack || selectedSignals.length === 0) return;
    const stack = stacks.find((s) => s.assetId === selectedStack);
    if (!stack) return;

    const payloads = selectedSignals
      .map((tagName) => buildPayload(-1, stack.name, tagName))
      .filter((p): p is Payload => p !== null);

    if (payloads.length === 0) return;

    setChartLoading(true);
    try {
      const responses: AnalyticsResponse[] = await Promise.all(
        payloads.map((p) => getAnalyticsData(p))
      );
      setChartData(formatToChartData(responses, timeRange));
    } catch (err) {
      console.error("Error fetching signal data:", err);
    } finally {
      setChartLoading(false);
    }
  };

  // ── Compare handlers ──────────────────────────────────────────────────────
  const addCompareRow = () => {
    setCompareRows((prev) => [
      ...prev,
      {
        id: Date.now(),
        stackId: "",
        stackName: "",
        signals: [],
        selectedSignal: "",
        color: COMPARE_COLORS[prev.length % COMPARE_COLORS.length],
      },
    ]);
  };

  const removeCompareRow = (id: number) => {
    setCompareRows((prev) => prev.filter((r) => r.id !== id));
    // Remove by rowId — safe regardless of stack/signal values
    setComparePayload((prev) => prev.filter((p) => p.rowId !== id));
  };

  const handleCompareStackChange = async (id: number, stackId: number) => {
    const stack = stacks.find((s) => s.assetId === stackId);
    if (!stack) return;
    try {
      const mappingData = await getMappingsByStack(stackId);
      setCompareRows((prev) =>
        prev.map((r) =>
          r.id === id
            ? {
                ...r,
                stackId,
                stackName: stack.name,
                signals: mappingData,
                selectedSignal: "",
              }
            : r
        )
      );
      // Clear any existing payload for this row since stack changed
      setComparePayload((prev) => prev.filter((p) => p.rowId !== id));
    } catch (err) {
      console.error("Failed to fetch signals for compare row:", err);
    }
  };

  const handleCompareSignalChange = (id: number, tagName: string) => {
    const row = compareRows.find((r) => r.id === id);
    if (!row || !row.stackName) return;

    setCompareRows((prev) =>
      prev.map((r) => (r.id === id ? { ...r, selectedSignal: tagName } : r))
    );

    // Build payload with rowId as the unique key.
    // This ensures two rows with the same signal on different stacks both survive.
    const payload = buildPayload(id, row.stackName, tagName);
    if (!payload) return;

    setComparePayload((prev) => {
      // Replace only this row's payload (by rowId), leave all others untouched
      const filtered = prev.filter((p) => p.rowId !== id);
      return [...filtered, payload];
    });
  };

  const getCompareAnalyticsData = async () => {
    if (comparePayload.length === 0) return;
    setChartLoading(true);
    try {
      // Attach a unique series label (StackName · TagName) so that two rows
      // with the same TagName but different stacks render as separate lines.
      const responses = await Promise.all(
        comparePayload.map(async (p) => {
          const res = await getAnalyticsData(p);
          return {
            ...res,
            _seriesLabel: `${p.AssetName} · ${p.TagName}`,
          };
        })
      );
      setChartData(formatToChartDataCompare(responses, timeRange));
    } catch (err) {
      console.error("Error fetching compare data:", err);
    } finally {
      setChartLoading(false);
    }
  };

  const handleClearCompare = () => {
    setCompareRows([]);
    setComparePayload([]);
    setChartData([]);
  };

  // ─────────────────────────────────────────────────────────────────────────
  // Render
  // ─────────────────────────────────────────────────────────────────────────
  return (
    <div className="p-4 space-y-6 min-h-screen bg-gray-50 dark:bg-gray-900">
      {/* Header */}
      <div className="flex items-center justify-between">
        <h2 className="text-2xl font-semibold text-gray-800 dark:text-gray-200">Signals</h2>
        <button
          onClick={() => {
            setCompareMode((v) => !v);
            setCompareRows([]);
            setComparePayload([]);
            setChartData([]);
          }}
          className={`px-4 py-2 rounded-lg text-sm font-medium transition-colors ${
            compareMode
              ? "bg-blue-600 text-white hover:bg-blue-700"
              : "bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-200 hover:bg-gray-100 dark:hover:bg-gray-700"
          }`}
        >
          {compareMode ? "✕ Exit Compare" : "⇄ Compare Signals"}
        </button>
      </div>

      {/* Filter bar */}
      <div className="flex flex-wrap items-start gap-4 bg-white dark:bg-gray-800 p-4 rounded-2xl shadow">
        {/* Plant */}
        <div className="flex flex-1 flex-col gap-1 min-w-[160px]">
          <label className="text-sm text-gray-500">Plant</label>
          <select
            value={selectedPlant}
            onChange={handlePlantChange}
            className="px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 dark:bg-gray-700 dark:text-gray-200"
          >
            <option value="">Select Plant</option>
            {plants.map((plant) => (
              <option key={plant.assetId} value={plant.assetId}>
                {plant.name}
              </option>
            ))}
          </select>
        </div>

        {/* Stack — hidden in compare mode */}
        {!compareMode && (
          <div className="flex flex-1 flex-col gap-1 min-w-[160px]">
            <label className="text-sm text-gray-500">Stack</label>
            <select
              value={selectedStack}
              onChange={handleStackChange}
              disabled={stacks.length === 0}
              className="px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 dark:bg-gray-700 dark:text-gray-200 disabled:opacity-50"
            >
              <option value="">Select Stack</option>
              {stacks.map((stack) => (
                <option key={stack.assetId} value={stack.assetId}>
                  {stack.name}
                </option>
              ))}
            </select>
          </div>
        )}

        {/* Multi-signal checkbox dropdown — normal mode only */}
        {!compareMode && (
          <SignalMultiSelect
            signals={signals}
            selectedSignals={selectedSignals}
            onChange={setSelectedSignals}
            disabled={signals.length === 0}
          />
        )}

        {/* Time Range */}
        <div className="flex flex-1 flex-col gap-1 min-w-[160px]">
          <label className="text-sm text-gray-500">Time Range</label>
          <select
            value={timeRange}
            onChange={(e) => setTimeRange(e.target.value as typeof timeRange)}
            className="px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 dark:bg-gray-700 dark:text-gray-200"
          >
            <option value="1h">Last 1 Hour</option>
            <option value="24h">Last 24 Hours</option>
            <option value="7d">Last 7 Days</option>
            <option value="30d">Last 30 Days</option>
            <option value="custom">Custom Range</option>
          </select>
          {timeRange === "custom" && (
            <div className="flex gap-2 mt-1">
              <DatePicker
                selected={customStart}
                onChange={setCustomStart}
                placeholderText="Start"
                className="px-2 py-1.5 border rounded-lg text-sm w-full"
              />
              <DatePicker
                selected={customEnd}
                onChange={setCustomEnd}
                placeholderText="End"
                className="px-2 py-1.5 border rounded-lg text-sm w-full"
              />
            </div>
          )}
        </div>

        {/* Fetch button — normal mode only */}
        {!compareMode && (
          <div className="flex flex-col justify-end">
            <label className="text-sm text-gray-500 invisible select-none">.</label>
            <button
              onClick={handleFetch}
              disabled={selectedSignals.length === 0 || !selectedStack}
              className="px-5 py-2 bg-blue-500 hover:bg-blue-600 text-white rounded-lg text-sm font-medium
                transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
            >
              Fetch Data
            </button>
          </div>
        )}
      </div>

      {/* Compare rows */}
      {compareMode && (
        <div className="bg-white dark:bg-gray-800 p-4 rounded-2xl shadow space-y-3">
          <div className="flex items-center justify-between mb-1">
            <h3 className="text-sm font-semibold text-gray-600 dark:text-gray-300">
              Select stacks &amp; signals to compare
            </h3>
            <div className="flex gap-3">
              <button
                onClick={addCompareRow}
                className="px-3 py-1.5 rounded-lg bg-blue-50 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400 text-sm font-medium hover:bg-blue-100 transition-colors"
              >
                + Add Stack
              </button>
              <button
                onClick={getCompareAnalyticsData}
                disabled={comparePayload.length === 0}
                className="px-3 py-1.5 rounded-lg bg-blue-50 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400 text-sm font-medium hover:bg-blue-100 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
              >
                Fetch
              </button>
              <button
                onClick={handleClearCompare}
                className="px-3 py-1.5 rounded-lg bg-red-50 text-red-500 text-sm font-medium hover:bg-red-100 transition-colors"
              >
                ✕ Clear All
              </button>
            </div>
          </div>

          {compareRows.length === 0 && (
            <p className="text-sm text-gray-400 dark:text-gray-500 py-2">
              Click "Add Stack" to start comparing.
            </p>
          )}

          {compareRows.map((row, idx) => (
            <div
              key={row.id}
              className="flex flex-wrap items-end gap-4 border rounded-xl p-3 dark:border-gray-700"
            >
              <div
                className="w-4 h-4 rounded-full mt-6 flex-shrink-0"
                style={{ backgroundColor: row.color }}
              />

              <div className="flex flex-1 flex-col gap-1 min-w-[160px]">
                <label className="text-xs text-gray-500">Stack {idx + 1}</label>
                <select
                  value={row.stackId}
                  onChange={(e) =>
                    handleCompareStackChange(row.id, Number(e.target.value))
                  }
                  className="px-3 py-2 rounded-lg border text-sm dark:bg-gray-700 dark:border-gray-600 dark:text-gray-200"
                >
                  <option value="">Select Stack</option>
                  {stacks.map((stack) => (
                    <option key={stack.assetId} value={stack.assetId}>
                      {stack.name}
                    </option>
                  ))}
                </select>
              </div>

              <div className="flex flex-1 flex-col gap-1 min-w-[160px]">
                <label className="text-xs text-gray-500">Signal</label>
                <select
                  value={row.selectedSignal}
                  onChange={(e) =>
                    handleCompareSignalChange(row.id, e.target.value)
                  }
                  disabled={row.signals.length === 0}
                  className="px-3 py-2 rounded-lg border text-sm disabled:opacity-50 dark:bg-gray-700 dark:border-gray-600 dark:text-gray-200"
                >
                  <option value="">Select Signal</option>
                  {row.signals.map((signal) => (
                    <option key={signal.mappingId} value={signal.tagName}>
                      {signal.tagName}
                    </option>
                  ))}
                </select>
              </div>

              <button
                onClick={() => removeCompareRow(row.id)}
                className="mb-0.5 px-2 py-2 rounded-lg text-gray-400 hover:text-red-500 hover:bg-red-50 dark:hover:bg-red-900/20 transition-colors text-sm"
              >
                ✕
              </button>
            </div>
          ))}
        </div>
      )}

      {/* Chart — same SignalChart component for both modes */}
      <div className="bg-white dark:bg-gray-800 p-4 rounded-2xl shadow">
        <h3 className="text-lg font-semibold mb-4 text-gray-700 dark:text-gray-200">
          {compareMode ? "Signal Comparison" : "Signal Trends"}
        </h3>
        <div className="w-full h-[400px]">
          {chartLoading ? (
            <ChartLoader />
          ) : chartData.length > 0 ? (
            <SignalChart chartData={chartData} />
          ) : (
            <div className="flex items-center justify-center h-full text-sm text-gray-400 dark:text-gray-500">
              {compareMode
                ? "Add stacks, select a signal per stack, then click Fetch."
                : "Select a stack, pick one or more signals, then click Fetch Data."}
            </div>
          )}
        </div>
      </div>

      {/* KPI Cards — normal mode only */}
      {!compareMode && kpiData.length > 0 && (
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          {kpiData.map((kpi, index) => (
            <div
              key={index}
              className="bg-white dark:bg-gray-800 p-4 rounded-2xl shadow"
            >
              <p className="text-sm text-gray-500">
                {kpi.kpiName.replaceAll("_", " ")}
              </p>
              <h3 className="text-xl font-bold text-gray-800 dark:text-white">
                {kpi.kpiValue.toFixed(2)}
              </h3>
              <p className="text-xs text-gray-400 mt-1">Level: {kpi.level}</p>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}