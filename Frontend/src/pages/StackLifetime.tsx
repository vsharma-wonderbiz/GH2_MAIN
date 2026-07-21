import { useState, useEffect } from "react";
import { getStacksByPlant, getPlants } from "@/api/assetApi";
import { type Stack, type Plant } from "@/api/assetApi";
import { type StackEfficiency, StackEfficiencyData } from "@/api/analyticsApi";
import EfficiencyChart from "@/components/ui/EfficiencyChart";
import { ChartLoader } from "./Signals"; // <- confirm karo ye sahi path hai
import { LatestEfficiencyRecord, type EfficiencyResult } from "@/api/analyticsApi";

export default function StackLifetime() {
    const [selectedPlant, setSelectedPlant] = useState<number | "">("");
    const [selectedStack, setSelectedStack] = useState<string>("");
    const [stacks, setStacks] = useState<Stack[]>([]);
    const [plants, setPlants] = useState<Plant[]>([]);
    const [efficiencyData, setEfficiencyData] = useState<StackEfficiency | null>(null);
    const [chartLoading, setChartLoading] = useState(false);
    const [efficiencyDetails, setEfficiencyDetails] = useState<EfficiencyResult | null>(null);

    useEffect(() => {
        getPlants()
            .then(setPlants)
            .catch((err) => console.error("Failed to fetch plants:", err));
    }, []);

    const handlePlantChange = async (e: React.ChangeEvent<HTMLSelectElement>) => {
        const plantId = Number(e.target.value);
        setSelectedPlant(plantId);
        setSelectedStack("");
        setStacks([]);
        setEfficiencyData(null);
        setEfficiencyDetails(null); // 🔴 plant change pe cards bhi clear honi chahiye

        if (!plantId) return;

        try {
            const data = await getStacksByPlant(plantId);
            setStacks(data);
        } catch (err) {
            console.error("Failed to fetch stacks:", err);
        }
    };

    const handleStackChange = async (e: React.ChangeEvent<HTMLSelectElement>) => {
        const stackName = e.target.value;
        setSelectedStack(stackName);

        if (!stackName) {
            setEfficiencyData(null);
            setEfficiencyDetails(null); // 🔴 fixed - pehle setLatestEfficiency tha jo defined hi nahi tha
            return;
        }

        setChartLoading(true);

        try {
            try {
                const graphData = await StackEfficiencyData(stackName);
                setEfficiencyData(graphData);
            } catch (err) {
                console.error("Failed to fetch graph data:", err);
                setEfficiencyData(null);
            }

            try {
                const latestEfficiency = await LatestEfficiencyRecord(stackName);
                setEfficiencyDetails(latestEfficiency);
            } catch (err) {
                console.error("Failed to fetch latest efficiency:", err);
                setEfficiencyDetails(null);
            }
        } finally {
            setChartLoading(false);
        }
    };

    return (
        <div className="min-h-full bg-gray-50 dark:bg-gray-900 p-3 space-y-3">
            <div className="flex items-center justify-between">
                <h2 className="text-2xl font-semibold text-gray-800 dark:text-gray-200">
                    Stack Lifetime Estimation
                </h2>
            </div>

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
                            <option key={stack.assetId} value={stack.name}>
                                {stack.name}
                            </option>
                        ))}
                    </select>
                </div>
            </div>

           {/* stack stats section  */}
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
                {!selectedStack ? (
                    <div className="col-span-1 sm:col-span-3 rounded-2xl bg-white p-4 shadow dark:bg-gray-800 flex items-center justify-center text-gray-400 h-24">
                        Please select a stack to see the stats
                    </div>
                ) : (
                    <>
                        {/* Card 1 - Current Efficiency */}
                        <div className="rounded-2xl bg-white p-4 shadow dark:bg-gray-800">
                            <p className="text-sm text-gray-500">Current Efficiency</p>
                            <p className="mt-2 text-2xl font-semibold text-gray-800 dark:text-gray-200">
                                {efficiencyDetails
                                    ? `${efficiencyDetails.latestEfficiency.toFixed(2)}%`
                                    : "—"}
                            </p>
                        </div>

                        {/* Card 2 - Remaining Lifetime */}
                        <div className="rounded-2xl bg-white p-4 shadow dark:bg-gray-800">
                            <p className="text-sm text-gray-500">Remaining Lifetime</p>
                            <p className="mt-2 text-2xl font-semibold text-gray-800 dark:text-gray-200">
                                {efficiencyDetails
                                    ? `${efficiencyDetails.operationalHours.toFixed(0)} hrs`
                                    : "—"}
                            </p>
                        </div>

                        {/* Card 3 - Status */}
                        <div className="rounded-2xl bg-white p-4 shadow dark:bg-gray-800">
                            <p className="text-sm text-gray-500">Status</p>
                            <p className="mt-2 text-2xl font-semibold text-gray-800 dark:text-gray-200">
                                {efficiencyDetails ? efficiencyDetails.status : "—"}
                            </p>
                        </div>
                    </>
                )}
            </div>

            {/* Chart */}
            <div className="bg-white dark:bg-gray-800 p-4 rounded-2xl shadow">
                <h3 className="text-lg font-semibold mb-4 text-gray-700 dark:text-gray-200">
                    Lifetime Prediction
                </h3>
                <div className="w-full h-[500px]">
                    {chartLoading ? (
                        <ChartLoader />
                    ) : efficiencyData && efficiencyData.actual.length > 0 ? (
                        <EfficiencyChart
                            actual={efficiencyData.actual}
                            predicted={efficiencyData.predicted}
                        />
                    ) : (
                        <div className="flex items-center justify-center h-full text-gray-400">
                            Select a stack to view efficiency data
                        </div>
                    )}
                </div>
            </div>
        </div>
    );
}