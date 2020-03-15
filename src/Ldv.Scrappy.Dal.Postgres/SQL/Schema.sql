CREATE TABLE public.ruledata
(
    id          serial    NOT NULL,
    ruleid      text      NOT NULL,
    "timestamp" timestamp NOT NULL,
    value       text      NOT NULL,
    CONSTRAINT scrapperdata_pkey PRIMARY KEY (id),
    CONSTRAINT scrapperdata_un UNIQUE (ruleid, "timestamp")
);