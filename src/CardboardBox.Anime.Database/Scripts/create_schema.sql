-- CREATE image TYPE

CREATE TYPE image AS (
    width bigint,
    height bigint,
    type text,
    source text,
    platform_id text
);

-- CREATE extensio TYPE

CREATE TYPE ext AS (
    type text,
    value text
)

-- CREATE anime TABLE

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
    ext ext[] not null,

    created_at timestamp,
    updated_at timestamp,
    deleted_at timestamp
);

ALTER TABLE anime ADD COLUMN fts tsvector GENERATED ALWAYS AS ( to_tsvector('english', title) ) STORED;

CREATE INDEX fts_index ON anime USING GIN (fts);

CREATE UNIQUE INDEX anime_uiq ON anime (hash_id);

-- CREATE profiles TABLE

CREATE TABLE profiles (
    id BIGSERIAL PRIMARY KEY,

    username text not null,
    avatar text not null,
    platform_id text not null,
    admin bool not null,
    email text not null,
    provider text null,
    provider_id text null,
    settings_blob text not null DEFAULT '{}',

    created_at timestamp,
    updated_at timestamp,
    deleted_at timestamp,

    CONSTRAINT profiles_platform_id_uiq UNIQUE(platform_id)
);

-- CREATE lists TABLE
CREATE TABLE lists (
    id BIGSERIAL PRIMARY KEY,

    title text not null,
    description text,
    profile_id bigint not null,
    is_public bool not null,

    created_at timestamp,
    updated_at timestamp,
    deleted_at timestamp,

    CONSTRAINT fk_lists_profiles FOREIGN KEY(profile_id) REFERENCES profiles(id)
);

CREATE UNIQUE INDEX lists_uiq ON lists (title, profile_id);

-- CREATE list_map TABLE

CREATE TABLE list_map (
    id BIGSERIAL PRIMARY KEY,

    list_id bigint not null,
    anime_id bigint not null,

    created_at timestamp,
    updated_at timestamp,
    deleted_at timestamp,

    CONSTRAINT fk_list_map_list_id FOREIGN KEY(list_id) REFERENCES lists(id),
    CONSTRAINT fk_list_map_anime_id FOREIGN KEY(anime_id) REFERENCES anime(id)
);

CREATE UNIQUE INDEX list_map_uiq ON list_map (list_id, anime_id);

-- CREATE light_novels TABLE
CREATE TABLE light_novels (
    id BIGSERIAL PRIMARY KEY,

    hash_id text not null,
    book_id text not null,
    book text not null,
    chapter text not null,
    content text not null,
    url text not null,
    next_url text,
    ordinal bigint not null,

    created_at timestamp,
    updated_at timestamp,
    deleted_at timestamp
);

CREATE UNIQUE INDEX light_novels_uiq ON light_novels (book_id, hash_id);

CREATE TABLE discord_guild_settings (
     id BIGSERIAL PRIMARY KEY,

     guild_id text not null,
     authed_users text[] not null default '{}',
     enable_lookup boolean not null default false,
     enable_theft boolean not null default false,
     manga_updates_channel text,
     manga_updates_ids text[] not null default '{}',
     manga_updates_nsfw boolean not null default false,

     created_at timestamp not null default CURRENT_TIMESTAMP,
     updated_at timestamp not null default CURRENT_TIMESTAMP,
     deleted_at timestamp,

     CONSTRAINT uiq_discord_guild_settings_guild_id UNIQUE(guild_id)
);