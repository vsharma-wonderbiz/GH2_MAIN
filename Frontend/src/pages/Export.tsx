import { useState, useEffect } from "react";
import DatePicker from "react-datepicker";
import "react-datepicker/dist/react-datepicker.css";

import { getPlants, getStacksByPlant, getMappingsByStack } from "@/api/assetApi";

import {
  RequestExportData,
  getExportRequests,
  downloadExportFile,
  type ExportRequestItem,
} from "@/api/exportApi";

import { SignalMultiSelect } from "@/components/ui/SignalMultiSelect";
import { ExportHistoryTable } from "@/components/ui/Exporthistorytable";

import { type Mapping, type Plant, type Stack } from "@/api/assetApi";

export interface ExportRequestPaylod {
  assetNamme: string;
  tagNames: string[];
  startTime: string;
  endTime: string;
}

export default function Export() {
  const [plants, setPlants] = useState<Plant[]>([]);
  const [stacks, setStacks] = useState<Stack[]>([]);
  const [signals, setSignals] = useState<Mapping[]>([]);
  const [selectedSignals, setSelectedSignals] = useState<string[]>([]);

  const [selectedPlant, setSelectedPlant] = useState<number | "">("");
  const [selectedStack, setSelectedStack] = useState<number | "">("");

  const [timeRange, setTimeRange] = useState<
    "1h" | "24h" | "7d" | "30d" | "custom"
  >("24h");

  const [customStart, setCustomStart] = useState<Date | null>(null);
  const [customEnd, setCustomEnd] = useState<Date | null>(null);

  // --- export history / pagination state ---
  const [exportHistory, setExportHistory] = useState<ExportRequestItem[]>([]);
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const pageSize = 6;

  // --- download state ---
  const [downloadingId, setDownloadingId] = useState<number | null>(null);

  useEffect(() => {
    getPlants()
      .then(setPlants)
      .catch((err) => console.error("Failed to fetch plants:", err));
  }, []);

  const fetchExportHistory = async (pageNumber: number) => {
    try {
      const data = await getExportRequests(pageNumber, pageSize);
      setExportHistory(data.items);
      setTotalCount(data.totalcounts);
    } catch (err) {
      console.error("Failed to fetch export history:", err);
    }
  };

  useEffect(() => {
    fetchExportHistory(page);
  }, [page]);

  const BuildPayload = async (): Promise<ExportRequestPaylod | null> => {
    if (!selectedStack || selectedSignals.length === 0) {
      return null;
    }

    let startTime: string;
    let endTime: string;

    if (timeRange === "custom") {
      if (!customStart || !customEnd) {
        return null;
      }
      startTime = customStart.toISOString();
      endTime = customEnd.toISOString();
    } else {
      const now = new Date();
      endTime = now.toISOString();

      switch (timeRange) {
        case "1h":
          startTime = new Date(now.getTime() - 1 * 60 * 60 * 1000).toISOString();
          break;
        case "24h":
          startTime = new Date(now.getTime() - 24 * 60 * 60 * 1000).toISOString();
          break;
        case "7d":
          startTime = new Date(now.getTime() - 7 * 24 * 60 * 60 * 1000).toISOString();
          break;
        case "30d":
          startTime = new Date(now.getTime() - 30 * 24 * 60 * 60 * 1000).toISOString();
          break;
        default:
          return null;
      }
    }

    const selectedStackData = stacks.find(
      (stack) => stack.assetId === selectedStack
    );

    if (!selectedStackData) {
      return null;
    }

    const payload: ExportRequestPaylod = {
      assetNamme: selectedStackData.name,
      tagNames: selectedSignals,
      startTime,
      endTime,
    };

    return payload;
  };

  const handlePlantChange = async (e: React.ChangeEvent<HTMLSelectElement>) => {
    const plantId = Number(e.target.value);
    setSelectedPlant(plantId);
    setSelectedStack("");
    setSelectedSignals([]);
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

  const handleStackChange = async (e: React.ChangeEvent<HTMLSelectElement>) => {
    const stackId = Number(e.target.value);
    setSelectedStack(stackId);
    setSelectedSignals([]);

    if (!stackId) return;
    try {
      const mappingData = await getMappingsByStack(stackId);
      setSignals(mappingData);
    } catch (err) {
      console.error("Failed to fetch stack data:", err);
    }
  };

  const handleExport = async () => {
    const payload = await BuildPayload();

    if (!payload) {
      alert("Please select a stack, signals and a valid time range.");
      return;
    }

    try {
      const response = await RequestExportData(payload);
      console.log("Export requested successfully");
      console.log(response);

      // refresh history so the new request shows up immediately
      setPage(1);
      fetchExportHistory(1);
    } catch (err) {
      console.error(err);
    }
  };

  const handleDownload = async (item: ExportRequestItem) => {
    setDownloadingId(item.exportRequestId);
    try {
      const blob = await downloadExportFile(item.exportRequestId);

      // derive a clean filename from filePath (e.g. "Exports\Export_Stack_1_52.csv" -> "Export_Stack_1_52.csv")
      const fileName =
        item.filePath.split(/[\\/]/).pop() ||
        `export_${item.exportRequestId}.csv`;

      const url = window.URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = url;
      link.download = fileName;
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.URL.revokeObjectURL(url);
    } catch (err) {
      console.error("Failed to download export file:", err);
      alert("Failed to download the file. Please try again.");
    } finally {
      setDownloadingId(null);
    }
  };

  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));

  return (
    <div className="min-h-full bg-gray-50 dark:bg-gray-900 p-3 space-y-3">
      {/* Header */}
      <div className="flex items-center justify-between">
        <h2 className="text-2xl font-semibold text-gray-800 dark:text-gray-200">
          Exports
        </h2>

        <button className="rounded-lg border border-gray-300 bg-white px-4 py-2 text-sm font-medium text-gray-700 transition-colors hover:bg-gray-100 dark:border-gray-600 dark:bg-gray-800 dark:text-gray-200 dark:hover:bg-gray-700">
          History
        </button>
      </div>

      {/* Filters */}
      <div className="flex flex-wrap items-end gap-4 rounded-2xl bg-white p-4 shadow dark:bg-gray-800">
        {/* Plant */}
        <div className="flex min-w-[180px] flex-1 flex-col gap-1">
          <label className="text-sm text-gray-500">Plant</label>

          <select
            value={selectedPlant}
            onChange={handlePlantChange}
            className="rounded-lg border border-gray-300 px-3 py-2 dark:border-gray-600 dark:bg-gray-700 dark:text-gray-200"
          >
            <option value="">Select Plant</option>

            {plants.map((plant) => (
              <option key={plant.assetId} value={plant.assetId}>
                {plant.name}
              </option>
            ))}
          </select>
        </div>

        {/* Stack */}
        <div className="flex min-w-[180px] flex-1 flex-col gap-1">
          <label className="text-sm text-gray-500">Stack</label>

          <select
            value={selectedStack}
            onChange={handleStackChange}
            className="rounded-lg border border-gray-300 px-3 py-2 dark:border-gray-600 dark:bg-gray-700 dark:text-gray-200"
          >
            <option value="">Select Stack</option>

            {stacks.map((stack) => (
              <option key={stack.assetId} value={stack.assetId}>
                {stack.name}
              </option>
            ))}
          </select>
        </div>

        <SignalMultiSelect
          signals={signals}
          selectedSignals={selectedSignals}
          onChange={setSelectedSignals}
          disabled={signals.length === 0}
        />

        {/* Time Range */}
        <div className="flex min-w-[180px] flex-1 flex-col gap-1">
          <label className="text-sm text-gray-500">Time Range</label>

          <select
            value={timeRange}
            onChange={(e) => setTimeRange(e.target.value as typeof timeRange)}
            className="rounded-lg border border-gray-300 px-3 py-2 dark:border-gray-600 dark:bg-gray-700 dark:text-gray-200"
          >
            <option value="1h">Last 1 Hour</option>
            <option value="24h">Last 24 Hours</option>
            <option value="7d">Last 7 Days</option>
            <option value="30d">Last 30 Days</option>
            <option value="custom">Custom Range</option>
          </select>

          {timeRange === "custom" && (
            <div className="mt-2 flex gap-2">
              <DatePicker
                selected={customStart}
                onChange={(date: Date | null) => setCustomStart(date)}
                placeholderText="Start Date"
                className="w-full rounded-lg border px-2 py-2 text-sm"
              />

              <DatePicker
                selected={customEnd}
                onChange={(date: Date | null) => setCustomEnd(date)}
                placeholderText="End Date"
                className="w-full rounded-lg border px-2 py-2 text-sm"
              />
            </div>
          )}
        </div>

        {/* Export */}
        <button
          onClick={handleExport}
          disabled={!selectedPlant || !selectedStack}
          className="rounded-lg bg-blue-600 px-6 py-2 font-medium text-white transition-colors hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-50"
        >
          Export CSV
        </button>
      </div>

      {/* Export History */}
      <div className="bg-white dark:bg-gray-800 p-4 rounded-2xl shadow">
        <h3 className="text-lg font-semibold mb-4 text-gray-700 dark:text-gray-200">
          Download Csv
        </h3>

        <ExportHistoryTable
          exportData={exportHistory}
          onDownload={handleDownload}
          downloadingId={downloadingId}
        />

        <div className="flex items-center justify-end gap-3 mt-4">
          <button
            onClick={() => setPage((p) => Math.max(1, p - 1))}
            disabled={page === 1}
            className="rounded-lg border border-gray-300 px-3 py-1 text-sm text-gray-700 disabled:cursor-not-allowed disabled:opacity-50 dark:border-gray-600 dark:text-gray-200"
          >
            Previous
          </button>

          <span className="text-sm text-gray-500 dark:text-gray-400">
            Page {page} of {totalPages}
          </span>

          <button
            onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
            disabled={page >= totalPages}
            className="rounded-lg border border-gray-300 px-3 py-1 text-sm text-gray-700 disabled:cursor-not-allowed disabled:opacity-50 dark:border-gray-600 dark:text-gray-200"
          >
            Next
          </button>
        </div>
      </div>
    </div>
  );
}