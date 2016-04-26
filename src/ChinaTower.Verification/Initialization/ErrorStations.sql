CREATE OR REPLACE VIEW "ErrorStations" AS 
	 SELECT "Result"."Number",
		"Result"."Name",
		"Result"."Time",
		"Result"."City",
		"Result"."District",
		"Result"."Count"
	   FROM ( SELECT "Stations"."UniqueKey" AS "Number",
				"Stations"."Name",
				"Stations"."VerificationTime" AS "Time",
				"Stations"."City",
				"Stations"."District",
				( SELECT count(*) AS count
					   FROM "Form"
					  WHERE ((("Form"."Status" = 0) AND (("Form"."Type" = 17) AND ("Stations"."UniqueKey" = "Form"."UniqueKey"))) OR (("Form"."Type" <> 17) AND ("Stations"."UniqueKey" = ("Form"."StationKey")::text)))) AS "Count"
			   FROM "Form" "Stations"
			  WHERE ("Stations"."Type" = 17)) "Result"
	  WHERE ("Result"."Count" > 0)
	  ORDER BY "Result"."City", "Result"."District", "Result"."Time";