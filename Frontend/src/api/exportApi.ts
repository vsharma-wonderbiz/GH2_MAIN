import axios from "axios";
import { type ExportRequestPaylod } from "@/pages/Export";
import { toast } from "react-toastify"; // assuming you use react-toastify

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
