# Cardboard Box | Anime, Manga, Novels & AI
[Live Site](https://cba.index-0.com/) - If my server is running that is. 

## Features:
* [Anime](https://cba.index-0.com/anime)
  * Search for anime from: [VRV](https://vrv.co), [HIDIVE](https://hidive.com), [Crunchyroll](https://crunchyroll.com), and [Funimation](https://funimation.com) in one place
  * Sort all anime into lists
  * Share lists with friends or the public
  * Search for anime via the discord bot
* [Manga](https://cba.index-0.com/manga)
  * Read Manga from:
    * [MangaDex.org](https://mangadex.org)
    * [mangakakalot.tv](https://ww4.mangakakalot.tv/)
  * Keep track of your progress while reading
  * Customize the manga reading experience with different views and even a blue-light filter for late night binges
  * Read manga via the discord bot
* [Light Novels](https://cba.index-0.com/series)
  * Read Light Novels from:
    * [lightnovelpub.com](https://www.lightnovelpub.com)
    * [re-library.com](https://re-library.com)
    * [scribblehub.com](https://www.scribblehub.com)
  * Generate ebooks (epubs) from any of the novels
* [AI Image Generation](https://cba.index-0.com/ai)
  * Text to image generation
  * Image to image generation
* [Mobile App (PWA)](https://cba.index-0.com/install)
* [Discord Bot](https://discord.com/api/oauth2/authorize?client_id=905632533981577247&permissions=8&scope=bot%20applications.commands)
  * Everything uses slash commands!
  * Search for your favourite anime - `/anime`
  * Search and read your favourite manga - `/manga`
  * Generate images from a text prompt (AI txt2img) - `/ai`
  * Search for your favourite holy book! - `/holybook`
* Full Open Graph link preview support

## Tech Stack
* [Backend](https://github.com/calico-crusade/cardboardbox-anime/tree/main/src/CardboardBox.Anime.Api): asp.net core 6.0 (C#)
* [Frontend](https://github.com/calico-crusade/cardboardbox-anime/tree/main/ui): Angular 14.0 (TypeScript, SCSS, HTML)
* Database: postgres & mongodb
* [Discord Bot](https://github.com/calico-crusade/cardboardbox-anime/tree/main/src/CardboardBox.Anime.Bot.Cli): Discord.net & CardboardBox.Discord (C#)
* CI / CD: Github workflows, docker & nginx

## Hosting your own instance
In order to host your own instance of cardboardbox-anime you will need to have docker installed.
Then you can follow these instructions:
1. Download [docker-compose.yml](https://github.com/calico-crusade/cardboardbox-anime/blob/main/docker-compose.yml)
2. Download [deploy.sh](https://github.com/calico-crusade/cardboardbox-anime/blob/main/deploy.sh)
3. Create a `.env` file and fill out the following variables:
  * `MONGO_CON_URL=mongodb://<user>:<password>@<host>` - Your hosted instance of MongoDB (this is not in the docker compose)
  * `POSTGRES_HOST=<database>` - The database name you want to use for the postgres database
  * `POSTGRES_USER=<username>` - The username you want to use for the postgres database
  * `POSTGRES_PASS=<password>` - The password you want to use for the postgres database
  * `OAUTH_APP_ID=<app-id>` - You can get this by heading over to [Cardboard OAuth](https://auth.index-0.com), logging in and clicking `Admin Panel`
  * `OAUTH_SECRET=<app-secret>` - You'll get it with your `APP-ID`
  * `OAUTH_KEY=<app-key>` - You'll get it with your `APP-ID`
  * `DISCORD_KEY=<discord-key>` - You can get this from the [Discord Developer Panel](https://discord.com/developers)
  * `DISCORD_APPID=<discord-app-id>` - Not actually needed right now (but maybe later)
  * `DISCORD_TOKEN=<discord-token>` - You get it with your `discord-key`
  * `DISCORD_AI_URL=<url>` - URL to your instance of the [stable-diffusion API](https://github.com/calico-crusade/stable-diffusion-webui)
4. Run the `./deploy.sh` script

### Notes:
* You need the API and the databases in order to run the bot. 
* You need to setup your own copy of the [stable-diffusion API](https://github.com/calico-crusade/stable-diffusion-webui) to get AI image generation to work.
  * I will not provide the necessary configuration, models, and embeddings for stable-diffusion. You can find them in the parent repo's documentation. 

## Contributing
Go ahead and submit a PR with anything you want added, I'll review when I get the notification. 
I tend to just commit directly to `main` myself as I'm the sole developer on this project at the moment.

## Contact Me
You can reach me on discord [Cardboard#0001](https://discord.com/users/191100926486904833) or you can [join my server](https://discord.gg/RV9MvvYXsp).
My discord server is mostly my testing grounds for the bot and other things, so it's not really setup all that well.