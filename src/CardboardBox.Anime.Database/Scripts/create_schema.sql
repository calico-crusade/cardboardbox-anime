CREATE TYPE image AS (
    width bigint,
    height bigint,
    type text,
    source text,
    platform_id text
);

CREATE TABLE anime (
    id BIGSERIAL PRIMARY KEY,

    hash_id text not null,
    anime_id text not null,
    link text not null,
    title text not null,
    description text not null,
    platform_id text not null,
    type text not null,
    mature bool not null,
    languages text[] not null,
    language_types text[] not null,
    ratings text[] not null,
    tags text[] not null,
    images image[] not null,

    created_at timestamp,
    updated_at timestamp,
    deleted_at timestamp
);

ALTER TABLE anime ADD COLUMN fts tsvector GENERATED ALWAYS AS ( to_tsvector('english', title) ) STORED;

CREATE INDEX fts_index ON anime USING GIN (fts);

CREATE UNIQUE INDEX anime_uiq ON anime (hash_id);