# Kasta
![GitHub Release](https://img.shields.io/github/v/release/ktwrd/kasta)
![GitHub branch check runs](https://img.shields.io/github/check-runs/ktwrd/kasta/main) 
![GitHub License](https://img.shields.io/github/license/ktwrd/kasta)

A Simple self-hostable File Sharing Service written in C#. Supports displaying media (image, video, audio), [ShareX](https://github.com/ShareX/ShareX) config generation, [rustgrab](https://github.com/ktwrd/rustgrab) support, and stores files with pretty much any S3-compatible service.

![Screenshot of the Kasta web application, displaying a list of uploaded images with their; filename, date created, file size, sharing status, and file size.](https://kate.pet/img/blog/firefox_1506_K10H17LkIG.png)

## Features
- User Management
- User Registration
- 2FA
- Config Generator for [ShareX](https://github.com/ShareX/ShareX) and [rustgrab](https://github.com/ktwrd/rustgrab)
- Compatible with AWS S3 and S3-Compatible Services (like Cloudflare R2, MinIO, and Wasabi[^1])
- Web File Upload
- Audit Logging
- Per-user storage quota & file size limits
- Public & Private uploads
- Image Preview Generation (including image info like file type, compression, interlacing, and dimensions)
- Text File Preview
- Link Shortener

## Installing
It is recommended to use Docker to make updating very easy. The following docker compose file is the recommended setup, along with copying `config.example.xml` to `config.xml` and change the settings in there so it fits your needs.
```yml
services:
  db:
    image: postgres:17-alpine
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
      - ./config.xml:/config.xml
    depends_on:
      - db
volumes:
  local_pgdata:
```

### Important Note

The first user that is created will be given the "Administrator" role, so make sure that it's only accessible to YOU when it's first deployed.

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

    # Fix for 520 Cloudflare errorr (see section above)
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

## Footnotes
[^1]: Kasta has only been tested with AWS S3 and MinIO (LAN Deployment). When using an S3 Compatible Service (that isn't AWS S3), then make sure that the `ForcePathStyle` element in your S3 configuration is set to `true` (which is nested in the `S3` element in `config.example.xml`).