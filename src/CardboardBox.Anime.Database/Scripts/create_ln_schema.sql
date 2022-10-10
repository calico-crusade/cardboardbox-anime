-- CREATE ln_series TABLE
CREATE TABLE ln_series (
    id BIGSERIAL PRIMARY KEY,
	
    hash_id text not null,
    title text not null,
    url text not null,
    last_chapter_url text not null,

    description text,
    image text,

    tags text[] not null default '{}',
    genre text[] not null default '{}',

    authors text[] not null default '{}',
    illustrators text[] not null default '{}',
    editors text[] not null default '{}',
    translators text[] not null default '{}',
	
    created_at timestamp not null default CURRENT_TIMESTAMP,
    updated_at timestamp not null default CURRENT_TIMESTAMP,
    deleted_at timestamp,

    CONSTRAINT uiq_ln_series_hash UNIQUE(hash_id)
);

-- CREATE ln_books TABLE
CREATE TABLE ln_books (
    id BIGSERIAL PRIMARY KEY,
	
    hash_id text not null,
    title text not null,
    ordinal bigint not null,
    
    series_id bigint not null references ln_series(id),
    cover_image text,
    forwards text[] not null default '{}',
    inserts text[] not null default '{}',

    authors text[] not null default '{}',
    illustrators text[] not null default '{}',
    editors text[] not null default '{}',
    translators text[] not null default '{}',
	
    created_at timestamp not null default CURRENT_TIMESTAMP,
    updated_at timestamp not null default CURRENT_TIMESTAMP,
    deleted_at timestamp,
   
    CONSTRAINT uiq_ln_books_hash UNIQUE(hash_id, series_id)
);

-- CREATE ln_chapters TABLE
CREATE TABLE ln_chapters (
    id BIGSERIAL PRIMARY KEY,
	
    hash_id text not null,
    title text not null,
    ordinal bigint not null,

    book_id bigint not null references ln_books(id),
	
    created_at timestamp not null default CURRENT_TIMESTAMP,
    updated_at timestamp not null default CURRENT_TIMESTAMP,
    deleted_at timestamp,

    CONSTRAINT uiq_ln_chapters_hash UNIQUE (hash_id, book_id)
);

-- CREATE ln_pages TABLE
CREATE TABLE ln_pages (
    id BIGSERIAL PRIMARY KEY,
	
    hash_id text not null,
    title text not null,
    ordinal bigint not null,

    series_id bigint not null references ln_series(id),
    url text not null,
    next_url text,

    content text not null,
    mimetype text not null,
	
    created_at timestamp not null default CURRENT_TIMESTAMP,
    updated_at timestamp not null default CURRENT_TIMESTAMP,
    deleted_at timestamp,

    CONSTRAINT uiq_ln_pages_hash UNIQUE(hash_id, series_id)
);

-- CREATE ln_chapter_pages TABLE
CREATE TABLE ln_chapter_pages (
    id BIGSERIAL PRIMARY KEY,

    chapter_id bigint not null references ln_chapters(id),
    page_id bigint not null references ln_pages(id),
    ordinal bigint not null,

    created_at timestamp not null default CURRENT_TIMESTAMP,
    updated_at timestamp not null default CURRENT_TIMESTAMP,
    deleted_at timestamp,

    CONSTRAINT uiq_ln_chapter_pages UNIQUE (chapter_id, page_id)
);