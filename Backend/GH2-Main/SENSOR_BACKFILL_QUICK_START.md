# Sensor Data Backfill - Quick Start

## Overview
Temporary service to backfill `SensorRawData` table with 1-second interval entries using Modbus-like simulation logic.

**Features:**
- Backfill by Asset name (includes all tags for that asset)
- 1 month of data: from current date - 30 days to current date
- 1 entry per second
- Modbus simulation logic for realistic value generation
- Bulk insert for fast performance (10K records per batch)
- Uses Tag MinValue/MaxValue for realistic ranges

## Setup

### 1. Register Service in Program.cs
```csharp
// Add this in Program.cs before app.Run();
builder.Services.AddScoped<BackfillSensorDataService>();
```

### 2. Ensure DbSet Exists in ApplicationDbContext
```csharp
public DbSet<SensorRawData> SensorRawDatas { get; set; }
```

## API Endpoints

### Backfill Asset (Past Month - 1 entry per second)
```
POST /api/backfilldata/backfill/asset/{assetName}
```
**Example:**
```
POST /api/backfilldata/backfill/asset/Plant_1
```

✓ Backfills all tags for that asset
✓ Uses current date back 30 days
✓ Uses Tag min/max ranges from tag seed

Response:
```json
{ "message": "Backfill completed for asset: Plant_1" }
```

### Backfill Asset (Custom Date Range)
```
POST /api/backfilldata/backfill/asset/{assetName}/range?startDate=2026-01-24T00:00:00Z&endDate=2026-02-24T23:59:59Z
```

### Get Statistics
```
GET /api/backfilldata/stats
```

Response:
```json
{
  "totalMappings": 22,
  "totalSensorRecords": 2592000,
  "earliestSensorTime": "2026-01-24T10:30:00Z",
  "latestSensorTime": "2026-02-24T14:45:00Z"
}
```

### Clear All Data (DANGEROUS!)
```
DELETE /api/backfilldata/clear/all
```

### Clear Data for Specific Asset
```
DELETE /api/backfilldata/clear/asset/{assetName}
```

## How It Works

1. **Get Asset** by name from database
2. **Get All Tags** for that asset via Mappings
3. **For Each Tag:**
   - Get min/max range from Tag entity
   - Generate 1 second interval data from startDate to endDate
   - Use Modbus-like simulation logic:
     - Start with midpoint value (min + max) / 2
     - Add random delta (±2% of range) each second
     - Clamp values to min-max range
     - Round to 2 decimal places
4. **Bulk Insert** every 10,000 records for speed
5. **Display Progress** in console

## Example Backfill Flow

```
Asset: Plant_1
├─ Tag: voltage (min: 200V, max: 250V)
│  └─ Generate 2,592,000 records (1 per second × 30 days)
├─ Tag: current (min: 0A, max: 100A)
│  └─ Generate 2,592,000 records
└─ Tag: power (min: 1000W, max: 2000W)
   └─ Generate 2,592,000 records

Total: ~7.8 million records for Plant_1
```

## Performance

- **Records per month (per tag):** ~2.6 million (86,400 sec/day × 30 days)
- **Batch size:** 10,000 records per insert
- **Time:** 2-5 minutes per asset (depending on tag count and server)
- **Memory:** Efficient batching prevents memory overflow

## Console Output Example

```
================================================================================
BACKFILL: Asset 'Plant_1' | Range: 2026-01-24 to 2026-02-24
================================================================================

✓ Found asset: Plant_1 (AssetId: 1)
✓ Found 3 tags for this asset

[1/3] Processing Tag: voltage
  Range: 200 - 250
    → Inserted 500000 records...
    → Inserted 1000000 records...
  ✓ Inserted 2592000 records

[2/3] Processing Tag: current
  Range: 0 - 100
    → Inserted 500000 records...
  ✓ Inserted 2592000 records

[3/3] Processing Tag: power
  Range: 1000 - 2000
    → Inserted 500000 records...
  ✓ Inserted 2592000 records

================================================================================
✓ BACKFILL COMPLETED!
  Asset: Plant_1
  Total Records: 7776000
  Date Range: 2026-01-24T00:00:00Z to 2026-02-24T23:59:59Z
================================================================================
```

## Examples

### Example 1: Backfill Plant_1
```bash
curl -X POST http://localhost:5000/api/backfilldata/backfill/asset/Plant_1
```

### Example 2: Backfill Stack_1 for Custom Range
```bash
curl -X POST "http://localhost:5000/api/backfilldata/backfill/asset/Stack_1/range?startDate=2026-02-10T00:00:00Z&endDate=2026-02-24T23:59:59Z"
```

### Example 3: Check Progress
```bash
curl -X GET http://localhost:5000/api/backfilldata/stats
```

### Example 4: Clear Plant_1 Data
```bash
curl -X DELETE http://localhost:5000/api/backfilldata/clear/asset/Plant_1
```

## Important Notes

⚠️ **Asset Names are Case-Sensitive** - Use exact name from database

⚠️ **No Duplicate Prevention** - Clear old data before re-running

⚠️ **Requires Tag Min/Max Values** - Ensure all tags have MinValue and MaxValue set

⚠️ **Run During Off-Peak Hours** - Heavy database operations

⚠️ **Backup First** - Always backup database before bulk operations!

## Cleanup After Backfill

Once backfill is complete, delete these temporary files:
1. `BackfillSensorDataService.cs` (Infrastructure/Services)
2. `BackfillDataController.cs` (Controllers)
3. Remove service registration from Program.cs
4. Delete this documentation file
