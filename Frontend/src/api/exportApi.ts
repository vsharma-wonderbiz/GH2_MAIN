import axios from "axios";
import { type ExportRequestPaylod } from "@/pages/Export";
import { toast } from "react-toastify"; // assuming you use react-toastify


export interface ExportRequestItem {
  exportRequestId: number;
  assetName: string;
  startTime: string;
  endTime: string;
  status: string;
  requestedBy: string;
  filePath: string;
  requestedAt: string;
  completedAt: string | null;
  requestedTags: string[] | null;
}

export interface ExportHistoryResponse {
  items: ExportRequestItem[];
  totalcounts: number;
  pageNumber: number;
  pageSize: number;
}

const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
  headers: {
    "Content-Type": "application/json",
  },
});

export const RequestExportData = async (
  payload: ExportRequestPaylod
): Promise<any> => {
  try {
    const res = await api.post("/Export", payload);
    toast.success("Export request successful ");
    return res.data;
  } catch (error: any) {
    console.error("Export request failed:", error);
    toast.error(
      error.response?.data?.message || "Export request failed "
    );
    
  }
};


export const getExportRequests = async (
  pageNumber: number,
  pageSize: number
): Promise<ExportHistoryResponse> => {
  const res = await api.get(`/Export`, {
    params: { PageNumber: pageNumber, PageSize: pageSize },
  });
  return res.data;
};

export const downloadExportFile = async (
  exportRequestId: number
): Promise<Blob> => {
  const res = await api.get(
    `/Export/${exportRequestId}/download`,
    { responseType: "blob" }
  );
  return res.data;
};
 