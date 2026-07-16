import {
  Table,
  TableBody,
  TableCaption,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import type { ExportRequestItem } from "@/api/exportApi";
import { formatIsoTimestamp } from "@/utils/time";

interface ExportHistoryTableProps {
  exportData: ExportRequestItem[];
  onDownload: (item: ExportRequestItem) => void;
  downloadingId?: number | null;
}

export function ExportHistoryTable({
  exportData,
  onDownload,
  downloadingId,
}: ExportHistoryTableProps) {
  return (
    <Table>
      <TableCaption>Recent Exports</TableCaption>

      <TableHeader>
        <TableRow>
          <TableHead>Asset Name</TableHead>
          <TableHead>Start Time</TableHead>
          <TableHead>End Time</TableHead>
          <TableHead>Status</TableHead>
          <TableHead>Requested At</TableHead>
          <TableHead>Completed At</TableHead>
          <TableHead>Download</TableHead>
        </TableRow>
      </TableHeader>

      <TableBody>
        {exportData?.map((e) => (
          <TableRow key={e.exportRequestId}>
            <TableCell>{e.assetName}</TableCell>
            <TableCell>{formatIsoTimestamp(e.startTime)}</TableCell>
            <TableCell>{formatIsoTimestamp(e.endTime)}</TableCell>
            <TableCell>
              <div
                className={
                  e.status === "Completed"
                    ? "bg-green-300 text-green-800 px-2 py-1 rounded"
                    : e.status === "Failed"
                      ? "bg-red-100 text-red-800 px-2 py-1 rounded"
                      : "bg-gray-100 text-gray-800 px-2 py-1 rounded"
                }
              >
                {e.status}
              </div>
            </TableCell>
            <TableCell>{formatIsoTimestamp(e.requestedAt)}</TableCell>
            <TableCell>
              {e.completedAt ? formatIsoTimestamp(e.completedAt) : "-"}
            </TableCell>
            <TableCell>
              {e.status === "Completed" ? (
                <button
                  onClick={() => onDownload(e)}
                  disabled={downloadingId === e.exportRequestId}
                  className="rounded-lg border border-gray-300 px-3 py-1 text-sm text-blue-600 hover:bg-blue-50 disabled:cursor-not-allowed disabled:opacity-50 dark:border-gray-600 dark:text-blue-400 dark:hover:bg-gray-700"
                >
                  {downloadingId === e.exportRequestId ? "Downloading..." : "Download"}
                </button>
              ) : (
                <span className="text-sm text-gray-400">-</span>
              )}
            </TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  );
}