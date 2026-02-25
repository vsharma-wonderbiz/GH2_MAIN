select * from "Mappings"

select * from "SensorRawDatas"
where "MappingId"=1;

select * from "SensorRawDatas"

select * from "SensorRawDatas"
where "AssetName"='Plant_1'





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
  AND "TimeStamp" BETWEEN '2026-01-15' AND '2026-01-31'
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




