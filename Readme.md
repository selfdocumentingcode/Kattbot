# Kattbot: A Meowchine for Discord Chats

![kattbot_banner_960_320](https://github.com/selfdocumentingcode/Kattbot/assets/8089471/65a5cd5c-4f07-453b-9bcf-07d4a76e5fe5)

Hey there! This is Kattbot. It's a small bot I developed for fun, and it comes with a handful of nifty features. If you're checking out this repo, here's a rundown of what Kattbot does:

- **Chatting with ChatGPT:** Kattbot uses ChatGPT to chat. It's not perfect, but it's decent company on a quiet day.
- **Image Creation with DALL-E:** You can give Kattbot text prompts, and it'll use DALL-E to turn them into images. It can also make variations of existing pics.
- **Image Effects:** Beyond creating images, Kattbot can tweak them a bit. It can deepfry, oil paint, twirl, and pet emojis, avatars, or other images. Need a super-sized emoji? It does that too.
- **Emote Tracking:** Just a little feature that tracks which server emotes are used on the server. Fun for some quick stats on user and emote activity.
- **A Simple Meow:** If you ask nicely with `kattpls meow`, Kattbot will give you a meow. Why? Why not?

If you're a dev and think of any improvements or have some cool ideas, contributions are always welcome. Thanks for checking out Kattbot!

## Local developement

### Requirements

-   [.NET 8 SDK](https://dotnet.microsoft.com/download/visual-studio-sdks)
-   [Visual Studio](https://visualstudio.microsoft.com/) (or any other preferred editor + dotnet command line tool)
-   [PostgreSQL 15+](https://www.postgresql.org/)

### Secrets

Configure the bot token, the "Open"AI key, and connection string in user secrets:

```
"Kattbot:BotToken": "TOKEN_GOES_HERE"​
"Kattbot:OpenAiApiKey": "API_KEY_GOES_HERE"​
"Kattbot:ConnectionString": "CONN_STRING_GOES_HERE"​
```

or as environment variables:

```
Kattbot__BotToken=TOKEN_GOES_HERE
Kattbot__OpenAiApiKey=API_KEY_GOES_HERE
Kattbot__ConnectionString=CONN_STRING_GOES_HERE
```

#### Connection string format

`Server=_DB_SERVER_IP_;Database=_DB_NAME_;User Id=_DB_USER_;Password=_DB_PASSWORD_;`

### Run from Visual Studio or VS Code with C# Dev Kit extension

Set `Kattbot` as the startup project and run the project using `Kattbot` profile.

### Run from dotnet command line tool

`dotnet run --project Kattbot`

### Run as a Docker container in Visual Studio using Fast Mode

#### Kattbot project only

Set `Kattbot` as the startup project and run the project using `Docker` profile.

#### Kattbot and PostgreSQL

Set `docker-vs` as the startup project and run the project using `Docker Compose` profile.

Optionally, use `Compose \W PgAdmin` profile to include a PgAdmin container.

### Run as a Docker container from the command line

#### Kattbot project only

`docker build -t kattbot -f docker/Dockerfile .`

`docker run -d --name kattbot kattbot`

#### Kattbot and PostgreSQL

`docker-compose -f docker/docker-compose.yml up`

Optionally, pass the `--profile tools` flag to include a PgAdmin container.

## Credits

- [DSharp+](https://github.com/DSharpPlus/DSharpPlus) .NET Discord wrapper
- [ImageSharp](https://github.com/SixLabors/ImageSharp) used for image manipulation
- [CommandLineParser](https://github.com/j-maly/CommandLineParser) used for parsing bot command arguments
- [MediatR](https://github.com/jbogard/MediatR) used in CQRS pattern implementation
- [Npgsql](https://github.com/npgsql/npgsql) .NET data provider for PostgreSQL
- [PuppeteerSharp](https://github.com/hardkoded/puppeteer-sharp) .NET port of Puppeteer Node library used for petting images
- [NSubstitute](https://github.com/nsubstitute/NSubstitute) used for mocking in unit tests
- [TiktokenSharp](https://github.com/aiqinxuancai/TiktokenSharp) used for calculating token count of OpenAI chat messages
