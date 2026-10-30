# CardboardBox.Anime.Patreon

This project contains the Patreon browser automation used by the CLI. It is the
only project with a direct `Microsoft.Playwright` package reference.

Each immediate child directory of the load root can contain a `meta.txt` file:

```text
Collection: https://www.patreon.com/collection/1234567
Author: ExampleAuthor
Home: https://www.patreon.com/cw/ExampleAuthor
Last Load: 2026-08-27T00:00:00
```

`Posts` can be used instead of `Collection`. When both `Home` and `Posts` are
present, the creator `Home` page is preferred because Patreon's `/posts` page
can return stale or non-chronological cards. Explicit `Collection` URLs retain
the highest priority. Discovered cards are sorted by numeric Patreon post ID
from newest to oldest before the existing-chapter boundary is applied.

Build the CLI and install the Playwright Chromium browser once:

```powershell
dotnet build src/CardboardBox.Anime.Cli/CardboardBox.Anime.Cli.csproj
powershell -ExecutionPolicy Bypass -File src/CardboardBox.Anime.Cli/bin/Debug/net10.0/playwright.ps1 install chromium
```

Then run:

```powershell
dotnet run --project src/CardboardBox.Anime.Cli --no-launch-profile -- patreon-scrape
```

The scraper stops immediately when its dedicated browser profile is signed out.
For the first run, opt into interactive login:

```powershell
dotnet run --project src/CardboardBox.Anime.Cli --no-launch-profile -- patreon-scrape --wait-for-login
```

This opens the dedicated profile in a normal, non-automated Chrome or Edge
process so Google OAuth does not reject the login. Finish signing into Patreon,
then close that browser completely; scraping starts afterward. The session is
retained under the user's local application-data directory, so subsequent runs
do not need that option. If browser detection fails, pass the executable path
with `--login-browser`. The
scraper writes the same `<h1>...</h1>` plus rich-text HTML structure consumed by
the existing `patreon` verb, then invokes that loader. `Last Load` advances only
after a series was scraped successfully.

Each series directory also receives a `.patreon-state.json` file containing the
Patreon post IDs already inspected. Later runs skip those post pages entirely,
write only newly discovered chapters, and invoke the database loader only for
the new HTML files. Existing manual `patreon` loads still process the complete
directory when explicitly requested.

An already installed browser may be used instead of downloaded Chromium:

```powershell
dotnet run --project src/CardboardBox.Anime.Cli --no-launch-profile -- patreon-scrape --browser-channel msedge
```
