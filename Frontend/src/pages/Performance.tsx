import React, { useState, useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { GetAllPlantKpis, GetAllStackKpis, type PlantKpis, type StackKpis } from "../api/assetApi";
import {
  GetPlantKpiData,
  GetStackKpiData,
  type PlantKpiAnalyticsResult,
  type stackKpiAnalyticsResult,
} from "../api/analyticsApi";
import KpiBarChart from "../components/ui/Kpibarchart";
import { ScatterChart } from "@/components/ui/ScatterChart";
import { Stepper } from "@/components/ui/Stepper";
import { DropdownMenu } from "@radix-ui/react-dropdown-menu";
import { Fullscreen } from "lucide-react";
import {formatDateToShort} from "../utils/time"

export interface DashboardSettings {
  plantNoOfWeeks: number;
  stackNoOfWeeks: number;
  stackNoOfStacks: number;
}

export const DEFAULT_SETTINGS: DashboardSettings = {
  plantNoOfWeeks: 2,
  stackNoOfWeeks: 3,
  stackNoOfStacks: 3,
};

export const SETTINGS_KEY = "dashboardSettings";

export function loadSettings(): DashboardSettings {
  try {
    const raw = localStorage.getItem(SETTINGS_KEY);
    return raw ? { ...DEFAULT_SETTINGS, ...JSON.parse(raw) } : DEFAULT_SETTINGS;
  } catch {
    return DEFAULT_SETTINGS;
  }
}

export function saveSettings(s: DashboardSettings) {
  localStorage.setItem(SETTINGS_KEY, JSON.stringify(s));
}




function EmptyState({ text }: { text: string }) {
  return (
    <div className="flex items-center justify-center flex-1 min-h-[260px] text-sm text-gray-400 dark:text-gray-500">
      {text}
    </div>
  );
}

// ─── Page ─────────────────────────────────────────────────────────────────────
export default function Performance() {
  const navigate = useNavigate();

  const [settings, setSettings] = useState<DashboardSettings>(loadSettings());

  // KPI lists for dropdowns
  const [plantKpis, setPlantKpis] = useState<PlantKpis[]>([]);
  const [stackKpis, setStackKpis] = useState<StackKpis[]>([]);

  // Selected KPIs
  const [selectedPlantKpi, setSelectedPlantKpi] = useState<PlantKpis | null>(null);
  const [selectedStackKpi, setSelectedStackKpi] = useState<StackKpis | null>(null);

  // Chart data
  const [plantAnalytics, setPlantAnalytics] = useState<PlantKpiAnalyticsResult | null>(null);
  const [stackAnalytics, setStackAnalytics] = useState<stackKpiAnalyticsResult | null>(null);

  // Loading states
  const [loadingPlant, setLoadingPlant] = useState(false);
  const [loadingStack, setLoadingStack] = useState(false);

  const [saved, setSaved] = useState(false);

  // ── Fetch dropdown lists once ─────────────────────────────────────────────
  useEffect(() => {
    (async () => {
      try {
        const [p, s] = await Promise.all([GetAllPlantKpis(), GetAllStackKpis()]);
        setPlantKpis(p);
        setStackKpis(s);
      } catch (e) {
        console.error(e);
      }
    })();
  }, []);

  // ── Re-fetch plant chart when KPI or week count changes ───────────────────
  useEffect(() => {
    if (!selectedPlantKpi) return;
    (async () => {
      setLoadingPlant(true);
      setPlantAnalytics(null);
      try {
        const res = await GetPlantKpiData({
          KpiId: selectedPlantKpi.tagId as number,
          KpiName: selectedPlantKpi.tagName as string,
          NoOfWeeks: settings.plantNoOfWeeks,
        });
        setPlantAnalytics(res);
      } catch (e) {
        console.error(e);
      } finally {
        setLoadingPlant(false);
      }
    })();
  }, [selectedPlantKpi, settings.plantNoOfWeeks]);

  // ── Re-fetch stack chart when KPI, stacks, or week count changes ──────────
  useEffect(() => {
    if (!selectedStackKpi) return;
    (async () => {
      setLoadingStack(true);
      setStackAnalytics(null);
      try {
        const res = await GetStackKpiData({
          KpiId: selectedStackKpi.tagId as number,
          KpiName: selectedStackKpi.tagName as string,
          NoOfStack: settings.stackNoOfStacks,
          NoOfWeeks: settings.stackNoOfWeeks,
        });
        setStackAnalytics(res);
      } catch (e) {
        console.error(e);
      } finally {
        setLoadingStack(false);
      }
    })();
  }, [selectedStackKpi, settings.stackNoOfStacks, settings.stackNoOfWeeks]);

  const handlePlantKpiChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const found = plantKpis.find((k) => k.tagId?.toString() === e.target.value) ?? null;
    setSelectedPlantKpi(found);
    setPlantAnalytics(null);
  };

  const handleStackKpiChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const found = stackKpis.find((k) => k.tagId?.toString() === e.target.value) ?? null;
    setSelectedStackKpi(found);
    setStackAnalytics(null);
  };

  const plantChartData = [
    ...(plantAnalytics?.weeklyData.map((w) => ({
      kpiName: `W-${w.weekNumber} ${formatDateToShort(w.startTime)} ${formatDateToShort(w.endTime)}`,
      kpiValue: w.value,
      level: "plant",
    })) ?? []),
    ...(plantAnalytics?.hourlyData.map((h) => ({
      kpiName: "Last Hour",
      kpiValue: h.value,
      level: "plant",
    })) ?? []),
  ];

 


  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900 p-6 flex flex-col gap-6">

      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
         
          <div>
            <h1 className="text-xl font-bold text-gray-900 dark:text-white">
              Customize Dashboard
            </h1>
            <p className="text-xs text-gray-400 mt-0.5">
              Pick a KPI, tweak parameters, see the live preview — then save
            </p>
          </div>
        </div>

        <div className="flex items-center gap-3">
          {/* <button
            onClick={() =>
              setSettings({
                plantNoOfWeeks: DEFAULT_SETTINGS.plantNoOfWeeks,
                stackNoOfWeeks: DEFAULT_SETTINGS.stackNoOfWeeks,
                stackNoOfStacks: DEFAULT_SETTINGS.stackNoOfStacks,
              })
            }
            className="px-4 py-2 text-sm text-gray-400 hover:text-gray-700 dark:hover:text-gray-200 transition-colors"
          >
            Reset
          </button>
          <button
            onClick={handleSave}
            disabled={saved}
            className={`px-5 py-2 rounded-xl text-sm font-semibold transition-all shadow-sm ${
              saved
                ? "bg-green-500 text-white scale-95"
                : "bg-blue-600 hover:bg-blue-700 text-white"
            }`}
          >
            {saved ? "✓ Saved!" : "Save & return"}
          </button> */}
        </div>
      </div>

      {/* Two cards */}
      <div className="flex flex-col gap-6 flex-1">

        {/* ══ Plant Card ══ */}
        <div className="flex-1 bg-white dark:bg-gray-800 rounded-2xl shadow flex flex-col overflow-hidden">
        
          <div className="p-5 flex flex-col gap-4 flex-1">

            <p className="text-xs font-semibold uppercase tracking-widest text-gray-500 dark:text-gray-400">
              Plant Level Insight
            </p>

            {/* KPI dropdown */}
            <select
              onChange={handlePlantKpiChange}
              defaultValue=""
              className="px-3 py-2 rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-800 dark:text-gray-100 text-sm"
            >
              <option value="" disabled>Select KPI</option>
              {plantKpis.map((kpi) => (
                <option key={kpi.tagId?.toString()} value={kpi.tagId?.toString()}>
                  {kpi.tagName?.replaceAll("_", " ")}
                </option>
              ))}
            </select>

            {/* Weeks stepper */}
            <div className="bg-gray-50 dark:bg-gray-700/50 rounded-xl px-4 py-3">
              <Stepper
                label="Weeks of data"
                value={settings.plantNoOfWeeks}
                min={1}
                max={12}
                onChange={(v) => setSettings((p) => ({ ...p, plantNoOfWeeks: v }))}
              />
            </div>

            {/* Live chart preview */}
            {loadingPlant && <EmptyState text="Loading…" />}
            {!loadingPlant && !plantAnalytics && (
              <EmptyState text="Select a KPI to preview the chart" />
            )}
            {!loadingPlant && plantAnalytics && plantChartData.length > 0 && (
              <div className="flex flex-col gap-1 flex-1">
                <p className="text-xs text-gray-400 capitalize">
                  {plantAnalytics.kpiName.replaceAll("_", " ")} — last{" "}
                  {plantAnalytics.noOfWeeks}w + last hour
                </p>
                <KpiBarChart data={plantChartData} color="#3b82f6" height={280} />
              </div>
            )}
          </div>
        </div>

        {/* ══ Stack Card ══ */}
        <div className="flex-1 bg-white dark:bg-gray-800 rounded-2xl shadow flex flex-col overflow-hidden">
          <div className="p-5 flex flex-col gap-4 flex-1">

            <p className="text-xs font-semibold uppercase tracking-widest text-gray-500 dark:text-gray-400">
              Stack Level Insight
            </p>

            {/* KPI dropdown */}
            <select
              onChange={handleStackKpiChange}
              defaultValue=""
              className="px-3 py-2 rounded-lg border border-gray-200 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-800 dark:text-gray-100 text-sm"
            >
              <option value="" disabled>Select KPI</option>
              {stackKpis.map((kpi) => (
                <option key={kpi.tagId?.toString()} value={kpi.tagId?.toString()}>
                  {kpi.tagName?.replaceAll("_", " ")}
                </option>
              ))}
            </select>

            {/* Steppers */}
            <div className="bg-gray-50 dark:bg-gray-700/50 rounded-xl px-4 py-3 flex flex-col gap-3">
              <Stepper
                label="Number of stacks"
                value={settings.stackNoOfStacks}
                min={1}
                max={10}
                onChange={(v) => setSettings((p) => ({ ...p, stackNoOfStacks: v }))}
              />
              <div className="border-t border-gray-100 dark:border-gray-600" />
              <Stepper
                label="Weeks of data"
                value={settings.stackNoOfWeeks}
                min={1}
                max={12}
                onChange={(v) => setSettings((p) => ({ ...p, stackNoOfWeeks: v }))}
              />
            </div>

            {/* Live chart preview */}
            {loadingStack && <EmptyState text="Loading…" />}
            {!loadingStack && !stackAnalytics && (
              <EmptyState text="Select a KPI to preview the chart" />
            )}
            {!loadingStack && stackAnalytics && (
              <div className="flex-1">
                <ScatterChart data={stackAnalytics} height={350} />
              </div>
            )}
          </div>
        </div>

        <DropdownMenu/>

      </div>
    </div>
  );
}