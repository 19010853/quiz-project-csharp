# Docker Infrastructure Setup

> Tài liệu tổng hợp từ bài giảng *#11: Docker Infrastructure Setup* (TEDU).
> Liên quan: [MongoDB Introduction.md](MongoDB%20Introduction.md) · [Clean Architecture.md](Clean%20Architecture.md) · [Work Breakdown Structure.md](Work%20Breakdown%20Structure.md) (5.2, 5.3)

## Mục lục

1. [Docker vs Virtual Machine](#1-docker-vs-virtual-machine)
2. [Docker Architecture](#2-docker-architecture)
3. [Docker Overview](#3-docker-overview)
4. [Docker Image và Container](#4-docker-image-và-container)
5. [Docker Image Layers](#5-docker-image-layers)
6. [Setup Infrastructure](#6-setup-infrastructure)
7. [Docker run](#7-docker-run)
8. [Docker Compose](#8-docker-compose)
9. [Các lệnh Docker thường dùng](#9-các-lệnh-docker-thường-dùng)
10. [Tài liệu tham khảo](#10-tài-liệu-tham-khảo)

---

## 1. Docker vs Virtual Machine

```mermaid
flowchart TB
    subgraph VM["Virtual Machines"]
        direction TB
        V1["App 1<br/>Bins/Libs<br/>Guest OS"] & V2["App 2<br/>Bins/Libs<br/>Guest OS"] & V3["App 3<br/>Bins/Libs<br/>Guest OS"]
        HV["Hypervisor"]
        HOS["Host Operating System"]
        INF1["Infrastructure"]
        V1 & V2 & V3 --> HV --> HOS --> INF1
    end
    subgraph CT["Containers"]
        direction TB
        C1["App 1<br/>Bins/Libs"] & C2["App 2<br/>Bins/Libs"] & C3["App 3<br/>Bins/Libs"]
        DE["Docker Engine"]
        OS["Operating System"]
        INF2["Infrastructure"]
        C1 & C2 & C3 --> DE --> OS --> INF2
    end
```

| Tiêu chí | Virtual Machine | Docker Container |
|---|---|---|
| Thành phần | Mỗi VM có **Guest OS riêng** | Các container **dùng chung kernel** của OS host |
| Lớp ảo hóa | Hypervisor | Docker Engine |
| Kích thước | Lớn (vài GB) | Nhỏ (vài MB đến vài trăm MB) |
| Khởi động | Chậm (vài phút) | Nhanh (vài giây) |
| Tài nguyên | Tốn nhiều RAM/CPU | Nhẹ, chạy được nhiều container trên một máy |
| Mức cô lập | Cô lập hoàn toàn ở mức OS | Cô lập ở mức process |

## 2. Docker Architecture

Docker hoạt động theo mô hình **Client – Host – Registry**:

```mermaid
flowchart LR
    subgraph Client
        CLI[">_ Docker CLI"]
        API["Remote API"]
    end
    subgraph Host
        D["Docker Daemon"]
        subgraph Containers
            C1["Container 1"]
            C2["Container 2"]
        end
        subgraph Images
            I1["Image"]
        end
    end
    subgraph Registry
        HUB["Docker Hub /<br/>Private Registry"]
    end

    CLI -->|build / pull / run| D
    API --> D
    D -->|build| I1
    D -->|run| C1
    D -->|pull| HUB
    HUB -->|image| I1
```

| Thành phần | Vai trò |
|---|---|
| **Client** | Nơi người dùng gửi lệnh: Docker CLI (`docker ...`) hoặc Remote API |
| **Host / Daemon** | `dockerd` nhận lệnh, quản lý image, container, network, volume |
| **Registry** | Kho chứa image: **Docker Hub** (public) hoặc registry riêng |

Ba luồng chính:
- **BUILD**: Client yêu cầu Daemon build image từ Dockerfile.
- **PULL**: Daemon tải image từ Registry về Host.
- **RUN**: Daemon tạo và chạy container từ image.

## 3. Docker Overview

```mermaid
flowchart LR
    UI["Docker Desktop UI"] --> DM
    CLI["Docker CLI"] --> DM
    U(["👤 User"]) -->|Host IP : port| C1

    REPO["Docker Image Repository<br/>(Docker Images)"] --> DM

    subgraph Host
        subgraph DM["Docker Daemon"]
            C1["Docker Container 1"]
            C2["Docker Container 2"]
            VOL["Volumes"]
            NET["Network"]
        end
        DISK[("Disk space")]
    end

    C1 --> VOL --> DISK
    C1 --> NET
    C2 --> NET
```

- **Docker Desktop UI / Docker CLI**: hai cách để điều khiển Docker Daemon.
- **Docker Image Repository**: nơi lưu image, Daemon pull về để tạo container.
- **Volumes**: lưu dữ liệu của container ra **disk** của host, để dữ liệu **không mất khi xóa container**.
- **Network**: các container giao tiếp với nhau qua mạng nội bộ của Docker.
- **User** truy cập ứng dụng trong container thông qua **Host IP + port** được map ra ngoài.

## 4. Docker Image và Container

```mermaid
flowchart LR
    F["📄 Dockerfile"] -->|build| I["🐳 Docker Image"] -->|run| C["📦 Docker Container"]
```

| Khái niệm | Mô tả | Tương tự trong lập trình |
|---|---|---|
| **Dockerfile** | File text chứa các lệnh để build image | Source code |
| **Image** | Bản đóng gói **read-only** gồm app, thư viện, cấu hình | Class |
| **Container** | Một instance đang chạy của image | Object |

Ví dụ Dockerfile cho một ASP.NET Core API:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/Quiz.Admin.Api -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Quiz.Admin.Api.dll"]
```

## 5. Docker Image Layers

```mermaid
flowchart TB
    subgraph Private["Private (riêng từng container)"]
        CA["Container A<br/>Writable Layer A"]
        CB["Container B<br/>Writable Layer B"]
    end
    subgraph Shared["Shared (dùng chung, read-only)"]
        L2["Layer 2 (ví dụ: Configuration)"]
        L1["Layer 1 (ví dụ: MySQL)"]
        BASE["Base Image (ví dụ: Ubuntu)"]
    end
    CA --> L2
    CB --> L2
    L2 --> L1 --> BASE
```

- Image được tạo từ **nhiều layer xếp chồng**. Mỗi lệnh trong Dockerfile tạo ra một layer.
- Các layer của image là **read-only** và được **dùng chung** giữa nhiều container, nên tiết kiệm dung lượng.
- Mỗi container có một **Writable Layer riêng**. Mọi thay đổi khi container chạy chỉ ghi vào layer này.
- **Snapshot**: có thể chụp Writable Layer thành một layer mới (`docker commit`) để tạo image mới: `Base Image → Layer 1 → Layer 2 (mới)`.

> Vì Writable Layer bị xóa cùng container, dữ liệu cần giữ lại (ví dụ dữ liệu database) phải lưu trong **Volume**.

## 6. Setup Infrastructure

| Dịch vụ | Loại | Công cụ quản lý (theo slide) | Ghi chú |
|---|---|---|---|
| **MongoDB** | NoSQL | **Robo3T** (desktop), **Mongo Express** (web UI) | Robo 3T đã ngừng phát triển; có thể dùng **MongoDB Compass** hoặc extension **MongoDB for VS Code** |
| **SQL Server** | RDBMS | **Azure Data Studio** (Mac), **SQL Server Management Studio** (Windows) | Azure Data Studio đã bị Microsoft cho ngừng hỗ trợ; trên Mac có thể dùng extension **SQL Server (mssql)** cho VS Code |

Hai cách dựng dịch vụ:
- **`docker run`**: chạy từng container bằng lệnh.
- **Docker Compose**: khai báo nhiều service trong file **`.yml` / `.yaml`** rồi chạy cùng lúc.

Trong Quiz Project:
- **MongoDB** lưu dữ liệu Quiz (Question, Exam, ExamResult…).
- **SQL Server** lưu dữ liệu **Identity** (user, role, client của Identity Server).

## 7. Docker run

```powershell
# SQL Server
docker run -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=Admin@123$' -p 1433:1433 -d mcr.microsoft.com/mssql/server:2017-latest

# Liệt kê container
docker ps            # hoặc: docker container ls

# MongoDB
docker run -d --name mongodb -e MONGO_INITDB_ROOT_USERNAME=mongoadmin -e 'MONGO_INITDB_ROOT_PASSWORD=Admin@123$' -p 127.0.0.1:27017:27017 mongo
```

> Chọn **một** trong hai cách: `docker run` ở đây **hoặc** `docker compose` ở mục 8. Cả hai cùng tạo container tên `mongodb`, chạy cả hai sẽ bị trùng tên.

| Tham số | Ý nghĩa |
|---|---|
| `-d` | Chạy ngầm (detached) |
| `--name` | Đặt tên container |
| `-p host:container` | Map port từ host vào container |
| `-e` | Biến môi trường |
| `-v volume:path` | Gắn volume để lưu dữ liệu bền vững |

## 8. Docker Compose

File [docker-compose.yaml](docker-compose.yaml) ở thư mục gốc của dự án:

```yaml
services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2017-latest
    container_name: sqlserver
    restart: unless-stopped
    ports:
      - "1433:1433"
    environment:
      ACCEPT_EULA: "Y"
      # "$$" là cách escape ký tự "$" trong docker compose → mật khẩu thực tế: Admin@123$
      SA_PASSWORD: "Admin@123$$"
    volumes:
      - sqlserver_data:/var/opt/mssql

  mongodb:
    image: mongo
    container_name: mongodb
    restart: unless-stopped
    ports:
      - "127.0.0.1:27017:27017"
    environment:
      MONGO_INITDB_ROOT_USERNAME: mongoadmin
      MONGO_INITDB_ROOT_PASSWORD: "Admin@123$$"
      MONGO_INITDB_DATABASE: QuizExamProject
    volumes:
      - mongo_data:/data/db

volumes:
  sqlserver_data:
  mongo_data:
```

```powershell
docker compose up -d      # khởi động toàn bộ service
docker compose ps         # xem trạng thái
docker compose logs -f    # xem log
docker compose down       # dừng và xóa container (giữ lại volume)
docker compose down -v    # dừng, xóa container và cả volume (mất dữ liệu)
```

Sau khi chạy:
- MongoDB connection string: `mongodb://mongoadmin:Admin%40123%24@localhost:27017/QuizExamProject?authSource=admin`
- SQL Server: `Server=localhost,1433;User Id=sa;Password=Admin@123$;TrustServerCertificate=True`

> - Trong connection string MongoDB, mật khẩu phải được **URL-encode**: `@` → `%40`, `$` → `%24`.
> - `authSource=admin` là bắt buộc, vì user root được tạo trong database `admin`, không phải trong `QuizExamProject`.

> - Trong cùng một compose, các service gọi nhau bằng **tên service** (ví dụ `mongodb:27017`), không dùng `localhost`.
> - Mật khẩu ở trên chỉ dùng cho môi trường dev. Không commit mật khẩu thật lên Git, nên dùng file `.env`.
> - Mật khẩu SQL Server phải đủ mạnh (tối thiểu 8 ký tự, có chữ hoa, chữ thường, số và ký tự đặc biệt), nếu không container sẽ dừng ngay khi khởi động.

## 9. Các lệnh Docker thường dùng

| Nhóm | Lệnh | Ý nghĩa |
|---|---|---|
| Image | `docker images` | Liệt kê image |
| | `docker pull <image>` | Tải image từ registry |
| | `docker build -t <name> .` | Build image từ Dockerfile |
| | `docker rmi <image>` | Xóa image |
| Container | `docker ps` / `docker ps -a` | Liệt kê container đang chạy / tất cả |
| | `docker start` / `stop` / `restart <name>` | Bật / tắt / khởi động lại container |
| | `docker rm <name>` | Xóa container |
| | `docker logs -f <name>` | Xem log |
| | `docker exec -it <name> bash` | Mở shell bên trong container |
| Volume | `docker volume ls` | Liệt kê volume |
| Network | `docker network ls` | Liệt kê network |
| Dọn dẹp | `docker system prune` | Xóa tài nguyên không còn dùng |

## 10. Tài liệu tham khảo

- Home: https://www.docker.com/
- Public registry: https://hub.docker.com/
- Docker Compose: https://docs.docker.com/compose/
- Install (Mac): https://docs.docker.com/docker-for-mac/install/
- Install (Windows): https://docs.docker.com/desktop/setup/install/windows-install/
