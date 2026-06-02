import React, { useState, useEffect } from "react";
import { GetAllPlantKpis, GetAllStackKpis, type PlantKpis, type StackKpis } from "../api/assetApi";
import { GetPlantKpiData,GetStackKpiData,GetLatestAlerts,type PlantKpiAnalyticsResult,type stackKpiAnalyticsResult,type AlertResponse } from "../api/analyticsApi";
import KpiBarChart from "../components/ui/Kpibarchart";
import { ScatterChart } from "@/components/ui/ScatterChart";
import {DataTable} from "@/components/ui/DataTable";
import { useNavigate } from "react-router-dom";
import {formatDateToShort} from "../utils/time"


interface PlantKpiPayload {
  KpiId: number;
  KpiName: string;
  NoOfWeeks: number;
}



export default function Dashboard() {
  const [Alert,setAlerts] = useState<AlertResponse[]>([])
  const [plantKpis, setPlantKpis] = useState<PlantKpis[]>([]);
  const [StackKpis, setStackKpis] = useState<StackKpis[]>([]);
  const [analyticsData, setAnalyticsData] = useState<PlantKpiAnalyticsResult | null>(null);
  const [selectedPlantkpi, setSelectedPlantKpi] = useState<string | null>(null);
  const [selectedStackKpi, setSelectedStackKpi] = useState<string | null>(null);
  const [stackData, setStackData] = useState<stackKpiAnalyticsResult | null>(null);
  const [loadingPlantAnalytics, setLoadingPlantAnalytics] = useState(false);
  const [loadingStackAnalytics, setLoadingStackAnalytics] = useState(false);
  const navigate = useNavigate();

  useEffect(() => {
    const fetchKpis = async () => {
      try {
        const kpis = await GetAllPlantKpis();
        const stacklevelKpis = await GetAllStackKpis();
        setPlantKpis(kpis);
        setStackKpis(stacklevelKpis);

        if (kpis.length > 0) {
          const firstKpi = kpis[0];
          const firstId = firstKpi.tagId?.toString() ?? null;
          setSelectedPlantKpi(firstId);
          if (firstId) {
            fetchPlantKpiAnalytics(firstKpi);
          }
        }

        if (stacklevelKpis.length >= 4) {
          const defaultStackKpi = stacklevelKpis[4];
          const defaultStackId = defaultStackKpi.tagId?.toString() ?? null;
          setSelectedStackKpi(defaultStackId);
          if (defaultStackId) {
            fetchStackKpiAnalytics(defaultStackKpi);
          }
        }
      } catch (error) {
        console.error("Error fetching KPIs:", error);
      }
    };

    const fetchAlerts = async () => {
      try {
        const alerts = await GetLatestAlerts();
        setAlerts(alerts);
      } catch (error) {
        console.error("Error fetching latest alerts:", error) ;
      }
    };

    fetchKpis();
    fetchAlerts();

    const intervalId = window.setInterval(fetchAlerts, 180000); // 180,000ms = 3 minutes
    return () => window.clearInterval(intervalId);
  }, []);

  // console.log(Alert);

  //to fetch the kpi data when the kpi is been selected in the dropdwon
const fetchPlantKpiAnalytics = async (kpi: PlantKpis) => {
    const payload: PlantKpiPayload = {
      KpiId: kpi.tagId as number,
      KpiName: kpi.tagName as string,
      NoOfWeeks: 2,
    };

    setLoadingPlantAnalytics(true);
    setAnalyticsData(null);

    try {
      const response = await GetPlantKpiData(payload);
      // if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);
      // console.log("Plant KPI Analytics Response:", response);
      setAnalyticsData(response);
    } catch (error) {
      console.error("Error fetching Plant KPI analytics:", error);
    } finally {
      setLoadingPlantAnalytics(false);
    }
  };


  const handlePlantKpiChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const selectedKpiId = e.target.value;
    setSelectedPlantKpi(selectedKpiId);
    const selected = plantKpis.find((k) => k.tagId?.toString() === selectedKpiId);
    setAnalyticsData(null);
    if (selected) fetchPlantKpiAnalytics(selected);
  };

  const handleStackKpiChange  =(e:React.ChangeEvent<HTMLSelectElement>)=>{
       const selectedKpiId = e.target.value;
       setSelectedStackKpi(selectedKpiId);
       const selected = StackKpis.find(k => k.tagId?.toString() === selectedKpiId);
       setStackData(null);
       if(selected) fetchStackKpiAnalytics(selected);
  };

const fetchStackKpiAnalytics = async (kpi: StackKpis) => {
  const Payload = {
    KpiId: kpi.tagId as number,        
    KpiName: kpi.tagName as string,   
    NoOfStack: 3,
    NoOfWeeks: 3,
  };

  setLoadingStackAnalytics(true);
  setStackData(null);

  try {
    const response = await GetStackKpiData(Payload);
    // console.log("Stack KPI Analytics Response:", response);
    setStackData(response);           
  } catch (error) {
    console.error("Error fetching Stack KPI analytics:", error);
  } finally {
    setLoadingStackAnalytics(false);
  }
};


  const chartData = [
    ...(analyticsData?.weeklyData.map((w) => ({
      kpiName: `Week ${w.weekNumber} ${formatDateToShort(w.startTime)} ${formatDateToShort(w.endTime)}`,
      kpiValue: w.value,
      level: "plant",
    })) ?? []),
    ...(analyticsData?.hourlyData.map( (h) => ({
      kpiName: `LastHour`,
      kpiValue: h.value,
      level: "plant",
    })) ?? []),
  ];



  const kpiLabel = analyticsData?.kpiName.replaceAll("_", " ");

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold text-foreground mb-2">
        Performance Insights Dashboard
        </h1>
      </div>

      <div className="flex flex-row gap-6">
        {/* ── Plant Level Insight card ── */}
        <div className="flex-1 bg-white dark:bg-gray-800 p-4 rounded-2xl shadow">
          <div className="flex flex-col gap-3">
            <div className="flex justify-between">
            <label className="text-sm text-gray-500 p-1">Plant Level Insight</label>
             <button onClick={()=> navigate("/performance")} className="px-2 py-2 rounded-lg bg-blue-50 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400 text-sm font-medium hover:bg-blue-100 dark:hover:bg-blue-900/50 transition-colors">Customize</button>
             </div>
            <select
              value={selectedPlantkpi ?? ""}
              onChange={handlePlantKpiChange}
              className="px-3 py-2 rounded-lg border"
            >  
              <option value="">Select KPI</option>
              {plantKpis.map((kpi) => (
                <option key={kpi.tagId?.toString()} value={kpi.tagId?.toString()}>
                  {kpi.tagName}
                </option>
              ))}
            </select>

            {/* Loading state */}
            {loadingPlantAnalytics && (
              <div className="flex items-center justify-center h-[300px] text-sm text-gray-400">
                Loading…
              </div>
            )}

            {/* Empty state */}
            {!loadingPlantAnalytics && !analyticsData && (
              <div className="flex items-center justify-center h-[300px] text-sm text-gray-400">
                Select a KPI to view the chart
              </div>
            )}

            {/* Data loaded — single chart with all bars */}
            {!loadingPlantAnalytics && analyticsData && chartData.length > 0 && (
              <div>
                <p className="text-xs font-medium text-gray-500 mb-1 capitalize">
                  {kpiLabel} — last {analyticsData.noOfWeeks} weeks + last hour
                </p>
                <KpiBarChart data={chartData} color="#3b82f6" height={300} />
              </div>
            )}
            
          </div>
        </div>


       
        <div className="flex-1 bg-white dark:bg-gray-800 p-4 rounded-2xl shadow">
          <div className="flex flex-col gap-3">
           <div className="flex justify-between">
            <label className="text-sm text-gray-500 p-1">Stack Level Insight</label>
             <button onClick={()=> navigate("/performance")} className="px-2 py-2 rounded-lg bg-blue-50 dark:bg-blue-900/30 text-blue-600 dark:text-blue-400 text-sm font-medium hover:bg-blue-100 dark:hover:bg-blue-900/50 transition-colors">Customize</button>
             </div>
            <select
              value={selectedStackKpi ?? ""}
              onChange={handleStackKpiChange}
              className="px-3 py-2 rounded-lg border"
            >
              <option value="">Select KPI</option>
              {StackKpis.map((kpi) => (
                <option key={kpi.tagId?.toString()} value={kpi.tagId?.toString()}>
                  {kpi.tagName}
                </option>
              ))}
               
            </select>


            {/* loader */}
            {loadingStackAnalytics && (
              <div className="flex items-center justify-center h-[300px] text-sm text-gray-400">
                Loading…
              </div>
            )}

            {!loadingStackAnalytics && !stackData && (
              <div className="flex items-center justify-center h-[300px] text-sm text-gray-400">
                Select a KPI to view the chart
              </div>
            )}

            {!loadingStackAnalytics && stackData && (
              <div>
                <ScatterChart data={stackData} />
              </div>
            )}

           
          </div>
        </div>

      </div>

     <div className="flex-1 bg-white dark:bg-gray-800 p-4 rounded-2xl shadow">
        <label className="text-sm text-gray-500">Recent Alerts</label>
        <DataTable AlertsData={Alert}/>
     </div>

    </div>
  );
}