# 📦 Task Logger System with Persistence

Sistem distributed task processing menggunakan **NATS JetStream** dan **.NET 9** yang mendukung persistent messaging, multiple workers, dan real-time monitoring.

## 🎯 Tujuan Pembelajaran

- ✅ Memahami **NATS JetStream** untuk persistent messaging
- ✅ Implementasi **Work Queue Pattern** dengan multiple workers
- ✅ Implementasi **Pub/Sub Pattern** untuk real-time logs
- ✅ Async task processing di .NET
- ✅ Message acknowledgement (Ack/Nak)
- ✅ Durable consumers dan replay capability

## 🏗️ Arsitektur Sistem

```
┌─────────────────┐         ┌─────────────────┐         ┌─────────────────┐
│   Publisher     │         │    Worker 1     │         │    Worker 2     │
│  (CLI Submit)   │         │  (Console App)  │         │  (Console App)  │
└────────┬────────┘         └────────┬────────┘         └────────┬────────┘
         │                           │                           │
         │  Publish Task             │  Pull & Process           │  Pull & Process
         │                           │                           │
         └───────────────────────────┼───────────────────────────┘
                                     │
                              ┌──────▼──────┐
                              │ NATS Server │
                              │  JetStream  │
                              └──────┬──────┘
                                     │
                                     │  Subscribe
                                     │
                              ┌──────▼──────┐
                              │  Dashboard  │
                              │ (Monitoring)│
                              └─────────────┘

Streams:
- TASKS    → Work queue untuk task processing (WorkQueue retention)
- LOGS     → Event log untuk monitoring (7 hari retention)

Subjects:
- tasks.pending       → Task yang menunggu diproses
- logs.{taskId}      → Log per task
- status.{taskId}    → Status update per task
```

## 📁 Struktur Folder

```
TaskLoggerSystem/
├── docker-compose.yml                      # NATS Server setup
├── TaskLogger.sln                          # Solution file
│
├── TaskLogger.Core/                        # Domain Layer
│   ├── Models/
│   │   ├── TaskPayload.cs                 # Task entity
│   │   ├── TaskLog.cs                     # Log entity
│   │   └── TaskStatus.cs                  # Status entity & enum
│   │
│   ├── DTOs/
│   │   ├── TaskSubmitDto.cs
│   │   ├── TaskStatusDto.cs
│   │   └── TaskLogDto.cs
│   │
│   └── TaskLogger.Core.csproj
│
├── TaskLogger.Infrastructure/              # Messaging & Storage Layer
│   ├── Messaging/
│   │   ├── INatsConnection.cs             # NATS connection interface
│   │   ├── NatsConnection.cs              # NATS connection + stream setup
│   │   ├── ITaskPublisher.cs              # Publisher interface
│   │   └── TaskPublisher.cs               # Task/Log/Status publisher
│   │
│   ├── Storage/
│   │   ├── ITaskLogStore.cs
│   │   └── FileTaskLogStore.cs
│   │
│   └── TaskLogger.Infrastructure.csproj
│
├── TaskLogger.Worker/                      # Background Worker
│   ├── Program.cs
│   ├── TaskWorker.cs                      # Task processor
│   ├── appsettings.json
│   └── TaskLogger.Worker.csproj
│
├── TaskLogger.Publisher/                   # CLI untuk submit task
│   ├── Program.cs
│   ├── appsettings.json
│   └── TaskLogger.Publisher.csproj
│
└── TaskLogger.Dashboard/                   # Monitoring Dashboard
    ├── Program.cs
    ├── appsettings.json
    └── TaskLogger.Dashboard.csproj
```

## 🚀 Cara Setup dari Awal

### **Prerequisites**

- ✅ .NET 9 SDK ([Download](https://dotnet.microsoft.com/download))
- ✅ Docker Desktop ([Download](https://www.docker.com/products/docker-desktop))
- ✅ Visual Studio Code atau IDE lainnya

### **1. Clone atau Buat Project**

```bash
# Buat folder project
mkdir TaskLoggerSystem
cd TaskLoggerSystem

# Buat solution
dotnet new sln -n TaskLoggerSystem

# Buat projects
dotnet new classlib -n TaskLogger.Core
dotnet new classlib -n TaskLogger.Infrastructure
dotnet new console -n TaskLogger.Worker
dotnet new console -n TaskLogger.Publisher
dotnet new console -n TaskLogger.Dashboard

# Tambahkan ke solution
dotnet sln add TaskLogger.Core/TaskLogger.Core.csproj
dotnet sln add TaskLogger.Infrastructure/TaskLogger.Infrastructure.csproj
dotnet sln add TaskLogger.Worker/TaskLogger.Worker.csproj
dotnet sln add TaskLogger.Publisher/TaskLogger.Publisher.csproj
dotnet sln add TaskLogger.Dashboard/TaskLogger.Dashboard.csproj
```

### **2. Setup Dependencies**

```bash
# TaskLogger.Infrastructure depends on Core
cd TaskLogger.Infrastructure
dotnet add reference ../TaskLogger.Core/TaskLogger.Core.csproj

# TaskLogger.Worker depends on Core + Infrastructure
cd ../TaskLogger.Worker
dotnet add reference ../TaskLogger.Core/TaskLogger.Core.csproj
dotnet add reference ../TaskLogger.Infrastructure/TaskLogger.Infrastructure.csproj

# TaskLogger.Publisher depends on Core + Infrastructure
cd ../TaskLogger.Publisher
dotnet add reference ../TaskLogger.Core/TaskLogger.Core.csproj
dotnet add reference ../TaskLogger.Infrastructure/TaskLogger.Infrastructure.csproj

# TaskLogger.Dashboard depends on Core + Infrastructure
cd ../TaskLogger.Dashboard
dotnet add reference ../TaskLogger.Core/TaskLogger.Core.csproj
dotnet add reference ../TaskLogger.Infrastructure/TaskLogger.Infrastructure.csproj
```

### **3. Install NuGet Packages**

```bash
# TaskLogger.Infrastructure
cd TaskLogger.Infrastructure
dotnet add package NATS.Client
dotnet add package NATS.Client.JetStream

# TaskLogger.Worker
cd ../TaskLogger.Worker
dotnet add package NATS.Client
dotnet add package NATS.Client.JetStream
dotnet add package Spectre.Console

# TaskLogger.Publisher
cd ../TaskLogger.Publisher
dotnet add package NATS.Client
dotnet add package NATS.Client.JetStream
dotnet add package Spectre.Console

# TaskLogger.Dashboard
cd ../TaskLogger.Dashboard
dotnet add package NATS.Client
dotnet add package NATS.Client.JetStream
dotnet add package Spectre.Console

cd ..
```

### **4. Buat Docker Compose File**

Buat file `docker-compose.yml` di root folder:

```yaml
version: '3.8'

services:
  nats:
    image: nats:latest
    container_name: nats-server
    ports:
      - "4222:4222"   # Client connections
      - "8222:8222"   # HTTP monitoring
      - "6222:6222"   # Cluster routes
    command: 
      - "-js"         # Enable JetStream
      - "-sd"         # Store directory
      - "/data"
      - "-m"          # Enable monitoring
      - "8222"
    volumes:
      - nats-data:/data
    networks:
      - task-logger-network

volumes:
  nats-data:

networks:
  task-logger-network:
    driver: bridge
```

### **5. Start NATS Server**

```bash
docker-compose up -d

# Cek status
docker ps

# Harusnya ada container nats-server running
```

### **6. Build All Projects**

```bash
dotnet build
```

## 🎮 Cara Menjalankan Aplikasi

### **Skenario 1: Testing Dasar (1 Worker)**

#### **Terminal 1 - Start Worker**

```bash
cd TaskLogger.Worker
dotnet run
```

Output yang diharapkan:
```
 _____         _      __        __         _             
|_   _|_ _ ___| | __ \ \      / /__  _ __| | _____ _ __ 
  | |/ _` / __| |/ /  \ \ /\ / / _ \| '__| |/ / _ \ '__|
  | | (_| \__ \   <    \ V  V / (_) | |  |   <  __/ |   
  |_|\__,_|___/_|\_\    \_/\_/ \___/|_|  |_|\_\___|_|   

Stream TASKS already exists
Stream LOGS already exists
Worker worker-MYPC-abc12345 started
```

#### **Terminal 2 - Start Dashboard**

```bash
cd TaskLogger.Dashboard
dotnet run
```

Output yang diharapkan:
```
 _____         _      ____            _     _                         _ 
|_   _|_ _ ___| | __ |  _ \  __ _ ___| |__ | |__   ___   __ _ _ __ __| |
  | |/ _` / __| |/ / | | | |/ _` / __| '_ \| '_ \ / _ \ / _` | '__/ _` |
  | | (_| \__ \   <  | |_| | (_| \__ \ | | | |_) | (_) | (_| | | | (_| |
  |_|\__,_|___/_|\_\ |____/ \__,_|___/_| |_|_.__/ \___/ \__,_|_|  \__,_|

[Real-time dashboard dengan tabel task dan logs]
```

#### **Terminal 3 - Submit Tasks**

```bash
cd TaskLogger.Publisher
dotnet run
```

Menu yang muncul:
```
? What would you like to do?
  > Submit Task
    Quick Submit (5 tasks)
    Exit
```

**Pilih "Quick Submit (5 tasks)"** → Akan submit 5 task sekaligus

#### **Hasil yang Diharapkan:**

- ✅ **Worker**: Akan process task satu per satu
- ✅ **Dashboard**: Update real-time dengan status task + logs
- ✅ **Publisher**: Konfirmasi task submitted

---

### **Skenario 2: Multiple Workers (Load Balancing)**

#### **Terminal 4 - Start Worker ke-2**

```bash
cd TaskLogger.Worker
dotnet run
```

Sekarang ada **2 workers** yang berjalan!

#### **Test Load Balancing:**

1. Di Publisher, submit 10 tasks sekaligus
2. Lihat di Dashboard: **task akan dibagi antara 2 workers**
3. Setiap worker akan ambil task dari queue secara round-robin

---

### **Skenario 3: Message Persistence (Worker Crash Recovery)**

#### **Test Crash Recovery:**

1. Submit 10 tasks dari Publisher
2. **Saat worker sedang process, tekan `Ctrl+C`** (matikan worker)
3. Lihat di Dashboard: task yang belum selesai akan kembali ke state **Pending**
4. **Start worker lagi**: `dotnet run`
5. Worker akan **lanjutkan task yang belum selesai** ✨

Ini mendemonstrasikan **message persistence** dengan JetStream!

---

## 📊 Monitoring Dashboard

Dashboard menampilkan:

### **1. Task Status Table**

```
┌─────────┬───────────┬──────────────────┬──────────┐
│Task ID  │ State     │ Worker           │ Duration │
├─────────┼───────────┼──────────────────┼──────────┤
│abc12345 │ Running   │ worker-PC-xyz    │ N/A      │
│def67890 │ Completed │ worker-PC-abc    │ 5.2s     │
│ghi11111 │ Pending   │ N/A              │ N/A      │
│jkl22222 │ Failed    │ worker-PC-xyz    │ 3.1s     │
└─────────┴───────────┴──────────────────┴──────────┘
```

**Status Warna:**
- 🟡 **Yellow** = Running
- 🟢 **Green** = Completed
- 🔴 **Red** = Failed
- ⚪ **Grey** = Pending

### **2. Recent Logs Panel**

```
┌─ Recent Logs ─────────────────────────────────────┐
│ 14:23:45 INFO Starting task: Download File        │
│ 14:23:46 INFO Progress: 1/5 - download-file       │
│ 14:23:47 INFO Progress: 2/5 - download-file       │
│ 14:23:48 INFO Progress: 3/5 - download-file       │
│ 14:23:49 INFO Progress: 4/5 - download-file       │
│ 14:23:50 INFO Progress: 5/5 - download-file       │
│ 14:23:50 INFO Task completed successfully         │
└───────────────────────────────────────────────────┘
```

Dashboard **auto-refresh setiap 500ms** untuk menampilkan data terbaru.

---

## 🧪 Testing Scenarios

### **Test 1: Submit Single Task**

```bash
# Di Publisher Terminal
# Pilih: Submit Task
Task name: Process Data
Task command: process-csv-file
Priority (1-10): 7
```

**Expected Result:**
- Task masuk queue
- Worker mulai process
- Dashboard update real-time
- Selesai dalam ~5 detik

---

### **Test 2: High Load Testing**

```bash
# Di Publisher Terminal
# Pilih: Quick Submit (5 tasks)
# Ulangi 5x (total 25 tasks)
```

**Expected Result:**
- Semua task masuk queue
- Worker process secara berurutan
- Dashboard menampilkan antrian

---

### **Test 3: Worker Scaling**

1. Start dengan 1 worker
2. Submit 20 tasks
3. Saat worker sedang process, **start 2 worker lagi**
4. Lihat task distribution di dashboard

**Expected Result:**
- Task otomatis terdistribusi ke 3 workers
- Processing time lebih cepat

---

### **Test 4: Failure Recovery**

1. Submit 10 tasks
2. Kill semua workers (`Ctrl+C`)
3. Tunggu 30 detik
4. Start 1 worker baru

**Expected Result:**
- Task yang belum selesai akan diproses ulang
- Tidak ada task yang hilang

---

## 🔧 Configuration

### **NATS Connection Settings**

Default connection: `nats://localhost:4222`

Untuk custom configuration, edit `NatsConnection.cs`:

```csharp
public NatsConnection(string natsUrl = "nats://localhost:4222")
{
    // Your custom config
}
```

### **JetStream Configuration**

**Stream: TASKS**
- Storage: File
- Retention: WorkQueue (dihapus setelah di-ack)
- Subjects: `tasks.*`

**Stream: LOGS**
- Storage: File
- Retention: 7 hari
- Subjects: `logs.*`

---

## 🐛 Troubleshooting

### **Problem: Connection Refused**

```
Error: nats: connection refused
```

**Solution:**
```bash
# Check NATS status
docker ps

# If not running, start it
docker-compose up -d

# Check logs
docker logs nats-server
```

---

### **Problem: Stream Already Exists**

```
NATSJetStreamException: stream name already in use
```

**Solution:**
Ini **normal** dan bisa diabaikan. Stream sudah dibuat sebelumnya.

---

### **Problem: Worker Tidak Process Task**

```
Worker started but no tasks are being processed
```

**Solution:**
```bash
# 1. Pastikan NATS running
docker ps

# 2. Restart worker
# Ctrl+C di terminal worker
dotnet run

# 3. Check NATS monitoring
# Buka browser: http://localhost:8222
```

---

### **Problem: Task Stuck di Pending**

```
Task tetap di state Pending
```

**Solution:**
```bash
# 1. Pastikan ada worker yang running
# 2. Check worker logs untuk error
# 3. Restart worker jika perlu
```

---

### **Problem: Ingin Reset Semua Data**

```bash
# Stop NATS dan hapus semua data
docker-compose down -v

# Start ulang
docker-compose up -d
```

---

## 📦 Dependencies

### **NuGet Packages:**

- `NATS.Client` - NATS client library
- `NATS.Client.JetStream` - JetStream support
- `Spectre.Console` - Beautiful console UI
- `System.Text.Json` - JSON serialization

### **Docker:**

- `nats:latest` - NATS server dengan JetStream

---

## 🎓 Konsep yang Dipelajari

### **1. NATS JetStream**

- **Stream**: Named persistent message log
- **Consumer**: Subscribes to stream dengan state management
- **Durable Consumer**: Consumer yang persist state-nya
- **Acknowledgement**: Ack (success) / Nak (retry)

### **2. Messaging Patterns**

- **Work Queue**: Task didistribusikan ke multiple workers
- **Pub/Sub**: Logs dikirim ke semua subscribers
- **Request-Reply**: (Bisa ditambahkan untuk monitoring)

### **3. .NET Async Patterns**

- `async/await` untuk non-blocking operations
- `Task.Delay()` untuk simulasi work
- `CancellationToken` untuk graceful shutdown

### **4. Distributed Systems**

- Load balancing otomatis
- Message persistence
- Failure recovery
- Horizontal scaling

---

## 🚀 Ide Pengembangan Selanjutnya

### **Level 1 - Improvements:**

- [ ] Task priority queue
- [ ] Retry mechanism dengan backoff
- [ ] Dead letter queue untuk failed tasks
- [ ] Task timeout handling

### **Level 2 - Features:**

- [ ] Web-based dashboard (ASP.NET Core + SignalR)
- [ ] Task scheduling (cron-like)
- [ ] Task dependencies (task A selesai → jalankan task B)
- [ ] File-based persistence untuk logs

### **Level 3 - Advanced:**

- [ ] Multi-stream processing
- [ ] Task result caching dengan Redis
- [ ] Metrics & alerting (Prometheus + Grafana)
- [ ] Distributed tracing (OpenTelemetry)

---

## 📚 Resources

- [NATS Documentation](https://docs.nats.io/)
- [JetStream Concepts](https://docs.nats.io/nats-concepts/jetstream)
- [.NET NATS Client](https://github.com/nats-io/nats.net)
- [Spectre.Console Documentation](https://spectreconsole.net/)

---

## 👨‍💻 Author

Dibuat untuk pembelajaran **NATS & .NET** - Project #2 dari 7 project progression.

**Next Project:** Monitoring Dashboard dengan ASP.NET Core + SignalR

---

## 📄 License

MIT License - Bebas digunakan untuk pembelajaran

---

## 💬 Feedback

Ada pertanyaan atau menemukan bug? Silakan buat issue atau diskusikan di chat!

Happy Coding! 🚀
