import {
  Table,
  TableBody,
  TableCaption,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import type { AlertResponse, RecommendationResponse } from "@/api/analyticsApi"
import { formatIsoTimestamp } from "@/utils/time";

export default function RecommendationTable({ data }:{data:RecommendationResponse[]}) {
  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>Asset Name</TableHead>
          <TableHead>Signal Name</TableHead>
          <TableHead>Value</TableHead>
          <TableHead>Message</TableHead>
          <TableHead>Triggered At</TableHead>
        </TableRow>
      </TableHeader>

      <TableBody>
        {data.map((item) => (
          <TableRow key={item.id}>
            <TableCell>{item.assetName}</TableCell>
            <TableCell>{item.signalName}</TableCell>
            <TableCell>{item.currentVal}</TableCell>
            <TableCell>{item.message}</TableCell>
            <TableCell>{formatIsoTimestamp(item.createdAt)}</TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  );
}