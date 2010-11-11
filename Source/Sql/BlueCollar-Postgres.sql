DROP TABLE IF EXISTS "blue_collar" CASCADE;
CREATE TABLE "blue_collar"
(
	"id" serial NOT NULL,
	"name" character varying(128) NOT NULL,
	"type" character varying(512) NOT NULL,
	"data" text NOT NULL,
	"status" character varying(24) NOT NULL,
	"exception" text NULL,
	"queue_date" timestamp without time zone NOT NULL,
	"start_date" timestamp without time zone NULL,
	"finish_date" timestamp without time zone NULL,
	"schedule_name" character varying(128) NULL,
	"try_number" boolean NOT NULL,
	CONSTRAINT "tasty_job_pkey" PRIMARY KEY("id")
)
WITHOUT OIDS;
ALTER TABLE "blue_collar" OWNER TO blue_collar;