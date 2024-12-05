# Kasta
A Simple self-hostable File Sharing Service written in C#. Supports displaying media (image, video, audio), ShareX config generation, [rustgrab](https://github.com/ktwrd/rustgrab) support, and stores files with any S3-compatible service.

## Features
- User Management
- User Registration
- 2FA
- Config Generator for ShareX and [rustgrab](https://github.com/ktwrd/rustgrab)
- Compatible with AWS S3 and S3-Compatible Services (like Cloudflare R2 and MinIO)
- Web File Upload
- Audit Logging
- Per-user storage quota & file size limits
- Public & Private uploads
- Image Preview Generation (including image info like file type, compression, interlacing, and dimensions)
- Text File Preview

## Installing
It is recommended to use Docker to make updating very easy. The following docker compose file is the recommended setup;
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
    image: ktwrd/kasta:latest
    environment:
      DATABASE_HOST: db
      DATABASE_USER: kasta
      DATABASE_PASSWORD: changeme123
      DATABASE_NAME: kasta
      DeploymentEndpoint: "https://kasta.example.com"
      S3_ServiceUrl: "http://s3.us-east-1.amazonaws.com"
      S3_AccessKey: "xxxxxx"
      S3_AccessSecret: "xxxxxx"
      S3_Bucket: "my-kasta-bucket"
    depends_on:
      - db
volumes:
  local_pgdata:
```