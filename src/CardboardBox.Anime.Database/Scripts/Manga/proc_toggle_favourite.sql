CREATE OR REPLACE function toggle_favourite(platformId text, mangaId bigint) RETURNS integer
AS $$
DECLARE profileId bigint;
BEGIN
    profileId := (
        SELECT
            id
        FROM profiles
        WHERE platform_id = platformId
    );

    IF (profileId IS NULL) THEN RETURN -1; END IF;

    IF EXISTS (
        SELECT 1
        FROM manga_favourites
        WHERE profile_id = profileId AND manga_id = mangaId
    ) THEN
        DELETE FROM manga_favourites WHERE profile_id = profileId AND manga_id = mangaId;
        RETURN 0;
    ELSE
        INSERT INTO manga_favourites (profile_id, manga_id) VALUES (profileId, mangaId);
        RETURN 1;
    END IF;
END
$$
LANGUAGE plpgsql;