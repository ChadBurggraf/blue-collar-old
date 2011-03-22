DROP TABLE IF EXISTS "blue_collar" CASCADE;
CREATE TABLE "blue_collar"
(
	"id" serial NOT NULL,
	"name" character varying(128) NOT NULL,
	"job_type" character varying(512) NOT NULL,
	"data" text NULL,
	"status" character varying(24) NOT NULL,
	"exception" text NULL,
	"queue_date" timestamp without time zone NOT NULL,
	"start_date" timestamp without time zone NULL,
	"finish_date" timestamp without time zone NULL,
	"schedule_name" character varying(128) NULL,
	"try_number" int NOT NULL,
	CONSTRAINT "tasty_job_pkey" PRIMARY KEY("id")
)
WITHOUT OIDS;
ALTER TABLE "blue_collar" OWNER TO blue_collar;
CREATE INDEX "blue_collar_queue_date_status_idx" ON "blue_collar"("queue_date", "status");
