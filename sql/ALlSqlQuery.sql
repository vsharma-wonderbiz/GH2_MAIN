




show timezone

ALTER DATABASE "GH2-Main" SET timezone TO 'UTC'

SET timezone = 'UTC';

explain analyse select * from "SensorRawDatas" limit 1;
select * from "SensorRawDatas";
explain analyse select * from "SensorRawDatas" where "MappingId"=24  Or "MappingId"=19;

SELECT  *
FROM "SensorRawDatas"
WHERE "AssetName" = 'Plant_1'
  AND "TagName" = 'power'
  AND "TimeStamp" BETWEEN '2026-04-24T05:25:00Z'	
                      AND '2026-04-24T06:27:00Z'
ORDER BY "TimeStamp" DESC;


SELECT  
    "AssetName",
    "TagName",
    AVG("Value") AS AvgValue,
    MIN("Value") AS MinValue,
    MAX("Value") AS MaxValue
FROM "SensorRawDatas"
WHERE "MappingId"=1
  AND "TimeStamp" BETWEEN '2026-04-27T04:54:48.5116817Z'
                      AND '2026-04-27T05:54:48.5116817Z'
GROUP BY "AssetName", "TagName";


SELECT *
FROM "SensorRawDatas"
WHERE "MappingId"=98
  AND "TimeStamp" BETWEEN '2026-04-27T05:09:40.2654923Z'
                      AND '2026-04-27T06:09:40.2654923Z'



---- index on the analytics data api point ----
CREATE INDEX idx_analytics_data
ON "SensorRawDatas" ("AssetName","TagName");


----- these the command to check what all are the index that is used to acess the data fast -----

SELECT *
FROM "SensorRawDatas"
ORDER BY "TimeStamp" DESC
LIMIT 10;


SELECT *
FROM "SensorRawDatas"
WHERE "TimeStamp" BETWEEN '2026-04-21T09:35:00Z'
                      AND '2026-04-21T10:34:00Z'
ORDER BY "TimeStamp" DESC;



UPDATE "Tags"
SET "DataType" = 'float32'
WHERE "TagId" = 20;

select * from "ProtocolConfig"

select * from "MappingTables"
   where "MappingId"=98


select * From "Users"

select * from "RegisterAddress"
   where "MappingId"=135
  
select * from "TagTypes"


select * from "Alarms"
order by "CreatedAt" Desc

SELECT column_name, data_type, is_nullable, column_default
FROM information_schema.columns
WHERE table_name = 'SensorRawDatas'



select * from "TagTypes"


select * from "Assets"

select * from "ProtocolConfig"
     where "MappingId"=74

select * from "NodeLastDatas"
  where "TagName"='warning_exists'

select * from "KpiTable"
where "AssetName"='Stack_1' and "KpiName"='stack_specific_energy'



SELECT 
    m."MappingId",
    a."Name" AS asset_name,
    t."TagName" AS tag_name,
	m."OpcNodeId" AS opc_node_id,
    t."DataType" AS data_type ,
    pc."SlaveId",
    pc."RegisterAddress",
    pc."FunctionCode",
    pc."RegisterCount",
    (pc."RegisterAddress" - 40001) AS adjusted_register,
    ROUND(t."Deadband"::numeric, 4) AS roundeddeadband
FROM "MappingTables" m
JOIN "Assets" a 
    ON m."AssetId" = a."AssetId"
JOIN "Tags" t
    ON m."TagId" = t."TagId"
JOIN "ProtocolConfig" pc 
    ON pc."MappingId" = m."MappingId"
WHERE t."IsDerived" = false
ORDER BY a."Name" , t."TagName";

SELECT 
    pc."Id",
    m."MappingId",
    a."Name" AS asset_name,
    t."TagName" AS tag_name,
	m."OpcNodeId" AS opc_node_id,
    t."DataType" AS data_type ,
    pc."SlaveId",
    pc."RegisterAddress",
    pc."FunctionCode",
    pc."RegisterCount",
    (pc."RegisterAddress" - 40001) AS adjusted_register,
    ROUND(t."Deadband"::numeric, 4) AS rounded_deadband
FROM "MappingTables" m
JOIN "Assets" a 
    ON m."AssetId" = a."AssetId"
JOIN "Tags" t
    ON m."TagId" = t."TagId"
JOIN "ProtocolConfig" pc 
    ON pc."MappingId" = m."MappingId"
WHERE t."IsDerived" = false
ORDER BY pc."Id" asc ;

-- these is bsaically checking if the value is been presnet in the tbale or not
SELECT *
FROM "WeeklyAvgData"
WHERE "IsFinal"='False'
WHERE "MappingId" = 1
  AND "WeekStartDate" <= '2026-04-27 00:00:00+00'
  AND "WeekEndDate" >= '2026-04-20 00:00:00+00';

--kpi table to chec if the value is avaliable or not 
  SELECT *
FROM "KpiTable"
WHERE "StartTime" <= '2026-04-26 00:00:00+00'
  AND "EndTime" >= '2026-04-20 00:00:00+00';

select * from "WeeklyAvgData"
  where "MappingId"=66
        AND "TimeStamp" between '2026-02-10T09:57:47.2045756Z' AND '2026-03-04T09:57:47.2045756Z'

select * from "Tags"
where "TagTypeId"=2 AND "IsDerived"='true'

SELECT * 
FROM "SensorRawDatas"
WHERE "AssetName" = 'Stack_1'
  AND "TagName" = 'current'
  AND "TimeStamp" >= '2026-04-07 11:30:00'
AND "TimeStamp" <=  '2026-04-07 12:30:00';

---latest one hour of data ----
SELECT * 
FROM "SensorRawDatas"
WHERE "AssetName" = 'Stack_1'
  AND "TagName" = 'current'
  AND "TimeStamp" >= NOW() - INTERVAL '1 hour'
  AND "TimeStamp" <= NOW()
  Order by "TimeStamp" desc


ALTER DATABASE "GH2-Main" SET timezone TO 'UTC';

SHOW timezone

SET TIME ZONE 'UTC';

SELECT * 
FROM "SensorRawDatas"
WHERE "AssetName" = 'Stack_1'
  AND "TagName" = 'current'
  order by "TimeStamp" desc
  


SELECT *
FROM "SensorRawDatas"
WHERE "MappingId" = 27	
  AND "TimeStamp"::date = '2026-02-28'

SHOW TIMEZONE;
SELECT NOW();
SELECT MAX(ts), MIN(ts) FROM "SensorRawDatas"





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
    m.mappingid,
    a.name AS asset_name,
    t.tagname,
    t.datatype,
    pc.slaveid,
    pc.registeraddress,
    pc.functioncode,
    pc.registercount,
    -- mirror your C# logic
    (pc.registeraddress - 40001) AS adjusted_register,
    ROUND(t.deadband::numeric, 4) AS rounded_deadband
FROM "MappingTable" as m
JOIN assets a 
    ON m.assetid = a.assetid
JOIN tag t 
    ON m.tagid = t.tagid
JOIN protocolconfig pc 
    ON pc.mappingid = m.mappingid
WHERE t.isderived = false
ORDER BY a.name, t.tagname;




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

SELECT *
FROM "SensorRawDatas"
WHERE "TimeStamp" >= NOW() - INTERVAL '6 hours';


SELECT "TimeStamp" 
FROM "SensorRawDatas"
ORDER BY "TimeStamp" DESC
LIMIT 30;


SELECT column_name, data_type
FROM information_schema.columns
WHERE table_name = 'SensorRawDatas';



delete from "SensorRawDatas"
delete from "WeeklyAvgData"
delete from "NodeLastDatas"
delete from "MappingTables"

delete from "ProtocolConfig"

delete from "KpiTable"

delete from "Assets"

delete from "Tags"

delete from "Alarms"

delete from "TagTypes"
