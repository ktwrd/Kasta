# Kasta
![GitHub Release](https://img.shields.io/github/v/release/ktwrd/kasta)
![GitHub branch check runs](https://img.shields.io/github/check-runs/ktwrd/kasta/main) 
![GitHub License](https://img.shields.io/github/license/ktwrd/kasta)
[![brainmade.org](https://img.shields.io/badge/brainmade.org-FFFFFF?style=plastic&logo=data%3Aimage%2Fsvg%2Bxml%3Bbase64%2CPHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHdpZHRoPSIxZW0iIGhlaWdodD0iNzkiIHZpZXdCb3g9IjAgMCA2NyA3OSIgZmlsbD0ibm9uZSI%2BPHBhdGggZmlsbD0iIzAwMCIgZD0iTTUyLjYxMiA3OC43ODJIMjMuMzNhMi41NTkgMi41NTkgMCAwIDEtMi41Ni0yLjU1OHYtNy42NzdoLTcuOTczYTIuNTYgMi41NiAwIDAgMS0yLjU2LTIuNTZWNTUuMzE1bC04LjgyLTQuMzk3YTIuNTU5IDIuNTU5IDAgMCAxLS45ODYtMy43MWw5LjgwNy0xNC43MTR2LTQuMzVDMTAuMjQgMTIuNTk5IDIyLjg0MyAwIDM4LjM4OCAwIDUzLjkzMiAwIDY2LjUzNCAxMi42IDY2LjUzOCAyOC4xNDNjLS42MzIgMjcuODI0LTEwLjc2IDIzLjUxNi0xMS4xOCAzNC4wNDVsLS4xODcgMTQuMDM1YTIuNTkgMi41OSAwIDAgMS0uNzUgMS44MSAyLjU1IDIuNTUgMCAwIDEtMS44MDkuNzVabS0yNi43MjMtNS4xMTdoMjQuMTY0bC4yODYtMTQuNTQyYy0uMjYzLTYuNjU2IDExLjcxNi04LjI0MyAxMS4wOC0zMC43MzQtLjM1OC0xMi43MTMtMTAuMzEzLTIzLjI3MS0yMy4wMzEtMjMuMjcxLTEyLjcxOCAwLTIzLjAyOSAxMC4zMDctMjMuMDMyIDIzLjAyNXY1LjExN2MwIC41MDYtLjE1IDEtLjQzIDEuNDJsLTguNjMgMTIuOTQxIDcuNjQ1IDMuODJhMi41NTkgMi41NTkgMCAwIDEgMS40MTUgMi4yOTF2OS42OTdoNy45NzRhMi41NTkgMi41NTkgMCAwIDEgMi41NiAyLjU1OXY3LjY3N1oiLz48cGF0aCBmaWxsPSIjMDAwIiBkPSJNNDAuMzcyIDU4LjIyMlYzOC45MzRjLjExOCAwIC4yMzcuMDE4LjM1NS4wMTggOS43NjktLjAxMiAxNy4wNS05LjAxMiAxNS4wMjItMTguNTY3YTIuMzY2IDIuMzY2IDAgMCAwLTEuODIxLTEuODIyYy04LjEwNi0xLjczLTE2LjEyMSAzLjI5Mi0xOC4wOTggMTEuMzQxLS4wMjQtLjAyNC0uMDQzLS4wNS0uMDY2LS4wNzNhMTUuMzIzIDE1LjMyMyAwIDAgMC0xNC4wNi00LjE3IDIuMzY1IDIuMzY1IDAgMCAwLTEuODIxIDEuODJjLTIuMDI4IDkuNTU1IDUuMjUyIDE4LjU1NCAxNS4wMiAxOC41NjguMjM2IDAgLjQ5Mi0uMDI4LjczOC0uMDR2MTIuMjEzaDQuNzMxWm0yLjgzOS0zMi4xNDNhMTAuNjQ2IDEwLjY0NiAwIDAgMSA4LjEyNC0zLjEwNmMuMzUgNi4zNC00Ljg4OCAxMS41NzctMTEuMjI4IDExLjIzYTEwLjU4IDEwLjU4IDAgMCAxIDMuMTA0LTguMTI0Wk0yNy40MDMgMzguMTkzYTEwLjYwNyAxMC42MDcgMCAwIDEtMy4xMTgtOC4xMjNjNi4zNDQtLjM1OCAxMS41ODcgNC44ODYgMTEuMjI4IDExLjIzLTMuMDIzLjE2OS01Ljk3My0uOTYxLTguMTEtMy4xMDdaIi8%2BPC9zdmc%2B)](https://brainmade.org/)

A Simple self-hostable File Sharing Service written in C#. Supports displaying media (image, video, audio), [ShareX](https://github.com/ShareX/ShareX) config generation, [rustgrab](https://github.com/ktwrd/rustgrab) support, and stores files with pretty much any S3-compatible service.

>[!IMPORTANT]
> Kasta is currently in Alpha!
> Many things are subject to change, and features may not be fully complete.

![Screenshot of the Kasta web application, displaying a list of uploaded images with their; filename, date created, file size, sharing status, and file size.](https://kate.pet/img/blog/firefox_1506_K10H17LkIG.png)

## Features
- User Management
- User Registration
- 2FA
- Config Generator for [ShareX](https://github.com/ShareX/ShareX) and [rustgrab](https://github.com/ktwrd/rustgrab)
- Compatible with AWS S3 and S3-Compatible Services (like Cloudflare R2, MinIO, and Wasabi[^1])
- Web File Upload
- Audit Logging[^2]
- Per-user storage quota & file size limits
- Public & Private uploads
- Image Preview Generation (including image info like file type, compression, interlacing, and dimensions)
- Text File Preview[^3]
- Link Shortener[^4]

## Installing
It is recommended to use Docker to make updating very easy. The following docker compose file is the recommended setup, along with copying `config.example.xml` to `config.xml` and change the settings in there so it fits your needs.
```yml
services:
  db:
    image: postgres:18-alpine
    environment:
      POSTGRES_PASSWORD: changeme123
      POSTGRES_USER: kasta
      POSTGRES_DB: kasta
    logging:
      driver: "none"
    restart: unless-stopped
    volumes:
      - local_pgdata:/var/lib/postgresql/data
  web:
    image: ghcr.io/ktwrd/kasta:latest
    environment:
      # optional setting
      SentryDsn: https://xxxx@sentry.example.com/1
    ports:
      - "127.0.0.1:8080:8080" # will only forward ports to localhost for security reasons.
    volumes:
	  # Configuration file defaults to /config/kasta.xml
      - ./kasta-config/:/config
    depends_on:
      - db
volumes:
  local_pgdata:
```

### Important Note

The first user that is created will be given the "Administrator" role, so make sure that your deployment is **only accessible locally at first**. By default, anyone can register for an account, so secure your deployment in the "System" tab when you first log in.

## Database Migrations for Development
When trying to do database migrations for development, make sure that the `CONFIG_LOCATION` environment variable is set to where your `config.xml` file is located so the Database Context can successfully create a connection string.

## 520 with Cloudflare
This occurs because the response header is >8kb. This can be fixed by disabling HTTP/2 support (`/speed/optimization/protocol`), and setting the following value in your NGINX config;
```
server {
  ...
  large_client_header_buffers 4 32k;
  ...
}
```

## NGINX Configuration
The following is the recommended NGINX site configuration for `proxy_pass` (only subdomain proxy is officially supported)

```nginx
server {
    listen 443 ssl;
    # Make sure that your SSL parameters are set here!!
    server_name kasta.example.com;

    access_log  /var/log/nginx/access.log;
    client_max_body_size 500M;

    # Fix for 520 Cloudflare error (see section above)
    large_client_header_buffers 4 32k;

    location / {
        # Make sure that the port is correct!
        proxy_pass http://127.0.0.1:5280/;
        proxy_buffer_size 32k;
        proxy_buffers 8 32k;
        proxy_busy_buffers_size 64k;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
server {
    server_name kasta.example.com;
    listen 80;
    return 301 https://$http_host$request_uri;
}
```

## Contributing

When creating new database migrations, both Sqlite and PostgreSQL migrations need to be made. So it runs properly, please put a `config.xml` file in the `Kasta.Data` directory with your configured PostgreSql database for creating the migrations for `PostgresDbContext`.

Creating migrations for `SqliteDbContext` requires no config file.

```bash
cd ./Kasta.Data
dotnet ef migrations add $migration_name --context PostgresDbContext --output-dir Migrations
dotnet ef migrations add $migration_name --context SqliteDbContext --output-dir SqliteMigrations
```

The `PostgresDbContext` has the output directory of `Migrations` since Kasta currently is a Postgres-First project, where Sqlite is only recommended for having a lightweight development environment.

It's also recommended to use local file storage when running a development instance of Kasta, but in production an S3-compatible server is strongly recommended (e.g: SeaweedFS, RustFS, Cloudflare r2, etc...)

## Footnotes
[^1]: Kasta has only been tested with AWS S3 and MinIO (LAN Deployment). When using an S3 Compatible Service (that isn't AWS S3), then make sure that the `ForcePathStyle` element in your S3 configuration is set to `true` (which is nested in the `S3` element in `config.example.xml`).
[^2]: Audit logging has not been fully implemented, but complete audit logging and webhooks are planned in a future release.
[^3]: Currently the text file preview has a hard-coded file size & extension limit. In the future, this will be fully configurable in the frontend.
[^4]: The Link Shortener is a bit buggy right now, and the text is not that legible with dark themes. This will be fixed in a future release.
