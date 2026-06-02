import type { AnalyticsResponse } from "@/api/analyticsApi";

export const getTimeRange = (range: string) => {
  const now = new Date();
  let start = new Date();

  switch (range) {
    case "1h":
      start.setHours(now.getHours() - 1);
      break;

    case "24h":
      start.setDate(now.getDate() - 1);
      break;

    case "7d":
      start.setDate(now.getDate() - 7);
      break;

    case "30d":
      start.setDate(now.getDate() - 30);
      break;

    default:
      return null;
  }

  return {
    startTime: start.toISOString(),
    endTime: now.toISOString(),
  };
};

export const formatChartData = (
  data: any[],   // array of stack objects
  timeRange: "1h" | "24h" | "7d" | "30d" | "custom"
): any[] => {
  console.log(data);
  return data.flatMap((stackObj) => {
    console.log(stackObj);

    return stackObj.values.map((item:any) => {
      const date = new Date(item.timeStamp);

      let formattedTime = "";
      if (timeRange === "1h" || timeRange === "24h") {
        formattedTime = date.toLocaleTimeString([], {
          hour: "2-digit",
          minute: "2-digit",
        });
      } else {
        formattedTime = date.toLocaleString([], {
          day: "2-digit",
          month: "short",
          hour: "2-digit",
          minute: "2-digit",
        });
      }

      return {
        stackName: stackObj.asseName,   
        tagName: stackObj.tagName,
        time: formattedTime,
        value: item.value ?? 0,
      };
    });
  });
};



export function formatIsoTimestamp(
  isoString: string,
  locale: string = "en-IN" // default to Indian locale, can override
): string {
  if (!isoString) return "";

  const date = new Date(isoString);

  const options: Intl.DateTimeFormatOptions = {
    year: "numeric",
    month: "short",   // "Apr"
    day: "numeric",   // "7"
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
    hour12: true      // 12-hour format with AM/PM
  };

  return date.toLocaleString(locale, options);
}

export const toIST = (utcDateString: string): string => {
  const date = new Date(utcDateString); // parse incoming string
  const istOffsetMinutes = 330; // IST = UTC + 5h30m
  const istDate = new Date(date.getTime() + istOffsetMinutes * 60 * 1000);
  return istDate.toISOString(); // return ISO string
};


export function formatDateToShort(input: string): string {
  const date = new Date(input);
  const day = date.getUTCDate();
  const month = date.getUTCMonth() + 1; // months are 0-based
  const year = date.getUTCFullYear() % 100; // last two digits
  return `${day}/${month}/${year}`;
}