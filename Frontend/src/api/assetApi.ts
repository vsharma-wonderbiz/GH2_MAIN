import axios from "axios";


export interface Plant {
  assetId: number;
  name: string;
  assetType: string;
  parentAssetId: number | null;
  createdAt: string;
  mappings: any[];
}

export interface Stack {
  assetId: number;
  name: string;
  assetType: string;
  parentAssetId: number;
  createdAt: string;
  mappings: any[];
}

export interface Mapping {
  mappingId: number;
  assetName: string;
  tagName: string;
}

export interface PlantKpis{
    tagId: Number,
    tagName:String
}

export interface StackKpis{
    tagId: Number,
    tagName:String
}

// export interface AlertResponse{
//   id:number,
//   mappingId:number,
//   assetname:string,
//   signalName:string,
//   value:number,
//   alarmType:string,
//   status:string,
//   createdAt:string,
//   resolvedAt:string,
//   "mapping":number
// }



const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
  headers: {
    "Content-Type": "application/json",
  },
});

//Get Plants
export const getPlants = async (): Promise<Plant[]> => {
  const res = await api.get<Plant[]>("/Asset/plants");
  return res.data;
};

//Get Stacks
export const getStacksByPlant = async (plantId: number): Promise<Stack[]> => {
  const res = await api.get<Stack[]>(`/Asset/${plantId}/Stacks`);
  return res.data;
};

//Get Signals on Stack
export const getMappingsByStack = async (
  stackId: number
): Promise<Mapping[]> => {
  const res = await api.get<Mapping[]>(`/Asset/${stackId}/mappings`);
  return res.data;
};

export const GetAllPlantKpis = async (): Promise<PlantKpis[]> => {
  try {
    const res = await api.get<PlantKpis[]>('/Asset/PlantKpis');
    return res.data;
  } catch (error: any) {
    console.error("Error fetching Plant KPIs:", error);

    
    throw new Error(
      error?.response?.data?.message || "Failed to fetch Plant KPIs"
    );
  }
};

export const GetAllStackKpis = async (): Promise<StackKpis[]> => {
  try {
    const res = await api.get<StackKpis[]>('/Asset/StackKpis');
    return res.data;
  } catch (error: any) {
    console.error("Error fetching Plant KPIs:", error);


    throw new Error(
      error?.response?.data?.message || "Failed to fetch Plant KPIs"
    );
  }
};
