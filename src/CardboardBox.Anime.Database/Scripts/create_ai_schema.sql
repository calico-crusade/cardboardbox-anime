CREATE TABLE ai_requests (
	id BIGSERIAL PRIMARY KEY,
	profile_id BIGINT not null,

	image_2_image BIT not null,
	prompt TEXT not null,
	negative_prompt TEXT not null,
	steps BIGINT not null,
	batch_count BIGINT not null,
	batch_size BIGINT not null,
	cfg_scale DECIMAL not null,
	seed BIGINT not null,
	height BIGINT not null,
	width BIGINT not null,
	image_url TEXT,
	denoise_strength DECIMAL,

	output_paths TEXT[] not null,

	generation_start TIMESTAMP not null,
	generation_end TIMESTAMP not null,
	seconds_elapsed BIGINT not null,

	created_at TIMESTAMP not null default CURRENT_TIMESTAMP,
	updated_at TIMESTAMP not null default CURRENT_TIMESTAMP,
	deleted_at TIMESTAMP
)
