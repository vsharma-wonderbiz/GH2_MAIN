select * from "MappingTables"
   where "MappingId"=23
  
select * from "Tags"

select * from "NodeLastDatas"



select * from "WeeklyAvgData"

select * from "Tags"

select * from "SensorRawDatas"
where "MappingId"=1
      AND "TimeStamp" between '2026-02-14' AND '2026-02-21'


SELECT *
FROM "SensorRawDatas"
WHERE "MappingId" = 27	
  AND "TimeStamp"::date = '2026-02-28'


select * from "SensorRawDatas"
where "TagName"='power'
order by "TimeStamp" '2026-01-23' AND '2026-02-27'

select * from "SensorRawDatas"
where "AssetName"='Plant_1'

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
