import axios from "axios";
import { toast } from "react-toastify"; // ADD THIS

export interface AnalyticsPayload {
  AssetName: string;
  TagName: string;
  StartTime: string;
  EndTime: string;
}

export interface DataPoint {
  timeStamp: string;
  value: number;
}

export interface AnalyticsResponse {
  asseName: string; 
  tagName: string;  
  values: DataPoint[];
}

export interface KPIItem {
  kpiName: string;
  kpiValue: number;
  level: string;
}

export interface KPIResponse {
  stackName: string;
  week: number;
  data: KPIItem[];
}

interface WeeklyDataItem {
  weekNumber: number;
  startTime: string;
  endTime: string;
  value: number;
}

interface HourlyDataItem {
  startTime: string;
  endTime: string;
  value: number;
}

export interface PlantKpiAnalyticsResult {
  kpiName: string;
  noOfWeeks: number;
  weeklyData: WeeklyDataItem[];
  hourlyData: HourlyDataItem[];
}

interface stackKpiVlaues {
  startTime: string,
  endpoint: string,
  assetname: string,
  weekNumber: number,
  value: number
} 

export interface stackKpiAnalyticsResult {
  kpiName: string,
  noOfStacks: number,
  noOfWeeks: number,
  values: stackKpiVlaues[]
}

export interface AlertResponse {
  id: number,
  mappingId: number,
  assetName:string,
  signalName: string,
  value: number,
  alarmType: string,
  status: string,
  createdAt: string,
  resolvedAt: string,
  mapping: number
}

//AXIOS INSTANCE
const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
  withCredentials: true,
  headers: {
    "Content-Type": "application/json"
  }
});


api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response) {
      if (error.response.status === 403) {
        toast.error("Access Denied ");
      }

      // Optional (recommended)
      if (error.response.status === 401) {
        toast.error("Session expired. Please login again.");
      }
    } else {
      toast.error("Network error. Please try again.");
    }

    return Promise.reject(error);
  }
);



export const getAnalyticsData = async (
  payload: AnalyticsPayload
): Promise<AnalyticsResponse> => {
  try {
    console.log(payload);
    const res = await api.post<AnalyticsResponse>(
      "/Analytics/data",
      payload
    );
    return res.data;
  } catch (error) {
    console.error("Analytics API Error:", error);
    throw error;
  } 
};

export const getKpisByStack = async (
  stackName: string
): Promise<KPIResponse> => {
  try {
    const res = await api.get<KPIResponse>(
      `/Analytics/latest/${stackName}`
    );
    return res.data;
  } catch (error) {
    console.error("KPI API Error:", error);
    throw error;
  }
};

export const GetPlantKpiData = async (
  payload: any
): Promise<PlantKpiAnalyticsResult> => {
  try {
    const res = await api.post<PlantKpiAnalyticsResult>(
      "/Analytics/PlantKpis",
      payload
    );
    return res.data;
  } catch (error) {
    console.error("Analytics API Error:", error);
    throw error;
  }
};

export const GetStackKpiData = async (
  payload: any
): Promise<stackKpiAnalyticsResult> => {
  try {
    const res = await api.post<stackKpiAnalyticsResult>(
      "/Analytics/StackKpis",
      payload
    );
    return res.data;
  } catch (error) {
    console.error("Analytics API Error:", error);
    throw error;
  }
};

export const GetLatestAlerts = async (): Promise<AlertResponse[]> => {
  try {
    const res = await api.get<AlertResponse[]>("/Analytics/Alerts");
    return res.data;
  } catch (error) {
    console.error("Alerts Api Error:", error);
    throw error;
  }
};