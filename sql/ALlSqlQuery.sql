
select * from "MappingTables"
   where "MappingId"=135
  
select * from "Tags"

select * from "Assets"

select * from "ProtocolConfig"

select * from "NodeLastDatas"	

select * from "KpiTable"
where "KpiValue"=


-- these is bsaically checking if the value is been presnet in the tbale or not
SELECT *
FROM "WeeklyAvgData"
WHERE "MappingId" = 66
  AND "WeekStartDate" <= '2026-03-04T09:57:47.2045756Z'
  AND "WeekEndDate" >= '2026-02-10T09:57:47.2045756Z';

--kpi table to chec if the value is avaliable or not 
  SELECT *
FROM "KpiTable"
WHERE "KpiName" = 'stack_specific_energy'
  AND "StartTime" <= '2026-03-04T09:57:47.2045756Z'
  AND "EndTime" >= '2026-02-10T09:57:47.2045756Z';





select * from "WeeklyAvgData"
  where "MappingId"=66
        AND "TimeStamp" between '2026-02-10T09:57:47.2045756Z' AND '2026-03-04T09:57:47.2045756Z'

select * from "Tags"

select * from "SensorRawDatas"
where "MappingId"=5
      AND "TimeStamp" between '2026-02-' AND '2026-02-21'


SELECT *
FROM "SensorRawDatas"
WHERE "MappingId" = 27	
  AND "TimeStamp"::date = '2026-02-28'


select * from "SensorRawDatas"
where "TagName"='power'
order by "TimeStamp" '2026-01-23' AND '2026-02-27'

select * from "SensorRawDatas"
where "AssetName"='Plant_1'

select * from "ProtocolConfig"

SELECT * FROM pg_extension;


select * from "TagTypes"


-- to fetch the telemntry from a timestamp to a timestamp
select * from "SensorRawDatas"
where "AssetName"='Plant_1'
      AND "TimeStamp" BETWEEN '2026-01-15' AND '2026-01-31'


-- these is the qurey to fetch the  raw sensor data with aggreate of 1 minute of asset=Planyt_1
-- and tag=Power
-- Round is done to restirc only 2 ponits after the decimal
SELECT 
    date_trunc('minute', "TimeStamp") AS minute_bucket,
    round(AVG("Value")::numeric,2) AS avg_value,
    "AssetName" AS AssetName,
    "TagName" AS TagName
FROM "SensorRawDatas"
WHERE "AssetName" = 'Plant_1' 
  AND "TagName" = 'power'
  AND "TimeStamp" BETWEEN '2026-01-15 16:20:00' AND '2026-01-31 16:00:00'
GROUP BY minute_bucket, "AssetName", "TagName"
ORDER BY minute_bucket;

SHOW TIMEZONE;

SET TIME ZONE 'UTC';

-- these is the qurey to fetch the  raw sensor data with aggreate of 1 minute of asset=Planyt_1
-- and all tags 
SELECT 
    date_trunc('minute', "TimeStamp") AS minute_bucket,
    AVG("Value") AS avg_value,
    "AssetName" AS AssetName,
    "TagName" AS TagName
FROM "SensorRawDatas"
WHERE "AssetName" = 'Plant_1' 
  AND "TimeStamp" BETWEEN '2026-01-15' AND '2026-01-31'
GROUP BY minute_bucket, "AssetName", "TagName"
ORDER BY minute_bucket;


-- using timesacle db by installing the extensiona and using the time_bucket
SELECT 
    time_bucket('15 minute', "TimeStamp") AS minute_bucket,
    ROUND(AVG("Value")::numeric, 2) AS avg_value,
    "AssetName",
    "TagName"
FROM "SensorRawDatas"
WHERE "AssetName" = 'Stack_1'
  AND "TagName" = 'current'
  AND "TimeStamp" >= '2026-02-10 10:00:00'
  AND "TimeStamp" <  '2026-02-24 16:00:00'
GROUP BY minute_bucket, "AssetName", "TagName"
ORDER BY minute_bucket;	



SELECT 
    AVG("Value") AS Avg,
    MIN("Value") AS Min,
    MAX("Value") AS Max,
    COUNT(*) AS Count
FROM "SensorRawDatas"
WHERE "MappingId" = 
  AND "TimeStamp"::Date >= '2026-02-08'
  AND "TimeStamp"::Date <= '2026-02-14';


select * from "WeeklyAvgData"
   where "AssetId"=2 and "MappingId"=31



delete from "SensorRawDatas"
delete from "WeeklyAvgData"
delete from "NodeLastDatas"
delete from "MappingTables"

delete from "ProtocolConfig"

delete from "KpiTable"

delete from "Assets"

delete from "Tags"


delete from "TagTypes"
